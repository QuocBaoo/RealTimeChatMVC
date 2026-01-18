using System.ComponentModel.DataAnnotations;

namespace RealTimeChatMVC.Models
{
    public class Friend
    {
        [Key]
        public int Id { get; set; }

        public int RequesterId { get; set; } // Người gửi lời mời
        public int ReceiverId { get; set; }  // Người nhận

        public int Status { get; set; } // 0: Pending, 1: Accepted, 2: Blocked
    }
}