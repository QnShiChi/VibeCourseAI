namespace CourseVideo.API.DTOs.Dashboard;

public class DashboardStatsResponse
{
    public int UsersCount { get; set; }
    public int SyllabusesCount { get; set; }
    public int CoursesCount { get; set; }
    public int GenerationJobsCount { get; set; }
    public int NegativeCommentsCount { get; set; }
}
