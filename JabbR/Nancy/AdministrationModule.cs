using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JabbR.ContentProviders.Core;
using JabbR.Infrastructure;
using JabbR.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace JabbR.Nancy
{
    [Route("administration")]
    [Authorize(Policy = "AdminPolicy")]
    public class AdministrationController : Controller
    {
        private readonly ApplicationSettings _applicationSettings;
        private readonly ISettingsManager _settingsManager;
        private readonly IEnumerable<IContentProvider> _contentProviders;

        public AdministrationController(ApplicationSettings applicationSettings,
                                        ISettingsManager settingsManager,
                                        IEnumerable<IContentProvider> contentProviders)
        {
            _applicationSettings = applicationSettings;
            _settingsManager = settingsManager;
            _contentProviders = contentProviders;
        }

        [HttpGet]
        public IActionResult Index()
        {
            if (!User.Identity.IsAuthenticated || !User.HasClaim(JabbRClaimTypes.Admin, "true"))
            {
                return Forbid();
            }

            var allContentProviders = _contentProviders
                .OrderBy(provider => provider.GetType().Name)
                .ToList();
            var model = new
            {
                AllContentProviders = allContentProviders,
                ApplicationSettings = _applicationSettings
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index([FromBody] ApplicationSettings settings, [FromBody] ContentProviderSetting[] contentProviders, [FromBody] EnabledContentProvidersResult enabledContentProvidersResult)
        {
            if (!User.Identity.IsAuthenticated || !User.HasClaim(JabbRClaimTypes.Admin, "true"))
            {
                return Forbid();
            }

            try
            {
                // filter out empty/null providers. The values posted may contain 'holes' due to removals.
                settings.ContentProviders = contentProviders
                    .Where(cp => !string.IsNullOrEmpty(cp.Name))
                    .ToList();

                // we posted the enabled ones, but we store the disabled ones. Flip it around...
                settings.DisabledContentProviders =
                    new HashSet<string>(_contentProviders
                        .Select(cp => cp.GetType().Name)
                        .Where(typeName => enabledContentProvidersResult.EnabledContentProviders == null ||
                            !enabledContentProvidersResult.EnabledContentProviders.Contains(typeName))
                        .ToList());

                if (ApplicationSettings.TryValidateSettings(settings, out var errors))
                {
                    _settingsManager.Save(settings);
                }
                else
                {
                    foreach (var error in errors)
                    {
                        ModelState.AddModelError(error.Key, error.Value);
                    }
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("_FORM", ex.Message);
            }

            if (ModelState.IsValid)
            {
                TempData["AlertMessage"] = new { type = "success", message = LanguageResources.SettingsSaveSuccess };
                return RedirectToAction("Index");
            }

            return View(_applicationSettings);
        }
    }

    public class EnabledContentProvidersResult
    {
        public List<string> EnabledContentProviders { get; set; }
    }
}