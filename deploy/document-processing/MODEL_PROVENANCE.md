# Rejestr modeli i licencji Docling

Ten rejestr opisuje minimalny zestaw modeli wymagany przez standardowy potok PDF wdrożony w tym repozytorium. Stan został zweryfikowany 2026-07-25 dla obrazu `docling-serve` `v1.27.0` (wydanego 2026-07-20). Wydanie zawiera m.in. `docling-slim 2.113.0`, `docling-core 2.87.1`, `docling-ibm-models 3.13.3` i `docling-parse 7.8.1`.

## Dopuszczone artefakty

| Rola | Źródło | Zweryfikowana rewizja | Licencja | Użycie |
| --- | --- | --- | --- | --- |
| Analiza układu strony | [`docling-project/docling-layout-old`](https://huggingface.co/docling-project/docling-layout-old) | `b5b4bd59ad2b69aab715e9b1f1dfd74394c45fd4` | Apache-2.0 | Standardowy model układu `DOCLING_LAYOUT_V2` w `docling-slim 2.113.0`. |
| Struktura tabel | [`docling-project/docling-models`](https://huggingface.co/docling-project/docling-models) | `2199320848bb9a8a519d22e4b528185a4f9a6f64` | Apache-2.0 oraz CDLA-Permissive-2.0 | TableFormer w trybie `accurate`; CDLA-Permissive-2.0 jest licencją permissive. |

Nie włączamy modeli do opisu obrazów, VLM, wzorów, kodu ani klasyfikacji obrazów. Nie jest używana zewnętrzna usługa OCR ani zewnętrzny model zdalny. Wywołanie API ustawia `do_ocr=true`, `force_ocr=false` dla pierwszej próby, języki `pl` i `en` oraz ponawia konwersję tylko wtedy, gdy wynik jest pusty, zbyt krótki lub zawiera ostrzeżenia.

## Reprodukowalność artefaktów

Docling pozwala wskazać lokalny katalog modeli przez `DOCLING_SERVE_ARTIFACTS_PATH`; Compose montuje go jako wolumen `docling-artifacts`. Wersja `docling-slim 2.113.0` deklaruje domyślną nazwę rewizji modeli jako `main`, dlatego sam tag kontenera nie zastępuje blokady wag.

W środowisku produkcyjnym należy jednorazowo przygotować artefakty z powyższych rewizji, zweryfikować ich sumy kontrolne, zachować immutowalną kopię wolumenu `docling-artifacts` i montować ją tylko do odczytu. Wdrożenie z nowym, pustym wolumenem jest dozwolone dla środowisk rozwojowych, lecz pobiera modele podczas pierwszego startu i wymaga następnie utrwalenia tej kopii. To zapewnia audytowalność bez wymuszania nieudokumentowanych zmiennych środowiskowych Docling.

## Komponenty aplikacyjne

| Komponent | Wersja | Licencja |
| --- | --- | --- |
| [`docling-serve`](https://github.com/docling-project/docling-serve/tree/v1.27.0) | `v1.27.0` | MIT |
| [`docling`](https://github.com/docling-project/docling/tree/v2.113.0) / `docling-slim` | `2.113.0` | MIT |
| [`Markdig`](https://www.nuget.org/packages/Markdig/1.3.2) | `1.3.2` | BSD-2-Clause |
| [`DocumentFormat.OpenXml`](https://www.nuget.org/packages/DocumentFormat.OpenXml/3.5.1) | `3.5.1` | MIT |

Źródła: [release docling-serve v1.27.0](https://github.com/docling-project/docling-serve/releases/tag/v1.27.0), [kod konfiguracji modeli Docling 2.113.0](https://github.com/docling-project/docling/blob/v2.113.0/docling/datamodel/layout_model_specs.py), [instrukcja lokalnych artefaktów](https://docling-project.github.io/docling/usage/advanced_options/) oraz metadane licencji przy wskazanych repozytoriach modeli.
