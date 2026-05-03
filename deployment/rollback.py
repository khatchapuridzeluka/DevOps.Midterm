#!/usr/bin/env python3
"""Rollback to the previous color. Just swaps nginx — both colors are still running."""

import shutil
import subprocess
from pathlib import Path

DEPLOYMENT_DIR = Path(__file__).parent
COMPOSE_FILE = DEPLOYMENT_DIR / "docker-compose.yml"
NGINX_CONF = DEPLOYMENT_DIR / "nginx.conf"
NGINX_BLUE = DEPLOYMENT_DIR / "nginx-blue.conf"
NGINX_GREEN = DEPLOYMENT_DIR / "nginx-green.conf"


def run(cmd, check=True):
    return subprocess.run(cmd, shell=True, check=check, capture_output=True, text=True)


def current_color():
    content = NGINX_CONF.read_text()
    if "server blue:8080" in content:
        return "blue"
    if "server green:8080" in content:
        return "green"
    raise RuntimeError("Could not determine current color from nginx.conf")


def main():
    live = current_color()
    target = "green" if live == "blue" else "blue"

    print(f"Current live color: {live}")
    print(f"Rolling back to: {target}")

    # Swap nginx config
    source_conf = NGINX_GREEN if target == "green" else NGINX_BLUE
    shutil.copy(source_conf, NGINX_CONF)

    # Reload nginx
    run(f"docker compose -f {COMPOSE_FILE} exec -T nginx nginx -s reload", check=True)

    print(f"Rollback complete. Traffic now routed to {target}.")


if __name__ == "__main__":
    main()