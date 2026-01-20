using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace RealTimeChatMVC.Providers
{
    public class CustomUserIdProvider : IUserIdProvider
    {
        public string? GetUserId(HubConnectionContext connection)
        {
            // Lấy UserId từ Claim "UserId" mà bạn đã lưu khi đăng nhập
            // Nếu không tìm thấy, fallback về NameIdentifier (mặc định)
            return connection.User?.FindFirst("UserId")?.Value 
                   ?? connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
    }
}