# Instructions for coding agents

This repository is an API for AI agents. Read this before changing anything.

## Build and test

```bash
dotnet build      # also checks requirement traceability (SpecTrace) and agent skills (SkillLock)
dotnet test
dotnet run --project src/AgenticApi   # http://localhost:5080 · MCP at /mcp · panels at /preview
```

The build must end with **no warnings**. A SpecTrace warning (ST0003, ST0004, ST0005) means a requirement has no code
or no test: fix it, don't suppress it.

## How a change is made

1. **Spec first.** Requirements live in `specs/*.spec.md` (ID, `Status:`, EARS statement, `- AC-n:` criteria).
   Only implement requirements with `Status: approved`. If something is unclear, add an open question to the spec
   (`> Open questions:`) instead of guessing.
2. **One class per endpoint**, in `src/AgenticApi/Features/<Area>/<Operation>/Endpoint.cs`, with its request contract
   in the same file. Copy an existing endpoint (`Features/Loans/Get`) as the starting point.
3. **Trace it.** Put `[Implements(Requirements.XX_000)]` on the code that implements a requirement and
   `[Verifies(Requirements.XX_000, AcceptanceCriteria.XX_000.AC_1)]` on each test. One test per acceptance criterion.
4. **Agents.** To offer an endpoint to agents, call `ExposeAsTool("...")` in its `Configure`. Write the description
   for the model: what it does and what the arguments mean.
   - Anything that changes data must set `tool.Destructive` (true if it overwrites or deletes).
   - Anything irreversible must ask the user: `RequireConfirmation<TRequest>(r => $"...")` with the real values.
   - Lists: set `tool.MaxItems` and `tool.FieldSelection = true`, so the agent does not pay for rows and fields it does
     not need on every later turn.
5. **Panels.** Interactive UI for the chat is a Razor component in `src/AgenticApi/Apps` with `[McpApp("ui://...")]`,
   opened by a tool with `[McpAppUi(ResourceUri = "ui://...")]` (see `ReviewTools.cs`).

## Never

- Remove or weaken `RequireConfirmation`, `Destructive = true` or authorization to make a tool easier to call.
- Change `Status:` of a requirement, or edit `spectrace.lock` / `skills.lock.json`: people review those.
- Add packages from feeds other than nuget.org.
