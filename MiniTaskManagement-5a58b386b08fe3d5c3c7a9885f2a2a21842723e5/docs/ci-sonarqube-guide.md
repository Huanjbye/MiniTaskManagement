# Hướng dẫn CI với GitHub Actions + SonarQube

## 1. Mục tiêu

Tự động chạy kiểm thử và phân tích chất lượng mã mỗi khi push hoặc tạo pull request.

## 2. Workflow được thêm

File: .github/workflows/ci.yml

Workflow gồm:

- Build backend ASP.NET Core
- Lint và build frontend Next.js
- Gửi kết quả cho SonarQube

## 3. Cấu hình GitHub Secrets

Trong GitHub Repository > Settings > Secrets and variables > Actions, thêm:

- SONAR_TOKEN: token của SonarQube/SonarCloud
- SONAR_HOST_URL: URL SonarQube, ví dụ https://sonarcloud.io

## 4. Khuyến nghị cho dự án

### Backend

- Thêm unit test bằng xUnit hoặc NUnit
- Chạy dotnet test trong CI

### Frontend

- Thêm test cho UI bằng Playwright hoặc Vitest
- Chạy npm test trong CI

### SonarQube

- Bật rule cho:
  - bugs
  - vulnerabilities
  - code smells
  - coverage

## 5. Gợi ý triển khai tiếp theo

1. Thêm test suite cho backend
2. Thêm test E2E cho login/task flow
3. Tích hợp coverage report vào SonarQube
4. Bật branch protection để không cho merge nếu CI fail
