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
    }
}