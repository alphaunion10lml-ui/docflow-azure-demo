namespace DocFlow.Api.Endpoints;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", () => Results.Ok(new { status = "ok", utc = DateTimeOffset.UtcNow }))
           .WithName("Health")
           .WithTags("Health")
           .Produces(StatusCodes.Status200OK);

        return app;
    }
}
