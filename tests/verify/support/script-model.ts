// A scripted OpenAI-compatible model service for the process-step-work specs. Unlike support/stub-model.ts (one fixed
// edit-file call), this one plays a WHOLE conversation per handed step: the step's task text says which process, run and
// step the agent has been handed (StepTaskText), the script names the tool calls that step makes (one per turn, in order)
// and the sentence it ends on. So a test can make "the agent" write exactly the files a step's wording asks for - or leave
// something out - and the real provider row, agent loop, guards, rights, session record, hand-over and checks do the rest.
import { createServer, IncomingMessage, Server } from 'http';
import { STUB_MODEL, STUB_PORT, StubModel } from './stub-model';

/** One tool call the scripted agent makes. */
export type ScriptedCall = { tool: string; args: object };

/** What the scripted agent does with one handed step. */
export type StepScript = {
  /** Said in the same reply as the first tool call (or alone, when there is no call). */
  opening?: string;
  /** The tool calls, one per turn, in order. */
  calls: ScriptedCall[];
  /** The closing message of the step - the agent's "what I did and which files I wrote". */
  final: string;
  /** Held back before the closing message, so a test can watch the step while it is still being worked. */
  delayBeforeFinalMs?: number;
};

/** Which step the request belongs to, read from the task text Chatur sent. */
export type StepContext = { process: string; step: number; steps: number; run: number; folder: string };

export type ScriptFn = (aContext: StepContext) => StepScript | null;

export type ScriptedModel = StubModel & {
  /** Replaces the script every following request is answered from. */
  setScript: (aScript: ScriptFn) => void;
  /** Each request answered, as `process#step:tool` or `process#step:text`, so a test can say what the agent did. */
  log: string[];
};

function readBody(aReq: IncomingMessage): Promise<string> {
  return new Promise((resolve) => {
    const vChunks: Buffer[] = [];
    aReq.on('data', (c) => vChunks.push(c));
    aReq.on('end', () => resolve(Buffer.concat(vChunks).toString('utf8')));
  });
}

const chunk = (aDelta: object, aFinish: string | null = null) =>
  `data: ${JSON.stringify({ id: 'script-1', object: 'chat.completion.chunk', created: 0, model: STUB_MODEL, choices: [{ index: 0, delta: aDelta, finish_reason: aFinish }] })}\n\n`;

const textOf = (aContent: unknown): string =>
  typeof aContent === 'string' ? aContent : Array.isArray(aContent) ? aContent.map((p) => (typeof p === 'string' ? p : (p?.text ?? ''))).join('') : '';

/** The step a request belongs to, from the first user message (the task Chatur handed the agent), or null when it is not a handed step. */
export function stepOf(aMessages: { role: string; content?: unknown }[]): StepContext | null {
  const vTask = aMessages.filter((m) => m.role === 'user').map((m) => textOf(m.content)).find((t) => /You have been handed one step/.test(t));
  if (!vTask) return null;
  const vProcess = /Process: ([a-z0-9-]+) \(run (\d+)\)/.exec(vTask);
  const vStep = /Step: (\d+) of (\d+)/.exec(vTask);
  const vProject = /Project: .*? at (.+)\r?\n/.exec(vTask);
  if (!vProcess || !vStep) return null;
  return { process: vProcess[1], run: Number(vProcess[2]), step: Number(vStep[1]), steps: Number(vStep[2]), folder: vProject?.[1].trim() ?? '' };
}

const sleep = (aMs: number) => new Promise((resolve) => setTimeout(resolve, aMs));

/** Starts the scripted model on the stub's port, so the same provider row (ensureStubProvider) reaches it. */
export async function startScriptedModel(aScript: ScriptFn): Promise<ScriptedModel> {
  let vScript = aScript;
  const vLog: string[] = [];
  const vServer: Server = createServer(async (aReq, aRes) => {
    const vUrl = aReq.url ?? '';
    if (aReq.method === 'GET' && /\/models\/?(\?.*)?$/.test(vUrl)) {
      aRes.writeHead(200, { 'content-type': 'application/json' });
      aRes.end(JSON.stringify({ object: 'list', data: [{ id: STUB_MODEL, object: 'model' }] }));
      return;
    }
    if (aReq.method === 'POST' && /\/chat\/completions\/?(\?.*)?$/.test(vUrl)) {
      const vBody = JSON.parse((await readBody(aReq)) || '{}');
      const vMessages: { role: string; content?: unknown }[] = vBody.messages ?? [];
      const vContext = stepOf(vMessages);
      const vPlan = vContext ? vScript(vContext) : null;
      // Each tool result in the conversation so far is one call already made.
      const vMade = vMessages.filter((m) => m.role === 'tool').length;
      const vCall = vPlan && vMade < vPlan.calls.length ? vPlan.calls[vMade] : null;
      const vLabel = vContext ? `${vContext.process}#${vContext.step}` : 'other';
      vLog.push(`${vLabel}:${vCall ? vCall.tool : 'text'}`);
      if (!vCall && vPlan?.delayBeforeFinalMs) await sleep(vPlan.delayBeforeFinalMs);
      const vText = vCall ? (vMade === 0 ? (vPlan?.opening ?? null) : null) : (vPlan?.final ?? 'Understood.');
      if (vBody.stream === false) {
        aRes.writeHead(200, { 'content-type': 'application/json' });
        aRes.end(
          JSON.stringify({
            id: 'script-1',
            object: 'chat.completion',
            created: 0,
            model: STUB_MODEL,
            choices: [
              {
                index: 0,
                finish_reason: vCall ? 'tool_calls' : 'stop',
                message: vCall
                  ? { role: 'assistant', content: vText, tool_calls: [{ id: `call_${vMade + 1}`, type: 'function', function: { name: vCall.tool, arguments: JSON.stringify(vCall.args) } }] }
                  : { role: 'assistant', content: vText },
              },
            ],
          }),
        );
        return;
      }
      aRes.writeHead(200, { 'content-type': 'text/event-stream', 'cache-control': 'no-cache' });
      if (vText) aRes.write(chunk({ role: 'assistant', content: vText }));
      if (vCall) {
        aRes.write(chunk({ role: 'assistant', tool_calls: [{ index: 0, id: `call_${vMade + 1}`, type: 'function', function: { name: vCall.tool, arguments: '' } }] }));
        aRes.write(chunk({ tool_calls: [{ index: 0, function: { arguments: JSON.stringify(vCall.args) } }] }));
        aRes.write(chunk({}, 'tool_calls'));
      } else {
        aRes.write(chunk({}, 'stop'));
      }
      aRes.write('data: [DONE]\n\n');
      aRes.end();
      return;
    }
    aRes.writeHead(404, { 'content-type': 'application/json' });
    aRes.end(JSON.stringify({ error: { message: `script model: no ${aReq.method} ${vUrl}` } }));
  });
  await new Promise<void>((resolve, reject) => {
    vServer.once('error', reject);
    vServer.listen(STUB_PORT, '127.0.0.1', () => resolve());
  });
  return {
    baseUrl: `http://127.0.0.1:${STUB_PORT}/v1`,
    calls: vLog,
    log: vLog,
    setScript: (aNext) => {
      vScript = aNext;
    },
    close: () => new Promise<void>((resolve) => vServer.close(() => resolve())),
  };
}

/** A tool call that writes a whole file (edit-file takes the project-relative path and the file's new text). */
export const put = (aPath: string, aContent: string): ScriptedCall => ({ tool: 'edit-file', args: { path: aPath, content: aContent } });

/** A tool call that builds the project (run-build). */
export const build = (): ScriptedCall => ({ tool: 'run-build', args: {} });
