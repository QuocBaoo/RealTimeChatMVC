using Microsoft.EntityFrameworkCore;
using RealTimeChatMVC.Models;

namespace RealTimeChatMVC.Data
{
    public class ChatDbContext : DbContext
    {
        public ChatDbContext(DbContextOptions<ChatDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<ChatGroup> ChatGroups { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Friend> Friends { get; set; }
        public DbSet<OnlineUser> OnlineUsers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Config many-to-many for Group Members
            modelBuilder.Entity<ChatGroup>()
                .HasMany(g => g.Members)
                .WithMany(u => u.Groups);
        }
    }
}