﻿using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace GithubReleaseUpgrader
{
    public abstract class UpgradeProgress
    {
        public class ForceUpgradeHandle
        {
            public bool NeedRestart { get; set; } = true;
            public bool UpgradeNow { get; set; } = true;
        }
        public class NotifyUpgradeHandle
        {
            public bool Donwload { get; set; } = false;
            public bool Ignore { get; set; } = false;
            public bool Cancel { get; set; } = true;
        }

        private const string UPGRADE_SCRIPT_NAME = "upgrade.bat";
        private string UpgradeScriptPath => Path.Combine(UpgradeTempFolder, UPGRADE_SCRIPT_NAME);
        private string UpgradeResourceFolder => Path.Combine(UpgradeTempFolder, "upgrades");
        internal string GithubLastReleaseUrl => $"{GithubUrl}/releases/latest";
        internal string UpgradeResourceUrl => $"{GithubUrl}/releases/latest/download/{UpgradeResourceName}";
        internal string UpgradeInfoUrl => $"{GithubUrl}/releases/latest/download/{UpgradeInfoName}";
        internal string ExecutableFolder { get; } = AppDomain.CurrentDomain.BaseDirectory;
        internal string ExecutablePath => Path.Combine(ExecutableFolder, ExecutableName);

        public abstract string UpgradeTempFolder { get; }
        public abstract string GithubUrl { get; }
        public abstract string UpgradeResourceName { get; }
        public abstract string UpgradeInfoName { get; }
        public abstract string ExecutableName { get; }

        public abstract void Tip(Version currentVersion, Version newtVersion, string? releaseLogMarkDown);
        public abstract Task Notify(Version currentVersion, Version newtVersion, string? releaseLogMarkDown, NotifyUpgradeHandle notifyUpgradeHandle);
        public abstract Task Force(Version currentVersion, Version newtVersion, string? releaseLogMarkDown, ForceUpgradeHandle forceUpgradeHandle);
        public abstract void Shutdown();

        internal void TipInternal(Version currentVersion, Version newtVersion, string? releaseLogMarkDown)
        {
            Tip(currentVersion, newtVersion, releaseLogMarkDown);
        }
        internal async Task<ReadyToUpgrade?> NotifyInternal(Version currentVersion, Version newtVersion, string? releaseLogMarkDown)
        {
            NotifyUpgradeHandle notifyUpgradeHandle = new NotifyUpgradeHandle();
            await Notify(currentVersion, newtVersion, releaseLogMarkDown, notifyUpgradeHandle);
            if (notifyUpgradeHandle.Cancel)
            {
                return null;
            }
            if (notifyUpgradeHandle.Ignore)
            {
                var readyToUpgrade = PrepareForUpgrade(false, false);
                SaveIgnoreVersion(newtVersion);
                return readyToUpgrade;
            }
            else if (notifyUpgradeHandle.Donwload)
            {
                var readyToUpgrade = await ForceInternal(currentVersion, newtVersion, releaseLogMarkDown);
                return readyToUpgrade;
            }
            else
            {
                var result = await PrepareForDownload();
                if (!result)
                {
                    return null;
                }
                var readyToUpgrade = PrepareForUpgrade(false, false);
                return readyToUpgrade;
            }
        }
        internal async Task<ReadyToUpgrade?> ForceInternal(Version currentVersion, Version newtVersion, string? releaseLogMarkDown)
        {
            var result = await PrepareForDownload();
            if (!result)
            {
                return null;
            }
            ForceUpgradeHandle forceUpgradeHandle = new ForceUpgradeHandle();
            await Force(currentVersion, newtVersion, releaseLogMarkDown, forceUpgradeHandle);
            var readyToUpgrade = PrepareForUpgrade(forceUpgradeHandle.NeedRestart, forceUpgradeHandle.UpgradeNow);
            return readyToUpgrade;
        }
        internal async Task<ReadyToUpgrade?> SilentInternal(Version currentVersion, Version newtVersion)
        {
            var result = await PrepareForDownload();
            if (!result)
            {
                return null;
            }
            var readyToUpgrade = PrepareForUpgrade(false, false);
            return readyToUpgrade;
        }

        internal ReadyToUpgrade? GetLastReadyToUpgrade()
        {
            if (!File.Exists(UpgradeScriptPath))
            {
                Log.Warning("UpgradeScriptPath no exist");
                return null;
            }
            if (!Directory.Exists(UpgradeResourceFolder))
            {
                Log.Warning("UpgradeResourceFolder no exist");
                return null;
            }
            ReadyToUpgrade readyToUpgrade = new ReadyToUpgrade()
            {
                UpgradeScriptPath = UpgradeScriptPath,
                OriginalFolder = UpgradeResourceFolder,
                TargetFolder = ExecutableFolder
            };
            return readyToUpgrade;
        }

        private string? GetUpgradeScriptContent()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            using Stream? stream = assembly.GetManifestResourceStream($"GithubReleaseUpgrader.upgrader.bat");
            if (stream == null)
            {
                Log.Warning("Can not found upgrader");
                return null;
            }
            using StreamReader reader = new StreamReader(stream);
            string content = reader.ReadToEnd();
            return content;
        }

        private async Task<bool> PrepareForDownload()
        {
            var scriptContent = GetUpgradeScriptContent();
            if (scriptContent == null)
            {
                return false;
            }
            FileOperationsHelper.SafeClearDirectory(UpgradeResourceFolder);
            var downloadFileSavePath = Path.Combine(UpgradeResourceFolder, UpgradeResourceName);
            var result = await Downloader.StartDonwload(UpgradeResourceUrl, downloadFileSavePath, true);
            FileOperationsHelper.SafeCreateFile(UpgradeScriptPath, scriptContent);
            return result;
        }

        private ReadyToUpgrade PrepareForUpgrade(bool needRestart = true, bool needShutdown = true)
        {
            ReadyToUpgrade readyToUpgrade = new ReadyToUpgrade()
            {
                UpgradeScriptPath = UpgradeScriptPath,
                OriginalFolder = UpgradeResourceFolder,
                TargetFolder = ExecutableFolder,
                NeedRestart = needRestart,
                NeedShutdown = needShutdown
            };
            return readyToUpgrade;
        }

        internal IgnoreVersion? GetIgnoreVersion()
        {
            var readyToUpgrade = IgnoreVersion.LoadFromJson(UpgradeTempFolder);
            return readyToUpgrade;
        }

        private void SaveIgnoreVersion(Version version)
        {
            IgnoreVersion ignoreVersion = new IgnoreVersion()
            {
                Version = version
            };
            FileOperationsHelper.SafeCreateDirectory(UpgradeTempFolder);
            ignoreVersion.SaveAsJson(UpgradeTempFolder);
        }

        public override string ToString()
        {
            var info = $"Start UpgradeResourceFolder:{UpgradeResourceFolder} githubLastReleaseUrl:{GithubLastReleaseUrl} upgradeResourceUrl:{UpgradeResourceUrl} upgradeInfoUrl:{UpgradeInfoUrl}";
            return info;
        }
    }
}
