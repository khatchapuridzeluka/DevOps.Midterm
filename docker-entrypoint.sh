#!/bin/sh
set -e

# Shared log volumes may be root-owned from an earlier run; ensure the app user can write.
mkdir -p /app/logs
chown -R "${APP_UID}:${APP_UID}" /app/logs

exec runuser -u app -- "$@"
