# Course Video System Scaffold Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Dựng bộ khung dự án gồm backend ASP.NET Core Web API theo layered architecture, frontend React skeleton, ai-worker placeholder, Docker Compose và kết nối MySQL bằng EF Core Code First.

**Architecture:** Backend dùng một Web API project, chia theo `Controllers`, `Services`, `Repositories`, `DTOs`, `Models`, `Data`, `Configurations`, `Middlewares`. Frontend là React app tối giản có routing cơ bản. Toàn hệ thống chạy qua `docker-compose` với `mysql`, `backend`, `frontend`, `ai-worker`, trong đó backend đọc connection string từ environment và seed dữ liệu role cơ bản.

**Tech Stack:** ASP.NET Core Web API, EF Core, Pomelo MySQL provider, React, Vite, Python FastAPI, Docker Compose, MySQL 8.

---

### Task 1: Khởi tạo cấu trúc thư mục và file môi trường

**Files:**
- Create: `backend/`
- Create: `frontend/`
- Create: `ai-worker/`
- Create: `storage/syllabuses/.gitkeep`
- Create: `storage/slides/.gitkeep`
- Create: `storage/audio/.gitkeep`
- Create: `storage/videos/.gitkeep`
- Create: `storage/thumbnails/.gitkeep`
- Create: `.env.example`
- Create: `.gitignore`

- [ ] **Step 1: Viết kiểm tra tối thiểu cho cấu trúc repo**

```bash
test -d backend && echo "backend exists"
```

Expected: command fails initially because `backend/` chưa tồn tại.

- [ ] **Step 2: Tạo cấu trúc thư mục tối thiểu**

```text
backend/
frontend/
ai-worker/
storage/
  syllabuses/
  slides/
  audio/
  videos/
  thumbnails/
```

- [ ] **Step 3: Tạo `.env.example`**

```env
MYSQL_ROOT_PASSWORD=root
MYSQL_DATABASE=course_video_db
MYSQL_USER=course_user
MYSQL_PASSWORD=course_password
MYSQL_PORT=3307
BACKEND_PORT=5000
FRONTEND_PORT=3000
AI_WORKER_PORT=8000
ASPNETCORE_ENVIRONMENT=Development
CONNECTION_STRING=server=mysql;port=3306;database=course_video_db;user=course_user;password=course_password
```

- [ ] **Step 4: Tạo `.gitignore`**

```gitignore
.env
**/bin/
**/obj/
**/node_modules/
**/dist/
**/.pytest_cache/
**/__pycache__/
storage/audio/*
storage/videos/*
storage/slides/*
storage/thumbnails/*
!storage/**/.gitkeep
```

- [ ] **Step 5: Xác minh cấu trúc đã được tạo**

Run: `find backend frontend ai-worker storage -maxdepth 2 -type d | sort`

Expected: hiển thị đầy đủ các thư mục vừa tạo.

### Task 2: Dựng backend ASP.NET Core Web API skeleton và cấu hình EF Core

**Files:**
- Create: `backend/CourseVideo.sln`
- Create: `backend/CourseVideo.API/CourseVideo.API.csproj`
- Create: `backend/CourseVideo.API/Program.cs`
- Create: `backend/CourseVideo.API/appsettings.json`
- Create: `backend/CourseVideo.API/appsettings.Development.json`
- Create: `backend/CourseVideo.API/Controllers/HealthController.cs`
- Create: `backend/CourseVideo.API/Controllers/AuthController.cs`
- Create: `backend/CourseVideo.API/Controllers/CoursesController.cs`
- Create: `backend/CourseVideo.API/DTOs/Auth/LoginRequest.cs`
- Create: `backend/CourseVideo.API/DTOs/Courses/CourseResponse.cs`
- Create: `backend/CourseVideo.API/Models/BaseEntity.cs`
- Create: `backend/CourseVideo.API/Models/Role.cs`
- Create: `backend/CourseVideo.API/Models/User.cs`
- Create: `backend/CourseVideo.API/Models/Course.cs`
- Create: `backend/CourseVideo.API/Data/AppDbContext.cs`
- Create: `backend/CourseVideo.API/Data/DbInitializer.cs`
- Create: `backend/CourseVideo.API/Services/Interfaces/IAuthService.cs`
- Create: `backend/CourseVideo.API/Services/Interfaces/ICourseService.cs`
- Create: `backend/CourseVideo.API/Services/AuthService.cs`
- Create: `backend/CourseVideo.API/Services/CourseService.cs`
- Create: `backend/CourseVideo.API/Repositories/Interfaces/ICourseRepository.cs`
- Create: `backend/CourseVideo.API/Repositories/CourseRepository.cs`
- Create: `backend/CourseVideo.API/Properties/launchSettings.json`
- Create: `backend/CourseVideo.API/Dockerfile`

- [ ] **Step 1: Kiểm tra khả năng dùng `dotnet` CLI**

Run: `which dotnet`

Expected: nếu trống, không thể scaffold bằng CLI và phải tạo file thủ công bằng patch; nếu có đường dẫn, có thể dùng `dotnet new`.

- [ ] **Step 2: Tạo project file với dependency cần thiết**

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.8">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="Pomelo.EntityFrameworkCore.MySql" Version="8.0.2" />
    <PackageReference Include="Swashbuckle.AspNetCore" Version="6.6.2" />
  </ItemGroup>
</Project>
```

- [ ] **Step 3: Tạo `AppDbContext` và entity tối thiểu**

```csharp
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Course> Courses => Set<Course>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Role>().HasData(
            new Role { Id = 1, Name = "Admin" },
            new Role { Id = 2, Name = "User" }
        );
    }
}
```

- [ ] **Step 4: Tạo `Program.cs` với đăng ký DI, EF Core, Swagger, CORS**

```csharp
var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? builder.Configuration["CONNECTION_STRING"]
    ?? throw new InvalidOperationException("Missing database connection string.");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

builder.Services.AddScoped<ICourseRepository, CourseRepository>();
builder.Services.AddScoped<ICourseService, CourseService>();
builder.Services.AddScoped<IAuthService, AuthService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    DbInitializer.Seed(db);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("Frontend");
app.UseAuthorization();
app.MapControllers();
app.Run();
```

- [ ] **Step 5: Tạo controller và service mẫu để health/API hoạt động**

```csharp
[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { status = "ok" });
}
```

- [ ] **Step 6: Tạo Dockerfile backend**

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY CourseVideo.API.csproj ./
RUN dotnet restore
COPY . ./
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "CourseVideo.API.dll"]
```

- [ ] **Step 7: Xác minh backend build được trong Docker**

Run: `docker build -t course-backend-test backend/CourseVideo.API`

Expected: build thành công và tạo image `course-backend-test`.

### Task 3: Dựng frontend React skeleton

**Files:**
- Create: `frontend/package.json`
- Create: `frontend/vite.config.js`
- Create: `frontend/index.html`
- Create: `frontend/src/main.jsx`
- Create: `frontend/src/App.jsx`
- Create: `frontend/src/routes/AppRoutes.jsx`
- Create: `frontend/src/api/axiosClient.js`
- Create: `frontend/src/components/layout/MainLayout.jsx`
- Create: `frontend/src/pages/LoginPage.jsx`
- Create: `frontend/src/pages/RegisterPage.jsx`
- Create: `frontend/src/pages/DashboardPage.jsx`
- Create: `frontend/src/pages/CoursesPage.jsx`
- Create: `frontend/nginx.conf`
- Create: `frontend/Dockerfile`

- [ ] **Step 1: Tạo `package.json` cho React + Vite**

```json
{
  "name": "course-video-frontend",
  "private": true,
  "version": "0.0.1",
  "type": "module",
  "scripts": {
    "dev": "vite",
    "build": "vite build"
  },
  "dependencies": {
    "axios": "^1.7.2",
    "react": "^18.3.1",
    "react-dom": "^18.3.1",
    "react-router-dom": "^6.26.0"
  },
  "devDependencies": {
    "@vitejs/plugin-react": "^4.3.1",
    "vite": "^5.4.2"
  }
}
```

- [ ] **Step 2: Tạo routing cơ bản**

```jsx
export default function AppRoutes() {
  return (
    <Routes>
      <Route path="/" element={<Navigate to="/dashboard" replace />} />
      <Route path="/login" element={<LoginPage />} />
      <Route path="/register" element={<RegisterPage />} />
      <Route element={<MainLayout />}>
        <Route path="/dashboard" element={<DashboardPage />} />
        <Route path="/courses" element={<CoursesPage />} />
      </Route>
    </Routes>
  );
}
```

- [ ] **Step 3: Tạo axios client trỏ đến backend container**

```js
import axios from "axios";

export const axiosClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || "http://localhost:5000/api",
  headers: {
    "Content-Type": "application/json"
  }
});
```

- [ ] **Step 4: Tạo Dockerfile frontend**

```dockerfile
FROM node:24-alpine AS build
WORKDIR /app
COPY package.json ./
RUN npm install
COPY . ./
RUN npm run build

FROM nginx:stable-alpine
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
```

- [ ] **Step 5: Xác minh frontend build được trong Docker**

Run: `docker build -t course-frontend-test frontend`

Expected: build thành công và tạo image `course-frontend-test`.

### Task 4: Dựng ai-worker placeholder và Docker Compose

**Files:**
- Create: `ai-worker/app/main.py`
- Create: `ai-worker/requirements.txt`
- Create: `ai-worker/Dockerfile`
- Create: `docker-compose.yml`

- [ ] **Step 1: Tạo app placeholder cho worker**

```python
from fastapi import FastAPI

app = FastAPI(title="Course Video AI Worker")

@app.get("/health")
def health():
    return {"status": "ok"}

@app.get("/jobs/ping")
def ping():
    return {"message": "worker ready"}
```

- [ ] **Step 2: Tạo `requirements.txt`**

```txt
fastapi==0.115.0
uvicorn[standard]==0.30.6
```

- [ ] **Step 3: Tạo Dockerfile worker**

```dockerfile
FROM python:3.12-slim
WORKDIR /app
COPY requirements.txt ./
RUN pip install --no-cache-dir -r requirements.txt
COPY app ./app
EXPOSE 8000
CMD ["uvicorn", "app.main:app", "--host", "0.0.0.0", "--port", "8000"]
```

- [ ] **Step 4: Tạo `docker-compose.yml`**

```yaml
services:
  mysql:
    image: mysql:8.0
    container_name: course_mysql
    restart: unless-stopped
    env_file:
      - .env
    environment:
      MYSQL_ROOT_PASSWORD: ${MYSQL_ROOT_PASSWORD}
      MYSQL_DATABASE: ${MYSQL_DATABASE}
      MYSQL_USER: ${MYSQL_USER}
      MYSQL_PASSWORD: ${MYSQL_PASSWORD}
    ports:
      - "${MYSQL_PORT}:3306"
    volumes:
      - mysql_data:/var/lib/mysql

  backend:
    build:
      context: ./backend/CourseVideo.API
    container_name: course_backend
    restart: unless-stopped
    env_file:
      - .env
    environment:
      ASPNETCORE_ENVIRONMENT: ${ASPNETCORE_ENVIRONMENT}
      CONNECTION_STRING: ${CONNECTION_STRING}
    ports:
      - "${BACKEND_PORT}:8080"
    depends_on:
      - mysql
    volumes:
      - ./storage:/app/storage

  frontend:
    build:
      context: ./frontend
    container_name: course_frontend
    restart: unless-stopped
    ports:
      - "${FRONTEND_PORT}:80"
    depends_on:
      - backend

  ai-worker:
    build:
      context: ./ai-worker
    container_name: course_ai_worker
    restart: unless-stopped
    ports:
      - "${AI_WORKER_PORT}:8000"
    depends_on:
      - mysql
    volumes:
      - ./storage:/app/storage

volumes:
  mysql_data:
```

- [ ] **Step 5: Xác minh file compose hợp lệ**

Run: `cp .env.example .env && docker compose config`

Expected: compose render thành công, không có lỗi syntax.

### Task 5: Kiểm tra chạy tích hợp toàn hệ thống

**Files:**
- Modify: `backend/CourseVideo.API/Program.cs`
- Modify: `docker-compose.yml`
- Verify: toàn bộ workspace

- [ ] **Step 1: Chạy toàn hệ thống**

Run: `cp .env.example .env && docker compose up --build -d`

Expected: `mysql`, `backend`, `frontend`, `ai-worker` đều ở trạng thái running hoặc healthy.

- [ ] **Step 2: Kiểm tra health backend**

Run: `curl http://localhost:5000/api/health`

Expected:

```json
{"status":"ok"}
```

- [ ] **Step 3: Kiểm tra health worker**

Run: `curl http://localhost:8000/health`

Expected:

```json
{"status":"ok"}
```

- [ ] **Step 4: Kiểm tra frontend trả HTML**

Run: `curl -I http://localhost:3000`

Expected: HTTP `200 OK`.

- [ ] **Step 5: Kiểm tra backend đã seed role**

Run:

```bash
docker compose exec -T mysql mysql -u${MYSQL_USER} -p${MYSQL_PASSWORD} ${MYSQL_DATABASE} -e "SELECT Id, Name FROM Roles;"
```

Expected: có 2 dòng `Admin` và `User`.

- [ ] **Step 6: Nếu `dotnet` không có sẵn trên máy host, dùng Docker để thay thế mọi thao tác CLI**

```bash
docker run --rm -v "$PWD/backend:/workspace" -w /workspace mcr.microsoft.com/dotnet/sdk:8.0 dotnet --info
```

Expected: hiển thị thông tin SDK .NET 8 trong container.

## Self-Review

- Spec coverage: plan bao phủ scaffold backend, frontend, ai-worker, Docker, MySQL, storage, kết nối DB và seed role cơ bản.
- Placeholder scan: không dùng `TBD`, `TODO`, hay tham chiếu mơ hồ.
- Type consistency: tên project, đường dẫn, biến môi trường và service names thống nhất với spec.
