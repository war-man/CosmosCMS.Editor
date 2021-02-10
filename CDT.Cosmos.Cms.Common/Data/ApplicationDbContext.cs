using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CDT.Cosmos.Cms.Common.Data
{
    /// <summary>
    ///     Database Context for Cosmos CMS
    /// </summary>
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="options"></param>
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        /// <summary>
        ///     Constructor without options.
        /// </summary>
        public ApplicationDbContext()
        {
        }

        /// <summary>
        ///     On model creating
        /// </summary>
        /// <param name="modelBuilder"></param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Article>()
                .HasIndex(p => new {p.UrlPath}).IsUnique(false);

            modelBuilder.Entity<Article>()
                .Property(b => b.Updated)
                .HasDefaultValueSql("GETUTCDATE()").ValueGeneratedOnAddOrUpdate();

            modelBuilder.Entity<MenuItem>()
                .Property(b => b.Guid)
                .HasDefaultValueSql("newid()");

            modelBuilder.Entity<Article>()
                .HasIndex(p => new {p.UrlPath, p.Published, p.StatusCode})
                .HasFilter("[Published] IS NOT NULL");

            base.OnModelCreating(modelBuilder);
        }

        #region DbContext

        /// <summary>
        ///     Articles
        /// </summary>
        public DbSet<Article> Articles { get; set; }

        /// <summary>
        ///     Article activity logs
        /// </summary>
        public DbSet<ArticleLog> ArticleLogs { get; set; }

        /// <summary>
        ///     Menu items
        /// </summary>
        public DbSet<MenuItem> MenuItems { get; set; }

        /// <summary>
        ///     Font icons
        /// </summary>
        public DbSet<FontIcon> FontIcons { get; set; }

        /// <summary>
        ///     Web page templates
        /// </summary>
        public DbSet<Template> Templates { get; set; }

        /// <summary>
        ///     Website layouts
        /// </summary>
        public DbSet<Layout> Layouts { get; set; }

        /// <summary>
        ///     Page teams
        /// </summary>
        public DbSet<Team> Teams { get; set; }

        /// <summary>
        ///     Team membership
        /// </summary>
        public DbSet<TeamMember> TeamMembers { get; set; }

        #endregion
    }
}