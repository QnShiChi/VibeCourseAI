export default function FinalQuizCard({ courseId, quizId, status, questionCount }) {
  return (
    <div className="final-quiz-card" data-course-id={courseId} data-quiz-id={quizId}>
      <p className="final-quiz-card__eyebrow">Danh gia tong ket</p>
      <h3>Quiz tong ket khoa hoc</h3>
      <p>{questionCount} cau hoi</p>
      {status === "Ready" ? (
        <button type="button">Lam quiz tong ket</button>
      ) : (
        <p>Quiz dang duoc chuan bi.</p>
      )}
    </div>
  );
}
