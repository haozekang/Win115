using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Win115.Helpers;
using Win115.Models;
using Win115.Services;

namespace Win115.Handlers
{
    public sealed class ApiRateLimitHandler : DelegatingHandler
    {
        private readonly object _scheduleLock = new();
        private DateTimeOffset _nextRequestAt = DateTimeOffset.MinValue;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var bufferedRequest = await BufferedRequest.CreateAsync(request, cancellationToken);

            for (var retryCount = 0; ; retryCount++)
            {
                await WaitForRequestSlotAsync(cancellationToken);
                using var attemptRequest = bufferedRequest.CreateMessage();
                var response = await base.SendAsync(attemptRequest, cancellationToken);

                if (!await IsRateLimitedAsync(response, cancellationToken)
                    || retryCount >= ApiSettings.MaxRateLimitRetries)
                {
                    return response;
                }

                response.Dispose();
                var delay = GetBackoffDelay(retryCount);
                await LogHelper.Trace(
                    $"API rate limit detected. Retrying {request.RequestUri} in {delay.TotalSeconds:F1}s " +
                    $"(attempt {retryCount + 1}/{ApiSettings.MaxRateLimitRetries}).");
                await Task.Delay(delay, cancellationToken);
            }
        }

        private async Task WaitForRequestSlotAsync(CancellationToken cancellationToken)
        {
            var rateLimit = Math.Clamp(
                App.Resolve<SystemInfoModel>().ApiRateLimit,
                0,
                ApiSettings.MaxRateLimit);
            if (rateLimit == 0)
            {
                return;
            }

            TimeSpan delay;
            lock (_scheduleLock)
            {
                var now = DateTimeOffset.UtcNow;
                var scheduledAt = _nextRequestAt > now ? _nextRequestAt : now;
                delay = scheduledAt - now;
                _nextRequestAt = scheduledAt.AddSeconds(1d / rateLimit);
            }

            if (delay > TimeSpan.Zero)
            {
                await Task.Delay(delay, cancellationToken);
            }
        }

        private static async Task<bool> IsRateLimitedAsync(
            HttpResponseMessage response,
            CancellationToken cancellationToken)
        {
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                return true;
            }

            if (response.Content is null)
            {
                return false;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            if (content.Contains("rate limit", StringComparison.OrdinalIgnoreCase)
                || content.Contains("too many", StringComparison.OrdinalIgnoreCase)
                || content.Contains("\u9891\u7e41", StringComparison.Ordinal)
                || content.Contains("\u9650\u6d41", StringComparison.Ordinal)
                || content.Contains("\u8bf7\u6c42\u8fc7\u5feb", StringComparison.Ordinal))
            {
                return true;
            }

            try
            {
                using var document = JsonDocument.Parse(content);
                return HasRateLimitCode(document.RootElement);
            }
            catch (JsonException)
            {
                return false;
            }
        }

        private static bool HasRateLimitCode(JsonElement element)
        {
            if (element.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            foreach (var propertyName in new[] { "code", "errno" })
            {
                if (element.TryGetProperty(propertyName, out var value)
                    && ((value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number) && number == 20130827)
                        || (value.ValueKind == JsonValueKind.String && value.GetString() == "20130827")))
                {
                    return true;
                }
            }

            return false;
        }

        private static TimeSpan GetBackoffDelay(int retryCount)
        {
            var exponentialDelay = Math.Min(
                ApiSettings.BackoffBaseMilliseconds * Math.Pow(2, retryCount),
                ApiSettings.BackoffMaxMilliseconds);
            var jitter = 0.75 + (Random.Shared.NextDouble() * 0.5);
            return TimeSpan.FromMilliseconds(exponentialDelay * jitter);
        }

        private sealed class BufferedRequest
        {
            private readonly HttpMethod _method;
            private readonly Uri? _requestUri;
            private readonly Version _version;
            private readonly HttpVersionPolicy _versionPolicy;
            private readonly KeyValuePair<string, IEnumerable<string>>[] _headers;
            private readonly KeyValuePair<string, IEnumerable<string>>[] _contentHeaders;
            private readonly byte[]? _content;

            private BufferedRequest(
                HttpMethod method,
                Uri? requestUri,
                Version version,
                HttpVersionPolicy versionPolicy,
                KeyValuePair<string, IEnumerable<string>>[] headers,
                KeyValuePair<string, IEnumerable<string>>[] contentHeaders,
                byte[]? content)
            {
                _method = method;
                _requestUri = requestUri;
                _version = version;
                _versionPolicy = versionPolicy;
                _headers = headers;
                _contentHeaders = contentHeaders;
                _content = content;
            }

            public static async Task<BufferedRequest> CreateAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                var content = request.Content is null
                    ? null
                    : await request.Content.ReadAsByteArrayAsync(cancellationToken);
                return new BufferedRequest(
                    request.Method,
                    request.RequestUri,
                    request.Version,
                    request.VersionPolicy,
                    request.Headers.Select(header => new KeyValuePair<string, IEnumerable<string>>(header.Key, header.Value)).ToArray(),
                    request.Content?.Headers.Select(header => new KeyValuePair<string, IEnumerable<string>>(header.Key, header.Value)).ToArray()
                        ?? Array.Empty<KeyValuePair<string, IEnumerable<string>>>(),
                    content);
            }

            public HttpRequestMessage CreateMessage()
            {
                var message = new HttpRequestMessage(_method, _requestUri)
                {
                    Version = _version,
                    VersionPolicy = _versionPolicy,
                };
                foreach (var header in _headers)
                {
                    message.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }

                if (_content is null)
                {
                    return message;
                }

                message.Content = new ByteArrayContent(_content);
                foreach (var header in _contentHeaders)
                {
                    message.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }

                return message;
            }
        }
    }
}
