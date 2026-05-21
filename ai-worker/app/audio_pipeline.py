from pathlib import Path
import wave


STORAGE_AUDIO_DIR = Path("/app/storage/audio")


def ensure_audio_dir() -> None:
    STORAGE_AUDIO_DIR.mkdir(parents=True, exist_ok=True)


def build_segment_path(lesson_id: str, slide_number: int) -> Path:
    ensure_audio_dir()
    return STORAGE_AUDIO_DIR / f"{lesson_id}-slide-{slide_number}.wav"


def build_final_path(lesson_id: str) -> Path:
    ensure_audio_dir()
    return STORAGE_AUDIO_DIR / f"{lesson_id}.wav"


def _read_wav_frames(path: Path) -> tuple[wave._wave_params, bytes, int]:
    with wave.open(str(path), "rb") as source:
        params = source.getparams()
        frames = source.readframes(source.getnframes())
        frame_width = params.nchannels * params.sampwidth
        actual_frame_count = len(frames) // frame_width if frame_width > 0 else 0
        return params, frames, actual_frame_count


def concatenate_wav_files(segment_paths: list[Path], final_path: Path) -> float:
    if not segment_paths:
        raise ValueError("Không có segment audio để ghép.")

    total_frames = 0
    params = None

    with wave.open(str(final_path), "wb") as destination:
        for path in segment_paths:
            source_params, frames, actual_frame_count = _read_wav_frames(path)
            if params is None:
                params = source_params
                destination.setnchannels(params.nchannels)
                destination.setsampwidth(params.sampwidth)
                destination.setframerate(params.framerate)
                destination.setcomptype(params.comptype, params.compname)
            total_frames += actual_frame_count
            destination.writeframes(frames)

    if params is None or params.framerate == 0:
        return 0.0

    return total_frames / float(params.framerate)


def get_wav_duration_seconds(path: Path) -> float:
    params, _, actual_frame_count = _read_wav_frames(path)
    if params.framerate == 0:
        return 0.0
    return actual_frame_count / float(params.framerate)
