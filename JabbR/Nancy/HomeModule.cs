using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using JabbR.Infrastructure;
using JabbR.Services;
using JabbR.UploadHandlers;
using JabbR.ViewModels;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using System.Text.Encodings.Web;

namespace JabbR.Nancy
{
    public class HomeController : Controller
    {
        private static readonly Regex clientSideResourceRegex = new Regex("^(Client_.*|Chat_.*|Content_.*|Create_.*|LoadingMessage|Room.*)$");
        private readonly ApplicationSettings _settings;
        private readonly IJabbrConfiguration _configuration;
        private readonly IConnectionManager _connectionManager;
        private readonly IJabbrRepository _jabbrRepository;

        public HomeController(ApplicationSettings settings,
                              IJabbrConfiguration configuration,
                              IConnectionManager connectionManager,
                              IJabbrRepository jabbrRepository)
        {
            _settings = settings;
            _configuration = configuration;
            _connectionManager = connectionManager;
            _jabbrRepository = jabbrRepository;
        }

        [HttpGet("/")]
        public IActionResult Index()
        {
            if (User.Identity.IsAuthenticated)
            {
                var viewModel = new SettingsViewModel
                {
                    GoogleAnalytics = _settings.GoogleAnalytics,
                    AppInsights = _settings.AppInsights,
                    Sha = _configuration.DeploymentSha,
                    Branch = _configuration.DeploymentBranch,
                    Time = _configuration.DeploymentTime,
                    DebugMode = (bool)HttpContext.Items["_debugMode"],
                    Version = Constants.JabbRVersion,
                    IsAdmin = User.HasClaim(JabbRClaimTypes.Admin, "true"),
                    ClientLanguageResources = BuildClientResources(),
                    MaxMessageLength = _settings.MaxMessageLength,
                    AllowRoomCreation = _settings.AllowRoomCreation || User.HasClaim(JabbRClaimTypes.Admin, "true")
                };

                return View("index", viewModel);
            }

            if (User.Identity.IsAuthenticated && User.HasPartialIdentity())
            {
                // If the user is partially authenticated then take them to the register page
                return RedirectToAction("Register", "Account");
            }

            return Unauthorized();
        }

        [HttpGet("/monitor")]
        public IActionResult Monitor()
        {
            if (User.Identity.IsAuthenticated && User.HasClaim(JabbRClaimTypes.Admin, "true"))
            {
                return View("monitor");
            }

            return Forbid();
        }

        [HttpGet("/status")]
        public async Task<IActionResult> Status()
        {
            var model = new StatusViewModel();

            // Try to send a message via SignalR
            var signalrStatus = new SystemStatus { SystemName = "SignalR messaging" };
            model.Systems.Add(signalrStatus);

            try
            {
                var hubContext = _connectionManager.GetHubContext<Chat>();
                await (Task)hubContext.Clients.Client("doesn't exist").noMethodCalledThis();

                signalrStatus.SetOK();
            }
            catch (Exception ex)
            {
                signalrStatus.SetException(ex.GetBaseException());
            }

            // Try to talk to database
            var dbStatus = new SystemStatus { SystemName = "Database" };
            model.Systems.Add(dbStatus);

            try
            {
                var roomCount = _jabbrRepository.Rooms.Count();
                dbStatus.SetOK();
            }
            catch (Exception ex)
            {
                dbStatus.SetException(ex.GetBaseException());
            }

            // Try to talk to azure storage
            var azureStorageStatus = new SystemStatus { SystemName = "Azure Upload storage" };
            model.Systems.Add(azureStorageStatus);

            try
            {
                if (!String.IsNullOrEmpty(_settings.AzureblobStorageConnectionString))
                {
                    var azure = new AzureBlobStorageHandler(_settings);
                    UploadResult result;
using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("test")))
                    {
                        result = await azure.UploadFile("statusCheck.txt", "text/plain", stream);
                    }

                    azureStorageStatus.SetOK();
                }
                else
                {
                    azureStorageStatus.StatusMessage = "Not configured";
                }
            }
            catch (Exception ex)
            {
                azureStorageStatus.SetException(ex.GetBaseException());
            }

            //try to talk to local storage
            var localStorageStatus = new SystemStatus { SystemName = "Local Upload storage" };
            model.Systems.Add(localStorageStatus);

            try
            {
                if (!String.IsNullOrEmpty(_settings.LocalFileSystemStoragePath) && !String.IsNullOrEmpty(_settings.LocalFileSystemStorageUriPrefix))
                {
                    var local = new LocalFileSystemStorageHandler(_settings);
                    UploadResult localResult;
using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("test")))
                    {
                        localResult = await local.UploadFile("statusCheck.txt", "text/plain", stream);
                    }

                    localStorageStatus.SetOK();
                }
                else
                {
                    localStorageStatus.StatusMessage = "Not configured";
                }
            }
            catch (Exception ex)
            {
                localStorageStatus.SetException(ex.GetBaseException());
            }

            // Force failure
            if (HttpContext.Request.Query.ContainsKey("fail"))
            {
                var failedSystem = new SystemStatus { SystemName = "Forced failure" };
                failedSystem.SetException(new ApplicationException("Forced failure for test purposes"));
                model.Systems.Add(failedSystem);
            }

            if (!model.AllOK)
            {
                return StatusCode(500, View("status", model));
            }

            return View("status", model);
        }

        private static string BuildClientResources()
        {
            var resourceSet = LanguageResources.ResourceManager.GetResourceSet(CultureInfo.CurrentUICulture, true, true);
            var invariantResourceSet = LanguageResources.ResourceManager.GetResourceSet(CultureInfo.InvariantCulture, true, true);

            var resourcesToEmbed = new Dictionary<string, string>();
            foreach (DictionaryEntry invariantResource in invariantResourceSet)
            {
                var resourceKey = (string)invariantResource.Key;

                if (clientSideResourceRegex.IsMatch(resourceKey))
                {
                    try
                    {
                        resourcesToEmbed.Add(resourceKey, resourceSet.GetString(resourceKey));
                    }
                    catch (InvalidOperationException)
                    {
                        // The resource specified by name is not a String.
                    }
                }
            }

            return String.Join(",", resourcesToEmbed.Select(e => string.Format("'{0}': {1}", e.Key, JavaScriptEncoder.Default.Encode(e.Value))));
        }
    }
}