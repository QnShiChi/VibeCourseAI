import { fireEvent, render, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import LessonComments from "./LessonComments";

const mockGetLessonComments = vi.fn();
const mockCreateLessonComment = vi.fn();
const mockCreateLessonReply = vi.fn();
const mockAddLessonCommentReaction = vi.fn();
const mockRemoveLessonCommentReaction = vi.fn();
const mockDeleteLessonComment = vi.fn();
const mockHideLessonComment = vi.fn();
const mockUnhideLessonComment = vi.fn();

vi.mock("../../api/commentService", () => ({
  getLessonComments: (...args) => mockGetLessonComments(...args),
  createLessonComment: (...args) => mockCreateLessonComment(...args),
  createLessonReply: (...args) => mockCreateLessonReply(...args),
  addLessonCommentReaction: (...args) => mockAddLessonCommentReaction(...args),
  removeLessonCommentReaction: (...args) => mockRemoveLessonCommentReaction(...args),
  deleteLessonComment: (...args) => mockDeleteLessonComment(...args),
  hideLessonComment: (...args) => mockHideLessonComment(...args),
  unhideLessonComment: (...args) => mockUnhideLessonComment(...args)
}));

describe("LessonComments", () => {
  beforeEach(() => {
    mockGetLessonComments.mockResolvedValue({
      items: [
        {
          comment: {
            id: "comment-1",
            userId: "user-1",
            authorName: "Alice",
            content: "Comment gốc",
            isHidden: false,
            isDeleted: false,
            canDelete: true,
            canModerate: false,
            createdAt: "2026-05-21T08:00:00Z",
            reactions: []
          },
          replies: [
            {
              id: "reply-1",
              userId: "user-2",
              authorName: "Bob",
              replyToUserId: "user-1",
              replyToUserName: "Alice",
              content: "Mình đồng ý",
              isHidden: false,
              isDeleted: false,
              canDelete: false,
              canModerate: false,
              createdAt: "2026-05-21T08:05:00Z",
              reactions: []
            }
          ]
        }
      ],
      page: 1,
      pageSize: 10,
      totalCount: 1,
      hasMore: false,
      sort: "newest"
    });
    mockCreateLessonComment.mockResolvedValue({});
    mockCreateLessonReply.mockResolvedValue({});
    mockAddLessonCommentReaction.mockResolvedValue({});
    mockRemoveLessonCommentReaction.mockResolvedValue({});
    mockDeleteLessonComment.mockResolvedValue({});
    mockHideLessonComment.mockResolvedValue({});
    mockUnhideLessonComment.mockResolvedValue({});
  });

  it("prefills @username when replying to a reply", async () => {
    render(<LessonComments lessonId="lesson-1" />);

    fireEvent.click((await screen.findAllByRole("button", { name: "Reply" }))[1]);

    expect((await screen.findByPlaceholderText("Trả lời bình luận này...")).value).toContain("@Bob");
  });

  it("shows hide controls for admin users", async () => {
    render(<LessonComments isAdmin lessonId="lesson-1" />);

    expect((await screen.findAllByRole("button", { name: "Ẩn bình luận" })).length).toBeGreaterThan(0);
  });
});
