# Codex: uniwersalne przetwarzanie dokumentów i pliki wynikowe

## Cel

Rozszerz repozytorium `jklebucki/rag-suite` o niezależny od dostawcy moduł, który:

1. przetwarza załączniki dodawane w oknie chatu;
2. obsługuje PDF z warstwą tekstową, skany PDF i dokumenty mieszane;
3. zwraca ujednolicony tekst/Markdown do istniejącego mechanizmu wstrzykiwania załączników do promptu;
4. pozwala w przyszłości dodawać kolejne formaty i silniki;
5. tworzy na żądanie użytkownika plik `.txt` albo `.docx`;
6. przechowuje plik wynikowy przez 12 godzin i dopina link do pobrania na końcu odpowiedzi asystenta.

Nie modyfikuj `RAG.Collector` ani mechanizmu indeksowania RAG. Dotyczy to wyłącznie załączników bieżącego chatu i artefaktów generowanych przez LLM.

## Wymagania bezwzględne

- Tylko komponenty dopuszczające bezpłatne użycie komercyjne: MIT, Apache-2.0, BSD-2-Clause lub podobne permissive.
- Nie dodawaj AGPL, SSPL ani niestandardowych licencji ograniczających zastosowania komercyjne.
- Podstawowy silnik: lokalny `docling-serve`, kod MIT; modele przypinaj do konkretnej wersji/revizji i zapisuj ich licencje.
- DOCX generuj przez `DocumentFormat.OpenXml` (MIT).
- Markdown parsuj przez `Markdig` (BSD-2-Clause).
- Nie używaj tagów Docker `latest`; przypnij wersję, docelowo także digest.
- Orchestrator nie może znać DTO ani szczegółów API Doclinga.
- LLM nie może sam generować adresu pobierania. Link dopina backend po fizycznym zapisaniu pliku.

## Nowe projekty

Dodaj do solution:

```text
src/RAG.DocumentProcessing.Abstractions
src/RAG.DocumentProcessing.Core
src/RAG.DocumentProcessing.Api
src/RAG.DocumentProcessing.Client
src/RAG.DocumentProcessing.Providers.Docling
```

### Kluczowe kontrakty

```csharp
public interface IDocumentProcessingProvider
{
    string Name { get; }
    bool CanProcess(DocumentDescriptor document);
    Task<DocumentProcessingResult> ProcessAsync(
        DocumentProcessingRequest request,
        CancellationToken cancellationToken);
}

public interface IDocumentProcessingClient
{
    Task<DocumentJobAccepted> SubmitAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken);
    Task<DocumentJobStatus> GetStatusAsync(string jobId, CancellationToken cancellationToken);
    Task<DocumentProcessingResult> GetResultAsync(string jobId, CancellationToken cancellationToken);
}

public interface ITemporaryArtifactStore
{
    Task<StoredArtifact> SaveAsync(
        GeneratedArtifact artifact,
        CancellationToken cancellationToken);
    Task<StoredArtifact?> GetAsync(
        string artifactId,
        string userId,
        CancellationToken cancellationToken);
    Task DeleteExpiredAsync(CancellationToken cancellationToken);
}
```

Wynik przetwarzania ma być niezależny od providera:

```csharp
public sealed record DocumentProcessingResult(
    string Markdown,
    string PlainText,
    int PageCount,
    bool OcrUsed,
    string Provider,
    double QualityScore,
    IReadOnlyList<string> Warnings);
```

## API przetwarzania

W `RAG.DocumentProcessing.Api` wystaw:

```text
POST   /api/v1/jobs
GET    /api/v1/jobs/{jobId}
GET    /api/v1/jobs/{jobId}/result
DELETE /api/v1/jobs/{jobId}
GET    /health
```

Statusy:

```text
queued | processing | ready | failed
```

Upload ma być asynchroniczny. Plik zapisz przed uruchomieniem OCR. Dodaj limity rozmiaru, liczby stron, czasu i równoległych zadań.

## Provider Docling

Wywołuj lokalny `docling-serve` przez:

```text
POST /v1/convert/file/async
GET  /v1/status/poll/{task_id}
GET  /v1/result/{task_id}
```

Domyślne opcje:

```text
to_formats=md,text,json
do_ocr=true
force_ocr=false
ocr_preset=tesseract
ocr_lang=pol,eng
table_mode=accurate
image_export_mode=placeholder
```

Nie używaj presetu `auto` jako głównego OCR dla tekstu wielojęzycznego. W obrazie `docling-serve` `v1.27.0` wybiera on RapidOCR, który nie stosuje przekazanej listy języków. Obraz pochodny musi zawierać dane językowe Tesseract, a lista języków ma być konfigurowana przez `Docling:OcrLanguages` w trzyliterowych kodach Tesseract, np. `pol,eng`.

Jeżeli wynik jest pusty, ma bardzo mało znaków na stronę, niski wynik jakości leksykalnej albo zawiera błąd ekstrakcji, wykonaj jeden retry z `force_ocr=true`. Jeżeli tabela głównego wyniku ma puste lub niespójne komórki, wykonaj dodatkową konwersję presetem `auto` i podmień wyłącznie lepszy blok tabeli Markdown; nie zastępuj nim poprawnego tekstu Tesseract. Znormalizuj wynik do `DocumentProcessingResult`.

Dodaj:

```text
deploy/document-processing/compose.yml
deploy/document-processing/.env.example
```

Uruchom początkowo obraz CPU. Konfigurację URL, API key, timeoutów i limitów umieść w `appsettings`.

## Integracja z załącznikami chatu

Zmień istniejące:

```text
MessageInput.tsx
useMultilingualChat.ts
chat.service.ts
chat.ts
ChatAttachmentService.cs
ChatAttachmentModels.cs
MemoryChatAttachmentStore.cs
UserChatEndpoints.cs
```

Wymagania:

- dodaj `.pdf` do UI i API;
- zachowaj szybkie lokalne przetwarzanie obecnych plików tekstowych;
- PDF kieruj przez `IDocumentProcessingClient`;
- rozszerz załącznik o `status`, `progress`, `pageCount`, `provider`, `errorCode`;
- UI odpytuje `/context` tylko podczas `queued/processing`;
- nie pozwalaj wysłać wiadomości z załącznikiem, który nie ma statusu `ready`;
- po zakończeniu licz tokeny z gotowego Markdown i dopiero wtedy sprawdzaj limit kontekstu;
- do istniejącego `BuildAttachmentsPromptBlock()` przekazuj gotowy Markdown.

### OCR: eksport wierny a transformacja przez LLM

- Kanoniczny Markdown OCR zapisuj wraz z wiadomością użytkownika i zawsze przekazuj go do modelu jako dane załącznika.
- Polecenia typu „pokaż pełną treść” albo „zwróć wynik w DOCX” obsługuj bez modelu: eksport ma być wierną kopią OCR.
- Jeżeli polecenie zawiera transformację (np. korektę językową, tłumaczenie, redakcję lub streszczenie) oraz format `.txt` albo `.docx`, nie omijaj modelu. Model otrzymuje pełny Markdown OCR i zwraca kompletny przetworzony dokument w Markdown.
- Backend zapisuje dokładnie ten przetworzony Markdown jako artefakt w żądanym formacie, wyświetla go w odpowiedzi czatu i zachowuje w historii rozmowy do kolejnych poleceń.
- Model może nadal zwrócić standardowy blok `<generated_artifact>` wymagany przez globalny prompt; dla transformacji OCR backend pobiera z niego wyłącznie Markdown i sam tworzy jeden artefakt w formacie wskazanym przez użytkownika.

## Generowanie TXT/DOCX przez LLM

Dodaj do kontraktu odpowiedzi LLM opcjonalny blok:

```text
<generated_artifact format="txt|docx" filename="bezpieczna-nazwa">
TREŚĆ ARTEFAKTU W MARKDOWN
</generated_artifact>
```

Zaktualizuj system prompt: blok ma być emitowany wyłącznie, gdy użytkownik jednoznacznie prosi o plik tekstowy lub DOCX.

Po odpowiedzi modelu backend:

1. wykrywa i parsuje blok;
2. usuwa go z widocznej treści odpowiedzi;
3. sanityzuje nazwę pliku i zabrania ścieżek;
4. dla `txt` zapisuje UTF-8;
5. dla `docx` konwertuje Markdown na podstawowe elementy Open XML: nagłówki, akapity, listy, tabele i kod;
6. zapisuje plik przez `ITemporaryArtifactStore`;
7. programowo dopina do odpowiedzi:

```markdown
[Pobierz nazwa.docx](/api/user-chat/artifacts/{artifactId}/download)

_Link ważny do: 2026-07-25 12:30 Europe/Warsaw._
```

Jeżeli zapis/konwersja się nie powiedzie, nie dopinaj martwego linku; zachowaj odpowiedź tekstową i dopisz krótki komunikat o błędzie.

## Magazyn tymczasowy

MVP: lokalny dysk, ale wyłącznie za abstrakcją `ITemporaryArtifactStore`.

```text
App_Data/generated-artifacts/{yyyy-MM-dd}/{artifactId}/{safeFileName}
```

Dodaj tabelę PostgreSQL `generated_artifacts`:

```text
id, user_id, session_id, assistant_message_id,
file_name, content_type, storage_key, size_bytes,
created_at, expires_at
```

Zasady:

- `expires_at = created_at + 12h`;
- endpoint pobierania wymaga JWT, zgodności `user_id` i nieprzekroczonego TTL;
- po wygaśnięciu zwróć HTTP 410;
- nigdy nie przyjmuj ścieżki pliku od klienta;
- nie ujawniaj `storage_key`;
- `BackgroundService` usuwa wygasłe rekordy i pliki co 30 minut;
- usunięcie sesji usuwa również jej niewygasłe artefakty.

Nie rozszerzaj istniejącego `FileDownloadService` obsługującego ścieżki współdzielone. Utwórz osobny feature `Features/Chat/Artifacts`.

Endpoint:

```text
GET /api/user-chat/artifacts/{artifactId}/download
```

Reactowy renderer Markdown musi przechwycić ten link i pobrać plik przez istniejący `apiHttpClient` jako `blob`, aby wysłać JWT, następnie uruchomić pobranie w przeglądarce.

## Testy wymagane

- PDF tekstowy, skanowany i mieszany;
- retry `force_ocr`;
- polskie znaki diakrytyczne i odrzucenie wyniku z nadmiarem osieroconych liter;
- selektywny fallback tabel bez podmiany poprawnego tekstu;
- błędny/za duży PDF;
- statusy oraz blokada wysłania;
- TXT UTF-8;
- DOCX otwierający się w Word/LibreOffice;
- sanitizacja nazwy i ochrona przed traversal;
- brak dostępu innego użytkownika;
- HTTP 410 po TTL;
- cleanup pliku i rekordu;
- link dopisany wyłącznie po udanym zapisie;
- brak bloku `<generated_artifact>` w widocznej odpowiedzi.

## Kryterium ukończenia

Funkcja jest ukończona, gdy użytkownik może:

1. dodać PDF w oknie chatu;
2. zobaczyć stan OCR i po zakończeniu zadać pytanie do jego pełnej treści;
3. poprosić np. „przygotuj wynik jako DOCX”;
4. otrzymać odpowiedź z działającym, autoryzowanym linkiem ważnym 12 godzin;
5. pobrać poprawny `.txt` lub `.docx`;
6. podmienić w przyszłości Docling na innego providera bez zmian w Orchestratorze i UI.

## Źródła techniczne

- Docling Serve REST API: https://docling-project.github.io/docling/usage/api_server/rest_api/
- Docling Serve deployment: https://docling-project.github.io/docling/usage/api_server/deployment/
- Docling Serve license: https://github.com/docling-project/docling-serve
- Docling supported formats: https://docling-project.github.io/docling/usage/supported_formats/
- Open XML SDK: https://github.com/dotnet/Open-XML-SDK
- Markdig: https://github.com/xoofx/markdig
