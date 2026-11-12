using System.ComponentModel;
using Keel.Endpoints;
using Keel.Endpoints.Mcp;
using Microsoft.AspNetCore.Http.HttpResults;

namespace AgenticApi.Features.Loans.List;

/// <summary>The request contract, bound from the query string (GET).</summary>
public sealed record ListLoans(
    [property: Description("Only applications in this status.")] LoanStatus? Status = null,
    [property: Description("Only applications of at least this amount, in euros.")] decimal? MinAmount = null);

/// <summary>GET /api/loans?status=Pending&amp;minAmount=10000</summary>
[Get("loans")]
public sealed class Endpoint(LoanBook loans) : ResultEndpoint<ListLoans, Ok<IReadOnlyList<LoanApplication>>>
{
    // Lean results: a list costs tokens on every later turn of the conversation, so the agent gets at most 20
    // applications (and is told how to narrow the call) and can ask for some fields only.
    public override void Configure(EndpointOptions options) =>
        options.ExposeAsTool("Lists loan applications ordered by ID, optionally filtered by status (Pending, Approved, Rejected) and a minimum amount in euros.", tool =>
        {
            tool.MaxItems = 20;
            tool.FieldSelection = true;
        });

    public override Task<Ok<IReadOnlyList<LoanApplication>>> HandleAsync(ListLoans request, CancellationToken ct) =>
        Task.FromResult(TypedResults.Ok(loans.List(request.Status, request.MinAmount ?? 0)));
}
