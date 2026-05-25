import os
import edge_tts


class EdgeTtsClient:
    def __init__(self, voice: str | None = None):
        self.voice = voice or os.getenv("OPENAI_TTS_VOICE", "vi-VN-HoaiMyNeural")

    async def synthesize_to_bytes(self, text: str) -> bytes:
        communicate = edge_tts.Communicate(text, self.voice)
        audio_data = bytearray()
        async for chunk in communicate.stream():
            if chunk["type"] == "audio":
                audio_data.extend(chunk["data"])
        return bytes(audio_data)
