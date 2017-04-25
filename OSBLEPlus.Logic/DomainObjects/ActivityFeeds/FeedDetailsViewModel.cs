using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSBLEPlus.Logic.DomainObjects.ActivityFeeds
{
    public class FeedDetailsViewModel
    {
        public AggregateFeedItem FeedItem { get; set; }
        public bool IsSubscribed { get; set; }
        public string Ids { get; set; }
    }
}
