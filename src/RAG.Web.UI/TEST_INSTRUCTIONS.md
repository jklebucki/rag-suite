# Test Scenariusz - Jak przetestować poprawki

## Instrukcje testowania:

### 1. Przygotowanie środowiska testowego:
```bash
cd /Users/jklebucki/Projects/rag-suite/src/RAG.Web.UI
npm run dev
```

### 2. Testowanie w przeglądarce:

#### Krok 1: Zaloguj się
1. Otwórz http://localhost:3000/login
2. Zaloguj się używając swoich poświadczeń
3. Sprawdź czy dostałeś się do dashboard'u
4. **WAŻNE**: Sprawdź w konsoli przeglądarki logi rozpoczynające się od 🔐

#### Krok 2: Sprawdź localStorage
W konsoli przeglądarki wpisz:
```javascript
// Sprawdź czy dane są zapisane
debugAuth()

// Lub ręcznie:
console.log('Token:', localStorage.getItem('auth_token'))
console.log('User:', localStorage.getItem('user_data'))
```

#### Krok 3: Test odświeżenia strony
1. Będąc zalogowany na dashboard, wciśnij F5 lub Ctrl+R
2. Strona powinna się załadować bez przekierowania na login
3. Sprawdź w konsoli logi z emoji 🔐

#### Krok 4: Test wielokrotnych kart
1. Skopiuj URL z zalogowaną sesją
2. Otwórz nową kartę z tym samym URL
3. Powinieneś być automatycznie zalogowany

#### Krok 5: Test długiej sesji
1. Zostaw aplikację otwartą na ~10 minut
2. Sprawdź czy token automatycznie się odświeża

### 3. Debugowanie problemów:

Jeśli nadal jest problem, sprawdź w konsoli:

1. **Logi inicjalizacji** - powinny zaczynać się od 🔐
2. **Logi reducera** - powinny pokazywać akcje SET_USER
3. **Logi tokenów** - powinny pokazywać pobieranie z localStorage

#### Najczęstsze problemy:
- **Brak danych w localStorage** - sprawdź logi 🔐 podczas logowania, powinny pokazać "storeAuthData called" i "Data stored successfully"
- Token wygasł - logi pokażą "Token expired"
- Serwer odrzuca token - logi pokażą błąd getCurrentUser
- Problem z localStorage - logi pokażą błędy dostępu

#### Szczegółowa diagnoza braku danych w localStorage:
1. **Podczas logowania** sprawdź w konsoli czy widzisz:
   - `🔐 Login attempt with credentials`
   - `🔐 Login response received`
   - `🔐 storeAuthData called with`
   - `🔐 Data stored in localStorage successfully`

2. **Jeśli nie ma logów storeAuthData** - problem w AuthContext
3. **Jeśli są logi ale brak danych** - problem z localStorage (prywatne okno?)
4. **Jeśli są błędy** - sprawdź szczegóły w konsoli

### 4. Ręczne czyszczenie dla świeżego testu:
```javascript
// W konsoli przeglądarki:
localStorage.clear()
location.reload()
```

### 5. Sprawdzenie tokenów:
```javascript
// Sprawdź ważność tokena:
const token = localStorage.getItem('auth_token')
if (token) {
  const payload = JSON.parse(atob(token.split('.')[1]))
  console.log('Expires:', new Date(payload.exp * 1000))
  console.log('Valid:', payload.exp * 1000 > Date.now())
}
```
