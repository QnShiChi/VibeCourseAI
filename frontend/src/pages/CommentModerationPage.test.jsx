import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import CommentModerationPage from "./CommentModerationPage";

const mockGetModerationComments = vi.fn();
const mockDeleteModerationComment = vi.fn();
const mockPinComment = vi.fn();
const mockGetPositiveCourseHighlights = vi.fn();
const mockUpdateUserActive = vi.fn();

vi.mock("../api/adminCommentModerationService", () => ({
  getModerationComments: (...args) => mockGetModerationComments(...args),
  deleteModerationComment: (...args) => mockDeleteModerationComment(...args),
  pinComment: (...args) => mockPinComment(...args),
  getPositiveCourseHighlights: (...args) => mockGetPositiveCourseHighlights(...args)
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
    mockGetPositiveCourseHighlights.mockResolvedValue([]);
  });

  it("renders negative comments by default with moderation actions", async () => {
    mockGetModerationComments.mockResolvedValue([
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

    expect(await screen.findByRole("heading", { name: "Điều phối bình luận" })).toBeInTheDocument();
    expect(screen.getByText("Hoc vien A")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Xóa bình luận" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Khóa tài khoản" })).toBeInTheDocument();
  });

  it("deletes a negative comment from the moderation queue", async () => {
    mockGetModerationComments.mockResolvedValue([
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
    mockDeleteModerationComment.mockResolvedValue({});

    renderPage();
    fireEvent.click(await screen.findByRole("button", { name: "Xóa bình luận" }));

    await waitFor(() => {
      expect(mockDeleteModerationComment).toHaveBeenCalledWith("comment-1", "lesson-1");
    });
    expect(screen.queryByText("Noi dung rat te")).not.toBeInTheDocument();
  });

  it("bans a user from the moderation queue", async () => {
    mockGetModerationComments.mockResolvedValue([
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

  it("switches to positive comments and pins a comment", async () => {
    mockGetModerationComments
      .mockResolvedValueOnce([])
      .mockResolvedValueOnce([
        {
          commentId: "comment-2",
          lessonId: "lesson-2",
          lessonTitle: "Bai 2",
          courseId: "course-2",
          courseTitle: "Tri tue nhan tao",
          authorUserId: "user-2",
          authorName: "Hoc vien B",
          authorEmail: "b@example.com",
          content: "Bai hoc rat de hieu",
          createdAt: "2026-06-11T13:00:00Z",
          sentiment: "positive",
          pinnedAt: null
        }
      ]);
    mockPinComment.mockResolvedValue({});

    renderPage();
    fireEvent.click(await screen.findByRole("button", { name: "Bình luận tích cực" }));

    expect(await screen.findByText("Hoc vien B")).toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: "Đẩy lên trước" }));

    await waitFor(() => {
      expect(mockPinComment).toHaveBeenCalledWith("comment-2");
    });
  });

  it("filters comments by author name for the active tab", async () => {
    mockGetModerationComments.mockResolvedValue([]);

    renderPage();
    fireEvent.change(await screen.findByPlaceholderText("Lọc theo tên người bình luận..."), {
      target: { value: "Duy" }
    });
    fireEvent.click(screen.getByRole("button", { name: "Lọc" }));

    await waitFor(() => {
      expect(mockGetModerationComments).toHaveBeenLastCalledWith({
        sentiment: "negative",
        authorName: "Duy"
      });
    });
  });

  it("switches to positive course highlights mode", async () => {
    mockGetModerationComments.mockResolvedValue([]);
    mockGetPositiveCourseHighlights.mockResolvedValue([
      {
        courseId: "course-9",
        courseTitle: "Khoa hoc noi bat",
        totalCommentCount: 5,
        positiveCommentCount: 3,
        positiveRatio: 0.6,
        latestPositiveCommentContent: "Rat huu ich",
        latestPositiveCommentAt: "2026-06-11T13:00:00Z"
      }
    ]);

    renderPage();
    fireEvent.click(await screen.findByRole("button", { name: "Khóa học tích cực nổi bật" }));

    expect(await screen.findByText("Khoa hoc noi bat")).toBeInTheDocument();
    expect(screen.getByText("60%")).toBeInTheDocument();
    expect(screen.getByText("Rat huu ich")).toBeInTheDocument();
    expect(screen.queryByPlaceholderText("Lọc theo tên người bình luận...")).not.toBeInTheDocument();
  });
});
