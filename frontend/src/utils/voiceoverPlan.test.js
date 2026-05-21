import { describe, expect, it } from "vitest";
import {
  normalizeVoiceoverPlan,
  parseVoiceoverPlanJson,
  serializeVoiceoverPlan,
  validateVoiceoverPlan
} from "./voiceoverPlan";

describe("voiceoverPlan helpers", () => {
  it("parses camelCase voiceover JSON into a normalized object", () => {
    const plan = parseVoiceoverPlanJson(
      '{"estimatedDurationMinutes":8,"tone":" Clear ","pacing":" Moderate ","targetAudience":" Students ","pronunciationNotes":" OOP "}'
    );

    expect(plan).toEqual({
      estimatedDurationMinutes: 8,
      tone: " Clear ",
      pacing: " Moderate ",
      targetAudience: " Students ",
      pronunciationNotes: " OOP "
    });
  });

  it("parses PascalCase voiceover JSON into a normalized object", () => {
    const plan = parseVoiceoverPlanJson(
      '{"EstimatedDurationMinutes":8,"Tone":" Clear ","Pacing":" Moderate ","TargetAudience":" Students ","PronunciationNotes":" OOP "}'
    );

    expect(plan).toEqual({
      estimatedDurationMinutes: 8,
      tone: " Clear ",
      pacing: " Moderate ",
      targetAudience: " Students ",
      pronunciationNotes: " OOP "
    });
  });

  it("normalizes and trims voiceover plan fields before save", () => {
    expect(
      normalizeVoiceoverPlan({
        estimatedDurationMinutes: "8",
        tone: " Clear ",
        pacing: " Moderate ",
        targetAudience: " Students ",
        pronunciationNotes: " OOP "
      })
    ).toEqual({
      estimatedDurationMinutes: 8,
      tone: "Clear",
      pacing: "Moderate",
      targetAudience: "Students",
      pronunciationNotes: "OOP"
    });
  });

  it("rejects malformed JSON", () => {
    expect(() => parseVoiceoverPlanJson("{bad json}")).toThrow();
  });

  it("rejects missing or invalid required fields", () => {
    expect(() =>
      validateVoiceoverPlan({
        estimatedDurationMinutes: 0,
        tone: "",
        pacing: "Moderate",
        targetAudience: "Students",
        pronunciationNotes: "OOP"
      })
    ).toThrow("Thời lượng dự kiến phải lớn hơn 0.");
  });

  it("serializes normalized voiceover plans to camelCase JSON", () => {
    expect(
      serializeVoiceoverPlan({
        estimatedDurationMinutes: 8,
        tone: "Clear",
        pacing: "Moderate",
        targetAudience: "Students",
        pronunciationNotes: "OOP"
      })
    ).toBe(
      '{"estimatedDurationMinutes":8,"tone":"Clear","pacing":"Moderate","targetAudience":"Students","pronunciationNotes":"OOP"}'
    );
  });
});
