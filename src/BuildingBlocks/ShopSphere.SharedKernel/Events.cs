namespace ShopSphere.SharedKernel.Events;

public record OrderCreated(Guid OrderId, string UserId, decimal Total, DateTimeOffset CreatedAt);
public record PaymentSucceeded(Guid OrderId, string PaymentId, DateTimeOffset PaidAt);
public record PaymentFailed(Guid OrderId, string Reason, DateTimeOffset FailedAt);
