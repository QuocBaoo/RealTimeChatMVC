"use strict";

// 1. Khởi tạo kết nối SignalR
var connection = new signalR.HubConnectionBuilder().withUrl("/chatHub").build();

// Tắt nút gửi cho đến khi kết nối thành công
document.getElementById("sendButton").disabled = true;

// --- HÀM HỖ TRỢ: Thêm tin nhắn vào giao diện ---
function appendMessage(user, message, time) {
    var chatBox = document.getElementById("chatBox");
    var isMine = (user === currentUser);
    
    var divItem = document.createElement("div");
    divItem.className = isMine ? "message-item msg-right" : "message-item msg-left";
    
    // Fix bảo mật: Dùng textContent cho message để tránh XSS
    divItem.innerHTML = `
        <div class="message-content"></div>
        <div class="message-info">${isMine ? "Bạn" : user} • ${time}</div>
    `;
    divItem.querySelector(".message-content").textContent = message;

    chatBox.appendChild(divItem);
    scrollToBottom();
}

function scrollToBottom() {
    var chatBox = document.getElementById("chatBox");
    chatBox.scrollTop = chatBox.scrollHeight;
}

// --- SIGNALR EVENTS ---

// 2. Nhận tin nhắn từ Server (Real-time)
connection.on("ReceiveMessage", function (user, message, time) {
    appendMessage(user, message, time);
});

// 3. Bắt đầu kết nối
connection.start().then(function () {
    document.getElementById("sendButton").disabled = false;
    console.log("SignalR Connected!");
}).catch(function (err) {
    return console.error(err.toString());
});

// ================= ONLINE USERS =================
connection.on("OnlineUsersSnapshot", function (users) {
    console.log("ONLINE SNAPSHOT:", users);
    renderOnlineUsers(users);
});

function renderOnlineUsers(users) {
    const list = document.getElementById("onlineUsers");
    if (!list) {
        console.warn("Không tìm thấy element #onlineUsers");
        return;
    }

    list.innerHTML = "";

    users.forEach(u => {
        const li = document.createElement("li");
        li.textContent = u.username;
        li.dataset.userid = u.id;
        list.appendChild(li);
    });
}

// --- DOM EVENTS ---

// 4. Xử lý nút Gửi
document.getElementById("sendButton").addEventListener("click", function (event) {
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
document.getElementById("messageInput").addEventListener("keyup", function(event) {
    if (event.key === "Enter") {
        document.getElementById("sendButton").click();
    }
});

// --- API CALLS ---

// 6. Load lịch sử tin nhắn khi trang vừa tải
document.addEventListener("DOMContentLoaded", function() {
    fetch('/Chat/GetHistory')
        .then(response => response.json())
        .then(data => {
            data.forEach(msg => {
                appendMessage(msg.user, msg.message, msg.time);
            });
        })
        .catch(error => console.error('Lỗi tải lịch sử:', error));
});
