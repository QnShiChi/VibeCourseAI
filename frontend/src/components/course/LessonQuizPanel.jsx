import { useState } from "react";
import QuizAttemptResult from "./QuizAttemptResult";

export default function LessonQuizPanel({ lessonId, quizId, initialStatus, onLoadQuiz, onStartAttempt, onSubmitAttempt }) {
  const [quiz, setQuiz] = useState(null);
  const [attemptId, setAttemptId] = useState("");
  const [answers, setAnswers] = useState({});
  const [result, setResult] = useState(null);
  const [isLoading, setIsLoading] = useState(false);
  const [errorMessage, setErrorMessage] = useState("");

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
    const payload = {
      answers: quiz.questions.map((question) => ({
        questionId: question.questionId,
        selectedOptionId: answers[question.questionId]
      }))
    };
    const submittedResult = await onSubmitAttempt(quiz.quizId, attemptId, payload);
    setResult(submittedResult);
  }

  if (!quizId && !initialStatus) {
    return null;
  }

  if (initialStatus && initialStatus !== "Ready") {
    return (
      <div className="lesson-quiz-panel">
        <div className="lesson-quiz-panel__header">
          <h3>Kiem tra nhanh sau bai hoc</h3>
        </div>
        <p>Quiz dang duoc chuan bi.</p>
      </div>
    );
  }

  return (
    <div className="lesson-quiz-panel">
      <div className="lesson-quiz-panel__header">
        <h3>Kiem tra nhanh sau bai hoc</h3>
      </div>
      {errorMessage ? <p>{errorMessage}</p> : null}
      {!quiz ? (
        <button disabled={isLoading} onClick={handleStart} type="button">
          Lam quiz
        </button>
      ) : (
        <>
          <h4>{quiz.title}</h4>
          {quiz.questions.map((question) => (
            <fieldset className="lesson-quiz-panel__question" key={question.questionId}>
              <legend>{question.questionText}</legend>
              {question.options.map((option) => (
                <label key={option.optionId}>
                  <input
                    aria-label={option.optionText}
                    checked={answers[question.questionId] === option.optionId}
                    name={question.questionId}
                    onChange={() => setAnswers((current) => ({ ...current, [question.questionId]: option.optionId }))}
                    type="radio"
                  />
                  <span>{option.optionText}</span>
                </label>
              ))}
            </fieldset>
          ))}
          <button disabled={quiz.questions.some((question) => !answers[question.questionId])} onClick={handleSubmit} type="button">
            Nop bai
          </button>
          {result ? <QuizAttemptResult questions={quiz.questions} result={result} /> : null}
        </>
      )}
    </div>
  );
}
