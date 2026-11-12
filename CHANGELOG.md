# Changelog

## 0.1.0-preview.2

- Keel.Endpoints 1.0.0-rc.8: lean tool results. `loans_list` returns at most 20 applications (the agent is told how
  to narrow the call) and, like `loans_get`, takes a `fields` argument; the model's text has no null fields. Optional
  filters are no longer required in the tool schemas.
- AGENTS.md: list tools set `MaxItems` and `FieldSelection`.

## 0.1.0-preview.1

First release of `dotnet new agentic-api`, published as `Amondejar.Templates.AgenticApi`.

- Keel.Endpoints API (list, get, approve, reject loan applications), every endpoint offered to agents as an MCP tool
  with parameter descriptions; approve and reject need the user's confirmation, with an approval link as fallback.
- McpRazor review panel (`review_loans`), with the person's decisions shared with the model.
- SpecTrace spec with three approved requirements, implemented and tested, and one proposed.
- SkillLock on every build.
- `AGENTS.md`, Copilot instructions and setup steps, MCP configuration for Claude Code and VS Code, CI.
- Options: `--port`, `--openapi`, `--no-restore`.
