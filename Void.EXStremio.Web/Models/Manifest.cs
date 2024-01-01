namespace Void.EXStremio.Web.Models {
    public class Manifest {
        public string Id { get; set; }
        public string Version { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string[] Types { get; set; }
        public Catalog[] Catalogs { get; set; }
        public object[] Resources { get; set; }
        public string[] IdPrefixes { get; set; }
    }

    public class Catalog {
        public string Type { get; set; }
        public string Id { get; set; }
        public string Name { get; set; }
        public ExtraParam[] Extra { get; set; }
    }

    public class ExtraParam {
        public string Name { get; set; }
        public bool IsRequired { get; set; }
    }
}
