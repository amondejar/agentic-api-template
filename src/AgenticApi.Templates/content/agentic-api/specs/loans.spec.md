---
spec: LN
title: Loan applications
---
# Loan applications

What the API does with loan applications. Each requirement has an ID, a statement in EARS form and numbered
acceptance criteria. Code that implements a requirement carries `[Implements(Requirements.LN_00x)]`; the tests that
check it carry `[Verifies(...)]`. The build warns about approved requirements without code or tests.

## LN-001: List loan applications
Status: approved

When a reviewer or an agent asks for loan applications, the system shall return them ordered by ID,
filtered by status and minimum amount when given.

- AC-1: Given applications of 3 500, 12 000 and 45 000 EUR, when the list is filtered by a minimum of 10 000, then only the last two are returned.
- AC-2: Given an approved and a pending application, when the list is filtered by Pending, then only the pending one is returned.

## LN-002: Approve a loan application
Status: approved

While an application is Pending, when a reviewer approves it, the system shall mark it Approved.

- AC-1: Given a Pending application, when it is approved, then its status is Approved.
- AC-2: Given an application that is not Pending, when it is approved, then the request is rejected and the application does not change.

## LN-003: Reject a loan application with a reason
Status: approved

While an application is Pending, when a reviewer rejects it with a reason, the system shall mark it Rejected
and keep the reason.

- AC-1: Given a Pending application, when it is rejected with a reason, then its status is Rejected and the reason is kept.
- AC-2: Given a Pending application, when it is rejected without a reason, then the request is rejected and the application does not change.

## LN-004: Reopen a rejected application
Status: proposed

> Open questions:
> - Who may reopen an application: the same reviewer or a supervisor?
> - Is there a time limit after the rejection?

When a supervisor reopens a Rejected application, the system shall move it back to Pending and keep the
previous decision in its history.

- AC-1: Given a Rejected application, when it is reopened, then its status is Pending.
