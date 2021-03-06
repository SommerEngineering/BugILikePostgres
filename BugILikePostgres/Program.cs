﻿using System;
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
                await db.Blogs.AddAsync(new Blog { Id = 1, Name = "Test 1", Topics = new List<string> { "Interesting", "Big Data", "Artificial Intelligence",}});
                await db.Blogs.AddAsync(new Blog { Id = 2, Name = "Test 2", Topics = new List<string> { "Interesting", "Smart Cities", "Artificial Topologies",}});
                await db.Blogs.AddAsync(new Blog { Id = 3, Name = "Other 3", Topics = new List<string> { "Not Interesting",}});
            }

            if (!await db.Topics.AnyAsync())
            {
                await db.Topics.AddAsync(new Topic{Id = 1, Name = "Artificial Intelligence"});
                await db.Topics.AddAsync(new Topic{Id = 2, Name = "Smart Cities"});
                await db.Topics.AddAsync(new Topic{Id = 3, Name = "Big Data"});
                await db.Topics.AddAsync(new Topic{Id = 4, Name = "Computer Science"});
                await db.Topics.AddAsync(new Topic{Id = 5, Name = "artificial intelligence"});
            }

            if (!await db.Users.AnyAsync())
            {
                await db.Users.AddAsync(new User { Id = 1, Name = "Tom", Topics = new []{1, 3}});
                await db.Users.AddAsync(new User { Id = 2, Name = "Joe", Topics = new []{2}});
                await db.Users.AddAsync(new User { Id = 3, Name = "Maria", Topics = new []{5, 3, 2}});
                await db.Users.AddAsync(new User { Id = 4, Name = "Julia", Topics = new []{3, 1, 5}});
            }

            await db.SaveChangesAsync();

            #endregion
            
            // Attempt 1: Partial search for "Artificial Intelligence" by means of ILike (or Like)
            // Matches: 1 entry
            // Result: System.NullReferenceException: Object reference not set to an instance of an object.
            try
            {
                var searchTerm1 = "intelli";
                
                // The complete example, simulating our real use case
                var result1 = db.Blogs.Where(n => n.Name.Contains("Test")).OrderBy(n => n.Id).Where(n => n.Topics.Any(s => EF.Functions.ILike(s, $"%{searchTerm1}%")));
                
                // This does not work at well (simpler, just one where clause)
                //var result1 = db.Blogs.Where(n => n.Topics.Any(s => EF.Functions.ILike(s, $"%{searchTerm1}%")));
                
                Console.WriteLine("Result attempt 1:");
                foreach (var blog in result1)
                    Console.WriteLine($"[{blog.Id}] name={blog.Name}, topics={string.Join(", ", blog.Topics)}");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                Console.WriteLine();
            }

            // Attempt 2: Search for exact matches
            // Matches: 2 entries
            // Result: works fine
            try
            {
                var searchTerm2 = "Interesting";
                var result2 = db.Blogs.Where(n => n.Name.Contains("Test")).OrderBy(n => n.Id).Where(n => n.Topics.Contains(searchTerm2));
                Console.WriteLine("Result attempt 2:");
                foreach (var blog in result2)
                    Console.WriteLine($"[{blog.Id}] name={blog.Name}, topics={string.Join(", ", blog.Topics)}");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                Console.WriteLine();
            }
            
            // Attempt 3: Partial search for "Artificial Intelligence" by means of Any
            // Matches: 1 entry
            // Result: The LINQ expression [...] could not be translated.
            try
            {
                var searchTerm3 = "intelli";
                var result3 = db.Blogs.Where(n => n.Name.Contains("Test")).OrderBy(n => n.Id).Where(n => n.Topics.Any(s => s.Contains(searchTerm3)));
                Console.WriteLine("Result attempt 3:");
                foreach (var blog in result3)
                    Console.WriteLine($"[{blog.Id}] name={blog.Name}, topics={string.Join(", ", blog.Topics)}");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                Console.WriteLine();
            }
            
            // Attempt 4: (Partial) search for "Artificial Intelligence" by means of ILike (or Like) where matchExpress & pattern gets exchanged
            // Matches: 1 entry
            // Result: "works", but makes no sense (or might we miss something?), because the search term gets used as matchExpression...
            try
            {
                var searchTerm4a = "intelli"; // partial search not possible, because we cannot use patterns :(
                var searchTerm4b = "artificial intelligence"; // at least, ilike works :)
                var result4 = db.Blogs.Where(n => n.Name.Contains("Test")).OrderBy(n => n.Id).Where(n => n.Topics.Any(s => EF.Functions.ILike(searchTerm4b, s)));
                Console.WriteLine("Result attempt 4:");
                foreach (var blog in result4)
                    Console.WriteLine($"[{blog.Id}] name={blog.Name}, topics={string.Join(", ", blog.Topics)}");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                Console.WriteLine();
            }
            
            // Attempt 5: using an additional table for the text and array columns to store ids pointing to them (pulling ids to C# client)
            // Matches: 3
            // Result: Works, when matching topic ids are pulled to the C# side. Does not work completely on db side (see attempt 6).
            try
            {
                var searchTerm5 = "in";
                var matchingTopicIds = db.Topics.Where(n => EF.Functions.ILike(n.Name, $"%{searchTerm5}%")).Select(n => n.Id).ToArray();
                var matchingUsers = db.Users.Where(n => n.Name.Length > 2).OrderBy(n => n.Name).Where(n => n.Topics.Any(i => matchingTopicIds.Contains(i)));
                
                Console.WriteLine("Result attempt 5:");
                foreach (var topic in matchingTopicIds)
                    Console.WriteLine($"matching topic id={topic}");
                foreach (var user in matchingUsers)
                    Console.WriteLine($"[{user.Id}] name={user.Name}, topics={string.Join(", ", user.Topics)}");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                Console.WriteLine();
            }
            
            // Attempt 6: using an additional table for the text and array columns to store ids pointing to them (db side)
            // Matches: 3
            // Result: Does not work...
            try
            {
                var searchTerm6 = "in";
                var matchingTopicIds = db.Topics.Where(n => EF.Functions.ILike(n.Name, $"%{searchTerm6}%")).Select(n => n.Id);
                var matchingUsers = db.Users.Where(n => n.Name.Length > 2).OrderBy(n => n.Name).Where(n => n.Topics.Any(i => matchingTopicIds.Contains(i)));
                
                Console.WriteLine("Result attempt 6:");
                foreach (var topic in matchingTopicIds)
                    Console.WriteLine($"matching topic id={topic}");
                foreach (var user in matchingUsers)
                    Console.WriteLine($"[{user.Id}] name={user.Name}, topics={string.Join(", ", user.Topics)}");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                Console.WriteLine();
            }
        }
    }
    
    public class Database : DbContext
    {
        public DbSet<Blog> Blogs { get; set; }

        public DbSet<User> Users { get; set; }

        public DbSet<Topic> Topics { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) => optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=BugILike;Username=tester;Password=test");

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Blog>().HasKey(n => n.Id);
            modelBuilder.Entity<Blog>().HasIndex(n => n.Name);
            modelBuilder.Entity<Blog>().HasIndex(n => n.Topics);

            modelBuilder.Entity<User>().HasKey(n => n.Id);
            modelBuilder.Entity<User>().HasIndex(n => n.Name);
            modelBuilder.Entity<User>().HasIndex(n => n.Topics);
            modelBuilder.Entity<Topic>().HasKey(n => n.Id);
            modelBuilder.Entity<Topic>().HasIndex(n => n.Name);
        }
    }
    
    public class Blog
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<string> Topics { get; set; }
    }

    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int[] Topics { get; set; }
    }

    public class Topic
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}