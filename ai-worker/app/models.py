from pydantic import BaseModel, Field


class NarrationSegment(BaseModel):
    slide_number: int
    title: str
    narration_text: str


class LessonAudioJobRequest(BaseModel):
    lesson_id: str
    lesson_title: str
    teaching_script: str
    slide_outline_json: str
    voiceover_plan_json: str


class LessonAudioSegmentResponse(BaseModel):
    slideNumber: int = Field(alias="slide_number")
    title: str
    narrationText: str = Field(alias="narration_text")
    audioUrl: str = Field(alias="audio_url")
    durationSeconds: float = Field(alias="duration_seconds")

    model_config = {"populate_by_name": True}


class LessonAudioJobResponse(BaseModel):
    audioUrl: str = Field(alias="audio_url")
    durationSeconds: float = Field(alias="duration_seconds")
    segments: list[LessonAudioSegmentResponse]
    errorMessage: str | None = Field(default=None, alias="error_message")

    model_config = {"populate_by_name": True}
