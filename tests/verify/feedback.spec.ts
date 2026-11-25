// Acceptance tests: Feedback and issues (REQ-UI-062, REQ-FN-073, REQ-UI-063, REQ-FN-074).
// Black-box, web harness. Route /feedback, mockup docs/mockups/feedback.html.
//
// The web harness (a test-only host) reads Feedback:Source from its own appsettings.json; "TestDouble" swaps App
// Manager for an in-memory stand-in. The tests drive everything through the page. The stand-in's own routes
// (/test/feedback/*, Development only) switch its network off, write an answer as the App Manager team would, and
// read what it holds — the only way to see that an entry really reached "App Manager" and to cut its network.
// The signed-in account is the Owner test user; nothing is created on a shared App Manager.
import { test, expect, Page } from '@playwright/test';
import { signIn } from './support/auth';
import { run } from './support/db';

test.setTimeout(120_000);

type Issue = { number: string; about: string; title: string; detail: string; finished: boolean; answer: string | null };

async function openFeedback(aPage: Page) {
  await signIn(aPage, 'Owner');
  await aPage.goto('/feedback', { waitUntil: 'networkidle' });
  await expect(aPage.getByTestId('feedback-heading')).toContainText('Tell them what is missing', { timeout: 30_000 });
}

async function issuesHeld(aPage: Page): Promise<Issue[]> {
  const vResponse = await aPage.request.get('/test/feedback/issues');
  expect(vResponse.ok(), 'the web harness must run with Feedback:Source = TestDouble in Development').toBeTruthy();
  return (await vResponse.json()) as Issue[];
}

async function pick(aPage: Page, aSelect: string, aItem: string) {
  await aPage.getByTestId(aSelect).click();
  await aPage.getByTestId(aItem).click();
}

async function fillAndSend(aPage: Page, aTitle: string) {
  await aPage.getByTestId('feedback-title').fill(aTitle);
  await aPage.getByTestId('feedback-detail').fill('Queued three runs, restarted Chatur, and they came back in a different order.');
  await aPage.getByTestId('feedback-detail').press('Tab');
  await expect(aPage.getByTestId('feedback-submit')).toBeEnabled();
  await aPage.getByTestId('feedback-submit').click();
}

function rowOf(aPage: Page, aTitle: string) {
  return aPage.locator('[data-testid^="fb-row-"]', { hasText: aTitle });
}

test.beforeEach(async ({ page }) => {
  await page.request.post('/test/feedback/reset').catch(() => undefined);
});

test.afterEach(async ({ page }) => {
  // Every entry a test made is removed: the pretend server forgets it and so does the account's own database.
  await page.request.post('/test/feedback/reset').catch(() => undefined);
  try {
    run("DELETE FROM FeedbackEntry WHERE Title LIKE 'verify %'");
  } catch {
    // The account's database is not there to clean (the test failed before sign-in).
  }
});

test('REQ-UI-062 feedback sent is raised through App Manager and listed as raised', async ({ page }) => {
  // Sign in as Owner, open /feedback, type a unique title and description, press Send it. The entry is raised
  // through App Manager (the stand-in holds it) and the list shows its row in the state "raised".
  await openFeedback(page);
  const vTitle = 'verify raised ' + Date.now();
  await fillAndSend(page, vTitle);
  const vRow = rowOf(page, vTitle);
  await expect(vRow).toHaveCount(1, { timeout: 15_000 });
  await expect(vRow.locator('[data-testid^="fb-state-"]')).toHaveText('raised', { timeout: 15_000 });
  await expect(page.getByTestId('feedback-empty')).toHaveCount(0);
  await expect(page.getByTestId('feedback-queued-note')).toHaveCount(0);
  const vHeld = (await issuesHeld(page)).filter(aIssue => aIssue.title.includes(vTitle));
  expect(vHeld).toHaveLength(1);
  expect(vHeld[0].detail).toContain('Queued three runs');
  await expect(page.getByTestId('feedback-title')).toHaveValue('');
});

test('REQ-FN-073 an entry about a library carries the library name the owner picked', async ({ page }) => {
  // On /feedback the owner picks TrBlazeUI in "What it is about", sends, and both the listed row and the entry
  // App Manager holds carry that name. A second entry left on the default carries "Chatur".
  await openFeedback(page);
  const vLibrary = 'verify library ' + Date.now();
  await pick(page, 'feedback-about-button', 'about-trblazeui');
  await fillAndSend(page, vLibrary);
  await expect(rowOf(page, vLibrary).locator('[data-testid^="fb-about-"]')).toHaveText('TrBlazeUI', { timeout: 15_000 });
  const vOwn = 'verify itself ' + Date.now();
  await pick(page, 'feedback-about-button', 'about-chatur');
  await fillAndSend(page, vOwn);
  await expect(rowOf(page, vOwn).locator('[data-testid^="fb-about-"]')).toHaveText('Chatur', { timeout: 15_000 });
  const vHeld = await issuesHeld(page);
  expect(vHeld.find(aIssue => aIssue.title.includes(vLibrary))?.about).toBe('TrBlazeUI');
  expect(vHeld.find(aIssue => aIssue.title.includes(vOwn))?.about).toBe('Chatur');
});

test('REQ-UI-063 an answered entry shows its state and the answer in its row', async ({ page }) => {
  // An entry is sent, then the App Manager team answers it (the stand-in's answer route). Refresh shows the row
  // as "answered" with the answer text; once the team resolves it, the row shows "fixed" and the new answer.
  await openFeedback(page);
  const vTitle = 'verify answered ' + Date.now();
  await fillAndSend(page, vTitle);
  const vRow = rowOf(page, vTitle);
  await expect(vRow.locator('[data-testid^="fb-state-"]')).toHaveText('raised', { timeout: 15_000 });
  await expect(vRow.locator('[data-testid^="fb-answer-"]')).toHaveText('—');

  const vAnswered = await page.request.post('/test/feedback/answer?title=' + encodeURIComponent(vTitle) + '&answer=' + encodeURIComponent('Fixed in 2.1.4, out this week') + '&finished=false');
  expect(vAnswered.ok()).toBeTruthy();
  await page.getByTestId('feedback-refresh').click();
  await expect(vRow.locator('[data-testid^="fb-state-"]')).toHaveText('answered', { timeout: 15_000 });
  await expect(vRow.locator('[data-testid^="fb-answer-"]')).toHaveText('Fixed in 2.1.4, out this week');

  await page.request.post('/test/feedback/answer?title=' + encodeURIComponent(vTitle) + '&answer=' + encodeURIComponent('Released on 20 Sep') + '&finished=true');
  await page.getByTestId('feedback-refresh').click();
  await expect(vRow.locator('[data-testid^="fb-state-"]')).toHaveText('fixed', { timeout: 15_000 });
  await expect(vRow.locator('[data-testid^="fb-answer-"]')).toHaveText('Released on 20 Sep');
});

test('REQ-FN-074 feedback that cannot be sent is kept and sent when the network returns', async ({ page }) => {
  // The stand-in's network is cut. The owner sends: the row is kept as "waiting to send", the note says so and
  // App Manager holds nothing. The network returns; with no further press the page sends it and the row becomes
  // "raised", held by App Manager exactly once.
  await openFeedback(page);
  expect((await page.request.post('/test/feedback/network?down=true')).ok()).toBeTruthy();
  const vTitle = 'verify offline ' + Date.now();
  await fillAndSend(page, vTitle);
  const vRow = rowOf(page, vTitle);
  await expect(vRow.locator('[data-testid^="fb-state-"]')).toHaveText('waiting to send', { timeout: 15_000 });
  await expect(page.getByTestId('feedback-queued-note')).toContainText('waiting to be sent');
  // Held after a reload too: it is kept on this machine, not in the page.
  await page.reload({ waitUntil: 'networkidle' });
  await expect(rowOf(page, vTitle).locator('[data-testid^="fb-state-"]')).toHaveText('waiting to send', { timeout: 15_000 });
  expect((await issuesHeld(page)).filter(aIssue => aIssue.title.includes(vTitle))).toHaveLength(0);

  expect((await page.request.post('/test/feedback/network?down=false')).ok()).toBeTruthy();
  await expect(rowOf(page, vTitle).locator('[data-testid^="fb-state-"]')).toHaveText('raised', { timeout: 45_000 });
  await expect(page.getByTestId('feedback-queued-note')).toHaveCount(0);
  expect((await issuesHeld(page)).filter(aIssue => aIssue.title.includes(vTitle))).toHaveLength(1);
});
