# DevOps.WebAPI — Midterm

A small .NET Web API used as a vehicle for setting up a full DevOps workflow: CI pipeline, infrastructure-as-code, blue-green deployment with rollback, and basic monitoring. The app itself is a calculator plus a person-categorization endpoint — the focus is on the surrounding automation, not the API.

## Tech stack

- C# / .NET 10, ASP.NET Core Web API
- xUnit for unit tests
- Docker (multi-stage build) and Docker Compose for orchestrating the blue/green/nginx setup
- nginx (Alpine) as the reverse proxy that does the traffic switching
- GitHub Actions for CI (lint and tests)
- Python 3 for the IaC, deployment, rollback, and monitoring scripts

## Repository layout

- `DevOps.WebAPI/` — the API
- `TestProject1/` — xUnit tests
- `infrastructure/setup.py` — IaC script
- `deployment/` — docker-compose, nginx configs, deploy and rollback scripts
- `monitoring/healthcheck.py` — periodic health check
- `.github/workflows/main.yml` — CI
- `Dockerfile` — multi-stage build for the API

## Endpoints

- `GET /api/Health` — health check, used by monitoring and deploy readiness
- `GET /api/calculator/{operation}/{a}/{b}` — dynamic route, supports add/subtract/multiply/divide
- `POST /api/person/categorize` — input endpoint, takes `{username, name, age}` JSON

![App responding via curl](screenshots/app-running.png)

## Setup

You need Docker Desktop (or Docker on Linux) and Python 3. The setup script handles the rest.

    python infrastructure/setup.py

What it does: on Debian/Ubuntu installs Docker via apt if missing; on Windows/macOS verifies Docker Desktop is installed (manual install required); verifies the Docker daemon is running and Docker Compose is available; creates the monitoring/logs and deployment directories; pre-pulls the .NET 10 SDK, ASP.NET runtime, and nginx Alpine images so the first deploy is faster.

![IaC setup script output](screenshots/infrastructure-setup.png)

## CI pipeline

Defined in .github/workflows/main.yml. Two jobs, sequential:

1. Code Linting — runs `dotnet format --verify-no-changes`. If formatting is off, this fails and the test job is skipped.
2. Run Unit Tests — dotnet restore, dotnet build, dotnet test on Ubuntu. Uses `needs: lint` so it only runs when linting passes.

Triggered on every push and pull request to main and develop. The lint-then-test ordering is the quality gate — broken formatting can't even reach the test step.

![Successful CI runs](screenshots/ci-success.png)

## Deployment — Blue-Green

The deployment runs three containers managed by Docker Compose. Two of them are copies of the API: `app-blue` exposed on host port 8080 and `app-green` on host port 8081. Only one of them is live at any time. The third container is nginx, listening on port 80 and acting as a reverse proxy in front of the two app containers — every public request goes through nginx, which forwards it to whichever color is currently active.

Which color is active is determined by the `nginx.conf` file. There are two template configs, `nginx-blue.conf` and `nginx-green.conf`, that proxy to `blue:8080` and `green:8080` respectively (using Docker network names, not host ports). The active `nginx.conf` is just a copy of one of those two templates. Deploying means: build a new image, restart the idle color with it, verify it's healthy, then overwrite `nginx.conf` with the other template and reload nginx. Rolling back means doing the same swap in reverse — and since the previous color is still running with the previous version, no rebuild is needed.

Bring it up the first time:

    docker compose -f deployment/docker-compose.yml up -d --build

This builds the API image, starts both colors and nginx. By default `nginx.conf` is a copy of `nginx-blue.conf`, so blue is the initial live color.

![Three containers running](screenshots/docker-composeps.png)

Deploy a new version:

    python deployment/deploy.py

The script reads nginx.conf to figure out which color is currently live, identifies the other color as the deployment target, rebuilds the target container's image from the latest code, restarts the target container with the new image, polls http://localhost:{target_port}/api/Health until it gets a 200 (up to 20 retries, 2s apart), and if healthy, copies nginx-{target}.conf over nginx.conf and reloads nginx. Traffic now flows to the new color; the previous color stays running with the old version. If the health check fails, the script aborts before swapping nginx — the previous version keeps serving traffic and nothing is lost.

![Deploy script output](screenshots/deployment-deploy.png)

Rollback:

    python deployment/rollback.py

Rollback is fast because the previous color is still running with the previous version. The script just swaps nginx.conf back and reloads. No rebuild, no health check needed — that color was already healthy before we deployed away from it.

![Rollback script output](screenshots/deployment-rollback.png)

I picked blue-green because the assignment specifically asks for it, and because rollback becomes trivially fast — flipping nginx back is a one-second operation. The downside is that you pay for double the infrastructure (two app containers running at all times instead of one). For a calculator API on a laptop that's not a real cost. In production you'd weigh it against rolling updates depending on what your infrastructure budget looks like.

## Monitoring

    python monitoring/healthcheck.py

Polls http://localhost/api/Health every 5 seconds and writes timestamped results to monitoring/logs/health.log. Each line shows status (UP/DOWN), HTTP status code, and response time in milliseconds. Press Ctrl+C to stop.

To demo failure detection, while the monitor is running you can stop the live container in another terminal with `docker stop app-blue`. The monitor will start logging DOWN entries. Bringing the container back with `docker start app-blue` recovers it.

![Health check monitoring](screenshots/monitoring.png)

## Local development without containers

If you want to run the API directly without going through Docker:

    dotnet run --project DevOps.WebAPI

Tests:

    dotnet test

The container setup is only needed for the blue-green demo.

## Workflow

When I push a commit, GitHub Actions picks it up and runs the lint job first. If formatting is wrong, that job fails and the test job is skipped — the pipeline stops there. If lint passes, the test job runs `dotnet restore`, `dotnet build`, and `dotnet test`. If any test fails, the run is marked failed.

Once CI is green I deploy locally by running `python deployment/deploy.py`. The script figures out which color is currently live by reading `nginx.conf`, builds a new image for the other color, restarts that container, and polls its health endpoint until it responds. If the new container becomes healthy, the script swaps `nginx.conf` to point at it and reloads nginx — traffic now flows to the new version. If the new container never becomes healthy, the script aborts before swapping, so the previous version keeps serving requests.

If something is wrong with the deployed version, `python deployment/rollback.py` flips traffic back. The previous color is still running with the previous version, so rollback is just a config swap and nginx reload — no rebuild, no waiting.

Throughout all of this, `python monitoring/healthcheck.py` can run in a separate terminal, polling `/api/Health` every five seconds and writing UP/DOWN status to a log file.

## Branches

- main — stable milestones
- develop — active work, all feature commits land here first

CI runs on both. When a feature is finished and tests pass on develop, develop is merged into main.

## Notes / known limitations

- The blue-green setup runs entirely on a single host (your machine). In real production you'd run blue and green on separate machines or pods. The mechanism is the same; only the scale changes.
- The monitor polls localhost. If the grader's machine doesn't have the stack running when they look at the script, the log will be empty — that's why a screenshot of a live run is included.
- The IaC script auto-installs Docker only on Debian-family Linux. On Windows and macOS it verifies that Docker Desktop is present, since Docker Desktop install isn't scriptable in the same way.