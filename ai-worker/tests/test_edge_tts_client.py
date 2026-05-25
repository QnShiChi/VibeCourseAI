import asyncio
import unittest
from unittest.mock import patch

from edge_tts.exceptions import NoAudioReceived

from app.edge_tts_client import EdgeTtsClient


class FakeCommunicateChunkFallback:
    def __init__(self, text: str, voice: str):
        self.text = text
        self.voice = voice

    async def stream(self):
        if self.text == "Xin chao. Toi la AI.":
            raise NoAudioReceived()

        payload = self.text.encode("utf-8")
        yield {"type": "audio", "data": payload}


class FakeCommunicateVoiceFallback:
    def __init__(self, text: str, voice: str):
        self.text = text
        self.voice = voice

    async def stream(self):
        if self.voice == "vi-VN-HoaiMyNeural":
            raise NoAudioReceived()

        yield {"type": "audio", "data": f"{self.voice}:{self.text}".encode("utf-8")}


class FakeCommunicateRetrySuccess:
    attempts_by_text: dict[str, int] = {}

    def __init__(self, text: str, voice: str):
        self.text = text
        self.voice = voice

    async def stream(self):
        attempts = self.attempts_by_text.get(self.text, 0) + 1
        self.attempts_by_text[self.text] = attempts
        if attempts == 1:
            raise NoAudioReceived()

        yield {"type": "audio", "data": self.text.encode("utf-8")}


class EdgeTtsClientTests(unittest.TestCase):
    def test_synthesize_to_bytes_retries_with_smaller_chunks_after_no_audio_received(self) -> None:
        client = EdgeTtsClient(voice="vi-VN-HoaiMyNeural")

        with patch("app.edge_tts_client.edge_tts.Communicate", FakeCommunicateChunkFallback):
            audio_bytes = asyncio.run(client.synthesize_to_bytes("Xin chao. Toi la AI."))

        self.assertEqual(audio_bytes, b"Xin chao.Toi la AI.")

    def test_synthesize_to_bytes_falls_back_to_secondary_voice_when_primary_voice_fails(self) -> None:
        client = EdgeTtsClient(voice="vi-VN-HoaiMyNeural")

        with patch("app.edge_tts_client.edge_tts.Communicate", FakeCommunicateVoiceFallback):
            audio_bytes = asyncio.run(client.synthesize_to_bytes("Xin chao"))

        self.assertEqual(audio_bytes, b"vi-VN-NamMinhNeural:Xin chao")

    def test_synthesize_to_bytes_retries_same_voice_before_failing_over(self) -> None:
        client = EdgeTtsClient(voice="vi-VN-HoaiMyNeural")
        FakeCommunicateRetrySuccess.attempts_by_text = {}

        with patch("app.edge_tts_client.edge_tts.Communicate", FakeCommunicateRetrySuccess):
            audio_bytes = asyncio.run(client.synthesize_to_bytes("Xin chao"))

        self.assertEqual(audio_bytes, b"Xin chao")
        self.assertEqual(FakeCommunicateRetrySuccess.attempts_by_text["Xin chao"], 2)


if __name__ == "__main__":
    unittest.main()
