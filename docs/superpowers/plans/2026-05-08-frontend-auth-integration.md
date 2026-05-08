# Frontend Auth Integration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Connect the React frontend to backend auth so users can register, log in, stay signed in, access protected learning pages, and see Vietnamese role-aware navigation.

**Architecture:** Add a focused auth layer under `frontend/src/auth` built around `AuthContext`, `localStorage`, and axios interceptors. Update routes and layout so home is public, `Courses` is protected, signed-in users get an account dropdown, and admins see a `Dashboard` entry.

**Tech Stack:** React 18, React Router 6, Axios, Vite, Vitest, React Testing Library

---

## File Map

### Create

- `frontend/src/auth/authStorage.js`
- `frontend/src/auth/authService.js`
- `frontend/src/auth/AuthContext.jsx`
- `frontend/src/auth/useAuth.js`
- `frontend/src/auth/RequireAuth.jsx`
- `frontend/src/pages/HomePage.jsx`
- `frontend/src/pages/ProfilePage.jsx`
- `frontend/src/pages/ChangePasswordPage.jsx`
- `frontend/src/test/setup.js`
- `frontend/src/auth/AuthContext.test.jsx`
- `frontend/src/auth/RequireAuth.test.jsx`
- `frontend/src/components/layout/MainLayout.test.jsx`

### Modify

- `frontend/package.json`
- `frontend/src/main.jsx`
- `frontend/src/App.jsx`
- `frontend/src/api/axiosClient.js`
- `frontend/src/routes/AppRoutes.jsx`
- `frontend/src/components/layout/MainLayout.jsx`
- `frontend/src/pages/LoginPage.jsx`
- `frontend/src/pages/RegisterPage.jsx`
- `frontend/src/pages/CoursesPage.jsx`
- `frontend/vite.config.js`

### Verify

- `docker run --rm -v /home/phan-duong-quoc-nhat/workspace/VibeCourseAI/frontend:/app -w /app node:24-alpine sh -lc "npm install && npm run test -- --run"`
- `docker compose build frontend`
- `docker compose up -d --force-recreate frontend`
- `curl -I http://localhost:3000`

---

### Task 1: Add Frontend Test Harness And Auth Storage Utilities

**Files:**
- Modify: `frontend/package.json`
- Modify: `frontend/vite.config.js`
- Create: `frontend/src/test/setup.js`
- Create: `frontend/src/auth/authStorage.js`
- Create: `frontend/src/auth/AuthContext.test.jsx`

- [ ] **Step 1: Write the failing auth-storage test**

Create `frontend/src/auth/AuthContext.test.jsx`:

```jsx
import { beforeEach, describe, expect, it } from "vitest";
import { clearAuthSession, loadAuthSession, saveAuthSession } from "./authStorage";

describe("authStorage", () => {
  beforeEach(() => {
    window.localStorage.clear();
  });

  it("lưu và đọc lại phiên đăng nhập từ localStorage", () => {
    const session = {
      accessToken: "access-token",
      refreshToken: "refresh-token",
      user: {
        id: "user-1",
        fullName: "Nguyễn Văn A",
        email: "vana@example.com",
        role: "User"
      }
    };

    saveAuthSession(session);

    expect(loadAuthSession()).toEqual(session);
  });

  it("xóa phiên đăng nhập khỏi localStorage", () => {
    saveAuthSession({
      accessToken: "access-token",
      refreshToken: "refresh-token",
      user: {
        id: "user-1",
        fullName: "Nguyễn Văn A",
        email: "vana@example.com",
        role: "User"
      }
    });

    clearAuthSession();

    expect(loadAuthSession()).toBeNull();
  });
});
```

- [ ] **Step 2: Run the test to verify it fails**

Run:

```bash
docker run --rm -v /home/phan-duong-quoc-nhat/workspace/VibeCourseAI/frontend:/app -w /app node:24-alpine sh -lc "npm install && npx vitest run src/auth/AuthContext.test.jsx"
```

Expected:

```text
FAIL
Failed to resolve import "./authStorage"
```

- [ ] **Step 3: Add Vitest and auth storage utilities**

Update `frontend/package.json`:

```json
{
  "scripts": {
    "dev": "vite",
    "build": "vite build",
    "test": "vitest"
  },
  "devDependencies": {
    "@testing-library/jest-dom": "^6.6.3",
    "@testing-library/react": "^16.0.1",
    "@vitejs/plugin-react": "^4.3.1",
    "jsdom": "^25.0.1",
    "vite": "^5.4.2",
    "vitest": "^2.1.3"
  }
}
```

Update `frontend/vite.config.js`:

```js
import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

export default defineConfig({
  plugins: [react()],
  test: {
    environment: "jsdom",
    setupFiles: "./src/test/setup.js"
  }
});
```

Create `frontend/src/test/setup.js`:

```js
import "@testing-library/jest-dom";
```

Create `frontend/src/auth/authStorage.js`:

```js
const STORAGE_KEY = "vibe_course_ai_auth";

export function loadAuthSession() {
  const raw = window.localStorage.getItem(STORAGE_KEY);
  return raw ? JSON.parse(raw) : null;
}

export function saveAuthSession(session) {
  window.localStorage.setItem(STORAGE_KEY, JSON.stringify(session));
}

export function clearAuthSession() {
  window.localStorage.removeItem(STORAGE_KEY);
}
```

- [ ] **Step 4: Run the auth-storage test to verify it passes**

Run:

```bash
docker run --rm -v /home/phan-duong-quoc-nhat/workspace/VibeCourseAI/frontend:/app -w /app node:24-alpine sh -lc "npm install && npx vitest run src/auth/AuthContext.test.jsx"
```

Expected:

```text
PASS
```

- [ ] **Step 5: Commit**

```bash
git add frontend/package.json frontend/vite.config.js frontend/src/test/setup.js frontend/src/auth/authStorage.js frontend/src/auth/AuthContext.test.jsx
git commit -m "test: add frontend auth storage harness"
```

### Task 2: Add Auth Service And Auth Context Bootstrap

**Files:**
- Create: `frontend/src/auth/authService.js`
- Create: `frontend/src/auth/AuthContext.jsx`
- Create: `frontend/src/auth/useAuth.js`
- Modify: `frontend/src/App.jsx`
- Modify: `frontend/src/main.jsx`
- Modify: `frontend/src/api/axiosClient.js`
- Modify: `frontend/src/auth/AuthContext.test.jsx`

- [ ] **Step 1: Extend the test to fail on auth bootstrap behavior**

Replace `frontend/src/auth/AuthContext.test.jsx` with:

```jsx
import { render, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { AuthProvider, useAuth } from "./AuthContext";
import { saveAuthSession } from "./authStorage";

vi.mock("./authService", () => ({
  getCurrentUser: vi.fn().mockResolvedValue({
    id: "user-1",
    fullName: "Nguyễn Văn A",
    email: "vana@example.com",
    role: "User"
  })
}));

function Probe() {
  const { isAuthenticated, user, isBootstrapping } = useAuth();
  return (
    <div>
      <span>{isBootstrapping ? "Đang khởi tạo" : "Đã khởi tạo"}</span>
      <span>{isAuthenticated ? "Đã đăng nhập" : "Chưa đăng nhập"}</span>
      <span>{user?.fullName ?? "Không có người dùng"}</span>
    </div>
  );
}

describe("AuthContext", () => {
  beforeEach(() => {
    window.localStorage.clear();
  });

  it("khôi phục phiên đăng nhập từ localStorage và gọi me", async () => {
    saveAuthSession({
      accessToken: "access-token",
      refreshToken: "refresh-token",
      user: {
        id: "user-1",
        fullName: "Nguyễn Văn A",
        email: "vana@example.com",
        role: "User"
      }
    });

    render(
      <AuthProvider>
        <Probe />
      </AuthProvider>
    );

    await waitFor(() => {
      expect(screen.getByText("Đã đăng nhập")).toBeInTheDocument();
      expect(screen.getByText("Nguyễn Văn A")).toBeInTheDocument();
    });
  });
});
```

- [ ] **Step 2: Run the test to verify it fails**

Run:

```bash
docker run --rm -v /home/phan-duong-quoc-nhat/workspace/VibeCourseAI/frontend:/app -w /app node:24-alpine sh -lc "npm install && npx vitest run src/auth/AuthContext.test.jsx"
```

Expected:

```text
FAIL
Failed to resolve import "./AuthContext"
```

- [ ] **Step 3: Create auth service and context**

Create `frontend/src/auth/authService.js`:

```js
import { axiosClient } from "../api/axiosClient";

export async function login(payload) {
  const { data } = await axiosClient.post("/auth/login", payload);
  return data;
}

export async function register(payload) {
  const { data } = await axiosClient.post("/auth/register", payload);
  return data;
}

export async function refresh(refreshToken) {
  const { data } = await axiosClient.post("/auth/refresh", { refreshToken });
  return data;
}

export async function logout(refreshToken) {
  await axiosClient.post("/auth/logout", { refreshToken });
}

export async function getCurrentUser() {
  const { data } = await axiosClient.get("/auth/me");
  return data;
}

export async function changePassword(payload) {
  await axiosClient.post("/auth/change-password", payload);
}
```

Create `frontend/src/auth/AuthContext.jsx`:

```jsx
import { createContext, useContext, useEffect, useMemo, useState } from "react";
import * as authService from "./authService";
import { clearAuthSession, loadAuthSession, saveAuthSession } from "./authStorage";

const AuthContext = createContext(null);

export function AuthProvider({ children }) {
  const [session, setSession] = useState(() => loadAuthSession());
  const [user, setUser] = useState(() => loadAuthSession()?.user ?? null);
  const [isBootstrapping, setIsBootstrapping] = useState(true);

  useEffect(() => {
    async function bootstrap() {
      if (!session?.accessToken || !session?.refreshToken) {
        setIsBootstrapping(false);
        return;
      }

      try {
        const currentUser = await authService.getCurrentUser();
        const nextSession = { ...session, user: currentUser };
        saveAuthSession(nextSession);
        setSession(nextSession);
        setUser(currentUser);
      } catch {
        clearAuthSession();
        setSession(null);
        setUser(null);
      } finally {
        setIsBootstrapping(false);
      }
    }

    bootstrap();
  }, []);

  const value = useMemo(
    () => ({
      session,
      user,
      isAuthenticated: Boolean(session?.accessToken && user),
      isBootstrapping
    }),
    [session, user, isBootstrapping]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error("useAuth must be used within AuthProvider");
  }

  return context;
}
```

Create `frontend/src/auth/useAuth.js`:

```js
export { useAuth } from "./AuthContext";
```

Keep `frontend/src/api/axiosClient.js` simple for now:

```js
import axios from "axios";

export const axiosClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || "http://localhost:5000/api",
  headers: {
    "Content-Type": "application/json"
  }
});
```

Update `frontend/src/App.jsx`:

```jsx
import { AuthProvider } from "./auth/AuthContext";
import AppRoutes from "./routes/AppRoutes";

export default function App() {
  return (
    <AuthProvider>
      <AppRoutes />
    </AuthProvider>
  );
}
```

Update `frontend/src/main.jsx` only if needed to preserve current `BrowserRouter` wrapping exactly as-is.

- [ ] **Step 4: Run the auth bootstrap test to verify it passes**

Run:

```bash
docker run --rm -v /home/phan-duong-quoc-nhat/workspace/VibeCourseAI/frontend:/app -w /app node:24-alpine sh -lc "npm install && npx vitest run src/auth/AuthContext.test.jsx"
```

Expected:

```text
PASS
```

- [ ] **Step 5: Commit**

```bash
git add frontend/src/auth/authService.js frontend/src/auth/AuthContext.jsx frontend/src/auth/useAuth.js frontend/src/api/axiosClient.js frontend/src/App.jsx frontend/src/main.jsx frontend/src/auth/AuthContext.test.jsx
git commit -m "feat: add frontend auth context bootstrap"
```

### Task 3: Implement Login And Register UI Against Real APIs

**Files:**
- Modify: `frontend/src/auth/AuthContext.jsx`
- Modify: `frontend/src/pages/LoginPage.jsx`
- Modify: `frontend/src/pages/RegisterPage.jsx`

- [ ] **Step 1: Extend AuthContext with login and register methods**

Update `frontend/src/auth/AuthContext.jsx` to include:

```jsx
  async function handleAuthSuccess(response) {
    const nextSession = {
      accessToken: response.accessToken,
      refreshToken: response.refreshToken,
      user: response.user
    };

    saveAuthSession(nextSession);
    setSession(nextSession);
    setUser(response.user);
  }

  async function login(formData) {
    const response = await authService.login(formData);
    await handleAuthSuccess(response);
  }

  async function register(formData) {
    const response = await authService.register(formData);
    await handleAuthSuccess(response);
  }
```

Expose these in the context value:

```jsx
login,
register
```

- [ ] **Step 2: Replace `LoginPage.jsx` with a real login form**

Update `frontend/src/pages/LoginPage.jsx`:

```jsx
import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useAuth } from "../auth/useAuth";

function getVietnameseErrorMessage(error) {
  const message = error?.response?.data?.message;

  if (message?.includes("Email hoặc mật khẩu không đúng")) {
    return "Email hoặc mật khẩu không đúng.";
  }

  if (message?.includes("Tài khoản đã bị khóa")) {
    return "Tài khoản đã bị khóa.";
  }

  return "Không thể kết nối đến máy chủ. Vui lòng thử lại.";
}

export default function LoginPage() {
  const navigate = useNavigate();
  const { login } = useAuth();
  const [formData, setFormData] = useState({ email: "", password: "" });
  const [errorMessage, setErrorMessage] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function handleSubmit(event) {
    event.preventDefault();
    setErrorMessage("");
    setIsSubmitting(true);

    try {
      await login(formData);
      navigate("/");
    } catch (error) {
      setErrorMessage(getVietnameseErrorMessage(error));
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <section style={{ padding: 40, fontFamily: "Georgia, serif", maxWidth: 480 }}>
      <h1>Đăng nhập</h1>
      <p>Vui lòng nhập thông tin tài khoản để truy cập hệ thống học tập.</p>

      <form onSubmit={handleSubmit} style={{ display: "grid", gap: 16, marginTop: 24 }}>
        <label htmlFor="email" style={{ display: "grid", gap: 8 }}>
          <span>Email</span>
          <input
            id="email"
            type="email"
            placeholder="Nhập email của bạn"
            value={formData.email}
            onChange={(event) => setFormData((current) => ({ ...current, email: event.target.value }))}
          />
        </label>

        <label htmlFor="password" style={{ display: "grid", gap: 8 }}>
          <span>Mật khẩu</span>
          <input
            id="password"
            type="password"
            placeholder="Nhập mật khẩu"
            value={formData.password}
            onChange={(event) => setFormData((current) => ({ ...current, password: event.target.value }))}
          />
        </label>

        {errorMessage ? <p style={{ color: "#ffd6d6" }}>{errorMessage}</p> : null}

        <button type="submit" disabled={isSubmitting}>
          {isSubmitting ? "Đang đăng nhập..." : "Đăng nhập"}
        </button>
      </form>

      <p style={{ marginTop: 24 }}>
        Bạn chưa có tài khoản? <Link to="/register">Đăng ký ngay</Link>
      </p>
    </section>
  );
}
```

- [ ] **Step 3: Replace `RegisterPage.jsx` with a real register form**

Update `frontend/src/pages/RegisterPage.jsx`:

```jsx
import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useAuth } from "../auth/useAuth";

function getVietnameseErrorMessage(error) {
  const message = error?.response?.data?.message;

  if (message?.includes("Email đã tồn tại")) {
    return "Email đã tồn tại.";
  }

  return "Không thể kết nối đến máy chủ. Vui lòng thử lại.";
}

export default function RegisterPage() {
  const navigate = useNavigate();
  const { register } = useAuth();
  const [formData, setFormData] = useState({
    fullName: "",
    email: "",
    password: ""
  });
  const [errorMessage, setErrorMessage] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function handleSubmit(event) {
    event.preventDefault();
    setErrorMessage("");

    if (!formData.fullName || !formData.email || !formData.password) {
      setErrorMessage("Vui lòng nhập đầy đủ thông tin.");
      return;
    }

    setIsSubmitting(true);

    try {
      await register(formData);
      navigate("/");
    } catch (error) {
      setErrorMessage(getVietnameseErrorMessage(error));
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <section style={{ padding: 40, fontFamily: "Georgia, serif", maxWidth: 480 }}>
      <h1>Đăng ký</h1>
      <p>Tạo tài khoản mới để bắt đầu học các khóa học trên hệ thống.</p>

      <form onSubmit={handleSubmit} style={{ display: "grid", gap: 16, marginTop: 24 }}>
        <label htmlFor="fullName" style={{ display: "grid", gap: 8 }}>
          <span>Họ và tên</span>
          <input
            id="fullName"
            type="text"
            placeholder="Nhập họ và tên"
            value={formData.fullName}
            onChange={(event) => setFormData((current) => ({ ...current, fullName: event.target.value }))}
          />
        </label>

        <label htmlFor="email" style={{ display: "grid", gap: 8 }}>
          <span>Email</span>
          <input
            id="email"
            type="email"
            placeholder="Nhập email của bạn"
            value={formData.email}
            onChange={(event) => setFormData((current) => ({ ...current, email: event.target.value }))}
          />
        </label>

        <label htmlFor="password" style={{ display: "grid", gap: 8 }}>
          <span>Mật khẩu</span>
          <input
            id="password"
            type="password"
            placeholder="Tạo mật khẩu"
            value={formData.password}
            onChange={(event) => setFormData((current) => ({ ...current, password: event.target.value }))}
          />
        </label>

        {errorMessage ? <p style={{ color: "#ffd6d6" }}>{errorMessage}</p> : null}

        <button type="submit" disabled={isSubmitting}>
          {isSubmitting ? "Đang tạo tài khoản..." : "Tạo tài khoản"}
        </button>
      </form>

      <p style={{ marginTop: 24 }}>
        Bạn đã có tài khoản? <Link to="/login">Đăng nhập</Link>
      </p>
    </section>
  );
}
```

- [ ] **Step 4: Run the existing auth context test to ensure no regression**

Run:

```bash
docker run --rm -v /home/phan-duong-quoc-nhat/workspace/VibeCourseAI/frontend:/app -w /app node:24-alpine sh -lc "npm install && npx vitest run src/auth/AuthContext.test.jsx"
```

Expected:

```text
PASS
```

- [ ] **Step 5: Commit**

```bash
git add frontend/src/auth/AuthContext.jsx frontend/src/pages/LoginPage.jsx frontend/src/pages/RegisterPage.jsx
git commit -m "feat: connect login and register pages to auth api"
```

### Task 4: Add Protected Routes And Public Home Routing

**Files:**
- Create: `frontend/src/auth/RequireAuth.jsx`
- Create: `frontend/src/auth/RequireAuth.test.jsx`
- Create: `frontend/src/pages/HomePage.jsx`
- Modify: `frontend/src/routes/AppRoutes.jsx`
- Modify: `frontend/src/pages/CoursesPage.jsx`

- [ ] **Step 1: Write the failing protected-route test**

Create `frontend/src/auth/RequireAuth.test.jsx`:

```jsx
import { render, screen } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { describe, expect, it, vi } from "vitest";
import RequireAuth from "./RequireAuth";

vi.mock("./useAuth", () => ({
  useAuth: () => ({
    isAuthenticated: false,
    isBootstrapping: false
  })
}));

describe("RequireAuth", () => {
  it("chuyển người dùng chưa đăng nhập về trang đăng nhập", () => {
    render(
      <MemoryRouter initialEntries={["/courses"]}>
        <Routes>
          <Route path="/login" element={<div>Trang đăng nhập</div>} />
          <Route
            path="/courses"
            element={
              <RequireAuth>
                <div>Trang khóa học</div>
              </RequireAuth>
            }
          />
        </Routes>
      </MemoryRouter>
    );

    expect(screen.getByText("Trang đăng nhập")).toBeInTheDocument();
  });
});
```

- [ ] **Step 2: Run the protected-route test to verify it fails**

Run:

```bash
docker run --rm -v /home/phan-duong-quoc-nhat/workspace/VibeCourseAI/frontend:/app -w /app node:24-alpine sh -lc "npm install && npx vitest run src/auth/RequireAuth.test.jsx"
```

Expected:

```text
FAIL
Failed to resolve import "./RequireAuth"
```

- [ ] **Step 3: Add home page and protected route wrapper**

Create `frontend/src/auth/RequireAuth.jsx`:

```jsx
import { Navigate, useLocation } from "react-router-dom";
import { useAuth } from "./useAuth";

export default function RequireAuth({ children }) {
  const { isAuthenticated, isBootstrapping } = useAuth();
  const location = useLocation();

  if (isBootstrapping) {
    return <p>Đang kiểm tra phiên đăng nhập...</p>;
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" replace state={{ from: location.pathname, message: "Bạn cần đăng nhập để tiếp tục." }} />;
  }

  return children;
}
```

Create `frontend/src/pages/HomePage.jsx`:

```jsx
export default function HomePage() {
  return (
    <section>
      <h1 style={{ fontSize: 42, marginBottom: 12 }}>Trang chủ</h1>
      <p style={{ maxWidth: 720, lineHeight: 1.6 }}>
        Chào mừng bạn đến với hệ thống học tập VibeCourseAI. Bạn có thể khám phá khóa học,
        đăng nhập để học tiếp, hoặc đăng ký tài khoản mới để bắt đầu.
      </p>
    </section>
  );
}
```

Update `frontend/src/pages/CoursesPage.jsx` so visible text is fully Vietnamese:

```jsx
export default function CoursesPage() {
  return (
    <section>
      <h1 style={{ fontSize: 42, marginBottom: 12 }}>Khóa học</h1>
      <p style={{ maxWidth: 720, lineHeight: 1.6 }}>
        Danh sách khóa học sẽ được lấy từ API `GET /api/courses`. Hiện tại backend
        seed sẵn một khóa học mẫu để kiểm tra luồng kết nối cơ bản.
      </p>
    </section>
  );
}
```

Update `frontend/src/routes/AppRoutes.jsx`:

```jsx
import { Route, Routes } from "react-router-dom";
import RequireAuth from "../auth/RequireAuth";
import MainLayout from "../components/layout/MainLayout";
import CoursesPage from "../pages/CoursesPage";
import DashboardPage from "../pages/DashboardPage";
import HomePage from "../pages/HomePage";
import LoginPage from "../pages/LoginPage";
import RegisterPage from "../pages/RegisterPage";

export default function AppRoutes() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/register" element={<RegisterPage />} />
      <Route element={<MainLayout />}>
        <Route path="/" element={<HomePage />} />
        <Route path="/dashboard" element={<DashboardPage />} />
        <Route
          path="/courses"
          element={
            <RequireAuth>
              <CoursesPage />
            </RequireAuth>
          }
        />
      </Route>
    </Routes>
  );
}
```

- [ ] **Step 4: Run the protected-route test to verify it passes**

Run:

```bash
docker run --rm -v /home/phan-duong-quoc-nhat/workspace/VibeCourseAI/frontend:/app -w /app node:24-alpine sh -lc "npm install && npx vitest run src/auth/RequireAuth.test.jsx"
```

Expected:

```text
PASS
```

- [ ] **Step 5: Commit**

```bash
git add frontend/src/auth/RequireAuth.jsx frontend/src/auth/RequireAuth.test.jsx frontend/src/pages/HomePage.jsx frontend/src/pages/CoursesPage.jsx frontend/src/routes/AppRoutes.jsx
git commit -m "feat: add public home and protected courses route"
```

### Task 5: Add Navbar Auth State, Admin Dashboard Link, And Account Dropdown

**Files:**
- Modify: `frontend/src/components/layout/MainLayout.jsx`
- Create: `frontend/src/components/layout/MainLayout.test.jsx`

- [ ] **Step 1: Write the failing navbar test**

Create `frontend/src/components/layout/MainLayout.test.jsx`:

```jsx
import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { describe, expect, it, vi } from "vitest";
import MainLayout from "./MainLayout";

vi.mock("../../auth/useAuth", () => ({
  useAuth: () => ({
    isAuthenticated: true,
    user: {
      fullName: "Quản trị viên hệ thống",
      role: "Admin"
    }
  })
}));

describe("MainLayout", () => {
  it("hiển thị nút Dashboard cho admin", () => {
    render(
      <MemoryRouter>
        <MainLayout />
      </MemoryRouter>
    );

    expect(screen.getByText("Dashboard")).toBeInTheDocument();
    expect(screen.getByText("Quản trị viên hệ thống")).toBeInTheDocument();
    expect(screen.getByText("Đăng xuất")).toBeInTheDocument();
  });
});
```

- [ ] **Step 2: Run the navbar test to verify it fails**

Run:

```bash
docker run --rm -v /home/phan-duong-quoc-nhat/workspace/VibeCourseAI/frontend:/app -w /app node:24-alpine sh -lc "npm install && npx vitest run src/components/layout/MainLayout.test.jsx"
```

Expected:

```text
FAIL
Unable to find text "Quản trị viên hệ thống"
```

- [ ] **Step 3: Update `MainLayout.jsx` for signed-in and signed-out states**

Update `frontend/src/components/layout/MainLayout.jsx`:

```jsx
import { Link, Outlet } from "react-router-dom";
import { useAuth } from "../../auth/useAuth";

const linkStyle = {
  color: "#f5f1e8",
  textDecoration: "none",
  fontWeight: 600
};

export default function MainLayout() {
  const { isAuthenticated, user, logout } = useAuth();
  const isAdmin = user?.role === "Admin";

  return (
    <div
      style={{
        minHeight: "100vh",
        background:
          "radial-gradient(circle at top, #244b5a 0%, #16313b 40%, #0d1f26 100%)",
        color: "#f5f1e8",
        fontFamily: "Georgia, serif"
      }}
    >
      <header
        style={{
          display: "flex",
          justifyContent: "space-between",
          alignItems: "center",
          padding: "20px 32px",
          borderBottom: "1px solid rgba(245, 241, 232, 0.2)"
        }}
      >
        <Link to="/" style={{ ...linkStyle, fontSize: 28 }}>
          VibeCourseAI
        </Link>

        <nav style={{ display: "flex", gap: 20, alignItems: "center" }}>
          <Link to="/" style={linkStyle}>
            Trang chủ
          </Link>

          <Link to="/courses" style={linkStyle}>
            Khóa học
          </Link>

          {isAuthenticated ? (
            <>
              {isAdmin ? (
                <Link to="/dashboard" style={linkStyle}>
                  Dashboard
                </Link>
              ) : null}

              <span>{user?.fullName}</span>
              <Link to="/profile" style={linkStyle}>
                Hồ sơ
              </Link>
              <Link to="/change-password" style={linkStyle}>
                Đổi mật khẩu
              </Link>
              <button
                type="button"
                onClick={logout}
                style={{ background: "transparent", border: "none", color: "#f5f1e8", fontWeight: 600 }}
              >
                Đăng xuất
              </button>
            </>
          ) : (
            <>
              <Link to="/login" style={linkStyle}>
                Đăng nhập
              </Link>
              <Link to="/register" style={linkStyle}>
                Đăng ký
              </Link>
            </>
          )}
        </nav>
      </header>
      <main style={{ padding: 32 }}>
        <Outlet />
      </main>
    </div>
  );
}
```

- [ ] **Step 4: Run the navbar test to verify it passes**

Run:

```bash
docker run --rm -v /home/phan-duong-quoc-nhat/workspace/VibeCourseAI/frontend:/app -w /app node:24-alpine sh -lc "npm install && npx vitest run src/components/layout/MainLayout.test.jsx"
```

Expected:

```text
PASS
```

- [ ] **Step 5: Commit**

```bash
git add frontend/src/components/layout/MainLayout.jsx frontend/src/components/layout/MainLayout.test.jsx
git commit -m "feat: add auth-aware vietnamese navbar"
```

### Task 6: Add Refresh Interceptor, Logout, Profile, And Change Password

**Files:**
- Modify: `frontend/src/auth/AuthContext.jsx`
- Modify: `frontend/src/api/axiosClient.js`
- Create: `frontend/src/pages/ProfilePage.jsx`
- Create: `frontend/src/pages/ChangePasswordPage.jsx`
- Modify: `frontend/src/routes/AppRoutes.jsx`

- [ ] **Step 1: Extend auth context for logout and change-password**

Update `frontend/src/auth/AuthContext.jsx` to add:

```jsx
  async function logout() {
    try {
      if (session?.refreshToken) {
        await authService.logout(session.refreshToken);
      }
    } finally {
      clearAuthSession();
      setSession(null);
      setUser(null);
    }
  }

  async function changePassword(payload) {
    await authService.changePassword(payload);
    await logout();
  }
```

Expose these in the context value:

```jsx
logout,
changePassword
```

- [ ] **Step 2: Add refresh-aware axios interceptor**

Replace `frontend/src/api/axiosClient.js` with:

```js
import axios from "axios";
import { clearAuthSession, loadAuthSession, saveAuthSession } from "../auth/authStorage";

export const axiosClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || "http://localhost:5000/api",
  headers: {
    "Content-Type": "application/json"
  }
});

let refreshPromise = null;

axiosClient.interceptors.request.use((config) => {
  const session = loadAuthSession();
  if (session?.accessToken) {
    config.headers.Authorization = `Bearer ${session.accessToken}`;
  }
  return config;
});

axiosClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;
    const session = loadAuthSession();

    if (!session?.refreshToken || originalRequest?._retry || error?.response?.status !== 401) {
      throw error;
    }

    originalRequest._retry = true;

    if (!refreshPromise) {
      refreshPromise = axios.post(
        `${import.meta.env.VITE_API_BASE_URL || "http://localhost:5000/api"}/auth/refresh`,
        { refreshToken: session.refreshToken },
        { headers: { "Content-Type": "application/json" } }
      ).then((response) => {
        const nextSession = {
          accessToken: response.data.accessToken,
          refreshToken: response.data.refreshToken,
          user: response.data.user
        };
        saveAuthSession(nextSession);
        return nextSession;
      }).finally(() => {
        refreshPromise = null;
      });
    }

    try {
      const nextSession = await refreshPromise;
      originalRequest.headers.Authorization = `Bearer ${nextSession.accessToken}`;
      return axiosClient(originalRequest);
    } catch (refreshError) {
      clearAuthSession();
      window.alert("Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.");
      window.location.href = "/login";
      throw refreshError;
    }
  }
);
```

- [ ] **Step 3: Add profile and change password pages**

Create `frontend/src/pages/ProfilePage.jsx`:

```jsx
import { useAuth } from "../auth/useAuth";

export default function ProfilePage() {
  const { user } = useAuth();

  return (
    <section>
      <h1 style={{ fontSize: 42, marginBottom: 12 }}>Hồ sơ</h1>
      <p>Họ và tên: {user?.fullName}</p>
      <p>Email: {user?.email}</p>
      <p>Vai trò: {user?.role}</p>
    </section>
  );
}
```

Create `frontend/src/pages/ChangePasswordPage.jsx`:

```jsx
import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "../auth/useAuth";

export default function ChangePasswordPage() {
  const navigate = useNavigate();
  const { changePassword } = useAuth();
  const [formData, setFormData] = useState({ currentPassword: "", newPassword: "" });
  const [message, setMessage] = useState("");
  const [errorMessage, setErrorMessage] = useState("");

  async function handleSubmit(event) {
    event.preventDefault();
    setMessage("");
    setErrorMessage("");

    try {
      await changePassword(formData);
      setMessage("Đổi mật khẩu thành công. Vui lòng đăng nhập lại.");
      navigate("/login");
    } catch {
      setErrorMessage("Không thể đổi mật khẩu. Vui lòng kiểm tra lại thông tin.");
    }
  }

  return (
    <section style={{ maxWidth: 480 }}>
      <h1 style={{ fontSize: 42, marginBottom: 12 }}>Đổi mật khẩu</h1>
      <form onSubmit={handleSubmit} style={{ display: "grid", gap: 16 }}>
        <label htmlFor="currentPassword" style={{ display: "grid", gap: 8 }}>
          <span>Mật khẩu hiện tại</span>
          <input
            id="currentPassword"
            type="password"
            value={formData.currentPassword}
            onChange={(event) => setFormData((current) => ({ ...current, currentPassword: event.target.value }))}
          />
        </label>

        <label htmlFor="newPassword" style={{ display: "grid", gap: 8 }}>
          <span>Mật khẩu mới</span>
          <input
            id="newPassword"
            type="password"
            value={formData.newPassword}
            onChange={(event) => setFormData((current) => ({ ...current, newPassword: event.target.value }))}
          />
        </label>

        {message ? <p>{message}</p> : null}
        {errorMessage ? <p style={{ color: "#ffd6d6" }}>{errorMessage}</p> : null}

        <button type="submit">Cập nhật mật khẩu</button>
      </form>
    </section>
  );
}
```

Update `frontend/src/routes/AppRoutes.jsx` to include:

```jsx
import ProfilePage from "../pages/ProfilePage";
import ChangePasswordPage from "../pages/ChangePasswordPage";
```

and routes:

```jsx
        <Route
          path="/profile"
          element={
            <RequireAuth>
              <ProfilePage />
            </RequireAuth>
          }
        />
        <Route
          path="/change-password"
          element={
            <RequireAuth>
              <ChangePasswordPage />
            </RequireAuth>
          }
        />
```

- [ ] **Step 4: Run the frontend test suite**

Run:

```bash
docker run --rm -v /home/phan-duong-quoc-nhat/workspace/VibeCourseAI/frontend:/app -w /app node:24-alpine sh -lc "npm install && npm run test -- --run"
```

Expected:

```text
PASS
```

- [ ] **Step 5: Commit**

```bash
git add frontend/src/auth/AuthContext.jsx frontend/src/api/axiosClient.js frontend/src/pages/ProfilePage.jsx frontend/src/pages/ChangePasswordPage.jsx frontend/src/routes/AppRoutes.jsx
git commit -m "feat: add frontend auth session lifecycle"
```

### Task 7: Runtime Verification Of UI Auth Flow

**Files:**
- Verify only

- [ ] **Step 1: Rebuild and restart frontend**

Run:

```bash
docker compose build frontend
docker compose up -d --force-recreate frontend
```

Expected:

```text
frontend Built
course_frontend Started
```

- [ ] **Step 2: Verify frontend responds**

Run:

```bash
curl -I http://localhost:3000
```

Expected:

```text
HTTP/1.1 200 OK
```

- [ ] **Step 3: Manual UI verification**

Verify in browser:

- home page opens at `/`
- signed-out navbar shows `Đăng nhập` and `Đăng ký`
- login works and redirects to `/`
- register works and redirects to `/`
- signed-in navbar shows user full name
- account controls show `Hồ sơ`, `Đổi mật khẩu`, `Đăng xuất`
- admin user sees `Dashboard`
- signed-out access to `/courses` redirects to login

- [ ] **Step 4: Commit**

```bash
git add .
git commit -m "test: verify frontend auth integration"
```

---

## Self-Review

### Spec Coverage

- redirect home after login/register: Task 3
- `localStorage` session persistence: Task 1 and Task 2
- startup `me` bootstrap: Task 2
- refresh token retry: Task 6
- clear session and redirect on refresh failure: Task 6
- protect `Courses`: Task 4
- admin `Dashboard` navbar entry: Task 5
- account dropdown items: Task 5
- `Hồ sơ` and `Đổi mật khẩu`: Task 6
- Vietnamese auth UI and messages: Tasks 3, 4, 5, 6

### Placeholder Scan

- No `TBD` or `TODO`
- All file paths and commands are explicit
- Each code step contains concrete code

### Type Consistency Check

- Auth state consistently uses `accessToken`, `refreshToken`, and `user`
- `AuthContext` remains the single source of UI auth state
- `RequireAuth` is the only route guard
- Vietnamese labels stay consistent across pages and navbar
