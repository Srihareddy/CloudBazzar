# Architecture (high level)

- Gateway routes requests to microservices.
- Services own their data (separate databases).
- Asynchronous events via RabbitMQ:
  - Ordering publishes OrderCreated
  - Payment consumes and publishes PaymentSucceeded/PaymentFailed
  - Notification consumes payment events
