using MediatR;
using ProductOrderingSystem.PaymentService.Application.Commands;
using ProductOrderingSystem.PaymentService.Application.Queries;

namespace ProductOrderingSystem.PaymentService.WebAPI.Endpoints;

public static class PaymentEndpoints
{
    public static void MapPaymentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/payments")
            .WithTags("Payments")
            .RequireAuthorization();

        group.MapGet("/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var query = new GetPaymentByIdQuery(id);
            var result = await mediator.Send(query);

            return result.Match(
                payment => Results.Ok(payment),
                errors => Results.Problem(
                    statusCode: errors[0].Type == ErrorOr.ErrorType.NotFound ? 404 : 400,
                    title: string.Join(", ", errors.Select(e => e.Description))));
        })
        .WithName("GetPaymentById")
        .WithDescription("Get a payment by ID");

        group.MapGet("/order/{orderId:guid}", async (Guid orderId, IMediator mediator) =>
        {
            var query = new GetPaymentsByOrderIdQuery(orderId);
            var result = await mediator.Send(query);

            return result.Match(
                payments => Results.Ok(payments),
                errors => Results.Problem(
                    statusCode: 400,
                    title: string.Join(", ", errors.Select(e => e.Description))));
        })
        .WithName("GetPaymentsByOrderId")
        .WithDescription("Get all payments for an order");

        group.MapPost("/{id:guid}/refund", async (
            Guid id,
            RefundRequest request,
            IMediator mediator) =>
        {
            var command = new RefundPaymentCommand(id, request.Reason);
            var result = await mediator.Send(command);

            return result.Match(
                _ => Results.NoContent(),
                errors => Results.Problem(
                    statusCode: errors[0].Type == ErrorOr.ErrorType.NotFound ? 404 : 400,
                    title: string.Join(", ", errors.Select(e => e.Description))));
        })
        .WithName("RefundPayment")
        .WithDescription("Refund a payment");

        // Note: ProcessPayment is triggered by OrderCreatedEvent consumer, not directly via API
        // But we can expose it for testing purposes
        group.MapPost("/process", async (
            ProcessPaymentRequest request,
            IMediator mediator) =>
        {
            var command = new ProcessPaymentCommand(
                request.OrderId,
                request.UserId,
                request.Amount,
                request.Currency);
            
            var result = await mediator.Send(command);

            return result.Match(
                payment => Results.Ok(payment),
                errors => Results.Problem(
                    statusCode: 400,
                    title: string.Join(", ", errors.Select(e => e.Description))));
        })
        .WithName("ProcessPayment")
        .WithDescription("Manually process a payment (for testing)");
    }

    public record RefundRequest(string Reason);
    
    public record ProcessPaymentRequest(
        Guid OrderId,
        Guid UserId,
        decimal Amount,
        string Currency);
}
