# SmartBar Microservices System

## 1. Overview
The SmartBar system is a microservices-based application designed for cafe staff to manage drink orders, inventory, and preparation workflows. The project focuses on implementing modern DevOps practices, including containerization, CI/CD, and observability.

## 2. Architecture & Communication
The system consists of 4 microservices and an API Gateway. It utilizes multiple types of communication to meet the specification:
- **REST API**: Used for external requests and inter-service commands.
- **gRPC**: High-performance synchronous communication between Order and Inventory services.
- **Kafka (Message Queue)**: Asynchronous event-driven communication for notifications.
- **System.Reactive (Rx.NET)**: Reactive stream processing in the BarService Kafka consumer.

## 3. Local URLs — Quick Reference

After running `docker-compose up -d`, all services are available at:

| Service / Tool          | URL                                      | Description                         |
|-------------------------|------------------------------------------|-------------------------------------|
| **API Gateway**         | http://localhost:8000                     | Single entry point for all services |
| **Swagger UI**          | http://localhost:8000/swagger             | Aggregated API docs (via Gateway)   |
| **Order Service**       | http://localhost:7224/swagger             | Order Service Swagger (direct)      |
| **Inventory Service**   | http://localhost:7042/swagger             | Inventory Service Swagger (direct)  |
| **Bar Service**         | http://localhost:7026/swagger             | Bar Service Swagger (direct)        |
| **Notification Service**| http://localhost:7081/swagger             | Notification Service Swagger (direct)|
| **Grafana**             | http://localhost:3000                     | Dashboards (admin / smartbar)       |
| **Prometheus**          | http://localhost:9090                     | Metrics query UI                    |
| **Jaeger UI**           | http://localhost:16686                    | Distributed tracing UI              |
| **PostgreSQL**          | localhost:5455                            | DB (admin / barpassword123)         |
| **Kafka**               | localhost:29092                           | Kafka broker (from host)            |

## 4. Application Workflow

The application follows this order flow:

1. **Browse Inventory** — `GET /api/Inventory` — See available ingredients and quantities.
2. **Place Order** — `POST /api/Orders` — Create a new order (status: `Pending`). This:
   - Checks inventory stock via **gRPC**
   - Deducts ingredient quantities
   - Publishes `order-events` to **Kafka** → creates bar tasks + notification
3. **View Bar Queue** — `GET /api/Bar` or `GET /api/Bar/{id}` — See drink preparation tasks.
4. **Mark Drink Ready** — `PUT /api/Bar/{id}/mark-ready` — Mark a drink as ready. This:
   - Updates the order status to `Ready` in OrderService
   - Publishes `drink-ready-events` to **Kafka** → creates notification
5. **Verify Order** — `GET /api/Orders` or `GET /api/Orders/{id}` — Confirm the order status is `Ready`.
6. **Check Notifications** — `GET /api/Notification` — View all notifications (after steps 2 and 4).

## 5. Microservice Specifications
| Service | Database | Core Entities & GUIDs | Primary Responsibilities |
|---------|----------|-----------------------|--------------------------|
| **Order Service** | PostgreSQL (`OrderDb`) | Order { Guid Id, int TableNum, string Items, string Status } | Accepts orders, calls Inventory via gRPC, and forwards tasks to Bar Service. |
| **Inventory Service** | PostgreSQL (`InventoryDb`) | Stock { Guid Id, string Ingredient, int Quantity } | Manages drink ingredients and performs synchronous stock checks. |
| **Bar Service** | PostgreSQL (`BarDb`) | DrinkTask { Guid Id, Guid OrderId, string Name, bool IsReady } | Manages the barista's queue. Publishes to Kafka when a drink is ready. |
| **Notification Service** | PostgreSQL (`NotificationDb`) | Log { Guid Id, Guid OrderId, string Message, DateTime SentAt } | Consumes Kafka events and logs notifications for staff. |

## 6. DevOps Requirements
- **VCS**: Git via GitHub.
- **CI/CD**: GitHub Actions for automated build, test (XUnit), and deployment.
- **Static Analysis**: SonarCloud in the CI pipeline.
- **Containerization**: Multi-stage Docker builds for .NET 10.
- **Observability & Monitoring**: OpenTelemetry, Prometheus, Grafana, and Jaeger.

## 7. Local Environment Setup

### Start Everything
```bash
docker-compose up -d
```

### Stop Everything
```bash
docker-compose down -v
```