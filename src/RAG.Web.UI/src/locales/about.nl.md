# RAG Suite

**Intelligente Documentverwerking en Zoekplatform**

## Over

RAG Suite is een uitgebreid platform ontworpen om organisaties te helpen hun documentcollecties efficiënt te verwerken, doorzoeken en ermee te interageren met behulp van geavanceerde AI- en machine learning-technologieën.

## Belangrijkste Functies

### 🤖 Intelligente Chat
- Natuurlijke taal gesprekken met uw kennisbank
- Contextbewuste antwoorden powered by geavanceerde taalmodellen
- Meertalige ondersteuning voor wereldwijde teams

### 🔍 Slim Zoeken
- Krachtige semantische zoekopdrachten in alle documenten
- Hybride zoekopdrachten die lexicale en vectorbenaderingen combineren
- Relevantie ranking met RRF (Reciprocal Rank Fusion)

### 📊 Analytics & Inzichten
- Uitgebreide gebruiksstatistieken en prestatiemonitoring
- Documentopname tracking en statusrapportage
- Real-time systeemgezondheidsmonitoring

### 🔧 Geavanceerde Configuratie
- Flexibele LLM-integratie (Ollama, OpenAI, en meer)
- Aanpasbare embedding modellen (BGE-M3 ondersteuning)
- Fijn afgestemde parameters voor optimale prestaties

## Technologie Stack

- **Backend**: .NET 8 Minimal APIs met Vertical Slice Architecture
- **Frontend**: React 18 met TypeScript
- **Database**: PostgreSQL met EF Core
- **Zoeken**: Elasticsearch met hybride zoekmogelijkheden
- **AI/ML**: Integratie met verschillende LLM-providers

## Architectuur

Gebouwd met moderne software architectuur principes:

- **Vertical Slice Architecture** voor duidelijke functiegrenzen
- **Domain-Driven Design** principes
- **CQRS patroon** voor optimale read/write scheiding
- **Event-gedreven architectuur** met outbox patroon
- **Microservices-ready** ontwerp voor schaalbaarheid

## Beveiliging & Compliance

- JWT-gebaseerde authenticatie met rolgebaseerde toegangscontrole
- Veilige API-eindpunten met juiste validatie
- Omgevingsgebaseerd configuratiebeheer
- Uitgebreide logging en monitoring

## Aan de Slag

1. **Setup**: Volg de implementatiegids om het platform op te zetten
2. **Configureer**: Stel uw LLM- en embedding-services in
3. **Opname**: Upload en verwerk uw documenten
4. **Zoeken**: Begin met het verkennen van uw kennisbank

## Ondersteuning

Voor ondersteuning en documentatie, raadpleeg:
- [API Documentatie](./api-documentation.md)
- [Implementatiegids](../DEPLOYMENT_GUIDE.md)
- [Probleemoplossing](../DOTNET8-TROUBLESHOOTING.md)

---

**Versie**: 1.0.0
**Licentie**: MIT
**Repository**: [GitHub](https://github.com/jklebucki/rag-suite)
