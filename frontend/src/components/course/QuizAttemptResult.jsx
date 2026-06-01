import { useMemo, useState } from "react";

function getScoreTone(score) {
  if (score >= 85) {
    return "excellent";
  }

  if (score >= 70) {
    return "solid";
  }

  return "retry";
}

function getScoreMessage(score) {
  if (score >= 85) {
    return "Bạn đã nắm bài rất tốt. Có thể chuyển sang phần tiếp theo.";
  }

  if (score >= 70) {
    return "Kết quả ổn định. Nên xem lại các câu sai để tăng độ chắc kiến thức.";
  }

  return "Bạn nên ôn lại lesson này và làm lại quiz để củng cố kiến thức.";
}

export default function QuizAttemptResult({ result, questions, onRetake, title }) {
  const [filter, setFilter] = useState("all");
  const answerLookup = useMemo(
    () =>
      new Map(
        result.answers.map((answer) => [answer.questionId, answer])
      ),
    [result.answers]
  );
  const scoredQuestions = questions.map((question) => {
    const answer = answerLookup.get(question.questionId);
    const selectedOption = question.options.find((option) => option.optionId === answer?.selectedOptionId);
    const correctOption = question.options.find((option) => option.optionId === answer?.correctOptionId);

    return {
      answer,
      correctOption,
      question,
      selectedOption
    };
  });
  const incorrectCount = Math.max(0, result.totalQuestions - result.correctCount);
  const firstIncorrect = scoredQuestions.find((item) => !item.answer?.isCorrect);
  const visibleQuestions = filter === "mistakes"
    ? scoredQuestions.filter((item) => !item.answer?.isCorrect)
    : scoredQuestions;
  const scoreTone = getScoreTone(Number(result.score));

  return (
    <div className={`quiz-result quiz-result--${scoreTone}`}>
      <div className="quiz-result__hero">
        <div
          aria-hidden="true"
          className="quiz-result__score-ring"
          style={{ background: `conic-gradient(#9ee939 0deg ${Math.max(0, Math.min(100, Number(result.score))) * 3.6}deg, rgba(255, 255, 255, 0.08) ${Math.max(0, Math.min(100, Number(result.score))) * 3.6}deg 360deg)` }}
        >
          <div className="quiz-result__score-core">
            <strong>{Math.round(Number(result.score))}</strong>
            <span>điểm / 100</span>
          </div>
        </div>

        <div className="quiz-result__hero-copy">
          <p className="quiz-result__eyebrow">Lesson Quiz Complete</p>
          <h3>{title}</h3>
          <p>{getScoreMessage(Number(result.score))}</p>
        </div>
      </div>

      <div className="quiz-result__summary-grid">
        <article className="quiz-result__summary-card quiz-result__summary-card--correct">
          <strong>{result.correctCount} câu đúng</strong>
          <span>Kết quả tốt ở phần kiến thức cốt lõi của lesson.</span>
        </article>
        <article className="quiz-result__summary-card quiz-result__summary-card--incorrect">
          <strong>{incorrectCount} câu sai</strong>
          <span>Tập trung xem lại các đáp án lệch để tránh lặp lại lỗi tương tự.</span>
        </article>
        <article className="quiz-result__summary-card quiz-result__summary-card--recommendation">
          <strong>Gợi ý ôn tập</strong>
          <span>{firstIncorrect?.answer?.explanation || "Bạn đã hoàn thành rất tốt. Có thể chuyển sang bài tiếp theo."}</span>
        </article>
      </div>

      <div className="quiz-result__analysis">
        <div className="quiz-result__analysis-header">
          <div>
            <p className="quiz-result__eyebrow">Question Analysis</p>
            <h4>Phân tích từng câu hỏi</h4>
          </div>
          <button
            className="quiz-result__filter-button"
            onClick={() => setFilter((current) => (current === "all" ? "mistakes" : "all"))}
            type="button"
          >
            {filter === "all" ? "Chỉ xem câu sai" : "Xem tất cả"}
          </button>
        </div>

        <div className="quiz-result__answers">
          {visibleQuestions.map(({ answer, correctOption, question, selectedOption }, index) => (
            <article className={`quiz-result__answer${answer?.isCorrect ? " quiz-result__answer--correct" : " quiz-result__answer--incorrect"}`} key={question.questionId}>
              <div className="quiz-result__answer-header">
                <div>
                  <span className="quiz-result__question-badge">Q{index + 1}</span>
                  <strong>{answer?.isCorrect ? "Đúng" : "Chưa đúng"}</strong>
                </div>
              </div>
              <p className="quiz-result__question-text">{question.questionText}</p>
              <div className="quiz-result__option-review">
                <span className={`quiz-result__option-chip${answer?.isCorrect ? " quiz-result__option-chip--correct" : " quiz-result__option-chip--incorrect"}`}>
                  Bạn chọn: {selectedOption?.optionText || "Chưa trả lời"}
                </span>
                <span className="quiz-result__option-chip quiz-result__option-chip--answer">
                  Đáp án đúng: {correctOption?.optionText || "Không có dữ liệu"}
                </span>
              </div>
              <p className="quiz-result__explanation">{answer?.explanation}</p>
            </article>
          ))}
        </div>
      </div>

      <div className="quiz-result__footer">
        <button className="lesson-quiz-panel__primary-action" onClick={onRetake} type="button">
          Làm lại quiz
        </button>
      </div>
    </div>
  );
}
