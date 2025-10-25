# Instrukcje generowania quizów dla LLM

## Cel
Wygenerowanie poprawnego pliku JSON do importu quizu zgodnego ze wszystkimi regułami walidacji zdefiniowanymi w `ImportQuizValidator.cs`.

## Krytyczne reguły walidacji

### Ograniczenia na poziomie quizu
1. **Title** (WYMAGANE)
   - Nie może być puste
   - Maksymalna długość: 200 znaków
   - Typ: string

2. **Description** (OPCJONALNE)
   - Maksymalna długość: 1000 znaków gdy podane
   - Typ: string | null

3. **IsPublished** (WYMAGANE)
   - Typ: boolean
   - Wartości: true lub false

4. **Questions** (WYMAGANE)
   - Minimum: 1 pytanie
   - Maksimum: 100 pytań
   - Typ: tablica QuestionDto

5. **CreateNew** (WYMAGANE)
   - Typ: boolean
   - Domyślnie: true (utwórz nowy quiz)
   - false = nadpisz istniejący quiz (wymaga OverwriteQuizId)

6. **OverwriteQuizId** (WARUNKOWE)
   - Wymagane gdy CreateNew = false
   - Musi być null gdy CreateNew = true
   - Typ: Guid string (np. "550e8400-e29b-41d4-a716-446655440000") | null

### Ograniczenia na poziomie pytania
1. **Text** (WYMAGANE)
   - Nie może być puste
   - Maksymalna długość: 1000 znaków
   - Typ: string

2. **ImageUrl** (OPCJONALNE)
   - Maksymalna długość: 100 000 znaków (wspiera base64 do ~100KB)
   - Może być:
     - null
     - URL string (np. "https://example.com/image.png")
     - Data URI z base64 (np. "data:image/png;base64,iVBORw0KGgo...")
   - Typ: string | null

3. **Points** (WYMAGANE)
   - Minimum: 1
   - Maksimum: 100
   - Typ: integer

4. **Options** (WYMAGANE)
   - Minimum: 2 opcje na pytanie
   - Maksimum: 10 opcji na pytanie
   - Co najmniej 1 opcja musi mieć isCorrect = true
   - Typ: tablica OptionDto

### Ograniczenia na poziomie opcji
1. **Text** (WYMAGANE)
   - Nie może być puste
   - Maksymalna długość: 500 znaków
   - Typ: string

2. **ImageUrl** (OPCJONALNE)
   - Maksymalna długość: 100 000 znaków
   - Te same zasady co ImageUrl w pytaniu
   - Typ: string | null

3. **IsCorrect** (WYMAGANE)
   - Typ: boolean
   - Co najmniej jedna opcja na pytanie musi być true

## Szablon struktury JSON

```json
{
  "title": "Tytuł quizu (max 200 znaków)",
  "description": "Opcjonalny opis (max 1000 znaków)",
  "isPublished": true,
  "createNew": true,
  "overwriteQuizId": null,
  "questions": [
    {
      "text": "Treść pytania (max 1000 znaków)",
      "imageUrl": null,
      "points": 10,
      "options": [
        {
          "text": "Tekst opcji (max 500 znaków)",
          "imageUrl": null,
          "isCorrect": false
        },
        {
          "text": "Poprawna odpowiedź",
          "imageUrl": null,
          "isCorrect": true
        }
      ]
    }
  ]
}
```

## Typowe błędy walidacji - czego unikać

❌ **NIE WOLNO:**
- Pozostawiać pustego tytułu
- Dodawać więcej niż 100 pytań
- Mieć pytań z mniej niż 2 opcjami
- Mieć pytań z więcej niż 10 opcjami
- Mieć pytań BEZ poprawnej odpowiedzi (wszystkie isCorrect = false)
- Przekraczać limitów znaków (tytuł: 200, opis: 1000, pytanie: 1000, opcja: 500)
- Ustawiać punktów na 0 lub wartość ujemną
- Ustawiać punktów powyżej 100
- Dodawać obrazów base64 większych niż ~100KB
- Ustawiać overwriteQuizId gdy createNew jest true
- Pozostawiać overwriteQuizId jako null gdy createNew jest false

✅ **NALEŻY:**
- Zawsze dodawać co najmniej jedną poprawną odpowiedź na pytanie
- Trzymać się limitów znaków dla wszystkich tekstów
- Używać 2-10 opcji na pytanie
- Ustawiać punkty między 1-100
- Używać null dla opcjonalnych pól gdy nie podajemy danych
- Testować poprawność JSON przed importem
- Używać właściwego formatu Guid dla overwriteQuizId przy nadpisywaniu

## Wsparcie dla obrazów base64

Jeśli dodajesz obrazy jako base64:
```json
{
  "imageUrl": "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg=="
}
```

Utrzymuj stringi base64 poniżej 100KB (około 75KB oryginalnych danych obrazu).

## Przykład poprawnego quizu

Zobacz `sample-cybersecurity-quiz.json` dla kompletnego działającego przykładu z 10 pytaniami na temat cyberbezpieczeństwa.

## Testowanie JSON quizu

Przed importem zweryfikuj:
1. ✅ JSON jest poprawny (użyj walidatora JSON)
2. ✅ Wszystkie wymagane pola są obecne
3. ✅ Żadne limity znaków nie zostały przekroczone
4. ✅ Każde pytanie ma co najmniej jedną poprawną odpowiedź
5. ✅ Pytania mają po 2-10 opcji każde
6. ✅ Punkty są między 1-100
7. ✅ Logika CreateNew/OverwriteQuizId jest poprawna

## Endpoint importu

```http
POST /api/cyberpanel/quizzes/import
Content-Type: application/json

{
  // Twój JSON quizu tutaj
}
```

**Spodziewane odpowiedzi:**
- `201 Created` - Quiz zaimportowany pomyślnie
- `400 Bad Request` - Błąd formatu JSON
- `422 Unprocessable Entity` - Walidacja nie powiodła się (sprawdź szczegóły błędów)
- `404 Not Found` - Quiz do nadpisania nie został znaleziony (gdy createNew = false)

## Przykład odpowiedzi z błędem walidacji

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Quiz import validation failed",
  "status": 422,
  "errors": {
    "Title": ["Quiz title is required"],
    "Questions[0].Options": ["Question must have at least 2 options"],
    "Questions[1].Text": ["Question text cannot exceed 1000 characters"]
  }
}
```

---

**Uwaga:** Ten walidator zapewnia integralność danych i zapobiega typowym problemom bezpieczeństwa, takim jak nadmiernie duże payloady lub źle sformatowane dane. Zawsze waliduj swój JSON zgodnie z tymi zasadami przed próbą importu.
