namespace DocFlow.Api.Endpoints;

public static class EndpointExtensions
{
    public static IEndpointRouteBuilder MapApiEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapHealthEndpoints();
        app.MapFileEndpoints();
        app.MapHomeEndpoints();

        return app;
    }
}
