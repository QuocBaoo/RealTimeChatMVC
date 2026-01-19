using System;
using System.ComponentModel.DataAnnotations;

namespace RealTimeChatMVC.Models
{
    public class GroupInvitation
    {
        [Key]
        public int Id { get; set; }
        public int GroupId { get; set; }
        public int InviterId { get; set; } // Người mời
        public int InviteeId { get; set; } // Người được mời
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}