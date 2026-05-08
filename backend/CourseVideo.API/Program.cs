using CourseVideo.API.Configuration;
using CourseVideo.API.Data;
using CourseVideo.API.Repositories;
using CourseVideo.API.Repositories.Interfaces;
using CourseVideo.API.Services;
using CourseVideo.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration["CONNECTION_STRING"]
    ?? builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Missing database connection string.");

builder.Services.Configure<AdminSeedOptions>(builder.Configuration.GetSection("AdminSeed"));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddScoped<ICourseRepository, CourseRepository>();
builder.Services.AddScoped<ICourseService, CourseService>();
builder.Services.AddScoped<IAuthService, AuthService>();

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

app.UseCors("Frontend");
app.UseAuthorization();
app.MapControllers();
app.Run();
