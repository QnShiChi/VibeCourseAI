import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import LessonQuizPanel from "./LessonQuizPanel";

describe("LessonQuizPanel", () => {
  it("loads quiz and submits answers", async () => {
    const onLoadQuiz = vi.fn().mockResolvedValue({
      quizId: "quiz-1",
      title: "Kiem tra nhanh",
      status: "Ready",
      questionCount: 1,
      questions: [
        {
          questionId: "q1",
          questionText: "AI mo phong dieu gi?",
          explanation: "AI mo phong tri tue con nguoi.",
          options: [
            { optionId: "o1", optionText: "Tri tue con nguoi" },
            { optionId: "o2", optionText: "May in" },
            { optionId: "o3", optionText: "Ban phim" },
            { optionId: "o4", optionText: "Loa" }
          ]
        }
      ]
    });
    const onStartAttempt = vi.fn().mockResolvedValue({ attemptId: "attempt-1", startedAt: "2026-05-28T00:00:00Z" });
    const onSubmitAttempt = vi.fn().mockResolvedValue({
      attemptId: "attempt-1",
      score: 100,
      correctCount: 1,
      totalQuestions: 1,
      answers: [
        {
          questionId: "q1",
          selectedOptionId: "o1",
          correctOptionId: "o1",
          isCorrect: true,
          explanation: "AI mo phong tri tue con nguoi."
        }
      ]
    });

    render(
      <LessonQuizPanel
        lessonId="lesson-1"
        initialStatus="Ready"
        onLoadQuiz={onLoadQuiz}
        onStartAttempt={onStartAttempt}
        onSubmitAttempt={onSubmitAttempt}
      />
    );

    fireEvent.click(await screen.findByRole("button", { name: "Lam quiz" }));
    fireEvent.click(await screen.findByLabelText("Tri tue con nguoi"));
    fireEvent.click(screen.getByRole("button", { name: "Nop bai" }));

    expect(await screen.findByText("Diem: 100")).toBeInTheDocument();
    expect(screen.getByText("AI mo phong tri tue con nguoi.")).toBeInTheDocument();
  });
});
