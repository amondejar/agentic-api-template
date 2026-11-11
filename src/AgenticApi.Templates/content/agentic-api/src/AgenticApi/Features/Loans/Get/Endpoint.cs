using System.ComponentModel;
using Keel.Endpoints;
using Keel.Endpoints.Mcp;
using Microsoft.AspNetCore.Http.HttpResults;

namespace AgenticApi.Features.Loans.Get;

/// <summary>The request contract, bound from the route (GET).</summary>
public sealed record GetLoan([property: Description("The application ID, for example L-1042.")] string Id);

/// <summary>GET /api/loans/{id}</summary>
[Get("loans/{id}")]
public sealed class Endpoint(LoanBook loans) : ResultEndpoint<GetLoan, Results<Ok<LoanApplication>, NotFound>>
{
    public override void Configure(EndpointOptions options) =>
        options.ExposeAsTool("Gets a loan application by its ID, for example L-1042.");

    public override Task<Results<Ok<LoanApplication>, NotFound>> HandleAsync(GetLoan request, CancellationToken ct) =>
        Task.FromResult<Results<Ok<LoanApplication>, NotFound>>(
            loans.Find(request.Id) is { } loan ? TypedResults.Ok(loan) : TypedResults.NotFound());
}
