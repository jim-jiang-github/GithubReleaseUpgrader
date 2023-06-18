using HtmlAgilityPack;
using ReverseMarkdown;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GithubReleaseUpgrader
{
    public class Upgrader
    {
        private static ReadyToUpgrade? _readyToUpgrade;
        private static IgnoreVersion? _ignoreVersion;

        public static bool CheckForUpgrade(UpgradeHandler upgradeHandler, bool forceCheck = false)
        {
            Log.Information("Start upgradeHandler:{upgradeHandler}", upgradeHandler);
            _readyToUpgrade = upgradeHandler.GetLastReadyToUpgrade();
            if (_readyToUpgrade != null)
            {
                Log.Information("Start _readyToUpgrade:{_readyToUpgrade} is not null", upgradeHandler);
                return true;
            }
            Task.Run(async () =>
            {
                try
                {
                    Version currentVersion = GetCurrentVersion(upgradeHandler.ExecutablePath);
                    Version? githubReleaseVersion = await GetLastReleaseVersion(upgradeHandler.GithubLastReleaseUrl);
                    if (githubReleaseVersion == null)
                    {
                        Log.Information("Can not get githubReleaseVersion");
                        return;
                    }
                    if (githubReleaseVersion <= currentVersion)
                    {
                        Log.Information("Current verison:{currentVersion} new version:{githubReleaseVersion} no need to upgrade", currentVersion, githubReleaseVersion);
                        return;
                    }
                    _ignoreVersion = upgradeHandler.GetIgnoreVersion();
                    if (!forceCheck && _ignoreVersion?.Version != null && githubReleaseVersion == _ignoreVersion.Version)
                    {
                        Log.Information("Current verison:{currentVersion} new version:{githubReleaseVersion} _ignoreVersion:{_ignoreVersion} ignore this version", currentVersion, githubReleaseVersion, _ignoreVersion);
                        return;
                    }
                    var releaseLogMarkDown = await GetReleaseLog(upgradeHandler.GithubLastReleaseUrl);
                    var upgradeInfo = await GetUpgradeInfo(upgradeHandler.UpgradeInfoUrl);
                    if (upgradeInfo == null)
                    {
                        _readyToUpgrade = await upgradeHandler.NotifyInternal(currentVersion, githubReleaseVersion, releaseLogMarkDown);
                        Log.Information("Notify ready to upgrade:{readyToUpgrade}", _readyToUpgrade);
                        if (_readyToUpgrade?.NeedShutdown == true)
                        {
                            upgradeHandler.Shutdown();
                        }
                        return;
                    }
                    if (upgradeInfo.SilentVersion != null && currentVersion < upgradeInfo.SilentVersion)
                    {
                        _readyToUpgrade = await upgradeHandler.SilentInternal(currentVersion, githubReleaseVersion);
                        Log.Information("Silent ready to upgrade:{readyToUpgrade}", _readyToUpgrade);
                        if (_readyToUpgrade?.NeedShutdown == true)
                        {
                            upgradeHandler.Shutdown();
                        }
                        return;
                    }
                    if (upgradeInfo.ForceVersion != null && currentVersion < upgradeInfo.ForceVersion)
                    {
                        _readyToUpgrade = await upgradeHandler.ForceInternal(currentVersion, githubReleaseVersion, releaseLogMarkDown);
                        Log.Information("Force ready to upgrade:{readyToUpgrade}", _readyToUpgrade);
                        if (_readyToUpgrade?.NeedShutdown == true)
                        {
                            upgradeHandler.Shutdown();
                        }
                        return;
                    }
                    if (upgradeInfo.NotifyVersion != null && currentVersion < upgradeInfo.NotifyVersion)
                    {
                        _readyToUpgrade = await upgradeHandler.NotifyInternal(currentVersion, githubReleaseVersion, releaseLogMarkDown);
                        Log.Information("Notify ready to upgrade:{readyToUpgrade}", _readyToUpgrade);
                        if (_readyToUpgrade?.NeedShutdown == true)
                        {
                            upgradeHandler.Shutdown();
                        }
                        return;
                    }
                    if (upgradeInfo.TipVersion != null && currentVersion < upgradeInfo.TipVersion)
                    {
                        upgradeHandler.TipInternal(currentVersion, githubReleaseVersion, releaseLogMarkDown);
                        Log.Information("Tip");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("Upgrade error hanppened:{ex}", ex);
                    return;
                }
            });
            return false;
        }

        public static void PerformUpgradeIfNeeded()
        {
            Log.Information("PerformUpgradeIfNeeded readyToUpgrade:{readyToUpgrade} NeedRestart:{NeedRestart}", _readyToUpgrade, _readyToUpgrade?.NeedRestart);
            if (_readyToUpgrade == null || !_readyToUpgrade.NeedRestart)
            {
                return;
            }
            if (_readyToUpgrade.OriginalFolder == null)
            {
                Log.Warning("_readyToUpgrade.OriginalFolder == null");
                return;
            }
            string? executablePath = Process.GetCurrentProcess()?.MainModule?.FileName;
            if (executablePath == null)
            {
                Log.Warning("executablePath == null");
                return;
            }
            var process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _readyToUpgrade.UpgradeScriptPath,
                    Arguments = $"{_readyToUpgrade.OriginalFolder} {_readyToUpgrade.TargetFolder} {executablePath} {_readyToUpgrade.NeedRestart}",
                    RedirectStandardOutput = false,
                    RedirectStandardError = false,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            try
            {
                process.Start();
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                FileOperationsHelper.SafeDeleteDirectory(_readyToUpgrade.OriginalFolder);
            }
            _ignoreVersion?.Dispose();
        }

        private static Version GetCurrentVersion(string executablePath)
        {
            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(executablePath);
            string versionStr = fileVersionInfo.FileVersion ?? "Unknow";
            var result = Version.TryParse(versionStr, out Version? version);
            var resultVersion = version == null ? Version.Parse("0.0.0.0") : new Version(version.Major, version.Minor, version.Build);
            Log.Information("Executable version FileVersion:{fileVersionInfo.FileVersion} version:{version} resultVersion:{resultVersion}", fileVersionInfo.FileVersion, version, resultVersion);
            return resultVersion;
        }

        private static async Task<UpgradeInfo?> GetUpgradeInfo(string upgradeInfoUrl)
        {
            using var httpClient = new HttpClient() { Timeout = TimeSpan.FromSeconds(10) };
            string upgradeInfoJson = await httpClient.GetStringAsync(upgradeInfoUrl);
            var upgradeInfo = UpgradeInfo.LoadFromJson(upgradeInfoJson);
            Log.Information("GetUpgradeInfo upgradeInfo:{upgradeInfo}", upgradeInfo);
            return upgradeInfo;
        }

        private static async Task<Version?> GetLastReleaseVersion(string githubLastReleaseUrl)
        {
            try
            {
                Log.Information("GetLastReleaseVersion githubLastReleaseUrl:{githubLastReleaseUrl}", githubLastReleaseUrl);
                using HttpClient httpClient = new() { Timeout = TimeSpan.FromSeconds(10) };
                var httpResponseMessage = await httpClient.GetAsync(githubLastReleaseUrl);
                httpResponseMessage.EnsureSuccessStatusCode();
                var requestUri = httpResponseMessage.RequestMessage?.RequestUri;
                Log.Information("GetLastReleaseVersion requestUri:{requestUri}", requestUri);
                if (requestUri == null)
                {
                    return null;
                }
                Log.Information("GetLastReleaseVersion OriginalString:{OriginalString}", requestUri.OriginalString);
                var match = Regex.Match(requestUri.OriginalString, "v(\\d+\\.?\\d+\\.?\\d+\\.?)");
                if (match.Success && match.Groups.Count == 2 && match.Groups[1].Value is string versionString && Version.TryParse(versionString, out Version? version))
                {
                    return version;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                Log.Error("GetLastReleaseVersion error hanppened:{ex}", ex);
                return null;
            }
        }

        private static async Task<string?> GetReleaseLog(string githubLastReleaseUrl)
        {
            try
            {
                Log.Information("GetReleaseLog githubLastReleaseUrl:{githubLastReleaseUrl}", githubLastReleaseUrl);
                using HttpClient httpClient = new() { Timeout = TimeSpan.FromSeconds(10) };
                var httpResponseMessage = await httpClient.GetAsync(githubLastReleaseUrl);
                httpResponseMessage.EnsureSuccessStatusCode();
                var requestUri = httpResponseMessage.RequestMessage?.RequestUri;
                var htmlWeb = new HtmlWeb();
                var doc = htmlWeb.Load(requestUri);
                var node = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'markdown-body')]");
                if (node == null)
                {
                    return null;
                }
                var html = node.InnerHtml;
                var converter = new Converter();
                var markdown = converter.Convert(html);
                return markdown;
            }
            catch (Exception ex)
            {
                Log.Error("GetLastReleaseVersion error hanppened:{ex}", ex);
                return null;
            }
        }
    }
}
