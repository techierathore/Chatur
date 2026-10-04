// A tiny OpenAI-compatible model service that always answers a conversation's first turn with one tool call
// and answers the turn after a tool result with a plain sentence. It lets an acceptance test about what Chatur
// does when a model ASKS FOR a tool (the guards, the refusal in the activity panel) run the same way every time:
// a real model sometimes declines, explains, or asks first, and then the guard is never reached.
// Everything else - the provider row, the model picker, the agent loop, TechieRag's streaming client, the guards and
// the activity panel - is the real thing; only the model's choice is fixed.
import { createServer, IncomingMessage, Server } from 'http';
import { Page, expect } from '@playwright/test';

export const STUB_PORT = Number(process.env.CHATUR_STUB_PORT ?? 18951);
export const STUB_PROVIDER_NAME = 'Stub edit model';
export const STUB_MODEL = 'stub-edit-model';

export type StubModel = { baseUrl: string; calls: string[]; close: () => Promise<void> };

function readBody(aReq: IncomingMessage): Promise<string> {
  return new Promise((resolve) => {
    const vChunks: Buffer[] = [];
    aReq.on('data', (c) => vChunks.push(c));
    aReq.on('end', () => resolve(Buffer.concat(vChunks).toString('utf8')));
  });
}

const chunk = (aDelta: object, aFinish: string | null = null) =>
  `data: ${JSON.stringify({ id: 'stub-1', object: 'chat.completion.chunk', created: 0, model: STUB_MODEL, choices: [{ index: 0, delta: aDelta, finish_reason: aFinish }] })}\n\n`;

/**
 * Starts the stub. aTool names the tool its first reply calls and aArguments are that call's JSON arguments;
 * `calls` records what each request was answered with, so a test can say what the model was "asked" and "did".
 */
export async function startStubModel(aTool: string, aArguments: object): Promise<StubModel> {
  const calls: string[] = [];
  const vServer: Server = createServer(async (aReq, aRes) => {
    const vUrl = aReq.url ?? '';
    if (aReq.method === 'GET' && /\/models\/?(\?.*)?$/.test(vUrl)) {
      aRes.writeHead(200, { 'content-type': 'application/json' });
      aRes.end(JSON.stringify({ object: 'list', data: [{ id: STUB_MODEL, object: 'model' }] }));
      return;
    }
    if (aReq.method === 'POST' && /\/chat\/completions\/?(\?.*)?$/.test(vUrl)) {
      const vBody = JSON.parse((await readBody(aReq)) || '{}');
      const vMessages: { role: string }[] = vBody.messages ?? [];
      const vAfterTool = vMessages.length > 0 && vMessages[vMessages.length - 1].role === 'tool';
      calls.push(vAfterTool ? 'text' : `tool:${aTool}`);
      if (vBody.stream === false) {
        aRes.writeHead(200, { 'content-type': 'application/json' });
        aRes.end(
          JSON.stringify({
            id: 'stub-1',
            object: 'chat.completion',
            created: 0,
            model: STUB_MODEL,
            choices: [
              {
                index: 0,
                finish_reason: vAfterTool ? 'stop' : 'tool_calls',
                message: vAfterTool
                  ? { role: 'assistant', content: 'Understood.' }
                  : { role: 'assistant', content: null, tool_calls: [{ id: 'call_1', type: 'function', function: { name: aTool, arguments: JSON.stringify(aArguments) } }] },
              },
            ],
          }),
        );
        return;
      }
      aRes.writeHead(200, { 'content-type': 'text/event-stream', 'cache-control': 'no-cache' });
      if (vAfterTool) {
        aRes.write(chunk({ role: 'assistant', content: 'Understood.' }));
        aRes.write(chunk({}, 'stop'));
      } else {
        aRes.write(chunk({ role: 'assistant', tool_calls: [{ index: 0, id: 'call_1', type: 'function', function: { name: aTool, arguments: '' } }] }));
        aRes.write(chunk({ tool_calls: [{ index: 0, function: { arguments: JSON.stringify(aArguments) } }] }));
        aRes.write(chunk({}, 'tool_calls'));
      }
      aRes.write('data: [DONE]\n\n');
      aRes.end();
      return;
    }
    aRes.writeHead(404, { 'content-type': 'application/json' });
    aRes.end(JSON.stringify({ error: { message: `stub model: no ${aReq.method} ${vUrl}` } }));
  });
  await new Promise<void>((resolve, reject) => {
    vServer.once('error', reject);
    vServer.listen(STUB_PORT, '127.0.0.1', () => resolve());
  });
  return {
    baseUrl: `http://127.0.0.1:${STUB_PORT}/v1`,
    calls,
    close: () => new Promise<void>((resolve) => vServer.close(() => resolve())),
  };
}

const field = (aPage: Page, aId: string) => aPage.locator(`input[data-testid=${aId}], [data-testid=${aId}] input`).first();

/** Adds the stub as an OpenAI-compatible provider through the real Add a provider dialog, unless it is already listed. */
export async function ensureStubProvider(aPage: Page, aStub: StubModel): Promise<void> {
  await aPage.goto('/settings/providers', { waitUntil: 'networkidle' });
  await aPage.waitForTimeout(1500);
  if ((await aPage.getByTestId('providers-table').getByText(STUB_PROVIDER_NAME, { exact: true }).count()) > 0) return;
  await aPage.getByTestId('add-provider').click();
  await field(aPage, 'field-name').fill(STUB_PROVIDER_NAME);
  await aPage.getByTestId('field-connector-button').click();
  await aPage.getByTestId('connector-compatible').click();
  await field(aPage, 'field-address').fill(aStub.baseUrl);
  await field(aPage, 'field-key').fill('stub-key');
  await aPage.getByTestId('dialog-add').click();
  await expect(aPage.getByTestId('add-provider-dialog')).toHaveCount(0, { timeout: 60_000 });
  await expect(aPage.getByTestId('providers-table').getByText(STUB_PROVIDER_NAME, { exact: true })).toBeVisible();
}

/** On the Workbench, chooses the stub's model in the composer's model picker. */
export async function chooseStubModel(aPage: Page): Promise<void> {
  await aPage.getByTestId('model-pick').click();
  await aPage.getByTestId('model-pop').getByText(STUB_MODEL, { exact: true }).click();
  await expect(aPage.getByTestId('model-pick')).toContainText(STUB_MODEL);
}

/** Takes the stub provider off Settings ▸ Model providers again, so no later test sees a provider it did not add. */
export async function removeStubProvider(aPage: Page): Promise<void> {
  try {
    await aPage.goto('/settings/providers', { waitUntil: 'networkidle' });
    await aPage.waitForTimeout(1500);
    const vRemove = aPage.getByTestId(`remove-${STUB_PROVIDER_NAME.toLowerCase().replace(/ /g, '-')}`);
    if ((await vRemove.count()) > 0) {
      await vRemove.click();
      await expect(aPage.getByTestId('providers-table').getByText(STUB_PROVIDER_NAME, { exact: true })).toHaveCount(0, { timeout: 20_000 });
    }
  } catch {
    // best effort: the stub address is fixed, so a provider left behind is found and reused by the next run
  }
}
