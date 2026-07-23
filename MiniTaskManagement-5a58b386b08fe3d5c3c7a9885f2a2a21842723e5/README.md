# Mini Task Management System

## Overview

Mini Task Management System là ứng dụng quản lý công việc nội bộ được xây dựng nhằm hỗ trợ người dùng tạo, theo dõi và quản lý các công việc hằng ngày.

Hệ thống hỗ trợ:

* Đăng ký và đăng nhập tài khoản
* Xác thực bằng JWT Authentication
* Phân quyền Admin và User
* Quản lý công việc cá nhân
* Tìm kiếm và lọc công việc
* Thống kê Dashboard
* Quản lý người dùng dành cho Admin

---

# Technologies

## Frontend

* Next.js
* TypeScript
* React
* Tailwind CSS
* Axios
* React Hook Form

## Backend

* ASP.NET Core Web API (.NET 10)
* Entity Framework Core
* JWT Authentication
* BCrypt Password Hashing

## Database

* PostgreSQL

---

# System Architecture

Frontend (Next.js)

↓

REST API (ASP.NET Core Web API)

↓

Service Layer

↓

Entity Framework Core

↓

PostgreSQL Database

---

# Features

## Authentication

* User Registration
* User Login
* JWT Authentication
* Password Hashing
* Role Based Authorization

### Register

Người dùng có thể đăng ký tài khoản với:

* Full Name
* Email
* Password

Validation:

* Email không được trùng
* Password tối thiểu 8 ký tự

### Login

Người dùng đăng nhập bằng:

* Email
* Password

Sau khi đăng nhập thành công, hệ thống trả về JWT Token để xác thực các yêu cầu tiếp theo.

---

## Task Management

Người dùng có thể:

* Tạo công việc mới
* Cập nhật công việc
* Xóa công việc
* Xem danh sách công việc

### Task Information

* Title
* Description
* Status
* Priority
* Due Date
* Created Date

### Task Status

* Todo
* In Progress
* Done

### Priority Levels

* Low
* Medium
* High

---

## Search and Filter

### Search

Tìm kiếm công việc theo:

* Task Title

### Filter

Lọc công việc theo:

* Status
* Priority
* Due Date

---

## Dashboard

Hiển thị thống kê:

* Total Tasks
* Todo Tasks
* In Progress Tasks
* Done Tasks

---

## Pagination

Danh sách công việc hỗ trợ:

* Previous Page
* Next Page
* Custom Page Size

---

## Administration

### User Management

Admin có thể:

* Xem danh sách người dùng
* Khóa tài khoản người dùng
* Mở khóa tài khoản người dùng

### Task Monitoring

Admin có thể:

* Xem toàn bộ công việc trong hệ thống

### Authorization

Các API quản trị được bảo vệ bằng:

```csharp
[Authorize(Roles = "Admin")]
```

---

# Project Structure

## Backend

```text
MiniTaskManagement.Api
│
├── Controllers
├── DTOs
├── Data
├── Entities
├── Services
├── Migrations
├── Program.cs
└── appsettings.json
```

## Frontend

```text
mini-task-management-web
│
├── src
│   ├── app
│   │   ├── login
│   │   ├── register
│   │   ├── dashboard
│   │   │   ├── create
│   │   │   ├── edit
│   │   │   ├── admin
│   │   │   ├── layout.tsx
│   │   │   └── page.tsx
│   │   └── page.tsx
│   │
│   └── lib
│       └── api.ts
```

---

# Database Design

## Users Table

| Column       | Type         |
| ------------ | ------------ |
| Id           | UUID         |
| FullName     | VARCHAR(200) |
| Email        | VARCHAR(200) |
| PasswordHash | TEXT         |
| Role         | VARCHAR(20)  |
| IsActive     | BOOLEAN      |
| CreatedAt    | TIMESTAMP    |

## Tasks Table

| Column      | Type         |
| ----------- | ------------ |
| Id          | UUID         |
| Title       | VARCHAR(300) |
| Description | TEXT         |
| Status      | VARCHAR(50)  |
| Priority    | VARCHAR(20)  |
| DueDate     | TIMESTAMP    |
| UserId      | UUID         |
| CreatedAt   | TIMESTAMP    |
| UpdatedAt   | TIMESTAMP    |

---

# Entity Relationship

```text
Users
  |
  | One To Many
  |
Tasks
```

Một người dùng có thể sở hữu nhiều công việc.

---

# API Documentation

## Authentication

### Register

```http
POST /api/auth/register
```

### Login

```http
POST /api/auth/login
```

---

## Tasks

### Get All Tasks

```http
GET /api/tasks
```

### Get Task By Id

```http
GET /api/tasks/{id}
```

### Create Task

```http
POST /api/tasks
```

### Update Task

```http
PUT /api/tasks/{id}
```

### Delete Task

```http
DELETE /api/tasks/{id}
```

---

## Search

```http
GET /api/tasks?search=meeting
```

---

## Filter

```http
GET /api/tasks?status=Todo
```

```http
GET /api/tasks?priority=High
```

```http
GET /api/tasks?dueDate=2026-06-25
```

---

## Pagination

```http
GET /api/tasks?page=1&pageSize=5
```

---

## Admin

### Get Users

```http
GET /api/admin/users
```

### Disable User

```http
PUT /api/admin/users/{id}/disable
```

### Enable User

```http
PUT /api/admin/users/{id}/enable
```

### Get All Tasks

```http
GET /api/admin/tasks
```

---

# Authentication

Tất cả API yêu cầu xác thực phải gửi JWT Token:

```http
Authorization: Bearer <token>
```

---

# Installation Guide

## Clone Repository

```bash
git clone <repository-url>
```

---

# Backend Setup

Di chuyển vào thư mục Backend:

```bash
cd MiniTaskManagement.Api
```

Khôi phục package:

```bash
dotnet restore
```

Áp dụng Migration:

```bash
dotnet ef database update
```

Chạy ứng dụng:

```bash
dotnet run
```

Backend mặc định chạy tại:

```text
http://localhost:5000
```

hoặc cổng được cấu hình trong launchSettings.json.

---

# Frontend Setup

Di chuyển vào thư mục Frontend:

```bash
cd mini-task-management-web
```

Cài đặt package:

```bash
npm install
```

Chạy ứng dụng:

```bash
npm run dev
```

Frontend mặc định chạy tại:

```text
http://localhost:3000/login
```

---

# Demo Flow

1. Register User
2. Login User
3. Create Task
4. Update Task
5. Delete Task
6. Search Task
7. Filter Task
8. View Dashboard Statistics
9. Pagination Task List
10. Login as Admin
11. View User List
12. Disable User Account
13. Enable User Account
14. View All Tasks

---

# Future Improvements

Các tính năng có thể mở rộng trong tương lai:

* Docker Compose
* File Attachment Upload
* Email Notification
* Dark Mode
* Refresh Token Authentication
* Audit Logging
* Soft Delete
* Swagger Documentation

---

# Author

Hoàng Tú

Internship Practice Project

Mini Task Management System developed using Next.js, ASP.NET Core Web API and PostgreSQL.
