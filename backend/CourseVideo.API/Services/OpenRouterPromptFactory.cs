using CourseVideo.API.DTOs.OpenRouter;

namespace CourseVideo.API.Services;

public class OpenRouterPromptFactory
{
    public OpenRouterChatCompletionRequest Create(string model, string extractedText)
    {
        return new OpenRouterChatCompletionRequest
        {
            Model = model,
            Temperature = 0.1,
            Messages =
            [
                new OpenRouterMessage
                {
                    Role = "system",
                    Content = """
                    Ban la chuyen gia phan tich de cuong hoc phan dai hoc bang tieng Viet.
                    Hay sinh cau truc khoa hoc phuc vu giang day gom course, modules, lessons.
                    Bo qua thong tin hanh chinh khong lien quan den noi dung bai giang.
                    Uu tien tach noi dung thanh cac don vi day hoc nho, tranh gop qua nhieu noi dung vao mot lesson.
                    Chi tra ve JSON dung schema. Khong them giai thich nao ben ngoai JSON.
                    """
                },
                new OpenRouterMessage
                {
                    Role = "user",
                    Content = $"""
                    Hay doc de cuong sau va sinh ra JSON voi cac truong:
                    - courseTitle
                    - courseDescription
                    - modules[]
                      - title
                      - description
                      - lessons[]
                        - title
                        - description
                        - contentSeed

                    Quy tac:
                    - title phai sach, ngan gon, dung ngu canh hoc phan
                    - bo thong tin giang vien, email, so dien thoai, dia chi neu khong can cho noi dung giang day
                    - uu tien noi dung hoc thuat, muc tieu, noi dung hoc phan, ke hoach giang day, chuong, bai
                    - so lesson trong moi module phai linh hoat theo muc do chi tiet cua noi dung; module co nhieu muc hoc, vi du, bai tap, hoac buoc thuc hanh thi nen co nhieu lesson hon
                    - neu de cuong co cac moc Tuan, Buoi, Chu de, Topic, Session thi uu tien dung cac moc do de tach lesson
                    - khong gop nhieu chu de lon vao cung 1 lesson neu co the tach thanh cac lesson rieng
                    - moi module phai co it nhat 1 lesson
                    - moi lesson phai co contentSeed du de sinh noi dung bai hoc ve sau

                    De cuong:
                    {extractedText}
                    """
                }
            ],
            ResponseFormat = new OpenRouterResponseFormat
            {
                Type = "json_schema",
                JsonSchema = new OpenRouterJsonSchema
                {
                    Name = "course_structure",
                    Strict = true,
                    Schema = BuildSchema()
                }
            }
        };
    }

    private static object BuildSchema()
    {
        return new
        {
            type = "object",
            additionalProperties = false,
            required = new[] { "courseTitle", "courseDescription", "modules" },
            properties = new
            {
                courseTitle = new { type = "string" },
                courseDescription = new { type = "string" },
                modules = new
                {
                    type = "array",
                    items = new
                    {
                        type = "object",
                        additionalProperties = false,
                        required = new[] { "title", "description", "lessons" },
                        properties = new
                        {
                            title = new { type = "string" },
                            description = new { type = "string" },
                            lessons = new
                            {
                                type = "array",
                                items = new
                                {
                                    type = "object",
                                    additionalProperties = false,
                                    required = new[] { "title", "description", "contentSeed" },
                                    properties = new
                                    {
                                        title = new { type = "string" },
                                        description = new { type = "string" },
                                        contentSeed = new { type = "string" }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        };
    }
}
