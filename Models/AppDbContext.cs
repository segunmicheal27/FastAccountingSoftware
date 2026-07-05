using Microsoft.EntityFrameworkCore;
using System.IO;
using System;

namespace FastAccountingSoftware.Models
{
    public class AppDbContext : DbContext
    {
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<StaffMember> Staff { get; set; }
        public DbSet<PayrollRun> PayrollRuns { get; set; }
        public DbSet<CompanyProfile> CompanyProfiles { get; set; }
        public DbSet<HmoProvider> HmoProviders { get; set; }
        public DbSet<InventoryItem> InventoryItems { get; set; }

        public AppDbContext()
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Use local AppData directory for the database file
            string folder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string dbPath = Path.Join(folder, "fast_accounting.db");
            
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
        }
    }
}
