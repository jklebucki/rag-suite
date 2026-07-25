#!/usr/bin/env bash
set -euo pipefail

script_directory="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
deployment_directory="$(cd "${script_directory}/../../deploy/document-processing" && pwd)"
environment_file="${deployment_directory}/.env"

if [[ ! -f "${environment_file}" ]]; then
  printf 'Missing %s. Copy .env.example and set the required API keys first.\n' "${environment_file}" >&2
  exit 1
fi

if ! command -v nvidia-smi >/dev/null 2>&1; then
  printf 'nvidia-smi is required for the GPU deployment profile.\n' >&2
  exit 1
fi

minimum_free_mib="$(awk -F= '/^DOCLING_GPU_MIN_FREE_MIB=/{print $2; exit}' "${environment_file}")"
minimum_free_mib="${minimum_free_mib:-6144}"
if [[ ! "${minimum_free_mib}" =~ ^[0-9]+$ ]]; then
  printf 'DOCLING_GPU_MIN_FREE_MIB must be an integer number of MiB.\n' >&2
  exit 1
fi

available_free_mib="$(nvidia-smi --query-gpu=memory.free --format=csv,noheader,nounits | awk 'NR == 1 || $1 < min { min = $1 } END { print min }')"
if [[ -z "${available_free_mib}" || ! "${available_free_mib}" =~ ^[0-9]+$ ]]; then
  printf 'Unable to determine the free GPU memory.\n' >&2
  exit 1
fi

if (( available_free_mib < minimum_free_mib )); then
  printf 'GPU deployment needs at least %s MiB free VRAM; only %s MiB is available.\n' "${minimum_free_mib}" "${available_free_mib}" >&2
  printf 'Stop or unload the competing GPU workload (for example: ollama stop gpt-oss:20b) and retry.\n' >&2
  exit 1
fi

docker compose --env-file "${environment_file}" -f "${deployment_directory}/compose.yml" -f "${deployment_directory}/compose.gpu.yml" up --detach --build
