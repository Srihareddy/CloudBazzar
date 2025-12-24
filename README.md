# CloudBazzar


## What’s inside
- **Microservices**: Catalog, Basket, Ordering, Payment, Notification
- **API Gateway**: YARP reverse proxy
- **Datastores**: PostgreSQL (Catalog), SQL Server (Ordering), Redis (Basket)
- **Messaging**: RabbitMQ event bus (publish/subscribe)
- **Observability**: OpenTelemetry (traces), Serilog → Seq, health checks
- **Testing**: xUnit (starter tests) + basic integration test pattern
- **CI**: GitHub Actions build + test
- **Containers**: Dockerfiles + docker-compose (one command runs everything)

## Quick start (Docker)
Prereqs: Docker Desktop

```bash
cd deploy/docker-compose
docker compose up --build
```

Then open:
- Gateway: http://localhost:8080
- Catalog via gateway: http://localhost:8080/catalog/swagger
- Basket via gateway: http://localhost:8080/basket/swagger
- Ordering via gateway: http://localhost:8080/ordering/swagger
- Payment: http://localhost:5004/swagger
- RabbitMQ UI: http://localhost:15672 (guest/guest)
- Seq logs: http://localhost:5341

## Auth (Dev)
A simple **dev JWT issuer** is included:
- Identity API: `POST http://localhost:5006/token`
- Use the returned JWT as `Authorization: Bearer <token>` for Ordering endpoints.

> For production-grade OAuth2/OIDC, swap Identity.API for OpenIddict or Duende IdentityServer.

## Event flow
- Ordering publishes `OrderCreated`
- Payment consumes `OrderCreated` → publishes `PaymentSucceeded` or `PaymentFailed`
- Notification consumes payment events

## Folder layout
See `src/` for services and `BuildingBlocks/` for shared infrastructure.
