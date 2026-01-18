using System.Collections.Generic;

namespace RealTimeChatMVC.Models
{
    public class User
    {
        public int Id { get; set; }

        public string Username { get; set; } // Bắt buộc

        public string Password { get; set; } // Bắt buộc

        public string FullName { get; set; } // Tùy chọn

        public string? Email { get; set; }

        public string AvatarColor { get; set; } = "#1A2980"; // Màu avatar cố định

        // Danh sách nhóm user đã tham gia
        public virtual ICollection<ChatGroup> Groups { get; set; } = new List<ChatGroup>();
    }
}