import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import CommentModerationPage from "./CommentModerationPage";

const mockGetNegativeComments = vi.fn();
const mockDeleteNegativeComment = vi.fn();
const mockUpdateUserActive = vi.fn();

vi.mock("../api/adminCommentModerationService", () => ({
  getNegativeComments: (...args) => mockGetNegativeComments(...args),
  deleteNegativeComment: (...args) => mockDeleteNegativeComment(...args)
}));

vi.mock("../api/userService", () => ({
  updateUserActive: (...args) => mockUpdateUserActive(...args)
}));

function renderPage() {
  render(
    <MemoryRouter>
      <CommentModerationPage />
    </MemoryRouter>
  );
}

describe("CommentModerationPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("renders negative comments with moderation actions", async () => {
    mockGetNegativeComments.mockResolvedValue([
      {
        commentId: "comment-1",
        lessonId: "lesson-1",
        lessonTitle: "Bai 1",
        courseId: "course-1",
        courseTitle: "Lap trinh Mobile",
        authorUserId: "user-1",
        authorName: "Hoc vien A",
        authorEmail: "a@example.com",
        content: "Noi dung rat te",
        createdAt: "2026-06-11T12:00:00Z",
        sentiment: "negative"
      }
    ]);

    renderPage();

    expect(await screen.findByRole("heading", { name: "Điều phối bình luận tiêu cực" })).toBeInTheDocument();
    expect(screen.getByText("Hoc vien A")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Xóa bình luận" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Khóa tài khoản" })).toBeInTheDocument();
  });

  it("deletes a comment from the moderation queue", async () => {
    mockGetNegativeComments.mockResolvedValue([
      {
        commentId: "comment-1",
        lessonId: "lesson-1",
        lessonTitle: "Bai 1",
        courseId: "course-1",
        courseTitle: "Lap trinh Mobile",
        authorUserId: "user-1",
        authorName: "Hoc vien A",
        authorEmail: "a@example.com",
        content: "Noi dung rat te",
        createdAt: "2026-06-11T12:00:00Z",
        sentiment: "negative"
      }
    ]);
    mockDeleteNegativeComment.mockResolvedValue({});

    renderPage();
    fireEvent.click(await screen.findByRole("button", { name: "Xóa bình luận" }));

    await waitFor(() => {
      expect(mockDeleteNegativeComment).toHaveBeenCalledWith("comment-1", "lesson-1");
    });
    expect(screen.queryByText("Noi dung rat te")).not.toBeInTheDocument();
  });

  it("bans a user from the moderation queue", async () => {
    mockGetNegativeComments.mockResolvedValue([
      {
        commentId: "comment-1",
        lessonId: "lesson-1",
        lessonTitle: "Bai 1",
        courseId: "course-1",
        courseTitle: "Lap trinh Mobile",
        authorUserId: "user-1",
        authorName: "Hoc vien A",
        authorEmail: "a@example.com",
        content: "Noi dung rat te",
        createdAt: "2026-06-11T12:00:00Z",
        sentiment: "negative"
      }
    ]);
    mockUpdateUserActive.mockResolvedValue({});

    renderPage();
    fireEvent.click(await screen.findByRole("button", { name: "Khóa tài khoản" }));

    await waitFor(() => {
      expect(mockUpdateUserActive).toHaveBeenCalledWith("user-1", false);
    });
    expect(screen.queryByText("Noi dung rat te")).not.toBeInTheDocument();
  });
});
