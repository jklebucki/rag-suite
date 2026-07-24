# Przetwarzanie dokumentów: wdrożenie kontenerowe

Ten katalog uruchamia wewnętrzną usługę `RAG.DocumentProcessing.Api` oraz lokalny `docling-serve`. Domyślny wariant używa CPU; wariant GPU jest nakładką Docker Compose dla NVIDIA CUDA 12.8. Żaden obraz nie używa tagu `latest`: obrazy Docling są przypięte do wydania `v1.27.0`.

## Konfiguracja wspólna

1. Skopiuj plik środowiskowy i ustaw dwa różne, długie klucze.

   ```bash
   cd deploy/document-processing
   cp .env.example .env
   ```

2. W produkcyjnym `appsettings` Orchestratora ustaw te same wartości:

   ```json
   {
     "Services": {
       "DocumentProcessing": {
         "Endpoint": "http://document-processing-api:8080",
         "ApiKey": "wartosc_DOCUMENT_PROCESSING_API_KEY",
         "TimeoutSeconds": 360
       }
     }
   }
   ```

3. Nie publikuj portu `5080` w Internecie. Najbezpieczniej ograniczyć go regułami sieciowymi do Orchestratora albo usunąć sekcję `ports` i uruchamiać oba kontenery w tej samej prywatnej sieci Compose.

## CPU (domyślnie)

Wymagany jest Docker Engine z Docker Compose v2. Dla małej instancji zacznij od `DOCLING_WORKERS=2` i `DOCLING_CPU_THREADS=4` w `.env`.

```bash
./scripts/document-processing/up-cpu.sh
curl -H "X-Api-Key: $DOCUMENT_PROCESSING_API_KEY" http://localhost:5080/health
```

Pierwsze uruchomienie pobiera modele Docling do wolumenu `docling-artifacts`; następne starty wykorzystują cache. Dla aktualizacji przypnij nowy tag oraz digest po jego zweryfikowaniu w rejestrze, a następnie odtwórz kontenery.

Źródła, rewizje i licencje modeli dopuszczonych w tej konfiguracji są zapisane w [MODEL_PROVENANCE.md](MODEL_PROVENANCE.md). Przed wdrożeniem o podwyższonych wymaganiach powtarzalności przygotuj i zachowaj obraz wolumenu `docling-artifacts` zgodny z tym rejestrem; świeży wolumen pobiera artefakty przy pierwszym starcie.

## NVIDIA GPU (alternatywa)

Wymagane są zgodny sterownik NVIDIA (co najmniej `550.54.14`) oraz `nvidia-container-toolkit`. Sprawdź najpierw widoczność GPU przez `nvidia-smi`, a potem uruchom:

```bash
./scripts/document-processing/up-gpu.sh
```

Skrypt łączy `compose.yml` z `compose.gpu.yml`; ten drugi podmienia wyłącznie obraz Docling na `docling-serve-cu128:v1.27.0` i rezerwuje GPU. Do zatrzymania obu wariantów użyj:

```bash
./scripts/document-processing/down.sh
```

## Operacyjne limity i diagnostyka

- `DOCLING_MAX_FILE_SIZE_BYTES` i `DOCLING_MAX_PAGE_COUNT` muszą odpowiadać limitom Orchestratora.
- API przyjmuje zadanie asynchronicznie, lokalnie zapisuje upload przed OCR i ogranicza kolejkę, równoległość oraz czas zadania.
- Stan usługi sprawdzisz pod `/health`; zadania udostępnia `/api/v1/jobs` wyłącznie z nagłówkiem `X-Api-Key`.
- Wolumen `document-processing-data` jest tymczasową przestrzenią uploadu. Po ukończeniu zadania plik wejściowy jest usuwany.

Docling i docling-serve są licencjonowane na MIT. API REST, opcje OCR i wymagania GPU opisuje oficjalna [dokumentacja Docling](https://docling-project.github.io/docling/usage/api_server/rest_api/) oraz [instrukcja deploymentu](https://docling-project.github.io/docling/usage/api_server/deployment/).
