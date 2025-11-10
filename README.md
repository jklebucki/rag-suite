# RAG Suite

 ![.NET 8](https://img.shields.io/badge/.NET-8-blueviolet?style=for-the-badge&logo=dotnet)
 ![React](https://img.shields.io/badge/React-19-blue?style=for-the-badge&logo=react)
 ![TypeScript](https://img.shields.io/badge/TypeScript-5.6-blue?style=for-the-badge&logo=typescript)
 ![Semantic Kernel](https://img.shields.io/badge/Semantic-Kernel-lightgrey?style=for-the-badge&logo=microsoft)
 ![Elasticsearch](https://img.shields.io/badge/Elasticsearch-orange?style=for-the-badge&logo=elasticsearch)
 ![BGE-M3 Embeddings](https://img.shields.io/badge/BGE--M3-1024D-green?style=for-the-badge)
 ![Docker](https://img.shields.io/badge/Docker-blue?style=for-the-badge&logo=docker)

## Project Goal

**RAG Suite** is a .NET 8 monorepo designed to implement a **Retrieval-Augmented Generation (RAG)** system using **Semantic Kernel** in conjunction with **BGE-M3 (1024D)** embeddings, stored and searched efficiently in **Elasticsearch**.


---

## Directory Structure & Purpose

| Folder | Description |
|----|----|
| `src/RAG.Web.UI` | Modern React TypeScript frontend with chat, knowledge exchange forum, and dashboard |
| `src/RAG.Orchestrator.Api` | Main API (Minimal API .NET) orchestrating agents, RAG workflows, and feature modules |
| `src/RAG.Collector` | Document collection and processing service for ingesting various content types |
| `src/RAG.Shared` | Shared libraries: DTOs, models, utilities |
| `src/RAG.Abstractions` | Common contracts and interfaces shared across backend modules (search, conversions, etc.) |
| `src/RAG.Plugins/…` | Agent plugins: `OracleSqlPlugin`, `IfsSopPlugin`, `BizProcessPlugin` |
| `src/RAG.Connectors/…` | Integration clients and vector store adapters: `Elastic`, `Oracle`, `Files` |
| `src/RAG.Forum` | Forum backend with threads, attachments, unread badges, and admin tooling |
| `src/RAG.AddressBook` | Address book microservice with change proposals, CSV import, and role-aware workflows |
| `src/RAG.CyberPanel` | Cybersecurity quiz engine with media-rich questions and scoring |
| `src/RAG.Security` | Authorization, policies, JWT/OIDC, corpus-level access control |
| `src/RAG.Telemetry` | Logging & metrics (Serilog + OpenTelemetry) |
| `src/RAG.Tests` | Unit and integration tests (xUnit) |
| `deploy/elastic/mappings` | Elasticsearch index mappings (768D / 1024D) |
| `deploy/nginx` | Reverse proxy (NGINX) configuration |
| `deploy/systemd` | systemd service files for APIs and workers |
| `docs/` | Documentation: architecture notes, prompts repository, runbooks |
| `scripts/` | Helper scripts: seed data, migrations, CI/CD tooling |


---

## Key Components

### 🌐 RAG.Web.UI - Frontend Application

Modern React TypeScript frontend providing:

* **🚀 Modern Stack**: React 19 + TypeScript 5.6 + Vite 7 + Tailwind CSS 3.4 (Node ≥ 20.10)
* **💬 Interactive Chat**: RAG-powered chat interface with multilingual support
* **🔍 Advanced Search**: Full-text and semantic search with filters
* **📊 Dashboard**: System metrics, analytics, and usage monitoring
* **🔌 Plugin Management**: Monitor and manage RAG plugins
* **🧠 Knowledge Exchange Forum**: Authenticated discussions with attachments, unread badges, configurable refresh cadence, and email notifications
* **⚙️ Forum Administration**: Manage categories (ordering, archiving), attachment policies, and default subscription behaviour from the Settings panel
* **🔔 Thread Subscriptions**: Opt-in email alerts per thread with automatic unread badge acknowledgement
* **👤 User Authentication**: JWT-based login with role-based access
* **📱 Responsive Design**: Works seamlessly on desktop and mobile

### 🛡️ RAG.Security - Authentication & Authorization

Complete security infrastructure with:

* **🔐 JWT Authentication**: Secure token-based authentication
* **👥 User Management**: Registration, login, profile management
* **🎭 Role-Based Access**: User, PowerUser, Admin roles
* **🐘 PostgreSQL Identity Store**: Shared database for users and roles with snake_case schema
* **🔄 Token Refresh**: Secure token renewal mechanism

### 🤖 RAG.Orchestrator.Api - Main Backend

Core API orchestrating the RAG system:

* **🧠 Semantic Kernel 1.24 Integration**: AI-powered response generation
* **💬 Per-User Chat Sessions**: Isolated chat sessions for each user
* **🌍 Multilingual Support**: Auto-detection and translation
* **🔍 Vector Search**: BGE-M3 embeddings with Elasticsearch
* **📊 Analytics**: Usage tracking and performance monitoring
* **🧩 Feature Hosting**: Boots AddressBook, CyberPanel, Forum, and Security modules with automatic PostgreSQL migrations
* **⚙️ Global Settings**: Centralized configuration for LLMs, forum policies, and feature toggles

### 🧵 RAG.Forum - Knowledge Exchange Backend

* **🗂️ Thread & Post Management**: Minimal API endpoints for listing, viewing, and replying within forum threads
* **📎 Attachments**: Secure upload/download with configurable limits on count and size
* **🔔 Notifications**: Thread subscriptions with email preferences and unread reply badges acknowledged per user
* **📛 Admin Toolkit**: Category CRUD with ordering, slug validation, and archiving workflows

### 📘 RAG.AddressBook - Contacts Service

* **👥 Contact Directory**: CRUD operations with full audit trail and role-aware authorization
* **📥 CSV Importer**: Bulk ingest from enterprise address book exports with duplicate detection
* **📝 Change Proposals**: Regular users may submit change requests, reviewed by PowerUser/Admin roles
* **🔎 Search & Tags**: Flexible filtering plus tagging support for segmentation

### 🛡️ RAG.CyberPanel - Cybersecurity Quiz Engine

* **🧠 Quiz Authoring**: Create and publish multi-question cybersecurity quizzes with images
* **📝 Attempt Tracking**: Score submissions, track attempts, and return detailed feedback
* **🏗️ Vertical Slice Architecture**: Feature-based slices with FluentValidation and OpenAPI support
* **🐘 PostgreSQL-backed**: Uses the shared security database connection with automatic migrations

---

## Forum Settings & Notifications

- **Attachments**: Toggle availability and define `maxAttachmentCount` / `maxAttachmentSizeMb`
- **Email Alerts**: Control default opt-in for reply notifications when users post
- **Badge Refresh**: Adjust client polling cadence (`badgeRefreshSeconds`) for unread indicators
- **Categories**: Admin UI supports ordering, archiving, and validation for unique slugs

Admin users can adjust these options from the Settings panel; values are persisted via the orchestrator’s global settings service.

---

## Quick Start


1. **Start the backend services**:

   ```bash
   cd deploy && docker-compose up -d
   ```

   > Ensure PostgreSQL is running and matches the `SecurityDatabase` connection string (default `Host=localhost:5432;Database=rag-suite;Username=pg-dev;Password=pg-dev`). Update `appsettings.Development.json` if your environment differs.
   >
   > The compose stack launches Elasticsearch 8.11.3, Kibana 8.11.3, Hugging Face Text Embeddings Inference 1.8 (MiniLM-L6-v2), Text Generation Inference 2.4.0 (GPT-2), and the latest Ollama image.
2. **Run the API**:

   ```bash
   cd src/RAG.Orchestrator.Api && dotnet run
   ```
3. **Start the frontend**:

   ```bash
   cd src/RAG.Web.UI && npm install && npm run dev
   ```

   > Requires Node.js ≥ 20.10 and npm ≥ 10 (enforced via `package.json` engines).
4. **Access the application**:
   * Frontend: http://localhost:3000
   * API: http://localhost:7107
   * Default admin credentials: `admin@citronex.pl` / `Citro@123`


---


