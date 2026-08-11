using DAL.Constants;
using DAL.Models.Common;
using DAL.Models.Identity;
using DAL.Models.Property;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Reflection.Emit;
using static System.Runtime.InteropServices.JavaScript.JSType;


namespace DAL
{
    public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<int>, int>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

            builder.Entity<IdentityRole<int>>().HasData(
                new IdentityRole<int> { Id = 1, Name = AppRoles.Admin, NormalizedName = "ADMIN", ConcurrencyStamp = "1" },
                new IdentityRole<int> { Id = 2, Name = AppRoles.Host, NormalizedName = "HOST", ConcurrencyStamp = "2" },
                new IdentityRole<int> { Id = 3, Name = AppRoles.Guest, NormalizedName = "GUEST", ConcurrencyStamp = "3" }
            );

            builder.Entity<Amenity>().HasData(
                new Amenity { Id = 1, Name = "WiFi", IconClass = "wifi" },
                new Amenity { Id = 2, Name = "Kitchen", IconClass = "kitchen" },
                new Amenity { Id = 3, Name = "Free parking", IconClass = "parking" },
                new Amenity { Id = 4, Name = "Air conditioning", IconClass = "ac" },
                new Amenity { Id = 5, Name = "Pool", IconClass = "pool" },
                new Amenity { Id = 6, Name = "Washer", IconClass = "washer" },
                new Amenity { Id = 7, Name = "TV", IconClass = "tv" },
                new Amenity { Id = 8, Name = "Heating", IconClass = "heating" },
                new Amenity { Id = 9, Name = "Dedicated workspace", IconClass = "workspace" },
                new Amenity { Id = 10, Name = "Pets allowed", IconClass = "pets" }
            );

            foreach (var entityType in builder.Model.GetEntityTypes())
            {
                if (typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
                {
                    builder.Entity(entityType.ClrType)
                        .HasQueryFilter(GetSoftDeleteFilter(entityType.ClrType));
                }
            }
        }

        private static LambdaExpression GetSoftDeleteFilter(Type entityType)
        {
            var parameter = Expression.Parameter(entityType, "e");
            var property = Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted));
            var condition = Expression.Equal(property, Expression.Constant(false));
            return Expression.Lambda(condition, parameter);
        }
    }
}
