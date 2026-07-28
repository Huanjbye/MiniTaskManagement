# Test Scenarios for Mini Task Management System

## 1. Authentication & Authorization

### 1.0 Triển khai chung
- Sử dụng API endpoint của `AuthController`.
- Chuẩn bị môi trường test với database sạch hoặc seed sẵn 2 tài khoản: một `User` và một `Admin`.
- Dùng Postman / Swagger / HTTP client hoặc test tự động để gửi request.
- Các endpoint chính:
  - `POST /api/auth/register`
  - `POST /api/auth/login`
  - `GET /api/users` hoặc admin endpoint tương tự để kiểm tra phân quyền
- Ghi lại token JWT từ kết quả login để dùng cho request tiếp theo.

### 1.1 User Registration
#### Bước triển khai
1. Gửi `POST /api/auth/register` với body:
   ```json
   {
     "fullName": "Test User",
     "email": "testuser@example.com",
     "password": "Password123!"
   }
   ```
2. Xác nhận response `200 OK` và thông báo `Register successful`.
3. Kiểm tra trong database bảng `Users` tồn tại user mới.

#### Biến thể kiểm thử
- Email đã tồn tại:
  - Dùng cùng `email` đã đăng ký, gửi lại request đăng ký.
  - Mong muốn `400 Bad Request` và message `Email already exists`.
- Password quá ngắn:
  - Gửi password < 8 ký tự.
  - Mong muốn `400 Bad Request` hoặc validation error nếu backend validate.
- Thiếu trường bắt buộc:
  - Bỏ `email` hoặc `password`.
  - Mong muốn `400 Bad Request` với lỗi validation.

### 1.2 User Login
#### Bước triển khai
1. Gửi `POST /api/auth/login` với body:
   ```json
   {
     "email": "testuser@example.com",
     "password": "Password123!"
   }
   ```
2. Xác nhận response `200 OK`.
3. Lấy giá trị token từ response: `accessToken` hoặc `token`.
4. Thử truy cập endpoint bảo vệ, ví dụ `GET /api/tasks` với header:
   ```http
   Authorization: Bearer <token>
   ```
5. Xác nhận response trả về dữ liệu thay vì `401 Unauthorized`.

#### Biến thể kiểm thử
- Email không tồn tại:
  - Đổi email sai.
  - Mong muốn `400 Bad Request` hoặc `401 Unauthorized`.
- Password sai:
  - Giữ email đúng, đổi password.
  - Mong muốn `400 Bad Request` hoặc `401 Unauthorized`.
- Token bị bỏ trống:
  - Không gắn header `Authorization`.
  - Mong muốn `401 Unauthorized`.

### 1.3 Role Validation
#### Bước triển khai
1. Tạo hoặc sử dụng tài khoản `User` và tài khoản `Admin`.
2. Login từng tài khoản và lấy token.
3. Gọi endpoint admin-only, ví dụ `GET /api/admin/users` hoặc một endpoint tương tự.

#### Kiểm tra `User` role
- Dùng token của `User`.
- Gọi endpoint admin.
- Mong muốn `403 Forbidden` hoặc `401 Unauthorized` nếu phân quyền đúng.

#### Kiểm tra `Admin` role
- Dùng token của `Admin`.
- Gọi same endpoint.
- Mong muốn `200 OK` và danh sách dữ liệu trả về.

#### Kiểm thử token xấu/expired
- Tạo token ngẫu nhiên hoặc sửa một ký tự trong token.
- Gọi endpoint bảo vệ.
- Mong muốn `401 Unauthorized`.
- Nếu có thể mô phỏng token hết hạn, dùng token expired và kiểm tra cùng kết quả.

### 1.4 Ghi nhận kết quả
- Ghi lại từng request, response code, và nội dung trả về.
- Kiểm tra trực tiếp dữ liệu trong bảng `Users` để xác nhận registration và role.
- Nếu viết test tự động, tạo 3 bộ test:
  1. `RegisterTests`
  2. `LoginTests`
  3. `AuthorizationTests`

## 2. Task Management

### 2.0 Tổng quát
- Chuẩn bị: seed database với một `Project` và hai user (`ownerUser`, `otherUser`) và một `Admin`.
- Sử dụng endpoints của `TasksController` (ví dụ `POST /api/tasks`, `PUT /api/tasks/{id}`, `DELETE /api/tasks/{id}`, `GET /api/tasks/{id}`).
- Thực hiện kiểm thử với token JWT của `ownerUser`, `otherUser`, và `Admin`.

### 2.1 Test Case Chi Tiết

| Test Case ID | Scenario | Steps | Input | Expected Result |
|--------------|----------|-------|-------|-----------------|
| TASK-01 | Tạo task hợp lệ | 1. Login `ownerUser` → lấy token<br>2. Gửi `POST /api/tasks` với token | `{ "title":"Task A","description":"desc","dueDate":"2026-08-01","projectId":"<projId>","priority":"High" }` | `201 Created`, response chứa `id`, task tồn tại trong DB và `ownerId` = `ownerUser` |
| TASK-02 | Tạo task thiếu trường bắt buộc | 1. Gửi `POST /api/tasks` thiếu `title` | `{ "description":"no title" }` | `400 Bad Request`, validation error |
| TASK-03 | Tạo task với project không tồn tại | 1. Gửi `POST /api/tasks` với `projectId` fake | `{ "title":"x","projectId":"00000000-0000-0000-0000-000000000000" }` | `404 Not Found` hoặc validation error |
| TASK-04 | Cập nhật task bởi chủ sở hữu | 1. Tạo task bởi `ownerUser`<br>2. Gửi `PUT /api/tasks/{id}` thay đổi `title` | `{ "title":"Task A updated" }` | `200 OK`, task được cập nhật trong DB |
| TASK-05 | Cập nhật task bởi user khác | 1. Login `otherUser`<br>2. Gọi `PUT /api/tasks/{id}` của `ownerUser` | `{ "title":"hacked" }` | `403 Forbidden` hoặc `401 Unauthorized` |
| TASK-06 | Cập nhật với trạng thái không hợp lệ | 1. Gửi `PUT /api/tasks/{id}` với `status` invalid | `{ "status":"NotAStatus" }` | `400 Bad Request` với message validation |
| TASK-07 | Xóa task bởi chủ sở hữu | 1. `ownerUser` gọi `DELETE /api/tasks/{id}` | N/A | `204 No Content` hoặc `200 OK`, task không còn trong DB |
| TASK-08 | Xóa task bởi Admin | 1. `Admin` gọi `DELETE /api/tasks/{id}` của user khác | N/A | `204 No Content` hoặc `200 OK` |
| TASK-09 | Xóa task không tồn tại | 1. Gọi `DELETE /api/tasks/{fakeId}` | N/A | `404 Not Found` |
| TASK-10 | Luồng trạng thái task | 1. Tạo task (Open)<br>2. `PUT` → `InProgress` → `Done` | `{ "status":"InProgress" }` then `{ "status":"Done" }` | Mỗi chuyển trạng thái trả `200 OK`, activity log lưu các trạng thái và timestamp |
| TASK-11 | Subtask tạo & cập nhật | 1. `POST /api/tasks/{id}/subtasks`<br>2. `PUT /api/tasks/{id}/subtasks/{subId}` | `{ "title":"Subtask 1" }` | `201 Created` cho subtask, cập nhật trạng thái parent nếu logic yêu cầu |
| TASK-12 | Thêm/xóa tag | 1. `POST /api/tasks/{id}/tags` để thêm<br>2. `DELETE /api/tasks/{id}/tags/{tagId}` để xóa | `{ "tagName":"bug" }` | `200 OK`, tags hiển thị trong `GET /api/tasks/{id}` |
| TASK-13 | Comment vào task | 1. `POST /api/tasks/{id}/comments` | `{ "content":"Please fix" }` | `201 Created`, comment có `author` và `timestamp` trong DB |
| TASK-14 | Activity log kiểm tra | 1. Thực hiện sequence: create → update → comment → status change<br>2. Gọi `GET /api/tasks/{id}/activity` | N/A | Activity list chứa các entry tương ứng với timestamp và user |

### 2.2 Kiểm tra dữ liệu và công cụ
- Xác nhận table `TaskItem`, `TaskSubtask`, `TaskTag`, `TaskComment`, `TaskActivity` trong DB sau các thao tác.
- Dùng Postman/Swagger để thử các payload mẫu; dùng test tự động (xUnit + TestContainers) cho các integration tests.

### 2.3 Đề xuất bộ test tự động
- `TaskCreateTests` : `TASK-01` → `TASK-03`.
- `TaskUpdateTests` : `TASK-04` → `TASK-06`.
- `TaskDeleteTests` : `TASK-07` → `TASK-09`.
- `TaskFlowTests` : `TASK-10` → `TASK-14` (integration + activity verification).

### 2.4 Sẵn sàng cho CI
- Đảm bảo tests tạo/clean test data (transaction rollback hoặc DB container reset) để không gây rác cho pipeline.
- Chạy các tests trong GitHub Actions job `backend-build` và upload coverage/artifacts.


## 3. Project Management

### 3.1 Create Project
- Verify user can create a project with valid name and description.
- Validate error when required project fields are missing.

### 3.2 Update Project
- Verify project details can be updated correctly.
- Verify invalid updates are rejected.

### 3.3 Project Access
- Verify project list returns projects visible to the user.
- Verify admin can access all projects.
- Verify a user cannot access private projects they are not part of.

## 4. Chat & Real-time Collaboration

### 4.1 Chat Room Management
- Verify user can create a chat room.
- Verify user can add members to a chat room.
- Verify only authorized users can access chat room details.

### 4.2 Chat Messaging
- Verify user can send a message in a chat room.
- Verify other room members receive the message in real-time.
- Verify message history is stored and returned correctly.

### 4.3 Read Receipts
- Verify message read status is tracked per user.
- Verify read receipts update when a user opens a chat room.

### 4.4 SignalR Authentication
- Verify chat hub connection accepts JWT via query string to authenticate.
- Verify unauthorized SignalR connections are rejected.

## 5. Admin Flows

### 5.1 User Management
- Verify admin can fetch the list of all users.
- Verify admin can change a user role.
- Verify admin can deactivate or reactivate a user account if supported.

### 5.2 Admin Dashboard
- Verify admin dashboard displays correct counts of users, projects, and tasks.
- Verify admin-only endpoints return forbidden for normal users.

## 6. API & Integration Scenarios

### 6.1 End-to-End API Flow
- Verify full flow: register → login → create project → create task → update task → add comment → view dashboard.
- Verify API response shapes for key endpoints match expectations.

### 6.2 Database Integration
- Verify task, project, user, comment, and chat data persist correctly in PostgreSQL.
- Verify foreign keys and relationships are enforced.

### 6.3 Error Handling
- Verify API returns meaningful error messages on invalid input.
- Verify HTTP status codes are correct for unauthorized, forbidden, not found, and validation errors.

## 7. Frontend Scenarios

### 7.1 Login Page
- Verify login form renders correctly.
- Verify validation warnings appear for empty credentials.
- Verify successful login navigates to dashboard.

### 7.2 Register Page
- Verify register form renders correctly.
- Verify form validates email format and password length.
- Verify successful registration shows confirmation or redirects.

### 7.3 Dashboard
- Verify dashboard loads user projects and tasks.
- Verify charts or summary widgets display correct counts.
- Verify navigation to task and project details works.

### 7.4 Task Form
- Verify task creation form input fields are present and required.
- Verify form submission sends correct API request.
- Verify validation handles invalid input.

### 7.5 Chat Interface
- Verify chat UI opens and displays existing messages.
- Verify sending a message updates the UI.
- Verify receiving a message from another user updates the chat view.

## 8. Security & Performance Scenarios

### 8.1 Security Tests
- Verify passwords are not returned in API responses.
- Verify JWT tokens are required for protected endpoints.
- Verify SQL injection attempts are rejected.
- Verify cross-site scripting attempts are sanitized in frontend displays.

### 8.2 Performance Tests
- Verify API response time is under acceptable threshold for list endpoints.
- Verify page load time for dashboard is within acceptable range.
- Verify SignalR chat connection establishment is stable under load.

## 9. Regression Scenarios
- Verify previously fixed issues do not recur after changes.
- Verify core flows continue to work after adding new features.
- Verify tests cover both happy path and edge cases.

## 10. Test Data and Fixtures
- Use seeded accounts for admin and regular users.
- Use sample projects, tasks, chat rooms, and messages.
- Validate cleanup between tests to avoid state leakage.

---

## Notes
- Prioritize **critical user flows** first: authentication, task creation/update, project access, and chat.
- Add automated tests for both **backend API** and **frontend UI**.
- Use this file as the base for writing unit, integration, and E2E test cases.