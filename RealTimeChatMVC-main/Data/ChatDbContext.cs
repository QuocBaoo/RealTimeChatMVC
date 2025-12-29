using Microsoft.EntityFrameworkCore;
using RealTimeChatMVC.Models; // Để dùng được User, Message, ChatGroup

namespace RealTimeChatMVC.Data
{
    public class ChatDbContext : DbContext
    {
        public ChatDbContext(DbContextOptions<ChatDbContext> options) : base(options) { }

        // Danh sách các bảng trong Database
        public DbSet<User> Users { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<ChatGroup> ChatGroups { get; set; }
    }
}