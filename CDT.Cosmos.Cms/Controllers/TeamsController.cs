﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using CDT.Cosmos.Cms.Common.Data;
using CDT.Cosmos.Cms.Common.Models;
using CDT.Cosmos.Cms.Common.Services;
using CDT.Cosmos.Cms.Models;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CDT.Cosmos.Cms.Controllers
{
    [Authorize(Roles = "Administrators, Editors")]
    public class TeamsController : BaseController
    {
        public TeamsController(IOptions<SiteCustomizationsConfig> options, ApplicationDbContext dbContext,
            ILogger<TeamsController> logger, UserManager<IdentityUser> userManager,
            IDistributedCache distributedCache = null,
            IOptions<RedisContextConfig> redisOptions = null) :
            base(options, dbContext, logger, userManager, distributedCache, redisOptions)
        {
        }

        public IActionResult Index()
        {
            if (SiteOptions.Value.ReadWriteMode) return View();

            return Unauthorized();
        }

        public async Task<ActionResult> Membership(int? id)
        {
            if (SiteOptions.Value.ReadWriteMode)
                try
                {
                    if (id == null) return RedirectToAction("index");

                    var data = await DbContext.Teams.FindAsync(id);
                    if (data == null) return NotFound();

                    var teamMembers = await UserManager.GetUsersInRoleAsync("Team Members");
                    var members = teamMembers.Select(s => new TeamMemberLookupItem
                    {
                        UserId = s.Id,
                        UserEmail = s.Email
                    }).OrderBy(o => o.UserEmail);

                    ViewData["Team Members"] = members;

                    return View(new TeamViewModel
                    {
                        Id = data.Id,
                        TeamName = data.TeamName,
                        TeamDescription = data.TeamDescription
                    });
                }
                catch (Exception e)
                {
                    Logger.LogError(e.Message, e);
                    throw;
                }

            return Unauthorized();
        }

        private async Task<ClaimsPrincipal> GetPrincipal(IdentityUser user)
        {
            var claims = await UserManager.GetClaimsAsync(user);

            claims.Add(new Claim(ClaimTypes.Name, user.UserName));
            claims.Add(new Claim(ClaimTypes.NameIdentifier, user.Id));
            claims.Add(new Claim(ClaimTypes.Email, user.Email));
            var roles = await UserManager.GetRolesAsync(user);
            foreach (var role in roles) claims.Add(new Claim(ClaimTypes.Role, role));

            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "Basic"));

            return principal;
        }

        #region TEAMS CRUD

        public async Task<ActionResult> Teams_Read([DataSourceRequest] DataSourceRequest request)
        {
            if (SiteOptions.Value.ReadWriteMode)
                try
                {
                    var data = await DbContext.Teams.ToListAsync();
                    return Json(await data.Select(s => new TeamViewModel
                    {
                        Id = s.Id,
                        TeamName = s.TeamName,
                        TeamDescription = s.TeamDescription
                    }).ToDataSourceResultAsync(request));
                }
                catch (Exception e)
                {
                    Logger.LogError(e.Message, e);
                    throw;
                }

            return Unauthorized();
        }

        [HttpPost]
        public async Task<ActionResult> Teams_Create([DataSourceRequest] DataSourceRequest request,
            [Bind(Prefix = "models")] IEnumerable<TeamViewModel> teams)
        {
            if (SiteOptions.Value.ReadWriteMode)
                try
                {
                    var results = new List<TeamViewModel>();

                    if (teams != null && ModelState.IsValid)
                        foreach (var team in teams)
                        {
                            var entity = new Team
                            {
                                Id = 0,
                                TeamName = team.TeamName,
                                TeamDescription = team.TeamDescription
                            };
                            DbContext.Teams.Add(entity);
                            await DbContext.SaveChangesAsync();
                            results.Add(new TeamViewModel
                            {
                                Id = entity.Id,
                                TeamName = entity.TeamName,
                                TeamDescription = entity.TeamDescription
                            });
                        }

                    return Json(await results.ToDataSourceResultAsync(request, ModelState));
                }
                catch (Exception e)
                {
                    Logger.LogError(e.Message, e);
                    throw;
                }

            return Unauthorized();
        }

        [HttpPost]
        public async Task<ActionResult> Teams_Update([DataSourceRequest] DataSourceRequest request,
            [Bind(Prefix = "models")] IEnumerable<TeamViewModel> teams)
        {
            if (SiteOptions.Value.ReadWriteMode)
                try
                {
                    if (teams != null && ModelState.IsValid)
                    {
                        foreach (var team in teams)
                        {
                            var entity = await DbContext.Teams.FirstOrDefaultAsync(f => f.Id == team.Id);
                            entity.TeamDescription = team.TeamDescription;
                            entity.TeamName = team.TeamName;
                        }

                        await DbContext.SaveChangesAsync();
                    }

                    return Json(teams.ToDataSourceResult(request, ModelState));
                }
                catch (Exception e)
                {
                    Logger.LogError(e.Message, e);
                    throw;
                }

            return Unauthorized();
        }

        [HttpPost]
        public async Task<ActionResult> Teams_Destroy([DataSourceRequest] DataSourceRequest request,
            [Bind(Prefix = "models")] IEnumerable<TeamViewModel> teams)
        {
            if (SiteOptions.Value.ReadWriteMode)
                try
                {
                    if (teams.Any())
                        foreach (var team in teams)
                        {
                            var entity = await DbContext.Teams.FirstOrDefaultAsync(f => f.Id == team.Id);
                            DbContext.Teams.Remove(entity);
                        }

                    await DbContext.SaveChangesAsync();

                    return Json(teams.ToDataSourceResult(request, ModelState));
                }
                catch (Exception e)
                {
                    Logger.LogError(e.Message, e);
                    throw;
                }

            return Unauthorized();
        }

        #endregion

        #region TEAM MEMBER CRUD

        public async Task<ActionResult> TeamMembers_Read([DataSourceRequest] DataSourceRequest request, int id)
        {
            if (SiteOptions.Value.ReadWriteMode)
                try
                {
                    var data = await DbContext.TeamMembers.Include(i => i.Team).Include(i => i.User)
                        .Include(i => i.Team)
                        .Where(w => w.TeamId == id)
                        .ToListAsync();
                    var model = data.Select(s => new TeamMemberViewModel
                    {
                        Id = s.Id,
                        TeamRole = new TeamRoleLookupItem
                        {
                            TeamRoleId = s.TeamId,
                            TeamRoleName = Enum.GetName(typeof(TeamRoleEnum), s.TeamRole)
                        },
                        Team = new TeamViewModel
                        {
                            Id = s.TeamId,
                            TeamDescription = s.Team.TeamDescription,
                            TeamName = s.Team.TeamName
                        },
                        Member = null
                    }).ToList();
                    return Json(model.ToDataSourceResult(request));
                }
                catch (Exception e)
                {
                    Logger.LogError(e.Message, e);
                    throw;
                }

            return Unauthorized();
        }

        /// <summary>
        ///     Adds members to a team.
        /// </summary>
        /// <param name="request">DataSourceRequest</param>
        /// <param name="members">New member list</param>
        /// <param name="id">Team ID</param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult> TeamMembers_Create([DataSourceRequest] DataSourceRequest request,
            [Bind(Prefix = "models")] IEnumerable<TeamMemberViewModel> members, int id)
        {
            if (SiteOptions.Value.ReadWriteMode)
                try
                {
                    var results = new List<TeamMemberViewModel>();


                    if (members != null && ModelState.IsValid)
                    {
                        var team = await DbContext.Teams.FindAsync(id);

                        foreach (var member in members)
                            if (member.Member.UserId != Guid.Empty.ToString())
                            {
                                var user = await UserManager.FindByIdAsync(member.Member.UserId);

                                var claimsPrincipal = await GetPrincipal(user);

                                if (!claimsPrincipal.IsInRole("Team Members"))
                                    await UserManager.AddToRoleAsync(user, "Team Members");

                                var membership = new TeamMember
                                {
                                    Id = 0,
                                    TeamRole = member.TeamRole?.TeamRoleId ?? (int) TeamRoleEnum.Reviewer,
                                    TeamId = team.Id,
                                    UserId = member.Member.UserId
                                };

                                DbContext.TeamMembers.Add(membership);
                                await DbContext.SaveChangesAsync();

                                //
                                // Now that the membership is saved, we have an ID.
                                // Retrieve with that ID plus the related User and
                                // Team information.
                                //
                                membership = await DbContext.TeamMembers
                                    .Include(i => i.User)
                                    .Include(i => i.Team)
                                    .Where(w => w.Id == membership.Id).FirstOrDefaultAsync();

                                results.Add(new TeamMemberViewModel
                                {
                                    Id = membership.Id,
                                    TeamRole = new TeamRoleLookupItem
                                    {
                                        TeamRoleId = membership.TeamRole,
                                        TeamRoleName = Enum.GetName(typeof(TeamRoleEnum), membership.TeamRole)
                                    },
                                    Team = new TeamViewModel
                                    {
                                        Id = membership.TeamId,
                                        TeamName = membership.Team.TeamName,
                                        TeamDescription = membership.Team.TeamDescription
                                    },
                                    Member = new TeamMemberLookupItem
                                    {
                                        UserId = membership.UserId,
                                        UserEmail = membership.User.Email
                                    }
                                });
                            }
                    }

                    return Json(results.ToDataSourceResult(request, ModelState));
                }
                catch (Exception e)
                {
                    Logger.LogError(e.Message, e);
                    throw;
                }

            return Unauthorized();
        }

        [HttpPost]
        public async Task<ActionResult> TeamMembers_Update([DataSourceRequest] DataSourceRequest request,
            [Bind(Prefix = "models")] IEnumerable<TeamMemberViewModel> members, int id)
        {
            if (SiteOptions.Value.ReadWriteMode)
                try
                {
                    var results = new List<TeamMemberViewModel>();

                    if (ModelState.IsValid == false)
                        return Json(await members.ToDataSourceResultAsync(request, ModelState));

                    foreach (var member in members)
                    {
                        var entity = await DbContext.TeamMembers.Include(i => i.Team).Include(i => i.User)
                            .Where(t => t.Id == member.Id).FirstOrDefaultAsync();
                        entity.TeamRole = member.TeamRole.TeamRoleId;
                        results.Add(new TeamMemberViewModel
                        {
                            Id = entity.Id,
                            Member = new TeamMemberLookupItem
                            {
                                UserEmail = entity.User.Email,
                                UserId = entity.UserId
                            },
                            Team = new TeamViewModel
                            {
                                Id = entity.TeamId,
                                TeamName = entity.Team.TeamName,
                                TeamDescription = entity.Team.TeamDescription
                            },
                            TeamRole = new TeamRoleLookupItem
                            {
                                TeamRoleId = entity.TeamRole,
                                TeamRoleName = Enum.GetName(typeof(TeamRoleEnum), entity.TeamRole)
                            }
                        });
                    }

                    await DbContext.SaveChangesAsync();

                    return Json(await results.ToDataSourceResultAsync(request, ModelState));
                }
                catch (Exception e)
                {
                    Logger.LogError(e.Message, e);
                    throw;
                }

            return Unauthorized();
        }

        [HttpPost]
        public async Task<ActionResult> TeamMembers_Destroy([DataSourceRequest] DataSourceRequest request,
            [Bind(Prefix = "models")] IEnumerable<TeamMemberViewModel> members)
        {
            if (SiteOptions.Value.ReadWriteMode)
                try
                {
                    if (members.Any())
                    {
                        var ids = members.Select(s => s.Id).ToArray();

                        DbContext.TeamMembers.RemoveRange(await DbContext.TeamMembers.Where(w => ids.Contains(w.Id))
                            .ToListAsync());
                        await DbContext.SaveChangesAsync();
                    }

                    return Json(members.ToDataSourceResult(request, ModelState));
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