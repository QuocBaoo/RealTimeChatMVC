using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations; // Quan trọng: Để dùng được [Key]

namespace RealTimeChatMVC.Models
{
    public class ChatGroup
    {
        [Key] // Khóa chính
        public string Name { get; set; }
        
        public string Description { get; set; }
        public string Avatar { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsPrivate { get; set; }
        public string PinCode { get; set; }
        
        public List<string> Members { get; set; } = new List<string>();
        public List<string> Admins { get; set; } = new List<string>();
    }
}