import asyncio
from fastapi import FastAPI, HTTPException

from app.audio_pipeline import (
    build_final_path,
    build_segment_path,
    concatenate_audio_files,
    get_audio_duration_seconds,
)
from app.models import LessonAudioJobRequest, LessonAudioJobResponse, LessonAudioSegmentResponse
from app.narration import build_narration_segments
from app.edge_tts_client import EdgeTtsClient

app = FastAPI(title="Course Video AI Worker")


@app.get("/health")
def health():
    return {"status": "ok"}


@app.get("/jobs/ping")
def ping():
    return {"message": "worker ready"}


@app.post("/jobs/generate-lesson-audio", response_model=LessonAudioJobResponse)
async def generate_lesson_audio(request: LessonAudioJobRequest):
    try:
        tts = EdgeTtsClient()
        narration_segments = build_narration_segments(
            teaching_script=request.teaching_script,
            slide_outline_json=request.slide_outline_json,
            voiceover_plan_json=request.voiceover_plan_json,
        )

        async def process_segment(segment):
            path = build_segment_path(request.lesson_id, segment.slide_number)
            audio_bytes = await tts.synthesize_to_bytes(segment.narration_text)
            path.write_bytes(audio_bytes)
            return path, LessonAudioSegmentResponse(
                slide_number=segment.slide_number,
                title=segment.title,
                narration_text=segment.narration_text,
                audio_url=f"/storage/audio/{path.name}",
                duration_seconds=get_audio_duration_seconds(path),
            )

        tasks = [process_segment(segment) for segment in narration_segments]
        results = await asyncio.gather(*tasks)

        results.sort(key=lambda r: r[1].slide_number)
        
        segment_paths = [r[0] for r in results]
        segment_results = [r[1] for r in results]

        final_path = build_final_path(request.lesson_id)
        duration_seconds = concatenate_audio_files(segment_paths, final_path)

        return LessonAudioJobResponse(
            audio_url=f"/storage/audio/{final_path.name}",
            duration_seconds=duration_seconds,
            segments=segment_results,
            error_message=None,
        )
    except Exception as exc:
        raise HTTPException(status_code=400, detail=str(exc)) from exc
