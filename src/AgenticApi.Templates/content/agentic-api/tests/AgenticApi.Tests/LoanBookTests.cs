using AgenticApi.Features.Loans;
using AgenticApi.Tests.Specs;
using SpecTrace;

namespace AgenticApi.Tests;

public class LoanBookTests
{
    private static LoanBook Book(params LoanApplication[] loans) => new(loans);

    private static LoanApplication Loan(string id, decimal amount, LoanStatus status = LoanStatus.Pending) =>
        new(id, "Applicant " + id, amount, 24, "Test", 700, status);

    [Fact]
    [Verifies(Requirements.LN_001, AcceptanceCriteria.LN_001.AC_1)]
    public void List_filters_by_minimum_amount()
    {
        var book = Book(Loan("L-1", 3_500m), Loan("L-2", 12_000m), Loan("L-3", 45_000m));

        Assert.Equal(["L-2", "L-3"], book.List(minAmount: 10_000m).Select(loan => loan.Id));
    }

    [Fact]
    [Verifies(Requirements.LN_001, AcceptanceCriteria.LN_001.AC_2)]
    public void List_filters_by_status()
    {
        var book = Book(Loan("L-1", 1_000m, LoanStatus.Approved), Loan("L-2", 1_000m));

        Assert.Equal(["L-2"], book.List(LoanStatus.Pending).Select(loan => loan.Id));
    }

    [Fact]
    [Verifies(Requirements.LN_002, AcceptanceCriteria.LN_002.AC_1)]
    public void Approve_a_pending_application()
    {
        var book = Book(Loan("L-1", 1_000m));

        Assert.Equal(LoanStatus.Approved, book.Approve("L-1").Status);
        Assert.Equal(LoanStatus.Approved, book.Find("L-1")!.Status);
    }

    [Fact]
    [Verifies(Requirements.LN_002, AcceptanceCriteria.LN_002.AC_2)]
    public void Approve_an_application_that_is_not_pending_is_rejected()
    {
        var book = Book(Loan("L-1", 1_000m, LoanStatus.Rejected));

        Assert.Throws<InvalidOperationException>(() => book.Approve("L-1"));
        Assert.Equal(LoanStatus.Rejected, book.Find("L-1")!.Status);
    }

    [Fact]
    [Verifies(Requirements.LN_003, AcceptanceCriteria.LN_003.AC_1)]
    public void Reject_with_a_reason_keeps_it()
    {
        var book = Book(Loan("L-1", 1_000m));

        var rejected = book.Reject("L-1", "  Income not verified  ");

        Assert.Equal(LoanStatus.Rejected, rejected.Status);
        Assert.Equal("Income not verified", rejected.Reason);
    }

    [Fact]
    [Verifies(Requirements.LN_003, AcceptanceCriteria.LN_003.AC_2)]
    public void Reject_without_a_reason_is_rejected()
    {
        var book = Book(Loan("L-1", 1_000m));

        Assert.Throws<ArgumentException>(() => book.Reject("L-1", " "));
        Assert.Equal(LoanStatus.Pending, book.Find("L-1")!.Status);
    }
}
