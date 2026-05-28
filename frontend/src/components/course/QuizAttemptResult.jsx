export default function QuizAttemptResult({ result, questions }) {
  return (
    <div className="quiz-result">
      <p className="quiz-result__headline">Diem: {result.score}</p>
      <p className="quiz-result__meta">
        Dung {result.correctCount}/{result.totalQuestions} cau
      </p>
      <div className="quiz-result__answers">
        {questions.map((question) => {
          const answer = result.answers.find((item) => item.questionId === question.questionId);
          const correctOption = question.options.find((option) => option.optionId === answer?.correctOptionId);

          return (
            <article className="quiz-result__answer" key={question.questionId}>
              <strong>{question.questionText}</strong>
              <p>{answer?.isCorrect ? "Ban tra loi dung." : "Ban tra loi chua dung."}</p>
              <p>Dap an dung: {correctOption?.optionText}</p>
              <p>{answer?.explanation}</p>
            </article>
          );
        })}
      </div>
    </div>
  );
}
