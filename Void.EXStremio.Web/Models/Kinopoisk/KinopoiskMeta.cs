using System.Text.Json.Serialization;
using Void.EXStremio.Web.Utility;

namespace Void.EXStremio.Web.Models.Kinopoisk {
    public class KinopoiskMeta : ExtendedMeta {
        [JsonPropertyName("kpRating")]
        public string KpRating { get; set; }

        [JsonPropertyName("kp_id")]
        public string KpId { get; set; }

        public bool IsMatch(ExtendedMeta meta) {
            if (Type != meta.Type) { return false; }
            if (Year != meta.Year) { return false; }

            var isNameMatch = MediaNameSimilarity.Calculate(meta.Name, Name) >= 90;
            var isAlternativeNameMatch = meta.AlternativeTitles?.Any(y => MediaNameSimilarity.Calculate(y, Name) >= 90) == true;
            var isLocalizedNameMatch =
                meta?.LocalizedTitles?.Any(y => MediaNameSimilarity.Calculate(y.Title, Name) >= 90) == true
                || (LocalizedTitles?.Any() == true && meta?.LocalizedTitles?.Any(y => MediaNameSimilarity.Calculate(y.Title, LocalizedTitles.First().Title) >= 90) == true);

            return (isNameMatch || isAlternativeNameMatch || isLocalizedNameMatch);
        }
    }
}