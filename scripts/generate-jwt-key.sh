#!/bin/bash

# Script to generate secure JWT keys for RAG Suite
# Usage: ./scripts/generate-jwt-key.sh [environment]

set -e

ENVIRONMENT=${1:-"prod"}

echo "🔐 Generating secure JWT key for environment: $ENVIRONMENT"

# Generate a cryptographically secure random key (512 bits / 64 bytes)
# This creates a base64-encoded string that's much longer than 256 bits minimum
JWT_KEY=$(openssl rand -base64 64 | tr -d '\n')

echo ""
echo "Generated JWT Secret Key:"
echo "========================="
echo "$JWT_KEY"
echo ""
echo "Key length: ${#JWT_KEY} characters"
echo "Bits: $((${#JWT_KEY} * 6)) bits (minimum required: 256 bits)"
echo ""

# Optionally save to environment-specific file
read -p "Save to appsettings.$ENVIRONMENT.json? (y/N): " -n 1 -r
echo
if [[ $REPLY =~ ^[Yy]$ ]]; then
    echo "Updating appsettings.$ENVIRONMENT.json..."
    # This would need to be implemented based on your specific needs
    echo "Manual update required - copy the key above to your appsettings file"
fi

echo ""
echo "🔒 Security Notes:"
echo "=================="
echo "1. Keep this key secret and secure"
echo "2. Use different keys for different environments"
echo "3. Rotate keys periodically for enhanced security"
echo "4. Never commit keys to version control"
echo "5. Consider using Azure Key Vault or similar for production"
