import subprocess
from pathlib import Path


def assemble_video(slide_paths: list[Path], durations: list[float], audio_path: Path, output_path: Path) -> float:
    if not slide_paths:
        raise ValueError("Không có slide để render video.")

    if len(slide_paths) != len(durations):
        raise ValueError("Số lượng slide và durations không khớp.")

    if not audio_path.exists():
        raise FileNotFoundError(f"Không tìm thấy audio source: {audio_path}")

    manifest_path = output_path.with_suffix(".txt")
    manifest_lines: list[str] = []

    for slide_path, duration in zip(slide_paths, durations):
        manifest_lines.append(f"file '{slide_path.as_posix()}'")
        manifest_lines.append(f"duration {max(duration, 0.1):.3f}")

    manifest_lines.append(f"file '{slide_paths[-1].as_posix()}'")
    manifest_path.write_text("\n".join(manifest_lines), encoding="utf-8")

    command = [
        "ffmpeg",
        "-y",
        "-f",
        "concat",
        "-safe",
        "0",
        "-i",
        str(manifest_path),
        "-i",
        str(audio_path),
        "-vsync",
        "vfr",
        "-pix_fmt",
        "yuv420p",
        "-c:v",
        "libx264",
        "-c:a",
        "aac",
        "-shortest",
        str(output_path),
    ]

    subprocess.run(command, check=True, capture_output=True, text=True)
    return sum(durations)
