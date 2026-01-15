namespace RealTimeChatMVC.Models
{
    public class User
    {
        public int Id { get; set; }
        
        public string Username { get; set; } // Bắt buộc
        
        public string Password { get; set; } // Bắt buộc
        
        public string FullName { get; set; } // Tùy chọn
        
        public string? Email { get; set; } 
    }
}