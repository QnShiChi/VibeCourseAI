# Roadmap Đọc Code Backend

Tài liệu này chỉ tập trung vào backend `ASP.NET Core` trong thư mục `backend/CourseVideo.API`.

Mục tiêu của roadmap:
- biết nên đọc từ file nào trước,
- biết mỗi lớp trong backend nằm ở đâu,
- biết mỗi chức năng của hệ thống đi qua những file nào,
- biết các file nào liên quan trực tiếp với nhau để lần theo code.

Tài liệu này **không giải thích code chi tiết**. Nó chỉ là bản đồ để bạn tự đọc code.

---

## 1. Phạm vi backend cần đọc

Thư mục chính:
- `backend/CourseVideo.API/Program.cs`
- `backend/CourseVideo.API/appsettings.json`
- `backend/CourseVideo.API/appsettings.Development.json`
- `backend/CourseVideo.API/Configuration/`
- `backend/CourseVideo.API/Data/`
- `backend/CourseVideo.API/Models/`
- `backend/CourseVideo.API/DTOs/`
- `backend/CourseVideo.API/Repositories/`
- `backend/CourseVideo.API/Services/`
- `backend/CourseVideo.API/Controllers/`
- `backend/CourseVideo.API/Hubs/`

Chỉ nhắc tới để hiểu dependency:
- `AI_WORKER_BASE_URL`
- `VIDEO_WORKER_BASE_URL`
- thư mục `storage`

Không cần ưu tiên đọc lúc đầu:
- `frontend/`
- `ai-worker/`
- `bin/`
- `obj/`
- `Dockerfile`
- `Properties/launchSettings.json`

---

## 2. Bản đồ thư mục backend

### 2.1. Điểm khởi động ứng dụng
<!-- xong -->
Đọc trước:
- `backend/CourseVideo.API/Program.cs`

Vai trò:
- cấu hình service container,
- cấu hình JWT,
- cấu hình CORS,
- cấu hình `DbContext`,
- đăng ký repository/service/worker,
- map controller,
- map SignalR hub,
- cấu hình static files `/storage`.

### 2.2. Cấu hình hệ thống

Đọc tiếp:
- `backend/CourseVideo.API/appsettings.json`
- `backend/CourseVideo.API/appsettings.Development.json`
- `backend/CourseVideo.API/Configuration/AdminSeedOptions.cs`
- `backend/CourseVideo.API/Configuration/JwtOptions.cs`
- `backend/CourseVideo.API/Configuration/OpenRouterOptions.cs`
- `backend/CourseVideo.API/Configuration/OpenAiAudioOptions.cs`
- `backend/CourseVideo.API/Configuration/SmtpOptions.cs`
- `backend/CourseVideo.API/Configuration/LessonVoiceTutorOptions.cs`

Vai trò:
- gom tất cả config backend thành các class options,
- nối config từ `appsettings` hoặc biến môi trường vào `Program.cs`.
<!-- Xong -->

### 2.3. Tầng dữ liệu

Đọc tiếp:
- `backend/CourseVideo.API/Data/AppDbContext.cs`
- `backend/CourseVideo.API/Data/DbInitializer.cs`

Vai trò:
- `AppDbContext.cs`: khai báo `DbSet<>`, quan hệ giữa entity, index, ràng buộc, seed role.
- `DbInitializer.cs`: migrate/ensure database, thêm bảng/cột khi khởi động, seed admin.

<!-- Xong -->

### 2.4. Tầng mô hình dữ liệu

Đọc tiếp:
- `backend/CourseVideo.API/Models/BaseEntity.cs`
- `backend/CourseVideo.API/Models/Role.cs`
- `backend/CourseVideo.API/Models/User.cs`
- `backend/CourseVideo.API/Models/RefreshToken.cs`
- `backend/CourseVideo.API/Models/Category.cs`
- `backend/CourseVideo.API/Models/CategoryStatus.cs`
- `backend/CourseVideo.API/Models/Course.cs`
- `backend/CourseVideo.API/Models/Module.cs`
- `backend/CourseVideo.API/Models/Lesson.cs`
- `backend/CourseVideo.API/Models/LessonComment.cs`
- `backend/CourseVideo.API/Models/LessonCommentReaction.cs`
- `backend/CourseVideo.API/Models/Syllabus.cs`
- `backend/CourseVideo.API/Models/GenerationJob.cs`
- `backend/CourseVideo.API/Models/LessonVoiceSession.cs`
- `backend/CourseVideo.API/Models/LessonVoiceTurn.cs`
- `backend/CourseVideo.API/Models/LessonVoiceMessage.cs`
- `backend/CourseVideo.API/Models/Quiz.cs`
- `backend/CourseVideo.API/Models/QuizQuestion.cs`
- `backend/CourseVideo.API/Models/QuizOption.cs`
- `backend/CourseVideo.API/Models/QuizAttempt.cs`
- `backend/CourseVideo.API/Models/QuizAttemptAnswer.cs`
- `backend/CourseVideo.API/Models/OpenRouter/OpenRouterLessonContentResult.cs`

Vai trò:
- định nghĩa entity chính của hệ thống,
- là trung tâm liên kết giữa repository, service và controller.

<!-- Xong -->

### 2.5. Tầng DTO

Đọc sau khi nắm model:
- `backend/CourseVideo.API/DTOs/Auth/`
- `backend/CourseVideo.API/DTOs/Categories/`
- `backend/CourseVideo.API/DTOs/Courses/`
- `backend/CourseVideo.API/DTOs/Modules/`
- `backend/CourseVideo.API/DTOs/Lessons/`
- `backend/CourseVideo.API/DTOs/Comments/`
- `backend/CourseVideo.API/DTOs/Quizzes/`
- `backend/CourseVideo.API/DTOs/Syllabuses/`
- `backend/CourseVideo.API/DTOs/GenerationJobs/`
- `backend/CourseVideo.API/DTOs/LessonVoiceTutor/`
- `backend/CourseVideo.API/DTOs/OpenRouter/`
- `backend/CourseVideo.API/DTOs/AudioWorker/`
- `backend/CourseVideo.API/DTOs/VideoWorker/`
- `backend/CourseVideo.API/DTOs/Users/`

Vai trò:
- request/response model cho API,
- tách model database khỏi dữ liệu trả về frontend.

<!-- Xong -->

### 2.6. Tầng repository

Đọc tiếp:

Interfaces:
- `backend/CourseVideo.API/Repositories/Interfaces/ICategoryRepository.cs`
- `backend/CourseVideo.API/Repositories/Interfaces/ICourseRepository.cs`
- `backend/CourseVideo.API/Repositories/Interfaces/IModuleRepository.cs`
- `backend/CourseVideo.API/Repositories/Interfaces/ILessonRepository.cs`
- `backend/CourseVideo.API/Repositories/Interfaces/ILessonCommentRepository.cs`
- `backend/CourseVideo.API/Repositories/Interfaces/ILessonVoiceSessionRepository.cs`
- `backend/CourseVideo.API/Repositories/Interfaces/IQuizRepository.cs`
- `backend/CourseVideo.API/Repositories/Interfaces/IGenerationJobRepository.cs`
- `backend/CourseVideo.API/Repositories/Interfaces/ISyllabusRepository.cs`
- `backend/CourseVideo.API/Repositories/Interfaces/IUserRepository.cs`
- `backend/CourseVideo.API/Repositories/Interfaces/IRefreshTokenRepository.cs`

<!-- Xong -->

Implementations:
- `backend/CourseVideo.API/Repositories/CategoryRepository.cs`
- `backend/CourseVideo.API/Repositories/CourseRepository.cs`
- `backend/CourseVideo.API/Repositories/ModuleRepository.cs`
- `backend/CourseVideo.API/Repositories/LessonRepository.cs`
- `backend/CourseVideo.API/Repositories/LessonCommentRepository.cs`
- `backend/CourseVideo.API/Repositories/LessonVoiceSessionRepository.cs`
- `backend/CourseVideo.API/Repositories/QuizRepository.cs`
- `backend/CourseVideo.API/Repositories/GenerationJobRepository.cs`
- `backend/CourseVideo.API/Repositories/SyllabusRepository.cs`
- `backend/CourseVideo.API/Repositories/UserRepository.cs`
- `backend/CourseVideo.API/Repositories/RefreshTokenRepository.cs`

Vai trò:
- làm việc trực tiếp với `AppDbContext`,
- lấy/lưu dữ liệu cho service.

<!-- Xong -->

### 2.7. Tầng service

Đọc theo 4 nhóm:

Service nghiệp vụ chính:
- `backend/CourseVideo.API/Services/AuthService.cs`
- `backend/CourseVideo.API/Services/CategoryService.cs`
- `backend/CourseVideo.API/Services/CourseService.cs`
- `backend/CourseVideo.API/Services/ModuleService.cs`
- `backend/CourseVideo.API/Services/LessonService.cs`
- `backend/CourseVideo.API/Services/LessonCommentService.cs`
- `backend/CourseVideo.API/Services/QuizService.cs`
- `backend/CourseVideo.API/Services/SyllabusService.cs`
- `backend/CourseVideo.API/Services/TokenService.cs`
- `backend/CourseVideo.API/Services/SmtpEmailService.cs`

Service generate nội dung:
- `backend/CourseVideo.API/Services/CourseGenerationService.cs`
- `backend/CourseVideo.API/Services/FullCourseGenerationService.cs`
- `backend/CourseVideo.API/Services/LessonContentGenerationService.cs`
- `backend/CourseVideo.API/Services/LessonAudioGenerationService.cs`
- `backend/CourseVideo.API/Services/LessonVideoGenerationService.cs`
- `backend/CourseVideo.API/Services/QuizGenerationService.cs`

Service AI/prompt/parse/validate:
- `backend/CourseVideo.API/Services/OpenRouterCourseStructureService.cs`
- `backend/CourseVideo.API/Services/OpenRouterLessonContentService.cs`
- `backend/CourseVideo.API/Services/OpenRouterQuizGenerationService.cs`
- `backend/CourseVideo.API/Services/OpenRouterPromptFactory.cs`
- `backend/CourseVideo.API/Services/CourseStructureParser.cs`
- `backend/CourseVideo.API/Services/LessonContextBuilder.cs`
- `backend/CourseVideo.API/Services/SlideOutlineValidation.cs`
- `backend/CourseVideo.API/Services/LessonAudioValidation.cs`
- `backend/CourseVideo.API/Services/LessonVideoValidation.cs`
- `backend/CourseVideo.API/Services/VoiceoverPlanParser.cs`
- `backend/CourseVideo.API/Services/VoiceoverPlanValidation.cs`

Service audio/video/tutor:
- `backend/CourseVideo.API/Services/Audio/AudioPipelineService.cs`
- `backend/CourseVideo.API/Services/Audio/EdgeTtsService.cs`
- `backend/CourseVideo.API/Services/Audio/NarrationService.cs`
- `backend/CourseVideo.API/Services/Video/StorageService.cs`
- `backend/CourseVideo.API/Services/Video/TimelineService.cs`
- `backend/CourseVideo.API/Services/Video/ImageProvider.cs`
- `backend/CourseVideo.API/Services/Video/RenderService.cs`
- `backend/CourseVideo.API/Services/Video/FFmpegService.cs`
- `backend/CourseVideo.API/Services/LessonVoiceTutorSessionService.cs`
- `backend/CourseVideo.API/Services/LessonVoiceTutorService.cs`
- `backend/CourseVideo.API/Services/Tutoring/LessonTutorSegmenter.cs`
- `backend/CourseVideo.API/Services/Tutoring/LessonTutorAudioCleanupService.cs`
- `backend/CourseVideo.API/Services/Tutoring/LessonNarrationVoiceResolver.cs`
- `backend/CourseVideo.API/Services/Tutoring/OpenRouterLessonTutorAnswerService.cs`
- `backend/CourseVideo.API/Services/Tutoring/OpenRouterLessonTutorResponseStreamService.cs`
- `backend/CourseVideo.API/Services/Tutoring/SegmentedLessonTutorSpeechService.cs`
- `backend/CourseVideo.API/Services/Transcription/OpenAiTranscriptionService.cs`

Interfaces tương ứng:
- `backend/CourseVideo.API/Services/Interfaces/`

### 2.8. Tầng background job/queue

Đọc sau khi hiểu service generate:
- `backend/CourseVideo.API/Services/GenerationJobQueue.cs`
- `backend/CourseVideo.API/Services/LessonAudioJobQueue.cs`
- `backend/CourseVideo.API/Services/LessonVideoJobQueue.cs`
- `backend/CourseVideo.API/Services/FullCourseJobQueue.cs`
- `backend/CourseVideo.API/Services/JobCancellationTracker.cs`
- `backend/CourseVideo.API/Services/LessonContentGenerationWorker.cs`
- `backend/CourseVideo.API/Services/LessonAudioGenerationWorker.cs`
- `backend/CourseVideo.API/Services/LessonVideoGenerationWorker.cs`
- `backend/CourseVideo.API/Services/FullCourseGenerationWorker.cs`

Vai trò:
- xử lý các tiến trình chạy nền,
- nhận job từ service,
- cập nhật `GenerationJob`.

### 2.9. Tầng controller

Đây là điểm vào HTTP chính, nên đọc sau khi đã nắm service:
- `backend/CourseVideo.API/Controllers/AuthController.cs`
- `backend/CourseVideo.API/Controllers/UsersController.cs`
- `backend/CourseVideo.API/Controllers/DashboardController.cs`
- `backend/CourseVideo.API/Controllers/CategoriesController.cs`
- `backend/CourseVideo.API/Controllers/AdminCategoriesController.cs`
- `backend/CourseVideo.API/Controllers/CoursesController.cs`
- `backend/CourseVideo.API/Controllers/LessonsController.cs`
- `backend/CourseVideo.API/Controllers/LessonCommentsController.cs`
- `backend/CourseVideo.API/Controllers/AdminCommentsController.cs`
- `backend/CourseVideo.API/Controllers/QuizzesController.cs`
- `backend/CourseVideo.API/Controllers/AdminQuizzesController.cs`
- `backend/CourseVideo.API/Controllers/SyllabusesController.cs`
- `backend/CourseVideo.API/Controllers/GenerationJobsController.cs`
- `backend/CourseVideo.API/Controllers/LessonVoiceSessionsController.cs`
- `backend/CourseVideo.API/Controllers/AudioWorkerJobsController.cs`
- `backend/CourseVideo.API/Controllers/VideoWorkerJobsController.cs`
- `backend/CourseVideo.API/Controllers/HealthController.cs`

### 2.10. Tầng realtime

Đọc cuối phần nền tảng:
- `backend/CourseVideo.API/Hubs/LessonVoiceTutorHub.cs`

Vai trò:
- realtime cho voice tutor qua SignalR.

---

## 3. Thứ tự đọc backend từ nền tảng đến nghiệp vụ

Đây là thứ tự đọc khuyến nghị mạnh nhất nếu bạn muốn hiểu hệ thống như một dự án `ASP.NET Core`.

### Bước 1: Khởi động ứng dụng

Đọc:
1. `backend/CourseVideo.API/Program.cs`
2. `backend/CourseVideo.API/appsettings.json`
3. `backend/CourseVideo.API/appsettings.Development.json`
4. toàn bộ `backend/CourseVideo.API/Configuration/`

Khi đọc bước này, bạn cần lần theo:
- config nào được bind vào options nào,
- service nào được đăng ký vào DI,
- repository nào map với interface nào,
- worker nào được `AddHostedService`,
- route nào map controller, route nào map hub.

### Bước 2: Cơ sở dữ liệu và entity

Đọc:
1. `backend/CourseVideo.API/Data/AppDbContext.cs`
2. `backend/CourseVideo.API/Data/DbInitializer.cs`
3. toàn bộ `backend/CourseVideo.API/Models/`

Khi đọc bước này, bạn cần nhìn:
- các bảng chính của hệ thống,
- quan hệ `User - Role`,
- quan hệ `Category - Course - Module - Lesson`,
- quan hệ `Lesson - Comment`,
- quan hệ `Syllabus - GenerationJob - Course`,
- quan hệ `Lesson/Course - Quiz`,
- quan hệ `LessonVoiceSession - Turn - Message`.

### Bước 3: Repository pattern của dự án

Đọc:
1. `backend/CourseVideo.API/Repositories/Interfaces/`
2. `backend/CourseVideo.API/Repositories/`

Cách đọc:
- đọc interface trước,
- sau đó đọc implementation cùng tên,
- nối lại với entity trong `Models/`,
- nối tiếp với `DbContext`.

Ví dụ cặp đọc:
- `ICourseRepository.cs` -> `CourseRepository.cs`
- `ILessonRepository.cs` -> `LessonRepository.cs`
- `IQuizRepository.cs` -> `QuizRepository.cs`

### Bước 4: DTO của API

Đọc:
- `backend/CourseVideo.API/DTOs/`

Cách đọc:
- đọc theo feature tương ứng với controller/service,
- không cần đọc toàn bộ một lần,
- mỗi khi đọc controller nào thì mở DTO của controller đó luôn.

### Bước 5: Service nghiệp vụ cốt lõi

Đọc theo thứ tự:
1. `AuthService.cs`
2. `TokenService.cs`
3. `CategoryService.cs`
4. `CourseService.cs`
5. `ModuleService.cs`
6. `LessonService.cs`
7. `LessonCommentService.cs`
8. `QuizService.cs`
9. `SyllabusService.cs`
10. `CourseGenerationService.cs`

Đây là lớp quan trọng nhất để báo cáo nghiệp vụ backend.

### Bước 6: Controller

Sau khi hiểu service, quay sang controller để hiểu:
- API endpoint nào gọi service nào,
- request/response nào dùng DTO nào,
- chỗ nào cần `[Authorize]`,
- chỗ nào cần role `Admin`.

### Bước 7: Luồng generate chạy nền

Đọc theo thứ tự:
1. `GenerationJob.cs`
2. `GenerationJobRepository.cs`
3. `CourseGenerationService.cs`
4. các queue
5. các worker
6. các service generate content/audio/video/full-course

### Bước 8: Realtime voice tutor

Đọc theo thứ tự:
1. `LessonVoiceSessionsController.cs`
2. `LessonVoiceTutorSessionService.cs`
3. `LessonVoiceTutorHub.cs`
4. `LessonVoiceTutorService.cs`
5. `Services/Tutoring/`
6. `OpenAiTranscriptionService.cs`

---

## 4. Roadmap đọc theo từng nhóm chức năng

Sau khi đọc xong phần nền tảng, bạn chuyển sang đọc theo từng nhóm nghiệp vụ sau.

### 4.1. Xác thực, phân quyền, tài khoản

Đọc theo thứ tự:
1. `backend/CourseVideo.API/Configuration/JwtOptions.cs`
2. `backend/CourseVideo.API/Program.cs`
3. `backend/CourseVideo.API/Models/Role.cs`
4. `backend/CourseVideo.API/Models/User.cs`
5. `backend/CourseVideo.API/Models/RefreshToken.cs`
6. `backend/CourseVideo.API/Repositories/Interfaces/IUserRepository.cs`
7. `backend/CourseVideo.API/Repositories/UserRepository.cs`
8. `backend/CourseVideo.API/Repositories/Interfaces/IRefreshTokenRepository.cs`
9. `backend/CourseVideo.API/Repositories/RefreshTokenRepository.cs`
10. `backend/CourseVideo.API/Services/Interfaces/IAuthService.cs`
11. `backend/CourseVideo.API/Services/AuthService.cs`
12. `backend/CourseVideo.API/Services/Interfaces/ITokenService.cs`
13. `backend/CourseVideo.API/Services/TokenService.cs`
14. `backend/CourseVideo.API/Controllers/AuthController.cs`
15. `backend/CourseVideo.API/Controllers/UsersController.cs`
16. `backend/CourseVideo.API/DTOs/Auth/`
17. `backend/CourseVideo.API/DTOs/Users/`

Liên kết chính:
- `AuthController` -> `AuthService` -> `UserRepository`, `RefreshTokenRepository`, `TokenService` -> `User`, `RefreshToken`, `Role`

### 4.2. Dashboard quản trị

Đọc:
1. `backend/CourseVideo.API/Controllers/DashboardController.cs`
2. `backend/CourseVideo.API/Data/AppDbContext.cs`
3. `backend/CourseVideo.API/Models/User.cs`
4. `backend/CourseVideo.API/Models/Syllabus.cs`
5. `backend/CourseVideo.API/Models/Course.cs`
6. `backend/CourseVideo.API/Models/GenerationJob.cs`

Liên kết chính:
- `DashboardController` -> `AppDbContext` -> các bảng tổng hợp

### 4.3. Danh mục khóa học

Đọc:
1. `backend/CourseVideo.API/Models/Category.cs`
2. `backend/CourseVideo.API/Models/CategoryStatus.cs`
3. `backend/CourseVideo.API/Repositories/Interfaces/ICategoryRepository.cs`
4. `backend/CourseVideo.API/Repositories/CategoryRepository.cs`
5. `backend/CourseVideo.API/Services/Interfaces/ICategoryService.cs`
6. `backend/CourseVideo.API/Services/CategoryService.cs`
7. `backend/CourseVideo.API/Controllers/CategoriesController.cs`
8. `backend/CourseVideo.API/Controllers/AdminCategoriesController.cs`
9. `backend/CourseVideo.API/DTOs/Categories/`

Liên kết chính:
- `AdminCategoriesController` -> `CategoryService` -> `CategoryRepository` -> `Category`
- `CategoriesController` -> `CategoryService` -> DTO option cho phần public

### 4.4. Khóa học, module, lesson

Đọc:
1. `backend/CourseVideo.API/Models/Course.cs`
2. `backend/CourseVideo.API/Models/Module.cs`
3. `backend/CourseVideo.API/Models/Lesson.cs`
4. `backend/CourseVideo.API/Repositories/CourseRepository.cs`
5. `backend/CourseVideo.API/Repositories/ModuleRepository.cs`
6. `backend/CourseVideo.API/Repositories/LessonRepository.cs`
7. `backend/CourseVideo.API/Services/CourseService.cs`
8. `backend/CourseVideo.API/Services/ModuleService.cs`
9. `backend/CourseVideo.API/Services/LessonService.cs`
10. `backend/CourseVideo.API/Controllers/CoursesController.cs`
11. `backend/CourseVideo.API/Controllers/LessonsController.cs`
12. `backend/CourseVideo.API/DTOs/Courses/`
13. `backend/CourseVideo.API/DTOs/Modules/`
14. `backend/CourseVideo.API/DTOs/Lessons/`

Liên kết chính:
- `CoursesController` -> `CourseService`, `ModuleService`, `LessonService`
- `LessonsController` -> `LessonService`, `LessonAudioGenerationService`, `LessonVideoGenerationService`

### 4.5. Bình luận bài học

Đọc:
1. `backend/CourseVideo.API/Models/LessonComment.cs`
2. `backend/CourseVideo.API/Models/LessonCommentReaction.cs`
3. `backend/CourseVideo.API/Repositories/Interfaces/ILessonCommentRepository.cs`
4. `backend/CourseVideo.API/Repositories/LessonCommentRepository.cs`
5. `backend/CourseVideo.API/Services/Interfaces/ILessonCommentService.cs`
6. `backend/CourseVideo.API/Services/LessonCommentService.cs`
7. `backend/CourseVideo.API/Controllers/LessonCommentsController.cs`
8. `backend/CourseVideo.API/Controllers/AdminCommentsController.cs`
9. `backend/CourseVideo.API/DTOs/Comments/`

Liên kết chính:
- `LessonCommentsController` -> `LessonCommentService` -> `LessonCommentRepository` -> `LessonComment`, `LessonCommentReaction`
- `AdminCommentsController` dùng lại cùng service để hide/unhide comment

### 4.6. Quiz

Đọc:
1. `backend/CourseVideo.API/Models/Quiz.cs`
2. `backend/CourseVideo.API/Models/QuizQuestion.cs`
3. `backend/CourseVideo.API/Models/QuizOption.cs`
4. `backend/CourseVideo.API/Models/QuizAttempt.cs`
5. `backend/CourseVideo.API/Models/QuizAttemptAnswer.cs`
6. `backend/CourseVideo.API/Repositories/Interfaces/IQuizRepository.cs`
7. `backend/CourseVideo.API/Repositories/QuizRepository.cs`
8. `backend/CourseVideo.API/Services/Interfaces/IQuizService.cs`
9. `backend/CourseVideo.API/Services/QuizService.cs`
10. `backend/CourseVideo.API/Services/Interfaces/IQuizGenerationService.cs`
11. `backend/CourseVideo.API/Services/QuizGenerationService.cs`
12. `backend/CourseVideo.API/Services/Interfaces/IOpenRouterQuizGenerationService.cs`
13. `backend/CourseVideo.API/Services/OpenRouterQuizGenerationService.cs`
14. `backend/CourseVideo.API/Controllers/QuizzesController.cs`
15. `backend/CourseVideo.API/Controllers/AdminQuizzesController.cs`
16. `backend/CourseVideo.API/DTOs/Quizzes/`
17. `backend/CourseVideo.API/DTOs/OpenRouter/OpenRouterQuizGenerationResult.cs`

Liên kết chính:
- `QuizzesController` -> `QuizService` -> `QuizRepository`
- `AdminQuizzesController` -> `QuizGenerationService` -> `OpenRouterQuizGenerationService`

### 4.7. Đề cương và import syllabus

Đọc:
1. `backend/CourseVideo.API/Models/Syllabus.cs`
2. `backend/CourseVideo.API/Repositories/Interfaces/ISyllabusRepository.cs`
3. `backend/CourseVideo.API/Repositories/SyllabusRepository.cs`
4. `backend/CourseVideo.API/Services/Interfaces/ISyllabusService.cs`
5. `backend/CourseVideo.API/Services/SyllabusService.cs`
6. `backend/CourseVideo.API/Controllers/SyllabusesController.cs`
7. `backend/CourseVideo.API/DTOs/Syllabuses/`

Liên kết chính:
- `SyllabusesController` -> `SyllabusService` -> `SyllabusRepository` -> `Syllabus`

### 4.8. Generation job và pipeline tạo khóa học

Đây là cụm nghiệp vụ phức tạp nhất.

Đọc theo thứ tự:
1. `backend/CourseVideo.API/Models/GenerationJob.cs`
2. `backend/CourseVideo.API/Repositories/Interfaces/IGenerationJobRepository.cs`
3. `backend/CourseVideo.API/Repositories/GenerationJobRepository.cs`
4. `backend/CourseVideo.API/Services/Interfaces/ICourseGenerationService.cs`
5. `backend/CourseVideo.API/Services/CourseGenerationService.cs`
6. `backend/CourseVideo.API/Services/Interfaces/IFullCourseGenerationService.cs`
7. `backend/CourseVideo.API/Services/FullCourseGenerationService.cs`
8. `backend/CourseVideo.API/Services/Interfaces/ILessonContentGenerationService.cs`
9. `backend/CourseVideo.API/Services/LessonContentGenerationService.cs`
10. `backend/CourseVideo.API/Services/Interfaces/ILessonAudioGenerationService.cs`
11. `backend/CourseVideo.API/Services/LessonAudioGenerationService.cs`
12. `backend/CourseVideo.API/Services/Interfaces/ILessonVideoGenerationService.cs`
13. `backend/CourseVideo.API/Services/LessonVideoGenerationService.cs`
14. `backend/CourseVideo.API/Services/GenerationJobQueue.cs`
15. `backend/CourseVideo.API/Services/LessonAudioJobQueue.cs`
16. `backend/CourseVideo.API/Services/LessonVideoJobQueue.cs`
17. `backend/CourseVideo.API/Services/FullCourseJobQueue.cs`
18. `backend/CourseVideo.API/Services/JobCancellationTracker.cs`
19. `backend/CourseVideo.API/Services/LessonContentGenerationWorker.cs`
20. `backend/CourseVideo.API/Services/LessonAudioGenerationWorker.cs`
21. `backend/CourseVideo.API/Services/LessonVideoGenerationWorker.cs`
22. `backend/CourseVideo.API/Services/FullCourseGenerationWorker.cs`
23. `backend/CourseVideo.API/Controllers/GenerationJobsController.cs`
24. `backend/CourseVideo.API/DTOs/GenerationJobs/`

Liên kết chính:
- `SyllabusesController` -> `CourseGenerationService`
- `CourseGenerationService` -> `GenerationJobRepository`, queue
- queue -> worker
- worker -> service generate cụ thể
- service generate -> OpenRouter / audio / video / repository

### 4.9. Sinh nội dung bài học bằng OpenRouter

Đọc:
1. `backend/CourseVideo.API/Services/OpenRouterPromptFactory.cs`
2. `backend/CourseVideo.API/Services/Interfaces/IOpenRouterCourseStructureService.cs`
3. `backend/CourseVideo.API/Services/OpenRouterCourseStructureService.cs`
4. `backend/CourseVideo.API/Services/Interfaces/IOpenRouterLessonContentService.cs`
5. `backend/CourseVideo.API/Services/OpenRouterLessonContentService.cs`
6. `backend/CourseVideo.API/Services/CourseStructureParser.cs`
7. `backend/CourseVideo.API/Services/LessonContextBuilder.cs`
8. `backend/CourseVideo.API/DTOs/OpenRouter/`
9. `backend/CourseVideo.API/Models/OpenRouter/OpenRouterLessonContentResult.cs`

Liên kết chính:
- service generate gọi service OpenRouter
- prompt factory tạo prompt
- parser/validation xử lý kết quả AI

### 4.10. Audio pipeline

Đọc:
1. `backend/CourseVideo.API/Controllers/AudioWorkerJobsController.cs`
2. `backend/CourseVideo.API/DTOs/AudioWorker/AudioWorkerModels.cs`
3. `backend/CourseVideo.API/Services/Audio/NarrationService.cs`
4. `backend/CourseVideo.API/Services/Audio/EdgeTtsService.cs`
5. `backend/CourseVideo.API/Services/Audio/AudioPipelineService.cs`
6. `backend/CourseVideo.API/Services/LessonAudioGenerationService.cs`
7. `backend/CourseVideo.API/Services/LessonAudioValidation.cs`
8. `backend/CourseVideo.API/Services/VoiceoverPlanParser.cs`
9. `backend/CourseVideo.API/Services/VoiceoverPlanValidation.cs`

Liên kết chính:
- `LessonAudioGenerationService` chuẩn bị dữ liệu
- `AudioWorkerJobsController` là điểm vào xử lý audio
- audio service dựng segment, TTS, pipeline audio

### 4.11. Video pipeline

Đọc:
1. `backend/CourseVideo.API/Controllers/VideoWorkerJobsController.cs`
2. `backend/CourseVideo.API/DTOs/VideoWorker/VideoWorkerModels.cs`
3. `backend/CourseVideo.API/Services/Video/StorageService.cs`
4. `backend/CourseVideo.API/Services/Video/TimelineService.cs`
5. `backend/CourseVideo.API/Services/Video/ImageProvider.cs`
6. `backend/CourseVideo.API/Services/Video/RenderService.cs`
7. `backend/CourseVideo.API/Services/Video/FFmpegService.cs`
8. `backend/CourseVideo.API/Services/LessonVideoGenerationService.cs`
9. `backend/CourseVideo.API/Services/LessonVideoValidation.cs`
10. `backend/CourseVideo.API/Services/SlideOutlineValidation.cs`

Liên kết chính:
- `LessonVideoGenerationService` chuẩn bị dữ liệu
- `VideoWorkerJobsController` dựng timeline, render slide, ghép video

### 4.12. Voice tutor realtime

Đọc:
1. `backend/CourseVideo.API/Configuration/LessonVoiceTutorOptions.cs`
2. `backend/CourseVideo.API/Models/LessonVoiceSession.cs`
3. `backend/CourseVideo.API/Models/LessonVoiceTurn.cs`
4. `backend/CourseVideo.API/Models/LessonVoiceMessage.cs`
5. `backend/CourseVideo.API/Repositories/Interfaces/ILessonVoiceSessionRepository.cs`
6. `backend/CourseVideo.API/Repositories/LessonVoiceSessionRepository.cs`
7. `backend/CourseVideo.API/Controllers/LessonVoiceSessionsController.cs`
8. `backend/CourseVideo.API/Services/LessonVoiceTutorSessionService.cs`
9. `backend/CourseVideo.API/Hubs/LessonVoiceTutorHub.cs`
10. `backend/CourseVideo.API/Services/LessonVoiceTutorService.cs`
11. `backend/CourseVideo.API/Services/Tutoring/LessonTutorSegmenter.cs`
12. `backend/CourseVideo.API/Services/Tutoring/OpenRouterLessonTutorAnswerService.cs`
13. `backend/CourseVideo.API/Services/Tutoring/OpenRouterLessonTutorResponseStreamService.cs`
14. `backend/CourseVideo.API/Services/Tutoring/SegmentedLessonTutorSpeechService.cs`
15. `backend/CourseVideo.API/Services/Tutoring/LessonTutorAudioCleanupService.cs`
16. `backend/CourseVideo.API/Services/Tutoring/LessonNarrationVoiceResolver.cs`
17. `backend/CourseVideo.API/Services/Transcription/OpenAiTranscriptionService.cs`
18. `backend/CourseVideo.API/DTOs/LessonVoiceTutor/`

Liên kết chính:
- `LessonVoiceSessionsController` tạo/lấy/đóng session
- `LessonVoiceTutorHub` nhận audio realtime
- `LessonVoiceTutorService` điều phối transcription -> AI answer -> audio segment -> message

---

## 5. Ma trận liên kết nhanh giữa chức năng và file

### Auth
- Controller: `AuthController.cs`
- Service: `AuthService.cs`, `TokenService.cs`
- Repository: `UserRepository.cs`, `RefreshTokenRepository.cs`
- Model: `User.cs`, `Role.cs`, `RefreshToken.cs`
- DTO: `DTOs/Auth/`

### User management
- Controller: `UsersController.cs`
- Repository: `UserRepository.cs`, `RefreshTokenRepository.cs`
- Model: `User.cs`, `RefreshToken.cs`
- DTO: `DTOs/Users/`

### Dashboard
- Controller: `DashboardController.cs`
- Data access: `AppDbContext.cs`
- Model: `User.cs`, `Syllabus.cs`, `Course.cs`, `GenerationJob.cs`

### Category
- Controllers: `CategoriesController.cs`, `AdminCategoriesController.cs`
- Service: `CategoryService.cs`
- Repository: `CategoryRepository.cs`
- Model: `Category.cs`
- DTO: `DTOs/Categories/`

### Course/Module/Lesson
- Controllers: `CoursesController.cs`, `LessonsController.cs`
- Services: `CourseService.cs`, `ModuleService.cs`, `LessonService.cs`
- Repositories: `CourseRepository.cs`, `ModuleRepository.cs`, `LessonRepository.cs`
- Models: `Course.cs`, `Module.cs`, `Lesson.cs`
- DTO: `DTOs/Courses/`, `DTOs/Modules/`, `DTOs/Lessons/`

### Comment
- Controllers: `LessonCommentsController.cs`, `AdminCommentsController.cs`
- Service: `LessonCommentService.cs`
- Repository: `LessonCommentRepository.cs`
- Models: `LessonComment.cs`, `LessonCommentReaction.cs`
- DTO: `DTOs/Comments/`

### Quiz
- Controllers: `QuizzesController.cs`, `AdminQuizzesController.cs`
- Services: `QuizService.cs`, `QuizGenerationService.cs`, `OpenRouterQuizGenerationService.cs`
- Repository: `QuizRepository.cs`
- Models: `Quiz.cs`, `QuizQuestion.cs`, `QuizOption.cs`, `QuizAttempt.cs`, `QuizAttemptAnswer.cs`
- DTO: `DTOs/Quizzes/`

### Syllabus
- Controller: `SyllabusesController.cs`
- Service: `SyllabusService.cs`
- Repository: `SyllabusRepository.cs`
- Model: `Syllabus.cs`
- DTO: `DTOs/Syllabuses/`

### Generation job
- Controller: `GenerationJobsController.cs`
- Services: `CourseGenerationService.cs`, `FullCourseGenerationService.cs`
- Repository: `GenerationJobRepository.cs`
- Model: `GenerationJob.cs`
- DTO: `DTOs/GenerationJobs/`

### Audio
- Controller: `AudioWorkerJobsController.cs`
- Services: `NarrationService.cs`, `EdgeTtsService.cs`, `AudioPipelineService.cs`, `LessonAudioGenerationService.cs`
- DTO: `DTOs/AudioWorker/`

### Video
- Controller: `VideoWorkerJobsController.cs`
- Services: `StorageService.cs`, `TimelineService.cs`, `ImageProvider.cs`, `RenderService.cs`, `FFmpegService.cs`, `LessonVideoGenerationService.cs`
- DTO: `DTOs/VideoWorker/`

### Voice tutor
- Controller: `LessonVoiceSessionsController.cs`
- Hub: `LessonVoiceTutorHub.cs`
- Services: `LessonVoiceTutorSessionService.cs`, `LessonVoiceTutorService.cs`, `Services/Tutoring/*`, `OpenAiTranscriptionService.cs`
- Repository: `LessonVoiceSessionRepository.cs`
- Models: `LessonVoiceSession.cs`, `LessonVoiceTurn.cs`, `LessonVoiceMessage.cs`
- DTO: `DTOs/LessonVoiceTutor/`

---

## 6. Thứ tự đọc đề xuất theo 3 mức

### 6.1. Mức 1: Đọc nhanh để nắm khung hệ thống

Đọc theo thứ tự:
1. `Program.cs`
2. `appsettings.json`
3. `Configuration/`
4. `AppDbContext.cs`
5. `Models/`
6. danh sách controller trong `Controllers/`
7. các service chính:
   - `AuthService.cs`
   - `CourseService.cs`
   - `LessonService.cs`
   - `CategoryService.cs`
   - `QuizService.cs`
   - `SyllabusService.cs`
   - `CourseGenerationService.cs`

### 6.2. Mức 2: Đọc đủ để báo cáo với giảng viên

Đọc theo thứ tự:
1. toàn bộ mục `2` và `3` trong roadmap này
2. sau đó đọc kỹ từng nhóm:
   - Auth
   - Category
   - Course/Module/Lesson
   - Comment
   - Quiz
   - Syllabus
   - Generation job
3. cuối cùng mới đọc:
   - Audio
   - Video
   - Voice tutor

### 6.3. Mức 3: Đọc sâu từng dòng code

Đọc theo chiến lược:
1. mở `Program.cs`
2. với mỗi service được đăng ký trong `Program.cs`, mở:
   - interface,
   - implementation,
   - repository liên quan,
   - model liên quan,
   - controller gọi nó
3. với mỗi controller, lần theo:
   - route,
   - DTO request,
   - service,
   - repository,
   - entity,
   - DTO response
4. với mỗi job nền, lần theo:
   - controller hoặc service tạo job,
   - queue,
   - worker,
   - service thực thi,
   - repository cập nhật `GenerationJob`

---

## 7. Lộ trình đọc thực tế trong 1 buổi hoặc nhiều buổi

### Buổi 1: Khung ASP.NET Core
- `Program.cs`
- `appsettings*.json`
- `Configuration/`
- `AppDbContext.cs`
- `DbInitializer.cs`

### Buổi 2: Model và repository
- toàn bộ `Models/`
- toàn bộ `Repositories/Interfaces/`
- toàn bộ `Repositories/`

### Buổi 3: Auth và user
- `AuthController.cs`
- `UsersController.cs`
- `AuthService.cs`
- `TokenService.cs`
- `UserRepository.cs`
- `RefreshTokenRepository.cs`
- `DTOs/Auth/`, `DTOs/Users/`

### Buổi 4: Category, course, module, lesson
- `AdminCategoriesController.cs`
- `CategoriesController.cs`
- `CoursesController.cs`
- `LessonsController.cs`
- `CategoryService.cs`
- `CourseService.cs`
- `ModuleService.cs`
- `LessonService.cs`

### Buổi 5: Comment, quiz, dashboard
- `DashboardController.cs`
- `LessonCommentsController.cs`
- `AdminCommentsController.cs`
- `QuizzesController.cs`
- `AdminQuizzesController.cs`
- `LessonCommentService.cs`
- `QuizService.cs`
- `QuizGenerationService.cs`

### Buổi 6: Syllabus và generation pipeline
- `SyllabusesController.cs`
- `GenerationJobsController.cs`
- `SyllabusService.cs`
- `CourseGenerationService.cs`
- `FullCourseGenerationService.cs`
- các queue
- các worker

### Buổi 7: Audio, video, voice tutor
- `AudioWorkerJobsController.cs`
- `VideoWorkerJobsController.cs`
- `LessonVoiceSessionsController.cs`
- `LessonVoiceTutorHub.cs`
- `Services/Audio/`
- `Services/Video/`
- `Services/Tutoring/`
- `OpenAiTranscriptionService.cs`

---

## 8. Nếu muốn tự lần flow một tính năng từ đầu đến cuối

Mẫu chung để lần:
1. Controller
2. DTO request
3. Service interface
4. Service implementation
5. Repository interface
6. Repository implementation
7. Model/Entity
8. DTO response

Ví dụ:
- đăng nhập: `AuthController` -> `LoginRequest` -> `IAuthService` -> `AuthService` -> `IUserRepository`/`IRefreshTokenRepository` -> `User`/`RefreshToken` -> `LoginResponse`
- import đề cương: `SyllabusesController` -> `ImportSyllabusRequest` -> `ISyllabusService` -> `SyllabusService` -> `ISyllabusRepository` -> `Syllabus`
- generate khóa học: `SyllabusesController` -> `ICourseGenerationService` -> `CourseGenerationService` -> `GenerationJobQueue` -> worker -> service generate

---

## 9. File quan trọng nhất nếu chỉ có ít thời gian

Nếu chỉ được chọn một nhóm file để đọc trước, hãy ưu tiên:
- `backend/CourseVideo.API/Program.cs`
- `backend/CourseVideo.API/Data/AppDbContext.cs`
- `backend/CourseVideo.API/Services/AuthService.cs`
- `backend/CourseVideo.API/Services/CourseService.cs`
- `backend/CourseVideo.API/Services/LessonService.cs`
- `backend/CourseVideo.API/Services/CourseGenerationService.cs`
- `backend/CourseVideo.API/Controllers/AuthController.cs`
- `backend/CourseVideo.API/Controllers/CoursesController.cs`
- `backend/CourseVideo.API/Controllers/LessonsController.cs`
- `backend/CourseVideo.API/Controllers/SyllabusesController.cs`
- `backend/CourseVideo.API/Controllers/GenerationJobsController.cs`

Nếu đọc xong nhóm này, bạn đã nắm được:
- app khởi động thế nào,
- database có gì,
- auth hoạt động ra sao,
- course/lesson chạy thế nào,
- pipeline generate vận hành ra sao.
