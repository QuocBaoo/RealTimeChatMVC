# 💬 RealTimeChatMVC

> **Đồ Án Cuối Kỳ Môn Lập Trình Mạng**
>
> Ứng dụng chat thời gian thực đa nền tảng, tốc độ cao, sử dụng công nghệ SignalR.

---

## 🚀 Tính Năng Chính
*   **Chat 1-1 & Chat Nhóm:** Gửi tin nhắn tức thì, độ trễ cực thấp.
*   **Trạng Thái Online:** Cập nhật real-time ai đang online/offline.
*   **Chia Sẻ File:** Hỗ trợ gửi ảnh, video, tài liệu lên đến 100MB.
*   **Thông Báo:** Nhận thông báo tin nhắn ngay lập tức.
*   **Bảo Mật:** Xác thực người dùng an toàn.

---

## 💻 Công Nghệ & Môi Trường

| Thành Phần | Công Nghệ / Công Cụ |
| :--- | :--- |
| **Backend** | ASP.NET Core 8.0, C# |
| **Real-time** | SignalR (WebSocket) |
| **Frontend** | Razor Views, JavaScript, Bootstrap 5 |
| **Database** | SQL Server 2022, Entity Framework Core |
| **Hạ Tầng** | Docker, Docker Compose |
| **IDE** | Visual Studio 2022 / VS Code |

> **Kiến thức áp dụng:** WebSocket, Xử lý đồng thời (Concurrency), Lập trình bất đồng bộ (Async/Await), Quản lý State.

---

## ⚙️ Hướng Dẫn Cài Đặt & Chạy

Bạn có thể chạy dự án dễ dàng theo 2 cách dưới đây:

### 🌟 Cách 1: Chạy Bằng Docker (Khuyên Dùng)
Cách này nhanh nhất, không cần cài đặt SQL Server hay môi trường phức tạp.

1.  **Mở Terminal** tại thư mục dự án.
2.  **Khởi chạy** bằng lệnh sau:
    ```bash
    docker-compose up -d --build
    ```
3.  **Truy cập**: Mở trình duyệt vào `http://localhost:5000`

### 🛠️ Cách 2: Chạy Thủ Công (Visual Studio)
Dành cho việc phát triển (Dev) hoặc Debug.

1.  **Cấu hình Database**: Mở `appsettings.json` và sửa `DefaultConnection` cho đúng với SQL Server của bạn.
2.  **Khởi tạo Database**:
    ```bash
    dotnet ef database update
    ```
3.  **Chạy dự án**: Nhấn nút **Play** (▶) trong Visual Studio hoặc gõ:
    ```bash
    dotnet run
    ```
4.  **Truy cập**: Vào địa chỉ `https://localhost:7123` (hoặc port hiển thị).

---

## 📂 Cấu Trúc Dự Án
*   `Hubs/ChatHub.cs`: Trái tim xử lý tín hiệu Real-time.
*   `Controllers/`: Xử lý Logic API và điều hướng.
*   `Views/`: Giao diện người dùng.
*   `wwwroot/`: File tĩnh (CSS, JS, Ảnh).

---
*Developed by [Tên Của Bạn] - [MSSV]*
