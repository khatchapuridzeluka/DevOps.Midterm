#!/usr/bin/env python3
"""Blue-Green deployment script. Swaps live traffic from one color to the other."""

import shutil
import subprocess
import sys
import time
import urllib.request
from pathlib import Path

DEPLOYMENT_DIR = Path(__file__).parent
COMPOSE_FILE = DEPLOYMENT_DIR / "docker-compose.yml"
NGINX_CONF = DEPLOYMENT_DIR / "nginx.conf"
NGINX_BLUE = DEPLOYMENT_DIR / "nginx-blue.conf"
NGINX_GREEN = DEPLOYMENT_DIR / "nginx-green.conf"


def run(cmd, check=True):
    return subprocess.run(cmd, shell=True, check=check, capture_output=True, text=True)


def current_color():
    """Read nginx.conf to determine which color is currently live."""
    content = NGINX_CONF.read_text()
    if "server blue:8080" in content:
        return "blue"
    if "server green:8080" in content:
        return "green"
    raise RuntimeError("Could not determine current color from nginx.conf")


def health_check(port, retries=20, delay=2):
    """Poll the target color until healthy or give up."""
    url = f"http://localhost:{port}/api/Health"
    for i in range(retries):
        try:
            with urllib.request.urlopen(url, timeout=2) as resp:
                if resp.status == 200:
                    return True
        except Exception:
            pass
        print(f"  Waiting for {url} ({i + 1}/{retries})...")
        time.sleep(delay)
    return False


def main():
    live = current_color()
    target = "green" if live == "blue" else "blue"
    target_port = 8081 if target == "green" else 8080

    print(f"Current live color: {live}")
    print(f"Deploying to: {target}")

    # Build new image
    print(f"Building new image for {target}...")
    run(f"docker compose -f {COMPOSE_FILE} build {target}", check=True)

    # Restart target container with new image
    print(f"Restarting {target} container...")
    run(f"docker compose -f {COMPOSE_FILE} up -d --no-deps --force-recreate {target}", check=True)

    # Health-check the new container
    print(f"Health-checking {target} on port {target_port}...")
    if not health_check(target_port):
        print(f"ERROR: {target} did not become healthy. Aborting deploy.")
        sys.exit(1)
    print(f"{target} is healthy.")

    # Swap nginx config
    print(f"Swapping nginx config to {target}...")
    source_conf = NGINX_GREEN if target == "green" else NGINX_BLUE
    shutil.copy(source_conf, NGINX_CONF)

    # Reload nginx
    print("Reloading nginx...")
    run(f"docker compose -f {COMPOSE_FILE} exec -T nginx nginx -s reload", check=True)

    print(f"Deploy complete. Traffic now routed to {target}.")


if __name__ == "__main__":
    main()