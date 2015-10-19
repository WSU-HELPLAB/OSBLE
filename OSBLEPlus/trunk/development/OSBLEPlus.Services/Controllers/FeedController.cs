using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Services.Protocols;
using OSBLEPlus.Logic.DataAccess.Activities;
using OSBLEPlus.Logic.DomainObjects.ActivityFeeds;
using OSBLEPlus.Services.Attributes;

namespace OSBLEPlus.Services.Controllers
{
    public class FeedController : ApiController
    {
        /// <summary>
        /// Queries the database for items to return to place in an Activity Feed.
        /// </summary>
        /// <param name="dmin">Minimum DateTime</param>
        /// <param name="dmax">Maximum DateTime</param>
        /// <param name="lmin">Minimum EventLogId</param>
        /// <param name="lmax">Maximum EventLogId</param>
        /// <param name="ls">IEnumerable of Specific LogIDs</param>
        /// <param name="ets">IEnumerable of Event Types</param>
        /// <param name="c">Course Id</param>
        /// <param name="r">Role Id</param>
        /// <param name="cf">Comment Filter (looks for a string at contains this item)</param>
        /// <param name="us">Sender Ids</param>
        /// <param name="topN">Max amount of posts to return</param>
        /// <returns>IEnumerable of FeedItem</returns>
        [AllowAdmin]
        public async Task<IEnumerable<FeedItem>> Get([FromUri]DateTime dmin,
            DateTime dmax, int? lmin, int? lmax, IEnumerable<int> ls, IEnumerable<int> ets,
            int? c, int? r, string cf, IEnumerable<int> us, int? topN)
        {
            return await Task.FromResult(Feeds.Get(dmin, dmax, lmin, lmax,
                ls, ets, c, r, cf, us, topN));
        }
    }
}
