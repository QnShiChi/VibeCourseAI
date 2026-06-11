const WEB_ACTIVITY_STORAGE_KEY = "vibe_course_ai_web_activity";

function getDayKey(date = new Date()) {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");
  return `${year}-${month}-${day}`;
}

function getDayLabel(date) {
  return date.toLocaleDateString("vi-VN", { weekday: "short" }).replace(".", "").toUpperCase();
}

function getMonthLabel(date) {
  return `T${date.getMonth() + 1}`;
}

function loadActivityStore() {
  try {
    const rawValue = window.localStorage.getItem(WEB_ACTIVITY_STORAGE_KEY);
    if (!rawValue) {
      return {};
    }

    const parsed = JSON.parse(rawValue);
    return parsed && typeof parsed === "object" ? parsed : {};
  } catch {
    return {};
  }
}

function saveActivityStore(store) {
  window.localStorage.setItem(WEB_ACTIVITY_STORAGE_KEY, JSON.stringify(store));
}

function trimActivityStore(store, maxDays = 365) {
  const keys = Object.keys(store).sort();
  if (keys.length <= maxDays) {
    return store;
  }

  const removableKeys = keys.slice(0, keys.length - maxDays);
  const nextStore = { ...store };
  removableKeys.forEach((key) => {
    delete nextStore[key];
  });
  return nextStore;
}

export function recordWebActivity(seconds, now = new Date()) {
  if (!Number.isFinite(seconds) || seconds <= 0) {
    return;
  }

  const store = loadActivityStore();
  const dayKey = getDayKey(now);
  const nextStore = trimActivityStore({
    ...store,
    [dayKey]: Math.round((store[dayKey] || 0) + seconds)
  });

  saveActivityStore(nextStore);
}

export function readRecentWebActivity(days = 7, now = new Date()) {
  const store = loadActivityStore();
  const items = [];

  for (let index = days - 1; index >= 0; index -= 1) {
    const date = new Date(now);
    date.setHours(0, 0, 0, 0);
    date.setDate(date.getDate() - index);
    const dayKey = getDayKey(date);
    const seconds = Number(store[dayKey] || 0);

    items.push({
      dayKey,
      label: getDayLabel(date),
      seconds,
      minutes: Math.round(seconds / 60)
    });
  }

  return items;
}

export function readWebActivitySeries(range = "7D", now = new Date()) {
  const store = loadActivityStore();

  if (range === "30D") {
    const items = [];
    for (let index = 29; index >= 0; index -= 1) {
      const date = new Date(now);
      date.setHours(0, 0, 0, 0);
      date.setDate(date.getDate() - index);
      const dayKey = getDayKey(date);
      const seconds = Number(store[dayKey] || 0);

      items.push({
        dayKey,
        label: String(date.getDate()).padStart(2, "0"),
        seconds,
        minutes: Math.round(seconds / 60)
      });
    }

    return items;
  }

  if (range === "1Y") {
    const items = [];
    for (let index = 11; index >= 0; index -= 1) {
      const date = new Date(now.getFullYear(), now.getMonth() - index, 1);
      const year = date.getFullYear();
      const month = date.getMonth();
      let seconds = 0;

      Object.entries(store).forEach(([dayKey, value]) => {
        const [entryYear, entryMonth] = dayKey.split("-").map(Number);
        if (entryYear === year && entryMonth === month + 1) {
          seconds += Number(value || 0);
        }
      });

      items.push({
        dayKey: `${year}-${String(month + 1).padStart(2, "0")}`,
        label: getMonthLabel(date),
        seconds,
        minutes: Math.round(seconds / 60)
      });
    }

    return items;
  }

  return readRecentWebActivity(7, now);
}

export function formatActivityDuration(seconds = 0) {
  if (seconds <= 0) {
    return "0 phút";
  }

  const totalMinutes = Math.round(seconds / 60);
  if (totalMinutes < 60) {
    return `${totalMinutes} phút`;
  }

  const hours = Math.floor(totalMinutes / 60);
  const minutes = totalMinutes % 60;
  if (minutes === 0) {
    return `${hours} giờ`;
  }

  return `${hours} giờ ${minutes} phút`;
}
