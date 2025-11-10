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
| `src/RAG.Orchestrator.Api` | Główne API — Minimal API .NET, orkiestruje agenty, moduły i zapytania RAG |
| `src/RAG.Collector` | Serwis zbierania i przetwarzania dokumentów do ingestii różnych typów treści |
| `src/RAG.Shared` | Wspólne biblioteki, typy DTO, modele, helpery |
| `src/RAG.Abstractions` | Wspólne kontrakty i interfejsy używane przez moduły backendowe (np. wyszukiwanie, konwersje) |
| `src/RAG.Plugins/…` | Pluginy-agent: `OracleSqlPlugin`, `IfsSopPlugin`, `BizProcessPlugin` |
| `src/RAG.Connectors/…` | Klienci/integracje i adaptery wektorowe: `Elastic`, `Oracle`, `Files` |
| `src/RAG.Forum` | Backend forum z wątkami, załącznikami, badge'ami i narzędziami administratorskimi |
| `src/RAG.AddressBook` | Mikroserwis książki adresowej z propozycjami zmian, importem CSV i workflow zależnym od ról |
| `src/RAG.CyberPanel` | Silnik quizów cyberbezpieczeństwa z pytaniami multimedialnymi i punktacją |
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
* **🧠 Forum wiedzy**: Uwierzytelnione dyskusje z załącznikami, badge'ami "nieprzeczytane" i konfigurowalnym odświeżaniem
* **⚙️ Panel ustawień forum**: Zarządzanie kategoriami (kolejność, archiwizacja), limitami załączników i domyślną subskrypcją odpowiedzi
* **🔔 Subskrypcje wątków**: Użytkownicy mogą zapisywać/wyrejestrowywać się z powiadomień, a odznaki są automatycznie potwierdzane
* **👤 Autoryzacja użytkowników**: Logowanie JWT z dostępem opartym na rolach
* **📱 Responsywny design**: Bezproblemowa praca na desktop i mobile

### 🛡️ RAG.Security - Autoryzacja i uwierzytelnienie

Kompletna infrastruktura bezpieczeństwa z:

* **🔐 Autoryzacja JWT**: Bezpieczne uwierzytelnienie oparte na tokenach
* **👥 Zarządzanie użytkownikami**: Rejestracja, logowanie, zarządzanie profilem
* **🎭 Dostęp oparty na rolach**: Role User, PowerUser, Admin
* **🐘 Baza PostgreSQL**: Wspólny magazyn użytkowników i ról z konwencją snake_case
* **🔄 Odświeżanie tokenów**: Bezpieczny mechanizm odnowy tokenów

### 🤖 RAG.Orchestrator.Api - Główny backend

Główne API orkiestrujące system RAG:

* **🧠 Integracja Semantic Kernel**: Generowanie odpowiedzi AI
* **💬 Sesje chat per użytkownik**: Izolowane sesje chat dla każdego użytkownika
* **🌍 Obsługa wielojęzyczności**: Auto-detekcja i tłumaczenie
* **🔍 Wyszukiwanie wektorowe**: Embeddingi BGE-M3 z Elasticsearch
* **📊 Analityka**: Śledzenie użycia i monitoring wydajności
* **🧩 Hosting modułów**: Startuje AddressBook, CyberPanel, Forum i Security z automatycznymi migracjami PostgreSQL
* **⚙️ Ustawienia globalne**: Centralne zarządzanie konfiguracją LLM, politykami forum i flagami funkcji

### 🧵 RAG.Forum - Backend forum wiedzy

* **🗂️ Wątki i posty**: Minimal API do listowania, szczegółów i odpowiedzi w ramach wątków
* **📎 Załączniki**: Bezpieczne uploady/pobieranie z limitami ilości i rozmiaru
* **🔔 Powiadomienia**: Subskrypcje wątków z preferencjami e-mail i odznaczaniem nieprzeczytanych odpowiedzi
* **📛 Panel administratora**: CRUD kategorii z walidacją slugów, kolejnością i archiwizacją

### 📘 RAG.AddressBook - Moduł książki adresowej

* **👥 Katalog kontaktów**: Operacje CRUD z audytem i autoryzacją zależną od roli
* **📥 Import CSV**: Hurtowy import z plików firmowych z wykrywaniem duplikatów
* **📝 Propozycje zmian**: Użytkownicy bez uprawnień admina zgłaszają zmiany, które zatwierdzają PowerUser/Admin
* **🔎 Wyszukiwanie i tagi**: Filtrowanie kontaktów plus tagowanie do segmentacji

### 🛡️ RAG.CyberPanel - Silnik quizów bezpieczeństwa

* **🧠 Tworzenie quizów**: Budowanie quizów wielopytaniowych z obrazami
* **📝 Ocena prób**: Liczenie punktów, historia prób i szczegółowe feedbacki
* **🏗️ Architektura Vertical Slice**: Walidacja FluentValidation i kompletna dokumentacja OpenAPI
* **🐘 PostgreSQL**: Wspólny connection string `SecurityDatabase` z automatycznymi migracjami

---

## Ustawienia forum i powiadomienia

- **Załączniki**: Włączanie/wyłączanie oraz limity `maxAttachmentCount` i `maxAttachmentSizeMb`
- **Powiadomienia e-mail**: Domyślne subskrypcje odpowiedzi dla nowych postów
- **Odświeżanie badge'y**: Konfiguracja częstotliwości (`badgeRefreshSeconds`) dla wskaźników nieprzeczytanych
- **Kategorie**: Panel admina pozwala ustawiać kolejność, archiwizować i pilnować unikalnych slugów

Ustawieniami zarządzają administratorzy w panelu Settings; wartości są zapisywane przez usługę ustawień globalnych w Orchestratorze.

---

## Szybki start

1. **Uruchom usługi backendowe**:
   ```bash
   cd deploy && docker-compose up -d
   ```
   > Aplikacja wymaga PostgreSQL dostępnego pod `ConnectionStrings:SecurityDatabase` (domyślnie `Host=localhost:5432;Database=rag-suite;Username=pg-dev;Password=pg-dev`). Uruchom lokalny serwer lub zaktualizuj `appsettings.Development.json`.

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
   - Domyślne dane logowania administratora: `admin@citronex.pl` / `Citro@123`

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
