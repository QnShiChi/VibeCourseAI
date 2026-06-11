import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import FinalQuizCard from "./FinalQuizCard";

describe("FinalQuizCard", () => {
  it("renders final quiz CTA when ready", () => {
    render(<FinalQuizCard courseId="course-1" quizId="quiz-1" questionCount={15} status="Ready" />);

    expect(screen.getByText("Quiz tong ket khoa hoc")).toBeInTheDocument();
    expect(screen.getByText("15 cau hoi")).toBeInTheDocument();
  });
});
