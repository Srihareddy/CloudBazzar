# Runbook (on-call simulation)

## Common checks
- Gateway up? http://localhost:8080
- Health endpoints:
  - Catalog: http://localhost:5001/health
  - Basket: http://localhost:5002/health
  - Ordering: http://localhost:5003/health
- RabbitMQ: http://localhost:15672 (guest/guest)
- Logs: Seq http://localhost:5341

## Incident: Ordering cannot create order (DB)
1. Check Ordering logs in Seq
2. Verify SQL Server container is healthy and listening on 1433
3. Restart ordering service:
   `docker compose restart orderingapi`

## Incident: Events not flowing
1. Check RabbitMQ queues and bindings
2. Confirm Payment + Notification containers are running
3. Restart consumers:
   `docker compose restart paymentapi notificationworker`
