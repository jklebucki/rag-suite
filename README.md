# RAG Suite

 ![.NET 8](https://img.shields.io/badge/.NET-8-blueviolet?style=for-the-badge&logo=dotnet)
 ![React](https://img.shields.io/badge/React-18-blue?style=for-the-badge&logo=react)
 ![TypeScript](https://img.shields.io/badge/TypeScript-5-blue?style=for-the-badge&logo=typescript)
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
| `src/RAG.Orchestrator.Api` | Main API (Minimal API .NET) orchestrating agents and RAG workflows |
| `src/RAG.Collector` | Document collection and processing service for ingesting various content types |
| `src/RAG.Shared` | Shared libraries: DTOs, models, utilities |
| `src/RAG.Plugins/…` | Agent plugins: `OracleSqlPlugin`, `IfsSopPlugin`, `BizProcessPlugin` |
| `src/RAG.Connectors/…` | Integration clients and vector store adapters: `Elastic`, `Oracle`, `Files` |
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

* **🚀 Modern Stack**: React 18 + TypeScript + Vite + Tailwind CSS
* **💬 Interactive Chat**: RAG-powered chat interface with multilingual support
* **🔍 Advanced Search**: Full-text and semantic search with filters
* **📊 Dashboard**: System metrics, analytics, and usage monitoring
* **🔌 Plugin Management**: Monitor and manage RAG plugins
* **🧠 Knowledge Exchange Forum**: Authenticated discussions with attachments, unread badges, and email notifications
* **👤 User Authentication**: JWT-based login with role-based access
* **📱 Responsive Design**: Works seamlessly on desktop and mobile

### 🛡️ RAG.Security - Authentication & Authorization

Complete security infrastructure with:

* **🔐 JWT Authentication**: Secure token-based authentication
* **👥 User Management**: Registration, login, profile management
* **🎭 Role-Based Access**: User, PowerUser, Admin roles
* **📊 SQLite Database**: Local user and role storage
* **🔄 Token Refresh**: Secure token renewal mechanism

### 🤖 RAG.Orchestrator.Api - Main Backend

Core API orchestrating the RAG system:

* **🧠 Semantic Kernel Integration**: AI-powered response generation
* **💬 Per-User Chat Sessions**: Isolated chat sessions for each user
* **🌍 Multilingual Support**: Auto-detection and translation
* **🔍 Vector Search**: BGE-M3 embeddings with Elasticsearch
* **📊 Analytics**: Usage tracking and performance monitoring


---

## Quick Start


1. **Start the backend services**:

   ```bash
   cd deploy && docker-compose up -d
   ```
2. **Run the API**:

   ```bash
   cd src/RAG.Orchestrator.Api && dotnet run
   ```
3. **Start the frontend**:

   ```bash
   cd src/RAG.Web.UI && npm install && npm run dev
   ```
4. **Access the application**:
   * Frontend: http://localhost:3000
   * API: http://localhost:7107
   * Default admin credentials: `admin@example.com` / `AdminPassword123!`


---


