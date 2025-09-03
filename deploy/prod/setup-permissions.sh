#!/bin/bash

# Skrypt do nadania uprawnień wykonywania dla skryptów RAG-Suite
echo "Nadawanie uprawnień wykonywania dla skryptów..."

chmod +x restart-embedding.sh
chmod +x reinstall-all.sh

echo "✅ Uprawnienia nadane dla:"
echo "   - restart-embedding.sh"
echo "   - reinstall-all.sh"

ls -la *.sh
