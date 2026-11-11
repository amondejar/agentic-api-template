# Changelog

## 0.1.0-preview.1

First release of `dotnet new agentic-api`, published as `Amondejar.Templates.AgenticApi`.

- Keel.Endpoints API (list, get, approve, reject loan applications), every endpoint offered to agents as an MCP tool
  with parameter descriptions; approve and reject need the user's confirmation, with an approval link as fallback.
- McpRazor review panel (`review_loans`), with the person's decisions shared with the model.
- SpecTrace spec with three approved requirements, implemented and tested, and one proposed.
- SkillLock on every build.
- `AGENTS.md`, Copilot instructions and setup steps, MCP configuration for Claude Code and VS Code, CI.
- Options: `--port`, `--openapi`, `--no-restore`.
