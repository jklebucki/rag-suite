# RAG Suite

![.NET 8](https://img.shields.io/badge/.NET-8-blueviolet?style=for-the-badge&logo=dotnet)
![React](https://img.shields.io/badge/React-18-blue?style=for-the-badge&logo=react)
![TypeScript](https://img.shields.io/badge/TypeScript-5-blue?style=for-the-badge&logo=typescript)
![Semantic Kernel](https://img.shields.io/badge/Semantic-Kernel-lightgrey?style=for-the-badge&logo=microsoft)
![Elasticsearch](https://img.shields.io/badge/Elasticsearch-orange?style=for-the-badge&logo=elasticsearch)
![BGE-M3 Embeddings](https://img.shields.io/badge/BGE--M3-1024D-green?style=for-the-badge)
![Docker](https://img.shields.io/badge/Docker-blue?style=for-the-badge&logo=docker)

## Cel projektu

RAG Suite to monorepo .NET 8, którego celem jest implementacja systemu RAG (Retrieval-Augmented Generation) z wykorzystaniem Semantic Kernel oraz wektorów osadzonych (embeddings) BGE-M3 (1024D), przechowywanych i wyszukiwanych za pomocą Elasticsearch.

## Struktura katalogów

| Folder | Zawartość i przeznaczenie |
|--------|---------------------------|
| `src/RAG.Web.UI` | Nowoczesny frontend React TypeScript z interfejsem chat i dashboard |
| `src/RAG.Orchestrator.Api` | Główne API — Minimal API .NET, orkiestruje agenty i zapytania RAG |
| `src/RAG.Collector` | Serwis zbierania i przetwarzania dokumentów do ingestii różnych typów treści |
| `src/RAG.Shared` | Wspólne biblioteki, typy DTO, modele, helpery |
| `src/RAG.Plugins/…` | Pluginy-agent: `OracleSqlPlugin`, `IfsSopPlugin`, `BizProcessPlugin` |
| `src/RAG.Connectors/…` | Klienci/integracje i adaptery wektorowe: `Elastic`, `Oracle`, `Files` |
| `src/RAG.Security` | Autoryzacja, polityki, JWT/OIDC, dostęp do korpusów |
| `src/RAG.Telemetry` | Logowanie, metryki (Serilog + OpenTelemetry) |
| `src/RAG.Tests` | Testy jednostkowe i integracyjne (xUnit) |
| `deploy/elastic/mappings` | Mapowania indeksów Elasticsearch (768D/1024D) |
| `deploy/nginx` | Konfiguracja reverse proxy (NGINX) |
| `deploy/systemd` | Pliki serwisów systemd dla API i Workerów |
| `docs/` | Dokumentacja architektury, prompty, runbooks |
| `scripts/` | Skrypty pomocnicze: seed, migracje, testy, CI/CD itp. |

---

## Kluczowe komponenty

### 🌐 RAG.Web.UI - Aplikacja frontendowa

Nowoczesny frontend React TypeScript oferujący:

* **🚀 Nowoczesny stack**: React 18 + TypeScript + Vite + Tailwind CSS
* **💬 Interaktywny chat**: Interfejs chat z obsługą RAG i wielojęzyczności
* **🔍 Zaawansowane wyszukiwanie**: Wyszukiwanie full-text i semantyczne z filtrami
* **📊 Dashboard**: Metryki systemu, analityka i monitoring użycia
* **🔌 Zarządzanie pluginami**: Monitorowanie i zarządzanie pluginami RAG
* **👤 Autoryzacja użytkowników**: Logowanie JWT z dostępem opartym na rolach
* **📱 Responsywny design**: Bezproblemowa praca na desktop i mobile

### 🛡️ RAG.Security - Autoryzacja i uwierzytelnienie

Kompletna infrastruktura bezpieczeństwa z:

* **🔐 Autoryzacja JWT**: Bezpieczne uwierzytelnienie oparte na tokenach
* **👥 Zarządzanie użytkownikami**: Rejestracja, logowanie, zarządzanie profilem
* **🎭 Dostęp oparty na rolach**: Role User, PowerUser, Admin
* **📊 Baza SQLite**: Lokalne przechowywanie użytkowników i ról
* **🔄 Odświeżanie tokenów**: Bezpieczny mechanizm odnowy tokenów

### 🤖 RAG.Orchestrator.Api - Główny backend

Główne API orkiestrujące system RAG:

* **🧠 Integracja Semantic Kernel**: Generowanie odpowiedzi AI
* **💬 Sesje chat per użytkownik**: Izolowane sesje chat dla każdego użytkownika
* **🌍 Obsługa wielojęzyczności**: Auto-detekcja i tłumaczenie
* **🔍 Wyszukiwanie wektorowe**: Embeddingi BGE-M3 z Elasticsearch
* **📊 Analityka**: Śledzenie użycia i monitoring wydajności

---

## Szybki start

1. **Uruchom usługi backendowe**:
   ```bash
   cd deploy && docker-compose up -d
   ```

2. **Uruchom API**:
   ```bash
   cd src/RAG.Orchestrator.Api && dotnet run
   ```

3. **Uruchom frontend**:
   ```bash
   cd src/RAG.Web.UI && npm install && npm run dev
   ```

4. **Dostęp do aplikacji**:
   - Frontend: http://localhost:3000
   - API: http://localhost:7107
   - Domyślne dane logowania administratora: `admin@example.com` / `AdminPassword123!`

---

## Przydatne źródła:

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
