# RAG Suite

**Inteligentna Platforma Przetwarzania i Wyszukiwania Dokumentów**

## O Aplikacji

RAG Suite to kompleksowa platforma zaprojektowana, aby pomóc organizacjom efektywnie przetwarzać, wyszukiwać i interagować z kolekcjami dokumentów przy użyciu zaawansowanych technologii AI i uczenia maszynowego.

## Kluczowe Funkcje

### 🤖 Inteligentny Czat
- Konwersacje w języku naturalnym z bazą wiedzy
- Odpowiedzi uwzględniające kontekst zasilane zaawansowanymi modelami językowymi
- Wsparcie wielojęzyczne dla globalnych zespołów

### 🔍 Inteligentne Wyszukiwanie
- Potężne semantyczne wyszukiwanie we wszystkich dokumentach
- Hybrydowe wyszukiwanie łączące podejścia leksykalne i wektorowe
- Ranking trafności z RRF (Reciprocal Rank Fusion)

### 📊 Analizy i Wnioski
- Kompleksowe metryki użytkowania i monitorowanie wydajności
- Śledzenie pozyskiwania dokumentów i raportowanie statusu
- Monitorowanie zdrowia systemu w czasie rzeczywistym

### 🔧 Zaawansowana Konfiguracja
- Elastyczna integracja LLM (Ollama, OpenAI i więcej)
- Konfigurowalne modele embeddingu (wsparcie BGE-M3)
- Dostrojone parametry dla optymalnej wydajności

## Stos Technologiczny

- **Backend**: .NET 8 Minimal APIs z Architekturą Wycinków Pionowych
- **Frontend**: React 18 z TypeScript
- **Baza danych**: PostgreSQL z EF Core
- **Wyszukiwanie**: Elasticsearch z możliwościami wyszukiwania hybrydowego
- **AI/ML**: Integracja z różnymi dostawcami LLM

## Architektura

Zbudowane z nowoczesnymi zasadami architektury oprogramowania:

- **Architektura Wycinków Pionowych** dla jasnych granic funkcji
- **Zasady Domain-Driven Design**
- **Wzorzec CQRS** dla optymalnego rozdzielenia odczytu/zapisu
- **Architektura sterowana zdarzeniami** z wzorcem outbox
- **Gotowy na mikrousługi** design dla skalowalności

## Bezpieczeństwo i Zgodność

- Uwierzytelnianie oparte na JWT z kontrolą dostępu opartą na rolach
- Bezpieczne punkty końcowe API z właściwą walidacją
- Zarządzanie konfiguracją opartą na środowisku
- Kompleksowe logowanie i monitorowanie

## Pierwsze Kroki

1. **Konfiguracja**: Postępuj zgodnie z przewodnikiem wdrażania, aby skonfigurować platformę
2. **Konfiguracja**: Skonfiguruj swoje usługi LLM i embeddingu
3. **Pozyskiwanie**: Prześlij i przetwórz swoje dokumenty
4. **Wyszukiwanie**: Zacznij eksplorować swoją bazę wiedzy

---

**Wersja**: 1.0.0
**Licencja**: MIT
**Repozytorium**: [GitHub](https://github.com/jklebucki/rag-suite)
