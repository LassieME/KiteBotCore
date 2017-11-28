using GiantBomb.Api.Model;

namespace ExtendedGiantBombClient.Model{
    public class Video{
        public string ApiDetailUrl { get; set; }
        public string Deck { get; set; }
        public string HdUrl { get; set; }
        public string HighUrl { get; set; }
        public string LowUrl { get; set; }
        public string EmbedPlayer { get; set; }
        public int Id { get; set; }
        public Image Image { get; set; }
        public int LengthSeconds { get; set; }
        public string Name { get; set; }
        public string PublishDate { get; set; }
        public string SiteDetailUrl { get; set; }
        public string Url { get; set; }
        public string User { get; set; }
        public VideoCategory[] VideoCategories { get; set; }
        public string VideoType { get; set; }
        public VideoShow VideoShow { get; set; }
        public string YoutubeId { get; set; }
        public string SavedTime { get; set; }
    }

    public class VideoShow{
        public string ApiDetailUrl { get; set; }
        public int Id { get; set; }
        public string Title { get; set; }
        public int Position { get; set; }
        public string SiteDetailUrl { get; set; }
    }

    public class VideoCategory
    {
        public string ApiDetailUrl { get; set; }
        public int Id { get; set; }
        public string Name { get; set; }
        public string SiteDetailUrl { get; set; }
    }
}
