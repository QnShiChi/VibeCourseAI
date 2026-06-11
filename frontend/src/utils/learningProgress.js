const LEARNING_PROGRESS_STORAGE_KEY = "vibe_course_ai_learning_progress";

function loadProgressStore() {
  try {
    const rawValue = window.localStorage.getItem(LEARNING_PROGRESS_STORAGE_KEY);
    if (!rawValue) {
      return {};
    }

    const parsed = JSON.parse(rawValue);
    return parsed && typeof parsed === "object" ? parsed : {};
  } catch {
    return {};
  }
}

function saveProgressStore(store) {
  window.localStorage.setItem(LEARNING_PROGRESS_STORAGE_KEY, JSON.stringify(store));
}

export function saveCurrentLearningProgress(progress) {
  if (!progress?.courseId) {
    return;
  }

  const store = loadProgressStore();
  saveProgressStore({
    ...store,
    [progress.courseId]: {
      ...progress,
      updatedAt: new Date().toISOString()
    }
  });
}

export function readCurrentLearningProgress() {
  const store = loadProgressStore();
  const items = Object.values(store).filter((item) => item && typeof item === "object");

  if (items.length === 0) {
    return null;
  }

  return items.sort((left, right) => new Date(right.updatedAt || 0).getTime() - new Date(left.updatedAt || 0).getTime())[0];
}
