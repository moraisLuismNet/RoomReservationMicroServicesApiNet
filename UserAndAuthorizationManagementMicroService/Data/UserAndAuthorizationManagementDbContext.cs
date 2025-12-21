using Microsoft.EntityFrameworkCore;
using UserAndAuthorizationManagementMicroService.Models;

namespace UserAndAuthorizationManagementMicroService.Data
{
    public class UserAndAuthorizationManagementDbContext : DbContext
    {
        public UserAndAuthorizationManagementDbContext(DbContextOptions<UserAndAuthorizationManagementDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configuring CHECK restrictions for User Role
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable(t => t.HasCheckConstraint("CK_User_Role", "Role IN ('client', 'admin', 'staff')"));
            });
        }
    }
}
