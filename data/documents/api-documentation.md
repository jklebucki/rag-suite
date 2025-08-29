# API Documentation - RAG Orchestrator

## Overview
RAG Orchestrator API provides endpoints for chat interactions and document search.

## Authentication
All requests require API key in header:
`
Authorization: Bearer YOUR_API_KEY
`

## Endpoints

### Chat API

#### POST /api/chat/sessions
Create new chat session
`json
{
  "userId": "string",
  "title": "string"
}
`

#### POST /api/chat/sessions/{sessionId}/messages
Send message to chat
`json
{
  "message": "string",
  "useRag": true
}
`

#### GET /api/chat/sessions/{sessionId}
Get chat session details

### Search API

#### GET /api/search
Search documents
`
GET /api/search?query=elasticsearch&limit=10
`

#### POST /api/search
Advanced search
`json
{
  "query": "string",
  "filters": {},
  "limit": 10,
  "useVector": true
}
`

## Response Format
`json
{
  "success": true,
  "data": {},
  "error": null,
  "timestamp": "2024-08-24T10:00:00Z"
}
`

## Error Codes
- 400: Bad Request
- 401: Unauthorized
- 404: Not Found
- 500: Internal Server Error
