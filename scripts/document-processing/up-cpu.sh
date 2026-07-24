#!/usr/bin/env bash
set -euo pipefail

script_directory="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
deployment_directory="$(cd "${script_directory}/../../deploy/document-processing" && pwd)"

docker compose --env-file "${deployment_directory}/.env" -f "${deployment_directory}/compose.yml" up --detach --build
