using Serilog;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GithubReleaseUpgrader
{
    internal class UpgradeInfo
    {
        public static UpgradeInfo? LoadFromJson(string json)
        {
            try
            {
                return JsonSerializer.Deserialize<UpgradeInfo>(json);
            }
            catch (Exception ex)
            {
                Log.Error("UpgradeInfo load from json error hanppened:{ex} raw json:{json}", ex, json);
                return null;
            }
        }

        public Version? TipVersion { get; set; }
        public Version? NotifyVersion { get; set; }
        public Version? ForceVersion { get; set; }
        public Version? SilentVersion { get; set; }

        public void SaveAsJson()
        {
            string json = JsonSerializer.Serialize(this, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "upgradeInfo.json");
            File.WriteAllText(filePath, json);
        }

        public override string ToString()
        {
            var info = $"TipVersion:{TipVersion} NotifyVersion:{NotifyVersion} ForceVersion:{ForceVersion} SilentVersion:{SilentVersion}";
            return info;
        }
    }
}