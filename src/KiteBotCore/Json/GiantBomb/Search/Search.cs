using System.Linq;
using Newtonsoft.Json;

namespace KiteBotCore.Json.GiantBomb.Search
{
    internal class Image
    {

        [JsonProperty("icon_url")]
        public string IconUrl { get; set; }

        [JsonProperty("medium_url")]
        public string MediumUrl { get; set; }

        [JsonProperty("screen_url")]
        public string ScreenUrl { get; set; }

        [JsonProperty("small_url")]
        public string SmallUrl { get; set; }

        [JsonProperty("super_url")]
        public string SuperUrl { get; set; }

        [JsonProperty("thumb_url")]
        public string ThumbUrl { get; set; }

        [JsonProperty("tiny_url")]
        public string TinyUrl { get; set; }
    }

    internal class OriginalGameRating
    {

        [JsonProperty("api_detail_url")]
        public string ApiDetailUrl { get; set; }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }

    internal class Platform
    {

        [JsonProperty("api_detail_url")]
        public string ApiDetailUrl { get; set; }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("site_detail_url")]
        public string SiteDetailUrl { get; set; }

        [JsonProperty("abbreviation")]
        public string Abbreviation { get; set; }
    }

    internal class FirstAppearedInFranchise
    {

        [JsonProperty("api_detail_url")]
        public string ApiDetailUrl { get; set; }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }

    internal class FirstAppearedInGame
    {

        [JsonProperty("api_detail_url")]
        public string ApiDetailUrl { get; set; }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }

    internal class FirstCreditedGame
    {

        [JsonProperty("api_detail_url")]
        public string ApiDetailUrl { get; set; }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }

    internal class Result
    {

        [JsonProperty("aliases")]
        public string Aliases { get; set; }

        [JsonProperty("api_detail_url")]
        public string ApiDetailUrl { get; set; }

        [JsonProperty("date_added")]
        public string DateAdded { get; set; }

        [JsonProperty("date_last_updated")]
        public string DateLastUpdated { get; set; }

        [JsonProperty("deck")]
        public string Deck { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("expected_release_day")]
        public object ExpectedReleaseDay { get; set; }

        [JsonProperty("expected_release_month")]
        public object ExpectedReleaseMonth { get; set; }

        [JsonProperty("expected_release_quarter")]
        public object ExpectedReleaseQuarter { get; set; }

        [JsonProperty("expected_release_year")]
        public object ExpectedReleaseYear { get; set; }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("image")]
        public Image Image { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("number_of_user_reviews")]
        public int NumberOfUserReviews { get; set; }

        [JsonProperty("original_game_rating")]
        public OriginalGameRating[] OriginalGameRating { get; set; }

        [JsonProperty("original_release_date")]
        public string OriginalReleaseDate { get; set; }

        [JsonProperty("platforms")]
        public Platform[] Platforms { get; set; }

        [JsonProperty("site_detail_url")]
        public string SiteDetailUrl { get; set; }

        [JsonProperty("resource_type")]
        public string ResourceType { get; set; }

        [JsonProperty("first_appeared_in_franchise")]
        public FirstAppearedInFranchise FirstAppearedInFranchise { get; set; }

        [JsonProperty("first_appeared_in_game")]
        public FirstAppearedInGame FirstAppearedInGame { get; set; }

        [JsonProperty("birth_date")]
        public object BirthDate { get; set; }

        [JsonProperty("country")]
        public object Country { get; set; }

        [JsonProperty("death_date")]
        public object DeathDate { get; set; }

        [JsonProperty("first_credited_game")]
        public FirstCreditedGame FirstCreditedGame { get; set; }

        [JsonProperty("gender")]
        public object Gender { get; set; }

        [JsonProperty("hometown")]
        public object Hometown { get; set; }

        [JsonProperty("abbreviation")]
        public object Abbreviation { get; set; }

        [JsonProperty("date_founded")]
        public string DateFounded { get; set; }

        [JsonProperty("location_address")]
        public object LocationAddress { get; set; }

        [JsonProperty("location_city")]
        public string LocationCity { get; set; }

        [JsonProperty("location_country")]
        public string LocationCountry { get; set; }

        [JsonProperty("location_state")]
        public string LocationState { get; set; }

        [JsonProperty("phone")]
        public object Phone { get; set; }

        [JsonProperty("website")]
        public string Website { get; set; }

        [JsonProperty("hd_url")]
        public string HdUrl { get; set; }

        [JsonProperty("high_url")]
        public string HighUrl { get; set; }

        [JsonProperty("low_url")]
        public string LowUrl { get; set; }

        [JsonProperty("embed_player")]
        public string EmbedPlayer { get; set; }

        [JsonProperty("length_seconds")]
        public int? LengthSeconds { get; set; }

        [JsonProperty("publish_date")]
        public string PublishDate { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("user")]
        public string User { get; set; }

        [JsonProperty("video_type")]
        public string VideoType { get; set; }

        [JsonProperty("youtube_id")]
        public object YoutubeId { get; set; }

        public override string ToString()
        {
            string s = "";
            s += Name != null ? "`Title:` **" + Name : "";
            s += Deck != null ? "**\n`Deck:` " + Deck : "";
            s += OriginalReleaseDate != null ? "\n`Release Date:` " + OriginalReleaseDate.Replace(" 00:00:00", "") : "";
            s += Platforms != null ? "\n`Platforms:` " + string.Join(", ", Platforms?.Select(x => x.Name)) : "";
            s += SiteDetailUrl != null ? "\n`Link:` " + SiteDetailUrl : "";
            s += Image?.SmallUrl != null ? "\n`img:` " + Image?.SmallUrl : "";
            return s;
        }

        /*public override string ToString()
        {
            return $@"
{(Name != null ? "`Title:` **" + Name:"")}
{(Deck != null ? " * *\n`Deck:` " + Deck:"")}
{(OriginalReleaseDate != null ? "\n`Release Date:` " + OriginalReleaseDate.Replace(" 00:00:00", "") : "")}
{(Platforms != null ? "\n`Platforms:` " + string.Join(", ", Platforms?.Select(x => x.Name)) : "")}
{(SiteDetailUrl != null ? "\n`Link:` " + SiteDetailUrl : "")}
{(Image?.SmallUrl != null ? "\n`img:` " + Image?.SmallUrl : "")}";
        }*/
    }

    internal class Search
    {

        [JsonProperty("error")]
        public string Error { get; set; }

        [JsonProperty("limit")]
        public int Limit { get; set; }

        [JsonProperty("offset")]
        public int Offset { get; set; }

        [JsonProperty("number_of_page_results")]
        public int NumberOfPageResults { get; set; }

        [JsonProperty("number_of_total_results")]
        public int NumberOfTotalResults { get; set; }

        [JsonProperty("status_code")]
        public int StatusCode { get; set; }

        [JsonProperty("results")]
        public Result[] Results { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }
    }
}
