#!/usr/bin/env python3
"""Validate compose files and run a post-build smoke test against /api/Health."""

import shutil
import subprocess
import sys
import time
import urllib.request
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parent.parent
IMAGE_TAG = "devops-webapi:verify"
CONTAINER_NAME = "devops-webapi-verify"
HEALTH_URL = "http://localhost:8080/api/Health"


def run(cmd, check=True):
    return subprocess.run(cmd, shell=True, cwd=REPO_ROOT, check=check)


def ensure_env_file():
    env_example = REPO_ROOT / ".env.example"
    env_file = REPO_ROOT / ".env"
    if env_example.exists() and not env_file.exists():
        shutil.copy(env_example, env_file)
        print("Created .env from .env.example")


def validate_compose_files():
    compose_files = [
        REPO_ROOT / "deployment" / "docker-compose.yml",
        REPO_ROOT / "observability" / "docker-compose.yml",
    ]
    for compose_file in compose_files:
        print(f"Validating {compose_file.relative_to(REPO_ROOT)}...")
        run(f'docker compose -f "{compose_file}" config --quiet')


def smoke_test():
    run(f"docker rm -f {CONTAINER_NAME}", check=False)

    print("Building Docker image...")
    run(f"docker build -t {IMAGE_TAG} .")

    print("Starting container for smoke test...")
    run(
        f"docker run -d --name {CONTAINER_NAME} "
        f"-e ASPNETCORE_URLS=http://+:8080 "
        f"-p 8080:8080 {IMAGE_TAG}"
    )

    try:
        for attempt in range(1, 21):
            try:
                with urllib.request.urlopen(HEALTH_URL, timeout=2) as resp:
                    if resp.status == 200:
                        print(f"Smoke test passed: {HEALTH_URL} returned 200")
                        return
            except Exception as exc:
                print(f"  Waiting for {HEALTH_URL} ({attempt}/20): {exc}")
                time.sleep(2)

        print("Smoke test failed: health endpoint did not respond in time")
        sys.exit(1)
    finally:
        run(f"docker rm -f {CONTAINER_NAME}", check=False)


def main():
    if run("docker info", check=False).returncode != 0:
        print("Docker daemon not running.")
        sys.exit(1)

    ensure_env_file()
    validate_compose_files()
    smoke_test()
    print("Verification complete.")


if __name__ == "__main__":
    main()
