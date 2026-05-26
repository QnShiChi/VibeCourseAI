from fastapi import FastAPI, HTTPException

from app.ffmpeg_pipeline import assemble_video
from app.models import SlideTimingResponse, VideoWorkerLessonRequest, VideoWorkerLessonResponse
from app.render import parse_slide_outline_json, render_slide_png
from app.storage import build_video_frames_dir, build_video_output_path, resolve_storage_path_from_url
from app.timeline import build_slide_timeline, parse_audio_segments_json

app = FastAPI(title="Course Video Worker")


@app.get("/health")
def health():
    return {"status": "ok"}


@app.post("/jobs/generate-lesson-video", response_model=VideoWorkerLessonResponse)
def generate_lesson_video(request: VideoWorkerLessonRequest):
    try:
        slides = parse_slide_outline_json(request.slide_outline_json)
        if not slides:
            raise ValueError("Lesson phải có ít nhất một slide để render video.")

        audio_segments = parse_audio_segments_json(request.audio_segments_json)
        timeline = build_slide_timeline(audio_segments)

        slide_lookup = {int(slide["slide_number"]): slide for slide in slides}
        slide_paths = []
        durations = []
        frames_dir = build_video_frames_dir(request.lesson_id)

        for item in timeline:
            slide = slide_lookup.get(int(item["slide_number"]))
            if slide is None:
                raise ValueError(f"Không tìm thấy slide {item['slide_number']} để khớp với audio segment.")

            slide_path = frames_dir / f"slide-{item['slide_number']:03d}.png"
            render_slide_png(
                slide_path,
                slide_number=int(slide["slide_number"]),
                title=str(slide["title"]),
                bullet_points=list(slide["bullet_points"]),
                image_keyword=str(slide.get("image_keyword") or ""),
            )
            slide_paths.append(slide_path)
            durations.append(float(item["duration_seconds"]))

        audio_path = resolve_storage_path_from_url(request.audio_url)
        final_path = build_video_output_path(request.lesson_id)
        duration_seconds = assemble_video(slide_paths, durations, audio_path, final_path)

        return VideoWorkerLessonResponse(
            video_url=f"/storage/video/{final_path.name}",
            duration_seconds=duration_seconds,
            error_message=None,
            slide_timings=[SlideTimingResponse(**item) for item in timeline],
        )
    except Exception as exc:
        raise HTTPException(status_code=400, detail=str(exc)) from exc
