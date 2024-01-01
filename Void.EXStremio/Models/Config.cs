using System;
using System.IO;
using System.Text;
using System.Text.Json;

namespace Void.EXStremio.Models {
    public interface IConfig {
        string StremioExePath { get; }
        public bool StartStremio { get; }
        public bool CloseStremio { get; }
        public bool CloseWithStremio { get; }
    }

    internal class Config : IConfig {
        readonly static string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
        public string StremioExePath { get; set; }
        public bool StartStremio { get; set; }
        public bool CloseStremio { get; set; }
        public bool CloseWithStremio { get; set; }

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
    }
}
