# 🐛 Debug Checklist - Tải Tin Nhắn

## 1. Kiểm tra API

Mở DevTools → Network tab:

- [ ] Gửi request: `GET /Chat/GetHistory`
- [ ] Status: `200` (OK)
- [ ] Response body có tin nhắn không?

## 2. Kiểm tra Console

Mở DevTools → Console tab:

```
Đảo tìm:
✅ "Joining global chat..." - Hàm được gọi
✅ "Response status: 200" - API trả về thành công
✅ "Loaded messages: [...]" - Có dữ liệu
✅ "Appending message: {...}" - Dữ liệu tin nhắn
```

❌ Nếu thấy lỗi:

```
- "Lỗi tải lịch sử: ..." - API fail
- "Cannot read property 'forEach'" - Dữ liệu format sai
- HTTP 404 - Endpoint không tồn tại
```

## 3. Restart Server

```bash
dotnet build
dotnet run
```

## 4. Test

- [ ] F5 refresh
- [ ] Mở DevTools Console
- [ ] Quan sát các log
- [ ] Xem có tin nhắn không?

---

**Nguyên nhân có thể:**

1. ❌ API `/Chat/GetHistory` không tồn tại → **FIX: Đã thêm**
2. ❌ Database không có tin nhắn → Tạo tin nhắn rồi test
3. ❌ Error từ API → Xem Console error message
4. ❌ Timing issue → `setTimeout()` sẽ giải quyết
