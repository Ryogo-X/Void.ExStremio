using System.Text.Json.Serialization;

namespace Void.EXStremio.Web.Models {
    public class MetaResponse<T> where T : Meta {
        public T Meta { get; set; }
    } 

    public class ExtendedMeta : Meta {
        [JsonIgnore]
        public List<string> AlternativeTitles { get; } = new List<string>();
        [JsonIgnore]
        public List<LocalizedTitle> LocalizedTitles { get; } = new List<LocalizedTitle>();

        public void Extend(ExtendedMeta meta) {
            foreach (var localizedTitle in meta.LocalizedTitles) {
                if (LocalizedTitles.Any(x => x.LangCode == localizedTitle.LangCode && x.Title == localizedTitle.Title)) { continue; }

                LocalizedTitles.Add(localizedTitle);
            }

            foreach (var altTitle in meta.AlternativeTitles) {
                if (AlternativeTitles.Contains(altTitle)) { continue; }

                AlternativeTitles.Add(altTitle);
            }
        }

        public int? GetYear() {
            var yearString = this.Year;
            if (string.IsNullOrWhiteSpace(yearString)) { return null; }
            
            var parts = yearString.Split(['-', '–'], StringSplitOptions.RemoveEmptyEntries);

            return int.Parse(parts[0]);
        }
    }

    public class LocalizedTitle {
        public string LangCode { get; }
        public string Title { get; }

        public LocalizedTitle(string langCode, string title) {
            LangCode = langCode; ;
            Title = title;
        }
    }

    public partial class Meta {
        [JsonPropertyName("awards")]
        public string Awards { get; set; }

        [JsonPropertyName("cast")]
        public string[] Cast { get; set; }

        [JsonPropertyName("country")]
        public string Country { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("director")]
        public string[] Director { get; set; }

        [JsonPropertyName("dvdRelease")]
        public DateTimeOffset? DvdRelease { get; set; }

        [JsonPropertyName("genre")]
        public string[] Genre { get; set; }

        [JsonPropertyName("imdbRating")]
        public string ImdbRating { get; set; }

        [JsonPropertyName("imdb_id")]
        public string ImdbId { get; set; }

        [JsonPropertyName("kpRating")]
        public string KpRating { get; set; }

        [JsonPropertyName("kp_id")]
        public string KpId { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("popularity")]
        public double Popularity { get; set; }

        [JsonPropertyName("poster")]
        public Uri Poster { get; set; }

        [JsonPropertyName("released")]
        public DateTimeOffset Released { get; set; }

        [JsonPropertyName("runtime")]
        public string Runtime { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("writer")]
        public string[] Writer { get; set; }

        [JsonPropertyName("year")]
        public string Year { get; set; }

        [JsonIgnore]
        public int? StartYear { get; set; }

        [JsonIgnore]
        public int? EndYear { get; set; }

        [JsonPropertyName("trailers")]
        public Trailer[] Trailers { get; set; }

        [JsonPropertyName("popularities")]
        public Popularities Popularities { get; set; }

        [JsonPropertyName("slug")]
        public string Slug { get; set; }

        [JsonPropertyName("background")]
        public Uri Background { get; set; }

        [JsonPropertyName("logo")]
        public Uri Logo { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("genres")]
        public string[] Genres { get; set; }

        [JsonPropertyName("releaseInfo")]
        public string ReleaseInfo { get; set; }

        [JsonPropertyName("trailerStreams")]
        public TrailerStream[] TrailerStreams { get; set; }

        [JsonPropertyName("links")]
        public Link[] Links { get; set; }

        [JsonPropertyName("behaviorHints")]
        public BehaviorHints BehaviorHints { get; set; }
        [JsonPropertyName("moviedb_id")]
        public long TmdbId { get; set; }
    }

    public partial class BehaviorHints {
        [JsonPropertyName("defaultVideoId")]
        public string DefaultVideoId { get; set; }

        [JsonPropertyName("hasScheduledVideos")]
        public bool HasScheduledVideos { get; set; }
    }

    public partial class Link {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("category")]
        public string Category { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }
    }

    public partial class Popularities {
        [JsonPropertyName("moviedb")]
        public double Moviedb { get; set; }

        [JsonPropertyName("stremio")]
        public double Stremio { get; set; }

        [JsonPropertyName("stremio_lib")]
        public long StremioLib { get; set; }

        [JsonPropertyName("trakt")]
        public long Trakt { get; set; }
    }

    public partial class TrailerStream {
        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("ytId")]
        public string YtId { get; set; }
    }

    public partial class Trailer {
        [JsonPropertyName("source")]
        public string Source { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }
    }

    //public enum MetaType {
    //    Movie,
    //    Series,
    //    Channel,
    //    TV
    //}

    //public enum PosterShape {
    //    Square,
    //    Regular,
    //    Landscape
    //}

    //public class Video {
    //    public string Id { get; set; }
    //    public string Title { get; set; }
    //    public object PublishedAt { get; set; }
    //    public string Thumbnail { get; set; }
    //    public Stream[] Streams { get; set; }
    //    public bool? Available { get; set; }
    //    public int? Season { get; set; }
    //    public int? Episode { get; set; }
    //    public string Trailer { get; set; }
    //    public string Overview { get; set; }
    //}

    //public class Meta {
    //    public string Id { get; set; }
    //    public string Type { get; set; }
    //    public string Name { get; set; }
    //    public string[] Genres { get; set; }
    //    public string Poster { get; set; }
    //    public string PosterShape { get; set; }
    //    public string Background { get; set; }
    //    public string Logo { get; set; }
    //    public string Description { get; set; }
    //    public string ReleaseInfo { get; set; }
    //    public string[] Director { get; set; }
    //    public string[] Cast { get; set; }
    //    public double? ImdbRating { get; set; }
    //    public string DvdRelease { get; set; }
    //    public bool? InTheaters { get; set; }
    //    public Video[] Videos { get; set; }
    //    public string Certification { get; set; }
    //    public string Runtime { get; set; }
    //    public string Language { get; set; }
    //    public string Country { get; set; }
    //    public string Awards { get; set; }
    //    public string Website { get; set; }
    //    public string IsPeered { get; set; }
    //    public int Year { get; set; }
    //}
}
