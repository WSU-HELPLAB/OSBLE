using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Http;

using OSBLEPlus.Logic.DataAccess.Activities;
using OSBLEPlus.Logic.DomainObjects.ActivityFeeds;
using OSBLEPlus.Services.Attributes;

namespace OSBLEPlus.Services.Controllers
{
    public class FeedController : ApiController
    {
        [AllowAdmin]
        public async Task<IEnumerable<FeedItem>> Get([FromUri]DateTime dmin,
            DateTime dmax, IEnumerable<int> ls, IEnumerable<int> ets,
            int? c, int? r, string cf, IEnumerable<int> us, int topN)
        {
            return await Task.FromResult(Feeds.Get(dmin, dmax,
                ls, ets, c, r, cf, us, topN));
        }
    }
}
