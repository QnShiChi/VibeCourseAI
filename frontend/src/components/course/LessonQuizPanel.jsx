import { useEffect, useState } from "react";
import QuizAttemptResult from "./QuizAttemptResult";

const OPTION_LABELS = ["A", "B", "C", "D", "E", "F"];

function getStatusCopy(initialQuestionCount) {
  if (!initialQuestionCount) {
    return "Sẵn sàng kiểm tra nhanh kiến thức của bạn ngay sau lesson.";
  }

  return `${initialQuestionCount} câu hỏi ngắn để củng cố lại các ý chính của bài học.`;
}

function getResultHeadline(score) {
  if (score >= 90) {
    return "Bạn đang nắm bài rất chắc.";
  }

  if (score >= 70) {
    return "Bạn đã hiểu phần lớn nội dung lesson.";
  }

  return "Nên ôn lại thêm vài ý chính trước khi sang bài tiếp theo.";
}

export default function LessonQuizPanel({
  lessonId,
  lessonTitle = "",
  quizId,
  initialQuestionCount = 0,
  initialStatus,
  onLoadQuiz,
  onStartAttempt,
  onSubmitAttempt
}) {
  const [quiz, setQuiz] = useState(null);
  const [attemptId, setAttemptId] = useState("");
  const [answers, setAnswers] = useState({});
  const [result, setResult] = useState(null);
  const [isLoading, setIsLoading] = useState(false);
  const [errorMessage, setErrorMessage] = useState("");
  const [currentQuestionIndex, setCurrentQuestionIndex] = useState(0);

  useEffect(() => {
    setQuiz(null);
    setAttemptId("");
    setAnswers({});
    setResult(null);
    setIsLoading(false);
    setErrorMessage("");
    setCurrentQuestionIndex(0);
  }, [lessonId, quizId]);

  async function handleStart() {
    setIsLoading(true);
    setErrorMessage("");
    try {
      const loadedQuiz = await onLoadQuiz(lessonId);
      const startedAttempt = await onStartAttempt(loadedQuiz.quizId);
      setQuiz(loadedQuiz);
      setAttemptId(startedAttempt.attemptId);
      setAnswers({});
      setResult(null);
      setCurrentQuestionIndex(0);
    } catch (error) {
      if (error?.response?.status === 404) {
        setErrorMessage("Quiz cho bài học này chưa sẵn sàng. Vui lòng thử lại sau.");
      } else {
        setErrorMessage("Không thể tải quiz lúc này. Vui lòng thử lại sau.");
      }
    } finally {
      setIsLoading(false);
    }
  }

  async function handleSubmit() {
    setIsLoading(true);
    setErrorMessage("");
    const payload = {
      answers: quiz.questions.map((question) => ({
        questionId: question.questionId,
        selectedOptionId: answers[question.questionId]
      }))
    };

    try {
      const submittedResult = await onSubmitAttempt(quiz.quizId, attemptId, payload);
      setResult(submittedResult);
    } catch {
      setErrorMessage("Không thể nộp bài lúc này. Vui lòng thử lại.");
    } finally {
      setIsLoading(false);
    }
  }

  async function handleRetake() {
    if (!quiz) {
      await handleStart();
      return;
    }

    setIsLoading(true);
    setErrorMessage("");
    try {
      const startedAttempt = await onStartAttempt(quiz.quizId);
      setAttemptId(startedAttempt.attemptId);
      setAnswers({});
      setResult(null);
      setCurrentQuestionIndex(0);
    } catch {
      setErrorMessage("Không thể tạo lượt làm mới lúc này. Vui lòng thử lại.");
    } finally {
      setIsLoading(false);
    }
  }

  if (!quizId && !initialStatus) {
    return null;
  }

  if (initialStatus && initialStatus !== "Ready") {
    return (
      <div className="lesson-quiz-panel lesson-quiz-panel--launch">
        <div className="lesson-quiz-panel__hero">
          <p className="lesson-quiz-panel__eyebrow">Quick Assessment</p>
          <h3>Quiz lesson đang được chuẩn bị</h3>
          <p>Hệ thống đang hoàn thiện bộ câu hỏi cho bài học này. Bạn quay lại sau ít phút để bắt đầu làm quiz.</p>
        </div>
        <div className="lesson-quiz-panel__launch-meta">
          <span className="lesson-quiz-panel__meta-pill">Trạng thái: {initialStatus}</span>
        </div>
      </div>
    );
  }

  const totalQuestions = quiz?.questions.length ?? initialQuestionCount ?? 0;
  const answeredCount = quiz ? quiz.questions.filter((question) => Boolean(answers[question.questionId])).length : 0;
  const currentQuestion = quiz?.questions[currentQuestionIndex] ?? null;
  const isLastQuestion = quiz ? currentQuestionIndex === quiz.questions.length - 1 : false;
  const currentSelection = currentQuestion ? answers[currentQuestion.questionId] : "";
  const progressPercent = quiz && quiz.questions.length ? Math.round(((currentQuestionIndex + 1) / quiz.questions.length) * 100) : 0;

  return (
    <div className={`lesson-quiz-panel${quiz ? " lesson-quiz-panel--active" : " lesson-quiz-panel--launch"}`}>
      {errorMessage ? <p>{errorMessage}</p> : null}
      {!quiz ? (
        <>
          <div className="lesson-quiz-panel__hero">
            <p className="lesson-quiz-panel__eyebrow">Quick Assessment</p>
            <h3>{lessonTitle || "Kiểm tra nhanh sau bài học"}</h3>
            <p>{getStatusCopy(initialQuestionCount)}</p>
          </div>
          <div className="lesson-quiz-panel__launch-meta">
            {totalQuestions ? <span className="lesson-quiz-panel__meta-pill">{totalQuestions} câu hỏi</span> : null}
            <span className="lesson-quiz-panel__meta-pill">Quiz theo lesson</span>
          </div>
          <button className="lesson-quiz-panel__primary-action" disabled={isLoading} onClick={handleStart} type="button">
            {isLoading ? "Đang tải quiz..." : "Làm quiz"}
          </button>
        </>
      ) : result ? (
        <QuizAttemptResult
          onRetake={handleRetake}
          questions={quiz.questions}
          result={result}
          title={quiz.title || lessonTitle || "Kết quả quiz lesson"}
        />
      ) : (
        <>
          <div className="lesson-quiz-panel__header">
            <div>
              <p className="lesson-quiz-panel__eyebrow">Quick Assessment</p>
              <h3>{quiz.title}</h3>
              <p className="lesson-quiz-panel__subcopy">
                {lessonTitle || "Hoàn thành từng câu để kiểm tra mức độ hiểu bài hiện tại của bạn."}
              </p>
            </div>
            <div className="lesson-quiz-panel__question-count">
              <strong>Câu {currentQuestionIndex + 1}</strong>
              <span>trên {quiz.questions.length}</span>
            </div>
          </div>

          <div className="lesson-quiz-panel__progress-block">
            <div aria-hidden="true" className="lesson-quiz-panel__progress-track">
              <span className="lesson-quiz-panel__progress-value" style={{ width: `${progressPercent}%` }} />
            </div>
            <div className="lesson-quiz-panel__progress-meta">
              <span>{answeredCount}/{quiz.questions.length} câu đã chọn đáp án</span>
              <span>{progressPercent}% tiến độ</span>
            </div>
          </div>

          {currentQuestion ? (
            <fieldset className="lesson-quiz-panel__question" key={currentQuestion.questionId}>
              <legend className="lesson-quiz-panel__question-prompt">{currentQuestion.questionText}</legend>
              <div className="lesson-quiz-panel__options-grid">
                {currentQuestion.options.map((option, optionIndex) => {
                  const isSelected = currentSelection === option.optionId;
                  return (
                    <button
                      aria-label={option.optionText}
                      className={`lesson-quiz-panel__option${isSelected ? " lesson-quiz-panel__option--selected" : ""}`}
                      key={option.optionId}
                      onClick={() => setAnswers((current) => ({ ...current, [currentQuestion.questionId]: option.optionId }))}
                      type="button"
                    >
                      <span className="lesson-quiz-panel__option-index">{OPTION_LABELS[optionIndex] ?? optionIndex + 1}</span>
                      <span className="lesson-quiz-panel__option-copy">{option.optionText}</span>
                      {isSelected ? <span className="lesson-quiz-panel__option-check">✓</span> : null}
                    </button>
                  );
                })}
              </div>
            </fieldset>
          ) : null}

          <div className="lesson-quiz-panel__footer">
            <button
              className="lesson-quiz-panel__secondary-action"
              disabled={currentQuestionIndex === 0 || isLoading}
              onClick={() => setCurrentQuestionIndex((current) => Math.max(0, current - 1))}
              type="button"
            >
              Câu trước
            </button>

            {isLastQuestion ? (
              <button
                className="lesson-quiz-panel__primary-action"
                disabled={isLoading || quiz.questions.some((question) => !answers[question.questionId])}
                onClick={handleSubmit}
                type="button"
              >
                {isLoading ? "Đang nộp bài..." : "Nộp bài"}
              </button>
            ) : (
              <button
                className="lesson-quiz-panel__primary-action"
                disabled={!currentSelection || isLoading}
                onClick={() => setCurrentQuestionIndex((current) => Math.min(quiz.questions.length - 1, current + 1))}
                type="button"
              >
                Câu tiếp theo
              </button>
            )}
          </div>
        </>
      )}
    </div>
  );
}
