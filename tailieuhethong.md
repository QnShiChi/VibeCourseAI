Nội dung đoạn chat: Thiết kế hệ thống tạo video khóa học bằng đề cương

Tài liệu này tổng hợp toàn bộ nội dung chính của đoạn chat hiện tại về kế hoạch thiết kế hệ thống tạo video khóa học từ đề cương, bao gồm yêu cầu ban đầu của người dùng, phản hồi kế hoạch của trợ lý và yêu cầu xuất file Word.

Lượt 1 - User
Tôi đang muốn làm một hệ thống Tạo video khóa học bằng đề cương, có phân quyền vai trò Admin và người dùng, với vai trò admin thì họ sẽ có quyền import đề cương đề tạo module khóa học và hệ thống sẽ tự động tạo video bài giảng bám sát theo cái đề cương đó (video ở đây là theo dạng slide và có giọng người đọc giảng dạy nội dung để tạo thành một video). Giọng nói tôi dữ định là sử dụng thư viện opensource Vibe Voice của Microsoft. Người dùng khi đăng ký xong rồi đăng nhập sẽ có thể truy cập vào và học những cái bài học mà người admin đã import vào trước đó. Trước mắt thì chúng ta hãy lên kế hoạch thiết kế hệ thống cho chức năng này trước sau đó thì chúng ta sẽ phát triển tiếp những chức năng khác theo sau. Công nghệ tôi yêu cầu sử dụng ở đây là ASP.NET Core Web API cho backend code theo kiến trúc như sau: có DTO cho request và reponse, service ImplService, repository, implReposetory, controller, ... đúng chuẩn solid architecture tuân thủ nguyên tắc resful API. Phần frontend thì sữ dụng ReactJS kết hợp với sử dụng tailwind css, cơ sở dữ liệu thì tôi dự định sẽ sử dụng DBaver để kết nối với MySQL. Tất cả các dịch vụ backend, frontend, db sẽ được chạy trên docker dể dễ dàng quản lý. Bạn hãy lên kế hoạch chị tiết cho hệ thống này.

Lượt 2 - Assistant cập nhật ngắn
Mình sẽ thiết kế theo hướng "làm được MVP trước, mở rộng sau": tách rõ module import đề cương, AI tạo nội dung slide, TTS tạo giọng đọc, render video, phân quyền và học bài. Mình cũng sẽ kiểm tra nhanh thông tin VibeVoice để kế hoạch không bị lệch với khả năng thực tế của thư viện.

Lượt 3 - Assistant trả lời chi tiết
Dưới đây là kế hoạch chi tiết cho hệ thống tạo video khóa học từ đề cương theo đúng công nghệ bạn yêu cầu: ASP.NET Core Web API + ReactJS + Tailwind CSS + MySQL + Docker, có phân quyền Admin/User, kiến trúc backend chuẩn DTO → Controller → Service/ImplService → Repository/ImplRepository → Database.

Lưu ý nhỏ: DBeaver không phải database, nó là công cụ để kết nối và quản lý database. Database thật của bạn sẽ là MySQL, còn DBeaver dùng để xem bảng, query dữ liệu, kiểm tra schema.

Với phần giọng nói, Microsoft VibeVoice phù hợp với ý tưởng này vì nó là mô hình open-source cho Text-to-Speech, hỗ trợ tạo giọng nói dài, nhiều speaker, và theo tài liệu Microsoft/GitHub có thể sinh speech dài tới khoảng 90 phút với tối đa 4 speaker trong bản VibeVoice-TTS.

1. Ý tưởng tổng quan hệ thống
Hệ thống sẽ hoạt động như sau:

Admin import đề cương môn học, ví dụ file .pdf, .docx, .txt, hoặc nhập text trực tiếp. Sau đó backend xử lý đề cương, tách thành các module, lesson, slide outline, rồi tự động sinh nội dung bài giảng. Tiếp theo hệ thống tạo slide, tạo giọng đọc bằng VibeVoice, ghép slide + audio thành video. Cuối cùng video được lưu lại để người dùng học.

User sau khi đăng ký và đăng nhập có thể xem danh sách khóa học, vào từng module, xem video bài giảng, theo dõi tiến độ học tập.

Luồng chính:

Admin import đề cương

↓

Backend phân tích đề cương

↓

Tạo Course → Module → Lesson

↓

Sinh nội dung slide + script giảng

↓

Tạo file slide hoặc image slide

↓

Tạo audio bằng VibeVoice

↓

Ghép slide + audio thành video

↓

User đăng nhập và học video

2. Phạm vi MVP nên làm trước
Ở giai đoạn đầu, bạn không nên làm quá nhiều chức năng. Nên tập trung vào core feature:

MVP Version 1
Admin
Admin có thể:

1. Đăng nhập.
2. Quản lý người dùng.
3. Import đề cương.
4. Xem danh sách đề cương đã import.
5. Yêu cầu hệ thống tạo khóa học từ đề cương.
6. Xem trạng thái xử lý: Pending, Processing, Completed, Failed.
7. Xem course/module/lesson được tạo.
8. Xem video bài giảng đã render.
User
User có thể:

1. Đăng ký.
2. Đăng nhập.
3. Xem danh sách khóa học.
4. Xem chi tiết khóa học.
5. Xem danh sách module và lesson.
6. Xem video bài giảng.
7. Đánh dấu lesson đã học.
3. Kiến trúc tổng thể hệ thống
Nên tách hệ thống thành nhiều service trong Docker:

course-video-system/

│

├── backend/ ASP.NET Core Web API

├── frontend/ ReactJS + Tailwind CSS

├── ai-worker/ Python worker xử lý AI, TTS, video

├── mysql/ MySQL database

├── storage/ Lưu file đề cương, audio, slide, video

└── docker-compose.yml

Vì sao nên có ai-worker riêng?
Vì tạo slide, tạo audio, render video là tác vụ nặng. Nếu xử lý trực tiếp trong ASP.NET Core API thì request dễ bị timeout, server dễ bị treo.

Nên backend chỉ nhận yêu cầu và tạo job. Sau đó ai-worker xử lý job ở background.

Kiến trúc hợp lý:

React Frontend

↓

ASP.NET Core Web API

↓

MySQL

↓

Background Job / Queue

↓

Python AI Worker

↓

VibeVoice + FFmpeg

↓

Storage video/audio/slide

4. Công nghệ sử dụng
Backend
Dùng:

ASP.NET Core Web API

Entity Framework Core

MySQL

JWT Authentication

Role-based Authorization

AutoMapper

FluentValidation

Swagger

Background Job hoặc Queue

Frontend
Dùng:

ReactJS

React Router DOM

Axios

Tailwind CSS

React Hook Form

Zustand hoặc Redux Toolkit

Database
Dùng:

MySQL

DBeaver để quản lý database

AI / Media Processing
Dùng:

Python

VibeVoice TTS

FFmpeg

LibreOffice hoặc HTML-to-image để tạo slide

VibeVoice nên được đặt ở ai-worker vì nó phù hợp môi trường Python hơn. Backend ASP.NET Core sẽ gọi worker thông qua REST API nội bộ, message queue, hoặc database job polling.

5. Phân quyền hệ thống
Bạn có 2 vai trò chính:

Admin
Có quyền:

Quản lý user

Import đề cương

Tạo khóa học tự động

Quản lý khóa học

Quản lý module

Quản lý lesson

Xem trạng thái tạo video

Xóa/sửa khóa học

Publish hoặc Unpublish course

User
Có quyền:

Đăng ký

Đăng nhập

Xem khóa học đã publish

Xem video bài học

Theo dõi tiến độ học

Cập nhật profile

Trong ASP.NET Core, bạn dùng:

[Authorize(Roles = "Admin")]

[Authorize(Roles = "User")]

[Authorize]

6. Các module chính của backend
Backend nên chia thành các module sau:

Auth Module

User Module

Role Module

Course Module

Module Module

Lesson Module

Syllabus Import Module

AI Generation Job Module

Media Module

Learning Progress Module

Chi tiết:

6.1 Auth Module
Xử lý:

Register

Login

Refresh token

Logout

Get current user

API mẫu:

POST /api/auth/register

POST /api/auth/login

POST /api/auth/refresh-token

GET /api/auth/me

6.2 User Module
Xử lý:

Admin xem danh sách user

Admin đổi role user

Admin khóa/mở khóa user

User xem/cập nhật profile

API mẫu:

GET /api/users

GET /api/users/{id}

PUT /api/users/{id}/role

PUT /api/users/{id}/status

PUT /api/users/profile

6.3 Syllabus Import Module
Đây là module quan trọng nhất.

Admin có thể upload đề cương. Hệ thống lưu file và tạo record trong database.

API mẫu:

POST /api/syllabuses/import

GET /api/syllabuses

GET /api/syllabuses/{id}

DELETE /api/syllabuses/{id}

Ví dụ request upload:

POST /api/syllabuses/import

Content-Type: multipart/form-data

file: de-cuong-mon-hoc.pdf

title: Lập trình Web

description: Đề cương môn lập trình web

6.4 Course Generation Module
Sau khi import đề cương, Admin bấm nút:

Generate Course

Backend tạo một job xử lý.

API mẫu:

POST /api/courses/generate-from-syllabus/{syllabusId}

GET /api/generation-jobs/{jobId}

Job sẽ có status:

Pending

Processing

GeneratingOutline

GeneratingSlides

GeneratingAudio

RenderingVideo

Completed

Failed

6.5 Course Module
Quản lý khóa học.

API mẫu:

GET /api/courses

GET /api/courses/{id}

POST /api/courses

PUT /api/courses/{id}

DELETE /api/courses/{id}

PUT /api/courses/{id}/publish

PUT /api/courses/{id}/unpublish

User chỉ xem được khóa học đã publish. Admin xem được tất cả.

6.6 Lesson Module
Quản lý bài học.

API mẫu:

GET /api/courses/{courseId}/lessons

GET /api/lessons/{id}

PUT /api/lessons/{id}

DELETE /api/lessons/{id}

Một lesson sẽ có:

Title

Summary

Slide content

Script giảng

Audio URL

Video URL

Duration

OrderIndex

6.7 Learning Progress Module
Theo dõi tiến độ học của user.

API mẫu:

POST /api/learning-progress/lessons/{lessonId}/complete

GET /api/learning-progress/courses/{courseId}

Ví dụ:

User A học Course 1

Đã hoàn thành 5/20 bài

Progress = 25%

7. Thiết kế database MySQL
Các bảng chính nên có:

7.1 Users
Users

- Id

- FullName

- Email

- PasswordHash

- AvatarUrl

- RoleId

- IsActive

- CreatedAt

- UpdatedAt

7.2 Roles
Roles

- Id

- Name

Data mẫu:

1 - Admin

2 - User

7.3 Syllabuses
Lưu thông tin đề cương admin upload.

Syllabuses

- Id

- Title

- Description

- OriginalFileName

- FileUrl

- FileType

- ExtractedText

- UploadedByUserId

- CreatedAt

7.4 Courses
Courses

- Id

- Title

- Description

- ThumbnailUrl

- SyllabusId

- Status

- IsPublished

- CreatedByUserId

- CreatedAt

- UpdatedAt

Status có thể là:

Draft

Generating

Completed

Failed

7.5 CourseModules
Không nên đặt tên bảng là Modules vì dễ trùng nghĩa. Nên dùng CourseModules.

CourseModules

- Id

- CourseId

- Title

- Description

- OrderIndex

- CreatedAt

7.6 Lessons
Lessons

- Id

- CourseModuleId

- Title

- Summary

- SlideContentJson

- TeachingScript

- AudioUrl

- VideoUrl

- DurationInSeconds

- OrderIndex

- Status

- CreatedAt

- UpdatedAt

SlideContentJson có thể lưu dạng:

[

{

"slideNumber": 1,

"title": "Giới thiệu bài học",

"bulletPoints": [

"Khái niệm cơ bản",

"Mục tiêu bài học"

],

"speakerNotes": "Ở phần này chúng ta sẽ..."

}

]

7.7 GenerationJobs
Bảng này rất quan trọng để theo dõi tiến trình tạo video.

GenerationJobs

- Id

- SyllabusId

- CourseId

- Status

- CurrentStep

- ProgressPercent

- ErrorMessage

- StartedAt

- CompletedAt

- CreatedAt

Ví dụ:

Status: Processing

CurrentStep: GeneratingAudio

ProgressPercent: 65

7.8 MediaFiles
Lưu thông tin file audio, slide, video.

MediaFiles

- Id

- LessonId

- FileType

- FileUrl

- FileName

- FileSize

- CreatedAt

FileType:

Syllabus

SlideImage

Audio

Video

Thumbnail

7.9 LearningProgress
LearningProgress

- Id

- UserId

- CourseId

- LessonId

- IsCompleted

- CompletedAt

- LastWatchedSecond

- UpdatedAt

8. Kiến trúc backend ASP.NET Core
Cấu trúc thư mục backend nên như sau:

backend/

│

├── CourseVideo.Api/

│ ├── Controllers/

│ ├── Middlewares/

│ ├── Program.cs

│ └── appsettings.json

│

├── CourseVideo.Application/

│ ├── DTOs/

│ │ ├── Requests/

│ │ └── Responses/

│ ├── Interfaces/

│ │ ├── Services/

│ │ └── Repositories/

│ ├── Services/

│ ├── Validators/

│ └── Mappings/

│

├── CourseVideo.Domain/

│ ├── Entities/

│ ├── Enums/

│ └── Common/

│

├── CourseVideo.Infrastructure/

│ ├── Data/

│ ├── Repositories/

│ ├── ExternalServices/

│ └── FileStorage/

│

└── CourseVideo.Tests/

9. Ví dụ kiến trúc chuẩn cho một module
Ví dụ với Course.

Entity
public class Course

{

public Guid Id { get; set; }

public string Title { get; set; }

public string Description { get; set; }

public string? ThumbnailUrl { get; set; }

public bool IsPublished { get; set; }

public Guid? SyllabusId { get; set; }

public Guid CreatedByUserId { get; set; }

public DateTime CreatedAt { get; set; }

}

DTO Request
public class CreateCourseRequest

{

public string Title { get; set; }

public string Description { get; set; }

}

DTO Response
public class CourseResponse

{

public Guid Id { get; set; }

public string Title { get; set; }

public string Description { get; set; }

public string? ThumbnailUrl { get; set; }

public bool IsPublished { get; set; }

}

Service Interface
public interface ICourseService

{

Task<CourseResponse> CreateAsync(CreateCourseRequest request);

Task<List<CourseResponse>> GetAllAsync();

Task<CourseResponse?> GetByIdAsync(Guid id);

Task PublishAsync(Guid id);

}

Repository Interface
public interface ICourseRepository

{

Task<Course> AddAsync(Course course);

Task<List<Course>> GetAllAsync();

Task<Course?> GetByIdAsync(Guid id);

Task UpdateAsync(Course course);

}

Controller
[ApiController]

[Route("api/courses")]

public class CoursesController : ControllerBase

{

private readonly ICourseService _courseService;

public CoursesController(ICourseService courseService)

{

_courseService = courseService;

}

[HttpGet]

public async Task<IActionResult> GetAll()

{

var result = await _courseService.GetAllAsync();

return Ok(result);

}

[HttpPost]

[Authorize(Roles = "Admin")]

public async Task<IActionResult> Create(CreateCourseRequest request)

{

var result = await _courseService.CreateAsync(request);

return Ok(result);

}

}

10. Nguyên tắc RESTful API nên áp dụng
Bạn nên đặt API theo tài nguyên, không đặt theo hành động quá nhiều.

Nên:

GET /api/courses

GET /api/courses/{id}

POST /api/courses

PUT /api/courses/{id}

DELETE /api/courses/{id}

Không nên:

POST /api/createCourse

POST /api/deleteCourse

POST /api/getCourseById

Với action đặc biệt như publish thì có thể dùng:

PUT /api/courses/{id}/publish

PUT /api/courses/{id}/unpublish

Với tạo khóa học từ đề cương:

POST /api/syllabuses/{id}/generate-course

11. Quy trình xử lý tạo video từ đề cương
Đây là phần quan trọng nhất.

Bước 1: Admin import đề cương
Admin upload file:

de-cuong-lap-trinh-web.pdf

Backend lưu:

/storage/syllabuses/de-cuong-lap-trinh-web.pdf

Database lưu record vào bảng Syllabuses.

Bước 2: Extract text từ đề cương
Backend hoặc AI worker đọc file và trích xuất text.

Nếu file là:

PDF → dùng thư viện extract PDF

DOCX → dùng thư viện đọc docx

TXT → đọc trực tiếp

Sau đó lưu text vào:

Syllabuses.ExtractedText

Bước 3: Phân tích đề cương thành cấu trúc khóa học
AI tạo ra cấu trúc:

{

"courseTitle": "Lập trình Web cơ bản",

"description": "Khóa học giúp sinh viên hiểu nền tảng lập trình web.",

"modules": [

{

"title": "Tổng quan về Web",

"lessons": [

{

"title": "Client và Server",

"objectives": [

"Hiểu client là gì",

"Hiểu server là gì",

"Hiểu mô hình request-response"

]

}

]

}

]

}

Backend lưu thành:

Courses

CourseModules

Lessons

Bước 4: Sinh nội dung slide cho từng lesson
Với mỗi lesson, hệ thống sinh:

Slide title

Bullet points

Speaker notes

Teaching script

Ví dụ:

[

{

"slideNumber": 1,

"title": "Client và Server là gì?",

"bulletPoints": [

"Client là phía gửi yêu cầu",

"Server là phía xử lý và phản hồi",

"Hai bên giao tiếp qua HTTP"

],

"speakerNotes": "Trong bài học này, chúng ta sẽ tìm hiểu..."

}

]

Bước 5: Tạo slide visual
Có 2 hướng:

Cách 1: HTML slide → Image
Tạo mỗi slide bằng HTML/CSS, sau đó convert sang ảnh.

Ưu điểm:

Dễ custom giao diện

Hợp với React/Tailwind style

Dễ render thành image

Cách 2: Tạo PowerPoint
Sinh file .pptx, sau đó convert thành image.

Ưu điểm:

Dễ xuất file slide cho admin tải về

Với MVP, mình khuyên dùng HTML slide → image, vì dễ kiểm soát giao diện và dễ ghép video.

Bước 6: Tạo audio bằng VibeVoice
Mỗi lesson sẽ có TeachingScript.

Ví dụ:

Xin chào các bạn. Trong bài học hôm nay, chúng ta sẽ tìm hiểu về client và server...

AI worker gửi text này cho VibeVoice để sinh audio.

VibeVoice phù hợp với long-form TTS và conversational audio, nên có thể dùng để tạo bài giảng dài hơn thay vì chỉ đọc từng câu ngắn.

Kết quả:

/storage/audio/lesson-001.wav

Bước 7: Ghép slide + audio thành video
Dùng FFmpeg.

Logic:

Slide 1 hiển thị 20 giây

Slide 2 hiển thị 25 giây

Slide 3 hiển thị 30 giây

...

Audio chạy xuyên suốt

Ghép thành video MP4

Kết quả:

/storage/videos/lesson-001.mp4

Bước 8: Cập nhật database
Sau khi render xong:

Lessons.VideoUrl = /storage/videos/lesson-001.mp4

Lessons.AudioUrl = /storage/audio/lesson-001.wav

Lessons.Status = Completed

GenerationJobs.Status = Completed

12. AI Worker nên thiết kế như thế nào?
Nên tách ai-worker bằng Python.

Cấu trúc:

ai-worker/

│

├── app/

│ ├── main.py

│ ├── services/

│ │ ├── syllabus_parser.py

│ │ ├── course_generator.py

│ │ ├── slide_generator.py

│ │ ├── tts_service.py

│ │ └── video_renderer.py

│ ├── jobs/

│ │ └── generation_job_worker.py

│ └── utils/

│

├── requirements.txt

└── Dockerfile

AI worker có thể chạy theo 2 kiểu.

Cách 1: Backend gọi REST API của worker
POST http://ai-worker:8000/generate-course

Dễ làm cho MVP.

Cách 2: Worker tự polling database
Worker kiểm tra bảng GenerationJobs:

Lấy job Pending

Đổi thành Processing

Xử lý

Cập nhật trạng thái

Cách này cũng dễ làm và không cần message queue.

Cách 3: Dùng RabbitMQ hoặc Redis Queue
Chuyên nghiệp hơn, nhưng chưa cần ở MVP.

MVP nên dùng database polling hoặc REST nội bộ.

13. Docker Compose tổng quan
Bạn sẽ có các container:

frontend

backend-api

mysql-db

ai-worker

Ví dụ cấu trúc docker-compose.yml:

services:

mysql:

image: mysql:8.0

container_name: course_mysql

environment:

MYSQL_ROOT_PASSWORD: root

MYSQL_DATABASE: course_video_db

MYSQL_USER: course_user

MYSQL_PASSWORD: course_password

ports:

- "3307:3306"

volumes:

- mysql_data:/var/lib/mysql

backend:

build: ./backend

container_name: course_backend

ports:

- "5000:8080"

depends_on:

- mysql

environment:

ConnectionStrings__DefaultConnection: server=mysql;port=3306;database=course_video_db;user=course_user;password=course_password

frontend:

build: ./frontend

container_name: course_frontend

ports:

- "3000:80"

depends_on:

- backend

ai-worker:

build: ./ai-worker

container_name: course_ai_worker

depends_on:

- mysql

volumes:

- ./storage:/app/storage

volumes:

mysql_data:

14. Frontend ReactJS nên chia như thế nào?
Cấu trúc:

frontend/

│

├── src/

│ ├── api/

│ │ ├── axiosClient.js

│ │ ├── authApi.js

│ │ ├── courseApi.js

│ │ └── syllabusApi.js

│ │

│ ├── components/

│ │ ├── common/

│ │ ├── layout/

│ │ ├── course/

│ │ └── admin/

│ │

│ ├── pages/

│ │ ├── auth/

│ │ ├── admin/

│ │ ├── user/

│ │ └── courses/

│ │

│ ├── routes/

│ │ ├── AppRoutes.jsx

│ │ ├── ProtectedRoute.jsx

│ │ └── AdminRoute.jsx

│ │

│ ├── stores/

│ │ └── authStore.js

│ │

│ ├── utils/

│ └── App.jsx

15. Các màn hình frontend cần có
Public
Login Page

Register Page

Home Page

User
Course List Page

Course Detail Page

Lesson Learning Page

My Learning Progress Page

Profile Page

Admin
Admin Dashboard

User Management Page

Syllabus Import Page

Syllabus Detail Page

Course Generation Status Page

Course Management Page

Course Detail Management Page

Lesson Management Page

16. Giao diện Admin nên có gì?
Admin Dashboard
Hiển thị:

Tổng số user

Tổng số khóa học

Tổng số đề cương đã import

Số video đã tạo thành công

Số job đang xử lý

Syllabus Import Page
Có:

Upload file đề cương

Nhập tiêu đề

Nhập mô tả

Button Import

Danh sách đề cương đã upload

Button Generate Course

Generation Status Page
Có progress:

Course: Lập trình Web

Status: GeneratingAudio

Progress: 65%

Current Lesson: Client và Server

17. Giao diện User nên có gì?
Course List Page
Hiển thị card khóa học:

Thumbnail

Title

Description

Số module

Số lesson

Progress

Button Start Learning

Lesson Learning Page
Bố cục:

Bên trái: danh sách module/lesson

Bên phải: video player

Bên dưới: mô tả bài học, tài liệu, button hoàn thành

18. Quy trình phân quyền frontend
Khi login thành công, backend trả về:

{

"accessToken": "...",

"user": {

"id": "uuid",

"fullName": "Nguyen Van A",

"email": "admin@example.com",

"role": "Admin"

}

}

Frontend lưu token vào:

localStorage hoặc httpOnly cookie

Với đồ án sinh viên, localStorage dễ làm hơn. Nhưng nếu muốn bảo mật hơn thì dùng httpOnly cookie.

Route guard:

<ProtectedRoute>

<UserLayout />

</ProtectedRoute>

<AdminRoute>

<AdminLayout />

</AdminRoute>

19. Những service backend nên có
IAuthService

IUserService

IRoleService

ISyllabusService

ICourseService

ICourseModuleService

ILessonService

IGenerationJobService

IFileStorageService

ILearningProgressService

IAiWorkerClientService

Implementation:

AuthService

UserService

RoleService

SyllabusService

CourseService

CourseModuleService

LessonService

GenerationJobService

LocalFileStorageService

LearningProgressService

AiWorkerClientService

Repository:

IUserRepository

IRoleRepository

ISyllabusRepository

ICourseRepository

ICourseModuleRepository

ILessonRepository

IGenerationJobRepository

IMediaFileRepository

ILearningProgressRepository

Implementation:

UserRepository

RoleRepository

SyllabusRepository

CourseRepository

CourseModuleRepository

LessonRepository

GenerationJobRepository

MediaFileRepository

LearningProgressRepository

20. Nguyên tắc SOLID áp dụng vào hệ thống
Single Responsibility
Mỗi service chỉ làm một việc.

Ví dụ:

SyllabusService: xử lý import đề cương

CourseService: xử lý khóa học

FileStorageService: xử lý lưu file

AiWorkerClientService: gọi AI worker

Không nên để CourseController tự xử lý upload file, parse file, tạo course, tạo video.

Open/Closed
Sau này muốn đổi VibeVoice sang TTS khác thì chỉ cần đổi implementation.

Ví dụ:

public interface ITtsService

{

Task<string> GenerateAudioAsync(string text);

}

Có thể có:

VibeVoiceTtsService

GoogleTtsService

AzureTtsService

Dependency Inversion
Controller không phụ thuộc trực tiếp vào repository.

Đúng:

Controller → Service Interface → Repository Interface

Không nên:

Controller → DbContext

21. Flow API quan trọng nhất
Flow import đề cương và tạo course
1. Admin login

2. Admin upload syllabus

3. Backend lưu file

4. Backend extract text

5. Admin bấm Generate Course

6. Backend tạo GenerationJob

7. AI Worker xử lý job

8. Backend/Worker tạo course/module/lesson

9. Worker tạo slide/audio/video

10. User vào học

API tương ứng:

POST /api/auth/login

POST /api/syllabuses/import

POST /api/syllabuses/{syllabusId}/generate-course

GET /api/generation-jobs/{jobId}

GET /api/courses/{courseId}

GET /api/lessons/{lessonId}

22. Trạng thái xử lý video nên thiết kế rõ
Enum GenerationJobStatus:

public enum GenerationJobStatus

{

Pending = 1,

Processing = 2,

Completed = 3,

Failed = 4,

Cancelled = 5

}

Enum GenerationStep:

public enum GenerationStep

{

ExtractingSyllabus = 1,

GeneratingCourseOutline = 2,

GeneratingLessons = 3,

GeneratingSlides = 4,

GeneratingAudio = 5,

RenderingVideo = 6,

SavingResult = 7

}

Frontend có thể hiển thị:

Đang phân tích đề cương...

Đang tạo nội dung bài học...

Đang tạo slide...

Đang tạo giọng đọc...

Đang render video...

Hoàn tất.

23. Cách lưu file trong MVP
MVP nên lưu local trước:

storage/

├── syllabuses/

├── slides/

├── audio/

├── videos/

└── thumbnails/

Sau này nếu deploy thật có thể chuyển sang:

AWS S3

Cloudflare R2

Azure Blob Storage

Google Cloud Storage

Nhưng khi làm đồ án, local storage + Docker volume là đủ.

24. Prompt AI tạo khóa học từ đề cương
Bạn có thể thiết kế prompt dạng:

Bạn là một chuyên gia thiết kế khóa học online.

Dựa trên đề cương sau, hãy tạo một khóa học có cấu trúc rõ ràng.

Yêu cầu:

- Chia thành nhiều module.

- Mỗi module có nhiều lesson.

- Mỗi lesson có title, objectives, summary.

- Nội dung phải bám sát đề cương.

- Không tự ý thêm nội dung không liên quan.

- Output bắt buộc là JSON hợp lệ.

Đề cương:

{{syllabus_text}}

Output mong muốn:

{

"courseTitle": "",

"courseDescription": "",

"modules": [

{

"moduleTitle": "",

"moduleDescription": "",

"lessons": [

{

"lessonTitle": "",

"objectives": [],

"summary": ""

}

]

}

]

}

25. Prompt AI tạo slide và script bài giảng
Bạn là một giảng viên đại học đang thiết kế video bài giảng.

Hãy tạo nội dung slide và lời giảng cho lesson sau.

Yêu cầu:

- Tạo từ 5 đến 8 slide.

- Mỗi slide có title ngắn gọn.

- Mỗi slide có 3 đến 5 bullet points.

- Mỗi slide có speakerNotes để giảng viên đọc.

- Giọng văn dễ hiểu, phù hợp sinh viên.

- Nội dung phải bám sát mục tiêu bài học.

- Output là JSON hợp lệ.

Lesson:

{{lesson_title}}

Objectives:

{{objectives}}

Summary:

{{summary}}

Output:

[

{

"slideNumber": 1,

"title": "",

"bulletPoints": [],

"speakerNotes": ""

}

]

26. Render video nên làm đơn giản trước
MVP không cần animation phức tạp.

Mỗi slide là một ảnh:

slide-1.png

slide-2.png

slide-3.png

Audio là:

lesson-audio.wav

FFmpeg tạo video:

lesson-video.mp4

Về sau mới thêm:

Hiệu ứng chuyển slide

Highlight text theo giọng đọc

Avatar giảng viên AI

Subtitle

Background music

27. Tính năng nên phát triển sau MVP
Sau khi core chạy ổn, bạn có thể thêm:

Quiz sau mỗi lesson

Tóm tắt bài học

Flashcards tự động

Transcript video

Subtitle tự động

Chatbot hỏi đáp theo bài học

Gợi ý lộ trình học

Export slide

Export tài liệu PDF

Đánh giá khóa học

Admin chỉnh sửa slide trước khi render video

Admin chọn giọng đọc

Admin chọn template slide

User ghi chú khi học

Đặc biệt, tính năng rất hay cho đồ án AI:

Chatbot hỏi đáp dựa trên nội dung khóa học

Ví dụ user hỏi:

Client-server là gì?

Bot chỉ trả lời dựa trên nội dung course/lesson đã tạo.

28. Roadmap phát triển theo từng giai đoạn
Giai đoạn 1: Setup nền tảng
Làm:

Docker compose

ASP.NET Core Web API

MySQL

ReactJS

Tailwind CSS

JWT Authentication

Role Admin/User

Kết quả:

User đăng ký/đăng nhập được

Admin vào dashboard được

User vào trang khóa học được

Giai đoạn 2: Course Management
Làm:

CRUD Course

CRUD Module

CRUD Lesson

Publish/Unpublish course

User xem course

User xem lesson

Kết quả:

Admin có thể tạo course thủ công

User có thể học course đã publish

Giai đoạn 3: Import đề cương
Làm:

Upload file đề cương

Lưu file

Extract text

Lưu extracted text vào DB

Hiển thị nội dung đề cương cho admin xem lại

Kết quả:

Admin upload đề cương thành công

Backend đọc được text từ đề cương

Giai đoạn 4: Tạo course tự động
Làm:

AI phân tích đề cương

Tạo Course

Tạo CourseModules

Tạo Lessons

Lưu vào database

Kết quả:

Từ một đề cương, hệ thống tự tạo khóa học có module và lesson

Giai đoạn 5: Tạo slide và script
Làm:

Sinh SlideContentJson

Sinh TeachingScript

Admin xem trước nội dung

Admin có thể chỉnh sửa nội dung trước khi render

Kết quả:

Mỗi lesson có nội dung slide và lời giảng

Giai đoạn 6: Tạo audio bằng VibeVoice
Làm:

Cài VibeVoice trong ai-worker

Nhận TeachingScript

Sinh audio

Lưu audio file

Cập nhật AudioUrl

Kết quả:

Mỗi lesson có audio bài giảng

Giai đoạn 7: Render video
Làm:

Tạo ảnh slide

Ghép slide + audio bằng FFmpeg

Lưu video

Cập nhật VideoUrl

Kết quả:

Mỗi lesson có video MP4 hoàn chỉnh

Giai đoạn 8: Learning Progress
Làm:

User xem video

Lưu tiến độ xem

Đánh dấu hoàn thành lesson

Tính phần trăm hoàn thành course

Kết quả:

User học được và theo dõi tiến độ

29. Thứ tự làm hợp lý nhất
Mình khuyên bạn làm theo thứ tự này:

1. Auth + Role

2. Course CRUD

3. Module CRUD

4. Lesson CRUD

5. User xem course/lesson

6. Import đề cương

7. Extract text

8. Generate course outline

9. Generate slide/script

10. Tạo audio

11. Render video

12. Learning progress

13. Dashboard thống kê

Đừng làm AI/video ngay từ đầu. Hãy làm hệ thống học course thủ công trước. Sau đó mới gắn AI vào.

30. Kết luận kiến trúc đề xuất
Kiến trúc tốt nhất cho hệ thống của bạn là:

Frontend:

ReactJS + Tailwind CSS

Backend:

ASP.NET Core Web API

DTO Request/Response

Controller

Service Interface

ImplService

Repository Interface

ImplRepository

Entity Framework Core

JWT + Role Authorization

Database:

MySQL

DBeaver để quản lý

AI Worker:

Python

VibeVoice

FFmpeg

Slide Renderer

Deployment:

Docker Compose

Core system nên đi theo mô hình:

Admin import đề cương

→ hệ thống tạo course/module/lesson

→ hệ thống tạo slide/script

→ VibeVoice tạo audio

→ FFmpeg tạo video

→ User học bài giảng

Nếu làm đúng lộ trình này, đồ án của bạn sẽ rất khác biệt vì không chỉ là web học online bình thường, mà là một hệ thống AI Course Video Generator: admin chỉ cần import đề cương, hệ thống tự động biến nó thành khóa học có video bài giảng.