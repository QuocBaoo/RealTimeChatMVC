using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace RealTimeChatMVC.Models
{
    public class ChatGroup
    {
        [Key]
        public int Id { get; set; } // Dùng ID số làm khóa chính (Chuẩn SQL)

        [Required(ErrorMessage = "Tên nhóm không được để trống")]
        [StringLength(100)]
        public string Name { get; set; } // Tên nhóm

        public string Description { get; set; } = "Nhóm chat vui vẻ"; // Mô tả (Mặc định)
        
        public string Avatar { get; set; } // Link ảnh nhóm (nếu có)

        public string CreatedBy { get; set; } // Người tạo (Lưu Username)
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // --- CÁC TÍNH NĂNG BẢO MẬT ---
        public bool IsPrivate { get; set; } = false; // Có phải nhóm kín không?
        
        [StringLength(10)]
        public string PinCode { get; set; } // Mật khẩu vào nhóm (Nếu là Private)

        // --- QUAN HỆ DATABASE (Thay cho List<string>) ---
        // Một nhóm có thể chứa nhiều tin nhắn
        // public virtual ICollection<Message> Messages { get; set; } 
    }
}