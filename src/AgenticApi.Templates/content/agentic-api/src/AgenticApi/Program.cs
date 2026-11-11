using AgenticApi.Apps;
using AgenticApi.Features.Loans;
using Keel.Endpoints;
using Keel.Endpoints.Agents;
using Keel.Endpoints.Mcp;
using McpRazor;
#if (openapi)
using Scalar.AspNetCore;
#endif

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddKeelEndpoints();   // fail-closed: every endpoint must declare who may call it
builder.Services.AddValidation();      // .NET 10 validation for request contracts
builder.Services.AddProblemDetails();  // RFC 9457 error responses
#if (openapi)
builder.Services.AddOpenApi();
#endif
builder.Services.AddSingleton(LoanBook.WithSampleData());   // replace with your database

// AI agents. The endpoints that call ExposeAsTool become MCP tools at /mcp, and the review_loans tool opens
// an interactive panel (a Razor component) inside Claude, Copilot and other MCP Apps hosts.
builder.Services.AddKeelMcp()
    .WithTools<ReviewTools>()
    .WithRazorApps(apps => apps.AddAppsFromAssembly(typeof(ReviewTools).Assembly));

// Destructive tools (approve, reject) run only after the user confirms. Clients that cannot show a
// confirmation get an approval link instead (/approvals/{id}).
builder.Services.AddKeelAgents(options => options.WhenClientCannotConfirm = ConfirmationFallback.Approval);

// Add authentication and authorization here, then require it for the whole API:
// app.MapKeelEndpoints("/api").RequireAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
#if (openapi)
    app.MapOpenApi();                                   // /openapi/v1.json
    app.MapScalarApiReference();                        // /scalar
#endif
    app.MapMcpRazorPreview("/preview", mcpPath: "/mcp"); // try the panels in a browser
}

app.MapKeelEndpoints("/api");
app.MapKeelMcp("/mcp");      // Claude Code: claude mcp add --transport http agenticapi http://localhost:5080/mcp
app.MapKeelApprovals();      // the approval pages

app.Run();

/// <summary>Visible to the tests' WebApplicationFactory.</summary>
public partial class Program;
