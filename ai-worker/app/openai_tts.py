import os

from openai import OpenAI


class OpenAITtsClient:
    def __init__(self, api_key: str | None = None, model: str | None = None, voice: str | None = None, audio_format: str | None = None):
        self.client = OpenAI(api_key=api_key or os.getenv("OPENAI_API_KEY"))
        self.model = model or os.getenv("OPENAI_TTS_MODEL", "gpt-4o-mini-tts")
        self.voice = voice or os.getenv("OPENAI_TTS_VOICE", "alloy")
        self.audio_format = audio_format or os.getenv("OPENAI_TTS_FORMAT", "wav")

    def build_payload(self, text: str) -> dict:
        return {
            "model": self.model,
            "voice": self.voice,
            "input": text,
            "response_format": self.audio_format,
        }

    def synthesize_to_bytes(self, text: str) -> bytes:
        response = self.client.audio.speech.create(**self.build_payload(text))
        return response.read()
