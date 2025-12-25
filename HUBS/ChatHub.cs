using Microsoft.AspNetCore.SignalR;
using RealTimeChatMVC.Models; // Đã sửa thành RealTimeChatMVC
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace RealTimeChatMVC.Hubs // Đã sửa thành RealTimeChatMVC
{
    // LƯU Ý: Dòng [Authorize] nghĩa là phải Đăng nhập mới được Chat.
    // Nếu bạn chưa làm chức năng Đăng nhập, tạm thời có thể comment dòng này lại bằng cách thêm // ở đầu:
    // [Authorize] 
    public class ChatHub : Hub
    {
        // 🔹 Danh sách user và nhóm (Lưu trên RAM)
        private static readonly ConcurrentDictionary<string, string> Users = new();
        private static readonly ConcurrentDictionary<string, ChatGroup> ChatGroups = new();

        // 🔹 Khi user kết nối
        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            var username = httpContext?.Request.Query["username"].ToString();

            // Nếu không có username trên URL thì lấy từ User đã đăng nhập (nếu có)
            if (string.IsNullOrEmpty(username))
            {
                username = Context.User?.Identity?.Name;
            }

            if (!string.IsNullOrEmpty(username))
            {
                Users[Context.ConnectionId] = username;

                // Tự động join vào các nhóm cũ
                foreach (var group in ChatGroups.Values)
                {
                    if (group.Members.Contains(username))
                        await Groups.AddToGroupAsync(Context.ConnectionId, group.Name);
                }

                await Clients.All.SendAsync("UserJoined", username);
                await Clients.All.SendAsync("UpdateUserList", Users.Values);
                await Clients.Caller.SendAsync("ReceiveGroups", ChatGroups.Values);
            }

            await base.OnConnectedAsync();
        }

        // 🔹 Khi user ngắt kết nối
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (Users.TryRemove(Context.ConnectionId, out var username))
            {
                await Clients.All.SendAsync("UserLeft", username);
                await Clients.All.SendAsync("UpdateUserList", Users.Values);
            }

            await base.OnDisconnectedAsync(exception);
        }

        // 🔹 Gửi tin nhắn Text
        public async Task SendMessage(string user, string message)
        {
            var msgObj = new
            {
                sender = user,
                content = message,
                type = "text",
                time = DateTime.Now.ToString("HH:mm:ss")
            };

            await Clients.All.SendAsync("ReceiveMessage", msgObj);
        }

        // 🔹 Gửi Sticker
        public async Task SendSticker(string user, string stickerUrl)
        {
            if (string.IsNullOrEmpty(stickerUrl) || !stickerUrl.StartsWith("/stickers/"))
            {
                await Clients.Caller.SendAsync("MessageError", "URL Sticker không hợp lệ.");
                return;
            }

            var msgObj = new
            {
                sender = user,
                content = stickerUrl,
                type = "sticker",
                time = DateTime.Now.ToString("HH:mm:ss")
            };

            await Clients.All.SendAsync("ReceiveMessage", msgObj);
        }

        // 🔹 Chat riêng (Private)
        public async Task SendPrivateMessage(string toUser, string fromUser, string message)
        {
            var targetConn = Users.FirstOrDefault(u => u.Value == toUser).Key;

            if (!string.IsNullOrEmpty(targetConn))
            {
                var msgObj = new
                {
                    sender = fromUser,
                    content = message,
                    type = "text",
                    time = DateTime.Now.ToString("HH:mm:ss")
                };

                await Clients.Client(targetConn).SendAsync("ReceivePrivateMessage", msgObj);
                await Clients.Caller.SendAsync("ReceivePrivateMessage", msgObj);
            }
        }

        // 🔹 Chat Nhóm
        public async Task SendGroupMessage(string groupName, string user, string message)
        {
            if (!ChatGroups.TryGetValue(groupName, out var group)) return;

            if (!group.Members.Contains(user))
            {
                await Clients.Caller.SendAsync("PermissionDenied", "Bạn không thuộc nhóm này!");
                return;
            }

            var msgObj = new
            {
                sender = user,
                group = groupName,
                content = message,
                type = "text",
                time = DateTime.Now.ToString("HH:mm:ss")
            };

            await Clients.Group(groupName).SendAsync("ReceiveGroupMessage", msgObj);
        }

        // 🔹 Tạo nhóm mới
        public async Task CreateGroup(string groupName, string description, string createdBy, string? avatar, List<string>? members, bool isPrivate, string pinCode)
        {
            if (ChatGroups.ContainsKey(groupName))
            {
                await Clients.Caller.SendAsync("GroupError", "Tên nhóm đã tồn tại!");
                return;
            }

            var group = new ChatGroup
            {
                Name = groupName,
                Description = description,
                Avatar = avatar ?? "/images/group-default.png",
                CreatedBy = createdBy,
                CreatedAt = DateTime.Now,
                IsPrivate = isPrivate,
                PinCode = pinCode,
                Members = new List<string> { createdBy },
                Admins = new List<string> { createdBy }
            };

            if (members != null)
            {
                foreach (var m in members)
                    if (!group.Members.Contains(m)) group.Members.Add(m);
            }

            if (ChatGroups.TryAdd(groupName, group))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
                await Clients.All.SendAsync("GroupCreated", group);
            }
        }

        // 🔹 Vào nhóm
        public async Task JoinGroup(string groupName, string username, string? pinInput = null)
        {
            if (!ChatGroups.TryGetValue(groupName, out var group))
            {
                await Clients.Caller.SendAsync("JoinFailed", "Nhóm không tồn tại.");
                return;
            }

            if (group.IsPrivate && group.PinCode != pinInput)
            {
                await Clients.Caller.SendAsync("JoinFailed", "Mã PIN không đúng.");
                return;
            }

            if (!group.Members.Contains(username)) group.Members.Add(username);

            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            await Clients.Group(groupName).SendAsync("UserJoinedGroup", username, groupName);
            await Clients.Caller.SendAsync("JoinedGroup", group);
        }

        // 🔹 Gửi file trong nhóm
        public async Task SendGroupFile(string groupName, string user, string fileName, string base64Data)
        {
            if (!ChatGroups.TryGetValue(groupName, out var group)) return;

            try
            {
                var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", groupName);
                Directory.CreateDirectory(uploadDir);

                var filePath = Path.Combine(uploadDir, fileName);
                // Xử lý Base64 để lưu thành file ảnh/tài liệu thật
                var cleanBase64Data = base64Data.Contains(',') ? base64Data.Substring(base64Data.IndexOf(',') + 1) : base64Data;
                await File.WriteAllBytesAsync(filePath, Convert.FromBase64String(cleanBase64Data));

                var fileUrl = $"/uploads/{groupName}/{fileName}";
                var ext = Path.GetExtension(fileName).ToLower();
                var fileType = (new[] { ".jpg", ".jpeg", ".png", ".gif" }.Contains(ext)) ? "image" : "file";

                var msgObj = new
                {
                    sender = user,
                    group = groupName,
                    content = fileUrl,
                    type = fileType,
                    fileName = fileName,
                    time = DateTime.Now.ToString("HH:mm:ss")
                };

                await Clients.Group(groupName).SendAsync("ReceiveGroupMessage", msgObj);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi upload: {ex.Message}");
            }
        }
    }
}