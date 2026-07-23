#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$repo_root"

echo "Checking Docker..."
if ! command -v docker >/dev/null 2>&1; then
  echo "Docker is not installed or not available in PATH. Install Docker Desktop first." >&2
  exit 1
fi

if ! docker info >/dev/null 2>&1; then
  echo "Docker daemon is not running. Start Docker Desktop and try again." >&2
  exit 1
fi

echo "Starting SonarQube and PostgreSQL..."
docker compose -f docker-compose.sonarqube.yml up -d

echo "Waiting for SonarQube to become ready..."
sleep 20

if curl -fsS http://localhost:9000/api/system/health >/tmp/sonar-health.json 2>/dev/null; then
  cat /tmp/sonar-health.json
else
  echo "SonarQube may still be starting. Open http://localhost:9000 after a few minutes."
fi

echo ""
echo "Login: admin / admin"
echo "Open: http://localhost:9000"
echo "Create a project token under My Account -> Security"
