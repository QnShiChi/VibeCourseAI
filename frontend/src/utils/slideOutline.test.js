import { describe, expect, it } from "vitest";
import {
  normalizeSlides,
  parseSlideOutlineJson,
  serializeSlideOutline,
  validateSlides
} from "./slideOutline";

describe("slideOutline helpers", () => {
  it("parses valid slide JSON into structured slides", () => {
    const slides = parseSlideOutlineJson(
      '[{"slideNumber":2,"title":" Intro ","bulletPoints":[" A ",""],"speakerNotes":" Notes "}]'
    );

    expect(slides).toEqual([
      {
        slideNumber: 2,
        title: " Intro ",
        bulletPoints: [" A ", ""],
        speakerNotes: " Notes "
      }
    ]);
  });

  it("parses legacy slide JSON with PascalCase keys", () => {
    const slides = parseSlideOutlineJson(
      '[{"SlideNumber":3,"Title":" Legacy ","BulletPoints":[" A "," B "],"SpeakerNotes":" Notes "}]'
    );

    expect(slides).toEqual([
      {
        slideNumber: 3,
        title: " Legacy ",
        bulletPoints: [" A ", " B "],
        speakerNotes: " Notes "
      }
    ]);
  });

  it("normalizes slides before save", () => {
    const normalized = normalizeSlides([
      {
        slideNumber: 8,
        title: " Intro ",
        bulletPoints: [" A ", "", " B "],
        speakerNotes: " Notes "
      }
    ]);

    expect(normalized).toEqual([
      {
        slideNumber: 1,
        title: "Intro",
        bulletPoints: ["A", "B"],
        speakerNotes: "Notes"
      }
    ]);
  });

  it("rejects slides missing title, bullet points, or speaker notes", () => {
    expect(() =>
      validateSlides([
        { slideNumber: 1, title: "", bulletPoints: ["A"], speakerNotes: "N" }
      ])
    ).toThrow("Tiêu đề slide là bắt buộc.");
  });

  it("serializes normalized slides to JSON array string", () => {
    const json = serializeSlideOutline([
      { slideNumber: 1, title: "Intro", bulletPoints: ["A"], speakerNotes: "N" }
    ]);

    expect(json).toBe(
      '[{"slideNumber":1,"title":"Intro","bulletPoints":["A"],"speakerNotes":"N"}]'
    );
  });
});
