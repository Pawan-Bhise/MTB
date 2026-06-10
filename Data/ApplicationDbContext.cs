using Microsoft.EntityFrameworkCore;
using PMEHCRM.Models;

namespace PMEHCRM.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Login> Users { get; set; } // DbSet for Login model

        public DbSet<CRMFormat> CRMFormatRecords { get; set; } // DbSet for User model

        public DbSet<TicketManagement> TicketManagementRecords { get; set; } // DbSet for User model

        public DbSet<Attachment> Attachments { get; set; } // DbSet for Attachment model

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure relationships between TicketManagement and Attachments
            modelBuilder.Entity<Attachment>()
                .HasKey(a => a.Id); // Primary key for Attachment

            modelBuilder.Entity<Attachment>()
                .HasOne<TicketManagement>()
                .WithMany(t => t.Attachments)
                .HasForeignKey(a => a.TicketId); // Foreign key to TicketManagement

            base.OnModelCreating(modelBuilder);
        }

    }
}
