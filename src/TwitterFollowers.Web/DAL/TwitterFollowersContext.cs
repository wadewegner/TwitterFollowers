using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Linq;
using System.Web;
using TwitterFollowers.Web.Models;

namespace TwitterFollowers.Web.DAL
{
    public class TwitterFollowersContext : DbContext
    {
        public TwitterFollowersContext()
            : base("TwitterFollowersContext")
        {
            Database.SetInitializer(new CreateDatabaseIfNotExists<TwitterFollowersContext>());
            Database.SetInitializer(new DropCreateDatabaseIfModelChanges<TwitterFollowersContext>());
        }

        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
        }
    }
}