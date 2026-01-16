using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RealTimeChatMVC.Models
{
    public class Message
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string SenderName { get; set; } // Tên người gửi

        [Required]
        public string Content { get; set; }    // Nội dung tin nhắn

        public DateTime Timestamp { get; set; } = DateTime.Now; // Thời gian gửi

        // --- QUAN HỆ VỚI NHÓM CHAT ---
        // ID của nhóm chat (Nullable: nếu null thì là chat chung/global)
        public int? ChatGroupId { get; set; }

        // Navigation property
        public virtual ChatGroup ChatGroup { get; set; }
    }
}