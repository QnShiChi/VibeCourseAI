# Backend Roadmap Document Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Create a root-level `roadmap-code-backend.md` that guides a student through the backend codebase from ASP.NET Core foundations to business features.

**Architecture:** The deliverable is a documentation artifact, not a code change. It should map backend folders, files, and feature flows in two layers: first by architectural foundations, then by business capability groupings.

**Tech Stack:** Markdown, ASP.NET Core project structure, Entity Framework Core, SignalR, background workers

---

### Task 1: Inventory The Backend Structure

**Files:**
- Read: `backend/CourseVideo.API/Program.cs`
- Read: `backend/CourseVideo.API/appsettings.json`
- Read: `backend/CourseVideo.API/appsettings.Development.json`
- Read: `backend/CourseVideo.API/Data/AppDbContext.cs`
- Read: `backend/CourseVideo.API/Data/DbInitializer.cs`
- Read: `backend/CourseVideo.API/Controllers/*.cs`
- Read: `backend/CourseVideo.API/Repositories/**/*.cs`
- Read: `backend/CourseVideo.API/Services/**/*.cs`
- Read: `backend/CourseVideo.API/Hubs/LessonVoiceTutorHub.cs`

- [ ] **Step 1: Capture the startup and configuration entry points**
- [ ] **Step 2: Capture the database layer entry points**
- [ ] **Step 3: Capture the HTTP controller entry points**
- [ ] **Step 4: Capture service, repository, worker, and hub groupings**

### Task 2: Draft The Reading Roadmap

**Files:**
- Create: `roadmap-code-backend.md`

- [ ] **Step 1: Write the backend folder map**
- [ ] **Step 2: Write the reading order from foundation to business logic**
- [ ] **Step 3: Write the feature-based reading map**
- [ ] **Step 4: Write the cross-file relationship map**
- [ ] **Step 5: Write suggested reading tracks for fast, medium, and deep study**

### Task 3: Verify The Document

**Files:**
- Verify: `roadmap-code-backend.md`

- [ ] **Step 1: Check that all referenced files exist**
- [ ] **Step 2: Check that frontend and ai-worker details are excluded except dependency mentions**
- [ ] **Step 3: Check that the document focuses on reading order and file relationships, not code explanation**
