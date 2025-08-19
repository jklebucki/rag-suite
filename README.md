# RAG Suite

[![.NET 8](https://img.shields.io/badge/.NET-8-blueviolet?style=for-the-badge&logo=dotnet)][dotnet]
[![Semantic Kernel](https://img.shields.io/badge/Semantic-Kernel-lightgrey?style=for-the-badge&logo=microsoft)][semantic-kernel]
[![Elastic](https://img.shields.io/badge/Elasticsearch-orange?style=for-the-badge&logo=elasticsearch)][elasticsearch]
[![BGE-M3 Embeddings](https://img.shields.io/badge/BGE--M3-1024D-green?style=for-the-badge)][bge-m3]
[![Docker](https://img.shields.io/badge/Docker-blue?style=for-the-badge&logo=docker)][docker]

---

## Project Goal

**RAG Suite** is a .NET 8 monorepo designed to implement a **Retrieval-Augmented Generation (RAG)** system using **Semantic Kernel** in conjunction with **BGE-M3 (1024D)** embeddings, stored and searched efficiently in **Elasticsearch**.

---

## Directory Structure & Purpose

| Folder                         | Description                                                                 |
|--------------------------------|-----------------------------------------------------------------------------|
| `src/RAG.Orchestrator.Api`     | Main API (Minimal API .NET) orchestrating agents and RAG workflows          |
| `src/RAG.Ingestion.Worker`     | Worker service for extracting metadata from Oracle and SOP/BPMN docs        |
| `src/RAG.Shared`               | Shared libraries: DTOs, models, utilities                                   |
| `src/RAG.Plugins/…`            | Agent plugins: `OracleSqlPlugin`, `IfsSopPlugin`, `BizProcessPlugin`       |
| `src/RAG.VectorStores`         | Abstractions and adapters for vector stores (e.g. Elasticsearch)            |
| `src/RAG.Connectors/…`         | Integration clients: `Elastic`, `Oracle`, `Files`                           |
| `src/RAG.Security`             | Authorization, policies, JWT/OIDC, corpus-level access control              |
| `src/RAG.Telemetry`            | Logging & metrics (Serilog + OpenTelemetry)                                |
| `src/RAG.Tests`                | Unit and integration tests (xUnit)                                         |
| `deploy/elastic/mappings`      | Elasticsearch index mappings (768D / 1024D)                                 |
| `deploy/nginx`                 | Reverse proxy (NGINX) configuration                                         |
| `deploy/systemd`               | systemd service files for APIs and workers                                  |
| `docs/`                        | Documentation: architecture notes, prompts repository, runbooks             |
| `scripts/`                     | Helper scripts: seed data, migrations, CI/CD tooling                        |

---


