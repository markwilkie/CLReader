using System;

namespace CLReader
{
    /// <summary>
    /// Represents a feed item.
    /// </summary>
    public class Item
    {
        public string Link { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string SearchString { get; set; }
        public bool Starred { get; set; }
        public bool MarkClicked { get; set; }
        public bool JennieClicked { get; set; }
        public bool MarkBuy { get; set; }
        public bool JennieBuy { get; set; }
        public bool MarkInterested { get; set; }
        public bool JennieInterested { get; set; }        
        public string WebSite { get; set; }
        public DateTime PublishDate { get; set; }
        public FeedType FeedType { get; set; }
        
        public Item()
        {
            Link = "";
            Title = "";
            Content = "";
            PublishDate = DateTime.Now;
            FeedType = FeedType.RSS;
        }
    }
}