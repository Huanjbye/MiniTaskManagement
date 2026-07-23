# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: home.spec.ts >> login form submits credentials and stores auth data
- Location: tests\home.spec.ts:3:5

# Error details

```
Error: page.goto: net::ERR_CONNECTION_REFUSED at http://127.0.0.1:3000/login
Call log:
  - navigating to "http://127.0.0.1:3000/login", waiting until "load"

```

# Test source

```ts
  1  | import { test, expect } from "@playwright/test";
  2  | 
  3  | test("login form submits credentials and stores auth data", async ({ page }) => {
  4  |   await page.route("**/api/Auth/login", async (route) => {
  5  |     await route.fulfill({
  6  |       status: 200,
  7  |       contentType: "application/json",
  8  |       body: JSON.stringify({
  9  |         token: "fake-token",
  10 |         role: "User",
  11 |         fullName: "Test User",
  12 |         email: "tester@example.com",
  13 |       }),
  14 |     });
  15 |   });
  16 | 
> 17 |   await page.goto("/login");
     |              ^ Error: page.goto: net::ERR_CONNECTION_REFUSED at http://127.0.0.1:3000/login
  18 |   await page.getByLabel("Email").fill("tester@example.com");
  19 |   await page.getByLabel("Password").fill("Password123!");
  20 |   await page.getByRole("button", { name: /login/i }).click();
  21 | 
  22 |   await expect(page).toHaveURL(/\/dashboard/);
  23 |   await expect.poll(() => page.evaluate(() => window.localStorage.getItem("token"))).toBe("fake-token");
  24 | });
  25 | 
  26 | test("register form submits and redirects to login", async ({ page }) => {
  27 |   await page.route("**/api/auth/register", async (route) => {
  28 |     await route.fulfill({
  29 |       status: 200,
  30 |       contentType: "application/json",
  31 |       body: JSON.stringify("Register successful"),
  32 |     });
  33 |   });
  34 | 
  35 |   await page.goto("/register");
  36 |   await page.getByLabel("Full Name").fill("Test User");
  37 |   await page.getByLabel("Email").fill("tester@example.com");
  38 |   await page.getByLabel("Password").fill("Password123!");
  39 |   await page.getByLabel("Confirm Password").fill("Password123!");
  40 |   await page.getByRole("button", { name: /register/i }).click();
  41 | 
  42 |   await expect(page).toHaveURL(/\/login/);
  43 | });
  44 | 
```