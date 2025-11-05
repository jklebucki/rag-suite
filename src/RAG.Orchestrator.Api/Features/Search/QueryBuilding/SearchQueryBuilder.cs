using RAG.Abstractions.Search;
using System.Text.Json;

namespace RAG.Orchestrator.Api.Features.Search.QueryBuilding;

/// <summary>
/// Builds Elasticsearch queries for search operations
/// </summary>
public class SearchQueryBuilder : ISearchQueryBuilder
{
    /// <inheritdoc />
    public Dictionary<string, object> BuildBm25Query(string query, int limit, int offset)
    {
        return new Dictionary<string, object>
        {
            ["query"] = new Dictionary<string, object>
            {
                ["bool"] = new Dictionary<string, object>
                {
                    ["should"] = new object[]
                    {
                        // Exact phrase match gets highest score
                        new Dictionary<string, object>
                        {
                            ["match_phrase"] = new Dictionary<string, object>
                            {
                                ["content"] = new Dictionary<string, object>
                                {
                                    ["query"] = query,
                                    ["boost"] = 3.0
                                }
                            }
                        },
                        // Important terms match with OR operator
                        new Dictionary<string, object>
                        {
                            ["match"] = new Dictionary<string, object>
                            {
                                ["content"] = new Dictionary<string, object>
                                {
                                    ["query"] = query,
                                    ["operator"] = "OR",
                                    ["boost"] = 2.0,
                                    ["minimum_should_match"] = "30%" // At least 30% of terms should match
                                }
                            }
                        },
                        // Fuzzy match for typos
                        new Dictionary<string, object>
                        {
                            ["match"] = new Dictionary<string, object>
                            {
                                ["content"] = new Dictionary<string, object>
                                {
                                    ["query"] = query,
                                    ["operator"] = "OR",
                                    ["fuzziness"] = "AUTO",
                                    ["boost"] = 1.0
                                }
                            }
                        }
                    },
                    ["minimum_should_match"] = 1 // At least one of the should clauses must match
                }
            },
            ["size"] = limit,
            ["from"] = offset,
            ["highlight"] = new Dictionary<string, object>
            {
                ["fields"] = new Dictionary<string, object>
                {
                    ["content"] = new { }
                },
                ["pre_tags"] = new[] { "<em>" },
                ["post_tags"] = new[] { "</em>" },
                ["fragment_size"] = 200,
                ["number_of_fragments"] = 3
            }
        };
    }

    /// <inheritdoc />
    public Dictionary<string, object> BuildHybridQuery(
        string query,
        float[] queryEmbedding,
        QueryProcessingResult queryProcessing,
        int limit,
        int offset)
    {
        return new Dictionary<string, object>
        {
            ["query"] = new Dictionary<string, object>
            {
                ["script_score"] = new Dictionary<string, object>
                {
                    // Base BM25 query
                    ["query"] = new Dictionary<string, object>
                    {
                        ["bool"] = new Dictionary<string, object>
                        {
                            ["should"] = new object[]
                            {
                                // Phrase match for exact terms - use optimized query
                                new Dictionary<string, object>
                                {
                                    ["match_phrase"] = new Dictionary<string, object>
                                    {
                                        ["content"] = new Dictionary<string, object>
                                        {
                                            ["query"] = query,
                                            ["boost"] = queryProcessing.Type == QueryType.Keywords ? 3.0 : 2.0
                                        }
                                    }
                                },
                                // Match with OR for broader coverage
                                new Dictionary<string, object>
                                {
                                    ["match"] = new Dictionary<string, object>
                                    {
                                        ["content"] = new Dictionary<string, object>
                                        {
                                            ["query"] = query,
                                            ["operator"] = "OR",
                                            ["minimum_should_match"] = queryProcessing.Type == QueryType.Keywords ? "50%" : "20%"
                                        }
                                    }
                                }
                            },
                            ["minimum_should_match"] = 1
                        }
                    },
                    // Hybrid scoring script combining BM25 and cosine similarity with adaptive weights
                    ["script"] = new Dictionary<string, object>
                    {
                        ["source"] = @"
                            double bm25Score = _score;
                            double cosineSim = cosineSimilarity(params.query_vector, 'embedding');
                            
                            // Use query-specific weights from processing
                            double bm25Weight = params.bm25_weight;
                            double semanticWeight = params.semantic_weight;
                            
                            // Combine scores with adaptive weighting
                            return bm25Weight * bm25Score + semanticWeight * (cosineSim + 1.0) * 10.0;
                        ",
                        ["params"] = new Dictionary<string, object>
                        {
                            ["query_vector"] = queryEmbedding,
                            ["bm25_weight"] = queryProcessing.KeywordWeight,
                            ["semantic_weight"] = queryProcessing.SemanticWeight
                        }
                    }
                }
            },
            ["size"] = limit * 3, // Get more results for better reconstruction
            ["_source"] = new[] { "content", "sourceFile", "position", "fileExtension", "indexedAt" },
            ["highlight"] = new Dictionary<string, object>
            {
                ["fields"] = new Dictionary<string, object>
                {
                    ["content"] = new Dictionary<string, object>
                    {
                        ["fragment_size"] = 150,
                        ["number_of_fragments"] = 3
                    }
                }
            }
        };
    }
}

