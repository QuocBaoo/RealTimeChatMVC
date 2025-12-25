using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System;

namespace RealTimeChatMVC.Models
{
    public class Message
    {
        [Key]
        public int Id { get; set; }

        public string Content { get; set; } // Nội dung
        
        public DateTime Timestamp { get; set; } = DateTime.Now;

        // Liên kết với bảng User
        public int SenderId { get; set; }
        
        [ForeignKey("SenderId")]
        public virtual User Sender { get; set; }
    }
}