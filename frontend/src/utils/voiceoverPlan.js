export function parseVoiceoverPlanJson(value) {
  if (!value?.trim()) {
    return null;
  }

  const parsed = JSON.parse(value);
  if (!parsed || Array.isArray(parsed) || typeof parsed !== "object") {
    throw new Error("Voiceover plan phải là một object.");
  }

  return {
    estimatedDurationMinutes: Number(
      parsed.estimatedDurationMinutes ?? parsed.EstimatedDurationMinutes ?? 0
    ),
    tone: String(parsed.tone ?? parsed.Tone ?? ""),
    pacing: String(parsed.pacing ?? parsed.Pacing ?? ""),
    targetAudience: String(parsed.targetAudience ?? parsed.TargetAudience ?? ""),
    pronunciationNotes: String(parsed.pronunciationNotes ?? parsed.PronunciationNotes ?? "")
  };
}

export function normalizeVoiceoverPlan(plan) {
  return {
    estimatedDurationMinutes: Number(plan.estimatedDurationMinutes),
    tone: String(plan.tone ?? "").trim(),
    pacing: String(plan.pacing ?? "").trim(),
    targetAudience: String(plan.targetAudience ?? "").trim(),
    pronunciationNotes: String(plan.pronunciationNotes ?? "").trim()
  };
}

export function validateVoiceoverPlan(plan) {
  const normalized = normalizeVoiceoverPlan(plan);

  if (!Number.isFinite(normalized.estimatedDurationMinutes) || normalized.estimatedDurationMinutes <= 0) {
    throw new Error("Thời lượng dự kiến phải lớn hơn 0.");
  }

  if (!normalized.tone) {
    throw new Error("Giọng điệu là bắt buộc.");
  }

  if (!normalized.pacing) {
    throw new Error("Nhịp đọc là bắt buộc.");
  }

  if (!normalized.targetAudience) {
    throw new Error("Đối tượng nghe là bắt buộc.");
  }

  if (!normalized.pronunciationNotes) {
    throw new Error("Lưu ý phát âm là bắt buộc.");
  }
}

export function serializeVoiceoverPlan(plan) {
  validateVoiceoverPlan(plan);
  return JSON.stringify(normalizeVoiceoverPlan(plan));
}
