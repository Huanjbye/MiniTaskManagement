# Kịch bản Testing cho dự án Mini Task Management

## 1. Mục đích

Tạo một kịch bản kiểm thử chức năng cho hệ thống Mini Task Management nhằm xác minh các luồng chính của người dùng hoạt động đúng trên môi trường test.

## 2. Phạm vi kiểm thử

Kiểm thử các chức năng chính sau:

- Đăng ký tài khoản
- Đăng nhập hệ thống
- Xem dashboard
- Tạo công việc mới
- Tìm kiếm và lọc công việc
- Chỉnh sửa và xóa công việc
- Xuất file CSV

## 3. Môi trường kiểm thử

### 3.1 Phần mềm cần có

- Visual Studio / VS Code
- .NET SDK 10.0
- Node.js và npm
- PostgreSQL
- Browser: Google Chrome hoặc Microsoft Edge
- Git
- Postman (khuyến nghị để kiểm tra API)

### 3.2 Công cụ hỗ trợ

- HP-UFT / UFT One (nếu áp dụng tự động hóa giao diện)
- Swagger UI (để test API trực tiếp)
- SQL client như pgAdmin hoặc DBeaver

## 4. Chuẩn bị môi trường

1. Cài đặt và khởi động PostgreSQL.
2. Tạo database cho dự án.
3. Cấu hình connection string trong file appsettings.json hoặc appsettings.Development.json.
4. Khởi động backend:
   - Chạy lệnh: dotnet run trong thư mục MiniTaskManagement.Api
5. Khởi động frontend:
   - Chạy lệnh: npm install
   - Chạy lệnh: npm run dev trong thư mục task-ui
6. Mở trình duyệt và truy cập:
   - Frontend: http://localhost:3000
   - Backend: http://localhost:5070

## 5. Kịch bản testing

### TC01 - Đăng ký tài khoản mới

**Mục tiêu:** Kiểm tra người dùng có thể đăng ký tài khoản thành công.

**Các bước:**

1. Mở trang đăng ký.
2. Nhập đầy đủ thông tin: họ tên, email, mật khẩu, xác nhận mật khẩu.
3. Nhấn nút Register.
4. Quan sát thông báo kết quả.

**Kết quả mong đợi:**

- Hệ thống hiển thị thông báo đăng ký thành công.
- Người dùng được chuyển về trang đăng nhập.

---

### TC02 - Đăng nhập hệ thống

**Mục tiêu:** Kiểm tra đăng nhập với tài khoản hợp lệ.

**Các bước:**

1. Mở trang đăng nhập.
2. Nhập email và mật khẩu đúng.
3. Nhấn nút Login.
4. Quan sát điều hướng sang dashboard.

**Kết quả mong đợi:**

- Người dùng đăng nhập thành công.
- Chuyển hướng tới dashboard.
- Token được lưu trong trình duyệt.

---

### TC03 - Đăng nhập thất bại

**Mục tiêu:** Kiểm tra hệ thống xử lý sai thông tin đăng nhập.

**Các bước:**

1. Mở trang đăng nhập.
2. Nhập email hoặc mật khẩu sai.
3. Nhấn nút Login.

**Kết quả mong đợi:**

- Hệ thống hiển thị thông báo lỗi.
- Người dùng vẫn ở trang đăng nhập.

---

### TC04 - Xem dashboard

**Mục tiêu:** Kiểm tra dashboard hiển thị đúng dữ liệu.

**Các bước:**

1. Đăng nhập thành công.
2. Truy cập dashboard.
3. Quan sát các thống kê và danh sách công việc.

**Kết quả mong đợi:**

- Các card thống kê hiển thị đúng.
- Danh sách công việc được load đầy đủ.

---

### TC05 - Tạo công việc mới

**Mục tiêu:** Kiểm tra chức năng tạo công việc.

**Các bước:**

1. Truy cập màn hình tạo công việc.
2. Nhập tiêu đề, mô tả, ưu tiên, hạn xử lý.
3. Nhấn nút tạo.

**Kết quả mong đợi:**

- Công việc mới được tạo thành công.
- Công việc xuất hiện trong dashboard.

---

### TC06 - Tìm kiếm và lọc công việc

**Mục tiêu:** Kiểm tra chức năng tìm kiếm và lọc.

**Các bước:**

1. Vào dashboard.
2. Nhập từ khóa tìm kiếm.
3. Chọn trạng thái hoặc mức độ ưu tiên.
4. Chọn ngày hạn hoặc trạng thái hạn.
5. Quan sát kết quả.

**Kết quả mong đợi:**

- Danh sách công việc được lọc đúng theo điều kiện.

---

### TC07 - Chỉnh sửa công việc

**Mục tiêu:** Kiểm tra chức năng sửa công việc.

**Các bước:**

1. Chọn một công việc có sẵn.
2. Nhấn nút Sửa.
3. Thay đổi thông tin cần thiết.
4. Lưu thay đổi.

**Kết quả mong đợi:**

- Thông tin công việc được cập nhật đúng.
- Dashboard hiển thị dữ liệu mới.

---

### TC08 - Xóa công việc

**Mục tiêu:** Kiểm tra chức năng xóa công việc.

**Các bước:**

1. Chọn một công việc trong danh sách.
2. Nhấn nút Xóa.
3. Xác nhận thao tác.

**Kết quả mong đợi:**

- Công việc bị xóa khỏi danh sách.

---

### TC09 - Xuất file CSV

**Mục tiêu:** Kiểm tra chức năng xuất danh sách công việc ra file CSV.

**Các bước:**

1. Vào dashboard.
2. Nhấn nút Xuất CSV.
3. Mở file tải về.

**Kết quả mong đợi:**

- File CSV được tải về thành công.
- Dữ liệu có đúng định dạng và thông tin.

## 6. Ghi nhận kết quả

Mỗi test case cần ghi lại:

- Trạng thái: Pass / Fail / Blocked
- Mô tả lỗi (nếu có)
- Thời gian thực hiện
- Người thực hiện

## 7. Kết luận

Kịch bản trên giúp kiểm thử các chức năng chính của hệ thống một cách có hệ thống và dễ theo dõi. Nếu áp dụng thêm HP-UFT, các kịch bản trên có thể được chuyển thành test automation cho giai đoạn regression testing.
