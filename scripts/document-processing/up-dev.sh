#!/usr/bin/env bash
set -euo pipefail

script_directory="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
deployment_directory="$(cd "${script_directory}/../../deploy/document-processing" && pwd)"
environment_file="${deployment_directory}/.env.development"

if [[ ! -f "${environment_file}" ]]; then
  printf 'Missing %s. Copy .env.development.example and set the local API keys first.\n' "${environment_file}" >&2
  exit 1
fi

docker compose \
  --project-name rag-document-processing-dev \
  --env-file "${environment_file}" \
  -f "${deployment_directory}/compose.yml" \
  up --detach --build
