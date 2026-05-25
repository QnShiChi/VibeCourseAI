from pathlib import Path
from mutagen.mp3 import MP3

STORAGE_AUDIO_DIR = Path("/app/storage/audio")


def ensure_audio_dir() -> None:
    STORAGE_AUDIO_DIR.mkdir(parents=True, exist_ok=True)


def build_segment_path(lesson_id: str, slide_number: int) -> Path:
    ensure_audio_dir()
    return STORAGE_AUDIO_DIR / f"{lesson_id}-slide-{slide_number}.mp3"


def build_final_path(lesson_id: str) -> Path:
    ensure_audio_dir()
    return STORAGE_AUDIO_DIR / f"{lesson_id}.mp3"


def concatenate_audio_files(segment_paths: list[Path], final_path: Path) -> float:
    if not segment_paths:
        raise ValueError("Không có segment audio để ghép.")

    with open(final_path, "wb") as destination:
        for path in segment_paths:
            with open(path, "rb") as source:
                destination.write(source.read())

    return get_audio_duration_seconds(final_path)


def get_audio_duration_seconds(path: Path) -> float:
    try:
        audio = MP3(path)
        return float(audio.info.length)
    except Exception:
        return 0.0
