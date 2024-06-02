using System;
using System.IO;
using System.Text;
using System.Text.Json;
using Void.EXStremio.Web.Providers.Media.AllohaTv;
using Void.EXStremio.Web.Providers.Media.Collaps;
using Void.EXStremio.Web.Providers.Media.Hdvb;
using Void.EXStremio.Web.Providers.Media.Kodik;
using Void.EXStremio.Web.Providers.Media.VideoCdn;
using Void.EXStremio.Web.Providers.Metadata;

namespace Void.EXStremio.Models {
    public interface IConfig {
        string StremioExePath { get; }
        public bool StartStremio { get; }
        public bool CloseStremio { get; }
        public bool CloseWithStremio { get; }

        void InitializeEnvironmentVariables();
    }

    internal class Config : IConfig {
        readonly static string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
        public string StremioExePath { get; set; }
        public bool StartStremio { get; set; }
        public bool CloseStremio { get; set; }
        public bool CloseWithStremio { get; set; }

        public string TmdbApiKey { get; set; }
        public string KodikApiKey { get; set; }
        public string VideoCdnApiKey { get; set; }
        public string AllohaTvApiKey { get; set; }
        public string CollapApiKey { get; set; }
        public string HdvbApiKey { get; set; }

        public static Config Load() {
            if (!File.Exists(path)) { return null; }

            try {
                using (var stream = File.OpenRead(path)) {
                    return JsonSerializer.Deserialize<Config>(stream);
                }
            } catch {
                return null;
            }
        }

        public static void Save(Config config) {
            var json = JsonSerializer.Serialize(config);
            File.WriteAllBytes(path, Encoding.UTF8.GetBytes(json));
        }

        public void InitializeEnvironmentVariables() {
            Environment.SetEnvironmentVariable(TmdbConfig.CONFIG_API_KEY, TmdbApiKey);
            Environment.SetEnvironmentVariable(KodikConfig.CONFIG_API_KEY, KodikApiKey);
            Environment.SetEnvironmentVariable(VideoCdnConfig.CONFIG_API_KEY, VideoCdnApiKey);
            Environment.SetEnvironmentVariable(AllohaTvConfig.CONFIG_API_KEY, AllohaTvApiKey);
            Environment.SetEnvironmentVariable(CollapsConfig.CONFIG_API_KEY, CollapApiKey);
            Environment.SetEnvironmentVariable(HdvbConfig.CONFIG_API_KEY, HdvbApiKey);
        }
    }
}
