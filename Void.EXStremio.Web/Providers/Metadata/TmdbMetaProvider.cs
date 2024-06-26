﻿using Void.EXStremio.Web.Models;
using Void.EXStremio.Web.Models.TMDB;

namespace Void.EXStremio.Web.Providers.Metadata {
    public class TmdbConfig {
        public const string CONFIG_API_KEY = "TMDB_API_KEY";

        public string ApiKey { get; }

        public TmdbConfig(string apiKey) {
            ApiKey = apiKey;
        }
    }

    public class TmdbMetaProvider : IAdditionalMetadataProvider {
        const string PREFIX = "tmdb";
        const string baseUri = "https://api.themoviedb.org/3/TYPE/ID?append_to_response=external_ids,alternative_titles,translations&api_key=API_KEY";

        readonly IHttpClientFactory httpClientFactory;
        readonly TmdbConfig config;

        public TmdbMetaProvider(IHttpClientFactory httpClientFactory, TmdbConfig config) {
            this.httpClientFactory = httpClientFactory;
            this.config = config;
        }

        public bool CanGetAdditionalMetadata(string id) {
            return id.StartsWith(PREFIX);
        }

        public async Task<ExtendedMeta?> GetAdditionalMetadataAsync(string type, string id) {
            if (!id.StartsWith(PREFIX)) { throw new InvalidOperationException($"Identifier {id} is not supported by TMDB metadata provider"); }

            var apiType = string.Empty;
            if (type == "movie") {
                apiType = "movie";
            } else if (type == "series") {
                apiType = "tv";
            } else {
                throw new InvalidOperationException($"Type {type} is not supported by TMDB metadata provider");
            }

            var uriString = baseUri
                .Replace("ID", id.Replace(PREFIX, ""))
                .Replace("TYPE", apiType)
                .Replace("API_KEY", config.ApiKey);
            var uri = new Uri(uriString);
            var client = httpClientFactory.CreateClient();

            if (type == "movie") {
                var response = await client.GetFromJsonAsync<MovieTmdbResponse>(uri);

                var meta = new ExtendedMeta();
                meta.Name = response.Title;
                // TODO: FIX?
                //meta.OriginalName = response.OriginalTitle;
                meta.Year = response.ReleaseDate.Year.ToString();
                meta.Type = "movie";
                meta.TmdbId = response.Id;
                meta.ImdbId = response.ExternalIds.ImdbId;
                var altTitles = response.AlternativeTitles
                    ?.Titles
                    ?.Select(x => x.Title)?.ToArray() 
                    ?? [];
                meta.AlternativeTitles.AddRange(altTitles);
                var localizedTitles = response.Translations?
                    .Translations
                    ?.Where(x => !string.IsNullOrWhiteSpace(x.Data.Title))
                    ?.Select(x => new LocalizedTitle(x.CountryCode, x.Data.Title))
                    ?.ToArray() 
                    ?? [];
                meta.LocalizedTitles.AddRange(localizedTitles);

                return meta;
            } else if (type == "series") {
                var response = await client.GetFromJsonAsync<TvTmdbResponse>(uri);

                var meta = new ExtendedMeta();
                meta.Name = response.Title;
                // TODO: FIX?
                //meta.OriginalName = response.OriginalTitle;
                meta.Year = response.StartDate.Value.Year.ToString();
                meta.StartYear = response.StartDate.HasValue ? response.StartDate.Value.Year : null;
                meta.EndYear = response.EndDate.HasValue ? response.EndDate.Value.Year : null;
                meta.Type = "series";
                meta.TmdbId = response.Id;
                meta.ImdbId = response.ExternalIds.ImdbId;
                var altTitles = response.AlternativeTitles
                    ?.Results
                    ?.Select(x => x.Title)
                    ?.ToArray()
                    ?? [];
                meta.AlternativeTitles.AddRange(altTitles);
                var localizedTitles = response.Translations?
                    .Translations
                    ?.Where(x => !string.IsNullOrWhiteSpace(x.Data.Name))
                    ?.Select(x => new LocalizedTitle(x.CountryCode, x.Data.Name))
                    ?.ToArray()
                    ?? [];
                meta.LocalizedTitles.AddRange(localizedTitles);

                return meta;
            }

            return null;
        }
    }
}
