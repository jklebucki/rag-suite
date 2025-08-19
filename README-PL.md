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

## Przydatne źródła (dla Ciebie)

- **[Vectors `dense_vector` docs & similarity options][1]**  
  Oficjalna dokumentacja Elasticsearch opisująca typ pola `dense_vector`, parametry takie jak `dims`, `index`, `similarity`, oraz użycie algorytmu HNSW do kNN.  
  Źródło: Elastic Documentation
- **[Cosine vs Dot Product similarity — efficiency note][2]**  
  Artykuł z Elastic Search Labs omawiający różne metryki podobieństwa wektorowego (m.in. L1, L2, cosine, dot product), z wyjaśnieniem korzyści z użycia `dot_product` po normalizacji wektorów.  
  Źródło: Elastic Search Labs  
- **[HNSW + tuning (`m`, `ef_construction`) and performance impact][3]**  
  Przewodnik Elastic Labs po konfiguracji wyszukiwania wektorowego w Elasticsearch — zawiera szczegóły dotyczące parametrów HNSW takich jak `m`, `ef_construction` i ich wpływ na wydajność i dokładność.  
  Źródło: Elastic Search Labs  
- **[Hybrid BM25 + vector (Convex Combination / RRF) — practical guide][4]**  
  Artykuł Elastic Labs opisujący hybrydowe wyszukiwanie łączące BM25 z wektorami, wykorzystujące metody takie jak Convex Combination i Reciprocal Rank Fusion (RRF).  
  Źródło: Elastic Search Labs  
- **[Dimension limits for indexed dense vectors (≤1024, ≤2048)][5]**  
  Informacja o limitach `dims`: do 1024 dla wektorów indeksowanych, do 2048 w wersji ES 8.10, i nawet 4096 od 8.11.  
  Źródło: Elastic Labs post (how to set up vector search)

---

[1]: https://www.elastic.co/docs/reference/elasticsearch/mapping-reference/dense-vector "Dokumentacja dense_vector + similarity"
[2]: https://www.elastic.co/search-labs/blog/vector-similarity-techniques-and-scoring "Porównanie metryk (cosine, dot product itd.)"
[3]: https://www.elastic.co/search-labs/blog/vector-search-set-up-elasticsearch "Tunele HNSW – m, ef_construction"
[4]: https://www.elastic.co/search-labs/blog/hybrid-search-elasticsearch "Hybrydowe wyszukiwanie BM25 + vector"
[5]: https://discuss.elastic.co/t/what-is-the-maximum-dimensionality-of-a-vector-field/342159 "Limity wymiarów dense_vector (1024 indexed, 2048 non-indexed)"
