﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Okta.AspNetCore;
using okta_aspnetcore_mvc_example.Models;
using Okta.Sdk;
using Okta.Sdk.Configuration;
using RestSharp;



namespace okta_aspnetcore_mvc_example.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration _config;
        private readonly string _redirectUrl;
        private readonly string _goToPortalUrl;

        public UserProfileModel _userProfileModel;
        public HomeController(ILogger<HomeController> logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;
            _redirectUrl = config.GetValue<string>("OktaWeb:PostLogoutRedirectUri");
            _goToPortalUrl = config.GetValue<string>("OktaWeb:GoToPortal");

            _userProfileModel = new UserProfileModel();
            _userProfileModel.assignedApps = new List<AppLink>();
            _userProfileModel.listPermissions = new List<ListPermissionModel>();
            _userProfileModel.listDelegates = new List<ListDelegateModel>();
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult About()
        {
            return View();
        }


        //[Authorize]
        public ActionResult Login()
        {
            ViewBag.Message = "Your Login Page.";

            if (!HttpContext.User.Identity.IsAuthenticated)
            {
                var properties = new AuthenticationProperties();
                //without this, the redirect defaults to entry point of initialization
                //properties.RedirectUri = "/Home/PostLogin";
                return Challenge(properties, OktaDefaults.MvcAuthenticationScheme);

            }

            bool userProfileComplete = CheckUserProfile();

            if (userProfileComplete)
            {
                return RedirectToAction("Index", "Home");
            }
            else
            {
                GetProfile();
                LocalLogout().Wait();
                return View("Profile", _userProfileModel);
            }

        }


        public async Task<ActionResult> LocalLogout()
        {
            //this will only logut of local session
            if (HttpContext.User.Identity.IsAuthenticated)
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }

            return View("Profile", _userProfileModel);
            //return Redirect(_redirectUrl);
        }


        public ActionResult PortalLogin()
        {
            return Redirect(_goToPortalUrl);
            //return Redirect(_redirectUrl);
        }


        public async Task<ActionResult> Logout()
        {
            //this will only logut of local session
            if (HttpContext.User.Identity.IsAuthenticated)
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }

            //return View("PostLogout");
            return Redirect(_redirectUrl);
        }

        //public ActionResult Logout()
        //{
        // this will logout of local session and okta session
        //    if (HttpContext.User.Identity.IsAuthenticated)
        //    {
        //        return new SignOutResult(
        //        new[]
        //        {
        //             OktaDefaults.MvcAuthenticationScheme,
        //             CookieAuthenticationDefaults.AuthenticationScheme
        //        },
        //       new AuthenticationProperties { RedirectUri = "/Home/Index" });
        //    }

        //    return RedirectToAction("PostLogOut", "Home");
        //}

        public ActionResult PostLogin()
        {

            return View();
        }

        public ActionResult PostLogOut()
        {

            return View();
        }



        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }


        public void GetProfile()
        {
            //UserProfileModel userProfile = new UserProfileModel();

            //get current profile
            Okta.Sdk.User oktaUser = null;

            var client = new OktaClient(new OktaClientConfiguration
            {
                OktaDomain = _config.GetValue<string>("OktaWeb:OktaDomain"),
                Token = _config.GetValue<string>("OktaWeb:ApiToken")
            });

            var oktaId = this.User.Claims.FirstOrDefault(x => x.Type == "sub").Value;
            var idp = this.User.Claims.FirstOrDefault(x => x.Type == "idp").Value;

            oktaUser = (Okta.Sdk.User)client.Users.GetUserAsync(oktaId).Result;
            //common attributes
            if (oktaUser.Profile.Email != null) { _userProfileModel.email = oktaUser.Profile.Email; }
            if (oktaUser.Profile.FirstName != null) { _userProfileModel.firstName = oktaUser.Profile.FirstName; }
            if (oktaUser.Profile.LastName != null) { _userProfileModel.lastName = oktaUser.Profile.LastName; }

            //consumer attributes
            if (oktaUser.Profile["region"] != null) { _userProfileModel.region = oktaUser.Profile["region"].ToString(); }

            //consumer progressive attributes
            if (oktaUser.Profile["streetAddress"] != null) { _userProfileModel.streetAddress = oktaUser.Profile["streetAddress"].ToString(); }
            if (oktaUser.Profile["city"] != null) { _userProfileModel.city = oktaUser.Profile["city"].ToString(); }
            if (oktaUser.Profile["state"] != null) { _userProfileModel.state = oktaUser.Profile["state"].ToString(); }
            if (oktaUser.Profile["zipCode"] != null) { _userProfileModel.zipCode = oktaUser.Profile["zipCode"].ToString(); }
            if (oktaUser.Profile["insuranceId"] != null) { _userProfileModel.insuranceId = oktaUser.Profile["insuranceId"].ToString(); }
            if (oktaUser.Profile["insuranceCarrier"] != null) { _userProfileModel.insuranceCarrier = oktaUser.Profile["insuranceCarrier"].ToString(); }


            //provider attributes
            if (oktaUser.Profile["licenseState"] != null) { _userProfileModel.licenseState = oktaUser.Profile["licenseState"].ToString(); }
            if (oktaUser.Profile["providerId"] != null) { _userProfileModel.providerId = oktaUser.Profile["providerId"].ToString(); }
            if (oktaUser.Profile["practiceName"] != null) { _userProfileModel.practiceName = oktaUser.Profile["practiceName"].ToString(); }

            //preferences
            if (oktaUser.Profile["Promotions"] != null) { _userProfileModel.Promotions = (bool)oktaUser.Profile["Promotions"]; }
            if (oktaUser.Profile["ProductUpdates"] != null) { _userProfileModel.ProductUpdates = (bool)oktaUser.Profile["ProductUpdates"]; }
            if (oktaUser.Profile["Webinars"] != null) { _userProfileModel.Webinars = (bool)oktaUser.Profile["Webinars"]; }


            //consent
            if (oktaUser.Profile["last_verification_date"] != null) { _userProfileModel.last_verification_date = oktaUser.Profile["last_verification_date"].ToString(); }
            if (oktaUser.Profile["consent"] != null) { _userProfileModel.consent = oktaUser.Profile["consent"].ToString(); }

            //misc
            if (oktaUser.Profile["level_of_assurance"] != null) { _userProfileModel.level_of_assurance = oktaUser.Profile["level_of_assurance"].ToString(); }
            if (oktaUser.Profile["primaryRole"] != null) { _userProfileModel.primaryRole = oktaUser.Profile["primaryRole"].ToString(); }
            _userProfileModel.oktaId = oktaId;
            _userProfileModel.auth_idp = idp;

            return;
        }

        [Authorize]
        public IActionResult Profile()
        {

            GetProfile();

            return View("Profile",_userProfileModel);
            //return View(_userProfileModel);
        }

        [HttpPost]
        //[Authorize]
        public IActionResult ProfileRoute([FromForm] UpdateProfileModel updateUser)
        {

            

            //if (string.IsNullOrEmpty(updateUser.insuranceId))
            //{
            //    updateUser.insuranceId = "\n";
            //}
            //if (string.IsNullOrEmpty(updateUser.insuranceCarrier))
            //{
            //    updateUser.insuranceCarrier = "\n";
            //}

            var destPage = _config.GetValue<string>("SendApi:UpdateProfileFlo");
            string consentToken = _config.GetValue<string>("SendApi:UpdateProfileToken");
            IRestResponse response = null;


            var client = new RestClient(destPage);
            var request = new RestRequest(Method.POST);
            // request.AddHeader("cache-control", "no-cache");
            request.AddHeader("Accept", "application/json");
            request.AddHeader("Content-Type", "application/json");
            request.AddHeader("x-api-client-token", consentToken);
            request.AddJsonBody(updateUser);
            response = client.Execute(request);


            if (response.StatusDescription == "Forbidden" || response.StatusDescription == "Unauthorized")
            {

            }


            if (response.StatusDescription == "OK")
            {

            }
            else
            {

            }

            return RedirectToAction("Index", "Home");
        }


        [Authorize]
        public IActionResult Token()
        {
            return View(HttpContext.User.Claims);
        }


        public UserProfileModel GetAppsUserData()
        {
            UserProfileModel myAppProfile = new UserProfileModel();
            myAppProfile.unassignedApps = new List<string>();
            myAppProfile.assignedApps = new List<AppLink>();
            Okta.Sdk.User oktaUser = null;
            myAppProfile.listPermissions = new List<ListPermissionModel>();
            myAppProfile.listDelegates = new List<ListDelegateModel>();
            //ListPermissionModel myPermissions = new ListPermissionModel();
            //ListDelegateModel myDelegates = new ListDelegateModel();

            var client = new OktaClient(new OktaClientConfiguration
            {
                OktaDomain = _config.GetValue<string>("OktaWeb:OktaDomain"),
                Token = _config.GetValue<string>("OktaWeb:ApiToken")
            });

            /// get apps assigned to user
            var oktaId = this.User.Claims.FirstOrDefault(x => x.Type == "sub").Value;
            oktaUser = (Okta.Sdk.User)client.Users.GetUserAsync(oktaId).Result;

            var sortingList = new List<string>();

            var listAssignedApps = client.Users.ListAppLinks(oktaId).ToListAsync().Result;
            foreach (var item in listAssignedApps)
            {
                if (item.Label.IndexOf("CVS Home") < 0)
                {
                    myAppProfile.assignedApps.Add((AppLink)item);

                }
                sortingList.Add(item.Label);
            }

            var listAllApps = client.Applications.ListApplications().ToListAsync().Result;
            foreach (var item in listAllApps)
            {
                var temp1 = item.Label.IndexOf("CVS");
                if (item.Label.IndexOf("CVS") == 0)
                {
                    if (!sortingList.Contains(item.Label))
                    {
                        //var temp = item.Label;
                        //allAppList.Add(item.Label);
                        myAppProfile.unassignedApps.Add(item.Label);
                    }
                }
            }


            //get list of current delegates
            if (this.User.Claims.FirstOrDefault(x => x.Type == "peopleIhaveDelegated") != null)
            {
                var tempDelegates = this.User.Claims.Where(x => x.Type == "peopleIhaveDelegated").Where(p => p.Issuer != "OpenIdConnect").ToList();
                foreach (var item in tempDelegates)
                {
                    ListDelegateModel myDelegates = new ListDelegateModel();
                    //value == oktaid, perform lookup and get addl data
                    oktaUser = (Okta.Sdk.User)client.Users.GetUserAsync(item.Value).Result;
                    myDelegates.oktaId = item.Value;
                    myDelegates.email = oktaUser.Profile.Email;
                    myDelegates.userName = oktaUser.Profile.LastName;
                    myAppProfile.listDelegates.Add(myDelegates);
                }
            }

            //get list of current permissions
            if (this.User.Claims.FirstOrDefault(x => x.Type == "peopleIhavePermissionOver") != null)
            {
                var tempPermissions = this.User.Claims.Where(x => x.Type == "peopleIhavePermissionOver").Where(p => p.Issuer != "OpenIdConnect").ToList();
                foreach (var item in tempPermissions)
                {
                    ListPermissionModel myPermissions = new ListPermissionModel();
                    //value == oktaid, perform lookup and get addl data
                    oktaUser = (Okta.Sdk.User)client.Users.GetUserAsync(item.Value).Result;
                    myPermissions.oktaId = item.Value;
                    myPermissions.email = oktaUser.Profile.Email;
                    myPermissions.userName = oktaUser.Profile.LastName;


                    myAppProfile.listPermissions.Add(myPermissions);
                }
            }



            return myAppProfile;
        }


        public List<string> GetAllApps()
        {

            List<string> allAppList = new List<string>();

            var client = new OktaClient(new OktaClientConfiguration
            {
                OktaDomain = _config.GetValue<string>("OktaWeb:OktaDomain"),
                Token = _config.GetValue<string>("OktaWeb:ApiToken")
            });



            var myList = client.Applications.ListApplications().ToListAsync().Result;
            //var myList = client.Users.ListAppLinks(oktaId, showAll: true).ToListAsync().Result;
            foreach (var item in myList)
            {
                var temp1 = item.Label.IndexOf("CVS");
                if (item.Label.IndexOf("CVS") == 0)
                {
                    //var temp = item.Label;
                    allAppList.Add(item.Label);
                }

            }

            return allAppList;
        }

        public List<AppLink> GetUserApps()
        {
            Okta.Sdk.User oktaUser = null;
            List<AppLink> userAppList = new List<AppLink>();

            var client = new OktaClient(new OktaClientConfiguration
            {
                OktaDomain = _config.GetValue<string>("OktaWeb:OktaDomain"),
                Token = _config.GetValue<string>("OktaWeb:ApiToken")
            });

            var oktaId = this.User.Claims.FirstOrDefault(x => x.Type == "sub").Value;



            oktaUser = (Okta.Sdk.User)client.Users.GetUserAsync(oktaId).Result;

            //string userId = oktaUser.Id;

            //var myResource = client.GetAsync<Okta.Sdk.Resource>(new Okta.Sdk.HttpRequest
            //{
            //    Uri = $"/api/v1/users/{userId}/appLinks",
            //    PathParameters = new Dictionary<string, object>()
            //    {
            //        ["userId"] = oktaId,
            //    }
            //});

            ////Okta.Sdk.IResource;

            //CollectionClient<Okta.Sdk.IResource> myCol = client.GetCollection<Okta.Sdk.IResource>(new Okta.Sdk.HttpRequest
            //{
            //    Uri = $"/api/v1/users/{userId}/appLinks",
            //    PathParameters = new Dictionary<string, object>()
            //    {
            //        ["userId"] = oktaId,
            //    }
            //});


            var myList = client.Users.ListAppLinks(oktaId).ToListAsync().Result;
            foreach (var item in myList)
            {
                if (item.Label.IndexOf("CVS Home") < 0)
                {
                    userAppList.Add((AppLink)item);
                }

            }

            return userAppList;
        }


        public Boolean CheckUserProfile()
        {
            bool userProfileComplete = true;

            if (this.User.Claims.FirstOrDefault(x => x.Type == "required") != null) 
            {
                List<System.Security.Claims.Claim> required = this.User.Claims.Where(x => x.Type == "required").Where(p => p.Issuer != "OpenIdConnect").ToList();

                 if (required.Count > 0)
                {
                    userProfileComplete = false;
                }
           }

            return userProfileComplete;
        }


    }
}
