using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using CDT.Cosmos.Cms.Common.Controllers;
using CDT.Cosmos.Cms.Common.Data;
using CDT.Cosmos.Cms.Common.Data.Logic;
using CDT.Cosmos.Cms.Common.Models;
using CDT.Cosmos.Cms.Common.Services;
using CDT.Cosmos.Cms.Models;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.CodeAnalysis.Differencing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CDT.Cosmos.Cms.Controllers
{
    public abstract class BaseController : Controller
    {
        internal IDistributedCache DistributedCache;
        internal IOptions<RedisContextConfig> RedisOptions;

        internal BaseController(IOptions<SiteCustomizationsConfig> options, ApplicationDbContext dbContext,
            ILogger logger, UserManager<IdentityUser> userManager, ArticleLogic articleLogic,
            IDistributedCache distributedCache = null,
            IOptions<RedisContextConfig> redisOptions = null)
        {
            SiteOptions = options;
            DbContext = dbContext;
            ArticleLogic = articleLogic;
            Logger = logger;
            UserManager = userManager;
            DistributedCache = distributedCache;
            RedisOptions = redisOptions;
        }

        internal ArticleLogic ArticleLogic { get; }
        internal IOptions<SiteCustomizationsConfig> SiteOptions { get; }
        internal ApplicationDbContext DbContext { get; }
        internal ILogger Logger { get; }
        internal UserManager<IdentityUser> UserManager { get; }

        /// <summary>
        ///     Gets an article or template to edit, or creates a new <see cref="Article" /> if id is null.
        /// </summary>
        /// <param name="id">Article or Template ID</param>
        /// <param name="controllerName"><see cref="EnumControllerName" />Controller Name</param>
        /// <param name="editMode"></param>
        /// <param name="defaultView"></param>
        /// <returns>
        ///     <para>
        ///         Using method <see cref="Common.Data.Logic.ArticleLogic.Get(int?, EnumControllerName)" /> to return an
        ///         instance of <see cref="ViewResult" /> where the model is of type <see cref="ArticleViewModel" />. Additionally:
        ///     </para>
        ///     <list type="bullet">
        ///         <item>
        ///             ViewData["FontIcons"] set with CA theme font icon list.
        ///         </item>
        ///         <item>
        ///             ViewData["PageUrls"] set as a list of type <see cref="SelectListItem" />
        ///         </item>
        ///         <item>
        ///             ViewData["WireFrames"] set as a list of type <see cref="SelectListItem" />
        ///         </item>
        ///         <item>
        ///             Sets <see cref="ArticleViewModel.EditModeOn" /> to true.
        ///         </item>
        ///     </list>
        /// </returns>
        /// <remarks>
        ///     Exceptions are logged with <see cref="Logger" />.
        /// </remarks>
        /// <exception cref="ControllerBase.Unauthorized()"></exception>
        internal async Task<IActionResult> Article_Get(int? id, EnumControllerName controllerName, bool editMode = true,
            bool defaultView = false)
        {
            if (SiteOptions.Value.ReadWriteMode)
                try
                {
                    var iconModel = await DbContext.FontIcons.OrderBy(o => o.IconCode).Select(s =>
                        new SelectListItem
                        {
                            Text = s.IconCode,
                            Value = s.IconCode
                        }).ToListAsync();
                    ViewData["FontIcons"] = iconModel;

                    var pageModel = (await ArticleLogic.GetArticleList()).OrderBy(o => o.Title).Select(s =>
                        new SelectListItem
                        {
                            Text = s.Title,
                            Value = "/" + s.UrlPath
                        }).ToList();
                    ViewData["PageUrls"] = pageModel;

                    //
                    // Determine if we are getting an article, or a template.
                    //
                    var model = await ArticleLogic.Get(id, controllerName);

                    // Override defaults
                    model.EditModeOn = editMode;

                    // Validate security for authors before going further
                    if (User.IsInRole("Team Members"))
                    {
                        var user = await UserManager.GetUserAsync(User);
                        var teamMember = await DbContext.TeamMembers
                            .Where(t =>
                                t.UserId == user.Id &&
                                t.Team.Articles.Any(a => a.Id == id))
                            .FirstOrDefaultAsync();
                        if (teamMember == null ||
                            model.Published.HasValue && teamMember.TeamRole != (int) TeamRoleEnum.Editor)
                            return Unauthorized();
                    }
                    else
                    {
                        if (model.Published.HasValue && User.IsInRole("Authors"))
                            return Unauthorized();
                    }

                    if (defaultView) return View(model);

                    return View("~/Views/Home/Index_standard1.cshtml", model);
                }
                catch (Exception e)
                {
                    Logger.LogError(e.Message, e);
                    throw;
                }

            return Unauthorized();
        }

        /// <summary>
        ///     Method saves an edit session for an <see cref="Article" /> or <see cref="Template" />.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="controllerName"></param>
        /// <param name="blobStorageRoot"></param>
        /// <returns></returns>
        /// <remarks>
        ///     <para>
        ///         This method is used by <see cref="EditorController.Edit(ArticleViewModel)" /> and <see cref="Edit{TNode}" />
        ///         methods.
        ///     </para>
        ///     <para>
        ///         Data is saved using method <see cref="SaveArticleChanges" /> and the model is reloaded
        ///         using <see cref="Common.Data.Logic.ArticleLogic.Get(int, EnumControllerName)" />.
        ///     </para>
        /// </remarks>
        /// <exception cref="ControllerBase.Unauthorized()"></exception>
        /// <exception cref="ControllerBase.NotFound()"></exception>
        [HttpPost]
        internal async Task<IActionResult> Article_Post(ArticleViewModel model, EnumControllerName controllerName)
        {
            if (!SiteOptions.Value.ReadWriteMode) return Unauthorized();

            if (model == null) return NotFound();

            try
            {
                if (model.ArticleNumber > 0)
                {
                    var article = await DbContext.Articles.FirstOrDefaultAsync(f => f.Id == model.Id);
                    var user = await UserManager.GetUserAsync(User);

                    if (await UserManager.IsInRoleAsync(user, "Authors") && article.Published.HasValue)
                        return Unauthorized();
                }

                //
                // Save as an article or as a template
                //
                await SaveArticleChanges(model, controllerName);

                //
                // Reload the saved model.
                //
                model = await ArticleLogic.Get(model.Id, controllerName);
                model.EditModeOn = true;

                var iconModel = await DbContext.FontIcons.OrderBy(o => o.IconCode).Select(s =>
                    new SelectListItem
                    {
                        Text = s.IconCode,
                        Value = s.IconCode
                    }).ToListAsync();
                ViewData["FontIcons"] = iconModel;

                var pageModel = (await ArticleLogic.GetArticleList()).OrderBy(o => o.Title).Select(s =>
                    new SelectListItem
                    {
                        Text = s.Title,
                        Value = "/" + s.UrlPath
                    }).ToList();
                ViewData["PageUrls"] = pageModel;

                return View("~/Views/Home/Index_standard1.cshtml", model);
            }
            catch (Exception e)
            {
                Logger.LogError(e.Message, e);
                throw;
            }
        }

        /// <summary>
        ///     Handles AJAX post of either an <see cref="Article" /> or a <see cref="Template" />.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="controllerName"></param>
        /// <param name="blobStorageRoot"></param>
        /// <returns>
        ///     Returns <see cref="JsonResult" /> with model being of type <see cref="SaveResultJsonModel" />.
        /// </returns>
        /// <remarks>
        ///     <para>
        ///         This method is used by <see cref="EditorController.SaveHtml(ArticleViewModel)" /> and
        ///         <see cref="Article_AjaxPost" /> methods.
        ///     </para>
        ///     <para>
        ///         Content is saved to the database using <see cref="SaveArticleChanges" />.
        ///     </para>
        ///     <para>
        ///         Errors are recorded using <see cref="Logger" /> and with <see cref="ModelStateDictionary" />.
        ///     </para>
        /// </remarks>
        /// <exception cref="ControllerBase.Unauthorized()"> is returned if not in edit mode.</exception>
        /// <exception cref="ControllerBase.NotFound()"> is returned if post model is null.</exception>
        [HttpPost]
        internal async Task<IActionResult> Article_AjaxPost(ArticleViewModel model, EnumControllerName controllerName)
        {
            if (!SiteOptions.Value.ReadWriteMode) return Unauthorized();

            if (model == null) return NotFound();

            try
            {
                //
                // Save as an article or a template
                //
                model = await SaveArticleChanges(model, controllerName);

                var errors = ModelState.Values
                    .Where(w => w.ValidationState == ModelValidationState.Invalid)
                    .ToList();
                var t = errors.FirstOrDefault();


                var data = new SaveResultJsonModel
                {
                    IsValid = ModelState.IsValid,
                    ErrorCount = ModelState.ErrorCount,
                    HasReachedMaxErrors = ModelState.HasReachedMaxErrors,
                    ValidationState = ModelState.ValidationState,
                    Model = model,
                    Errors = errors
                };
                return Json(data);
            }
            catch (Exception e)
            {
                Logger.LogError(e.Message, e);
                throw;
            }
        }

        /// <summary>
        ///     Server-side validation of HTML.
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="inputHtml"></param>
        /// <param name="modelState"></param>
        /// <returns>HTML content</returns>
        /// <remarks>
        ///     <para>
        ///         The purpose of this method is to validate HTML prior to be saved to the database.
        ///         It uses an instance of <see cref="HtmlAgilityPack.HtmlDocument" /> to check HTML formatting.
        ///     </para>
        ///     <para>Errors are recorded using <see cref="Logger" /> and with <see cref="ModelStateDictionary" />.</para>
        /// </remarks>
        internal string ValidateHtml(string fieldName, string inputHtml, ModelStateDictionary modelState)
        {
            try
            {
                if (!string.IsNullOrEmpty(inputHtml))
                {
                    var contentHtmlDocument = new HtmlDocument();
                    contentHtmlDocument.LoadHtml(HttpUtility.HtmlDecode(inputHtml));
                    if (contentHtmlDocument.ParseErrors.Any())
                        foreach (var error in contentHtmlDocument.ParseErrors)
                            modelState.AddModelError(fieldName, error.Reason);

                    return contentHtmlDocument.ParsedText.Trim();
                }

                return string.Empty;
            }
            catch (Exception e)
            {
                Logger.LogError(e.Message, e);
                throw;
            }
        }

        /// <summary>
        ///     Private, internal method that saves changes to an article.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="controllerName"></param>
        /// <returns></returns>
        /// <remarks>
        ///     <para>If a user is a member of the 'Team Members' role, ensures that user has ability to save article.</para>
        ///     <para>
        ///         This method is used by <see cref="Article_Post" /> and the AJAX posting method
        ///         <see cref="Article_AjaxPost" />.
        ///     </para>
        ///     <para>
        ///         If this is an article (or regular page) content being saved, the method
        ///         <see cref="Common.Data.Logic.ArticleLogic.UpdateOrInsert" /> is used. Saving a template uses method
        ///         <see cref="SaveTemplateChanges" />.
        ///     </para>
        ///     <para>Errors are recorded using <see cref="Logger" /> and with <see cref="ControllerBase.ModelState" />.</para>
        /// </remarks>
        public async Task<ArticleViewModel> SaveArticleChanges(ArticleViewModel model,
            EnumControllerName controllerName)
        {
            if (ModelState.IsValid)
                try
                {
                    model.Title = ValidateHtml("Title", model.Title, ModelState);
                    model.Content = ValidateHtml("Content", model.Content, ModelState);

                    var user = await UserManager.GetUserAsync(User);
                    // Get a new copy of the model
                    if (controllerName == EnumControllerName.Edit)
                    {
                        model = await ArticleLogic.UpdateOrInsert(model, user.Id);
                        // Re-enable editable sections.
                        model.Content = model.Content.Replace(" crx=\"", " contenteditable=\"",
                            StringComparison.CurrentCultureIgnoreCase);
                    }
                    else
                        model = await SaveTemplateChanges(model);

                    if (DistributedCache != null)
                        // Data object cache key
                        await ArticleLogic.FlushRedis("");
                }
                catch (Exception e)
                {
                    Logger.LogError(e, e.Message);
                    ModelState.AddModelError("", e.Message);
                }

            var menu = await ArticleLogic.BuildMenu("en-US");

            ViewData["MenuContent"] = menu;
            ViewData["ReadWriteMode"] = SiteOptions.Value.ReadWriteMode;
            ViewData["PreviewMode"] = false;
            return model;
        }

        /// <summary>
        ///     This private method is used by <see cref="SaveArticleChanges" /> to save a <see cref="Template" />.
        /// </summary>
        /// <param name="model"></param>
        /// <returns>
        ///     <para>Returns an <see cref="ArticleViewModel" /> where:</para>
        ///     <para></para>
        ///     <para>
        ///         * <see cref="ArticleViewModel.ReadWriteMode" /> is set using
        ///         <see cref="IOptions{SiteCustomizationsConfig}" /> injected into <see cref="BaseController" />.
        ///     </para>
        ///     <para>Errors are recorded using <see cref="Logger" /> and with <see cref="ControllerBase.ModelState" />.</para>
        /// </returns>
        private async Task<ArticleViewModel> SaveTemplateChanges(ArticleViewModel model)
        {
            if (ModelState.IsValid)
                try
                {
                    model.Title = ValidateHtml("Title", model.Title, ModelState);
                    model.Content = ValidateHtml("Content", model.Content, ModelState);
                    //model.SubSection1 = ValidateHtml("SubSection1", model.SubSection1, ModelState);

                    //var userId = UserManager.GetUserId(User);
                    // Get a new copy of the model
                    var template = await DbContext.Templates.FindAsync(model.Id) ?? new Template();

                    template.Content = model.Content;
                    template.Title = model.Title;

                    if (template.Id == 0) DbContext.Templates.Add(template);

                    await DbContext.SaveChangesAsync();
                }
                catch (Exception e)
                {
                    Logger.LogError(e, e.Message);
                    ModelState.AddModelError("", e.Message);
                }

            model.ReadWriteMode = SiteOptions.Value.ReadWriteMode;
            return model;
        }

        /// <summary>
        ///     Not all pages use an <see cref="ArticleViewModel" />, so these pages need menu loaded into view data.
        /// </summary>
        /// <returns></returns>
        internal async Task LoadMenuIntoViewData()
        {
            ViewData["MenuContent"] = await ArticleLogic.BuildMenu("en-US");
        }

        internal async Task<List<SelectListItem>> GetLayoutListItems()
        {
            var layouts = await DbContext.Layouts.Select(s => new SelectListItem
            {
                Value = s.Id.ToString(),
                Text = s.LayoutName
            }).ToListAsync();
            if (layouts != null) return layouts;

            var layoutViewModel = new LayoutViewModel();

            DbContext.Layouts.Add(layoutViewModel.GetLayout());
            await DbContext.SaveChangesAsync();

            return await DbContext.Layouts.Select(s => new SelectListItem
            {
                Value = s.Id.ToString(),
                Text = s.LayoutName
            }).ToListAsync();
        }
    }
}