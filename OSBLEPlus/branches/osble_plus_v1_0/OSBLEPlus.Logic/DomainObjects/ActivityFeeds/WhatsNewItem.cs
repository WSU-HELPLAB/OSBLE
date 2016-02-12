using System;

namespace OSBLEPlus.Logic.DomainObjects.ActivityFeeds
{
    public class WhatsNewItem
    {
        public int Id { get; set; }

        public DateTime DatePosted { get; set; }

        public string NewsHeader { get; set; }

        public string Content { get; set; }
    }
}
