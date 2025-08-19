# Elasticsearch Index Mappings (BGE-M3 1024D / E5 768D)

This folder contains JSON mappings for three corpora used by the RAG Suite:

* `rag_oracle_schema` – compact schema records (tables/columns/relations/labels)
* `rag_ifs_sop` – SOP/KB chunks (500–1000 tokens)
* `rag_biz_processes` – process descriptions (BPMN/handbooks flattened)

## Similarity & Dimensions

* For best performance, **normalize embeddings to unit length** and use `"similarity": "dot_product"`.If you cannot normalize vectors, use `"similarity": "cosine"`.(Cosine via normalized vectors + dot product is the most efficient path.)
* Indexed vectors are limited to **1024 dims**. Non-indexed vectors can go up to 2048.See Elastic docs for `dense_vector`, kNN/HNSW, and hybrid search.References: Elastic Docs & Blogs. \[1\]\[2\]\[3\]\[4\]

## Create indices (examples)

BGE-M3 (1024D):

```bash
# Oracle schema
curl -u elastic:$ELASTIC_PASSWORD -X PUT http://localhost:9200/rag_oracle_schema \
  -H "Content-Type: application/json" \
  --data-binary @rag_oracle_schema_1024.json

# SOP
curl -u elastic:$ELASTIC_PASSWORD -X PUT http://localhost:9200/rag_ifs_sop \
  -H "Content-Type: application/json" \
  --data-binary @rag_ifs_sop_1024.json

# Business processes
curl -u elastic:$ELASTIC_PASSWORD -X PUT http://localhost:9200/rag_biz_processes \
  -H "Content-Type: application/json" \
  --data-binary @rag_biz_processes_1024.json

#E5-base (768D)
curl -u elastic:$ELASTIC_PASSWORD -X PUT http://localhost:9200/rag_oracle_schema_dev \
  -H "Content-Type: application/json" \
  --data-binary @rag_oracle_schema_768.json
```

## HNSW tuning

Default HNSW parameters in the mappings:

```json
"index_options": { "type": "hnsw", "m": 32, "ef_construction": 128 }
```

* Increase m and/or ef_construction for better recall (at the cost of memory and indexing time).
* At query time, control candidate breadth with num_candidates; consider hybrid BM25 + kNN for best relevance.

## Hybrid search

Elasticsearch supports hybrid search strategies (e.g., convex combination and RRF) to combine lexical (BM25) and vector scores. We'll wire this in query templates later.

## Notes

Use Compose v2 (docker compose) and omit the legacy version: in the compose file.
Make sure app-side embedding size matches the mapped dims (1024 for BGE-M3, 768 for E5).

## References


1. Elastic Docs – dense_vector & kNN search, constraints and similarity.
2. Elastic Search Labs – vector setup & hybrid search posts.
3. Elastic forum threads – HNSW params and hybrid scoring notes.
4. Dimension limits for indexed vectors (<=1024).


