# SmartBar Microservices System

## 1. Overview
The SmartBar system is a microservices-based application designed for cafe staff to manage drink orders, inventory, and preparation workflows. The project focuses on implementing modern DevOps practices, including containerization, CI/CD, and observability.

## 2. Architecture & Communication
The system consists of 4 microservices and an API Gateway. It utilizes three types of communication to meet the specification:
- **REST API**: Used for external requests and inter-service commands.
- **gRPC**: High-performance synchronous communication between Order and Inventory services.
- **Kafka (Message Queue)**: Asynchronous event-driven communication for notifications.

## 3. Microservice Specifications
| Service | Database | Core Entities & GUIDs | Primary Responsibilities |
|---------|----------|-----------------------|--------------------------|
| **Order Service** | PostgreSQL (`OrderDb`) | Order { Guid Id, int TableNum, string Items, string Status } | Accepts orders, calls Inventory via gRPC, and forwards tasks to Bar Service. |
| **Inventory Service** | PostgreSQL (`InventoryDb`) | Stock { Guid Id, string Ingredient, int Quantity } | Manages drink ingredients and performs synchronous stock checks. |
| **Bar Service** | PostgreSQL (`BarDb`) | DrinkTask { Guid Id, Guid OrderId, string Name, bool IsReady } | Manages the barista's queue. Publishes to Kafka when a drink is ready. |
| **Notification Service** | PostgreSQL (`NotificationDb`) | Log { Guid Id, Guid OrderId, string Message, DateTime SentAt } | Consumes Kafka events and logs notifications for staff. |

## 4. DevOps Requirements
- **VCS**: Git via GitHub.
- **CI/CD**: GitHub Actions for automated build, test (XUnit), and deployment.
- **Static Analysis**: SonarCloud and StyleCop in the CI pipeline.
- **Containerization**: Multi-stage Docker builds for .NET 10.
- **Observability & Monitoring**: OpenTelemetry, Prometheus, and Grafana.

## local Environment Setup

### Shared Infrastructure
Start the required local infrastructure (PostgreSQL, Kafka, Zookeeper) using Docker Compose:

```bash
docker-compose up -d
```

- **PostgreSQL**: `localhost:5432` (User: admin / Pass: barpassword123)
- **Kafka**: `localhost:29092` (from host), `kafka:9092` (from other containers)