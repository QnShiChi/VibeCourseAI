import asyncio
import os
import re

import edge_tts
from edge_tts.exceptions import NoAudioReceived


class EdgeTtsClient:
    def __init__(self, voice: str | None = None):
        self.voice = voice or os.getenv("OPENAI_TTS_VOICE", "vi-VN-HoaiMyNeural")

    async def synthesize_to_bytes(self, text: str) -> bytes:
        normalized_text = self._normalize_text(text)
        if not normalized_text:
            raise ValueError("Text to speech không nhận được nội dung narration hợp lệ.")

        try:
            return await self._synthesize_with_retries(normalized_text)
        except NoAudioReceived:
            chunks = self._split_into_chunks(normalized_text)
            if len(chunks) == 1:
                raise

            audio_parts: list[bytes] = []
            for chunk in chunks:
                audio_parts.append(await self._synthesize_with_retries(chunk))
                await asyncio.sleep(1.5)

            return b"".join(audio_parts)

    async def _synthesize_with_retries(self, text: str, attempts_per_voice: int = 2) -> bytes:
        last_exception: Exception | None = None

        for voice in self._voice_candidates():
            for attempt in range(attempts_per_voice):
                try:
                    return await self._synthesize_once(text, voice)
                except NoAudioReceived as exception:
                    last_exception = exception
                    if attempt < attempts_per_voice - 1:
                        await asyncio.sleep(0.75 * (attempt + 1))

        if last_exception is not None:
            raise last_exception

        raise NoAudioReceived()

    async def _synthesize_once(self, text: str, voice: str) -> bytes:
        communicate = edge_tts.Communicate(text, voice)
        audio_data = bytearray()
        async for chunk in communicate.stream():
            if chunk["type"] == "audio":
                audio_data.extend(chunk["data"])

        if not audio_data:
            raise NoAudioReceived()

        return bytes(audio_data)

    def _voice_candidates(self) -> list[str]:
        fallback_map = {
            "vi-VN-HoaiMyNeural": "vi-VN-NamMinhNeural",
            "vi-VN-NamMinhNeural": "vi-VN-HoaiMyNeural",
        }

        voices = [self.voice]
        fallback_voice = fallback_map.get(self.voice)
        if fallback_voice and fallback_voice not in voices:
            voices.append(fallback_voice)

        return voices

    @staticmethod
    def _normalize_text(text: str) -> str:
        normalized = re.sub(r"\s+", " ", text).strip()
        normalized = normalized.replace("•", ", ").replace("–", "-").replace("—", "-")
        return normalized

    @classmethod
    def _split_into_chunks(cls, text: str, max_chars: int = 220) -> list[str]:
        sentence_parts = [cls._normalize_text(part) for part in re.split(r"(?<=[.!?;:])\s+", text) if cls._normalize_text(part)]
        if len(sentence_parts) > 1:
            return sentence_parts

        return cls._split_long_part(text, max_chars)

    @classmethod
    def _split_long_part(cls, text: str, max_chars: int) -> list[str]:
        normalized = cls._normalize_text(text)
        phrase_parts = [cls._normalize_text(part) for part in re.split(r"(?<=[,])\s+", normalized) if cls._normalize_text(part)]

        if len(phrase_parts) > 1:
            return phrase_parts

        if len(normalized) > max_chars:
            return [normalized[index:index + max_chars].strip() for index in range(0, len(normalized), max_chars)]

        words = normalized.split()
        if len(words) <= 1:
            return [normalized]

        midpoint = max(1, len(words) // 2)
        first_half = " ".join(words[:midpoint]).strip()
        second_half = " ".join(words[midpoint:]).strip()
        return [chunk for chunk in [first_half, second_half] if chunk]
