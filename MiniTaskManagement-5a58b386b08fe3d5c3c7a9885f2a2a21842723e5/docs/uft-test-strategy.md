# HP-UFT Test Strategy for Mini Task Management

## 1. Mục tiêu

Nghiên cứu này đánh giá khả năng áp dụng HPE Unified Functional Testing (HP-UFT, trước đây là QTP) cho dự án Mini Task Management, một ứng dụng web gồm:

- Frontend: Next.js + TypeScript
- Backend: ASP.NET Core Web API
- Database: PostgreSQL

Mục tiêu là xây dựng giai đoạn kiểm thử chức năng tự động cho các luồng người dùng chính như đăng nhập, đăng ký, quản lý công việc và dashboard.

## 2. Tổng quan dự án

### 2.1 Kiến trúc hiện tại

- Frontend chạy trên Next.js, giao diện hiện tại là các trang đăng nhập, đăng ký và dashboard.
- Backend cung cấp API REST cho authentication, projects, tasks và chat.
- Ứng dụng có cấu trúc SPA-like với routing phía client.
- Các trang UI có thể được test bằng công cụ ghi hình/automation như UFT nếu có thể nhận diện object ổn định.

### 2.2 Điểm mạnh để áp dụng UFT

- Ứng dụng có luồng người dùng rõ ràng và tập trung vào giao diện web.
- Các màn hình có các thành phần nhập liệu, nút bấm, bảng dữ liệu và alert/message rõ ràng.
- Có thể dùng UFT để kiểm thử end-to-end các kịch bản nghiệp vụ chính.

### 2.3 Thách thức

- Giao diện đang dùng React/Next.js, nên các đối tượng DOM có thể thay đổi theo render.
- Một số thành phần được render động, ví dụ bảng công việc, pagination, modal, alerts.
- Nếu không có chuẩn hóa object repository và naming convention, việc duy trì script UFT sẽ khó khăn.

## 3. Phạm vi đề xuất cho HP-UFT

### 3.1 Các kịch bản ưu tiên

1. Đăng ký tài khoản mới
2. Đăng nhập thành công
3. Đăng nhập thất bại với thông tin sai
4. Tạo công việc mới
5. Tìm kiếm và lọc công việc trên dashboard
6. Chỉnh sửa công việc
7. Xóa công việc
8. Xuất file CSV từ dashboard
9. Xác thực phân quyền admin/user (nếu có dữ liệu test sẵn)

### 3.2 Các màn hình phù hợp

- Trang đăng nhập
- Trang đăng ký
- Dashboard công việc
- Trang tạo/cập nhật công việc
- Trang quản trị/admin (nếu mở rộng sau)

## 4. Cách áp dụng HP-UFT

### 4.1 Môi trường triển khai

Khuyến nghị triển khai UFT trên máy test chuyên dụng với:

- Windows desktop
- Browser Chrome hoặc Edge
- Backend đang chạy trên localhost:5070
- Frontend đang chạy trên localhost:3000
- Cơ sở dữ liệu PostgreSQL sẵn sàng

### 4.2 Cấu trúc test suite đề xuất

- Login_Test
- Register_Test
- Dashboard_Filter_Test
- Create_Task_Test
- Edit_Task_Test
- Delete_Task_Test
- Export_Csv_Test

### 4.3 Chiến lược nhận diện đối tượng

Để script UFT chạy ổn định, nên:

- Gắn định danh rõ ràng cho input/button/table bằng data-testid hoặc accessible attributes.
- Khuyến nghị bổ sung các thuộc tính như:
  - data-testid="login-email"
  - data-testid="login-password"
  - data-testid="login-submit"
  - data-testid="dashboard-search"
  - data-testid="task-create-button"

Vì UFT hoạt động tốt hơn khi UI có các selector ổn định, việc này là bắt buộc để giảm lỗi do thay đổi giao diện.

## 5. Kịch bản kiểm thử đề xuất

### TC01 - Đăng nhập thành công

- Mở trang đăng nhập
- Nhập email hợp lệ
- Nhập mật khẩu hợp lệ
- Nhấn nút Login
- Kiểm tra điều hướng sang dashboard
- Kiểm tra token được lưu trong localStorage

### TC02 - Đăng nhập thất bại

- Mở trang đăng nhập
- Nhập email sai hoặc mật khẩu sai
- Nhấn Login
- Kiểm tra hiện thông báo lỗi
- Kiểm tra vẫn ở trang đăng nhập

### TC03 - Đăng ký tài khoản mới

- Mở trang đăng ký
- Nhập họ tên, email, mật khẩu, xác nhận mật khẩu
- Nhấn Register
- Kiểm tra thông báo thành công và điều hướng về login

### TC04 - Tạo công việc mới

- Đăng nhập thành công
- Mở màn hình tạo công việc
- Nhập tiêu đề, mô tả, ưu tiên, hạn xử lý
- Nhấn tạo
- Kiểm tra công việc mới xuất hiện trong dashboard

### TC05 - Lọc công việc

- Truy cập dashboard
- Nhập từ khóa tìm kiếm
- Chọn status/priority
- Kiểm tra bảng công việc được lọc đúng

## 6. Khuyến nghị triển khai thực tế

### Giai đoạn 1 - Chuẩn bị

- Cài đặt UFT trên máy test
- Cấu hình kết nối với browser và môi trường chạy
- Chuẩn hóa dữ liệu test
- Chuẩn bị tài khoản test cố định

### Giai đoạn 2 - Automation core

- Tạo các script cho auth flow
- Tạo object repository cho login, dashboard, create task
- Xây dựng reusable actions cho nhập liệu, click, assert, login/logout

### Giai đoạn 3 - Mở rộng

- Thêm các test cho project management
- Thêm các test cho admin workflows
- Tích hợp chạy hàng ngày qua ALM/HPQC nếu có sẵn

## 7. Ưu nhược điểm khi dùng UFT cho dự án này

### Ưu điểm

- Hỗ trợ kiểm thử end-to-end trên giao diện web
- Dễ mô phỏng thao tác người dùng thật
- Phù hợp cho các luồng nghiệp vụ quan trọng
- Có thể dùng cho regression testing sau mỗi release

### Nhược điểm

- UFT không phải lựa chọn tối ưu nếu chỉ cần kiểm thử unit/API nhanh
- Tốn chi phí và thời gian setup ban đầu
- Dễ bị ảnh hưởng bởi thay đổi DOM/UI
- Cần có chuẩn hóa object identification để giữ script ổn định

## 8. Kết luận

HP-UFT là một lựa chọn phù hợp để triển khai giai đoạn kiểm thử chức năng cho dự án Mini Task Management, đặc biệt cho các luồng người dùng cốt lõi như đăng nhập, đăng ký và quản lý công việc. Tuy nhiên, để hiệu quả, cần kết hợp với việc chuẩn hóa UI và môi trường test rõ ràng. Với cấu trúc hiện tại, UFT nên được dùng như công cụ cho kiểm thử giao diện end-to-end, còn unit/API tests nên thực hiện bằng các công cụ khác phù hợp hơn.

## 9. Khuyến nghị cuối cùng

- Ưu tiên triển khai UFT cho các luồng nghiệp vụ cốt lõi trước.
- Bổ sung data-testid cho giao diện frontend.
- Dùng UFT cho regression suite, không dùng cho unit test.
- Kết hợp với CI/CD và môi trường test riêng để tăng độ ổn định.
