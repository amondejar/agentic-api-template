using System.Collections.Concurrent;
using System.Text.Json.Serialization;
using AgenticApi.Specs;
using Keel.Endpoints;
using SpecTrace;

namespace AgenticApi.Features.Loans;

/// <summary>Written as text in JSON ("Pending"), for people and agents alike.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<LoanStatus>))]
public enum LoanStatus { Pending, Approved, Rejected }

public sealed record LoanApplication(
    string Id,
    string Applicant,
    decimal Amount,
    int TermMonths,
    string Purpose,
    int RiskScore,
    LoanStatus Status = LoanStatus.Pending,
    string? Reason = null);

/// <summary>The loan applications. An in-memory store standing in for a database.</summary>
public sealed class LoanBook
{
    private readonly ConcurrentDictionary<string, LoanApplication> _loans;

    public LoanBook(IEnumerable<LoanApplication> loans) =>
        _loans = new(loans.Select(loan => KeyValuePair.Create(loan.Id, loan)));

    /// <summary>A few applications to try the API, the agent tools and the review panel with.</summary>
    public static LoanBook WithSampleData() => new(
    [
        new("L-1042", "María López", 12_000m, 36, "Car", 712),
        new("L-1043", "Jordi Puig", 45_000m, 84, "Home renovation", 655),
        new("L-1044", "Lucía Fernández", 3_500m, 12, "Studies", 780),
        new("L-1045", "Andrés Ruiz", 28_000m, 60, "Business equipment", 590),
    ]);

    [Implements(Requirements.LN_001)]
    public IReadOnlyList<LoanApplication> List(LoanStatus? status = null, decimal minAmount = 0) =>
        _loans.Values
            .Where(loan => (status is null || loan.Status == status) && loan.Amount >= minAmount)
            .OrderBy(loan => loan.Id, StringComparer.Ordinal)
            .ToList();

    public LoanApplication? Find(string id) => _loans.GetValueOrDefault(id);

    /// <exception cref="KeyNotFoundException">No application with this ID.</exception>
    /// <exception cref="InvalidOperationException">The application is not pending.</exception>
    [Implements(Requirements.LN_002)]
    public LoanApplication Approve(string id) => Decide(id, LoanStatus.Approved, reason: null);

    /// <exception cref="ArgumentException">No reason.</exception>
    /// <exception cref="KeyNotFoundException">No application with this ID.</exception>
    /// <exception cref="InvalidOperationException">The application is not pending.</exception>
    [Implements(Requirements.LN_003)]
    public LoanApplication Reject(string id, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("A rejection needs a reason.", nameof(reason));
        return Decide(id, LoanStatus.Rejected, reason.Trim());
    }

    private LoanApplication Decide(string id, LoanStatus status, string? reason)
    {
        while (true)
        {
            var loan = Find(id) ?? throw new KeyNotFoundException($"There is no loan application {id}.");
            if (loan.Status != LoanStatus.Pending)
                throw new InvalidOperationException($"Loan application {id} is already {loan.Status.ToString().ToLowerInvariant()}.");

            var decided = loan with { Status = status, Reason = reason };
            if (_loans.TryUpdate(id, decided, loan)) return decided;
        }
    }
}

/// <summary>
/// Applies to every endpoint under Features.Loans. Replace AllowAnonymous with a real policy once authentication
/// is set up, for example options.RequireAuthorization("loan-officers").
/// </summary>
public sealed class LoansGroup : EndpointGroup
{
    public override void Configure(EndpointOptions options) => options.AllowAnonymous();
}
