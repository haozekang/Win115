using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Win115.Helpers;

namespace Win115.Services
{
    public partial class UpdateService : ObservableObject
    {
        private const string VersionUrl = "https://raw.githubusercontent.com/haozekang/Win115/main/.version";
        private static readonly HttpClient HttpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        private readonly SemaphoreSlim _checkLock = new(1, 1);

        [ObservableProperty]
        public partial bool IsUpdateAvailable { get; private set; }

        [ObservableProperty]
        public partial Version? LatestVersion { get; private set; }

        public Version CurrentVersion { get; } =
            Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(0, 0, 0, 0);

        public async Task<bool> CheckForUpdatesAsync()
        {
            await _checkLock.WaitAsync();

            try
            {
                var versionText = (await HttpClient.GetStringAsync(VersionUrl)).Trim();
                if (!Version.TryParse(versionText, out var latestVersion) ||
                    latestVersion.Revision < 0)
                {
                    await LogHelper.Error($"GitHub .version 内容无效：{versionText}");
                    return false;
                }

                LatestVersion = latestVersion;
                IsUpdateAvailable = latestVersion > CurrentVersion;
                return true;
            }
            catch (Exception ex)
            {
                await LogHelper.Trace(ex);
                return false;
            }
            finally
            {
                _checkLock.Release();
            }
        }
    }
}
