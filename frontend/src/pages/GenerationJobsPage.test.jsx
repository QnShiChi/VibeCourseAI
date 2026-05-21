import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { describe, expect, it, vi } from "vitest";
import GenerationJobsPage from "./GenerationJobsPage";

const mockGetGenerationJobs = vi.fn();
const mockGetGenerationJobDetail = vi.fn();

vi.mock("../api/generationJobService", () => ({
  getGenerationJobs: (...args) => mockGetGenerationJobs(...args),
  getGenerationJobDetail: (...args) => mockGetGenerationJobDetail(...args)
}));

describe("GenerationJobsPage", () => {
  it("renders generation jobs with status and course title", async () => {
    mockGetGenerationJobs.mockResolvedValue([
      {
        id: "job-1",
        syllabusId: "sy-1",
        syllabusTitle: "Lap trinh huong doi tuong",
        courseTitle: "Lap trinh huong doi tuong",
        status: "Completed"
      }
    ]);
    mockGetGenerationJobDetail.mockResolvedValue({
      id: "job-1",
      syllabusId: "sy-1",
      syllabusTitle: "Lap trinh huong doi tuong",
      courseTitle: "Lap trinh huong doi tuong",
      status: "Completed",
      errorMessage: "",
      createdByName: "Admin User",
      createdAt: new Date("2026-05-20T10:00:00Z").toISOString(),
      startedAt: new Date("2026-05-20T10:01:00Z").toISOString(),
      completedAt: new Date("2026-05-20T10:02:00Z").toISOString()
    });

    render(
      <MemoryRouter>
        <GenerationJobsPage />
      </MemoryRouter>
    );

    expect(await screen.findByRole("heading", { name: "Generation Jobs" })).toBeInTheDocument();
    expect(await screen.findByText("Chi tiết job")).toBeInTheDocument();
    expect(await screen.findAllByText("Lap trinh huong doi tuong")).toHaveLength(4);
    expect(await screen.findAllByText("Completed")).toHaveLength(2);
  });
});
