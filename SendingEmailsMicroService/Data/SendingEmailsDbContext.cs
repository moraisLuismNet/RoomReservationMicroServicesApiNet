using Microsoft.EntityFrameworkCore;
using SendingEmailsMicroService.Models;

namespace SendingEmailsMicroService.Data
{
    public class SendingEmailsDbContext : DbContext
    {
        public SendingEmailsDbContext(DbContextOptions<SendingEmailsDbContext> options) : base(options)
        {
        }

        public DbSet<EmailQueue> EmailQueues { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configuring CHECK restrictions for EmailType
            modelBuilder.Entity<EmailQueue>(entity =>
            {
                entity.ToTable(t => t.HasCheckConstraint("CK_EmailQueue_EmailType", "EmailType IN ('confirmation', 'reminder', 'cancellation', 'notification')"));
            });

            // Configuring CHECK restrictions for Email Status
            modelBuilder.Entity<EmailQueue>(entity =>
            {
                entity.ToTable(t => t.HasCheckConstraint("CK_EmailQueue_Status", "Status IN ('pending', 'sent', 'failed', 'retrying')"));
            });
        }
    }
}
