import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import CommentReactionBar from "./CommentReactionBar";

describe("CommentReactionBar", () => {
  it("opens the popup on hover and selects a new emoji", () => {
    const onSelectReaction = vi.fn();

    render(<CommentReactionBar commentId="comment-1" onSelectReaction={onSelectReaction} reactions={[]} />);

    fireEvent.mouseEnter(screen.getByRole("button", { name: /like/i }));
    fireEvent.click(screen.getByRole("button", { name: "😀" }));

    expect(onSelectReaction).toHaveBeenCalledWith("comment-1", null, "😀");
  });

  it("removes the current reaction when clicking the selected primary button again", () => {
    const onSelectReaction = vi.fn();

    render(
      <CommentReactionBar
        commentId="comment-1"
        onSelectReaction={onSelectReaction}
        reactions={[{ emoji: "❤️", count: 3, reactedByCurrentUser: true }]}
      />
    );

    fireEvent.click(screen.getByRole("button", { name: /đã thả cảm xúc/i }));

    expect(onSelectReaction).toHaveBeenCalledWith("comment-1", "❤️", null);
  });

  it("shows only the top 3 reactions in the summary", () => {
    render(
      <CommentReactionBar
        commentId="comment-1"
        onSelectReaction={() => {}}
        reactions={[
          { emoji: "👍", count: 5, reactedByCurrentUser: false },
          { emoji: "❤️", count: 7, reactedByCurrentUser: false },
          { emoji: "🔥", count: 3, reactedByCurrentUser: false },
          { emoji: "😀", count: 2, reactedByCurrentUser: false }
        ]}
      />
    );

    const summary = screen.getByRole("list", { name: "Tóm tắt cảm xúc" });
    const items = screen.getAllByRole("listitem");

    expect(summary).toBeInTheDocument();
    expect(items).toHaveLength(3);
    expect(screen.getByLabelText("❤️ 7")).toBeInTheDocument();
    expect(screen.getByLabelText("👍 5")).toBeInTheDocument();
    expect(screen.getByLabelText("🔥 3")).toBeInTheDocument();
    expect(screen.queryByLabelText("😀 2")).not.toBeInTheDocument();
  });
});
