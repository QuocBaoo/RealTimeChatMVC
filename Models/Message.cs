using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RealTimeChatMVC.Models
{
    public class Message
    {
        [Key]
        public int Id { get; set; }

        public string SenderName { get; set; } // Tên người gửi

        [Required]
        public string Content { get; set; } // Nội dung tin nhắn

        public DateTime Timestamp { get; set; } = DateTime.Now;

        public string Type { get; set; } = "Text"; // Text, Image, File...

        public int? ChatGroupId { get; set; } // Null nếu là chat chung (Global)

        [ForeignKey("ChatGroupId")]
        public virtual ChatGroup ChatGroup { get; set; }

        public int? ToUserId { get; set; } // [MỚI] ID người nhận (nếu chat riêng)
    }
}