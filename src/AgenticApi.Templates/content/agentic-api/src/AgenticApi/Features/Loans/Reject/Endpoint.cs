using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Keel.Endpoints;
using Keel.Endpoints.Agents;
using Keel.Endpoints.Mcp;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace AgenticApi.Features.Loans.Reject;

/// <summary>The reason, read from the JSON body.</summary>
public sealed record RejectLoanBody(
    [property: Required, StringLength(500, MinimumLength = 5), Description("Why the application is rejected. The applicant reads it.")] string Reason);

/// <summary>The request contract: the ID from the route and the reason from the body. Validated before the handler runs.</summary>
/// <remarks>
/// Fully qualified: projects with Razor components (the review panel) also get an embedded ValidatableType from the
/// SDK, and the short name would be ambiguous.
/// </remarks>
[BindRequest(RequestBinding.Parameters), Microsoft.Extensions.Validation.ValidatableType]
public sealed record RejectLoan(
    [property: Description("The application ID, for example L-1042.")] string Id,
    [property: FromBody] RejectLoanBody Body);

/// <summary>POST /api/loans/{id}/reject</summary>
[Post("loans/{id}/reject")]
public sealed class Endpoint(LoanBook loans) : ResultEndpoint<RejectLoan, Results<Ok<LoanApplication>, NotFound, ProblemHttpResult>>
{
    // The confirmation shows the real reason, so the user agrees to this rejection, not "a rejection".
    public override void Configure(EndpointOptions options) => options
        .ExposeAsTool("Rejects a pending loan application, with the reason given to the applicant.", tool => tool.Destructive = true)
        .RequireConfirmation<RejectLoan>(request => $"Reject loan application {request.Id}: \"{request.Body.Reason}\"");

    public override Task<Results<Ok<LoanApplication>, NotFound, ProblemHttpResult>> HandleAsync(RejectLoan request, CancellationToken ct)
    {
        Results<Ok<LoanApplication>, NotFound, ProblemHttpResult> result;
        try { result = TypedResults.Ok(loans.Reject(request.Id, request.Body.Reason)); }
        catch (KeyNotFoundException) { result = TypedResults.NotFound(); }
        catch (InvalidOperationException e) { result = TypedResults.Problem(e.Message, statusCode: StatusCodes.Status409Conflict); }
        return Task.FromResult(result);
    }
}
