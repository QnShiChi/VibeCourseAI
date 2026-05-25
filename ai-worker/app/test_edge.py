import asyncio
import pyodbc
from app.narration import build_narration_segments
import edge_tts

async def main():
    conn = pyodbc.connect("DRIVER={ODBC Driver 18 for SQL Server};SERVER=sqlserver;DATABASE=vibe_course_ai_db;UID=sa;PWD=VibeCourse@123;TrustServerCertificate=yes")
    cursor = conn.cursor()
    cursor.execute("SELECT TOP 1 Title, ContentSeed, SlideOutlineJson, VoiceoverPlanJson FROM Lessons WHERE AudioGenerationStatus = 'Failed' ORDER BY UpdatedAt DESC")
    row = cursor.fetchone()
    
    content_seed = row[1]
    slide_outline = row[2]
    voiceover_plan = row[3]
    
    segments = build_narration_segments(content_seed, slide_outline, voiceover_plan)
    
    for i, seg in enumerate(segments):
        print(f"\n--- Segment {i} length: {len(seg.narration_text)}")
        print(f"Text: {seg.narration_text[:100]}...")
        
        communicate = edge_tts.Communicate(seg.narration_text, 'vi-VN-HoaiMyNeural')
        audio_data = bytearray()
        try:
            async for chunk in communicate.stream():
                if chunk["type"] == "audio":
                    audio_data.extend(chunk["data"])
            print(f"Success, length: {len(audio_data)}")
            if not audio_data:
                print("NO AUDIO RECEIVED FROM EDGE-TTS!")
        except Exception as e:
            print(f"FAILED on segment {i} STREAM: {repr(e)}")

asyncio.run(main())
import pyodbc
from app.edge_tts_client import EdgeTtsClient
from app.narration import build_narration_segments

async def main():
    conn = pyodbc.connect("DRIVER={ODBC Driver 18 for SQL Server};SERVER=sqlserver;DATABASE=vibe_course_ai_db;UID=sa;PWD=VibeCourse@123;TrustServerCertificate=yes")
    cursor = conn.cursor()
    cursor.execute("SELECT TOP 1 Title, ContentSeed, SlideOutlineJson, VoiceoverPlanJson FROM Lessons WHERE AudioGenerationStatus = 'Failed' ORDER BY UpdatedAt DESC")
    row = cursor.fetchone()
    
    title = row[0]
    content_seed = row[1]
    slide_outline = row[2]
    voiceover_plan = row[3]
    
    segments = build_narration_segments(content_seed, slide_outline, voiceover_plan)
    client = EdgeTtsClient()
    
    for i, seg in enumerate(segments):
        print(f"--- Segment {i} length: {len(seg.narration_text)}")
        try:
            audio = await client.synthesize_to_bytes(seg.narration_text)
            print(f"Success, length: {len(audio)}")
        except Exception as e:
            print(f"FAILED on segment {i}: {e}")
            print(f"Text snippet: {seg.narration_text[:100]}...")

asyncio.run(main())
