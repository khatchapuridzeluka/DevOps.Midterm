#!/usr/bin/env python3
"""Periodic health check. Hits /api/Health every N seconds and logs to file."""

import time
import urllib.request
from datetime import datetime
from pathlib import Path

URL = "http://localhost/api/Health"
INTERVAL_SECONDS = 5
LOG_FILE = Path(__file__).parent / "logs" / "health.log"


def check():
    timestamp = datetime.now().isoformat(timespec="seconds")
    try:
        start = time.time()
        with urllib.request.urlopen(URL, timeout=3) as resp:
            elapsed_ms = int((time.time() - start) * 1000)
            return f"{timestamp} | {URL} | UP | {resp.status} | {elapsed_ms}ms"
    except Exception as e:
        return f"{timestamp} | {URL} | DOWN | error: {e}"


def main():
    LOG_FILE.parent.mkdir(parents=True, exist_ok=True)
    print(f"Monitoring {URL} every {INTERVAL_SECONDS}s. Logs: {LOG_FILE}")
    print("Press Ctrl+C to stop.")
    print()

    while True:
        result = check()
        print(result)
        with LOG_FILE.open("a") as f:
            f.write(result + "\n")
        time.sleep(INTERVAL_SECONDS)


if __name__ == "__main__":
    try:
        main()
    except KeyboardInterrupt:
        print("\nStopped.")