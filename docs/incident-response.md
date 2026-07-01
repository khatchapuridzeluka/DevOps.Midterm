# Incident response

Short runbook for common failures in the local DevOps.WebAPI environment.

## Service availability target

- **SLO:** 99% availability for the public API endpoint (`http://localhost/api/Health` via nginx) over a rolling 24-hour window.
- **Error budget:** about 14 minutes of downtime per day before the SLO is missed.
- For this lab the stack runs on a single machine, so the SLO is a design target rather than a measured SLA.

## Alerts

| Alert | Severity | Meaning |
|---|---|---|
| `HighErrorRate` | critical | More than 5 application errors per minute |
| `AppDown` | critical | Prometheus cannot scrape `/metrics` for 1 minute |

Check fired alerts in Prometheus (`http://localhost:9090/alerts`) and Alertmanager (`http://localhost:9093`).

## If the API is down

1. Confirm the symptom: `curl http://localhost/api/Health` or open Grafana/Prometheus alerts.
2. Check container status: `docker ps -a` — look for `app-blue`, `app-green`, `app`, or `bluegreen-nginx` in a bad state.
3. Inspect logs: `docker logs app-blue` (or the live color / observability `app` container).
4. If a deploy just happened, roll back: `python deployment/rollback.py`
5. If a single container crashed, Docker should restart it automatically (`restart: unless-stopped`). If not: `docker compose -f deployment/docker-compose.yml up -d` or `python infrastructure/start.py`

## If error rate is high

1. Open Grafana dashboard — error rate panel and Loki logs.
2. Reproduce or confirm simulated errors: check whether `/simulate-error` was called.
3. If errors are real, inspect application logs for exceptions and roll back if tied to a recent deploy.

## If observability stack is unhealthy

1. `docker compose -f observability/docker-compose.yml ps`
2. Restart the failed service: `docker compose -f observability/docker-compose.yml up -d <service>`
3. Verify Prometheus targets: `http://localhost:9090/targets` — `devops-webapi` should be UP.

## Escalation (lab context)

For this project, resolution stops at restoring the local stack and documenting what failed. In production you would page an on-call engineer, open an incident ticket, and post a short post-mortem after recovery.
