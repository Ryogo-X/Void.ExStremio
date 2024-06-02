using Void.EXStremio.Web.Models.Kinopoisk;
using Void.EXStremio.Web.Utility;

namespace Void.EXStremio.Web.Models {
    interface IMetadataProvider {
        Task<ExtendedMeta?> GetMetadataAsync(string type, string id);
    }

    interface IAdditionalMetadataProvider {
        Task<ExtendedMeta?> GetAdditionalMetadataAsync(string type, string id);
    }

    interface ICatalogProvider {
        Task<CatalogResponse<KinopoiskMeta>> GetAsync(string type, string searchQuery);
    }

    interface IKinopoiskIdProvider {
        Task<string?> GetKinopoiskIdAsync(string imdbId);
    }

    interface IKinopoiskFallbackIdProvider {
        Task<string> GetKinopoiskIdAsync(ExtendedMeta meta);
    }

    interface IMediaProvider {
        string ServiceName { get; }

        bool CanHandle(string id);
        bool CanHandle(MediaLink link);
        Task<MediaStream[]> GetStreams(string id, int? season = null, int? episode = null);
        Task<IMediaSource> GetMedia(MediaLink link);
    }
}
