from fastapi import FastAPI, HTTPException

from app.audio_pipeline import (
    build_final_path,
    build_segment_path,
    concatenate_wav_files,
    get_wav_duration_seconds,
)
from app.models import LessonAudioJobRequest, LessonAudioJobResponse, LessonAudioSegmentResponse
from app.narration import build_narration_segments
from app.openai_tts import OpenAITtsClient

app = FastAPI(title="Course Video AI Worker")


@app.get("/health")
def health():
    return {"status": "ok"}


@app.get("/jobs/ping")
def ping():
    return {"message": "worker ready"}


@app.post("/jobs/generate-lesson-audio", response_model=LessonAudioJobResponse)
def generate_lesson_audio(request: LessonAudioJobRequest):
    try:
        tts = OpenAITtsClient()
        narration_segments = build_narration_segments(
            teaching_script=request.teaching_script,
            slide_outline_json=request.slide_outline_json,
            voiceover_plan_json=request.voiceover_plan_json,
        )

        segment_results: list[LessonAudioSegmentResponse] = []
        segment_paths = []

        for segment in narration_segments:
            path = build_segment_path(request.lesson_id, segment.slide_number)
            path.write_bytes(tts.synthesize_to_bytes(segment.narration_text))
            segment_paths.append(path)
            segment_results.append(
                LessonAudioSegmentResponse(
                    slide_number=segment.slide_number,
                    title=segment.title,
                    narration_text=segment.narration_text,
                    audio_url=f"/storage/audio/{path.name}",
                    duration_seconds=get_wav_duration_seconds(path),
                )
            )

        final_path = build_final_path(request.lesson_id)
        duration_seconds = concatenate_wav_files(segment_paths, final_path)

        return LessonAudioJobResponse(
            audio_url=f"/storage/audio/{final_path.name}",
            duration_seconds=duration_seconds,
            segments=segment_results,
            error_message=None,
        )
    except Exception as exc:
        raise HTTPException(status_code=400, detail=str(exc)) from exc
