export default function FinalQuizCard({ courseId, quizId, status, questionCount, onStart }) {
  return (
    <div className="final-quiz-card" data-course-id={courseId} data-quiz-id={quizId}>
      <p className="final-quiz-card__eyebrow">Đánh giá tổng kết</p>
      <h3>Quiz tổng kết khóa học</h3>
      <p>{questionCount} câu hỏi</p>
      {status === "Ready" ? (
        <button onClick={onStart} type="button">Làm quiz tổng kết</button>
      ) : (
        <p>Quiz đang được chuẩn bị.</p>
      )}
    </div>
  );
}
