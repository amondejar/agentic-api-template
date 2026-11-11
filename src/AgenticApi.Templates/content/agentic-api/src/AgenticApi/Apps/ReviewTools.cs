using System.ComponentModel;
using AgenticApi.Features.Loans;
using ModelContextProtocol.Extensions.Apps;
using ModelContextProtocol.Server;

namespace AgenticApi.Apps;

/// <summary>What review_loans returns: the panel's data, and the structured content the model reads.</summary>
public sealed record LoanQueue(IReadOnlyList<LoanApplication> Loans, string Currency = "EUR");

/// <summary>
/// MCP tools that open panels. A panel is a Razor component (see LoanReviewApp.razor) rendered by McpRazor
/// inside the chat. In the panel the user decides; the agent only opens it and reads the outcome.
/// </summary>
[McpServerToolType]
public sealed class ReviewTools(LoanBook loans)
{
    [McpServerTool(Name = "review_loans", ReadOnly = true, UseStructuredContent = true)]
    [McpAppUi(ResourceUri = "ui://loans/review")]
    [Description("Shows the pending loan applications in an interactive panel where the user approves or rejects each one.")]
    public LoanQueue ReviewLoans(
        [Description("Only show applications of at least this amount, in euros.")] decimal minAmount = 0) =>
        new(loans.List(LoanStatus.Pending, minAmount));
}
