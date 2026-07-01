#!/usr/bin/env python3
"""Start the full local environment: blue-green deployment and observability stack."""

import shutil
import subprocess
import sys
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parent.parent
DEPLOYMENT_COMPOSE = REPO_ROOT / "deployment" / "docker-compose.yml"
OBSERVABILITY_COMPOSE = REPO_ROOT / "observability" / "docker-compose.yml"


def ensure_env_file():
    env_example = REPO_ROOT / ".env.example"
    env_file = REPO_ROOT / ".env"
    if not env_file.exists():
        if env_example.exists():
            shutil.copy(env_example, env_file)
            print("Created .env from .env.example")
        else:
            print("ERROR: .env.example is missing. Cannot create .env.")
            sys.exit(1)


def run(cmd, check=True):
    return subprocess.run(cmd, shell=True, cwd=REPO_ROOT, check=check)


def main():
    if run("docker info", check=False).returncode != 0:
        print("Docker daemon not running. Start Docker Desktop and try again.")
        sys.exit(1)

    if run("docker compose version", check=False).returncode != 0:
        print("Docker Compose not available.")
        sys.exit(1)

    ensure_env_file()

    print("Starting blue-green deployment...")
    run(f"docker compose -f {DEPLOYMENT_COMPOSE} up -d --build")

    print("Starting observability stack...")
    run(f"docker compose -f {OBSERVABILITY_COMPOSE} up -d --build")

    print()
    print("Environment is up.")
    print("  Blue-green API (nginx):  http://localhost")
    print("  Blue app:                http://localhost:8080")
    print("  Green app:               http://localhost:8081")
    print("  Observability API:       http://localhost:8082")
    print("  Grafana:                 http://localhost:3000  (see .env)")
    print("  Prometheus:              http://localhost:9090")
    print("  Alertmanager:            http://localhost:9093")


if __name__ == "__main__":
    main()
