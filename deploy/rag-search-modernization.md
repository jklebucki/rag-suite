# RAG search modernization — operator handoff

This document lists the runtime/infra steps needed to activate the search-quality improvements that
were implemented in code. All new code paths are **off by default**, so the current system keeps
working unchanged until you perform the steps below. The code plan lives in the approved plan file
(`mocno-zastan-w-si-czy-eager-badger.md`).

## What changed in code (already merged to the working tree)

- **Collector** now indexes analyzed `fileName` + `title` fields, a diacritic-folded `content.folded`
  sub-field, and a `rag_filename` analyzer (splits on `_ - .`, folds diacritics). Content analyzer is
  configurable (`Collector:ContentAnalyzer`, default `standard`). Embedding input can carry a passage
  prefix (`Collector:EmbeddingPassagePrefix`) and a deterministic file/section context prepend
  (`Collector:PrependFileContextToEmbedding`, default true).
- **Orchestrator** boosts `fileName^4`, `title^3`, `content.folded^1.5` in BM25/hybrid queries
  (inert on the old index because those fields are unmapped there), applies a configurable query prefix
  (`Services:EmbeddingService:QueryPrefix`), and runs an optional cross-encoder rerank stage
  (`Services:RerankService`). `Services:Elasticsearch:AutoCreateIndices` is now `false` so the Collector
  is the single owner of the index mapping. The orchestrator's own fallback mapping
  (`IndexManagementService.CreateIndexAsync`, used by the admin `/index` endpoint) was **corrected** — it
  previously declared fields the Collector never writes (`contentVector`, `fileType`, `documentId`…),
  which would have silently broken kNN; it is now aligned with the Collector mapping.

## Ground truth (verified)

- Deployed embedding model: **`intfloat/multilingual-e5-base`** (768-dim, multilingual) — see
  `deploy/embedding-service/compose.yml`. The `dims: 768` mapping is correct. The `EmbeddingModelName`
  labels in the collector appsettings (`multilingual-e5-small` / `all-MiniLM-L6-v2`) are **stale labels**
  only (used for logging); fix them to `intfloat/multilingual-e5-base` for clarity.
- **e5 requires `query:` / `passage:` prefixes.** They are currently applied on neither side, so index
  and query are at least *consistent* today. Turning prefixes on must be done on **both** sides together
  **and** followed by a reindex — never one side alone.

## Step 1 — Enable the reranker (biggest single quality win, no reindex needed)

The reranker fixes the exact failure you hit (a keyword-stuffed "Przyjęcie towaru" outranking the real
"Zamówienie zakupu"): it rescores the retrieved candidates by true query-document relevance.

1. Deploy the reranker container (added to `deploy/embedding-service/compose.yml`):
   ```bash
   docker compose -f deploy/embedding-service/compose.yml up -d rerank-service
   curl -s http://<host>:8581/health
   ```
2. Point the orchestrator at it (appsettings / env):
   ```json
   "Services": {
     "RerankService": { "Url": "http://<host>:8581", "Enabled": true, "RetrieveTopN": 40, "TimeoutSeconds": 30 }
   }
   ```
3. Restart the orchestrator. Hybrid search now retrieves top-40 and reranks down to the caller's limit
   (the chat's `DocumentSearchLimit`). This works on the **current** ES 8.11 and index — no reindex.

## Step 2 — Reindex with the modernized mapping (filename/title search + Polish + prefixes)

Needed to activate `fileName`/`title` boosting, the Polish analyzer, and (optionally) e5 prefixes.

1. **Elasticsearch:** upgrade to 9.x (target of the plan) and install the Polish plugin in the ES image:
   ```dockerfile
   FROM docker.elastic.co/elasticsearch/elasticsearch:9.x.y
   RUN bin/elasticsearch-plugin install --batch analysis-stempel
   ```
   (If you cannot upgrade yet, the reindex also works on 8.11 with `analysis-stempel` for 8.11; only the
   native `rrf`/`semantic_text` features from Step 3 require 9.x.)
2. **Collector config** for the reindex (point at a fresh index, then cut over via alias):
   ```json
   "Collector": {
     "IndexName": "rag-chunks-v2",
     "ContentAnalyzer": "polish",
     "EmbeddingPassagePrefix": "passage: ",
     "ChunkSize": 2000, "ChunkOverlap": 200
   }
   ```
   Set the **orchestrator** side consistently: `Services:EmbeddingService:QueryPrefix = "query: "`.
   (Leave both prefixes empty if you decide to switch to BGE-M3 instead — see Step 4.)
3. Run the Collector to build `rag-chunks-v2`, verify it (chunk count, non-empty `content`, populated
   `fileName`/`title`, vector dim = 768), then repoint reads via an alias:
   ```bash
   curl -X POST "$ES/_aliases" -H 'Content-Type: application/json' -d '{
     "actions": [
       { "remove": { "index": "rag-chunks",    "alias": "rag-chunks-read" } },
       { "add":    { "index": "rag-chunks-v2", "alias": "rag-chunks-read" } }
     ]
   }'
   ```
   Set the orchestrator `Services:Elasticsearch:DefaultIndexName` to the alias (`rag-chunks-read`) so
   future cutovers need no redeploy.

## Step 3 — (ES 9.x) Native RRF retriever

Once on ES 9.x, replace the `script_score` weighted-sum fusion in
`SearchQueryBuilder.BuildHybridQuery` with a native `rrf` retriever (standard BM25 sub-retriever +
`knn` sub-retriever, `rank_constant: 60`, `rank_window_size: 50`). This is a code change gated on the
upgrade; the current script_score path (with the new field boosts) remains the fallback for 8.11.

## Step 4 — (Optional) Upgrade the embedding model to BGE-M3

For stronger multilingual quality, serve `BAAI/bge-m3` (1024-dim) via TEI instead of e5-base:
- set the embedding-service `MODEL_ID=BAAI/bge-m3`, set collector `Constants.DefaultEmbeddingDimensions`
  / mapping `dims` to **1024**, clear the passage/query prefixes (BGE-M3 needs none), and reindex.

## Verification

- Query set (PL) incl. the hard case: "jak zrobić zamówienie zakupu" → expect
  `Zamowienie_zakupu_instrukcja_RAG.docx` in top-1/top-3; "jak przyjąć towar" → `Przyjecie_towaru...`.
- API smoke test: login `admin@citronex.pl`, chat with `useDocumentSearch:true`, check `documentsUsed`
  and answer quality; watch orchestrator logs for the "Reranked N candidates to top K" line.
- Unit tests: `dotnet test src/RAG.Tests` (see `RerankServiceTests`, `SearchQueryBuilderTests`).
