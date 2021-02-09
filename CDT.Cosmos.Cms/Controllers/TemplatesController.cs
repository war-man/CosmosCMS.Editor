using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CDT.Cosmos.Cms.Common.Controllers;
using CDT.Cosmos.Cms.Common.Data;
using CDT.Cosmos.Cms.Common.Models;
using CDT.Cosmos.Cms.Common.Services;
using CDT.Cosmos.Cms.Models;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CDT.Cosmos.Cms.Controllers
{
    [Authorize(Roles = "Administrators, Editors")]
    public class TemplatesController : BaseController
    {
        public TemplatesController(ILogger<TemplatesController> logger, ApplicationDbContext dbContext,
            IOptions<SiteCustomizationsConfig> options, UserManager<IdentityUser> userManager,
            IDistributedCache distributedCache, IOptions<RedisContextConfig> redisOptions) :
            base(options, dbContext, logger, userManager, distributedCache, redisOptions)
        {
        }

        public async Task<IActionResult> Index()
        {
            if (SiteOptions.Value.ReadWriteMode)
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
                if (SiteOptions.Value.ReadWriteMode)
                {
                    await LoadMenuIntoViewData();
                    ViewData["EditModeOn"] = false; // Used by page views
                    ViewData["Layouts"] = await GetLayoutListItems();

                    return await Article_Get(null, EnumControllerName.Edit, false, true);
                }

                return Unauthorized();
            }

            return Unauthorized();
        }

        public async Task<IActionResult> Create()
        {
            var entity = new Template
            {
                Id = 0,
                Title = "New Template " + await DbContext.Templates.CountAsync(),
                Description = "<p>New template, please add descriptive and helpful information here.</p>",
                Content = "<p>" + LoremIpsum.SubSection1 + "</p>"
            };
            DbContext.Templates.Add(entity);
            await DbContext.SaveChangesAsync();
            return RedirectToAction("EditCode", "Templates", new {entity.Id});
        }

        public async Task<IActionResult> EditCode(int id)
        {
            if (SiteOptions.Value.ReadWriteMode)
            {
                var entity = await DbContext.Templates.FindAsync(id);

                var model = new TemplateCodeEditorViewModel
                {
                    Id = entity.Id,
                    EditorTitle = entity.Title,
                    EditorFields = new List<EditorField>
                    {
                        new EditorField
                        {
                            EditorMode = EditorMode.Html,
                            FieldName = "Html Content",
                            FieldId = "Content",
                            IconUrl = "~/images/seti-ui/icons/html.svg"
                        }
                    },
                    EditingField = "Content",
                    Content = entity.Content,
                    CustomButtons = new List<string>
                    {
                        "Preview"
                    }
                };
                return View(model);
            }

            return Unauthorized();
        }

        [HttpPost]
        public async Task<IActionResult> EditCode(TemplateCodeEditorViewModel model)
        {
            if (SiteOptions.Value.ReadWriteMode)
            {
                var entity = await DbContext.Templates.FindAsync(model.Id);

                entity.Content = model.Content;

                await DbContext.SaveChangesAsync();

                model = new TemplateCodeEditorViewModel
                {
                    Id = entity.Id,
                    EditorTitle = entity.Title,
                    EditorFields = new List<EditorField>
                    {
                        new EditorField
                        {
                            EditorMode = EditorMode.Html,
                            FieldName = "Html Content",
                            FieldId = "Content",
                            IconUrl = "~/images/seti-ui/icons/html.svg"
                        }
                    },
                    EditingField = "Content",
                    Content = entity.Content,
                    CustomButtons = new List<string>
                    {
                        "Preview"
                    }
                };
                return View(model);
            }

            return Unauthorized();
        }

        public async Task<IActionResult> Preview(int id)
        {
            var template = await DbContext.Templates.FindAsync(id);

            var model = await ArticleLogic.Create("Layout Preview");
            model.Content = template?.Content;
            model.EditModeOn = false;
            model.ReadWriteMode = false;
            model.PreviewMode = true;

            return View("~/Views/Home/Index_standard1RO.cshtml", model);
        }

        public async Task<IActionResult> PreviewEdit(int id)
        {
            var template = await DbContext.Templates.FindAsync(id);
            var model = await ArticleLogic.Create("Layout Preview");
            model.Content = template.Content;
            model.EditModeOn = true;
            model.ReadWriteMode = true;
            model.PreviewMode = false;
            return View("~/Views/Home/Index_standard1.cshtml", model);
        }

        /// <summary>
        ///     Creates a new template
        /// </summary>
        /// <param name="request"></param>
        /// <param name="template"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> EditingInline_Create([DataSourceRequest] DataSourceRequest request,
            TemplateIndexViewModel template)
        {
            if (template != null && ModelState.IsValid)
            {
                var entity = new Template
                {
                    Id = 0,
                    Title = template.Title,
                    Description = template.Description,
                    Content = LoremIpsum.SubSection1
                };
                await DbContext.Templates.AddAsync(entity);
                await DbContext.SaveChangesAsync();
                template.Id = entity.Id;
            }

            return Json(new[] {template}.ToDataSourceResult(request, ModelState));
        }

        /// <summary>
        ///     Reads the list of templates
        /// </summary>
        /// <param name="request">Data source request</param>
        /// <returns>JsonResult</returns>
        public async Task<IActionResult> Templates_Read([DataSourceRequest] DataSourceRequest request)
        {
            return Json(await DbContext.Templates.OrderBy(t => t.Title).Select(s => new TemplateIndexViewModel
            {
                Id = s.Id,
                Description = s.Description,
                Title = s.Title
            }).ToDataSourceResultAsync(request));
        }

        [HttpPost]
        public async Task<IActionResult> Templates_Update([DataSourceRequest] DataSourceRequest request,
            [Bind(Prefix = "models")] IEnumerable<TemplateIndexViewModel> templates)
        {
            if (SiteOptions.Value.ReadWriteMode)
            {
                if (templates != null && ModelState.IsValid)
                {
                    foreach (var template in templates)
                    {
                        var entity = await DbContext.Templates.FindAsync(template.Id);
                        entity.Description = template.Description;
                        entity.Title = template.Title;
                    }

                    await DbContext.SaveChangesAsync();
                }

                return Json(await templates.ToDataSourceResultAsync(request, ModelState));
            }

            return Unauthorized();
        }

        [HttpPost]
        public async Task<IActionResult> Templates_Destroy([DataSourceRequest] DataSourceRequest request,
            [Bind(Prefix = "models")] IEnumerable<TemplateIndexViewModel> templates)
        {
            if (SiteOptions.Value.ReadWriteMode)
            {
                if (templates.Any())
                {
                    foreach (var template in templates)
                    {
                        var entity = await DbContext.Templates.FindAsync(template.Id);
                        DbContext.Templates.Remove(entity);
                    }

                    await DbContext.SaveChangesAsync();
                }

                return Json(await templates.ToDataSourceResultAsync(request, ModelState));
            }

            return Unauthorized();
        }
    }
}