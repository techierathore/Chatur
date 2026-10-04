# TechieRag feedback — found while building Chatur

| | |
|---|---|
| App | Chatur |
| Upstream | TechieRag |
| Updated | 2026-09-30 (TR-RAG-003 added) |

## Summary

5 entries: 0 blocking now, 0 open, 5 fixed upstream (2 in TechieRag 1.0.8, 3 in 1.0.9).

Nothing is blocked. Chatur is on 1.0.9; OpenCode Go answers through Chatur (checked live 2026-10-01).

## Entries

### TR-RAG-003 — An OpenAI-compatible model cannot be sent extra request headers

- **Status:** closed 2026-10-01 — fixed upstream in TechieRag 1.0.9 (`UseOpenAICompatibleLlm(…, headers, sessionHeader)`, `LlmCompletionOptions.SessionId`, `opencode-go` connector). Chatur sends `User-Agent: Chatur/<version>` and the conversation id; OpenCode Go replied live.
- **Severity:** blocker
- **Blocks:** yes — the owner chose OpenCode Go (decision 1, option A); every conversation row that
  needs a real reply waits on it (REQ-FN-021, 022, 025–027, 033–036, REQ-UI-021, 022, 032, 033).
  Key-only providers are not affected.
- **Repro:** 1.0.7 and 1.0.8. `UseOpenAICompatibleLlm("https://opencode.ai/zen/go/v1", key, "kimi-k2.7-code")`
  then `ChatAsync` → HTTP 400 `MissingSessionID` ("Request is missing x-opencode-session"). The same
  request by hand with that header answers 200 (2026-09-30, owner's key).
- **Expected:** Extra request headers on an OpenAI-compatible model, including one that stays the
  same for a conversation (https://opencode.ai/docs/go/).
- **Actual:** No header option on `UseOpenAICompatibleLlm` or `LlmConfig`; the `HttpClient`
  constructor of `OpenAICompatibleLlmProvider` is internal; no `opencode` catalog entry.
- **Encountered in:** REQ-FN-025
- **Workaround:** None; the rows stay blocked.
- **Suggested fix:** `LlmConfig.Headers` (like `McpServerConfig.Headers`) plus a per-call session id
  in `LlmCompletionOptions`; or a public `HttpClient` constructor; or an `opencode-go` catalog entry.

### TR-RAG-004 — The ChatGPT subscription connector's name is not public

- **Status:** closed 2026-10-01 — fixed upstream in TechieRag 1.0.9 (`LlmConnectorCatalog.ChatGptSubscriptionName`); Chatur now uses it.
- **Severity:** minor
- **Blocks:** no — Chatur hardcodes the name and reads the terms through the public `LlmConnectorCatalog.Find`. The work carried on.
- **Repro:** TechieRag 1.0.8. `SubscriptionConnectorRows` and its `ChatGptName` are internal.
- **Expected:** A public constant for the connector key, e.g. `LlmConnectorCatalog.ChatGptSubscriptionName`.
- **Actual:** A host must write `"chatgpt-subscription"` itself and hope it never changes.
- **Encountered in:** REQ-FN-016
- **Workaround:** `ProviderActions.ChatGptCatalogName = "chatgpt-subscription"`, named after this entry.
- **Suggested fix:** Make `SubscriptionConnectorRows` (or just its names) public.

### TR-RAG-005 — No clear way to say "the subscription session is gone, sign in again" during a model turn

- **Status:** closed 2026-10-01 — fixed upstream in TechieRag 1.0.9 (`SubscriptionSignInException.CodeSignInRequired`); Chatur turns it into "not signed in any more, sign in again".
- **Severity:** minor
- **Blocks:** no — Chatur's sign-in callback throws `InvalidOperationException` during a turn, and the turn tells the owner to sign in again under Settings ▸ Providers.
- **Repro:** TechieRag 1.0.8. `UseChatGptSubscriptionLlm` needs a sign-in callback; during a model turn a host has no window to show a code in.
- **Expected:** A documented exception (or a documented rule that the callback may throw) meaning "not signed in any more", that the host can catch and turn into a "sign in again" message.
- **Actual:** Nothing documents what TechieRag does when the callback throws.
- **Encountered in:** REQ-FN-016
- **Workaround:** As above; not confirmed against TechieRag's own handling.
- **Suggested fix:** A `SubscriptionSignInException` code such as `CodeSignInRequired`, raised when a stored session is refused and no interactive callback is available.

### TR-RAG-001 — No browser-based subscription sign-in for any connector

- **Status:** closed 2026-09-30 — fixed upstream in TechieRag 1.0.8 (`UseChatGptSubscriptionLlm`, device-code sign-in, `ISubscriptionSessionStore`)
- **Severity:** blocker
- **Blocks:** yes — REQ-FN-016, "Sign in to a ChatGPT subscription" (Settings ▸ Model providers)
- **Repro:** `.techierag/TechieRag-AI-Reference.md` — every `TechieRagBuilder` LLM method
  (`UseAnthropicLlm`, `UseOpenAICompatibleLlm`, `UseGeminiLlm`, `UseOllamaLlm`, `UseLmStudioLlm`,
  `UseAzureAIFoundryLlm`) takes a pasted key, a local endpoint, or nothing — none opens a browser for
  an OAuth-style subscription sign-in.
- **Expected:** A way to start a browser sign-in flow for a subscription-based source (e.g. a ChatGPT
  Plus/Pro account) and receive back a token or session TechieRag can then use for completions.
- **Actual:** The only "no key" path is a local server (Ollama/LM Studio) with no sign-in step at
  all. There is no `LlmSource` value, builder method, or documented flow for a hosted subscription.
- **Encountered in:** REQ-FN-016
- **Workaround:** None. `Chatur.Core.Models.ProviderActions.AddSubscriptionAsync` throws
  `NotSupportedException` naming this entry; the checklist row is `Blocked`, not worked around.
- **Suggested fix:** A builder method such as `UseChatGptSubscriptionLlm(deviceCodeCallback)` (or any
  browser/device-code OAuth flow) that yields an `ILlmProvider` once the flow completes, so a host
  application can drive the browser step itself and hand the result to TechieRag.

#### Detail

Chatur's other two provider kinds work today: `AddWithKeyAsync` (pasted key — `UseAnthropicLlm` /
`UseOpenAICompatibleLlm` / `UseGeminiLlm`) and `AddLocalAsync` (`UseOllamaLlm`, no sign-in). Only the
third kind the mockup (`mockups/settings-providers.html`, `data-testid="signin-browser"`) shows is
unreachable through TechieRag. Everything else about Chatur's provider/routing/model layer
(REQ-FN-014, 015, 017–023) is unaffected and built.

### TR-RAG-002 — `ChatStreamAsync` cannot surface a tool call, so a tool-using turn can't stream

- **Status:** closed 2026-09-30 — fixed upstream in TechieRag 1.0.8 (`ChatStreamEventsAsync` yields `TextDelta`, `ToolCall` and `Completed`)
- **Severity:** minor
- **Blocks:** no — the agent loop calls `ChatAsync` (non-streaming) whenever tools are offered, so
  every tool call still passes Chatur's guards; the real, finished answer is then paced out to the
  UI in small pieces so the conversation still visibly grows (REQ-FN-027). The work carried on.
- **Repro:** `.techierag/TechieRag-AI-Reference.md` — `ILlmProvider.ChatStreamAsync` returns
  `IAsyncEnumerable<string>` (plain text only); `ToolCalls`/`HasToolCalls` exist only on
  `ChatAsync`'s `LlmResponse`.
- **Expected:** A streaming call that also yields when the model decides to call a tool, so a
  tool-using conversation turn can still stream its assistant text token by token instead of
  only being usable once the whole non-streaming response is back.
- **Actual:** There is no streaming path that reports a tool call at all — a caller must choose
  between real token streaming (`ChatStreamAsync`, no tool calls) or tool calls (`ChatAsync`, no
  streaming), never both in the same turn.
- **Encountered in:** REQ-FN-025, REQ-FN-027, REQ-FN-036
- **Workaround:** `Chatur.Core.Agent.AgentActions.SendAsync` uses `ChatAsync` for every turn so
  tool calls run through the guard pipeline, then hands the finished, real reply text back to the
  caller in small pieces (`PaceReplyAsync`) rather than inventing or altering any of it.
- **Suggested fix:** A `ChatStreamAsync` (or `AskStreamAsync`) overload that yields a small
  discriminated event per piece — a text delta, or a decided tool call — the way OpenAI's own
  streaming chat-completions API reports `tool_calls` deltas alongside content deltas.

## Replies from TechieRag

<!-- The upstream team's answers, newest block first. Left in full: this is the record. -->
