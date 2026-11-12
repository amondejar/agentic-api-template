# AgenticApi

An ASP.NET Core API built for AI agents, created with `dotnet new agentic-api`. It manages loan applications, a
sample domain you replace with yours. Everything around it is the part to keep:

| | What it gives you | Where |
|---|---|---|
| **[Keel.Endpoints](https://github.com/amondejar/keel-endpoints)** | One class per endpoint. Each one can be offered to agents as an MCP tool, and destructive tools run only after the user confirms | `src/AgenticApi/Features` |
| **[McpRazor](https://github.com/amondejar/mcp-razor)** | Interactive panels inside Claude and Copilot, written as Razor components | `src/AgenticApi/Apps` |
| **[SpecTrace](https://github.com/amondejar/spec-trace)** | Requirements in Markdown, traced to the code and tests. The build warns when one is missing | `specs/` |
| **[SkillLock](https://github.com/amondejar/skill-lock)** | Checks the agent skills your NuGet packages bring in, on every build | `Directory.Build.props` |
| **Agent-ready repo** | `AGENTS.md`, Copilot setup steps, MCP configuration for Claude Code and VS Code | root, `.github/` |

## Run it

```bash
dotnet run --project src/AgenticApi
```

- API: `http://localhost:5080/api/loans`
- MCP server for agents: `http://localhost:5080/mcp`
- Panels in a browser, without an AI client: `http://localhost:5080/preview`

## Connect an agent

**Claude Code**: `.mcp.json` is already here. Start `claude` in this folder and approve the `agenticapi` server.

**VS Code with GitHub Copilot**: `.vscode/mcp.json` is already here. Open the folder, start the server from the MCP
view, and turn on `chat.mcp.apps.enabled` to see the panels.

**Claude Desktop**: Settings → Connectors → Add custom connector → `http://localhost:5080/mcp` (or a public HTTPS
URL, see below).

Then try:

- *"Which loan applications are pending over 20 000 €?"* → the agent calls `loans_list`. Results are lean: no null
  fields, at most 20 applications (the agent is told how to narrow the call), and only the fields it asks for.
- *"Let me review the pending loans"* → `review_loans` opens the review panel. You approve or reject with a click.
- *"Reject L-1043, the debt ratio is above 40 %"* → the agent calls `loans_reject`, and **you** confirm. If your client
  cannot show a confirmation, the agent gives you a link to approve it at `/approvals/...`.

## Add a feature

The same way the agent will (see `AGENTS.md`):

1. Write the requirement in `specs/loans.spec.md` (or a new `*.spec.md`) with `Status: approved`.
   `dotnet build` now warns that nothing implements it.
2. Add `src/AgenticApi/Features/<Area>/<Operation>/Endpoint.cs`, mark it `[Implements(Requirements.XX_000)]` and,
   for agents, `ExposeAsTool(...)`.
3. Add a test per acceptance criterion with `[Verifies(...)]`. The build is clean again.

`LN-004` (reopen a rejected application) is waiting in the spec as `proposed`. Answer its open questions, approve it,
and hand it to your coding agent.

## Before production

- **Authentication**: endpoints are anonymous (`LoansGroup`). Add authentication and require it on the API and on
  `/mcp`. Keel authorizes every tool call with its endpoint's policies.
- **Storage**: `LoanBook` keeps data in memory. Replace it with your database.
- **Several instances**: share the Data Protection key ring (it signs the confirmations) and register an
  `IAgentApprovalStore` backed by a database.
- **Remote MCP clients** (claude.ai, Claude Desktop custom connectors) need a public HTTPS URL: deploy it, or use a
  tunnel such as `devtunnel host -p 5080 --allow-anonymous` while you try it.

## Commands

```bash
dotnet tool restore               # spectrace and skilllock, pinned in .config/dotnet-tools.json
dotnet spectrace report           # requirement → code → test matrix
dotnet spectrace lock             # record the reviewed version of each requirement
dotnet skilllock scan             # what the agent skills in your packages ask agents to do
dotnet skilllock lock             # approve them; the build then flags any skill a package update changes
```
