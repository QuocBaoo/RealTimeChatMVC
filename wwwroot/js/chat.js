"use strict";

// 1. Khởi tạo kết nối SignalR
var connection = new signalR.HubConnectionBuilder().withUrl("/chatHub").build();

// Tắt nút gửi cho đến khi kết nối thành công
document.getElementById("sendButton").disabled = true;

// --- HÀM HỖ TRỢ: Thêm tin nhắn vào giao diện ---
function appendMessage(user, message, time, type = "Text") {
  var chatBox = document.getElementById("chatBox");
  var isMine = user === currentUser;

  // Debug: Kiểm tra type được truyền
  console.log("appendMessage called with:", { user, message, time, type });

  var divItem = document.createElement("div");
  divItem.className = isMine
    ? "message-item msg-right"
    : "message-item msg-left";

  var contentDiv = document.createElement("div");
  contentDiv.className = "message-content";

  // Xử lý hiển thị dựa trên loại tin nhắn
  if (type === "File" || type === "Image") {
    // Nếu là link file/ảnh
    if (message.startsWith("/uploads/")) {
      if (type === "Image") {
        // Hiển thị ảnh
        var img = document.createElement("img");
        img.src = message;
        img.style.maxWidth = "250px";
        img.style.maxHeight = "250px";
        img.style.borderRadius = "10px";
        contentDiv.appendChild(img);
      } else {
        // Hiển thị link file
        var fileName = message.split("/").pop();
        var link = document.createElement("a");
        link.href = message;
        link.download = "";
        link.textContent = "📎 " + fileName;
        link.target = "_blank";
        link.style.color = "#1A2980";
        link.style.textDecoration = "underline";
        contentDiv.appendChild(link);
      }
    } else {
      // Fallback nếu không phải đường dẫn
      contentDiv.textContent = message;
    }
  } else {
    // Tin nhắn text thường
    contentDiv.textContent = message;
  }

  divItem.appendChild(contentDiv);

  // Thêm thông tin thời gian
  var infoDiv = document.createElement("div");
  infoDiv.className = "message-info";
  infoDiv.textContent = (isMine ? "Bạn" : user) + " • " + time;
  divItem.appendChild(infoDiv);

  chatBox.appendChild(divItem);
  scrollToBottom();
}

function scrollToBottom() {
  var chatBox = document.getElementById("chatBox");
  chatBox.scrollTop = chatBox.scrollHeight;
}

// --- SIGNALR EVENTS ---

// 2. Nhận tin nhắn từ Server (Real-time)
connection.on("ReceiveMessage", function (user, message, time, type) {
  appendMessage(user, message, time, type || "Text");
});

// 3. Bắt đầu kết nối
connection
  .start()
  .then(function () {
    document.getElementById("sendButton").disabled = false;
    console.log("SignalR Connected!");
  })
  .catch(function (err) {
    return console.error(err.toString());
  });

// ================= ONLINE USERS =================
connection.on("OnlineUsersSnapshot", function (users) {
  console.log("ONLINE SNAPSHOT:", users);
  renderOnlineUsers(users);
});

function renderOnlineUsers(users) {
  var listHtml = "";

  // Loop qua danh sách user đang online
  users.forEach((u) => {
    // Server trả về object có dạng { id, username }
    var name = u.username || u.Username;
    var id = u.id || u.Id;

    // Không hiển thị chính mình trong danh sách online
    if (name === currentUser) return;

    // Tạo màu avatar nếu chưa có
    if (!userColorMap[name]) {
      userColorMap[name] =
        "#" + Math.floor(Math.random() * 16777215).toString(16);
    }

    // [FIX] Gọi đúng hàm renderUserItem của bạn
    listHtml += renderUserItem({ username: name, id: id });
  });

  // [QUAN TRỌNG] Sửa id="userList" thành id="onlineUsers" để khớp với HTML
  var listElement = document.getElementById("onlineUsers");
  if (listElement) {
    listElement.innerHTML = listHtml;
  } else {
    console.error("Không tìm thấy thẻ có id='onlineUsers'");
  }
}
// --- DOM EVENTS ---

// 4. Xử lý nút Gửi
document
  .getElementById("sendButton")
  .addEventListener("click", function (event) {
    var input = document.getElementById("messageInput");
    var message = input.value;

    if (message.trim() !== "") {
      // Gọi hàm SendMessage bên Hub (Server)
      // Tham số đầu tiên là user (để trống vì Server tự lấy từ Context), tham số 2 là message
      connection.invoke("SendMessage", "", message).catch(function (err) {
        return console.error(err.toString());
      });
      input.value = "";
      input.focus();
    }
    event.preventDefault(); // Chặn reload trang
  });

// 5. Bấm Enter để gửi
document
  .getElementById("messageInput")
  .addEventListener("keyup", function (event) {
    if (event.key === "Enter") {
      document.getElementById("sendButton").click();
    }
  });

// --- API CALLS ---

// 6. Load lịch sử tin nhắn - LOẠI BỎ vì joinGlobalChat() sẽ load
// (Nếu cần load khi trang vừa mở, dùng joinGlobalChat() trong window.onload thay vì)
