using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace coco
{
    public class AppDbContext :DbContext
    {
        public AppDbContext(DbContextOptions options): base(options)
        {
            InitalizeContext();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseInMemoryDatabase(databaseName: "BankDb");
            

            base.OnConfiguring(optionsBuilder);
        }

        protected virtual void InitalizeContext()
        {
            ChangeTracker.AutoDetectChangesEnabled = false;
            ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        }



        public DbSet<CustomerDetails> CustomerDetails { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {


            modelBuilder.Entity<CustomerDetails>().HasKey(u => new {u.CustomerId});
         

            base.OnModelCreating(modelBuilder);
        }
    }
}
