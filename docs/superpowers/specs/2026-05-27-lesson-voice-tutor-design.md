# Lesson Voice Tutor Design

Date: 2026-05-27

## Goal

Bo sung tro giang giong noi theo thoi gian thuc vao trang hoc lesson. Khi user dang xem video bai hoc, ho co the bam nut micro de hoi bang giong noi ngay trong lesson hien tai. He thong se pause video, nghe cau hoi, sinh cau tra loi bang dung giong cua bai giang, hoi lai xem user con thac mac hay muon tiep tuc hoc, sau do hoac tiep tuc hoi dap hoac resume video tu dung thoi diem da pause.

## Scope

Trong pham vi thay doi:

- them workflow hoi dap bang giong noi ngay trong lesson video
- pause va resume video quanh moi voice turn
- giu context hoi dap trong pham vi lesson session hien tai
- tao audio tra loi bang cung `NarrationVoiceKey` voi bai giang
- dung backend `ASP.NET Core` lam trung tam dieu phoi nghiep vu, state, auth, audit, va streaming event
- stream text/audio response gan realtime theo segment ve frontend
- luu session, turn, va message history co ban de phuc vu follow-up, debug, va audit

Ngoai pham vi thay doi:

- khong ho tro always-listening hoac wake word trong V1
- khong ho tro hoi dap xuyen nhieu lesson hoac luu memory dai han xuyen khoa hoc
- khong duyet web live trong luc user hoi
- khong bien voice tutor thanh tro ly chat tu do ngoai ngu canh hoc tap
- khong thay doi pipeline tao audio/video bai giang hien co ngoai viec tai su dung voice profile

## Product Decisions

Nhung quyet dinh da duoc chot cho V1:

- nguon tra loi: uu tien lesson hien tai, mo rong course hien tai, sau cung moi dung kien thuc nen ben ngoai cua model
- kieu kich hoat: push-to-ask, user bam nut micro de bat dau mot luot ghi am
- kieu tra loi: voice-first, backend sinh audio tra loi bang dung giong lesson
- sau khi tra loi xong: frontend hien `Hoi tiep` va `Tiep tuc hoc`
- memory hoi thoai: nho lich su hoi dap trong pham vi lesson session hien tai cho den khi user roi lesson hoac reload trang
- kieu phan hoi: text/audio streaming theo segment gan realtime thong qua `SignalR`

## Existing Context

Codebase hien tai da co:

- backend `ASP.NET Core Web API` voi pattern `Controller -> Service -> Repository`
- frontend co trang hoc `frontend/src/pages/CourseLearnPage.jsx`
- lesson payload da co video, content, module/course context
- pipeline audio/video da ton tai trong backend va co khai niem lesson narration/audio
- he thong comment theo lesson da co, nhung voice tutor la luong tuong tac rieng

Chuc nang moi nen la mot subsystem rieng trong backend de tranh lam ban logic lesson/video hien tai.

## Recommended Approach

Chon kien truc `ASP.NET Core + SignalR + external STT/LLM/TTS providers under backend orchestration`.

Ly do chon:

- backend van nam quyen dieu phoi toan bo nghiep vu va auth
- phu hop voi muc tieu "100% bang ASP.NET Core" o cap ung dung va orchestration
- giu duoc voice consistency bang cach tai su dung `NarrationVoiceKey` cua lesson
- de mo rong va thay provider sau nay thong qua interface service
- phu hop voi codebase hien co hon viec dua logic voice sang browser hoac tu host toan bo model trong V1

Khong chon:

- always-listening V1, vi phuc tap ve echo cancellation, privacy, false trigger, va latency
- browser TTS, vi khong dam bao trung giong bai giang
- self-host full STT/TTS/LLM V1, vi tang rui ro ha tang va van hanh

## User Experience Flow

### Primary Flow

1. User dang xem video lesson.
2. User bam `Hoi bang giong noi`.
3. Frontend pause video va luu `currentTime`.
4. Frontend mo mot luot ghi am.
5. User ket thuc ghi am.
6. Audio duoc gui len backend.
7. Backend transcription audio thanh text.
8. Backend dung context tu lesson, course, va lich su hoi dap trong lesson session.
9. Backend goi LLM de tao cau tra loi.
10. Backend chia answer thanh cac segment TTS ngan.
11. Backend sinh audio bang dung `NarrationVoiceKey` cua lesson.
12. Backend stream text segment va audio segment ve frontend qua `SignalR`.
13. Frontend phat audio ngay khi nhan du segment.
14. Sau khi phat xong, frontend hien `Hoi tiep` va `Tiep tuc hoc`.
15. Neu user chon `Hoi tiep`, mot luot hoi moi bat dau trong cung session.
16. Neu user chon `Tiep tuc hoc`, video resume tu timestamp da luu.

### State Boundaries

Video player chi can them hai trang thai nghiep vu:

- `playing`
- `pausedByTutor`

Voice tutor panel co cac state UI:

- `idle`
- `recording`
- `uploading`
- `thinking`
- `speaking`
- `awaitingDecision`
- `error`

Khong de tutor chen vao state machine chi tiet cua player ngoai pause/resume checkpoint.

## Architecture

### Backend Components

#### `LessonVoiceTutorHub`

- dung `SignalR` de giu ket noi realtime
- nhan event tu client cho voice turn
- day event state, text segment, audio segment, error, va completion ve client

#### `LessonVoiceTutorController`

- cung cap REST endpoints phu tro tao session, lay session hien tai, lay lich su, dong session
- khong dat media streaming chinh qua REST trong V1

#### `ILessonVoiceTutorService`

- dieu phoi mot voice turn end-to-end
- goi transcription, context building, answer generation, TTS, luu state, va phat event hub

#### `ILessonContextBuilder`

- gom du lieu tu lesson/module/course:
  - `teachingScript`
  - `slideOutline`
  - `voiceoverPlan`
  - lesson title/description
  - transcript neu co
  - playback time neu can
- tom gon context de kiem soat token

#### `ITranscriptionService`

- nhan audio user
- tra `transcriptionText` va `confidence`

#### `ILessonTutorAnswerService`

- goi LLM de tao answer text
- gan `SourceType` cho answer:
  - `Lesson`
  - `Course`
  - `ExternalKnowledge`
  - `Mixed`

#### `ILessonTutorSpeechService`

- resolve dung `NarrationVoiceKey` cua lesson
- chia answer thanh cac segment TTS phu hop
- sinh audio tung segment de stream gan realtime

#### `ILessonVoiceSessionRepository`

- luu session, turn, va message history
- ho tro noi lai session lesson hien tai cua user

### Dependency Boundaries

Subsystem nay phai tach khoi `CourseService` va `LessonService` hien co. Cac service hien tai chi duoc doc du lieu lesson/course de xay context; khong dua voice tutor logic tro lai cac service domain khac.

## Realtime Protocol

### SignalR Hub Route

- `/hubs/lesson-voice-tutor`

### Client-to-Server Events

- `StartTurn(sessionId, lessonId, playbackTimeSeconds)`
- `UploadAudioChunk(turnId, chunkIndex, base64Audio)`
- `CompleteTurnAudio(turnId)`
- `RequestFollowUp(sessionId, playbackTimeSeconds)`
- `ResumeVideo(sessionId)`
- `CancelTurn(turnId)`

### Server-to-Client Events

- `VoiceTurnAccepted`
- `TranscriptionStarted`
- `TranscriptionCompleted`
- `AnswerGenerationStarted`
- `AnswerTextSegment`
- `AnswerAudioSegment`
- `AnswerCompleted`
- `AwaitingFollowUpDecision`
- `LessonResumeApproved`
- `VoiceTurnFailed`

Event contract nen explicit, khong generic hoa thanh mot event bus mo ho.

## Data Model

### New Entity: `LessonVoiceSession`

De xuat fields:

- `Id`
- `LessonId`
- `CourseId`
- `UserId`
- `Status`
- `StartedAt`
- `LastActivityAt`
- `EndedAt`
- `LastPausedVideoTimeSeconds`
- `VoiceProfileKey`
- `ContextScope`
- `ConversationSummary`
- `CreatedAt`
- `UpdatedAt`

Vai tro:

- dai dien cho mot phien hoi dap trong pham vi lesson cua mot user
- giu memory cho cac follow-up question trong lesson session

### New Entity: `LessonVoiceTurn`

De xuat fields:

- `Id`
- `SessionId`
- `TurnNumber`
- `Status`
- `PlaybackPausedAtSeconds`
- `UserAudioUrl`
- `TranscriptionText`
- `TranscriptionConfidence`
- `AnswerText`
- `AnswerSourceSummary`
- `ErrorCode`
- `ErrorMessage`
- `StartedAt`
- `CompletedAt`

Vai tro:

- theo doi mot luot hoi dap tu luc user bat dau den luc ket thuc
- phuc vu retry, audit, va failure analysis

### New Entity: `LessonVoiceMessage`

De xuat fields:

- `Id`
- `SessionId`
- `TurnNumber`
- `Role`
- `ContentText`
- `ContentSourceType`
- `AudioUrl`
- `AudioDurationSeconds`
- `SequenceIndex`
- `CreatedAt`

Vai tro:

- luu chuoi message da duoc dua vao hoac tra ve trong session
- ho tro replay, debug, va hien thi lich su neu sau nay can

### Lesson Changes

Them cac field vao `Lesson`:

- `NarrationVoiceKey`
- `LessonTranscriptJson` hoac `TranscriptText`
- tuy chon `VoiceTutorEnabled`

`NarrationVoiceKey` la field quan trong de dam bao giua audio bai giang va audio tra loi co cung voice profile.

## Storage

Tach luu media voice tutor khoi audio lesson:

- `storage/voice-tutor/user-questions/...`
- `storage/voice-tutor/assistant-answers/...`

Khong tron audio hoi dap vao `storage/audio/lesson-*`, vi vong doi file, retention, va quyen truy cap khac nhau.

## REST API

### `POST /api/lessons/{lessonId}/voice-sessions`

- tao session moi hoac noi lai session active cua user trong lesson

### `GET /api/lessons/{lessonId}/voice-sessions/current`

- lay session hien tai neu ton tai

### `GET /api/voice-sessions/{sessionId}/messages`

- lay lich su hoi dap cua session

### `POST /api/voice-sessions/{sessionId}/close`

- dong session khi user roi lesson hoac reset ngu canh

## Conversation and Prompting

### Context Priority

Thu tu uu tien answer:

1. lesson hien tai
2. course hien tai
3. external knowledge cua model

Neu can dung tri thuc mo rong ben ngoai, answer nen noi ro do la phan giai thich bo sung.

### Prompt Structure

Prompt cho answer service nen co 4 phan:

1. tutor role
2. lesson context
3. conversation history trong lesson session
4. response policy

### Response Policy

- tra loi bang tieng Viet
- uu tien giai thich phuc vu viec hoc lesson hien tai
- neu lesson/course khong du thi moi mo rong bang kien thuc ngoai
- khong tra loi theo kieu chat tu do khong lien quan
- khong khang dinh manh khi khong chac
- ket thuc bang loi moi hoi tiep ngan

### Source Labeling

Moi answer nen duoc gan nhan noi bo:

- `Lesson`
- `Course`
- `ExternalKnowledge`
- `Mixed`

Frontend co the hien thi nhan nho nhu:

- `Tra loi theo noi dung bai hoc`
- `Co bo sung kien thuc mo rong`

## Answer Length and Segmentation

Voice UX khong phu hop voi answer qua dai. V1 nen:

- mac dinh gioi han answer voice khoang 20-45 giay
- neu dai hon thi chia 2-3 segment
- neu van qua dai thi tom tat bang voice va de user hoi tiep

## Error Handling

### Transcription Failure

- thong bao user he thong nghe khong ro
- cho phep ghi am lai hoac tiep tuc hoc
- video van giu pause cho den khi user chon hanh dong tiep

### LLM Failure

- turn chuyen sang `Failed`
- cho phep `Thu lai` hoac `Tiep tuc hoc`

### TTS Failure

- uu tien retry TTS mot lan
- neu that bai sau retry, frontend co the hien text tam thoi nhung V1 van uu tien voice-first

### SignalR Disconnect

- frontend hien mat ket noi tutor
- backend giu session active trong TTL ngan, de xuat 10-15 phut
- reconnect xong co the noi lai session

### User Leaves Mid-Turn

- frontend gui `CancelTurn`
- backend huy pipeline neu provider ho tro
- neu khong huy duoc, danh dau ket qua la da huy va bo qua khi no tra ve

## Performance and Operational Constraints

- gioi han 1 turn active cho moi `user + lesson`
- gioi han audio question toi da 20-30 giay
- gioi han so follow-up lien tiep trong mot session, de xuat 5-8 luot
- cache ngan han lesson context da parse
- nen co `ConversationSummary` de nen lich su cu
- chi gui 2-4 luot hoi dap gan nhat vao prompt raw

## Security

- chi user co quyen hoc lesson moi duoc mo voice tutor session
- `SignalR` dung cung auth token voi API
- audio voice tutor khong public truc tiep
- neu can phat lai audio da luu, dung endpoint co kiem quyen hoac signed URL ngan han
- xem xet retention ngan hon cho audio raw so voi text logs

## Testing Strategy

### Backend Unit Tests

- `LessonContextBuilder` xay dung dung lesson/course context
- `LessonVoiceTutorService` chuyen state dung trong success path
- retry/failure cho transcription, answer generation, va TTS
- `NarrationVoiceKey` resolution dung theo lesson

### Backend Integration Tests

- tao session dung `lessonId` va `userId`
- chan user khong co quyen hoc lesson
- luong hub co ban: start turn -> complete -> awaiting decision
- chi resume video sau khi turn da ket thuc hoac da huy

### Frontend Tests

- bam micro thi video pause va panel tutor mo
- nhan `AnswerAudioSegment` thi audio queue phat dung thu tu
- bam `Tiep tuc hoc` thi video resume dung timestamp
- bam `Hoi tiep` thi quay lai flow recording
- co loi van thoat duoc de quay lai hoc

## Acceptance Criteria

- user hoi bang giong noi duoc trong lesson dang phat
- video pause dung luc va resume dung luc
- AI tra loi bang dung giong lesson
- follow-up hoat dong trong cung lesson session
- backend luu duoc session, turn, va message history co ban
- luong fail khong lam ket trang hoc

## Risks and Trade-Offs

- streaming audio segment tang do phuc tap hon tra file audio hoan chinh, nhung can thiet de dat muc responsiveness mong muon
- cho phep external knowledge giup answer huu ich hon, nhung tang nhu cau guardrail va source labeling
- reuse voice profile cua lesson giu trai nghiem dong nhat, nhung buoc TTS phan hoi phai phu thuoc chat vao du lieu voice metadata cua lesson

## Recommended Implementation Sequence

1. them entities, migration, repository, va basic REST session endpoints
2. them `SignalR` hub va auth flow
3. them transcription, answer generation, va TTS abstraction services
4. them end-to-end service orchestration cho voice turn
5. cap nhat `CourseLearnPage` voi tutor panel, record flow, va player pause/resume integration
6. them tests cho backend va frontend

## Summary

V1 cua lesson voice tutor nen duoc xay nhu mot subsystem realtime trong `ASP.NET Core`, dung `SignalR` de dieu phoi voice turn, giu nguc canh hoi dap trong lesson session, va sinh audio tra loi bang dung voice profile cua bai giang. Thiet ke nay dat duoc trai nghiem "hoi ngay trong luc hoc" ma van kiem soat duoc auth, logging, consistency, va kha nang mo rong sau nay.
