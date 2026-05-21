from pydantic import BaseModel, Field


class VideoWorkerLessonRequest(BaseModel):
    lesson_id: str
    lesson_title: str
    slide_outline_json: str
    audio_url: str
    audio_segments_json: str


class SlideTimingResponse(BaseModel):
    slideNumber: int = Field(alias="slide_number")
    startSeconds: float = Field(alias="start_seconds")
    durationSeconds: float = Field(alias="duration_seconds")
    endSeconds: float = Field(alias="end_seconds")

    model_config = {"populate_by_name": True}


class VideoWorkerLessonResponse(BaseModel):
    videoUrl: str = Field(alias="video_url")
    durationSeconds: float = Field(alias="duration_seconds")
    errorMessage: str | None = Field(default=None, alias="error_message")
    slideTimings: list[SlideTimingResponse] = Field(default_factory=list, alias="slide_timings")

    model_config = {"populate_by_name": True}
