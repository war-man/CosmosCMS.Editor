using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace CDT.Cosmos.Cms.Common.Migrations
{
    /// <summary>
    ///     Initial create site
    /// </summary>
    public partial class InitialCreate : Migration
    {
        /// <summary>
        /// Migration up
        /// </summary>
        /// <param name="migrationBuilder"></param>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            if (migrationBuilder.ActiveProvider == "Microsoft.EntityFrameworkCore.Sqlite")
            {
                //
                // SQLITE is used for in-memory unit testing.
                //
                migrationBuilder.CreateTable(
                    "AspNetRoles",
                    table => new
                    {
                        Id = table.Column<string>("nvarchar(450)", nullable: false),
                        Name = table.Column<string>("nvarchar(256)", maxLength: 256, nullable: true),
                        NormalizedName = table.Column<string>("nvarchar(256)", maxLength: 256, nullable: true),
                        ConcurrencyStamp = table.Column<string>("nvarchar(1024)", maxLength: 1024, nullable: true)
                    },
                    constraints: table => { table.PrimaryKey("PK_AspNetRoles", x => x.Id); });

                migrationBuilder.CreateTable(
                    "AspNetUsers",
                    table => new
                    {
                        Id = table.Column<string>("nvarchar(450)", nullable: false),
                        UserName = table.Column<string>("nvarchar(256)", maxLength: 256, nullable: true),
                        NormalizedUserName = table.Column<string>("nvarchar(256)", maxLength: 256, nullable: true),
                        Email = table.Column<string>("nvarchar(256)", maxLength: 256, nullable: true),
                        NormalizedEmail = table.Column<string>("nvarchar(256)", maxLength: 256, nullable: true),
                        EmailConfirmed = table.Column<bool>("bit", nullable: false),
                        PasswordHash = table.Column<string>("nvarchar(1024)", maxLength: 1024, nullable: true),
                        SecurityStamp = table.Column<string>("nvarchar(1024)", maxLength: 1024, nullable: true),
                        ConcurrencyStamp = table.Column<string>("nvarchar(1024)", maxLength: 1024, nullable: true),
                        PhoneNumber = table.Column<string>("nvarchar(1024)", maxLength: 1024, nullable: true),
                        PhoneNumberConfirmed = table.Column<bool>("bit", nullable: false),
                        TwoFactorEnabled = table.Column<bool>("bit", nullable: false),
                        LockoutEnd = table.Column<DateTimeOffset>("datetimeoffset", nullable: true),
                        LockoutEnabled = table.Column<bool>("bit", nullable: false),
                        AccessFailedCount = table.Column<int>("int", nullable: false)
                    },
                    constraints: table => { table.PrimaryKey("PK_AspNetUsers", x => x.Id); });

                migrationBuilder.CreateTable(
                    "FontIcons",
                    table => new
                    {
                        Id = table.Column<int>("int", nullable: false)
                            .Annotation("SqlServer:Identity", "1, 1"),
                        IconCode = table.Column<string>("nvarchar(1024)", maxLength: 1024, nullable: true)
                    },
                    constraints: table => { table.PrimaryKey("PK_FontIcons", x => x.Id); });

                migrationBuilder.CreateTable(
                    "Layouts",
                    table => new
                    {
                        Id = table.Column<int>("int", nullable: false)
                            .Annotation("SqlServer:Identity", "1, 1"),
                        IsDefault = table.Column<bool>("bit", nullable: false),
                        LayoutName = table.Column<string>("nvarchar(128)", maxLength: 128, nullable: true),
                        Notes = table.Column<string>("nvarchar(1024)", maxLength: 1024, nullable: true),
                        Head = table.Column<string>("nvarchar(1024)", maxLength: 1024, nullable: true),
                        BodyHtmlAttributes = table.Column<string>("nvarchar(256)", maxLength: 256, nullable: true),
                        BodyHeaderHtmlAttributes =
                            table.Column<string>("nvarchar(256)", maxLength: 256, nullable: true),
                        HtmlHeader = table.Column<string>("nvarchar(1024)", maxLength: 1024, nullable: true),
                        FooterHtmlAttributes = table.Column<string>("nvarchar(256)", maxLength: 256, nullable: true),
                        FooterHtmlContent = table.Column<string>("nvarchar(1024)", maxLength: 1024, nullable: true),
                        PostFooterBlock = table.Column<string>("nvarchar(1024)", maxLength: 1024, nullable: true)
                    },
                    constraints: table => { table.PrimaryKey("PK_Layouts", x => x.Id); });

                migrationBuilder.CreateTable(
                    "Teams",
                    table => new
                    {
                        Id = table.Column<int>("int", nullable: false)
                            .Annotation("SqlServer:Identity", "1, 1"),
                        TeamName = table.Column<string>("nvarchar(64)", maxLength: 64, nullable: true),
                        TeamDescription = table.Column<string>("nvarchar(1024)", maxLength: 1024, nullable: true)
                    },
                    constraints: table => { table.PrimaryKey("PK_Teams", x => x.Id); });

                migrationBuilder.CreateTable(
                    "Templates",
                    table => new
                    {
                        Id = table.Column<int>("int", nullable: false)
                            .Annotation("SqlServer:Identity", "1, 1"),
                        Title = table.Column<string>("nvarchar(128)", maxLength: 128, nullable: true),
                        Description = table.Column<string>("nvarchar(1024)", maxLength: 1024, nullable: true),
                        Content = table.Column<string>("nvarchar(1024)", maxLength: 1024, nullable: true)
                    },
                    constraints: table => { table.PrimaryKey("PK_Templates", x => x.Id); });

                migrationBuilder.CreateTable(
                    "AspNetRoleClaims",
                    table => new
                    {
                        Id = table.Column<int>("int", nullable: false)
                            .Annotation("SqlServer:Identity", "1, 1"),
                        RoleId = table.Column<string>("nvarchar(450)", nullable: false),
                        ClaimType = table.Column<string>("nvarchar(1024)", maxLength: 1024, nullable: true),
                        ClaimValue = table.Column<string>("nvarchar(1024)", maxLength: 1024, nullable: true)
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                        table.ForeignKey(
                            "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                            x => x.RoleId,
                            "AspNetRoles",
                            "Id",
                            onDelete: ReferentialAction.Cascade);
                    });

                migrationBuilder.CreateTable(
                    "AspNetUserClaims",
                    table => new
                    {
                        Id = table.Column<int>("int", nullable: false)
                            .Annotation("SqlServer:Identity", "1, 1"),
                        UserId = table.Column<string>("nvarchar(450)", nullable: false),
                        ClaimType = table.Column<string>("nvarchar(1024)", maxLength: 1024, nullable: true),
                        ClaimValue = table.Column<string>("nvarchar(1024)", maxLength: 1024, nullable: true)
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                        table.ForeignKey(
                            "FK_AspNetUserClaims_AspNetUsers_UserId",
                            x => x.UserId,
                            "AspNetUsers",
                            "Id",
                            onDelete: ReferentialAction.Cascade);
                    });

                migrationBuilder.CreateTable(
                    "AspNetUserLogins",
                    table => new
                    {
                        LoginProvider = table.Column<string>("nvarchar(450)", nullable: false),
                        ProviderKey = table.Column<string>("nvarchar(450)", nullable: false),
                        ProviderDisplayName = table.Column<string>("nvarchar(1024)", maxLength: 1024, nullable: true),
                        UserId = table.Column<string>("nvarchar(450)", nullable: false)
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_AspNetUserLogins", x => new {x.LoginProvider, x.ProviderKey});
                        table.ForeignKey(
                            "FK_AspNetUserLogins_AspNetUsers_UserId",
                            x => x.UserId,
                            "AspNetUsers",
                            "Id",
                            onDelete: ReferentialAction.Cascade);
                    });

                migrationBuilder.CreateTable(
                    "AspNetUserRoles",
                    table => new
                    {
                        UserId = table.Column<string>("nvarchar(450)", nullable: false),
                        RoleId = table.Column<string>("nvarchar(450)", nullable: false)
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_AspNetUserRoles", x => new {x.UserId, x.RoleId});
                        table.ForeignKey(
                            "FK_AspNetUserRoles_AspNetRoles_RoleId",
                            x => x.RoleId,
                            "AspNetRoles",
                            "Id",
                            onDelete: ReferentialAction.Cascade);
                        table.ForeignKey(
                            "FK_AspNetUserRoles_AspNetUsers_UserId",
                            x => x.UserId,
                            "AspNetUsers",
                            "Id",
                            onDelete: ReferentialAction.Cascade);
                    });

                migrationBuilder.CreateTable(
                    "AspNetUserTokens",
                    table => new
                    {
                        UserId = table.Column<string>("nvarchar(450)", nullable: false),
                        LoginProvider = table.Column<string>("nvarchar(450)", nullable: false),
                        Name = table.Column<string>("nvarchar(450)", nullable: false),
                        Value = table.Column<string>("nvarchar(1024)", maxLength: 1024, nullable: true)
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_AspNetUserTokens", x => new {x.UserId, x.LoginProvider, x.Name});
                        table.ForeignKey(
                            "FK_AspNetUserTokens_AspNetUsers_UserId",
                            x => x.UserId,
                            "AspNetUsers",
                            "Id",
                            onDelete: ReferentialAction.Cascade);
                    });

                migrationBuilder.CreateTable(
                    "Articles",
                    table => new
                    {
                        Id = table.Column<int>("int", nullable: false)
                            .Annotation("SqlServer:Identity", "1, 1"),
                        LayoutId = table.Column<int>("int", nullable: true),
                        TeamId = table.Column<int>("int", nullable: true),
                        ArticleNumber = table.Column<int>("int", nullable: false),
                        StatusCode = table.Column<int>("int", nullable: false),
                        UrlPath = table.Column<string>("nvarchar(128)", maxLength: 128, nullable: true),
                        VersionNumber = table.Column<int>("int", nullable: false),
                        Published = table.Column<DateTime>("datetime2", nullable: true),
                        Title = table.Column<string>("nvarchar(254)", maxLength: 254, nullable: true),
                        Content = table.Column<string>("nvarchar(1024)", maxLength: 1024, nullable: true),
                        Updated = table.Column<DateTime>("datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                        HeaderJavaScript = table.Column<string>("nvarchar(1024)", maxLength: 1024, nullable: true),
                        FooterJavaScript = table.Column<string>("nvarchar(1024)", maxLength: 1024, nullable: true),
                        FontIconId = table.Column<int>("int", nullable: true)
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_Articles", x => x.Id);
                        table.ForeignKey(
                            "FK_Articles_FontIcons_FontIconId",
                            x => x.FontIconId,
                            "FontIcons",
                            "Id",
                            onDelete: ReferentialAction.Restrict);
                        table.ForeignKey(
                            "FK_Articles_Layouts_LayoutId",
                            x => x.LayoutId,
                            "Layouts",
                            "Id",
                            onDelete: ReferentialAction.Restrict);
                        table.ForeignKey(
                            "FK_Articles_Teams_TeamId",
                            x => x.TeamId,
                            "Teams",
                            "Id",
                            onDelete: ReferentialAction.Restrict);
                    });

                migrationBuilder.CreateTable(
                    "TeamMembers",
                    table => new
                    {
                        Id = table.Column<int>("int", nullable: false)
                            .Annotation("SqlServer:Identity", "1, 1"),
                        TeamRole = table.Column<int>("int", nullable: false),
                        TeamId = table.Column<int>("int", nullable: false),
                        UserId = table.Column<string>("nvarchar(450)", nullable: true)
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_TeamMembers", x => x.Id);
                        table.ForeignKey(
                            "FK_TeamMembers_AspNetUsers_UserId",
                            x => x.UserId,
                            "AspNetUsers",
                            "Id",
                            onDelete: ReferentialAction.Restrict);
                        table.ForeignKey(
                            "FK_TeamMembers_Teams_TeamId",
                            x => x.TeamId,
                            "Teams",
                            "Id",
                            onDelete: ReferentialAction.Cascade);
                    });

                migrationBuilder.CreateTable(
                    "ArticleLogs",
                    table => new
                    {
                        Id = table.Column<int>("int", nullable: false)
                            .Annotation("SqlServer:Identity", "1, 1"),
                        IdentityUserId = table.Column<string>("nvarchar(450)", nullable: true),
                        ActivityNotes = table.Column<string>("nvarchar(1024)", nullable: true),
                        DateTimeStamp = table.Column<DateTime>("datetime2", nullable: false),
                        ArticleId = table.Column<int>("int", nullable: false)
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_ArticleLogs", x => x.Id);
                        table.ForeignKey(
                            "FK_ArticleLogs_Articles_ArticleId",
                            x => x.ArticleId,
                            "Articles",
                            "Id",
                            onDelete: ReferentialAction.Cascade);
                        table.ForeignKey(
                            "FK_ArticleLogs_AspNetUsers_IdentityUserId",
                            x => x.IdentityUserId,
                            "AspNetUsers",
                            "Id",
                            onDelete: ReferentialAction.Restrict);
                    });

                migrationBuilder.CreateTable(
                    "MenuItems",
                    table => new
                    {
                        Id = table.Column<int>("int", nullable: false)
                            .Annotation("SqlServer:Identity", "1, 1"),
                        Guid = table.Column<Guid>("uniqueidentifier", nullable: false, defaultValueSql: "newid()"),
                        SortOrder = table.Column<int>("int", nullable: false),
                        MenuText = table.Column<string>("nvarchar(100)", maxLength: 100, nullable: true),
                        Url = table.Column<string>("nvarchar(256)", maxLength: 256, nullable: true),
                        IconCode = table.Column<string>("nvarchar(256)", maxLength: 256, nullable: true),
                        ParentId = table.Column<int>("int", nullable: true),
                        HasChildren = table.Column<bool>("bit", nullable: false),
                        ArticleId = table.Column<int>("int", nullable: true)
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_MenuItems", x => x.Id);
                        table.ForeignKey(
                            "FK_MenuItems_Articles_ArticleId",
                            x => x.ArticleId,
                            "Articles",
                            "Id",
                            onDelete: ReferentialAction.Restrict);
                        table.ForeignKey(
                            "FK_MenuItems_MenuItems_ParentId",
                            x => x.ParentId,
                            "MenuItems",
                            "Id",
                            onDelete: ReferentialAction.Restrict);
                    });

                migrationBuilder.CreateIndex(
                    "IX_ArticleLogs_ArticleId",
                    "ArticleLogs",
                    "ArticleId");

                migrationBuilder.CreateIndex(
                    "IX_ArticleLogs_IdentityUserId",
                    "ArticleLogs",
                    "IdentityUserId");

                migrationBuilder.CreateIndex(
                    "IX_Articles_FontIconId",
                    "Articles",
                    "FontIconId");

                migrationBuilder.CreateIndex(
                    "IX_Articles_LayoutId",
                    "Articles",
                    "LayoutId");

                migrationBuilder.CreateIndex(
                    "IX_Articles_TeamId",
                    "Articles",
                    "TeamId");

                migrationBuilder.CreateIndex(
                    "IX_Articles_UrlPath",
                    "Articles",
                    "UrlPath");

                migrationBuilder.CreateIndex(
                    "IX_Articles_UrlPath_Published_StatusCode",
                    "Articles",
                    new[] {"UrlPath", "Published", "StatusCode"},
                    filter: "[Published] IS NOT NULL");

                migrationBuilder.CreateIndex(
                    "IX_AspNetRoleClaims_RoleId",
                    "AspNetRoleClaims",
                    "RoleId");

                migrationBuilder.CreateIndex(
                    "RoleNameIndex",
                    "AspNetRoles",
                    "NormalizedName",
                    unique: true,
                    filter: "[NormalizedName] IS NOT NULL");

                migrationBuilder.CreateIndex(
                    "IX_AspNetUserClaims_UserId",
                    "AspNetUserClaims",
                    "UserId");

                migrationBuilder.CreateIndex(
                    "IX_AspNetUserLogins_UserId",
                    "AspNetUserLogins",
                    "UserId");

                migrationBuilder.CreateIndex(
                    "IX_AspNetUserRoles_RoleId",
                    "AspNetUserRoles",
                    "RoleId");

                migrationBuilder.CreateIndex(
                    "EmailIndex",
                    "AspNetUsers",
                    "NormalizedEmail");

                migrationBuilder.CreateIndex(
                    "UserNameIndex",
                    "AspNetUsers",
                    "NormalizedUserName",
                    unique: true,
                    filter: "[NormalizedUserName] IS NOT NULL");

                migrationBuilder.CreateIndex(
                    "IX_MenuItems_ArticleId",
                    "MenuItems",
                    "ArticleId");

                migrationBuilder.CreateIndex(
                    "IX_MenuItems_ParentId",
                    "MenuItems",
                    "ParentId");

                migrationBuilder.CreateIndex(
                    "IX_TeamMembers_TeamId",
                    "TeamMembers",
                    "TeamId");

                migrationBuilder.CreateIndex(
                    "IX_TeamMembers_UserId",
                    "TeamMembers",
                    "UserId");
            }
            else
            {
                //
                // SQL Server Migration 
                //
                migrationBuilder.CreateTable(
                    "AspNetRoles",
                    table => new
                    {
                        Id = table.Column<string>("nvarchar(450)", nullable: false),
                        Name = table.Column<string>("nvarchar(256)", maxLength: 256, nullable: true),
                        NormalizedName = table.Column<string>("nvarchar(256)", maxLength: 256, nullable: true),
                        ConcurrencyStamp = table.Column<string>("nvarchar(max)", nullable: true)
                    },
                    constraints: table => { table.PrimaryKey("PK_AspNetRoles", x => x.Id); });

                migrationBuilder.CreateTable(
                    "AspNetUsers",
                    table => new
                    {
                        Id = table.Column<string>("nvarchar(450)", nullable: false),
                        UserName = table.Column<string>("nvarchar(256)", maxLength: 256, nullable: true),
                        NormalizedUserName = table.Column<string>("nvarchar(256)", maxLength: 256, nullable: true),
                        Email = table.Column<string>("nvarchar(256)", maxLength: 256, nullable: true),
                        NormalizedEmail = table.Column<string>("nvarchar(256)", maxLength: 256, nullable: true),
                        EmailConfirmed = table.Column<bool>("bit", nullable: false),
                        PasswordHash = table.Column<string>("nvarchar(max)", nullable: true),
                        SecurityStamp = table.Column<string>("nvarchar(max)", nullable: true),
                        ConcurrencyStamp = table.Column<string>("nvarchar(max)", nullable: true),
                        PhoneNumber = table.Column<string>("nvarchar(max)", nullable: true),
                        PhoneNumberConfirmed = table.Column<bool>("bit", nullable: false),
                        TwoFactorEnabled = table.Column<bool>("bit", nullable: false),
                        LockoutEnd = table.Column<DateTimeOffset>("datetimeoffset", nullable: true),
                        LockoutEnabled = table.Column<bool>("bit", nullable: false),
                        AccessFailedCount = table.Column<int>("int", nullable: false)
                    },
                    constraints: table => { table.PrimaryKey("PK_AspNetUsers", x => x.Id); });

                migrationBuilder.CreateTable(
                    "FontIcons",
                    table => new
                    {
                        Id = table.Column<int>("int", nullable: false)
                            .Annotation("SqlServer:Identity", "1, 1"),
                        IconCode = table.Column<string>("nvarchar(max)", nullable: true)
                    },
                    constraints: table => { table.PrimaryKey("PK_FontIcons", x => x.Id); });

                migrationBuilder.CreateTable(
                    "Layouts",
                    table => new
                    {
                        Id = table.Column<int>("int", nullable: false)
                            .Annotation("SqlServer:Identity", "1, 1"),
                        IsDefault = table.Column<bool>("bit", nullable: false),
                        LayoutName = table.Column<string>("nvarchar(128)", maxLength: 128, nullable: true),
                        Notes = table.Column<string>("nvarchar(max)", nullable: true),
                        Head = table.Column<string>("nvarchar(max)", nullable: true),
                        BodyHtmlAttributes = table.Column<string>("nvarchar(256)", maxLength: 256, nullable: true),
                        BodyHeaderHtmlAttributes =
                            table.Column<string>("nvarchar(256)", maxLength: 256, nullable: true),
                        HtmlHeader = table.Column<string>("nvarchar(max)", nullable: true),
                        FooterHtmlAttributes = table.Column<string>("nvarchar(256)", maxLength: 256, nullable: true),
                        FooterHtmlContent = table.Column<string>("nvarchar(max)", nullable: true),
                        PostFooterBlock = table.Column<string>("nvarchar(max)", nullable: true)
                    },
                    constraints: table => { table.PrimaryKey("PK_Layouts", x => x.Id); });

                migrationBuilder.CreateTable(
                    "Teams",
                    table => new
                    {
                        Id = table.Column<int>("int", nullable: false)
                            .Annotation("SqlServer:Identity", "1, 1"),
                        TeamName = table.Column<string>("nvarchar(64)", maxLength: 64, nullable: true),
                        TeamDescription = table.Column<string>("nvarchar(1024)", maxLength: 1024, nullable: true)
                    },
                    constraints: table => { table.PrimaryKey("PK_Teams", x => x.Id); });

                migrationBuilder.CreateTable(
                    "Templates",
                    table => new
                    {
                        Id = table.Column<int>("int", nullable: false)
                            .Annotation("SqlServer:Identity", "1, 1"),
                        Title = table.Column<string>("nvarchar(128)", maxLength: 128, nullable: true),
                        Description = table.Column<string>("nvarchar(max)", nullable: true),
                        Content = table.Column<string>("nvarchar(max)", nullable: true)
                    },
                    constraints: table => { table.PrimaryKey("PK_Templates", x => x.Id); });

                migrationBuilder.CreateTable(
                    "AspNetRoleClaims",
                    table => new
                    {
                        Id = table.Column<int>("int", nullable: false)
                            .Annotation("SqlServer:Identity", "1, 1"),
                        RoleId = table.Column<string>("nvarchar(450)", nullable: false),
                        ClaimType = table.Column<string>("nvarchar(max)", nullable: true),
                        ClaimValue = table.Column<string>("nvarchar(max)", nullable: true)
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                        table.ForeignKey(
                            "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                            x => x.RoleId,
                            "AspNetRoles",
                            "Id",
                            onDelete: ReferentialAction.Cascade);
                    });

                migrationBuilder.CreateTable(
                    "AspNetUserClaims",
                    table => new
                    {
                        Id = table.Column<int>("int", nullable: false)
                            .Annotation("SqlServer:Identity", "1, 1"),
                        UserId = table.Column<string>("nvarchar(450)", nullable: false),
                        ClaimType = table.Column<string>("nvarchar(max)", nullable: true),
                        ClaimValue = table.Column<string>("nvarchar(max)", nullable: true)
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                        table.ForeignKey(
                            "FK_AspNetUserClaims_AspNetUsers_UserId",
                            x => x.UserId,
                            "AspNetUsers",
                            "Id",
                            onDelete: ReferentialAction.Cascade);
                    });

                migrationBuilder.CreateTable(
                    "AspNetUserLogins",
                    table => new
                    {
                        LoginProvider = table.Column<string>("nvarchar(450)", nullable: false),
                        ProviderKey = table.Column<string>("nvarchar(450)", nullable: false),
                        ProviderDisplayName = table.Column<string>("nvarchar(max)", nullable: true),
                        UserId = table.Column<string>("nvarchar(450)", nullable: false)
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_AspNetUserLogins", x => new {x.LoginProvider, x.ProviderKey});
                        table.ForeignKey(
                            "FK_AspNetUserLogins_AspNetUsers_UserId",
                            x => x.UserId,
                            "AspNetUsers",
                            "Id",
                            onDelete: ReferentialAction.Cascade);
                    });

                migrationBuilder.CreateTable(
                    "AspNetUserRoles",
                    table => new
                    {
                        UserId = table.Column<string>("nvarchar(450)", nullable: false),
                        RoleId = table.Column<string>("nvarchar(450)", nullable: false)
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_AspNetUserRoles", x => new {x.UserId, x.RoleId});
                        table.ForeignKey(
                            "FK_AspNetUserRoles_AspNetRoles_RoleId",
                            x => x.RoleId,
                            "AspNetRoles",
                            "Id",
                            onDelete: ReferentialAction.Cascade);
                        table.ForeignKey(
                            "FK_AspNetUserRoles_AspNetUsers_UserId",
                            x => x.UserId,
                            "AspNetUsers",
                            "Id",
                            onDelete: ReferentialAction.Cascade);
                    });

                migrationBuilder.CreateTable(
                    "AspNetUserTokens",
                    table => new
                    {
                        UserId = table.Column<string>("nvarchar(450)", nullable: false),
                        LoginProvider = table.Column<string>("nvarchar(450)", nullable: false),
                        Name = table.Column<string>("nvarchar(450)", nullable: false),
                        Value = table.Column<string>("nvarchar(max)", nullable: true)
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_AspNetUserTokens", x => new {x.UserId, x.LoginProvider, x.Name});
                        table.ForeignKey(
                            "FK_AspNetUserTokens_AspNetUsers_UserId",
                            x => x.UserId,
                            "AspNetUsers",
                            "Id",
                            onDelete: ReferentialAction.Cascade);
                    });

                migrationBuilder.CreateTable(
                    "Articles",
                    table => new
                    {
                        Id = table.Column<int>("int", nullable: false)
                            .Annotation("SqlServer:Identity", "1, 1"),
                        LayoutId = table.Column<int>("int", nullable: true),
                        TeamId = table.Column<int>("int", nullable: true),
                        ArticleNumber = table.Column<int>("int", nullable: false),
                        StatusCode = table.Column<int>("int", nullable: false),
                        UrlPath = table.Column<string>("nvarchar(128)", maxLength: 128, nullable: true),
                        VersionNumber = table.Column<int>("int", nullable: false),
                        Published = table.Column<DateTime>("datetime2", nullable: true),
                        Title = table.Column<string>("nvarchar(254)", maxLength: 254, nullable: true),
                        Content = table.Column<string>("nvarchar(max)", nullable: true),
                        Updated = table.Column<DateTime>("datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                        HeaderJavaScript = table.Column<string>("nvarchar(max)", nullable: true),
                        FooterJavaScript = table.Column<string>("nvarchar(max)", nullable: true),
                        FontIconId = table.Column<int>("int", nullable: true)
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_Articles", x => x.Id);
                        table.ForeignKey(
                            "FK_Articles_FontIcons_FontIconId",
                            x => x.FontIconId,
                            "FontIcons",
                            "Id",
                            onDelete: ReferentialAction.Restrict);
                        table.ForeignKey(
                            "FK_Articles_Layouts_LayoutId",
                            x => x.LayoutId,
                            "Layouts",
                            "Id",
                            onDelete: ReferentialAction.Restrict);
                        table.ForeignKey(
                            "FK_Articles_Teams_TeamId",
                            x => x.TeamId,
                            "Teams",
                            "Id",
                            onDelete: ReferentialAction.Restrict);
                    });

                migrationBuilder.CreateTable(
                    "TeamMembers",
                    table => new
                    {
                        Id = table.Column<int>("int", nullable: false)
                            .Annotation("SqlServer:Identity", "1, 1"),
                        TeamRole = table.Column<int>("int", nullable: false),
                        TeamId = table.Column<int>("int", nullable: false),
                        UserId = table.Column<string>("nvarchar(450)", nullable: true)
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_TeamMembers", x => x.Id);
                        table.ForeignKey(
                            "FK_TeamMembers_AspNetUsers_UserId",
                            x => x.UserId,
                            "AspNetUsers",
                            "Id",
                            onDelete: ReferentialAction.Restrict);
                        table.ForeignKey(
                            "FK_TeamMembers_Teams_TeamId",
                            x => x.TeamId,
                            "Teams",
                            "Id",
                            onDelete: ReferentialAction.Cascade);
                    });

                migrationBuilder.CreateTable(
                    "ArticleLogs",
                    table => new
                    {
                        Id = table.Column<int>("int", nullable: false)
                            .Annotation("SqlServer:Identity", "1, 1"),
                        IdentityUserId = table.Column<string>("nvarchar(450)", nullable: true),
                        ActivityNotes = table.Column<string>("nvarchar(max)", nullable: true),
                        DateTimeStamp = table.Column<DateTime>("datetime2", nullable: false),
                        ArticleId = table.Column<int>("int", nullable: false)
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_ArticleLogs", x => x.Id);
                        table.ForeignKey(
                            "FK_ArticleLogs_Articles_ArticleId",
                            x => x.ArticleId,
                            "Articles",
                            "Id",
                            onDelete: ReferentialAction.Cascade);
                        table.ForeignKey(
                            "FK_ArticleLogs_AspNetUsers_IdentityUserId",
                            x => x.IdentityUserId,
                            "AspNetUsers",
                            "Id",
                            onDelete: ReferentialAction.Restrict);
                    });

                migrationBuilder.CreateTable(
                    "MenuItems",
                    table => new
                    {
                        Id = table.Column<int>("int", nullable: false)
                            .Annotation("SqlServer:Identity", "1, 1"),
                        Guid = table.Column<Guid>("uniqueidentifier", nullable: false, defaultValueSql: "newid()"),
                        SortOrder = table.Column<int>("int", nullable: false),
                        MenuText = table.Column<string>("nvarchar(100)", maxLength: 100, nullable: true),
                        Url = table.Column<string>("nvarchar(256)", maxLength: 256, nullable: true),
                        IconCode = table.Column<string>("nvarchar(256)", maxLength: 256, nullable: true),
                        ParentId = table.Column<int>("int", nullable: true),
                        HasChildren = table.Column<bool>("bit", nullable: false),
                        ArticleId = table.Column<int>("int", nullable: true)
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_MenuItems", x => x.Id);
                        table.ForeignKey(
                            "FK_MenuItems_Articles_ArticleId",
                            x => x.ArticleId,
                            "Articles",
                            "Id",
                            onDelete: ReferentialAction.Restrict);
                        table.ForeignKey(
                            "FK_MenuItems_MenuItems_ParentId",
                            x => x.ParentId,
                            "MenuItems",
                            "Id",
                            onDelete: ReferentialAction.Restrict);
                    });

                migrationBuilder.CreateIndex(
                    "IX_ArticleLogs_ArticleId",
                    "ArticleLogs",
                    "ArticleId");

                migrationBuilder.CreateIndex(
                    "IX_ArticleLogs_IdentityUserId",
                    "ArticleLogs",
                    "IdentityUserId");

                migrationBuilder.CreateIndex(
                    "IX_Articles_FontIconId",
                    "Articles",
                    "FontIconId");

                migrationBuilder.CreateIndex(
                    "IX_Articles_LayoutId",
                    "Articles",
                    "LayoutId");

                migrationBuilder.CreateIndex(
                    "IX_Articles_TeamId",
                    "Articles",
                    "TeamId");

                migrationBuilder.CreateIndex(
                    "IX_Articles_UrlPath",
                    "Articles",
                    "UrlPath");

                migrationBuilder.CreateIndex(
                    "IX_Articles_UrlPath_Published_StatusCode",
                    "Articles",
                    new[] {"UrlPath", "Published", "StatusCode"},
                    filter: "[Published] IS NOT NULL");

                migrationBuilder.CreateIndex(
                    "IX_AspNetRoleClaims_RoleId",
                    "AspNetRoleClaims",
                    "RoleId");

                migrationBuilder.CreateIndex(
                    "RoleNameIndex",
                    "AspNetRoles",
                    "NormalizedName",
                    unique: true,
                    filter: "[NormalizedName] IS NOT NULL");

                migrationBuilder.CreateIndex(
                    "IX_AspNetUserClaims_UserId",
                    "AspNetUserClaims",
                    "UserId");

                migrationBuilder.CreateIndex(
                    "IX_AspNetUserLogins_UserId",
                    "AspNetUserLogins",
                    "UserId");

                migrationBuilder.CreateIndex(
                    "IX_AspNetUserRoles_RoleId",
                    "AspNetUserRoles",
                    "RoleId");

                migrationBuilder.CreateIndex(
                    "EmailIndex",
                    "AspNetUsers",
                    "NormalizedEmail");

                migrationBuilder.CreateIndex(
                    "UserNameIndex",
                    "AspNetUsers",
                    "NormalizedUserName",
                    unique: true,
                    filter: "[NormalizedUserName] IS NOT NULL");

                migrationBuilder.CreateIndex(
                    "IX_MenuItems_ArticleId",
                    "MenuItems",
                    "ArticleId");

                migrationBuilder.CreateIndex(
                    "IX_MenuItems_ParentId",
                    "MenuItems",
                    "ParentId");

                migrationBuilder.CreateIndex(
                    "IX_TeamMembers_TeamId",
                    "TeamMembers",
                    "TeamId");

                migrationBuilder.CreateIndex(
                    "IX_TeamMembers_UserId",
                    "TeamMembers",
                    "UserId");
            }
        }
        /// <summary>
        /// Migration rollback
        /// </summary>
        /// <param name="migrationBuilder"></param>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                "ArticleLogs");

            migrationBuilder.DropTable(
                "AspNetRoleClaims");

            migrationBuilder.DropTable(
                "AspNetUserClaims");

            migrationBuilder.DropTable(
                "AspNetUserLogins");

            migrationBuilder.DropTable(
                "AspNetUserRoles");

            migrationBuilder.DropTable(
                "AspNetUserTokens");

            migrationBuilder.DropTable(
                "MenuItems");

            migrationBuilder.DropTable(
                "TeamMembers");

            migrationBuilder.DropTable(
                "Templates");

            migrationBuilder.DropTable(
                "AspNetRoles");

            migrationBuilder.DropTable(
                "Articles");

            migrationBuilder.DropTable(
                "AspNetUsers");

            migrationBuilder.DropTable(
                "FontIcons");

            migrationBuilder.DropTable(
                "Layouts");

            migrationBuilder.DropTable(
                "Teams");
        }
    }
}