using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CourseVideo.API.Configuration;
using CourseVideo.API.Data;
using CourseVideo.API.Services.Video;
using CourseVideo.API.Models;
using CourseVideo.API.Repositories;
using CourseVideo.API.Repositories.Interfaces;
using CourseVideo.API.Services;
using CourseVideo.API.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration["CONNECTION_STRING"]
    ?? builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Missing database connection string.");

builder.Services.Configure<AdminSeedOptions>(builder.Configuration.GetSection("AdminSeed"));
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<OpenRouterOptions>(builder.Configuration.GetSection("OpenRouter"));
builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection("Smtp"));

var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()
    ?? throw new InvalidOperationException("Missing JWT configuration.");

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Video Worker Services
builder.Services.AddHttpClient();
builder.Services.AddSingleton<IStorageService, StorageService>();
builder.Services.AddSingleton<ITimelineService, TimelineService>();
builder.Services.AddTransient<IImageProvider, ImageProvider>();
builder.Services.AddTransient<IRenderService, RenderService>();
builder.Services.AddTransient<IFFmpegService, FFmpegService>();

// Audio Worker Services
builder.Services.AddSingleton<CourseVideo.API.Services.Audio.INarrationService, CourseVideo.API.Services.Audio.NarrationService>();
builder.Services.AddSingleton<CourseVideo.API.Services.Audio.IEdgeTtsService, CourseVideo.API.Services.Audio.EdgeTtsService>();
builder.Services.AddSingleton<CourseVideo.API.Services.Audio.IAudioPipelineService, CourseVideo.API.Services.Audio.AudioPipelineService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
            NameClaimType = JwtRegisteredClaimNames.Sub,
            RoleClaimType = ClaimTypes.Role
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddScoped<ICourseRepository, CourseRepository>();
builder.Services.AddScoped<IModuleRepository, ModuleRepository>();
builder.Services.AddScoped<ILessonRepository, LessonRepository>();
builder.Services.AddScoped<ILessonCommentRepository, LessonCommentRepository>();
builder.Services.AddScoped<IGenerationJobRepository, GenerationJobRepository>();
builder.Services.AddScoped<ISyllabusRepository, SyllabusRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddSingleton<IGenerationJobQueue, GenerationJobQueue>();
builder.Services.AddSingleton<ILessonAudioJobQueue, LessonAudioJobQueue>();
builder.Services.AddSingleton<ILessonVideoJobQueue, LessonVideoJobQueue>();
builder.Services.AddSingleton<IFullCourseJobQueue, FullCourseJobQueue>();
builder.Services.AddSingleton<IJobCancellationTracker, JobCancellationTracker>();
builder.Services.AddHostedService<LessonContentGenerationWorker>();
builder.Services.AddHostedService<LessonAudioGenerationWorker>();
builder.Services.AddHostedService<LessonVideoGenerationWorker>();
builder.Services.AddHostedService<FullCourseGenerationWorker>();
builder.Services.AddScoped<ICourseService, CourseService>();
builder.Services.AddScoped<ILessonContentGenerationService, LessonContentGenerationService>();
builder.Services.AddScoped<ILessonAudioGenerationService, LessonAudioGenerationService>();
builder.Services.AddScoped<ILessonVideoGenerationService, LessonVideoGenerationService>();
builder.Services.AddScoped<IFullCourseGenerationService, FullCourseGenerationService>();
builder.Services.AddScoped<ICourseGenerationService, CourseGenerationService>();
builder.Services.AddScoped<ICourseStructureParser, CourseStructureParser>();
builder.Services.AddScoped<OpenRouterPromptFactory>();
builder.Services.AddHttpClient<IOpenRouterCourseStructureService, OpenRouterCourseStructureService>((serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<IOptions<OpenRouterOptions>>().Value;
    client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds > 0 ? options.TimeoutSeconds : 30);
});
builder.Services.AddHttpClient<IOpenRouterLessonContentService, OpenRouterLessonContentService>((serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<IOptions<OpenRouterOptions>>().Value;
    client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds > 0 ? options.TimeoutSeconds : 30);
});
builder.Services.AddHttpClient("AiWorker", client =>
{
    // The lesson audio generation endpoint is now merged into the backend at localhost:8080
    client.BaseAddress = new Uri(builder.Configuration["AI_WORKER_BASE_URL"] ?? "http://localhost:8080");
    client.Timeout = TimeSpan.FromMinutes(10);
});
builder.Services.AddHttpClient("VideoWorker", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["VIDEO_WORKER_BASE_URL"] ?? "http://video-worker:8001");
    client.Timeout = TimeSpan.FromMinutes(15);
});
builder.Services.AddScoped<IModuleService, ModuleService>();
builder.Services.AddScoped<ILessonService, LessonService>();
builder.Services.AddScoped<ILessonCommentService, LessonCommentService>();
builder.Services.AddScoped<ISyllabusService, SyllabusService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddScoped<PasswordHasher<User>>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var adminSeedOptions = scope.ServiceProvider.GetRequiredService<IOptions<AdminSeedOptions>>();
    DbInitializer.Initialize(db, adminSeedOptions);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var storageDirectory = Path.Combine(app.Environment.ContentRootPath, "storage");
Directory.CreateDirectory(storageDirectory);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(storageDirectory),
    RequestPath = "/storage"
});

app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
