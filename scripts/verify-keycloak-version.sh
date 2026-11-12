#!/usr/bin/env bash

# Verifies the Keycloak server and keycloak-config-cli image pins. Nothing bumps these
# automatically: dependabot does not watch Docker images here, so a forgotten copy would
# otherwise drift silently.
#
# 1. The Keycloak version is the same in development (AppHost), the auth tests
#    (Testcontainers fixture), the production image (infra/keycloak/Dockerfile) and its
#    operations doc. The faber login theme forks two keycloak.v2 templates, so production must
#    run the exact version the tests ran.
# 2. The keycloak-config-cli tag is the same in the AppHost, the test fixture and the
#    operations doc, so the realm import the tests prove is the one that runs everywhere.
# 3. The keycloak-config-cli tag is a full pin, <cli-semver>-<major>.<minor>.<patch>. Its
#    typed realm model knows only the fields of the Keycloak build it was compiled against,
#    and an unknown field in faber.json fails the whole import. Upstream assigns major-only
#    aliases such as 6.5.1-26 and latest-26 by a CI race (adorsys/keycloak-config-cli#1741),
#    so they can point at an older build than their name suggests: 6.5.1-26 is the 26.1.0
#    build.
# 4. The Keycloak suffix of the keycloak-config-cli tag is not newer than the Keycloak server.
#    An older suffix is allowed: the server may be ahead of the CLI as long as faber.json uses
#    no newer fields, and the integration tests' realm import catches it when it does.

set -euo pipefail

script_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "$script_dir/.." && pwd)"
cd "$repo_root"

apphost="src/api/Aspire/Cvitlo.AppHost/Program.cs"
fixture="tests/Common/Cvitlo.Common.Testing/Containers/FaberKeycloak.cs"
dockerfile="infra/keycloak/Dockerfile"
operations_doc="docs/operations/keycloak.md"

has_errors=false

# Prints the first capture of a sed -E pattern in a file, or nothing.
extract() {
  sed -nE "s/$2/\\1/p" "$1" | head -n 1
}

# Checks that every "path|version" source pins the same version, and stores that version in
# $verified. The first source is the reference that mismatches are reported against.
verify_same() {
  local name="$1"
  shift
  local reference="" path version
  verified=""

  for source in "$@"; do
    path="${source%%|*}"
    version="${source#*|}"

    if [[ -z "$version" ]]; then
      echo "error: no $name version found in $path" >&2
      has_errors=true
      continue
    fi

    if [[ -z "$verified" ]]; then
      verified="$version"
      reference="$path"
    elif [[ "$version" != "$verified" ]]; then
      echo "error: $path pins $name $version, but $reference pins $verified" >&2
      has_errors=true
    fi
  done
}

verify_same "Keycloak" \
  "$apphost|$(extract "$apphost" '.*const string keycloakTag = "([^"]+)";.*')" \
  "$fixture|$(extract "$fixture" '.*"quay\.io\/keycloak\/keycloak:([^"]+)".*')" \
  "$dockerfile|$(extract "$dockerfile" '^ARG KEYCLOAK_VERSION=([^[:space:]]+).*')" \
  "$operations_doc|$(extract "$operations_doc" '.*`quay\.io\/keycloak\/keycloak:([^`]+)`.*')"
keycloak_version="$verified"

verify_same "keycloak-config-cli" \
  "$apphost|$(extract "$apphost" '.*"adorsys\/keycloak-config-cli", "([^"]+)".*')" \
  "$fixture|$(extract "$fixture" '.*"adorsys\/keycloak-config-cli:([^"]+)".*')" \
  "$operations_doc|$(extract "$operations_doc" '.*`adorsys\/keycloak-config-cli:([^`]+)`.*')"
config_cli_tag="$verified"

full_pin='^[0-9]+\.[0-9]+\.[0-9]+-[0-9]+\.[0-9]+\.[0-9]+$'

if [[ -n "$config_cli_tag" && ! "$config_cli_tag" =~ $full_pin ]]; then
  echo "error: keycloak-config-cli $config_cli_tag is not a full pin; use <cli-semver>-<major>.<minor>.<patch>, such as 6.5.1-26.1.0 (major-only aliases can point at an older build)" >&2
  has_errors=true
elif [[ -n "$config_cli_tag" && -n "$keycloak_version" ]]; then
  config_cli_keycloak="${config_cli_tag#*-}"
  newest="$(printf '%s\n%s\n' "$config_cli_keycloak" "$keycloak_version" | sort -V | tail -n 1)"

  if [[ "$config_cli_keycloak" != "$keycloak_version" && "$newest" == "$config_cli_keycloak" ]]; then
    echo "error: keycloak-config-cli $config_cli_tag is built for Keycloak $config_cli_keycloak, newer than the server $keycloak_version" >&2
    has_errors=true
  fi
fi

if [[ "$has_errors" == true ]]; then
  exit 1
fi

echo "Keycloak version verified: $keycloak_version in the AppHost, the test fixture, the Dockerfile and the operations doc."
echo "keycloak-config-cli verified: $config_cli_tag in the AppHost, the test fixture and the operations doc, built for Keycloak $config_cli_keycloak (server $keycloak_version)."
