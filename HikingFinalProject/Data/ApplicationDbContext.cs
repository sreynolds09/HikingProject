using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using HikingFinalProject.Models;

namespace HikingFinalProject.Data
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Optional: EF Core DbSets (not required for Dapper, but useful)
        public DbSet<Park> Parks { get; set; }
        public DbSet<HikingRoute> Routes { get; set; }
        public DbSet<RoutePoint> RoutePoints { get; set; }
        public DbSet<RouteImages> RouteImages { get; set; }
        public DbSet<RouteFeedback> RouteFeedbacks { get; set; }
    }
}

