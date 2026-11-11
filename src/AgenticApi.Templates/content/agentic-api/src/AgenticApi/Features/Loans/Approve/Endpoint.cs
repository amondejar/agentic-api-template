using System.ComponentModel;
using Keel.Endpoints;
using Keel.Endpoints.Agents;
using Keel.Endpoints.Mcp;
using Microsoft.AspNetCore.Http.HttpResults;

namespace AgenticApi.Features.Loans.Approve;

/// <summary>The request contract: the ID comes from the route, so it binds as parameters (POST has no body here).</summary>
[BindRequest(RequestBinding.Parameters)]
public sealed record ApproveLoan([property: Description("The application ID, for example L-1042.")] string Id);

/// <summary>POST /api/loans/{id}/approve</summary>
[Post("loans/{id}/approve")]
public sealed class Endpoint(LoanBook loans) : ResultEndpoint<ApproveLoan, Results<Ok<LoanApplication>, NotFound, ProblemHttpResult>>
{
    // Approving changes the application for good: when an agent calls it, the user confirms first.
    public override void Configure(EndpointOptions options) => options
        .ExposeAsTool("Approves a pending loan application.", tool => tool.Destructive = true)
        .RequireConfirmation<ApproveLoan>(request =>
            loans.Find(request.Id) is { } loan
                ? $"Approve loan application {loan.Id}: {loan.Applicant}, {loan.Amount:N0} EUR over {loan.TermMonths} months"
                : $"Approve loan application {request.Id}");

    public override Task<Results<Ok<LoanApplication>, NotFound, ProblemHttpResult>> HandleAsync(ApproveLoan request, CancellationToken ct)
    {
        Results<Ok<LoanApplication>, NotFound, ProblemHttpResult> result;
        try { result = TypedResults.Ok(loans.Approve(request.Id)); }
        catch (KeyNotFoundException) { result = TypedResults.NotFound(); }
        catch (InvalidOperationException e) { result = TypedResults.Problem(e.Message, statusCode: StatusCodes.Status409Conflict); }
        return Task.FromResult(result);
    }
}
