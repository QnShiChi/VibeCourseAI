import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { describe, expect, it, vi } from "vitest";
import SyllabusesPage from "./SyllabusesPage";

const mockGetSyllabuses = vi.fn();
const mockGetSyllabusDetail = vi.fn();
const mockImportSyllabus = vi.fn();
const mockDeleteSyllabus = vi.fn();
const mockGenerateSyllabusCourse = vi.fn();
const mockGetGenerationJobs = vi.fn();

vi.mock("../api/syllabusService", () => ({
  getSyllabuses: (...args) => mockGetSyllabuses(...args),
  getSyllabusDetail: (...args) => mockGetSyllabusDetail(...args),
  importSyllabus: (...args) => mockImportSyllabus(...args),
  deleteSyllabus: (...args) => mockDeleteSyllabus(...args),
  generateSyllabusCourse: (...args) => mockGenerateSyllabusCourse(...args)
}));

vi.mock("../api/generationJobService", () => ({
  getGenerationJobs: (...args) => mockGetGenerationJobs(...args)
}));

describe("SyllabusesPage", () => {
  it("renders the upload form and admin syllabus heading", async () => {
    mockGetSyllabuses.mockResolvedValue([]);
    mockGetGenerationJobs.mockResolvedValue([]);

    render(
      <MemoryRouter>
        <SyllabusesPage />
      </MemoryRouter>
    );

    expect(screen.getByRole("heading", { name: "Đề cương" })).toBeInTheDocument();
    expect(screen.getByLabelText("Tiêu đề")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Import đề cương" })).toBeInTheDocument();
    expect(await screen.findByText("Chưa có đề cương nào được import.")).toBeInTheDocument();
  });

  it("calls generate API and shows success message", async () => {
    mockGetSyllabuses.mockResolvedValue([
      { id: "syllabus-1", title: "OOP", originalFileName: "oop.pdf", fileType: "pdf" }
    ]);
    mockGetGenerationJobs.mockResolvedValue([]);
    mockGetSyllabusDetail.mockResolvedValue({
      id: "syllabus-1",
      title: "OOP",
      description: "Mo ta",
      originalFileName: "oop.pdf",
      fileType: "pdf",
      fileSize: 123,
      extractedText: "Noi dung"
    });
    mockGenerateSyllabusCourse.mockResolvedValue({
      jobId: "job-1",
      courseTitle: "OOP",
      status: "Completed"
    });

    render(
      <MemoryRouter>
        <SyllabusesPage />
      </MemoryRouter>
    );

    const button = await screen.findByRole("button", { name: "Generate khóa học" });
    fireEvent.click(button);

    await waitFor(() => expect(mockGenerateSyllabusCourse).toHaveBeenCalledWith("syllabus-1"));
    expect(await screen.findByText("Đã tạo course structure bằng AI cho khóa học: OOP.")).toBeInTheDocument();
  });

  it("disables generate button when syllabus already has a completed job", async () => {
    mockGetSyllabuses.mockResolvedValue([
      { id: "syllabus-1", title: "OOP", originalFileName: "oop.pdf", fileType: "pdf" }
    ]);
    mockGetGenerationJobs.mockResolvedValue([
      { id: "job-1", syllabusId: "syllabus-1", status: "Completed" }
    ]);
    mockGetSyllabusDetail.mockResolvedValue({
      id: "syllabus-1",
      title: "OOP",
      description: "Mo ta",
      originalFileName: "oop.pdf",
      fileType: "pdf",
      fileSize: 123,
      extractedText: "Noi dung"
    });

    render(
      <MemoryRouter>
        <SyllabusesPage />
      </MemoryRouter>
    );

    const button = await screen.findByRole("button", { name: "Đề cương này đã generate" });
    expect(button).toBeDisabled();
  });

  it("shows backend AI error message when generate fails", async () => {
    mockGetSyllabuses.mockResolvedValue([
      { id: "syllabus-1", title: "OOP", originalFileName: "oop.pdf", fileType: "pdf" }
    ]);
    mockGetGenerationJobs.mockResolvedValue([]);
    mockGetSyllabusDetail.mockResolvedValue({
      id: "syllabus-1",
      title: "OOP",
      description: "Mo ta",
      originalFileName: "oop.pdf",
      fileType: "pdf",
      fileSize: 123,
      extractedText: "Noi dung"
    });
    mockGenerateSyllabusCourse.mockRejectedValue({
      response: {
        data: {
          message: "Thiếu cấu hình OPENROUTER_API_KEY."
        }
      }
    });

    render(
      <MemoryRouter>
        <SyllabusesPage />
      </MemoryRouter>
    );

    fireEvent.click(await screen.findByRole("button", { name: "Generate khóa học" }));

    expect(await screen.findByText("Thiếu cấu hình OPENROUTER_API_KEY.")).toBeInTheDocument();
  });
});
