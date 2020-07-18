﻿using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using PinkUmbrella.Models;
using PinkUmbrella.Services;
using PinkUmbrella.ViewModels.Home;
using PinkUmbrella.ViewModels;
using PinkUmbrella.Util;
using PinkUmbrella.ViewModels.Shared;
using PinkUmbrella.Models.Settings;
using Microsoft.FeatureManagement.Mvc;
using PinkUmbrella.Models.Search;
using PinkUmbrella.Services.Local;
using PinkUmbrella.Models.Public;
using Poncho.Models;
using Poncho.Models.Public;
using Poncho.Models.Auth;

namespace PinkUmbrella.Controllers
{
    [FeatureGate(FeatureFlags.ControllerHome)]
    [AllowAnonymous]
    public class HomeController : BaseController
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ISearchService _searchService;
        private readonly IFeedService _feedService;

        public HomeController(IWebHostEnvironment environment, ILogger<HomeController> logger, SignInManager<UserProfileModel> signInManager,
            UserManager<UserProfileModel> userManager, IPostService postService, IUserProfileService localProfiles, IPublicProfileService publicProfiles, ISearchService searchService,
            IReactionService reactions, IFeedService feedService, ITagService tags, INotificationService notifications, IPeerService peers,
            IAuthService auth, ISettingsService settings):
            base(environment, signInManager, userManager, postService, localProfiles, publicProfiles, reactions, tags, notifications, peers, auth, settings)
        {
            _logger = logger;
            _searchService = searchService;
            _feedService = feedService;
        }

        [Route("/"), Route("/{address}-{port}")]
        public async Task<IActionResult> Index(string address = null, int? port = null)
        {
            ViewData["Controller"] = $"Home ({address}-{port})";
            ViewData["Action"] = nameof(Index);
            var user = await GetCurrentUserAsync();
            var model = new IndexViewModel() {
                Source = FeedSource.Following,
                MyProfile = user,
            };

            if (address == null)
            {
                if (user != null)
                {
                    model.MyFeed = await _feedService.GetFeedForUser(user.PublicId, user.UserId, false, new PaginationModel() { count = 10, start = 0 });
                    return View(model);
                }
                else
                {
                    if (_signInManager.IsSignedIn(User)) {
                        await _signInManager.SignOutAsync();
                        return RedirectToAction(nameof(Index));
                    }
                    return View("Welcome", model);
                }
            }
            else
            {
                var ip = await _auth.GetOrRememberIP(IPAddress.Parse(address));
                var client = await _peers.Open(ip, port);
                var peer = await _peers.GetPeer(ip, port);

                ViewData["Peer"] = peer;
                var bodycontent = await client.QueryHtml("", await _auth.GetKeyPair(peer.PublicKey));
                bodycontent = bodycontent.HtmlExtractBody();
                bodycontent = bodycontent.HtmlExtractMain();
                return View("Proxy/_BodyContent", new BodyContentViewModel() {
                    MyProfile = user,
                    Html = bodycontent,
                });
            }
        }


        [Route("/Welcome")]
        public async Task<IActionResult> Welcome()
        {
            ViewData["Controller"] = "Home";
            ViewData["Action"] = nameof(Welcome);
            return View(new BaseViewModel() { MyProfile = await GetCurrentUserAsync() });
        }

        [Route("/Mentions")]
        public async Task<IActionResult> Mentions()
        {
            ViewData["Controller"] = "Home";
            ViewData["Action"] = nameof(Mentions);
            var user = await GetCurrentUserAsync();
            var model = new IndexViewModel() {
                Source = FeedSource.Mentions,
                MyProfile = user,
                MyFeed = await _posts.GetMentionsForUser(user.PublicId, user.UserId, false, new PaginationModel() { count = 10, start = 0 })
            };
            return View(nameof(Index), model);
        }

        [Route("/MyPosts")]
        public async Task<IActionResult> MyPosts()
        {
            ViewData["Controller"] = "Home";
            ViewData["Action"] = nameof(MyPosts);
            var user = await GetCurrentUserAsync();
            var model = new IndexViewModel() {
                Source = FeedSource.Myself,
                MyProfile = user,
                MyFeed = await _posts.GetPostsForUser(user.PublicId, user.UserId, false, new PaginationModel() { count = 10, start = 0 })
            };
            return View(nameof(Index), model);
        }
        
        [Route("/Search")]
        public async Task<IActionResult> Search(string q, SearchResultOrder order = SearchResultOrder.Top,
                                                SearchResultType? t = null, int start = 0, int count = 10,
                                                string[] tags = null)
        {
            ViewData["Controller"] = "Home";
            ViewData["Action"] = nameof(Search);
            var pagination = new PaginationModel() { start = start, count = count };
            var user = await GetCurrentUserAsync();
            var request = new SearchRequestModel() {
                text = q, viewerId = user?.UserId,
                type = t, order = order,
                pagination = pagination, tags = tags
            };
            SearchResultsModel results = await _searchService.Search(request);

            return View(new SearchViewModel()
            {
                SearchText = q,
                Order = order,
                Type = t,
                MyProfile = user,
                Results = results
            });
        }

        [Authorize, HttpGet]
        public async Task<IActionResult> Notifications(int? sinceId = null, bool includeViewed = true, bool includeDismissed = false, PaginationModel pagination = null)
        {
            ViewData["Controller"] = "Home";
            ViewData["Action"] = nameof(Notifications);
            var user = await GetCurrentUserAsync();
            var notifs = await _notifications.GetNotifications(user.UserId, sinceId, includeViewed, includeDismissed, pagination ?? new PaginationModel());

            var fromUsers = new Dictionary<string, PublicProfileModel>();
            foreach (var notif in notifs.Items)
            {
                var id = new PublicId(notif.Notif.FromUserId, notif.Notif.FromPeerId);
                if (fromUsers.TryGetValue(id.ToString(), out var userProfile))
                {
                    notif.FromUser = userProfile;
                }
                else
                {
                    notif.FromUser = await _publicProfiles.GetUser(id, user.UserId);
                    fromUsers.Add(id.ToString(), notif.FromUser);
                }
            }

            return View(new NotificationsViewModel()
            {
                MyProfile = user,
                Items = notifs,
                SinceId = sinceId,
                IncludeViewed = includeViewed,
                IncludeDismissed = includeDismissed,
            });
        }

        [Route("/Links")]
        public async Task<IActionResult> Links()
        {
            ViewData["Controller"] = "Home";
            ViewData["Action"] = nameof(Links);
            return View(new BaseViewModel() { MyProfile = await GetCurrentUserAsync() });
        }

        [Route("/Privacy")]
        public async Task<IActionResult> Privacy()
        {
            ViewData["Controller"] = "Home";
            ViewData["Action"] = nameof(Privacy);
            return View(new PrivacyViewModel() { MyProfile = await GetCurrentUserAsync() });
        }

        [Route("/About")]
        public async Task<IActionResult> About()
        {
            ViewData["Controller"] = "Home";
            ViewData["Action"] = nameof(About);
            return View(new BaseViewModel() { MyProfile = await GetCurrentUserAsync() });
        }

        [Route("/Terms")]
        public async Task<IActionResult> Terms()
        {
            ViewData["Controller"] = "Home";
            ViewData["Action"] = nameof(Terms);
            return View(new BaseViewModel() { MyProfile = await GetCurrentUserAsync() });
        }

        [Route("/Error/{code}")]
        public async Task<IActionResult> Error(string code)
        {
            string originalUrl = null;
            #region snippet_StatusCodeReExecute
            var statusCodeReExecuteFeature = HttpContext.Features.Get<IStatusCodeReExecuteFeature>();
            if (statusCodeReExecuteFeature != null)
            {
                originalUrl =
                    statusCodeReExecuteFeature.OriginalPathBase
                    + statusCodeReExecuteFeature.OriginalPath
                    + statusCodeReExecuteFeature.OriginalQueryString;
            }
            #endregion

            var user = await GetCurrentUserAsync();
            return View(new ErrorViewModel()
            {
                MyProfile = user,
                ErrorCode = code,
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                OriginalURL = originalUrl,
            });
        }
    }
}