# RAG Suite

**Platformă Inteligentă de Procesare și Căutare Documente**

## Despre

RAG Suite este o platformă cuprinzătoare concepută pentru a ajuta organizațiile să proceseze, să caute și să interacționeze eficient cu colecțiile lor de documente folosind tehnologii avansate de AI și învățare automată.

## Despre Proiect

Acest proiect a fost dezvoltat la **Citronex**, unde **Jarosław Kłębucki** servește ca dezvoltator principal, cu sprijinul lui **Kacper Kozłowski**. Toate elementele infrastructurii tehnice și datele sunt găzduite exclusiv în resursele interne ale Citronex, asigurând securitatea maximă și conformitatea cu politicile companiei.

## Funcții Principale

### 🤖 Chat Inteligent
- Conversații în limbaj natural cu baza dumneavoastră de cunoștințe
- Răspunsuri conștiente de context alimentate de modele lingvistice avansate
- Suport multilingv pentru echipe globale

### 🔍 Căutare Inteligentă
- Căutare semantică puternică în toate documentele
- Căutare hibridă care combină abordări lexicale și vectoriale
- Clasare relevanță cu RRF (Reciprocal Rank Fusion)

### 📊 Analize și Perspective
- Metrici cuprinzătoare de utilizare și monitorizare performanță
- Urmărire ingestie documente și raportare status
- Monitorizare sănătate sistem în timp real

### 🔧 Configurație Avansată
- Integrare flexibilă LLM (Ollama, OpenAI, și mai multe)
- Modele embedding configurabile (suport BGE-M3)
- Parametri ajustați fin pentru performanță optimă

## Stivă Tehnologică

- **Backend**: .NET 8 Minimal APIs cu Arhitectură Vertical Slice
- **Frontend**: React 18 cu TypeScript
- **Bază de date**: PostgreSQL cu EF Core
- **Căutare**: Elasticsearch cu capabilități căutare hibridă
- **AI/ML**: Integrare cu diverși furnizori LLM

## Arhitectură

Construit cu principii moderne de arhitectură software:

- **Arhitectură Vertical Slice** pentru limite clare de funcționalități
- **Principiile Domain-Driven Design**
- **Model CQRS** pentru separare optimă citire/scriere
- **Arhitectură bazată pe evenimente** cu model outbox
- **Design pregătit pentru microservicii** pentru scalabilitate

## Securitate și Conformitate

- Autentificare bazată pe JWT cu control acces bazat pe roluri
- Puncte finale API sigure cu validare corespunzătoare
- Management configurație bazat pe mediu
- Logging și monitorizare cuprinzătoare

## Noțiuni de Bază

1. **Configurare**: Urmați ghidul de implementare pentru a configura platforma
2. **Configurare**: Configurați serviciile dumneavoastră LLM și embedding
3. **Ingestie**: Încărcați și procesați documentele dumneavoastră
4. **Căutare**: Începeți să explorați baza dumneavoastră de cunoștințe

## Suport

Pentru suport și documentație, consultați:
- [Documentație API](./api-documentation.md)
- [Ghid Implementare](../DEPLOYMENT_GUIDE.md)
- [Depanare](../DOTNET8-TROUBLESHOOTING.md)

---

**Versiune**: 1.0.0
**Licență**: MIT
**Repository**: [GitHub](https://github.com/jklebucki/rag-suite)
**Dezvoltat de**: Citronex (Dezvoltator principal: Jarosław Kłębucki, Suport: Kacper Kozłowski)
**Infrastructură**: Toate componentele tehnice și datele găzduite exclusiv în resursele interne Citronex