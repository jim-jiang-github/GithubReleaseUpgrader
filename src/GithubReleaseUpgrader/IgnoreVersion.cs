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
    internal class IgnoreVersion : IDisposable
    {
        public static IgnoreVersion? LoadFromJson(string rootDir)
        {
            string filePath = Path.Combine(rootDir, FILE_NAME);
            if (!File.Exists(filePath))
            {
                return null;
            }
            try
            {
                var json = File.ReadAllText(filePath);
                var ignoreVersion = JsonSerializer.Deserialize<IgnoreVersion>(json);
                ignoreVersion._rootDir = rootDir;
                return ignoreVersion;
            }
            catch (Exception ex)
            {
                Log.Error("IgnoreVersion load from json error hanppened:{ex} raw json:{json}", ex, filePath);
                return null;
            }
        }

        private const string FILE_NAME = "ignoreVersion.json";
        private string _rootDir;

        public Version? Version { get; set; }

        public override string ToString()
        {
            var info = $"Version:{Version}";
            return info;
        }

        public void SaveAsJson(string rootDir)
        {
            _rootDir = rootDir;
            string json = JsonSerializer.Serialize(this, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            string filePath = Path.Combine(rootDir, FILE_NAME);
            File.WriteAllText(filePath, json);
        }

        public void Dispose()
        {
            string filePath = Path.Combine(_rootDir, FILE_NAME);
            FileOperationsHelper.SafeDeleteFile(filePath);
        }
    }
}
