using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace BugILikePostgres
{
    internal static class Program
    {
        private static async Task Main(string[] args)
        {
            await using var db = new Database();

            #region Set up and preparation

            await db.Database.MigrateAsync();
            if (!await db.Blogs.AnyAsync())
            {
                await db.Blogs.AddAsync(new Blog
                {
                    Id = 1,
                    Name = "Test 1",
                    Topics = new List<string>
                    {
                        "Interesting",
                        "Big Data",
                        "Artificial Intelligence",
                    },
                });

                await db.Blogs.AddAsync(new Blog
                {
                    Id = 2,
                    Name = "Test 2",
                    Topics = new List<string>
                    {
                        "Interesting",
                        "Smart Cities",
                        "Artificial Topologies",
                    },
                });
                
                await db.Blogs.AddAsync(new Blog
                {
                    Id = 3,
                    Name = "Other 3",
                    Topics = new List<string>
                    {
                        "Not Interesting",
                    },
                });
            }

            await db.SaveChangesAsync();

            #endregion

            var searchTerm = "intelli";
            var result = db.Blogs.Where(n => n.Name.Contains("Test")).OrderBy(n => n.Id).Where(n => n.Topics.Any(s => EF.Functions.ILike(s, $"%{searchTerm}%")));
            foreach (var blog in result)
                Console.WriteLine($"[{blog.Id}] name={blog.Name}, topics={string.Join(", ", blog.Topics)}");
        }
    }
    
    public class Database : DbContext
    {
        public DbSet<Blog> Blogs { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) => optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=BugILike;Username=tester;Password=test");

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Blog>().HasKey(n => n.Id);
            modelBuilder.Entity<Blog>().HasIndex(n => n.Name);
            modelBuilder.Entity<Blog>().HasIndex(n => n.Topics);
        }
    }
    
    public class Blog
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<string> Topics { get; set; }
    }
}