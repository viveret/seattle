﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using seattle.Models;
using seattle.Services;
using seattle.ViewModels.Home;

namespace seattle.Controllers
{
    [AllowAnonymous]
    public class HomeController : BaseController
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ISearchService _searchService;

        public HomeController(IWebHostEnvironment environment, ILogger<HomeController> logger, SignInManager<UserProfileModel> signInManager,
            UserManager<UserProfileModel> userManager, IPostService postService, IUserProfileService userProfiles, ISearchService searchService):
            base(environment, signInManager, userManager, postService, userProfiles)
        {
            _logger = logger;
            _searchService = searchService;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Controller"] = "Home";
            ViewData["Action"] = nameof(Index);
            var user = await GetCurrentUserAsync();
            var model = new IndexViewModel() {
                Source = FeedSource.Following,
                MyProfile = user,
            };

            if (user != null)
            {
                model.MyFeed = await _posts.GetFeedForUser(user.Id, user.Id, false, new PaginationModel() { count = 10, start = 0 });
                return View(model);
            }
            else
            {
                if (_signInManager.IsSignedIn(User)) {
                    await _signInManager.SignOutAsync();
                    return RedirectToAction(nameof(Index));
                }
                return View("IndexAnonymous", model);
            }
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
                MyFeed = await _posts.GetMentionsForUser(user.Id, user.Id, false, new PaginationModel() { count = 10, start = 0 })
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
                MyFeed = await _posts.GetPostsForUser(user.Id, user.Id, false, new PaginationModel() { count = 10, start = 0 })
            };
            return View(nameof(Index), model);
        }

        [Route("/Privacy")]
        public IActionResult Privacy()
        {
            ViewData["Controller"] = "Home";
            ViewData["Action"] = nameof(Privacy);
            return View(new PrivacyViewModel());
        }

        [Route("/About")]
        public IActionResult About()
        {
            ViewData["Controller"] = "Home";
            ViewData["Action"] = nameof(About);
            return View();
        }
        
        [Route("/Search")]
        public async Task<IActionResult> Search(string q, SearchResultOrder order, SearchResultType? t, int start, int count = 10)
        {
            ViewData["Controller"] = "Home";
            ViewData["Action"] = nameof(Search);
            var pagination = new PaginationModel() { start = start, count = count };
            var user = await GetCurrentUserAsync();
            SearchResultsModel results;
            if (t.HasValue) {
                results = await _searchService.Get(t.Value).Search(q, order, pagination);
            } else {
                results = await _searchService.Search(q, order, pagination);
            }

            return View(new SearchViewModel() {
                SearchText = q,
                Order = order,
                Type = t,
                MyProfile = user,
                Results = results
            });
        }

        [Route("/StatusCode")]
        public void StatusCode(string code)
        {
            ViewData["RequestId"] = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            ViewData["ErrorStatusCode"] = code;

            #region snippet_StatusCodeReExecute
            var statusCodeReExecuteFeature = HttpContext.Features.Get<IStatusCodeReExecuteFeature>();
            if (statusCodeReExecuteFeature != null)
            {
                ViewData["OriginalURL"] =
                    statusCodeReExecuteFeature.OriginalPathBase
                    + statusCodeReExecuteFeature.OriginalPath
                    + statusCodeReExecuteFeature.OriginalQueryString;
            }
            #endregion
        }
    }
}
