using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CDT.Cosmos.Cms.Common.Controllers;
using CDT.Cosmos.Cms.Common.Data;
using CDT.Cosmos.Cms.Common.Data.Logic;
using CDT.Cosmos.Cms.Common.Models;
using CDT.Cosmos.Cms.Common.Services;
using CDT.Cosmos.Cms.Models;
using CDT.Cosmos.Cms.Services;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace CDT.Cosmos.Cms.Controllers
{
    [Authorize(Roles = "Reviewers, Administrators, Editors, Authors, Team Members")]
    public class EditorController : BaseController
    {
        private readonly IOptions<AkamaiContextConfig> _akamaiConfig;
        private readonly IOptions<AzureCdnConfig> _azureCdnConfig;
        private readonly string _blobEndpointUrl;
        private readonly ILogger _logger;

        public EditorController(ILogger<EditorController> logger,
            ApplicationDbContext dbContext,
            UserManager<IdentityUser> userManager,
            IOptions<SiteCustomizationsConfig> options,
            IDistributedCache distributedCache,
            IOptions<AzureCdnConfig> azureCdnService,
            IOptions<AkamaiContextConfig> akamaiService,
            ArticleLogic articleLogic,
            IOptions<RedisContextConfig> redisOptions,
            IOptions<AzureBlobServiceConfig> blobConfig
        ) :
            base(options,
                dbContext,
                logger,
                userManager,
                articleLogic,
                distributedCache,
                redisOptions)
        {
            _logger = logger;
            _azureCdnConfig = azureCdnService;
            _akamaiConfig = akamaiService;
            _blobEndpointUrl = blobConfig.Value.BlobServicePublicUrl.TrimEnd('/') + "/pub/";
            //_blobConfig = blobConfig;
        }

        public IActionResult Index()
        {
            if (SiteOptions.Value.ReadWriteMode) return View();

            return Unauthorized();
        }

        /// <summary>
        ///     Creates a <see cref="CreatePageViewModel" /> used to create a new article.
        /// </summary>
        /// <returns></returns>
        [Authorize(Roles = "Administrators, Editors, Authors, Team Members")]
        public async Task<IActionResult> Create()
        {
            if (SiteOptions.Value.ReadWriteMode)
                try
                {
                    if (User.IsInRole("Team Members"))
                    {
                        var identityUser = await UserManager.GetUserAsync(User);

                        var teams = await DbContext
                            .Teams
                            .Where(a => a.Members.Any(x => x.UserId == identityUser.Id))
                            .ToListAsync();

                        ViewData["Teams"] = teams.Select(c => new TeamViewModel
                        {
                            Id = c.Id,
                            TeamDescription = c.TeamDescription,
                            TeamName = c.TeamName
                        }).ToList();

                        return View(new CreatePageViewModel
                        {
                            Id = 0,
                            Title = string.Empty,
                            TeamId = teams.FirstOrDefault()?.Id,
                            Templates = await DbContext.Templates.Select(s =>
                                new SelectListItem
                                {
                                    Value = s.Id.ToString(),
                                    Text = s.Title
                                }).ToListAsync()
                        });
                    }

                    ViewData["Teams"] = null;
                    return View(new CreatePageViewModel
                    {
                        Id = 0,
                        Title = string.Empty,
                        Templates = await DbContext.Templates.Select(s =>
                            new SelectListItem
                            {
                                Value = s.Id.ToString(),
                                Text = s.Title
                            }).ToListAsync()
                    });
                }
                catch (Exception e)
                {
                    Logger.LogError(e.Message, e);
                    throw;
                }

            return Unauthorized();
        }

        /// <summary>
        ///     Uses <see cref="ArticleLogic.Create(string, int?)" /> to create an <see cref="ArticleViewModel" /> that is saved to
        ///     the database with <see cref="ArticleLogic.UpdateOrInsert" /> ready for editing.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [Authorize(Roles = "Administrators, Editors, Authors, Team Members")]
        [HttpPost]
        public async Task<IActionResult> Create(CreatePageViewModel model)
        {
            if (SiteOptions.Value.ReadWriteMode)
                try
                {
                    if (model == null) return NotFound();

                    if (User.IsInRole("Team Members") && model.TeamId == null)
                    {
                        ModelState.AddModelError("TeamId", "Choose a team for this page.");

                        var identityUser = await UserManager.GetUserAsync(User);

                        var teams = await DbContext
                            .Teams
                            .Where(a => a.Members.Any(x => x.UserId == identityUser.Id))
                            .ToListAsync();

                        ViewData["Teams"] = teams.Select(c => new TeamViewModel
                        {
                            Id = c.Id,
                            TeamDescription = c.TeamDescription,
                            TeamName = c.TeamName
                        }).ToList();
                    }

                    if (await DbContext.Articles.AnyAsync(a =>
                        a.StatusCode != 2
                        && a.Title.Trim().ToLower() == model.Title.Trim().ToLower()))
                        ModelState.AddModelError("Title", $"Title {model.Title} is already taken.");

                    // Check for conflict with blob storage root path.
                    var blobRootPath = "pub";

                    if (!string.IsNullOrEmpty(blobRootPath))
                        if (model.Title.ToLower() == blobRootPath.ToLower())
                            ModelState.AddModelError("Title",
                                $"Title {model.Title} conflicts with the file folder \"{blobRootPath}/\".");

                    if (!ModelState.IsValid)
                    {
                        model.Templates = await DbContext.Templates.Select(s =>
                            new SelectListItem
                            {
                                Value = s.Id.ToString(),
                                Text = s.Title
                            }).ToListAsync();

                        return View(model);
                    }

                    var article = await ArticleLogic.Create(model.Title, model.TemplateId);
                    var savedArticle =
                        await ArticleLogic.UpdateOrInsert(article, UserManager.GetUserId(User), model.TeamId);
                    return RedirectToAction("Edit", new { savedArticle.Id });
                }
                catch (Exception e)
                {
                    Logger.LogError(e.Message, e);
                    throw;
                }

            return Unauthorized();
        }

        /// <summary>
        ///     Creates a new version for an article and redirects to editor.
        /// </summary>
        /// <param name="id">Article ID</param>
        /// <param name="entityId">Entity Id to use as new version</param>
        /// <returns></returns>
        [Authorize(Roles = "Administrators, Editors, Authors")]
        public async Task<IActionResult> CreateVersion(int id, int? entityId = null)
        {
            if (SiteOptions.Value.ReadWriteMode)
                try
                {
                    IQueryable<Article> query;
                    if (entityId == null)
                    {
                        //
                        // Create a new version based on the latest version
                        //
                        var maxVersion = await DbContext.Articles.Where(a => a.ArticleNumber == id)
                            .MaxAsync(m => m.VersionNumber);
                        query = DbContext.Articles.Where(f =>
                            f.ArticleNumber == id &&
                            f.VersionNumber == maxVersion);
                    }
                    else
                    {
                        //
                        // Create a new version based on a specific version
                        //
                        query = DbContext.Articles.Where(f =>
                            f.Id == entityId.Value);
                    }

                    var article = await query.FirstOrDefaultAsync();
                    var defaultLayout = await ArticleLogic.GetDefaultLayout("en-US");
                    var model = new ArticleViewModel
                    {
                        Id = article.Id, // This is the article we are going to clone as a new version.
                        StatusCode = StatusCodeEnum.Active,
                        ArticleNumber = article.ArticleNumber,
                        UrlPath = article.UrlPath,
                        VersionNumber = 0,
                        Published = null,
                        Title = article.Title,
                        Content = article.Content,
                        Updated = DateTime.UtcNow,
                        HeaderJavaScript = article.HeaderJavaScript,
                        FooterJavaScript = article.FooterJavaScript,
                        Layout = defaultLayout,
                        ReadWriteMode = false,
                        PreviewMode = false,
                        EditModeOn = false,
                        CacheKey = null,
                        CacheDuration = 0
                    };

                    var userId = UserManager.GetUserId(User);
                    var result = await ArticleLogic.UpdateOrInsert(model, userId);

                    return RedirectToAction("Edit", "Editor", new { result.Id });
                }
                catch (Exception e)
                {
                    Logger.LogError(e.Message, e);
                    throw;
                }

            return Unauthorized();
        }

        [Authorize(Roles = "Administrators, Editors, Authors, Team Members")]
        public IActionResult MonacoEditor()
        {
            return View();
        }

        /// <summary>
        ///     Creates a <see cref="CreatePageViewModel" /> used to create a new article.
        /// </summary>
        /// <returns></returns>
        [Authorize(Roles = "Administrators, Editors")]
        public async Task<IActionResult> NewHome(int id)
        {
            if (SiteOptions.Value.ReadWriteMode)
                try
                {
                    var page = await ArticleLogic.Get(id, EnumControllerName.Edit);
                    return View(new NewHomeViewModel
                    {
                        Id = page.Id,
                        ArticleNumber = page.ArticleNumber,
                        Title = page.Title,
                        IsNewHomePage = false,
                        UrlPath = page.UrlPath
                    });
                }
                catch (Exception e)
                {
                    Logger.LogError(e.Message, e);
                    throw;
                }

            return Unauthorized();
        }

        [HttpPost]
        [Authorize(Roles = "Administrators, Editors")]
        public async Task<IActionResult> NewHome(NewHomeViewModel model)
        {
            if (SiteOptions.Value.ReadWriteMode)
            {
                if (model == null) return NotFound();
                try
                {
                    await ArticleLogic.NewHomePage(model.Id, UserManager.GetUserId(User));
                    return RedirectToAction("Index");
                }
                catch (Exception e)
                {
                    Logger.LogError(e.Message, e);
                    throw;
                }
            }

            return Unauthorized();
        }

        [Authorize(Roles = "Administrators, Editors, Authors, Team Members")]
        public IActionResult Trash()
        {
            if (SiteOptions.Value.ReadWriteMode) return View();

            return Unauthorized();
        }

        [Authorize(Roles = "Administrators, Editors")]
        public async Task<IActionResult> Publish()
        {
            try
            {
                ViewData["EditModeOn"] = false;
                ViewData["MenuContent"] = ArticleLogic.BuildMenu("en-US").Result;

                if (DistributedCache == null)
                {
                    ViewData["RedisStatus"] = "REDIS is not connected.";
                }
                else
                {
                    var key = Guid.NewGuid().ToString();
                    await DistributedCache.SetAsync(key, Encoding.ASCII.GetBytes("Hello World"),
                        new DistributedCacheEntryOptions
                        {
                            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1)
                        });
                    var result = Encoding.ASCII.GetString(await DistributedCache.GetAsync(key));
                    if (result == "Hello World") ViewData["RedisStatus"] = "REDIS is OK";
                }
            }
            catch (Exception e)
            {
                ViewData["RedisStatus"] = e.Message;
            }

            var layout = await ArticleLogic.GetDefaultLayout("en-US");
            ViewData["AkamaiStatus"] = "Ready";
            var model = await ArticleLogic.Create("Layout Preview");
            model.Layout = layout;
            model.EditModeOn = false;
            model.ReadWriteMode = false;
            model.PreviewMode = true;
            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Administrators, Editors")]
        public async Task<JsonResult> UpdateTimeStamps()
        {
            try
            {
                var result = await ArticleLogic.UpdateDateTimeStamps();
                return Json(result);
            }
            catch (Exception e)
            {
                Logger.LogError(e.Message, e);
                throw;
            }
        }

        [HttpPost]
        [Authorize(Roles = "Administrators, Editors")]
        public async Task<JsonResult> FlushRedis()
        {
            try
            {
                var model = await ArticleLogic.FlushRedis("");
                return Json(model);
            }
            catch (Exception e)
            {
                Logger.LogError(e.Message, e);
                throw;
            }
        }

        [HttpPost]
        [Authorize(Roles = "Administrators, Editors")]
        public async Task<JsonResult> FlushCdn()
        {
            try
            {
                CdnPurgeViewModel model = new CdnPurgeViewModel()
                {
                    Detail = "Test CDN",
                    EstimatedSeconds = 600,
                    HttpStatus = "OK",
                    PurgeId = "",
                    SupportId = ""
                };

                if (_azureCdnConfig != null && !string.IsNullOrEmpty(_azureCdnConfig.Value.CdnProfileName))
                {
                    var cdnManager = new AzureCdnService(_azureCdnConfig, DbContext, ArticleLogic);
                    model = await cdnManager.Purge("/*");
                }
                else if (_akamaiConfig != null && !string.IsNullOrEmpty(_akamaiConfig.Value.AccessToken))
                {
                    var cdnManager = new AkamaiService(_akamaiConfig);
                    var data = cdnManager.PurgeCdnByCpCode();
                    model = JsonConvert.DeserializeObject<CdnPurgeViewModel>(data);
                    model.Detail = "Akamai Premium";
                }

                return Json(model);
            }
            catch (Exception e)
            {
                Logger.LogError(e.Message, e);
                throw;
            }
        }

        /// <summary>
        ///     Gets all the versions for an article
        /// </summary>
        /// <param name="id">Article number</param>
        /// <returns></returns>
        public async Task<IActionResult> Versions(int? id)
        {
            if (SiteOptions.Value.ReadWriteMode)
            {
                ViewData["EditModeOn"] = false;
                ViewData["MenuContent"] = ArticleLogic.BuildMenu("en-US").Result;
                var title = await DbContext.Articles.Where(a => a.ArticleNumber == id.Value)
                    .Select(s => s.Title).FirstOrDefaultAsync();
                ViewData["ArticleTitle"] = title;

                if (id == null)
                    return RedirectToAction("Index");

                ViewData["ArticleId"] = id.Value;

                return View();
            }

            return Unauthorized();
        }

        [Authorize(Roles = "Administrators, Editors")]
        public async Task<IActionResult> Logs()
        {
            var layout = await ArticleLogic.GetDefaultLayout("en-US");
            var model = await ArticleLogic.Create("Layout Preview");
            model.Layout = layout;
            model.EditModeOn = false;
            model.ReadWriteMode = false;
            model.PreviewMode = true;
            return View(model);
        }

        [Authorize(Roles = "Administrators, Editors")]
        public async Task<IActionResult> Read_Logs([DataSourceRequest] DataSourceRequest request)
        {
            var data = await DbContext.ArticleLogs
                .OrderByDescending(o => o.DateTimeStamp)
                .Include(i => i.IdentityUser)
                .Include(b => b.Article)
                .Select(s => new
                {
                    s.Id,
                    s.ActivityNotes,
                    s.DateTimeStamp,
                    s.Article.Title,
                    s.IdentityUser.Email
                }).ToListAsync();
            var result = await data.Select(s => new ArticleLogJsonModel
            {
                Id = s.Id,
                ActivityNotes = s.ActivityNotes,
                DateTimeStamp = DateTime.SpecifyKind(s.DateTimeStamp, DateTimeKind.Utc),
                Title = s.Title,
                Email = s.Email
            }).ToDataSourceResultAsync(request);
            return Json(result);
        }

        #region EDIT ARTICLE FUNCTIONS

        [Authorize(Roles = "Administrators, Editors, Authors, Team Members")]
        public async Task<IActionResult> Edit(int id)
        {
            ViewData["BlobEndpointUrl"] = _blobEndpointUrl;
            //ViewData["Layouts"] = await GetLayoutListItems();
            //
            // Validate team member access.
            //

            return await Article_Get(id, EnumControllerName.Edit);
        }


        /// <summary>
        ///     Saves an article via HTTP POST.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <remarks>
        /// </remarks>
        [HttpPost]
        [Authorize(Roles = "Administrators, Editors, Authors, Team Members")]
        public async Task<IActionResult> SaveHtml(ArticleViewModel model)
        {
            if (!SiteOptions.Value.ReadWriteMode) return Unauthorized();

            if (model == null) return NotFound();

            try
            {
                // Validate security for authors before going further
                if (User.IsInRole("Team Members"))
                {
                    var user = await UserManager.GetUserAsync(User);
                    var teamMember = await DbContext.TeamMembers
                        .Where(t => t.UserId == user.Id &&
                                    t.Team.Articles.Any(a => a.Id == model.Id))
                        .FirstOrDefaultAsync();

                    if (teamMember == null ||
                        model.Published.HasValue && teamMember.TeamRole != (int)TeamRoleEnum.Editor)
                        return Unauthorized();
                }
                else
                {
                    if (model.Published.HasValue && User.IsInRole("Authors"))
                        return Unauthorized();
                }

                //
                // Save as an article or a template
                //
                model = await SaveArticleChanges(model, EnumControllerName.Edit);

                var errors = ModelState.Values
                    .Where(w => w.ValidationState == ModelValidationState.Invalid)
                    .ToList();
                //var t = errors.FirstOrDefault();


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

            //return await Article_AjaxPost(model, EnumControllerName.Edit);
        }

        [Authorize(Roles = "Administrators, Editors, Authors, Team Members")]
        public async Task<IActionResult> EditCode(int id)
        {
            try
            {
                if (SiteOptions.Value.ReadWriteMode)
                {
                    var article = await ArticleLogic.Get(id, EnumControllerName.Edit);
                    if (article == null) return NotFound();

                    // Validate security for authors before going further
                    if (User.IsInRole("Team Members"))
                    {
                        var user = await UserManager.GetUserAsync(User);
                        var teamMember = await DbContext.TeamMembers
                            .Where(t => t.UserId == user.Id &&
                                        t.Team.Articles.Any(a => a.Id == id))
                            .FirstOrDefaultAsync();

                        if (teamMember == null || article.Published.HasValue &&
                            teamMember.TeamRole != (int)TeamRoleEnum.Editor)
                            return Unauthorized();
                    }
                    else
                    {
                        if (article.Published.HasValue && User.IsInRole("Authors"))
                            return Unauthorized();
                    }

                    ViewData["Version"] = article.VersionNumber;

                    return View(new EditCodePostModel
                    {
                        Id = article.Id,
                        ArticleNumber = article.ArticleNumber,
                        EditorTitle = article.Title,
                        EditorFields = new[]
                        {
                            new EditorField
                            {
                                FieldId = "HeaderJavaScript",
                                FieldName = "Header JavaScript",
                                EditorMode = EditorMode.JavaScript,
                                IconUrl = "/images/seti-ui/icons/javascript.svg"
                            },
                            new EditorField
                            {
                                FieldId = "Content",
                                FieldName = "Html Content",
                                EditorMode = EditorMode.Html,
                                IconUrl = "~/images/seti-ui/icons/html.svg"
                            },
                            new EditorField
                            {
                                FieldId = "FooterJavaScript",
                                FieldName = "Footer JavaScript",
                                EditorMode = EditorMode.JavaScript,
                                IconUrl = "~/images/seti-ui/icons/javascript.svg"
                            }
                        },
                        HeaderJavaScript = article.HeaderJavaScript,
                        FooterJavaScript = article.FooterJavaScript,
                        Content = article.Content,
                        EditingField = "HeaderJavaScript",
                        CustomButtons = new[] { "Preview", "Html" }
                    });
                }

                return Unauthorized();
            }
            catch (Exception e)
            {
                Logger.LogError(e.Message, e);
                throw;
            }
        }

        /// <summary>
        ///     Saves the code and html of the page.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <remarks>
        ///     This method saves page code to the database.  <see cref="EditCodePostModel.Content" /> is validated using method
        ///     <see cref="BaseController.ValidateHtml" />.
        ///     HTML formatting errors that could not be automatically fixed are logged with
        ///     <see cref="ControllerBase.ModelState" /> and
        ///     the code is not saved in the database.
        /// </remarks>
        /// <exception cref="NotFoundResult"></exception>
        /// <exception cref="UnauthorizedResult"></exception>
        [HttpPost]
        [Authorize(Roles = "Administrators, Editors, Authors, Team Members")]
        public async Task<IActionResult> EditCode(EditCodePostModel model)
        {
            try
            {
                if (SiteOptions.Value.ReadWriteMode && ModelState.IsValid)
                {
                    if (model == null) return NotFound();

                    var article = await DbContext.Articles.FirstOrDefaultAsync(f => f.Id == model.Id);

                    if (article == null) return NotFound();

                    // Validate security for authors before going further
                    if (User.IsInRole("Team Members"))
                    {
                        var user = await UserManager.GetUserAsync(User);
                        var teamMember = await DbContext.TeamMembers
                            .Where(t => t.UserId == user.Id &&
                                        t.Team.Articles.Any(a => a.Id == article.Id))
                            .FirstOrDefaultAsync();

                        if (teamMember == null || article.Published.HasValue &&
                            teamMember.TeamRole != (int)TeamRoleEnum.Editor)
                            return Unauthorized();
                    }
                    else
                    {
                        if (article.Published.HasValue && User.IsInRole("Authors"))
                            return Unauthorized();
                    }

                    // Validate HTML
                    model.Content = ValidateHtml("Content", model.Content, ModelState);

                    if (ModelState.IsValid) article.Content = model.Content;

                    if (string.IsNullOrEmpty(model.HeaderJavaScript) ||
                        string.IsNullOrWhiteSpace(model.HeaderJavaScript))
                        article.HeaderJavaScript = string.Empty;
                    else
                        article.HeaderJavaScript = model.HeaderJavaScript.Trim();

                    if (string.IsNullOrEmpty(model.FooterJavaScript) ||
                        string.IsNullOrWhiteSpace(model.FooterJavaScript))
                        article.FooterJavaScript = string.Empty;
                    else
                        article.FooterJavaScript = model.FooterJavaScript.Trim();

                    // Check for validation errors...
                    if (ModelState.IsValid)
                        // If no HTML errors were thrown, save here.
                        try
                        {
                            await DbContext.SaveChangesAsync();
                            //
                            // Pull back out of the database, so user can see exactly what was saved.
                            //
                            article = await DbContext.Articles.FirstOrDefaultAsync(f => f.Id == model.Id);
                            if (article == null) throw new Exception("Could not retrieve saved code!");
                        }
                        catch (Exception e)
                        {
                            var provider = new EmptyModelMetadataProvider();
                            ModelState.AddModelError("Save", e, provider.GetMetadataForType(typeof(string)));
                        }


                    ViewData["Version"] = article.VersionNumber;

                    return View(new EditCodePostModel
                    {
                        Id = article.Id,
                        ArticleNumber = article.ArticleNumber,
                        EditorTitle = article.Title,
                        EditorFields = new[]
                        {
                            new EditorField
                            {
                                FieldId = "HeaderJavaScript",
                                FieldName = "Header JavaScript",
                                EditorMode = EditorMode.JavaScript
                            },
                            new EditorField
                            {
                                FieldId = "Content",
                                FieldName = "Html Content",
                                EditorMode = EditorMode.Html
                            },
                            new EditorField
                            {
                                FieldId = "FooterJavaScript",
                                FieldName = "Footer JavaScript",
                                EditorMode = EditorMode.JavaScript
                            }
                        },
                        EditorMode = EditorMode.JavaScript,
                        HeaderJavaScript = article.HeaderJavaScript,
                        FooterJavaScript = article.FooterJavaScript,
                        Content = article.Content,
                        EditingField = model.EditingField,
                        CustomButtons = new[] { "Preview", "Html" }
                    });
                }

                return Unauthorized();
            }
            catch (Exception e)
            {
                Logger.LogError(e.Message, e);
                throw;
            }
        }

        #endregion

        #region Data Services

        [HttpGet]
        [HttpPost]
        public async Task<IActionResult> CheckTitle(int articleNumber, string title)
        {
            if (SiteOptions.Value.ReadWriteMode)
                try
                {
                    var result = await ArticleLogic.ValidateTitle(title, articleNumber);

                    if (result) return Json(true);

                    return Json($"Email {title} is already taken.");
                }
                catch (Exception e)
                {
                    Logger.LogError(e.Message, e);
                    throw;
                }

            return Unauthorized();
        }

        /// <summary>
        ///     Get list of articles
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <remarks>
        ///     Note: This method cannot retrieve articles that are in the trash.
        /// </remarks>
        public async Task<IActionResult> Get_Articles([DataSourceRequest] DataSourceRequest request)
        {
            if (SiteOptions.Value.ReadWriteMode)
                try
                {
                    List<ArticleListItem> list;
                    if (User.IsInRole("Team Members"))
                    {
                        var identityUser = await UserManager.GetUserAsync(User);
                        ViewData["TeamsLookup"] = await DbContext.Teams
                            .Where(a => a.Members.Any(a => a.UserId == identityUser.Id))
                            .Select(s => new TeamViewModel
                            {
                                Id = s.Id,
                                TeamDescription = s.TeamDescription,
                                TeamName = s.TeamName
                            }).OrderBy(o => o.TeamName)
                            .ToListAsync();

                        var userId = await UserManager.GetUserIdAsync(await UserManager.GetUserAsync(User));

                        var query = DbContext.Articles
                            .Include(i => i.Team)
                            .Where(w => w.Team.Members.Any(a => a.UserId == userId));

                        list = await ArticleLogic.GetArticleList(query);
                    }
                    else
                    {
                        list = await ArticleLogic.GetArticleList();

                        ViewData["TeamsLookup"] = null;
                    }

                    return Json(await list.ToDataSourceResultAsync(request));
                }
                catch (Exception e)
                {
                    Logger.LogError(e.Message, e);
                    throw;
                }

            return Unauthorized();
        }

        [HttpPost]
        public async Task<IActionResult> Trash_Article([DataSourceRequest] DataSourceRequest request,
            ArticleListItem model)
        {
            if (model != null) await ArticleLogic.TrashArticle(model.ArticleNumber);
            return Json(await new[] { model }.ToDataSourceResultAsync(request, ModelState));
        }

        /// <summary>
        ///     Get list of articles that are in the trash bin.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<IActionResult> Get_TrashedArticles([DataSourceRequest] DataSourceRequest request)
        {
            if (SiteOptions.Value.ReadWriteMode)
                try
                {
                    var list = await ArticleLogic.GetArticleTrashList();
                    return Json(await list.ToDataSourceResultAsync(request));
                }
                catch (Exception e)
                {
                    Logger.LogError(e.Message, e);
                    throw;
                }

            return Unauthorized();
        }

        public async Task<IActionResult> Get_Versions([DataSourceRequest] DataSourceRequest request, int id)
        {
            if (SiteOptions.Value.ReadWriteMode)
                try
                {
                    var data = await DbContext.Articles.OrderByDescending(o => o.VersionNumber)
                        .Where(a => a.ArticleNumber == id).Select(s => new 
                        {
                            s.Id,
                            s.Published,
                            s.Title,
                            s.Updated,
                            s.VersionNumber
                        }).ToListAsync();

                    var model = data.Select(x =>
                        new ArticleVersionInfo
                        {
                            Id = x.Id,
                            VersionNumber = x.VersionNumber,
                            Title = x.Title,
                            Updated = DateTime.SpecifyKind(x.Updated, DateTimeKind.Utc),
                            Published = x.Published.HasValue ? DateTime.SpecifyKind(x.Published.Value, DateTimeKind.Utc) : (DateTime?)null
                        });
                    return Json(await model.ToDataSourceResultAsync(request));
                }
                catch (Exception e)
                {
                    Logger.LogError(e.Message, e);
                    throw;
                }

            return Unauthorized();
        }

        #endregion
    }
}