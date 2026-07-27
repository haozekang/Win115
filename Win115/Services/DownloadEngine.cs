using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using Win115.Entities;
using Win115.Handlers;
using Win115.Models;

namespace Win115.Services
{
    public sealed class DownloadEngine
    {
        private const int MaxRetryCount = 4;
        private const int BufferSize = 128 * 1024;
        private const long MinimumSegmentSize = 4L * 1024 * 1024;
        private static readonly TimeSpan RequestTimeout = TimeSpan.FromMinutes(2);

        private readonly HttpClient _httpClient;
        private readonly SemaphoreSlim _connectionLimiter = new(8, 8);
        private readonly SystemInfoModel _system;
        private readonly object _throttleLock = new();
        private long _nextWriteTimestamp;
        private long _throttleBytesPerSecond;

        public DownloadEngine(SystemInfoModel system)
        {
            _system = system;
            var handler = new SocketsHttpHandler
            {
                AutomaticDecompression = DecompressionMethods.None,
                ConnectTimeout = TimeSpan.FromSeconds(30),
                MaxConnectionsPerServer = DownloadSettings.MaxSegmentCount * DownloadSettings.MaxConcurrentTasks,
                PooledConnectionLifetime = TimeSpan.FromMinutes(10)
            };
            var tokenHandler = new TokenRefreshHandler { InnerHandler = handler };
            _httpClient = new HttpClient(tokenHandler)
            {
                Timeout = Timeout.InfiniteTimeSpan
            };
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/140.0.0.0 Safari/537.36");
        }

        public async Task<DownloadResult> DownloadAsync(
            Uri uri,
            string savePath,
            long expectedSize,
            long legacyDownloadedSize,
            IReadOnlyCollection<DownloadSegmentEntity>? persistedSegments,
            Func<bool> shouldContinue,
            Func<DownloadProgress, Task> reportProgressAsync,
            CancellationToken cancellationToken = default)
        {
            if (expectedSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(expectedSize));
            }

            var supportsRanges = await SupportsRangesAsync(uri, cancellationToken);
            var segmentCount = Math.Clamp(_system.DownloadSegmentCount, 1, DownloadSettings.MaxSegmentCount);
            var segments = CreateSegments(expectedSize, legacyDownloadedSize, persistedSegments, supportsRanges, segmentCount);
            Directory.CreateDirectory(Path.GetDirectoryName(savePath)!);

            await using var output = new FileStream(
                savePath,
                FileMode.OpenOrCreate,
                FileAccess.Write,
                FileShare.Read,
                bufferSize: 1,
                FileOptions.Asynchronous | FileOptions.RandomAccess);
            output.SetLength(expectedSize);

            using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var progressTask = ReportProgressAsync(segments, expectedSize, shouldContinue, reportProgressAsync, linkedCancellation.Token);
            try
            {
                var downloads = segments
                    .Where(segment => segment.Downloaded < segment.End - segment.Start + 1)
                    .Select(segment => DownloadSegmentWithRetryAsync(uri, output.SafeFileHandle, segment, shouldContinue, linkedCancellation.Token));
                await Task.WhenAll(downloads);
            }
            finally
            {
                linkedCancellation.Cancel();
                try
                {
                    await progressTask;
                }
                catch (OperationCanceledException)
                {
                }
            }

            var downloaded = segments.Sum(segment => segment.Downloaded);
            await reportProgressAsync(new DownloadProgress(downloaded, expectedSize, 0, CloneSegments(segments)));
            return new DownloadResult(downloaded == expectedSize, downloaded, CloneSegments(segments));
        }

        private async Task<bool> SupportsRangesAsync(Uri uri, CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.Range = new RangeHeaderValue(0, 0);
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(RequestTimeout);
            await _connectionLimiter.WaitAsync(timeout.Token);
            try
            {
                using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
                if (response.StatusCode != HttpStatusCode.PartialContent)
                {
                    return false;
                }

                var contentRange = response.Content.Headers.ContentRange;
                return contentRange?.From is null || contentRange.From == 0;
            }
            finally
            {
                _connectionLimiter.Release();
            }
        }

        private static List<DownloadSegmentEntity> CreateSegments(
            long fileSize,
            long legacyDownloadedSize,
            IReadOnlyCollection<DownloadSegmentEntity>? persistedSegments,
            bool supportsRanges,
            int configuredSegmentCount)
        {
            if (supportsRanges && persistedSegments?.Count > 0 && persistedSegments.All(segment =>
                segment.Start >= 0 && segment.End >= segment.Start && segment.End < fileSize &&
                segment.Downloaded >= 0 && segment.Downloaded <= segment.End - segment.Start + 1))
            {
                return persistedSegments.Select(CloneSegment).OrderBy(segment => segment.Index).ToList();
            }

            var completedPrefix = supportsRanges ? Math.Clamp(legacyDownloadedSize, 0, fileSize) : 0;
            if (!supportsRanges || fileSize - completedPrefix < MinimumSegmentSize)
            {
                return new List<DownloadSegmentEntity>
                {
                    new() { Index = 0, Start = 0, End = fileSize - 1, Downloaded = completedPrefix }
                };
            }

            var remaining = fileSize - completedPrefix;
            var segmentCount = (int)Math.Min(configuredSegmentCount, Math.Max(1, remaining / MinimumSegmentSize));
            var result = new List<DownloadSegmentEntity>(segmentCount + (completedPrefix > 0 ? 1 : 0));
            if (completedPrefix > 0)
            {
                result.Add(new DownloadSegmentEntity { Index = 0, Start = 0, End = completedPrefix - 1, Downloaded = completedPrefix });
            }

            var segmentSize = remaining / segmentCount;
            var offset = completedPrefix;
            for (var index = 0; index < segmentCount; index++)
            {
                var end = index == segmentCount - 1 ? fileSize - 1 : offset + segmentSize - 1;
                result.Add(new DownloadSegmentEntity { Index = result.Count, Start = offset, End = end });
                offset = end + 1;
            }
            return result;
        }

        private async Task DownloadSegmentWithRetryAsync(
            Uri uri,
            SafeFileHandle fileHandle,
            DownloadSegmentEntity segment,
            Func<bool> shouldContinue,
            CancellationToken cancellationToken)
        {
            for (var attempt = 0; ; attempt++)
            {
                if (!shouldContinue())
                {
                    return;
                }

                try
                {
                    await DownloadSegmentAsync(uri, fileHandle, segment, shouldContinue, cancellationToken);
                    return;
                }
                catch (Exception ex) when (IsRetryable(ex) && attempt < MaxRetryCount && shouldContinue())
                {
                    var jitterMilliseconds = segment.Index * 150;
                    await Task.Delay(
                        TimeSpan.FromMilliseconds(1000 * Math.Pow(2, attempt) + jitterMilliseconds),
                        cancellationToken);
                }
            }
        }

        private async Task DownloadSegmentAsync(
            Uri uri,
            SafeFileHandle fileHandle,
            DownloadSegmentEntity segment,
            Func<bool> shouldContinue,
            CancellationToken cancellationToken)
        {
            var requestStart = segment.Start + segment.Downloaded;
            if (requestStart > segment.End)
            {
                return;
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.Range = new RangeHeaderValue(requestStart, segment.End);
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(RequestTimeout);
            await _connectionLimiter.WaitAsync(timeout.Token);
            try
            {
                using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
                var contentRange = response.Content.Headers.ContentRange;
                var isRequestedRange = response.StatusCode == HttpStatusCode.PartialContent &&
                    (contentRange?.From is null || contentRange.From == requestStart);
                var isCompleteResponse = response.StatusCode == HttpStatusCode.OK && requestStart == 0 && segment.Start == 0;
                if (!isRequestedRange && !isCompleteResponse)
                {
                    var rawContentRange = response.Content.Headers.TryGetValues("Content-Range", out var values)
                        ? string.Join(",", values)
                        : "<缺失>";
                    throw new HttpRequestException(
                        $"服务器未返回请求的字节范围：请求 bytes={requestStart}-{segment.End}，" +
                        $"HTTP {(int)response.StatusCode}，Content-Range={rawContentRange}。",
                        null,
                        response.StatusCode);
                }

                await using var input = await response.Content.ReadAsStreamAsync(timeout.Token);
                var buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
                try
                {
                    while (shouldContinue())
                    {
                        var downloaded = segment.Downloaded;
                        var remaining = segment.End - segment.Start + 1 - downloaded;
                        if (remaining <= 0)
                        {
                            return;
                        }

                        var bytesRead = await input.ReadAsync(buffer.AsMemory(0, (int)Math.Min(buffer.Length, remaining)), timeout.Token);
                        if (bytesRead == 0)
                        {
                            throw new EndOfStreamException("下载响应在分片完成前结束。");
                        }

                        await ThrottleAsync(bytesRead, timeout.Token);
                        await RandomAccess.WriteAsync(fileHandle, buffer.AsMemory(0, bytesRead), segment.Start + downloaded, timeout.Token);
                        segment.Downloaded += bytesRead;
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }
            finally
            {
                _connectionLimiter.Release();
            }
        }

        private async Task ThrottleAsync(int bytes, CancellationToken cancellationToken)
        {
            var bytesPerSecond = (long)Math.Max(0, _system.DownloadSpeedLimitKbps) * 1024;
            if (bytesPerSecond == 0)
            {
                lock (_throttleLock)
                {
                    _nextWriteTimestamp = 0;
                }
                return;
            }

            long delayTicks;
            lock (_throttleLock)
            {
                var now = System.Diagnostics.Stopwatch.GetTimestamp();
                if (_throttleBytesPerSecond != bytesPerSecond)
                {
                    _throttleBytesPerSecond = bytesPerSecond;
                    _nextWriteTimestamp = now;
                }
                var start = Math.Max(now, _nextWriteTimestamp);
                delayTicks = start - now;
                var durationTicks = (long)Math.Ceiling(
                    bytes * (double)System.Diagnostics.Stopwatch.Frequency / bytesPerSecond);
                _nextWriteTimestamp = start + durationTicks;
            }

            if (delayTicks > 0)
            {
                var delay = TimeSpan.FromSeconds(
                    delayTicks / (double)System.Diagnostics.Stopwatch.Frequency);
                await Task.Delay(delay, cancellationToken);
            }
        }

        private static async Task ReportProgressAsync(
            IReadOnlyCollection<DownloadSegmentEntity> segments,
            long totalBytes,
            Func<bool> shouldContinue,
            Func<DownloadProgress, Task> reportProgressAsync,
            CancellationToken cancellationToken)
        {
            var previousBytes = segments.Sum(segment => segment.Downloaded);
            var previousTime = DateTime.UtcNow;
            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(500));
            while (shouldContinue() && await timer.WaitForNextTickAsync(cancellationToken))
            {
                var currentBytes = segments.Sum(segment => segment.Downloaded);
                var now = DateTime.UtcNow;
                var seconds = Math.Max(0.001, (now - previousTime).TotalSeconds);
                var speed = (long)Math.Max(0, (currentBytes - previousBytes) / seconds);
                await reportProgressAsync(new DownloadProgress(currentBytes, totalBytes, speed, CloneSegments(segments)));
                previousBytes = currentBytes;
                previousTime = now;
            }
        }

        private static bool IsRetryable(Exception exception) => exception is HttpRequestException or IOException or TaskCanceledException;

        private static List<DownloadSegmentEntity> CloneSegments(IEnumerable<DownloadSegmentEntity> segments) =>
            segments.Select(CloneSegment).ToList();

        private static DownloadSegmentEntity CloneSegment(DownloadSegmentEntity segment) => new()
        {
            Index = segment.Index,
            Start = segment.Start,
            End = segment.End,
            Downloaded = segment.Downloaded
        };
    }

    public sealed record DownloadProgress(long DownloadedBytes, long TotalBytes, long BytesPerSecond, List<DownloadSegmentEntity> Segments);
    public sealed record DownloadResult(bool IsCompleted, long DownloadedBytes, List<DownloadSegmentEntity> Segments);
}
