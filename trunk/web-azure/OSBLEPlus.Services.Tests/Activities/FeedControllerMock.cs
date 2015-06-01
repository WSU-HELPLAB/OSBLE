using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using OSBLEPlus.Logic.DomainObjects.ActivityFeeds;

namespace OSBLEPlus.Services.Tests.Activities
{
    public class FeedControllerMock
    {
        private readonly List<FeedItem> _feeds;

        public bool FailGet { get; set; }

        public FeedControllerMock()
        {
            _feeds = new List<FeedItem>()
            {
                new FeedItem{Event = new AskForHelpEvent()},
                new FeedItem{Event = new FeedPostEvent(), Comments = new List<LogCommentEvent>{new LogCommentEvent()}},
                new FeedItem{Event = new FeedPostEvent()},
            };
        }

        public async Task<IEnumerable<FeedItem>> GetFeedsAsync()
        {
            if (FailGet)
            {
                throw new InvalidOperationException();
            }
            await Task.Delay(1000);
            return _feeds;
        }
    }
}