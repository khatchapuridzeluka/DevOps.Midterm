#!/usr/bin/env python3
"""Environment setup for DevOps.WebAPI. Verifies dependencies and prepares the environment."""

import platform
import shutil
import subprocess
import sys
from pathlib import Path


def run(cmd, check=False):
    """Run a shell command. Returns the completed process."""
    return subprocess.run(cmd, shell=True, capture_output=True, text=True, check=check)


def has_command(cmd):
    """Check if a command exists on PATH."""
    return shutil.which(cmd) is not None


def main():
    print("Setting up environment...")
    system = platform.system()

    # Verify Docker
    if not has_command("docker"):
        if system == "Linux":
            print("Docker not found. Installing via apt...")
            run("sudo apt update", check=True)
            run("sudo apt install -y docker.io docker-compose-v2", check=True)
            run("sudo systemctl enable --now docker", check=True)
        else:
            print(f"Docker not installed. Install Docker Desktop for {system} manually.")
            sys.exit(1)

    # Verify Docker daemon is running
    if run("docker info").returncode != 0:
        print("Docker daemon not running. Start Docker Desktop and try again.")
        sys.exit(1)

    # Verify Docker Compose
    if run("docker compose version").returncode != 0:
        print("Docker Compose not available.")
        sys.exit(1)

    # Create directories
    Path("monitoring/logs").mkdir(parents=True, exist_ok=True)
    Path("deployment").mkdir(parents=True, exist_ok=True)

    # Pull base images
    images = [
        "mcr.microsoft.com/dotnet/sdk:10.0",
        "mcr.microsoft.com/dotnet/aspnet:10.0",
        "nginx:alpine",
    ]
    for img in images:
        print(f"Pulling {img}...")
        run(f"docker pull {img}", check=True)

    print("Done.")


if __name__ == "__main__":
    main()