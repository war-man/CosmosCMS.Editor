using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using CDT.Cosmos.Cms.Common.Data;
using CDT.Cosmos.Cms.Common.Data.Logic;
using CDT.Cosmos.Cms.Common.Models;
using CDT.Cosmos.Cms.Common.Services;
using CDT.Cosmos.Cms.Models;
using HtmlAgilityPack;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Z.EntityFramework.Plus;

namespace CDT.Cosmos.Cms.Controllers
{
    [Authorize(Roles = "Administrators, Editors")]
    public class LayoutsController : BaseController
    {
        public LayoutsController(ILogger<LayoutsController> logger, ApplicationDbContext dbContext,
            UserManager<IdentityUser> userManager, IOptions<SiteCustomizationsConfig> options, ArticleLogic articleLogic,
            IDistributedCache distributedCache, IOptions<RedisContextConfig> redisOptions) : base(options, dbContext,
            logger, userManager, articleLogic
            , distributedCache, redisOptions)
        {
        }

        // GET: Layouts
        public async Task<IActionResult> Index()
        {
            if (SiteOptions.Value.ReadWriteMode)
                try
                {
                    if (!await DbContext.Layouts.AnyAsync())
                    {
                        DbContext.Layouts.AddRange(LayoutDefaults.GetStarterLayouts());
                        await DbContext.SaveChangesAsync();
                    }

                    var model = await ArticleLogic.Create("Layouts");
                    model.Title = "Layouts";
                    return View(model);
                }
                catch (Exception e)
                {
                    Logger.LogError(e.Message, e);
                    throw;
                }

            return Unauthorized();
        }

        public async Task<IActionResult> Create()
        {
            var layout = LayoutDefaults.GetOceanside();
            layout.LayoutName = "New Layout " + await DbContext.Layouts.CountAsync();
            layout.Notes = "New layout created. Please customize using code editor.";
            DbContext.Layouts.Add(layout);
            await DbContext.SaveChangesAsync();
            return RedirectToAction("EditCode", new {layout.Id});
        }

        public async Task<IActionResult> EditNotes(int? id)
        {
            if (SiteOptions.Value.ReadWriteMode)
            {
                if (id == null)
                    return RedirectToAction("Index");

                var model = await DbContext.Layouts.Select(s => new LayoutIndexViewModel
                {
                    Id = s.Id,
                    IsDefault = s.IsDefault,
                    LayoutName = s.LayoutName,
                    Notes = s.Notes
                }).FirstOrDefaultAsync(f => f.Id == id.Value);

                if (model == null) return NotFound();

                return View(model);
            }

            return Unauthorized();
        }

        [HttpPost]
        public async Task<IActionResult> EditNotes(LayoutIndexViewModel model)
        {
            if (SiteOptions.Value.ReadWriteMode)
            {
                if (!ModelState.IsValid)
                    return View(model);

                if (model != null)
                {
                    var layout = await DbContext.Layouts.FindAsync(model.Id);
                    layout.LayoutName = model.LayoutName;
                    var contentHtmlDocument = new HtmlDocument();
                    contentHtmlDocument.LoadHtml(HttpUtility.HtmlDecode(model.Notes));
                    if (contentHtmlDocument.ParseErrors.Any())
                        foreach (var error in contentHtmlDocument.ParseErrors)
                            ModelState.AddModelError("Notes", error.Reason);

                    var remove = "<div style=\"display:none;\"></div>";
                    layout.Notes = contentHtmlDocument.ParsedText.Replace(remove, "").Trim();
                    //layout.IsDefault = model.IsDefault;
                    if (model.IsDefault)
                    {
                        var layouts = await DbContext.Layouts.Where(w => w.Id != model.Id).ToListAsync();
                        foreach (var layout1 in layouts) layout1.IsDefault = false;
                    }

                    await DbContext.SaveChangesAsync();
                }

                return RedirectToAction("Index");
            }

            return Unauthorized();
        }

        // GET: Layouts/Edit/5
        public async Task<IActionResult> EditCode(int? id)
        {
            if (SiteOptions.Value.ReadWriteMode)
                try
                {
                    if (id == null) return NotFound();

                    var layout = await DbContext.Layouts.FindAsync(id);
                    if (layout == null) return NotFound();

                    var model = new LayoutCodeViewModel
                    {
                        Id = layout.Id,
                        EditorTitle = layout.LayoutName,
                        EditorFields = new List<EditorField>
                        {
                            new EditorField
                            {
                                FieldId = "Head",
                                FieldName = "Head",
                                EditorMode = EditorMode.Html
                            },
                            new EditorField
                            {
                                FieldId = "BodyHtmlAttributes",
                                FieldName = "Body Html Attributes",
                                EditorMode = EditorMode.Html
                            },
                            new EditorField
                            {
                                FieldId = "BodyHeaderHtmlAttributes",
                                FieldName = "Header Html Attributes",
                                EditorMode = EditorMode.Html
                            },
                            new EditorField
                            {
                                FieldId = "HtmlHeader",
                                FieldName = "Header Content",
                                EditorMode = EditorMode.Html
                            },
                            new EditorField
                            {
                                FieldId = "FooterHtmlAttributes",
                                FieldName = "Footer Html Attributes",
                                EditorMode = EditorMode.Html
                            },
                            new EditorField
                            {
                                FieldId = "FooterHtmlContent",
                                FieldName = "Footer Content",
                                EditorMode = EditorMode.Html
                            },
                            new EditorField
                            {
                                FieldId = "PostFooterBlock",
                                FieldName = "Post Footer Block",
                                EditorMode = EditorMode.Html
                            }
                        },
                        CustomButtons = new List<string> {"Preview", "Layouts"},
                        Head = layout.Head,
                        BodyHtmlAttributes = layout.BodyHtmlAttributes,
                        BodyHeaderHtmlAttributes = layout.BodyHeaderHtmlAttributes,
                        HtmlHeader = layout.HtmlHeader,
                        FooterHtmlAttributes = layout.FooterHtmlAttributes,
                        FooterHtmlContent = layout.FooterHtmlContent,
                        PostFooterBlock = layout.PostFooterBlock,
                        EditingField = ""
                    };
                    return View(model);
                }
                catch (Exception e)
                {
                    Logger.LogError(e.Message, e);
                    throw;
                }

            return Unauthorized();
        }

        /// <summary>
        ///     Saves the code and html of the page.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="layout"></param>
        /// <returns></returns>
        /// <remarks>
        ///     <para>
        ///         This method saves page code to the database. The following properties are validated with method
        ///         <see cref="BaseController.ValidateHtml" />:
        ///     </para>
        ///     <list type="bullet">
        ///         <item>
        ///             <see cref="LayoutCodeViewModel.Head" />
        ///         </item>
        ///         <item>
        ///             <see cref="LayoutCodeViewModel.HtmlHeader" />
        ///         </item>
        ///         <item>
        ///             <see cref="LayoutCodeViewModel.FooterHtmlContent" />
        ///         </item>
        ///     </list>
        ///     <para>
        ///         HTML formatting errors that could not be automatically fixed by <see cref="BaseController.ValidateHtml" />
        ///         are logged with <see cref="ControllerBase.ModelState" />.
        ///     </para>
        /// </remarks>
        /// <exception cref="NotFoundResult"></exception>
        /// <exception cref="UnauthorizedResult"></exception>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCode(int id, LayoutCodeViewModel layout)
        {
            if (SiteOptions.Value.ReadWriteMode)
                try
                {
                    if (id != layout.Id) return NotFound();

                    if (ModelState.IsValid)
                        try
                        {
                            //
                            // This layout now is the default, make sure the others are set to "false."

                            var entity = await DbContext.Layouts.FindAsync(layout.Id);
                            entity.BodyHeaderHtmlAttributes = layout.BodyHeaderHtmlAttributes;
                            entity.BodyHtmlAttributes = layout.BodyHtmlAttributes;
                            entity.FooterHtmlAttributes = layout.FooterHtmlAttributes;
                            entity.FooterHtmlContent =
                                ValidateHtml("FooterHtmlContent", layout.FooterHtmlContent, ModelState);
                            entity.Head = ValidateHtml("Head", layout.Head, ModelState);
                            entity.HtmlHeader = ValidateHtml("HtmlHeader", layout.HtmlHeader, ModelState);
                            entity.PostFooterBlock = layout.PostFooterBlock;

                            // Check validation again after validation of HTML
                            if (ModelState.IsValid)
                                // Go ahead and save
                                await DbContext.SaveChangesAsync();
                            var model = new LayoutCodeViewModel
                            {
                                Id = layout.Id,
                                EditorTitle = layout.EditorTitle,
                                EditorFields = new List<EditorField>
                                {
                                    new EditorField
                                    {
                                        FieldId = "Head",
                                        FieldName = "Head",
                                        EditorMode = EditorMode.Html
                                    },
                                    new EditorField
                                    {
                                        FieldId = "BodyHtmlAttributes",
                                        FieldName = "Body Html Attributes",
                                        EditorMode = EditorMode.Html
                                    },
                                    new EditorField
                                    {
                                        FieldId = "BodyHeaderHtmlAttributes",
                                        FieldName = "Header Html Attributes",
                                        EditorMode = EditorMode.Html
                                    },
                                    new EditorField
                                    {
                                        FieldId = "HtmlHeader",
                                        FieldName = "Header Content",
                                        EditorMode = EditorMode.Html
                                    },
                                    new EditorField
                                    {
                                        FieldId = "FooterHtmlAttributes",
                                        FieldName = "Footer Html Attributes",
                                        EditorMode = EditorMode.Html
                                    },
                                    new EditorField
                                    {
                                        FieldId = "FooterHtmlContent",
                                        FieldName = "Footer Content",
                                        EditorMode = EditorMode.Html
                                    },
                                    new EditorField
                                    {
                                        FieldId = "PostFooterBlock",
                                        FieldName = "Post Footer Block",
                                        EditorMode = EditorMode.Html
                                    }
                                },
                                CustomButtons = new List<string> {"Preview", "Layouts"},
                                Head = layout.Head,
                                BodyHtmlAttributes = layout.BodyHtmlAttributes,
                                BodyHeaderHtmlAttributes = layout.BodyHeaderHtmlAttributes,
                                HtmlHeader = layout.HtmlHeader,
                                FooterHtmlAttributes = layout.FooterHtmlAttributes,
                                FooterHtmlContent = layout.FooterHtmlContent,
                                PostFooterBlock = layout.PostFooterBlock,
                                EditingField = layout.EditingField,
                                IsValid = ModelState.IsValid
                            };
                            return View(model);
                        }
                        catch (DbUpdateConcurrencyException)
                        {
                            if (!LayoutExists(layout.Id)) return NotFound();
                            throw;
                        }

                    return View(layout);
                }
                catch (Exception e)
                {
                    Logger.LogError(e.Message, e);
                    throw;
                }

            return Unauthorized();
        }

        public async Task<IActionResult> Preview(int id)
        {
            var layout = await DbContext.Layouts.FindAsync(id);
            var model = await ArticleLogic.Create("Layout Preview");
            model.Layout = new LayoutViewModel(layout);
            model.EditModeOn = false;
            model.ReadWriteMode = false;
            model.PreviewMode = true;

            return View("~/Views/Home/CosmosIndex.cshtml", model);
        }

        public async Task<IActionResult> EditPreview(int id)
        {
            var layout = await DbContext.Layouts.FindAsync(id);
            var model = await ArticleLogic.Create("Layout Preview");
            model.Layout = new LayoutViewModel(layout);
            model.EditModeOn = true;
            model.ReadWriteMode = true;
            model.PreviewMode = true;
            return View("~/Views/Home/Index.cshtml", model);
        }

        [HttpGet]
        public async Task<IActionResult> SetLayoutAsDefault(int? id)
        {
            if (SiteOptions.Value.ReadWriteMode)
            {
                if (id == null)
                    return RedirectToAction("Index");

                var model = await DbContext.Layouts.Select(s => new LayoutIndexViewModel
                {
                    Id = s.Id,
                    IsDefault = s.IsDefault,
                    LayoutName = s.LayoutName,
                    Notes = s.Notes
                }).FirstOrDefaultAsync(f => f.Id == id.Value);

                if (model == null) return NotFound();

                return View(model);
            }

            return Unauthorized();
        }

        /// <summary>
        /// Sets a layout as the default layout
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> SetLayoutAsDefault(LayoutIndexViewModel model)
        {
            if (!SiteOptions.Value.ReadWriteMode) return Unauthorized();
            if (!ModelState.IsValid)
                return View(model);

            if (model == null) return RedirectToAction("Index");

            var layout = await DbContext.Layouts.FindAsync(model.Id);
            layout.IsDefault = model.IsDefault;
            if (model.IsDefault)
            {
                await DbContext.SaveChangesAsync();
                await DbContext.Layouts.Where(w => w.Id != model.Id)
                    .UpdateAsync(u => new Layout
                    {
                        IsDefault = false
                    });
                int[] validCodes =
                {
                    (int) StatusCodeEnum.Active,
                    (int) StatusCodeEnum.Inactive
                };

                await DbContext.Articles.Where(w => validCodes.Contains(w.StatusCode))
                    .UpdateAsync(u => new Article
                    {
                        LayoutId = layout.Id
                    });
                return RedirectToAction("Publish", "Editor");
            }

            return RedirectToAction("Index", "Layouts");
        }

        private bool LayoutExists(int id)
        {
            try
            {
                return DbContext.Layouts.Any(e => e.Id == id);
            }
            catch (Exception e)
            {
                Logger.LogError(e.Message, e);
                throw;
            }
        }

        /// <summary>
        /// Gets a list of layouts
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<IActionResult> Read_Layouts([DataSourceRequest] DataSourceRequest request)
        {
            var model = DbContext.Layouts.Select(s => new LayoutIndexViewModel
            {
                Id = s.Id,
                IsDefault = s.IsDefault,
                LayoutName = s.LayoutName,
                Notes = s.Notes
            }).OrderByDescending(o => o.IsDefault).ThenBy(t => t.LayoutName);

            return Json(await model.ToDataSourceResultAsync(request));
        }

        /// <summary>
        /// Updates a layout
        /// </summary>
        /// <param name="request"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> Update_Layout([DataSourceRequest] DataSourceRequest request,
            LayoutIndexViewModel model)
        {
            if (model != null && ModelState.IsValid)
            {
                var entity = await DbContext.Layouts.FindAsync(model.Id);
                entity.IsDefault = model.IsDefault;
                entity.LayoutName = model.LayoutName;
                entity.Notes = model.Notes;
                await DbContext.SaveChangesAsync();
            }

            return Json(new[] {model}.ToDataSourceResult(request, ModelState));
        }

        /// <summary>
        /// Removes a layout
        /// </summary>
        /// <param name="request"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> Destroy_Layout([DataSourceRequest] DataSourceRequest request,
            LayoutIndexViewModel model)
        {
            if (model != null)
            {
                var entity = await DbContext.Layouts.FindAsync(model.Id);
                DbContext.Layouts.Remove(entity);
                await DbContext.SaveChangesAsync();
            }

            return Json(new[] {model}.ToDataSourceResult(request, ModelState));
        }
    }
}