using System.Text;

namespace Void.EXStremio.Web.Utility {
    public static class Base64Ext {
        public static string Decode(string value) {
            var bytes = Convert.FromBase64String(value);

            return Encoding.UTF8.GetString(bytes);
        }
    }
}
