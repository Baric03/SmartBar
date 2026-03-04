<h1 align="center">SmartBar</h1>
<p align="center">
   <strong>A cloud-native microservices platform for café order management, inventory tracking, and drink preparation workflows.</strong>
</p>

## Overview

**SmartBar** is a microservices-based system designed for café staff to manage drink orders, inventory, and barista preparation workflows. The platform demonstrates real-world integration patterns including synchronous inter-service communication via gRPC, asynchronous event-driven messaging via Apache Kafka, and reactive stream processing with Rx.NET.

The entire system is containerized with Docker, continuously integrated and deployed through GitHub Actions, and monitored with a full observability stack (Prometheus, Grafana, Jaeger).

---

## Architecture

The system is composed of **4 domain microservices** and an **API Gateway**, each running as an independent Docker container with its own dedicated PostgreSQL database (database-per-service pattern).

```
                          ┌─────────────────────┐
                          │     API Gateway      │
                          │       (YARP)         │
                          └──────────┬──────────┘
                                     │ HTTP (REST)
              ┌──────────────────────┼──────────────────────┐
              │                      │                      │
   ┌──────────▼──────────┐ ┌────────▼────────┐ ┌───────────▼──────────┐
   │   Order Service     │ │  Bar Service    │ │ Notification Service │
   │                     │ │                 │ │                      │
   │  ┌───────────────┐  │ │  ┌───────────┐  │ │  ┌───────────────┐   │
   │  │   OrderDb     │  │ │  │   BarDb   │  │ │  │NotificationDb │   │
   │  └───────────────┘  │ │  └───────────┘  │ │  └───────────────┘   │
   └──────────┬──────────┘ └────────▲────────┘ └──────────▲───────────┘
              │ gRPC                │ Kafka                │ Kafka
   ┌──────────▼──────────┐         │                      │
   │ Inventory Service   │    ┌────┴──────────────────────┴────┐
   │                     │    │          Apache Kafka           │
   │  ┌───────────────┐  │    │  order-events │ drink-ready-events │
   │  │ InventoryDb   │  │    └────────────────────────────────┘
   │  └───────────────┘  │
   └─────────────────────┘
```

---

## Communication Patterns

SmartBar implements **four distinct communication patterns**, each chosen for a specific integration need:

| Pattern | Technology | Usage | Why |
|---------|-----------|-------|-----|
| **REST API** | ASP.NET Core Controllers | External client access via API Gateway | Industry standard for client-facing APIs |
| **gRPC** | Protocol Buffers + HTTP/2 | Order → Inventory stock checks & deductions | High-performance, type-safe synchronous RPC |
| **Message Queue** | Apache Kafka | Order events → Bar & Notification services | Decoupled, resilient async event-driven flow |
| **Reactive Streams** | System.Reactive (Rx.NET) | BarService Kafka consumer pipeline | Buffered batch processing with backpressure |

### Event Flow

1. **`order-events`** topic — Published by OrderService when a new order is created. Consumed by BarService (creates drink tasks) and NotificationService (logs order notification).
2. **`drink-ready-events`** topic — Published by BarService when a drink is marked as ready. Consumed by OrderService (updates order status to `Ready`) and NotificationService (logs ready notification).

---

## Microservice Details

### Order Service
> Accepts drink orders, verifies stock via gRPC, and publishes events to Kafka.

| Property | Value |
|----------|-------|
| **Database** | PostgreSQL (`OrderDb`) |
| **Entity** | `Order { Id, TableNum, Items, Status }` |
| **Produces to** | `order-events` (Kafka) |
| **Consumes from** | `drink-ready-events` (Kafka) |
| **Calls** | InventoryService via gRPC (`CheckStock`, `DeductStock`) |

### Inventory Service
> Manages drink ingredient stock and serves synchronous gRPC queries.

| Property | Value |
|----------|-------|
| **Database** | PostgreSQL (`InventoryDb`) |
| **Entity** | `Stock { Id, Ingredient, Quantity }` |
| **gRPC Methods** | `CheckStock`, `DeductStock` |

### Bar Service
> Manages the barista's drink preparation queue with reactive stream processing.

| Property | Value |
|----------|-------|
| **Database** | PostgreSQL (`BarDb`) |
| **Entity** | `DrinkTask { Id, OrderId, Name, IsReady }` |
| **Consumes from** | `order-events` (Kafka, via Rx.NET pipeline) |
| **Produces to** | `drink-ready-events` (Kafka) |
| **Rx.NET** | 3-second buffered batch processing of incoming order events |

### Notification Service
> Consumes Kafka events and logs staff notifications.

| Property | Value |
|----------|-------|
| **Database** | PostgreSQL (`NotificationDb`) |
| **Entity** | `Log { Id, OrderId, Message, SentAt }` |
| **Consumes from** | `order-events`, `drink-ready-events` (Kafka) |

### API Gateway
> Single entry point for all client requests, built with Microsoft YARP reverse proxy.

| Property | Value |
|----------|-------|
| **Technology** | YARP (Yet Another Reverse Proxy) |
| **Swagger UI** | Aggregates all service API docs at `/swagger` |
| **Routes** | `/api/Orders/*`, `/api/Inventory/*`, `/api/Bar/*`, `/api/Notification/*` |

---

## Application Workflow

The typical order lifecycle through the system:

```
1. Browse Inventory       GET  /api/Inventory          → View available ingredients
       │
2. Place Order            POST /api/Orders             → Creates order (status: Pending)
       │                          │
       │                    ┌─────┴─────┐
       │                    │   gRPC    │
       │                    ▼           │
       │              CheckStock()      │
       │              DeductStock()     │
       │                    │           │
       │                    └─────┬─────┘
       │                          │
       │                    Kafka: order-events
       │                    ┌─────┴─────┐
       │                    ▼           ▼
       │              Bar Service   Notification
       │             (drink tasks)    (log msg)
       │
3. View Bar Queue         GET  /api/Bar                → See drink preparation tasks
       │
4. Mark Drink Ready       PUT  /api/Bar/{id}/mark-ready → Updates task, publishes event
       │                          │
       │                    Kafka: drink-ready-events
       │                    ┌─────┴─────┐
       │                    ▼           ▼
       │             Order Service   Notification
       │            (status→Ready)    (log msg)
       │
5. Verify Order           GET  /api/Orders/{id}        → Confirm status is "Ready"
       │
6. Check Notifications    GET  /api/Notification       → View all staff notifications
```

---

## Tech Stack

| Category | Technology |
|----------|-----------|
| **Runtime** | .NET 10, ASP.NET Core |
| **API Gateway** | YARP (Yet Another Reverse Proxy) |
| **Database** | PostgreSQL 15, Entity Framework Core |
| **Messaging** | Apache Kafka (Confluent Platform 7.3.2) |
| **Synchronous RPC** | gRPC with Protocol Buffers |
| **Reactive Streams** | System.Reactive (Rx.NET) |
| **Containerization** | Docker, Docker Compose |
| **CI/CD** | GitHub Actions |
| **Static Analysis** | SonarCloud |
| **Tracing** | OpenTelemetry, Jaeger |
| **Metrics** | Prometheus, Grafana |
| **Testing** | xUnit, Moq (Unit + E2E) |

---

## Getting Started

### Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (Docker Compose v2+)
- [.NET 10 SDK](https://dotnet.microsoft.com/download) (for local development only)

### Start All Services

```bash
docker compose up -d
```

This will spin up all microservices, the API Gateway, PostgreSQL, Kafka, Prometheus, Grafana, and Jaeger. Database of Inventory service is automatically seeded on first run.

### Stop All Services

```bash
docker compose down -v
```

> **Note:** The `-v` flag removes persistent volumes (database data, Grafana dashboards). Omit it to preserve data between restarts.

---

## Local Service URLs

After running `docker compose up -d`, all services are accessible at:

| Service / Tool | URL | Description |
|----------------|-----|-------------|
| **API Gateway** | http://localhost:8000 | Single entry point for all services |
| **Swagger UI** | http://localhost:8000/swagger | Aggregated API documentation (via Gateway) |
| **Order Service** | http://localhost:7224/swagger | Order Service Swagger (direct) |
| **Inventory Service** | http://localhost:7042/swagger | Inventory Service Swagger (direct) |
| **Bar Service** | http://localhost:7026/swagger | Bar Service Swagger (direct) |
| **Notification Service** | http://localhost:7081/swagger | Notification Service Swagger (direct) |
| **Grafana** | http://localhost:3000 | Monitoring dashboards (`admin` / `smartbar`) |
| **Prometheus** | http://localhost:9090 | Metrics query UI |
| **Jaeger UI** | http://localhost:16686 | Distributed tracing UI |
| **PostgreSQL** | `localhost:5455` | Database (`admin` / `barpassword123`) |
| **Kafka** | `localhost:29092` | Kafka broker (from host) |

---

## CI/CD Pipeline

### Continuous Integration (CI)

The CI pipeline runs on every push and pull request to `main` and `dev` branches. Documentation-only changes (`.md`, `docs/`, `.gitignore`, `LICENSE`) are automatically skipped via `paths-ignore`.

```
Build & Test ──────────────► SonarCloud Analysis
     │
     └──► Detect Changes ──► Docker Build (per changed service)
```

| Stage | Description |
|-------|-------------|
| **Build & Test** | Restores, builds the solution, runs all unit tests (xUnit) and E2E tests |
| **SonarCloud** | Static code analysis for bugs, code smells, security vulnerabilities, and coverage |
| **Detect Changes** | Identifies which services have file changes (path-aware) |
| **Docker Build** | Builds Docker images only for services with actual code changes |

### Continuous Deployment (CD)

The CD pipeline triggers on pushes to `main` and deploys the application to [Render](https://render.com/) via deploy hooks with an automated post-deployment health check.

---

## Observability

SmartBar implements a comprehensive observability stack:

- **Metrics** — All services expose a `/metrics` endpoint (Prometheus format) via OpenTelemetry. Prometheus scrapes these every 15 seconds, and Grafana provides pre-configured dashboards.
- **Distributed Tracing** — OpenTelemetry traces are exported to Jaeger via OTLP (gRPC on port 4317). Traces cover HTTP requests, gRPC calls, and HTTP client operations.
- **Health Checks** — Every service exposes a `/health` endpoint for Docker health checks and deployment verification.

---

## Project Structure

```
SmartBar/
├── src/
│   ├── ApiGateway/              # YARP reverse proxy & aggregated Swagger UI
│   ├── OrderService/            # Order management, gRPC client, Kafka producer/consumer
│   │   ├── Controllers/         # REST API endpoints
│   │   ├── Core/                # Service interfaces & implementations
│   │   ├── Data/                # EF Core DbContext & seeder
│   │   ├── Events/              # Kafka event DTOs
│   │   ├── Messaging/           # Kafka producer & consumer
│   │   ├── Models/              # Domain entities
│   │   └── Protos/              # gRPC .proto definitions
│   ├── InventoryService/        # Stock management, gRPC server
│   │   ├── Core/                # Service layer + gRPC service implementation
│   │   └── Protos/              # gRPC .proto definitions
│   ├── BarService/              # Drink task management, Rx.NET Kafka consumer
│   │   ├── Messaging/           # Kafka producer & Rx.NET consumer pipeline
│   │   └── Events/              # Kafka event DTOs
│   └── NotificationService/     # Event-driven notification logging
│       ├── Converters/          # JSON converters
│       └── Messaging/           # Kafka consumer
├── Tests/
│   ├── OrderService.UnitTests/
│   ├── InventoryService.UnitTests/
│   ├── BarService.UnitTests/
│   ├── NotificationService.UnitTests/
│   └── SmartBar.E2E.Tests/      # End-to-end integration tests
├── monitoring/
│   ├── grafana/                 # Dashboards & provisioning config
│   └── prometheus/              # Scrape configuration
├── .github/workflows/
│   ├── ci.yml                   # CI pipeline (build, test, analyze, docker)
│   └── cd.yml                   # CD pipeline (deploy to Render)
├── docker-compose.yml           # Full-stack orchestration
└── SmartBar.sln                 # .NET solution file
```
