import unittest

from app.openai_tts import OpenAITtsClient


class OpenAITtsClientTests(unittest.TestCase):
    def test_build_payload_uses_response_format(self) -> None:
        client = OpenAITtsClient(
            api_key="test",
            model="gpt-4o-mini-tts",
            voice="alloy",
            audio_format="wav",
        )

        payload = client.build_payload("Hello world")

        self.assertEqual(payload["model"], "gpt-4o-mini-tts")
        self.assertEqual(payload["voice"], "alloy")
        self.assertEqual(payload["input"], "Hello world")
        self.assertEqual(payload["response_format"], "wav")
        self.assertNotIn("format", payload)


if __name__ == "__main__":
    unittest.main()
