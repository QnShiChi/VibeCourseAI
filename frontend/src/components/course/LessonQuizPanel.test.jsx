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
        quizId="quiz-1"
        onLoadQuiz={onLoadQuiz}
        onStartAttempt={onStartAttempt}
        onSubmitAttempt={onSubmitAttempt}
      />
    );

    fireEvent.click(await screen.findByRole("button", { name: "Làm quiz" }));
    fireEvent.click(await screen.findByLabelText("Tri tue con nguoi"));
    fireEvent.click(screen.getByRole("button", { name: "Nộp bài" }));

    expect(await screen.findByText("100")).toBeInTheDocument();
    expect(screen.getByText("AI mo phong tri tue con nguoi.")).toBeInTheDocument();
  });

  it("shows a friendly message when quiz is not found", async () => {
    const onLoadQuiz = vi.fn().mockRejectedValue({ response: { status: 404 } });

    render(
      <LessonQuizPanel
        lessonId="lesson-1"
        initialStatus="Ready"
        quizId="quiz-1"
        onLoadQuiz={onLoadQuiz}
        onStartAttempt={vi.fn()}
        onSubmitAttempt={vi.fn()}
      />
    );

    fireEvent.click(await screen.findByRole("button", { name: "Làm quiz" }));

    expect(await screen.findByText("Quiz cho bài học này chưa sẵn sàng. Vui lòng thử lại sau.")).toBeInTheDocument();
  });

  it("does not render when the lesson has no quiz metadata", () => {
    const { container } = render(
      <LessonQuizPanel
        lessonId="lesson-1"
        initialStatus=""
        quizId=""
        onLoadQuiz={vi.fn()}
        onStartAttempt={vi.fn()}
        onSubmitAttempt={vi.fn()}
      />
    );

    expect(container).toBeEmptyDOMElement();
  });

  it("resets the loaded quiz state when switching to another lesson", async () => {
    const onLoadQuiz = vi.fn().mockResolvedValue({
      quizId: "quiz-1",
      title: "Kiem tra nhanh bai 1",
      status: "Ready",
      questionCount: 1,
      questions: [
        {
          questionId: "q1",
          questionText: "AI mo phong dieu gi?",
          explanation: "AI mo phong tri tue con nguoi.",
          options: [
            { optionId: "o1", optionText: "Tri tue con nguoi" },
            { optionId: "o2", optionText: "May in" }
          ]
        }
      ]
    });
    const onStartAttempt = vi.fn().mockResolvedValue({ attemptId: "attempt-1", startedAt: "2026-05-28T00:00:00Z" });

    const { rerender } = render(
      <LessonQuizPanel
        initialQuestionCount={1}
        initialStatus="Ready"
        lessonId="lesson-1"
        lessonTitle="Bai 1"
        quizId="quiz-1"
        onLoadQuiz={onLoadQuiz}
        onStartAttempt={onStartAttempt}
        onSubmitAttempt={vi.fn()}
      />
    );

    fireEvent.click(await screen.findByRole("button", { name: "Làm quiz" }));
    expect(await screen.findByText("Kiem tra nhanh bai 1")).toBeInTheDocument();

    rerender(
      <LessonQuizPanel
        initialQuestionCount={2}
        initialStatus="Ready"
        lessonId="lesson-2"
        lessonTitle="Bai 2"
        quizId="quiz-2"
        onLoadQuiz={onLoadQuiz}
        onStartAttempt={onStartAttempt}
        onSubmitAttempt={vi.fn()}
      />
    );

    expect(await screen.findByText("Bai 2")).toBeInTheDocument();
    expect(await screen.findByRole("button", { name: "Làm quiz" })).toBeInTheDocument();
    expect(screen.queryByText("Kiem tra nhanh bai 1")).not.toBeInTheDocument();
  });
});
