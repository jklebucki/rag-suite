# Przetwarzanie dokumentów: wdrożenie kontenerowe

Ten katalog uruchamia wewnętrzną usługę `RAG.DocumentProcessing.Api` oraz lokalny `docling-serve`. Domyślny wariant używa CPU; wariant GPU jest nakładką Docker Compose dla NVIDIA CUDA 12.8. Żaden obraz nie używa tagu `latest`: bazowe obrazy Docling są przypięte do wydania `v1.27.0`, a `Dockerfile.docling-ocr` dodaje przypięte pakiety językowe Tesseract.

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

## Języki i jakość OCR

Czysty skan nie zawiera tekstu, na podstawie którego można wiarygodnie ustalić język przed pierwszym rozpoznaniem. Dlatego produkcyjny profil jawnie uruchamia Tesseract z językami oczekiwanymi w dokumencie:

```dotenv
DOCLING_OCR_PRESET=tesseract
DOCLING_OCR_LANGUAGES=pol,eng
```

To są trzyliterowe kody Tesseract (zgodne z jego listą języków), a nie dwuliterowe kody EasyOCR. Obraz zawiera `ces`, `dan`, `deu`, `eng`, `fin`, `fra`, `hun`, `ita`, `nld`, `nor`, `pol`, `por`, `ron`, `rus`, `slk`, `spa`, `swe`, `tur` i `ukr`. Przykładowo dokument niemiecko-angielski ustaw jako `deu,eng`, a ukraińsko-polski jako `ukr,pol`. Nie dodawaj wszystkich języków bez potrzeby: mniejszy, właściwy zestaw daje lepszą dokładność i krótszy czas.

Główny tekst jest oceniany również pod kątem gęstości, znaków zastępczych i nadmiaru osieroconych jedno-/dwuliterowych fragmentów. Wynik podejrzany jest ponawiany z pełnostronicowym OCR. Ponieważ na dokumentach mieszanych Tesseract lepiej odtwarza tekst i diakrytykę, a RapidOCR bywa lepszy dla komórek tabel, niekompletna tabela jest opcjonalnie przeliczana presetem `auto`; aplikacja podmienia wyłącznie lepszy blok tabeli, zachowując tekst Tesseract.

Progi i fallback są konfigurowalne przez `DOCLING_MINIMUM_TEXT_QUALITY`, `DOCLING_ENABLE_TABLE_FALLBACK`, `DOCLING_TABLE_FALLBACK_OCR_PRESET` oraz `DOCLING_MINIMUM_TABLE_COMPLETENESS`.

## Development lokalny

W repozytorium jest gotowy, odseparowany profil CPU dla lokalnego `dotnet run`. Zużywa jeden worker Doclinga z dwoma wątkami, maksymalnie 4 vCPU i 6 GiB RAM. API publikuje port wyłącznie pod `127.0.0.1:5080`, dlatego nie jest dostępne z lokalnej sieci.

W bieżącym klonie zostały utworzone ignorowane pliki lokalne `.env.development` oraz `appsettings.Development.local.json`, zawierające zgodne klucze deweloperskie. Dla nowego klonu przygotuj je z wersji przykładowych, ustawiając **identyczną** wartość klucza API w obu plikach:

```bash
cp deploy/document-processing/.env.development.example deploy/document-processing/.env.development
cp src/RAG.Orchestrator.Api/appsettings.Development.local.example.json \
  src/RAG.Orchestrator.Api/appsettings.Development.local.json
```

Uruchom kontenery, zaczekaj na pobranie modeli przy pierwszym starcie, a następnie uruchom Orchestrator z profilem `Development`:

```bash
./scripts/document-processing/up-dev.sh
export OCR_LOCAL_API_KEY='ta-sama-wartosc-co-DOCUMENT_PROCESSING_API_KEY-w-.env.development'
curl -H "X-Api-Key: $OCR_LOCAL_API_KEY" http://localhost:5080/health
dotnet run --project src/RAG.Orchestrator.Api --launch-profile http
```

Do zatrzymania lokalnego stosu użyj `./scripts/document-processing/down-dev.sh`. Nie kopiuj tych kluczy ani plików do środowiska produkcyjnego.

## CPU (produkcja)

Wymagany jest Docker Engine z Docker Compose v2. Profil w `.env.example` został dobrany dla hosta `llm-cuda-srv`: Intel i5-14600K (20 logicznych CPU), 46 GiB RAM i działające usługi embeddingu, rerankera oraz Ollama. Ustawia dwa workery po cztery wątki, limit 8 vCPU i 10 GiB RAM dla Doclinga oraz 1 vCPU i 1 GiB RAM dla API. To pozostawia zapas dla istniejących kontenerów.

```bash
./scripts/document-processing/up-cpu.sh
curl -H "X-Api-Key: $DOCUMENT_PROCESSING_API_KEY" http://localhost:5080/health
```

Pierwsze uruchomienie pobiera modele Docling do wolumenu `docling-artifacts`, zamontowanego pod domyślną ścieżką cache obrazu `/opt/app-root/src/.cache/docling/models`; następne starty wykorzystują cache. Nie ustawiaj `DOCLING_SERVE_ARTIFACTS_PATH` dla pustego wolumenu — ta zmienna jest przeznaczona dla uprzednio przygotowanych artefaktów. Dla aktualizacji przypnij nowy tag oraz digest po jego zweryfikowaniu w rejestrze, a następnie odtwórz kontenery.

Źródła, rewizje i licencje modeli dopuszczonych w tej konfiguracji są zapisane w [MODEL_PROVENANCE.md](MODEL_PROVENANCE.md). Przed wdrożeniem o podwyższonych wymaganiach powtarzalności przygotuj i zachowaj obraz wolumenu `docling-artifacts` zgodny z tym rejestrem; świeży wolumen pobiera artefakty przy pierwszym starcie.

## NVIDIA GPU (alternatywa)

Wymagane są zgodny sterownik NVIDIA (co najmniej `550.54.14`) oraz `nvidia-container-toolkit`. Sprawdź najpierw widoczność GPU przez `nvidia-smi`, a potem uruchom:

```bash
./scripts/document-processing/up-gpu.sh
```

Skrypt łączy `compose.yml` z `compose.gpu.yml`, buduje obraz `rag-suite/docling-serve-cu128:v1.27.0-ocr.1` na bazie `docling-serve-cu128:v1.27.0`, rezerwuje GPU i zmniejsza liczbę workerów oraz zadań do jednego. Docker nie potrafi narzucić twardego limitu VRAM, dlatego skrypt przed startem wymaga co najmniej `DOCLING_GPU_MIN_FREE_MIB=6144` MiB wolnej pamięci.

Na zbadanym hoście jest RTX 5060 Ti z 16 GiB VRAM, ale `gpt-oss:20b` w Ollama zajmuje około 15 GiB. Te obciążenia nie mogą bezpiecznie działać jednocześnie. Przed wariantem GPU zatrzymaj model, sprawdź wynik `nvidia-smi`, a po zakończeniu konwersji możesz go ponownie uruchomić:

```bash
ollama stop gpt-oss:20b
./scripts/document-processing/up-gpu.sh
```

Do zatrzymania obu wariantów użyj:

```bash
./scripts/document-processing/down.sh
```

## Operacyjne limity i diagnostyka

- `DOCLING_MAX_FILE_SIZE_BYTES` i `DOCLING_MAX_PAGE_COUNT` muszą odpowiadać limitom Orchestratora.
- API przyjmuje zadanie asynchronicznie, lokalnie zapisuje upload przed OCR i ogranicza kolejkę, równoległość oraz czas zadania.
- Wariant CPU jest właściwym wyborem, gdy Ollama obsługuje zapytania na GPU; wariant GPU jest przeznaczony dla okna wsadowego po zwolnieniu VRAM.
- Stan usługi sprawdzisz pod `/health`; zadania udostępnia `/api/v1/jobs` wyłącznie z nagłówkiem `X-Api-Key`.
- Wolumen `document-processing-data` jest tymczasową przestrzenią uploadu. Po ukończeniu zadania plik wejściowy jest usuwany.

Docling i docling-serve są licencjonowane na MIT. API REST, opcje OCR i wymagania GPU opisuje oficjalna [dokumentacja Docling](https://docling-project.github.io/docling/usage/api_server/rest_api/) oraz [instrukcja deploymentu](https://docling-project.github.io/docling/usage/api_server/deployment/).
