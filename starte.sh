#!/bin/sh
# Startet ein Beispiel mit den Zugangsdaten aus .env.
#   ./starte.sh kapitel10_erste_schritte/01_verbinden.cs [Argumente]
#   ./starte.sh kapitel17_dashboard [Argumente]          (Projektordner)
set -e
cd "$(dirname "$0")"
set -a
[ -f .env ] && . ./.env
set +a
ziel=$1; shift
if [ -d "$ziel" ]; then
  exec dotnet run --project "$ziel" -- "$@"
fi
exec dotnet run "$ziel" -- "$@"
