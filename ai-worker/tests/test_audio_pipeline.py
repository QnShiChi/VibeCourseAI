import struct
import tempfile
import unittest
import wave
from pathlib import Path

from app.audio_pipeline import concatenate_wav_files, get_wav_duration_seconds


class AudioPipelineTests(unittest.TestCase):
    def test_get_wav_duration_uses_actual_frame_bytes_when_header_uses_sentinel_sizes(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            path = Path(temp_dir) / "segment.wav"
            self._write_pcm_wav(path, frame_count=24_000)
            self._patch_wav_header_with_sentinel_sizes(path)

            duration = get_wav_duration_seconds(path)

            self.assertAlmostEqual(duration, 1.0, places=2)

    def test_concatenate_wav_files_handles_sentinel_header_sizes(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            segment_a = Path(temp_dir) / "a.wav"
            segment_b = Path(temp_dir) / "b.wav"
            final_path = Path(temp_dir) / "final.wav"

            self._write_pcm_wav(segment_a, frame_count=24_000)
            self._write_pcm_wav(segment_b, frame_count=12_000)
            self._patch_wav_header_with_sentinel_sizes(segment_a)
            self._patch_wav_header_with_sentinel_sizes(segment_b)

            duration = concatenate_wav_files([segment_a, segment_b], final_path)

            self.assertAlmostEqual(duration, 1.5, places=2)
            self.assertTrue(final_path.exists())
            self.assertAlmostEqual(get_wav_duration_seconds(final_path), 1.5, places=2)

    def _write_pcm_wav(self, path: Path, frame_count: int, frame_rate: int = 24_000) -> None:
        silence = b"\x00\x00" * frame_count
        with wave.open(str(path), "wb") as wav_file:
            wav_file.setnchannels(1)
            wav_file.setsampwidth(2)
            wav_file.setframerate(frame_rate)
            wav_file.writeframes(silence)

    def _patch_wav_header_with_sentinel_sizes(self, path: Path) -> None:
        data = bytearray(path.read_bytes())
        data[4:8] = struct.pack("<L", 0xFFFFFFFF)
        data[40:44] = struct.pack("<L", 0xFFFFFFFF)
        path.write_bytes(data)


if __name__ == "__main__":
    unittest.main()
