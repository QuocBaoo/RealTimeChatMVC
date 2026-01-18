using System;
using System.ComponentModel.DataAnnotations;

namespace RealTimeChatMVC.Models
{
    public class OnlineUser
    {
        [Key]
        public string ConnectionId { get; set; }
        
        public int UserId { get; set; }
        public string Username { get; set; }
        public DateTime LoginTime { get; set; } = DateTime.Now;
    }
}