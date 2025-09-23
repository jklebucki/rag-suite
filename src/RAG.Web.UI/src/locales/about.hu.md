# RAG Suite

**Intelligens Dokumentum Feldolgozás és Kereső Platform**

## Rólunk

A RAG Suite egy átfogó platform, amely segít a szervezeteknek hatékonyan feldolgozni, keresni és interakcióba lépni dokumentum gyűjteményeikkel fejlett AI és gépi tanulási technológiák használatával.

## Főbb Funkciók

### 🤖 Intelligens Csevegés
- Természetes nyelvi beszélgetések a tudásbázisával
- Kontextus-tudatos válaszok fejlett nyelvi modellekkel
- Többnyelvű támogatás globális csapatok számára

### 🔍 Intelligens Keresés
- Erős szemantikus keresés minden dokumentumban
- Hibrid keresés, amely lexikális és vektor megközelítéseket kombinál
- Relevancia rangsorolás RRF-fel (Reciprocal Rank Fusion)

### 📊 Elemzések és Betekintések
- Átfogó használati metrikák és teljesítmény monitorozás
- Dokumentum bevitel nyomonkövetés és állapot jelentés
- Valós idejű rendszer egészség monitorozás

### 🔧 Fejlett Konfiguráció
- Rugalmas LLM integráció (Ollama, OpenAI, és több)
- Testreszabható beágyazási modellek (BGE-M3 támogatás)
- Finomhangolt paraméterek optimális teljesítményért

## Technológiai Stack

- **Backend**: .NET 8 Minimal APIs Vertical Slice Architecture-rel
- **Frontend**: React 18 TypeScript-tel
- **Adatbázis**: PostgreSQL EF Core-ral
- **Keresés**: Elasticsearch hibrid keresési képességekkel
- **AI/ML**: Integráció különböző LLM szolgáltatókkal

## Architektúra

Modern szoftver architektúra elvekkel épült:

- **Vertical Slice Architecture** tiszta funkció határokért
- **Domain-Driven Design** elvek
- **CQRS minta** optimális olvasás/írás elválasztáshoz
- **Eseményvezérelt architektúra** outbox mintával
- **Mikroszolgáltatásokra kész** design skálázhatósághoz

## Biztonság és Megfelelés

- JWT alapú hitelesítés szerep alapú hozzáférés vezérléssel
- Biztonságos API végpontok megfelelő validációval
- Környezet alapú konfiguráció kezelés
- Átfogó naplózás és monitorozás

## Kezdő Lépések

1. **Beállítás**: Kövesd a telepítési útmutatót a platform beállításához
2. **Konfigurálás**: Állítsd be LLM és beágyazási szolgáltatásaidat
3. **Bevitel**: Töltsd fel és dolgozd fel dokumentumaidat
4. **Keresés**: Kezdd el felfedezni tudásbázisodat

---

**Verzió**: 1.0.0
**Licenc**: MIT
**Repository**: [GitHub](https://github.com/jklebucki/rag-suite)
