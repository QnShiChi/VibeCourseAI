from pathlib import Path


STORAGE_ROOT = Path("/app/storage")
STORAGE_VIDEO_DIR = STORAGE_ROOT / "video"
STORAGE_VIDEO_FRAMES_DIR = STORAGE_VIDEO_DIR / "frames"


def ensure_video_dirs() -> None:
    STORAGE_VIDEO_DIR.mkdir(parents=True, exist_ok=True)
    STORAGE_VIDEO_FRAMES_DIR.mkdir(parents=True, exist_ok=True)


def build_video_output_path(lesson_id: str) -> Path:
    ensure_video_dirs()
    return STORAGE_VIDEO_DIR / f"{lesson_id}.mp4"


def build_video_frames_dir(lesson_id: str) -> Path:
    ensure_video_dirs()
    frames_dir = STORAGE_VIDEO_FRAMES_DIR / lesson_id
    frames_dir.mkdir(parents=True, exist_ok=True)
    return frames_dir


def resolve_storage_path_from_url(storage_url: str) -> Path:
    if not storage_url.startswith("/storage/"):
        raise ValueError("Asset URL phải bắt đầu bằng /storage/.")
    relative = storage_url.removeprefix("/storage/")
    return STORAGE_ROOT / relative
