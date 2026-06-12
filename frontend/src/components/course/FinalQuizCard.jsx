export default function FinalQuizCard({ courseId, quizId, status, questionCount, onStart }) {
  return (
    <div className="final-quiz-card" data-course-id={courseId} data-quiz-id={quizId}>
      <p className="final-quiz-card__eyebrow">Danh gia tong ket</p>
      <h3>Quiz tong ket khoa hoc</h3>
      <p>{questionCount} cau hoi</p>
      {status === "Ready" ? (
        <button onClick={onStart} type="button">Làm quiz tổng kết</button>
      ) : (
        <p>Quiz dang duoc chuan bi.</p>
      )}
    </div>
  );
}
