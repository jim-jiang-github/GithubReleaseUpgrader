using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace GithubReleaseUpgrader
{
    internal class ReadyToUpgrade
    {
        public string? UpgradeTempFolder { get; set; }
        public string? UpgradeScriptPath { get; set; }
        public string? OriginalFolder { get; set; }
        public string? TargetFolder { get; set; }
        public bool NeedRestart { get; set; } = true;

        public override string ToString()
        {
            var info = $"UpgradeTempFolder:{UpgradeTempFolder} UpgradeScriptPath:{UpgradeScriptPath} OriginalFolder:{OriginalFolder} TargetFolder:{TargetFolder} NeedRestart:{NeedRestart}";
            return info;
        }
    }
}
