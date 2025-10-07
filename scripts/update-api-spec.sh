#!/usr/bin/env bash
set -euo pipefail

usage() {
  cat <<'EOF'
Usage: scripts/update-api-spec.sh [--apply] [--force] [--url <uri>]

Fetches the latest Trakt API Blueprint from Apiary and compares it with spec/trakt.apib.

Options:
  --apply, -a         Overwrite spec/trakt.apib with the downloaded copy if differences exist.
  --force, -f         When combined with --apply, overwrite even if the downloaded payload is empty.
  --url <uri>         Override the source URL (defaults to https://trakt.docs.apiary.io/api-description-document).
  --help, -h          Show this help message.

Without --apply the script operates in check mode and exits with a non-zero status
when the local spec differs from the remote copy.
EOF
}

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
LOCAL_SPEC="${REPO_ROOT}/spec/trakt.apib"
REMOTE_URL="https://trakt.docs.apiary.io/api-description-document"
MODE="check"
FORCE_OVERWRITE="false"

while [[ $# -gt 0 ]]; do
  case "$1" in
    --apply|-a)
      MODE="apply"
      shift
      ;;
    --force|-f)
      FORCE_OVERWRITE="true"
      shift
      ;;
    --url)
      if [[ $# -lt 2 ]]; then
        echo "Error: --url requires an argument" >&2
        exit 2
      fi
      REMOTE_URL="$2"
      shift 2
      ;;
    --help|-h)
      usage
      exit 0
      ;;
    *)
      echo "Error: unknown option '$1'" >&2
      usage
      exit 2
      ;;
  esac
done

if [[ ! -f "${LOCAL_SPEC}" ]]; then
  echo "Error: local spec not found at ${LOCAL_SPEC}" >&2
  exit 1
fi

TMP_DIR="$(mktemp -d)"
trap 'rm -rf "${TMP_DIR}"' EXIT
REMOTE_SPEC="${TMP_DIR}/trakt.apib"

HTTP_STATUS=$(curl -sS -w "%{http_code}" -o "${REMOTE_SPEC}" "${REMOTE_URL}") || {
  echo "Error: failed to download spec from ${REMOTE_URL}" >&2
  exit 1
}

if [[ "${HTTP_STATUS}" -ne 200 ]]; then
  echo "Error: unexpected HTTP status ${HTTP_STATUS} from ${REMOTE_URL}" >&2
  exit 1
fi

if [[ ! -s "${REMOTE_SPEC}" ]]; then
  echo "Warning: downloaded spec is empty." >&2
  if [[ "${MODE}" == "apply" && "${FORCE_OVERWRITE}" != "true" ]]; then
    echo "Refusing to overwrite local spec without --force." >&2
    exit 1
  fi
fi

if diff -q "${LOCAL_SPEC}" "${REMOTE_SPEC}" >/dev/null; then
  echo "Spec is already up to date."
  exit 0
fi

echo "Changes detected between local spec and remote source." >&2

diff -u "${LOCAL_SPEC}" "${REMOTE_SPEC}" || true

if [[ "${MODE}" == "check" ]]; then
  echo "Run with --apply to update spec/trakt.apib." >&2
  exit 1
fi

cp "${REMOTE_SPEC}" "${LOCAL_SPEC}"

echo "Updated spec/trakt.apib with the latest content from ${REMOTE_URL}."
exit 0
