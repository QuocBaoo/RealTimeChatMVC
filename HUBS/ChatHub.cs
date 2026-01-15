using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using RealTimeChatMVC.Data;
using RealTimeChatMVC.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RealTimeChatMVC.Hubs
{
    [Authorize] // Bắt buộc đăng nhập mới được vào
    public class ChatHub : Hub
    {
        // 1. Khai báo Database (Để lưu tin nhắn vĩnh viễn)
        private readonly ChatDbContext _context;

        // 2. Khai báo Bộ nhớ tạm (RAM) để quản lý danh sách Online và Nhóm
        private static readonly ConcurrentDictionary<string, string> Users = new();
        private static readonly ConcurrentDictionary<string, ChatGroup> ChatGroups = new();

        // Inject Database vào Hub
        public ChatHub(ChatDbContext context)
        {
            _context = context;
        }

        // -----------------------------------------------------------------------
        // PHẦN 1: QUẢN LÝ KẾT NỐI (ON/OFFLINE)
        // -----------------------------------------------------------------------
        public override async Task OnConnectedAsync()
        {
            // Lấy tên người dùng từ Cookie đăng nhập
            string username = Context.User.Identity.Name;

            // Lưu vào danh sách Online
            Users.TryAdd(Context.ConnectionId, username);

            // Báo cho mọi người biết
            await Clients.All.SendAsync("UserJoined", username);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            // Xóa khỏi danh sách Online
            if (Users.TryRemove(Context.ConnectionId, out var username))
            {
                await Clients.All.SendAsync("UserLeft", username);
            }
            await base.OnDisconnectedAsync(exception);
        }

        // -----------------------------------------------------------------------
        // PHẦN 2: CHAT CHUNG (CÓ LƯU DATABASE - QUAN TRỌNG)
        // -----------------------------------------------------------------------
        public async Task SendMessage(string user, string message)
        {
            // Để an toàn, ta lấy tên thật từ hệ thống đăng nhập
            string realUser = Context.User.Identity.Name;

            // 1. Lưu vào SQL Server (Phần của Dũng)
            var msgEntity = new Message
            {
                SenderName = realUser,
                Content = message,
                Timestamp = DateTime.UtcNow.AddHours(7) // Fix: Giờ Việt Nam
            };

            _context.Messages.Add(msgEntity);
            await _context.SaveChangesAsync(); // Lưu vĩnh viễn

            // 2. Gửi ra cho mọi người (Kèm giờ giấc)
            // Format gửi về: User, Message, Time (Khớp với chat.js mới nhất)
            await Clients.All.SendAsync("ReceiveMessage", realUser, message, msgEntity.Timestamp.ToString("HH:mm:ss"));
        }

        // -----------------------------------------------------------------------
        // PHẦN 3: TÍNH NĂNG NÂNG CAO (GROUP, PRIVATE, STICKER)
        // (Tạm thời xử lý trên RAM, chưa lưu SQL để tránh lỗi DB phức tạp)
        // -----------------------------------------------------------------------

        // Gửi Sticker
        public async Task SendSticker(string user, string stickerUrl)
        {
            await Clients.All.SendAsync("ReceiveMessage", user, stickerUrl, DateTime.UtcNow.AddHours(7).ToString("HH:mm:ss"));
        }

        // Chat Riêng (Private)
        public async Task SendPrivateMessage(string toUser, string message)
        {
            string fromUser = Context.User.Identity.Name;
            
            // Tìm ConnectionId của người nhận
            var targetConn = Users.FirstOrDefault(u => u.Value == toUser).Key;

            if (!string.IsNullOrEmpty(targetConn))
            {
                // Gửi cho người nhận
                await Clients.Client(targetConn).SendAsync("ReceivePrivateMessage", fromUser, message, DateTime.UtcNow.AddHours(7).ToString("HH:mm:ss"));
                // Gửi lại cho chính mình (để hiện lên màn hình mình)
                await Clients.Caller.SendAsync("ReceivePrivateMessage", fromUser, message, DateTime.UtcNow.AddHours(7).ToString("HH:mm:ss"));
            }
        }

        // Tạo nhóm
        public async Task CreateGroup(string groupName)
        {
            string creator = Context.User.Identity.Name;
            var newGroup = new ChatGroup { Name = groupName, CreatedBy = creator };
            
            if (ChatGroups.TryAdd(groupName, newGroup))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
                await Clients.All.SendAsync("GroupCreated", groupName);
            }
        }

        // Chat trong nhóm
        public async Task SendGroupMessage(string groupName, string message)
        {
            string user = Context.User.Identity.Name;
            // Gửi tin nhắn vào nhóm cụ thể
            await Clients.Group(groupName).SendAsync("ReceiveGroupMessage", user, groupName, message, DateTime.UtcNow.AddHours(7).ToString("HH:mm:ss"));
        }

        // Vào nhóm
        public async Task JoinGroup(string groupName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            await Clients.Group(groupName).SendAsync("UserJoinedGroup", Context.User.Identity.Name, groupName);
        }
    }
}