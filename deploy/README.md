# Deployment with Docker Compose

This directory contains the Docker Compose setup for deploying core infrastructure of RAG Suite.

## Purpose

Place your `docker-compose.yml` file here where it's clearly separated from source code but adjacent to other deployment resources.

## Context

This directory already includes:

* `elastic/` — Elasticsearch-specific mappings and environment configs
* `nginx/` — NGINX reverse-proxy configuration
* `systemd/` — systemd service files for API and ingestion workers
* `mappings/` — mapping files for ES indices (768D/1024D embeddings)

By adding `docker-compose.yml` here, you maintain a clean separation:

* Source code in `/src`
* Deployment & infra orchestration in `/deploy`

## Quick Start

To start the full local stack, run from root:

```bash
cd deploy
docker compose up -d
```

This brings up:
Elasticsearch + Kibana
Embedding Service (e5-base — substitute to BGE-M3 when ready)
Make sure to adjust any environment variables or volumes before moving to production.

---



