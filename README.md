<p align="center"><img src="https://raw.githubusercontent.com/amondejar/agentic-api-template/main/icon.png" width="96" alt=""></p>

# Agentic API template

**`dotnet new agentic-api`: an ASP.NET Core API ready for AI agents, in one command.**

Agents can call your API as MCP tools, but the parts around it take weeks to get right: tools that can't do damage
without the user, UI inside the chat, requirements the agent can follow, and a repo that coding agents work in
without guessing. This template puts them together, working, with a sample domain you replace with yours.

```bash
dotnet new install Amondejar.Templates.AgenticApi
dotnet new agentic-api -n Lending
cd Lending
dotnet run --project src/Lending
```

Then connect Claude Code (`.mcp.json` is already there) or VS Code with Copilot (`.vscode/mcp.json`), and ask:
*"Let me review the pending loans"*.

## What you get

| | |
|---|---|
| **API endpoints as MCP tools** | One class per endpoint with [Keel.Endpoints](https://github.com/amondejar/keel-endpoints). `ExposeAsTool` offers it to agents at `/mcp`, with authorization per tool |
| **Nothing irreversible without the user** | Approve and reject are destructive: when an agent calls them, the user confirms first. Clients that cannot show a confirmation get an approval link |
| **UI inside the chat** | `review_loans` opens a review panel in Claude and Copilot, written as a Razor component with [McpRazor](https://github.com/amondejar/mcp-razor). The person decides with a click |
| **Specs the agent follows** | Requirements in `specs/loans.spec.md`, traced to code (`[Implements]`) and tests (`[Verifies]`) with [SpecTrace](https://github.com/amondejar/spec-trace). Approve a requirement and the build says what is missing |
| **Safe agent skills** | [SkillLock](https://github.com/amondejar/skill-lock) checks the agent skills your NuGet packages bring in, on every build |
| **A repo for coding agents** | `AGENTS.md` with the rules, Copilot setup steps (the coding agent doesn't burn credits restoring packages), CI with traceability report |

```text
Lending/
├─ specs/loans.spec.md              requirements LN-001…LN-004 (LN-004 waits as "proposed")
├─ src/Lending/
│  ├─ Features/Loans/               List · Get · Approve · Reject, one endpoint class each
│  └─ Apps/                         LoanReviewApp.razor + the review_loans tool
├─ tests/Lending.Tests/             one test per acceptance criterion, API and MCP tests
├─ AGENTS.md                        how coding agents work in this repo
├─ .mcp.json · .vscode/mcp.json     Claude Code and VS Code connect in one step
└─ .github/workflows/               CI and copilot-setup-steps
```

## Options

| Option | Default | |
|---|---|---|
| `--port <n>` | `5080` | HTTP port in development; the MCP configuration files and the README follow it |
| `--openapi <bool>` | `true` | OpenAPI document and the Scalar API reference at `/scalar` |
| `--no-restore` | | Skip the restore after creating the solution |

## How it is tested

Every change to the template packs it, installs it and creates solutions with it. Each solution must build with
**warnings as errors** (so SpecTrace finds every requirement implemented and tested), pass its tests, report the
missing implementation when a proposed requirement is approved, and then pass 28 checks of an AI agent driving the
running API over MCP: tool list and annotations, filtered queries, the review panel rendered and clicked, and
destructive tools that never run without the user. See [tests/run.sh](tests/run.sh) and
[tests/mcp-agent.mjs](tests/mcp-agent.mjs).

## Requirements

.NET 10 SDK. The generated solution uses preview versions of Keel.Endpoints, McpRazor, SpecTrace and SkillLock.

## License

MIT
