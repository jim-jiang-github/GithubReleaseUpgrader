using Serilog;
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
        public enum UpgradeOption
        {
            Cancel,
            RemindAfter30Minutes,
            IgnoreCurrentVersion,
            ConfirmDownload
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
        public abstract UpgradeOption Notify(Version currentVersion, Version newtVersion, string? releaseLogMarkDown);
        public abstract void Force(Version currentVersion, Version newtVersion, string? releaseLogMarkDown);

        internal void TipInternal(Version currentVersion, Version newtVersion, string? releaseLogMarkDown)
        {
            Tip(currentVersion, newtVersion, releaseLogMarkDown);
        }
        internal async Task<ReadyToUpgrade?> NotifyInternal(Version currentVersion, Version newtVersion, string? releaseLogMarkDown)
        {
            var upgradeOption = Notify(currentVersion, newtVersion, releaseLogMarkDown);
            switch (upgradeOption)
            {
                case UpgradeOption.Cancel:
                default:
                    return null;
                case UpgradeOption.RemindAfter30Minutes:
                    return null;
                case UpgradeOption.IgnoreCurrentVersion:
                    return null;
                case UpgradeOption.ConfirmDownload:
                    var readyToUpgrade = await PrepareForUpgrade();
                    return readyToUpgrade;
            }
        }
        internal async Task<ReadyToUpgrade?> ForceInternal(Version currentVersion, Version newtVersion, string? releaseLogMarkDown)
        {
            Force(currentVersion, newtVersion, releaseLogMarkDown);
            var readyToUpgrade = await PrepareForUpgrade();
            return readyToUpgrade;
        }
        internal async Task<ReadyToUpgrade?> SilentInternal(Version currentVersion, Version newtVersion)
        {
            var readyToUpgrade = await PrepareForUpgrade(false);
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

        private async Task<ReadyToUpgrade?> PrepareForUpgrade(bool needRestart = true)
        {
            var scriptContent = GetUpgradeScriptContent();
            if (scriptContent == null)
            {
                return null;
            }
            FileOperationsHelper.SafeClearDirectory(UpgradeResourceFolder);
            var downloadFileSavePath = Path.Combine(UpgradeResourceFolder, UpgradeResourceName);
            await Downloader.StartDonwload(UpgradeResourceUrl, downloadFileSavePath, true);
            ReadyToUpgrade readyToUpgrade = new ReadyToUpgrade()
            {
                UpgradeScriptPath = UpgradeScriptPath,
                OriginalFolder = UpgradeResourceFolder,
                TargetFolder = ExecutableFolder,
                NeedRestart = needRestart
            };
            FileOperationsHelper.SafeCreateFile(UpgradeScriptPath, scriptContent);
            return readyToUpgrade;
        }

        public override string ToString()
        {
            var info = $"Start UpgradeResourceFolder:{UpgradeResourceFolder} githubLastReleaseUrl:{GithubLastReleaseUrl} upgradeResourceUrl:{UpgradeResourceUrl} upgradeInfoUrl:{UpgradeInfoUrl}";
            return info;
        }
    }
}
