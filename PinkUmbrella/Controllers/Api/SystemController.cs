using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement.Mvc;
using PinkUmbrella.Models.Settings;
using PinkUmbrella.Repositories;
using PinkUmbrella.Services;
using PinkUmbrella.Util;
using Poncho.Models.Auth;
using Poncho.Models.Peer;

namespace PinkUmbrella.Controllers.Api
{
    [FeatureGate(FeatureFlags.ApiControllerSystem)]
    [ServiceFilter(typeof(ApiCallFilterAttribute))]
    [Route("/Api/[controller]/[action]"), ApiController]
    [AllowAnonymous]
    [ApiExplorerSettings(IgnoreApi = false, GroupName = nameof(SystemController))]
    [ApiVersionNeutral]
    public class SystemController: Controller
    {
        private readonly IAuthService _auth;
        private readonly IPeerService _peers;
        private readonly SimpleDbContext _db;
        private readonly IApiDescriptionGroupCollectionProvider _apiExplorer;
        
        public SystemController(IAuthService auth, SimpleDbContext db, IPeerService peers, IApiDescriptionGroupCollectionProvider apiExplorer)
        {
            _auth = auth;
            _db = db;
            _peers = peers;
            _apiExplorer = apiExplorer;
        }

        [AllowAnonymous]
        [Produces("application/json", "application/pink-umbrella")]
        [ProducesResponseType(typeof(PeerModel), 200)]
        [HttpGet]
        public async Task<ActionResult> Index()
        {
            return Json(new PeerModel() {
                DisplayName = "Hello World",
                Address = new IPAddressModel()
                {
                    Name = HttpContext.Request.Host.Host,
                },
                AddressPort = HttpContext.Request.Host.Port ?? 443,
                PublicKey = await _auth.GetCurrent(),
            });
        }

        [AllowAnonymous]
        [Produces("application/json", "application/pink-umbrella")]
        [ProducesResponseType(typeof(PeerStatsModel), 200)]
        [HttpGet]
        public async Task<ActionResult> Stats()
        {
            return Json(new PeerStatsModel() {
                PeerCount = await _peers.CountAsync(),
                MediaCount = await _db.ArchivedMedia.CountAsync(),
                PostCount = await _db.Posts.CountAsync(),
                ShopCount = await _db.Shops.CountAsync(),
                UserCount = await _db.Users.CountAsync(),
                StartTime = Process.GetCurrentProcess().StartTime,
            });
        }

        [AllowAnonymous]
        [Produces("application/json", "application/pink-umbrella")]
        [ProducesResponseType(typeof(ApiListModel), 200)]
        [HttpGet]
        public async Task<ActionResult> Api(string filter)
        {
            var actions = _apiExplorer.ApiDescriptionGroups.Items.SelectMany(api => api.Items).Select(action => new ApiListModel
            {
                Method = action.HttpMethod,
                Route = action.RelativePath,
            });
            if (!string.IsNullOrWhiteSpace(filter) && filter.Length < 255)
            {
                filter = filter.ToLower();
                actions = actions.Where(e => e.Route.ToLower().Contains(filter));
            }
            return Json(new
            {
                Items = actions.ToList()
            });
        }
    }
}