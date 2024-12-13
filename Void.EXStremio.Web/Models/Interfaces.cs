using System.Net.Http.Headers;
using Void.EXStremio.Web.Models.Kinopoisk;
using Void.EXStremio.Web.Utility;

namespace Void.EXStremio.Web.Models {
    interface IMetadataProvider {
        Task<ExtendedMeta?> GetMetadataAsync(string type, string id);
    }

    interface IAdditionalMetadataProvider {
        bool CanGetAdditionalMetadata(string id);
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

    interface ICustomIdProvider {
        bool CanGetCustomId(ExtendedMeta meta);
        Task<CustomIdResult[]> GetCustomIds(ExtendedMeta meta);
    }

    interface IMediaProvider {
        string ServiceName { get; }

        bool CanGetStreams(string id);
        bool CanGetMedia(MediaLink link);
        Task<MediaStream[]> GetStreams(string id, int? season = null, int? episode = null, ExtendedMeta meta = null);
        Task<IMediaSource> GetMedia(MediaLink link, RangeHeaderValue range = null);
    }

    interface IInitializableProvider {
        bool IsInitialized { get; }
        bool IsReady { get; }
        Task Initialize();
    }
}
