# RAG Suite

**Intelligent Document Processing and Search Platform**

## About

RAG Suite is a comprehensive platform designed to help organizations efficiently process, search, and interact with their document collections using advanced AI and machine learning technologies.

## Key Features

### 🤖 Intelligent Chat
- Natural language conversations with your knowledge base
- Context-aware responses powered by advanced language models
- Multi-language support for global teams

### 🔍 Smart Search
- Powerful semantic search across all documents
- Hybrid search combining lexical and vector approaches
- Relevance ranking with RRF (Reciprocal Rank Fusion)

### 📊 Analytics & Insights
- Comprehensive usage metrics and performance monitoring
- Document ingestion tracking and status reporting
- Real-time system health monitoring

### 🔧 Advanced Configuration
- Flexible LLM integration (Ollama, OpenAI, and more)
- Customizable embedding models (BGE-M3 support)
- Fine-tuned parameters for optimal performance

## Technology Stack

- **Backend**: .NET 8 Minimal APIs with Vertical Slice Architecture
- **Frontend**: React 18 with TypeScript
- **Database**: PostgreSQL with EF Core
- **Search**: Elasticsearch with hybrid search capabilities
- **AI/ML**: Integration with various LLM providers

## Architecture

Built with modern software architecture principles:

- **Vertical Slice Architecture** for clear feature boundaries
- **Domain-Driven Design** principles
- **CQRS pattern** for optimal read/write separation
- **Event-driven architecture** with outbox pattern
- **Microservices-ready** design for scalability

## Security & Compliance

- JWT-based authentication with role-based access control
- Secure API endpoints with proper validation
- Environment-based configuration management
- Comprehensive logging and monitoring

## Getting Started

1. **Setup**: Follow the deployment guide to set up the platform
2. **Configure**: Set up your LLM and embedding services
3. **Ingest**: Upload and process your documents
4. **Search**: Start exploring your knowledge base

## Support

For support and documentation, please refer to:
- [API Documentation](./api-documentation.md)
- [Deployment Guide](../DEPLOYMENT_GUIDE.md)
- [Troubleshooting](../DOTNET8-TROUBLESHOOTING.md)

---

**Version**: 1.0.0
**License**: MIT
**Repository**: [GitHub](https://github.com/jklebucki/rag-suite)
