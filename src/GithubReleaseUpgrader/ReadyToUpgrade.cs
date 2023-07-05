using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GithubReleaseUpgrader
{
    internal class ReadyToUpgrade
    {
        private UpgradeHandler _upgradeHandler;

        public string? UpgradeScriptPath { get; set; }
        public string? OriginalFolder { get; set; }
        public string? TargetFolder { get; set; }
        public string? ExecutablePath { get; set; }
        public bool NeedRestart { get; set; } = true;
        public bool NeedShutdown { get; set; } = true;

        public ReadyToUpgrade(UpgradeHandler upgradeHandler)
        {
            _upgradeHandler = upgradeHandler;
        }

        public override string ToString()
        {
            var info = $"UpgradeScriptPath:{UpgradeScriptPath} OriginalFolder:{OriginalFolder} TargetFolder:{TargetFolder} ExecutablePath:{ExecutablePath} NeedRestart:{NeedRestart} NeedShutdown:{NeedShutdown}";
            return info;
        }

        public void DoUpgrade()
        {
            Log.Information("PerformUpgradeIfNeeded readyToUpgrade:{readyToUpgrade} NeedRestart:{NeedRestart}", this, NeedRestart);
            if (!NeedRestart)
            {
                return;
            }
            if (UpgradeScriptPath == null)
            {
                Log.Warning("_readyToUpgrade.UpgradeScriptPath == null");
                return;
            }
            if (OriginalFolder == null)
            {
                Log.Warning("_readyToUpgrade.OriginalFolder == null");
                return;
            }
            if (TargetFolder == null)
            {
                Log.Warning("_readyToUpgrade.TargetFolder == null");
                return;
            }
            if (ExecutablePath == null)
            {
                Log.Warning("executablePath == null");
                return;
            }
            _upgradeHandler.DoUpgrade(UpgradeScriptPath, OriginalFolder, TargetFolder, ExecutablePath, NeedRestart);
        }
    }
}
