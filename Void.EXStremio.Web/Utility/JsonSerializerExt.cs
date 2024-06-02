using System.Text.Json;
using System.Text;

namespace Void.EXStremio.Web.Utility {
    public static class JsonSerializerExt {
        public static ValueTask<T?> DeserializeAsync<T>(string json, JsonSerializerOptions? options = null, CancellationToken cancellationToken = default) {
            if (options == null) {
                options = new JsonSerializerOptions() {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
            }

            var data = Encoding.UTF8.GetBytes(json);
            using (var ms = new MemoryStream(data)) { 
                return JsonSerializer.DeserializeAsync<T>(ms, options);
            }
        }

        public static async Task<string> SerializeAsync<T>(T value, JsonSerializerOptions? options = null, CancellationToken cancellationToken = default) {
            if (options == null) {
                options = new JsonSerializerOptions() {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
            }

            using (var ms = new MemoryStream()) {
                await JsonSerializer.SerializeAsync(ms, value, options);

                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }
    }
}
