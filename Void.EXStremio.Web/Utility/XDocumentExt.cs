using System.Text;
using System.Xml.Linq;

namespace Void.EXStremio.Web.Utility {
    public static class XDocumentExt {
        public static XDocument Load(string xml) {
            var bytes = Encoding.UTF8.GetBytes(xml);
            using (var ms = new MemoryStream(bytes)) {
                return XDocument.Load(ms);
            }
        }

        public static string Save(XDocument document) {
            using (var ms = new MemoryStream()) {
                document.Save(ms);
                var bytes = ms.ToArray();

                return Encoding.UTF8.GetString(bytes);
            }
        }
    }
}
