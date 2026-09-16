using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<ProductType> ProductTypes { get; set; }
        public DbSet<ProductSpec> ProductSpecs { get; set; }

        public DbSet<ContactSubmission> ContactSubmissions { get; set; }
        public DbSet<User> Users { get; set; }

        public DbSet<Company> Companies { get; set; }
        public DbSet<CompanyPhone> CompanyPhones { get; set; }
        public DbSet<CompanyEmail> CompanyEmails { get; set; }
        public DbSet<LedgerEntry> LedgerEntries { get; set; }
        public DbSet<ReminderSchedule> ReminderSchedules { get; set; }
        public DbSet<ReminderAttachment> ReminderAttachments { get; set; }
        public DbSet<BusinessProfile> BusinessProfiles { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Product -> Specs relationship
            modelBuilder.Entity<Product>()
                .HasMany(p => p.Specs)
                .WithOne(s => s.Product)
                .HasForeignKey(s => s.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // Index for fast spec lookups
            modelBuilder.Entity<ProductSpec>()
                .HasIndex(s => new { s.ProductId, s.DisplayOrder });

            // Company -> Phones relationship
            modelBuilder.Entity<Company>()
                .HasMany(c => c.Phones)
                .WithOne(p => p.Company)
                .HasForeignKey(p => p.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);

            // Company -> Emails relationship
            modelBuilder.Entity<Company>()
                .HasMany(c => c.Emails)
                .WithOne(e => e.Company)
                .HasForeignKey(e => e.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);

            // Company -> Ledger Entries relationship
            modelBuilder.Entity<Company>()
                .HasMany(c => c.LedgerEntries)
                .WithOne(e => e.Company)
                .HasForeignKey(e => e.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);

            // Index for fast ledger lookups by company + date
            modelBuilder.Entity<LedgerEntry>()
                .HasIndex(e => new { e.CompanyId, e.EntryDate });

            // Company -> Reminder Schedules relationship
            modelBuilder.Entity<Company>()
                .HasMany(c => c.ReminderSchedules)
                .WithOne(r => r.Company)
                .HasForeignKey(r => r.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);

            // Reminder Schedule -> Attachments relationship
            modelBuilder.Entity<ReminderSchedule>()
                .HasMany(r => r.Attachments)
                .WithOne(a => a.ReminderSchedule)
                .HasForeignKey(a => a.ReminderScheduleId)
                .OnDelete(DeleteBehavior.Cascade);

            // Index for the scheduler's "what's due" poll
            modelBuilder.Entity<ReminderSchedule>()
                .HasIndex(r => new { r.IsActive, r.NextRunAt });
        }
    }
}