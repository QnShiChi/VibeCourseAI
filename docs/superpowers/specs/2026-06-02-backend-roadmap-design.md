# Backend Roadmap Design

## Goal

Create a root-level document `roadmap-code-backend.md` that helps a student read and understand the backend codebase of this ASP.NET Core project in a structured order, starting from platform foundations and moving toward business features.

## Audience

- Primary reader: a student learning C# ASP.NET Core
- Use case: code reading and oral/report presentation with a lecturer
- Scope: backend only

## Required Output

The deliverable is a single file at the repository root:

- `roadmap-code-backend.md`

The document is not a code explanation. It is a reading roadmap that tells the reader:

- which files to read first,
- why that order matters,
- which files relate to each other,
- which backend features map to which files.

## Structure Decision

The roadmap will be organized in this order:

1. **System map**
   - quick overview of backend folders and their roles
2. **Reading order from foundation to business logic**
   - startup/config
   - app settings/options
   - database layer
   - domain models
   - repositories
   - services
   - controllers
   - background workers and queues
   - real-time hub
3. **Feature-based reading map**
   - auth and user management
   - dashboard/admin
   - categories/courses/modules/lessons
   - comments
   - quizzes
   - syllabuses
   - generation jobs and content pipeline
   - audio/video generation
   - voice tutor
4. **Cross-file relationship map**
   - which features flow through which controllers, services, repositories, models, and DTO areas
5. **Suggested reading tracks**
   - fast overview
   - enough for presentation/report
   - deep reading path

## Content Rules

The roadmap should:

- reference real backend files and folders from this repo,
- stay focused on reading order and code relationships,
- avoid explaining detailed implementation logic,
- avoid frontend and ai-worker except where backend depends on them,
- be written in Vietnamese because the user is studying and reporting in Vietnamese.

## Backend Scope

Included:

- `backend/CourseVideo.API/Program.cs`
- `Configuration/`
- `Data/`
- `Models/`
- `Repositories/`
- `Services/`
- `Controllers/`
- `DTOs/`
- `Hubs/`
- config files such as `appsettings*.json`

Mentioned only as dependency context when necessary:

- worker integrations configured through backend HTTP clients
- static storage wiring

Excluded:

- frontend code details
- ai-worker internal code details

## Risk Control

The main risk is producing a roadmap that is too shallow or too mixed between architecture and feature flow. The document should separate:

- the order for learning ASP.NET Core architecture,
- the order for understanding concrete business features.

## Success Criteria

The final `roadmap-code-backend.md` lets the reader:

- know exactly where to start,
- know what to read next,
- know which backend files belong to each major feature,
- know how backend layers connect without needing code explanations yet.
