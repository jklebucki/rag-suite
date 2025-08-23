using RAG.Orchestrator.Api.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add CORS for development
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors("AllowFrontend");
}

app.UseHttpsRedirection();

app.UseHttpsRedirection();

// Mock data
var mockSessions = new List<ChatSession>();
var mockMessages = new Dictionary<string, List<ChatMessage>>();

// === SEARCH ENDPOINTS ===
app.MapPost("/api/search", (SearchRequest request) =>
{
    var results = new SearchResult[]
    {
        new("1", "Oracle Database Schema Guide", 
            "Comprehensive guide to Oracle database schema design and best practices for RAG applications...", 
            0.95, "oracle", "schema", 
            new Dictionary<string, object> { {"table_count", 15}, {"size_mb", 250} }, 
            DateTime.Now.AddDays(-10), DateTime.Now.AddDays(-5)),
        
        new("2", "IFS SOP Document - User Management", 
            "Standard Operating Procedure for user management within IFS system including role assignments...", 
            0.87, "ifs", "sop", 
            new Dictionary<string, object> { {"version", "2.1"}, {"department", "IT"} }, 
            DateTime.Now.AddDays(-15), DateTime.Now.AddDays(-2)),
        
        new("3", "Business Process Automation Guidelines", 
            "Guidelines for implementing business process automation using workflow engines and approval systems...", 
            0.82, "files", "process", 
            new Dictionary<string, object> { {"category", "automation"}, {"complexity", "medium"} }, 
            DateTime.Now.AddDays(-7), DateTime.Now.AddDays(-1))
    };

    var filteredResults = results.Where(r => 
        r.Title.Contains(request.Query, StringComparison.OrdinalIgnoreCase) ||
        r.Content.Contains(request.Query, StringComparison.OrdinalIgnoreCase)
    ).ToArray();

    return new SearchResponse(filteredResults, filteredResults.Length, 45, request.Query);
})
.WithName("SearchDocuments")
.WithOpenApi();

// === CHAT ENDPOINTS ===
app.MapGet("/api/chat/sessions", () => mockSessions.ToArray())
.WithName("GetChatSessions")
.WithOpenApi();

app.MapPost("/api/chat/sessions", (CreateSessionRequest? request) =>
{
    var sessionId = Guid.NewGuid().ToString();
    var session = new ChatSession(
        sessionId,
        request?.Title ?? "New Chat",
        Array.Empty<ChatMessage>(),
        DateTime.Now,
        DateTime.Now
    );
    
    mockSessions.Add(session);
    mockMessages[sessionId] = new List<ChatMessage>();
    
    return session;
})
.WithName("CreateChatSession")
.WithOpenApi();

app.MapGet("/api/chat/sessions/{sessionId}", (string sessionId) =>
{
    var session = mockSessions.FirstOrDefault(s => s.Id == sessionId);
    if (session == null) return Results.NotFound();
    
    var messages = mockMessages.GetValueOrDefault(sessionId, new List<ChatMessage>());
    return Results.Ok(session with { Messages = messages.ToArray() });
})
.WithName("GetChatSession")
.WithOpenApi();

app.MapPost("/api/chat/sessions/{sessionId}/messages", (string sessionId, ChatRequest request) =>
{
    if (!mockMessages.ContainsKey(sessionId))
        return Results.NotFound();

    // Add user message
    var userMessage = new ChatMessage(
        Guid.NewGuid().ToString(),
        "user",
        request.Message,
        DateTime.Now
    );
    mockMessages[sessionId].Add(userMessage);

    // Generate mock AI response
    var aiResponse = GenerateMockResponse(request.Message);
    var aiMessage = new ChatMessage(
        Guid.NewGuid().ToString(),
        "assistant",
        aiResponse.Content,
        DateTime.Now.AddSeconds(2),
        aiResponse.Sources
    );
    mockMessages[sessionId].Add(aiMessage);

    // Update session timestamp
    var sessionIndex = mockSessions.FindIndex(s => s.Id == sessionId);
    if (sessionIndex >= 0)
    {
        mockSessions[sessionIndex] = mockSessions[sessionIndex] with { UpdatedAt = DateTime.Now };
    }

    return Results.Ok(aiMessage);
})
.WithName("SendMessage")
.WithOpenApi();

app.MapDelete("/api/chat/sessions/{sessionId}", (string sessionId) =>
{
    mockSessions.RemoveAll(s => s.Id == sessionId);
    mockMessages.Remove(sessionId);
    return Results.Ok();
})
.WithName("DeleteChatSession")
.WithOpenApi();

// === PLUGIN ENDPOINTS ===
app.MapGet("/api/plugins", () =>
{
    var plugins = new PluginInfo[]
    {
        new("oracle-sql", "Oracle SQL Plugin", "Provides access to Oracle database schema and query capabilities", "1.0.0", true, new[] {"database", "sql", "schema"}),
        new("ifs-sop", "IFS SOP Plugin", "Handles IFS Standard Operating Procedures and documentation", "1.2.1", true, new[] {"sop", "documentation", "ifs"}),
        new("biz-process", "Business Process Plugin", "Manages business process workflows and automation", "2.0.0", false, new[] {"workflow", "automation", "process"})
    };
    
    return plugins;
})
.WithName("GetPlugins")
.WithOpenApi();

// === ANALYTICS ENDPOINTS ===
app.MapGet("/api/analytics/usage", () =>
{
    var stats = new UsageStats(
        TotalQueries: 1247,
        TotalSessions: 89,
        AvgResponseTime: 1250.5,
        TopQueries: new[] { "Oracle schema", "user management", "process automation", "database backup", "IFS configuration" },
        PluginUsage: new Dictionary<string, int>
        {
            {"oracle-sql", 456},
            {"ifs-sop", 321},
            {"biz-process", 89}
        }
    );
    
    return stats;
})
.WithName("GetUsageStats")
.WithOpenApi();

app.Run();

// Helper method to generate mock AI responses
static (string Content, SearchResult[]? Sources) GenerateMockResponse(string userMessage)
{
    var lowerMessage = userMessage.ToLower();
    
    if (lowerMessage.Contains("oracle") || lowerMessage.Contains("database"))
    {
        return (
            "Based on the Oracle database documentation, I can help you with schema design and query optimization. The current schema includes 15 tables with relationships optimized for RAG applications. Would you like me to explain specific table structures or relationships?",
            new SearchResult[]
            {
                new("1", "Oracle Database Schema Guide", "Comprehensive guide...", 0.95, "oracle", "schema", new Dictionary<string, object>(), DateTime.Now.AddDays(-10), DateTime.Now.AddDays(-5))
            }
        );
    }
    
    if (lowerMessage.Contains("ifs") || lowerMessage.Contains("user"))
    {
        return (
            "According to the IFS Standard Operating Procedures, user management follows a structured approach with role-based access control. The latest version 2.1 includes enhanced security features and automated provisioning workflows.",
            new SearchResult[]
            {
                new("2", "IFS SOP Document - User Management", "Standard Operating Procedure...", 0.87, "ifs", "sop", new Dictionary<string, object>(), DateTime.Now.AddDays(-15), DateTime.Now.AddDays(-2))
            }
        );
    }
    
    if (lowerMessage.Contains("process") || lowerMessage.Contains("automation"))
    {
        return (
            "Business process automation can be implemented using workflow engines and approval systems. The guidelines recommend starting with medium complexity processes and gradually scaling up. Would you like specific implementation examples?",
            new SearchResult[]
            {
                new("3", "Business Process Automation Guidelines", "Guidelines for implementing...", 0.82, "files", "process", new Dictionary<string, object>(), DateTime.Now.AddDays(-7), DateTime.Now.AddDays(-1))
            }
        );
    }
    
    return (
        "I understand your question. Based on the available knowledge base, I can provide information about Oracle databases, IFS systems, and business processes. Could you please be more specific about what you'd like to know?",
        null
    );
}
