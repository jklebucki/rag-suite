# RAG Suite

![.NET 8](https://img.shields.io/badge/.NET-8-blueviolet?style=for-the-badge&logo=dotnet)
![Semantic Kernel](https://img.shields.io/badge/Semantic-Kernel-lightgrey?style=for-the-badge&logo=microsoft)
![Elasticsearch](https://img.shields.io/badge/Elasticsearch-orange?style=for-the-badge&logo=elasticsearch)
![BGE-M3 Embeddings](https://img.shields.io/badge/BGE--M3-1024D-green?style=for-the-badge)
![Docker](https://img.shields.io/badge/Docker-blue?style=for-the-badge&logo=docker)

## Cel projektu

RAG Suite to monorepo .NET 8, którego celem jest implementacja systemu RAG (Retrieval-Augmented Generation) z wykorzystaniem Semantic Kernel oraz wektorów osadzonych (embeddings) BGE-M3 (1024D), przechowywanych i wyszukiwanych za pomocą Elasticsearch.

## Struktura katalogów

| Folder | Zawartość i przeznaczenie |
|--------|---------------------------|
| `src/RAG.Orchestrator.Api` | Główne API — Minimal API .NET, orkiestruje agenty i zapytania RAG |
| `src/RAG.Ingestion.Worker` | Worker do ekstrakcji metadata z Oracle i dokumentów SOP/BPMN |
| `src/RAG.Shared` | Wspólne biblioteki, typy DTO, modele, helpery |
| `src/RAG.Plugins/…` | Pluginy-agent: `OracleSqlPlugin`, `IfsSopPlugin`, `BizProcessPlugin` |
| `src/RAG.VectorStores` | Abstrakcje oraz adaptery dla wektorowego store (np. Elasticsearch) |
| `src/RAG.Connectors/…` | Klienci/integracje: `Elastic`, `Oracle`, `Files` |
| `src/RAG.Security` | Autoryzacja, polityki, JWT/OIDC, dostęp do korpusów |
| `src/RAG.Telemetry` | Logowanie, metryki (Serilog + OpenTelemetry) |
| `src/RAG.Tests` | Testy jednostkowe i integracyjne (xUnit) |
| `deploy/elastic/mappings` | Mapowania indeksów Elasticsearch (768D/1024D) |
| `deploy/nginx` | Konfiguracja reverse proxy (NGINX) |
| `deploy/systemd` | Pliki serwisów systemd dla API i Workerów |
| `docs/` | Dokumentacja architektury, prompty, runbooks |
| `scripts/` | Skrypty pomocnicze: seed, migracje, testy, CI/CD itp. |

---
