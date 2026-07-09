# Modernizacja wyszukiwania RAG — przekazanie dla operatora

Dokument opisuje kroki runtime/infra potrzebne do aktywacji usprawnień jakości wyszukiwania, które
zostały zaimplementowane w kodzie. Wszystkie nowe ścieżki są **domyślnie wyłączone**, więc obecny system
działa bez zmian, dopóki nie wykonasz poniższych kroków. Plan kodu znajduje się w zatwierdzonym pliku
planu (`mocno-zastan-w-si-czy-eager-badger.md`).

## Co zmieniło się w kodzie (już scalone w drzewie roboczym)

* **Collector** indeksuje teraz analizowane pola `fileName` + `title`, podpole `content.folded` z
  foldingiem diakrytyków oraz analizator `rag_filename` (dzieli po `_ - .`, składa diakrytyki). Analizator
  treści jest konfigurowalny (`Collector:ContentAnalyzer`, domyślnie `standard`). Wejście embeddingu może
  nieść prefiks passage (`Collector:EmbeddingPassagePrefix`) oraz deterministyczny prepend kontekstu
  pliku/sekcji (`Collector:PrependFileContextToEmbedding`, domyślnie true).
* **Orchestrator** boostuje `fileName^4`, `title^3`, `content.folded^1.5` w zapytaniach BM25/hybrydowych
  (bezczynne na starym indeksie, bo tych pól tam nie ma), stosuje konfigurowalny prefiks zapytania
  (`Services:EmbeddingService:QueryPrefix`) oraz uruchamia opcjonalny etap rerankingu cross-encoder
  (`Services:RerankService`). `Services:Elasticsearch:AutoCreateIndices` jest teraz `false`, więc Collector
  jest jedynym właścicielem mappingu indeksu. Zapasowy mapping samego orchestratora
  (`IndexManagementService.CreateIndexAsync`, używany przez adminowy endpoint `/index`) został
  **poprawiony** — wcześniej deklarował pola, których Collector nigdy nie zapisuje (`contentVector`,
  `fileType`, `documentId`…), co po cichu zepsułoby kNN; teraz jest zgodny z mappingiem Collectora.

## Prawda gruntowa (zweryfikowana)

* Wdrożony model embeddingów: `intfloat/multilingual-e5-base` (768-wym., multilingual) — patrz
  `deploy/embedding-service/compose.yml`. Mapping `dims: 768` jest poprawny. Etykiety `EmbeddingModelName`
  w appsettings Collectora (`multilingual-e5-small` / `all-MiniLM-L6-v2`) to **nieaktualne etykiety**
  (używane tylko do logów); dla porządku popraw je na `intfloat/multilingual-e5-base`.
* **e5 wymaga prefiksów** `query:` / `passage:`. Obecnie nie są stosowane po żadnej stronie, więc indeks
  i zapytanie są przynajmniej *spójne* dziś. Włączenie prefiksów trzeba zrobić po **obu** stronach naraz
  **oraz** wykonać reindex — nigdy tylko po jednej stronie.

## Krok 1 — Włącz reranker (największy pojedynczy zysk jakości, bez reindexu)

Reranker naprawia dokładnie ten błąd, który wystąpił (keyword-stuffed „Przyjęcie towaru" wyprzedzało
właściwe „Zamówienie zakupu"): przelicza trafność pobranych kandydatów względem rzeczywistej relacji
zapytanie–dokument.

Reranker jest serwowany przez **Infinity** (`michaelf34/infinity`), a nie TEI: build TEI CPU (`cpu-1.8`)
poprawnie ładuje embeddingi e5, ale jego backend candle **cicho kończy działanie** przy ładowaniu głowicy
klasyfikacji XLM-RoBERTa rerankera (`bge-reranker-v2-m3` dochodzi do „Starting Bert model on Cpu" i wychodzi
z kodem 0; `jina-reranker-v2` wywala się na parsowaniu config). Infinity serwuje `bge-reranker-v2-m3`
niezawodnie na CPU i wystawia Cohere-zgodne `/rerank`. `RerankService` obsługuje oba API przez
`Services:RerankService:Api` (`"tei"` lub `"cohere"`).

1. Uruchom kontener rerankera (Infinity, w `deploy/embedding-service/compose.yml`):

   ```bash
   docker compose -f deploy/embedding-service/compose.yml up -d rerank-service
   curl -s http://<host>:8582/health && echo OK
   # test funkcjonalny (oczekiwany wyższy relevance_score dla indeksu 1):
   curl -s http://<host>:8582/rerank -H 'Content-Type: application/json' \
     -d '{"model":"BAAI/bge-reranker-v2-m3","query":"jak zrobić zamówienie zakupu","documents":["instrukcja przyjęcia towaru na magazyn","instrukcja tworzenia zamówienia zakupu w IFS"]}'
   ```
2. Wskaż go orchestratorowi (appsettings / env):

   ```json
   "Services": {
     "RerankService": {
       "Url": "http://<host>:8582", "Enabled": true,
       "Api": "cohere", "Model": "BAAI/bge-reranker-v2-m3",
       "RetrieveTopN": 40, "TimeoutSeconds": 30
     }
   }
   ```
3. Zrestartuj orchestrator. Wyszukiwanie hybrydowe pobiera teraz top-40 i rerankuje w dół do limitu
   wołającego (czatowe `DocumentSearchLimit`). Działa na **obecnym** ES 8.11 i istniejącym indeksie — bez
   reindexu.

## Krok 2 — Reindex z nowym mappingiem (wyszukiwanie po nazwie pliku/tytule + polski + prefiksy)

Potrzebny, by aktywować boosting `fileName`/`title`, polski analizator i (opcjonalnie) prefiksy e5.


1. **Elasticsearch:** aktualizacja do 9.x (cel planu) i instalacja polskiego pluginu w obrazie ES:

   ```dockerfile
   FROM docker.elastic.co/elasticsearch/elasticsearch:9.x.y
   RUN bin/elasticsearch-plugin install --batch analysis-stempel
   ```

   (Jeśli nie możesz jeszcze zaktualizować, reindex działa też na 8.11 z `analysis-stempel` dla 8.11;
   tylko natywne funkcje `rrf`/`semantic_text` z Kroku 3 wymagają 9.x.)
2. **Konfiguracja Collectora** do reindexu (skieruj na świeży indeks, potem cutover przez alias):

   ```json
   "Collector": {
     "IndexName": "rag-chunks-v2",
     "ContentAnalyzer": "polish",
     "EmbeddingPassagePrefix": "passage: ",
     "ChunkSize": 2000, "ChunkOverlap": 200
   }
   ```

   Ustaw spójnie po stronie **orchestratora**: `Services:EmbeddingService:QueryPrefix = "query: "`.
   (Zostaw oba prefiksy puste, jeśli zdecydujesz się przejść na BGE-M3 — patrz Krok 4.)
3. Uruchom Collector, by zbudować `rag-chunks-v2`, zweryfikuj go (liczba chunków, niepuste `content`,
   wypełnione `fileName`/`title`, wymiar wektora = 768), następnie przełącz odczyty przez alias:

   ```bash
   curl -X POST "$ES/_aliases" -H 'Content-Type: application/json' -d '{
     "actions": [
       { "remove": { "index": "rag-chunks",    "alias": "rag-chunks-read" } },
       { "add":    { "index": "rag-chunks-v2", "alias": "rag-chunks-read" } }
     ]
   }'
   ```

   Ustaw orchestratorowe `Services:Elasticsearch:DefaultIndexName` na alias (`rag-chunks-read`), aby
   przyszłe cutovery nie wymagały redeployu.

## Krok 3 — (ES 9.x) Natywny retriever RRF

Po przejściu na ES 9.x zastąp fuzję sumą ważoną `script_score` w
`SearchQueryBuilder.BuildHybridQuery` natywnym retrieverem `rrf` (sub-retriever standard BM25 +
sub-retriever `knn`, `rank_constant: 60`, `rank_window_size: 50`). To zmiana w kodzie zależna od
aktualizacji; obecna ścieżka script_score (z nowymi boostami pól) pozostaje fallbackiem dla 8.11.

## Krok 4 — (Opcjonalnie) Aktualizacja modelu embeddingów do BGE-M3

Dla lepszej jakości multilingual serwuj `BAAI/bge-m3` (1024-wym.) przez TEI zamiast e5-base:

* ustaw w embedding-service `MODEL_ID=BAAI/bge-m3`, ustaw w Collectorze `Constants.DefaultEmbeddingDimensions`
  / `dims` w mappingu na **1024**, wyczyść prefiksy passage/query (BGE-M3 ich nie potrzebuje) i zrób reindex.

## Weryfikacja

* Zestaw zapytań (PL), w tym trudny przypadek: „jak zrobić zamówienie zakupu" → oczekiwany
  `Zamowienie_zakupu_instrukcja_RAG.docx` w top-1/top-3; „jak przyjąć towar" → `Przyjecie_towaru...`.
* Test dymny API: login `admin@citronex.pl`, czat z `useDocumentSearch:true`, sprawdź `documentsUsed`
  i jakość odpowiedzi; obserwuj w logach orchestratora linię „Reranked N candidates to top K".
* Testy jednostkowe: `dotnet test src/RAG.Tests` (patrz `RerankServiceTests`, `SearchQueryBuilderTests`).


