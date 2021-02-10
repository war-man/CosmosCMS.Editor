using System;
using System.Linq;
using System.Threading.Tasks;
using CDT.Cosmos.Cms.Common.Controllers;
using CDT.Cosmos.Cms.Common.Data;
using CDT.Cosmos.Cms.Common.Data.Logic;
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
    [Authorize(Roles = "Administrators,Editors")]
    public class MenuController : BaseController
    {
        private readonly IDistributedCache _distributedCache;

        public MenuController(ILogger<MenuController> logger, ApplicationDbContext dbContext,
            UserManager<IdentityUser> userManager, IOptions<SiteCustomizationsConfig> options, ArticleLogic articleLogic,
            IDistributedCache distributedCache, IOptions<RedisContextConfig> redisOptions) :
            base(options, dbContext, logger, userManager, articleLogic, distributedCache, redisOptions)
        {
            _distributedCache = distributedCache;
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

        public async Task<IActionResult> Destroy([DataSourceRequest] DataSourceRequest request, MenuItemViewModel item)
        {
            if (SiteOptions.Value.ReadWriteMode)
            {
                if (ModelState.IsValid)
                    try
                    {
                        var entity = await DbContext.MenuItems.FindAsync(item.Id);
                        if (entity == null)
                        {
                            Logger.LogError($"Could not destroy menu item ID: {item.Id}.");
                            return NotFound();
                        }

                        DbContext.MenuItems.Remove(entity);
                        await DbContext.SaveChangesAsync();
                        await FlushMenuFromRedis(entity.Guid);
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(e, e.Message);
                        throw;
                    }


                return Json(await new[] {item}.ToTreeDataSourceResultAsync(request, ModelState));
            }

            return Unauthorized();
        }

        public async Task<IActionResult> Create([DataSourceRequest] DataSourceRequest request, MenuItemViewModel item)
        {
            if (SiteOptions.Value.ReadWriteMode)
            {
                if (ModelState.IsValid)
                    try
                    {
                        var entity = item.ToEntity();
                        DbContext.MenuItems.Add(entity);

                        await DbContext.SaveChangesAsync();
                        item.Id = entity.Id; // Send back the ID number

                        await FlushMenuFromRedis(entity.Guid);
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(e, e.Message);
                        throw;
                    }

                return Json(new[] {item}.ToTreeDataSourceResult(request, ModelState));
            }

            return Unauthorized();
        }

        public async Task<IActionResult> Read([DataSourceRequest] DataSourceRequest request)
        {
            if (SiteOptions.Value.ReadWriteMode)
                try
                {
                    var items = await DbContext.MenuItems.OrderBy(o => o.SortOrder).Select(s =>
                        new MenuItemViewModel
                        {
                            Id = s.Id,
                            hasChildren = s.HasChildren,
                            ParentId = s.ParentId,
                            SortOrder = s.SortOrder,
                            MenuText = s.MenuText,
                            Url = s.Url,
                            IconCode = s.IconCode
                        }).ToListAsync();
                    var result = items.ToTreeDataSourceResult(request,
                        e => e.Id,
                        e => e.ParentId,
                        e => e
                    );
                    return Json(result);
                }
                catch (Exception e)
                {
                    Logger.LogError(e, e.Message);
                    throw;
                }

            return Unauthorized();
        }

        public async Task<IActionResult> Update([DataSourceRequest] DataSourceRequest request, MenuItemViewModel item)
        {
            if (SiteOptions.Value.ReadWriteMode)
            {
                try
                {
                    var entity = await DbContext.MenuItems.FindAsync(item.Id);
                    if (entity == null)
                    {
                        Logger.LogError($"Could not destroy menu item ID: {item.Id}.");
                        return NotFound();
                    }

                    var oldGuid = entity.Guid;

                    entity.SortOrder = item.SortOrder;
                    entity.MenuText = item.MenuText;
                    entity.IconCode = item.IconCode;
                    entity.Url = item.Url;
                    entity.ParentId = item.ParentId;
                    entity.HasChildren = item.hasChildren;

                    await DbContext.SaveChangesAsync();
                    await FlushMenuFromRedis(oldGuid);
                }
                catch (Exception e)
                {
                    Logger.LogError(e, e.Message);
                    throw;
                }

                return Json(new[] {item}.ToTreeDataSourceResult(request, ModelState));
            }

            return Unauthorized();
        }

        private async Task FlushMenuFromRedis(Guid guid)
        {
            await _distributedCache.RemoveAsync(guid.ToString());
        }
    }
}