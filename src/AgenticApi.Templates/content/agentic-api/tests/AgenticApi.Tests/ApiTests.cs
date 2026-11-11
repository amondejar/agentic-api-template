using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using AgenticApi.Features.Loans;
using AgenticApi.Tests.Specs;
using Microsoft.AspNetCore.Mvc.Testing;
using SpecTrace;

namespace AgenticApi.Tests;

/// <summary>The API over HTTP, and the MCP server agents connect to.</summary>
public class ApiTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    [Verifies(Requirements.LN_001)]
    public async Task Lists_pending_loans()
    {
        var loans = await factory.CreateClient().GetFromJsonAsync<List<LoanApplication>>("/api/loans?status=Pending", Json);

        Assert.NotNull(loans);
        Assert.All(loans, loan => Assert.Equal(LoanStatus.Pending, loan.Status));
    }

    [Fact]
    [Verifies(Requirements.LN_003, AcceptanceCriteria.LN_003.AC_2)]
    public async Task Rejecting_without_a_reason_is_a_validation_error()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/loans/L-1045/reject", new { reason = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Agents_see_the_loan_tools_and_the_review_panel()
    {
        using var result = await Mcp("tools/list", new { });
        var tools = result.RootElement.GetProperty("result").GetProperty("tools").EnumerateArray()
            .ToDictionary(tool => tool.GetProperty("name").GetString()!);

        Assert.Contains("loans_list", tools.Keys);
        Assert.True(tools["loans_reject"].GetProperty("annotations").GetProperty("destructiveHint").GetBoolean());
        Assert.Equal("ui://loans/review", tools["review_loans"].GetProperty("_meta").GetProperty("ui").GetProperty("resourceUri").GetString());
    }

    [Fact]
    public async Task An_agent_cannot_reject_without_the_user()
    {
        // This client cannot ask the user, so the server answers with an approval link and does nothing.
        using var result = await Mcp("tools/call", new { name = "loans_reject", arguments = new { id = "L-1043", body = new { reason = "Debt ratio above 40 %" } } });

        Assert.True(result.RootElement.GetProperty("result").GetProperty("isError").GetBoolean());
        Assert.Equal(LoanStatus.Pending, (await factory.CreateClient().GetFromJsonAsync<LoanApplication>("/api/loans/L-1043", Json))!.Status);
    }

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() } };

    private async Task<JsonDocument> Mcp(string method, object parameters)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/mcp")
        {
            Content = new StringContent(JsonSerializer.Serialize(new { jsonrpc = "2.0", id = 1, method, @params = parameters }), Encoding.UTF8, "application/json"),
        };
        request.Headers.Accept.ParseAdd("application/json");
        request.Headers.Accept.ParseAdd("text/event-stream");
        request.Headers.Add("MCP-Protocol-Version", "2025-11-25");

        var response = await factory.CreateClient().SendAsync(request);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        // Streamable HTTP may answer as a server-sent event: take the data line.
        var json = body.TrimStart().StartsWith('{') ? body : string.Join("", body.Split('\n').Where(l => l.StartsWith("data:")).Select(l => l[5..]));
        return JsonDocument.Parse(json);
    }
}
