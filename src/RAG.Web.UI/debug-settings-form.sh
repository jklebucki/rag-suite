#!/bin/bash

# Skrypt do debugowania testów SettingsForm

echo "🔍 Debugowanie testów SettingsForm"
echo "===================================="
echo ""

cd "$(dirname "$0")"

# Wybór opcji
echo "Wybierz opcję:"
echo "1. Uruchom testy z verbose output"
echo "2. Uruchom testy z UI (interaktywne)"
echo "3. Uruchom konkretny test: 'should use useActionState for form submission'"
echo "4. Uruchom konkretny test: 'should display field errors from useActionState'"
echo "5. Uruchom wszystkie testy SettingsForm z większym timeoutem"
echo "6. Uruchom z watch mode"
echo ""

read -p "Wybierz opcję (1-6): " option

case $option in
  1)
    echo "📊 Uruchamianie z verbose output..."
    npm test -- --run src/features/settings/components/SettingsForm.test.tsx --reporter=verbose --no-coverage
    ;;
  2)
    echo "🖥️  Uruchamianie Vitest UI..."
    echo "Otwórz przeglądarkę na adresie pokazany poniżej:"
    npm test -- --ui
    ;;
  3)
    echo "🧪 Uruchamianie testu: 'should use useActionState for form submission'..."
    npm test -- --run src/features/settings/components/SettingsForm.test.tsx -t "should use useActionState for form submission" --reporter=verbose
    ;;
  4)
    echo "🧪 Uruchamianie testu: 'should display field errors from useActionState'..."
    npm test -- --run src/features/settings/components/SettingsForm.test.tsx -t "should display field errors from useActionState" --reporter=verbose
    ;;
  5)
    echo "⏱️  Uruchamianie z większym timeoutem (30s)..."
    npm test -- --run src/features/settings/components/SettingsForm.test.tsx --test-timeout=30000 --reporter=verbose
    ;;
  6)
    echo "👀 Uruchamianie w trybie watch..."
    echo "Testy będą się uruchamiać automatycznie po każdej zmianie w plikach"
    npm test -- src/features/settings/components/SettingsForm.test.tsx
    ;;
  *)
    echo "❌ Nieprawidłowa opcja"
    exit 1
    ;;
esac

