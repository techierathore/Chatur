# Chatur
My AI-first IDE , the visual enablement of Techieflow 

## The agents and their commands

Chatur has four agents. Type a command with its star in the agent's conversation.

- **Analyst**: `*help` · `*day1-brownfield {App}` · `*day1-greenfield {App}` · `*mockups {App} [--update]` · `*split-brd {App}` · `*brainstorm {topic}` · `*amend-docs {App} {change}` · `*create-project-brief {App}` · `*doc-out` · `*research-prompt {topic}` · `*yolo` · `*exit`
- **Architect**: `*help` · `*doc-out` · `*research {topic}` · `*yolo` · `*exit` · `*architect {App}` · `*create-doc {document}`
- **Flow master**: `*develop-end-to-end {App} [{brief}] [--no-git] [--model <id>] | --follow | --stop | --resume` · `*develop-status {App}` · `*estimate-phase {App}` · `*build-phase {App}` · `*fix-issues {App} {folder}` · `*triage-issues {App} {evidence} [verify]` · `*log-miss {App} "sentence" [--fixed]` · `*triage-and-fix {App} {evidence}` · `*amend-docs {App} {change}` · `*devguide {App} [--update]` · `*productguide {App}` · `*handoff-phase {App}` · `*deploy-checklist {App} {pipeline-document}` · `*refresh-status {App} [verify]` · `*metrics {App}` · `*generate-html <path>` · `*render-workflow-docs {App}` · `*yolo` · `*help` · `*exit`
- **Verifier**: `*verify {scope} {App} [--phase N|all]` · `*report {scope} {App}` · `*help` · `*exit` · `*triage-issues {evidence}` · `*metrics-report` · `*log-miss "sentence" [--fixed]`

What each command does, an example to type and where the result appears: [The agents and their commands](docs/Chatur-UsageGuide.md#the-agents-and-their-commands).
