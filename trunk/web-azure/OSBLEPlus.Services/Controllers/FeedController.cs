using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Cors;

using OSBLEPlus.Logic.DataAccess.Activities;
using OSBLEPlus.Logic.DomainObjects.ActivityFeeds;

namespace OSBLEPlus.Services.Controllers
{
    [EnableCors(origins: "http://localhost:8088", headers: "*", methods: "*")]
    public class FeedController : ApiController
    {
        public async Task<IEnumerable<FeedItem>> Get(DateTime dmin,
            DateTime dmax, IEnumerable<int> ls, IEnumerable<int> ets,
            int? c, int? r, string cf, IEnumerable<int> us)
        {
            return await Task.FromResult(Feeds.Get(dmin, dmax,
                ls, ets, c, r, cf, us, 20));
        }
    }
}
