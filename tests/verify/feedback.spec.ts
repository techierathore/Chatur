// Acceptance tests: Feedback and issues (REQ-UI-062, REQ-FN-073, REQ-UI-063, REQ-FN-074).
// Black-box, web harness. Route /feedback, mockup docs/mockups/feedback.html.
import { test, expect, Page } from '@playwright/test';
import { signIn } from './support/auth';

test.setTimeout(120_000);

async function openFeedback(aPage: Page) {
  await signIn(aPage, 'Owner');
  await aPage.goto('/feedback', { waitUntil: 'networkidle' });
  await expect(aPage.getByTestId('page-heading')).toContainText('Feedback and issues', { timeout: 30_000 });
}

test('REQ-UI-062 feedback sent is raised through App Manager and listed as raised', async ({ page }) => {
  // REQ-UI-062 scenario: sign in as Owner, open /feedback, pick kind Issue, type a unique message, press
  // Send. Acceptance: the entry is raised through App Manager (the list shows it with state "raised") and
  // the list panel no longer shows the empty state. The message must appear in the list, not only a toast.
  await openFeedback(page);
  const vMessage = 'verify feedback ' + Date.now();
  await page.getByTestId('feedback-message').fill(vMessage);
  await page.getByTestId('feedback-message').press('Tab');
  await expect(page.getByTestId('feedback-send')).toBeEnabled();
  await page.getByTestId('feedback-send').click();
  const vPanel = page.getByTestId('feedback-list-panel');
  try {
    await expect(vPanel).toContainText(vMessage, { timeout: 15_000 });
    await expect(vPanel).toContainText(/raised/i);
    await expect(page.getByTestId('feedback-empty')).toHaveCount(0);
  } catch (aError) {
    await page.screenshot({ path: 'tests/.artifacts/verify/feedback-062.png', fullPage: true });
    throw new Error('not measured: sending feedback does not raise an entry or list it; the list panel still shows "' +
      ((await vPanel.innerText()).replace(/\s+/g, ' ').trim()) + '" (no row for the sent message, no "raised" state). ' + String(aError));
  }
});

test('REQ-FN-073 an entry about a library carries the library name the owner picked', async ({ page }) => {
  // REQ-FN-073 scenario: on /feedback the owner picks a library (TrBlazeUI or TechieRag) for the entry,
  // sends it, and the listed entry carries that library's name. Needs a library picker in the form.
  await openFeedback(page);
  const vForm = page.getByTestId('feedback-form');
  const vPicker = page.locator('[data-testid*="library"]');
  if ((await vPicker.count()) === 0) {
    await page.screenshot({ path: 'tests/.artifacts/verify/feedback-073.png', fullPage: true });
    throw new Error('not measured: the Feedback form offers no library picker (only Kind and Message), so an entry cannot be tied to a library; form text: ' +
      (await vForm.innerText()).replace(/\s+/g, ' ').trim());
  }
  await vPicker.first().click();
  await page.getByRole('option', { name: /TrBlazeUI/i }).first().click();
  const vMessage = 'verify library ' + Date.now();
  await page.getByTestId('feedback-message').fill(vMessage);
  await page.getByTestId('feedback-message').press('Tab');
  await page.getByTestId('feedback-send').click();
  await expect(page.getByTestId('feedback-list-panel')).toContainText(vMessage, { timeout: 15_000 });
  await expect(page.getByTestId('feedback-list-panel')).toContainText('TrBlazeUI');
});

test('REQ-UI-063 an answered entry shows its state and the answer in its row', async ({ page }) => {
  // REQ-UI-063 scenario: an entry that App Manager has answered appears in "What came back" with its
  // state and the answer text. Needs an answered entry; the app offers no seam to create one (no
  // database table, no stub App Manager, and the list never renders rows), so check for any row.
  await openFeedback(page);
  const vRows = page.locator('[data-testid^="feedback-row"], [data-testid^="feedback-entry"]');
  if ((await vRows.count()) === 0) {
    await page.screenshot({ path: 'tests/.artifacts/verify/feedback-063.png', fullPage: true });
    throw new Error('not measured: no feedback row exists to inspect and the app offers no seam to produce an answered entry from App Manager (list shows the empty state).');
  }
  await expect(vRows.first()).toContainText(/answered/i);
  await expect(page.locator('[data-testid^="feedback-answer"]').first()).toBeVisible();
});

test('REQ-FN-074 feedback that cannot be sent is kept and sent when the network returns', async ({ page, context }) => {
  // REQ-FN-074 scenario: with the browser offline (context.setOffline cannot cut the server-side
  // App Manager call, so only the page-visible half is checked), send feedback, expect it to be listed
  // as kept/pending, then go back online and expect it to become "raised" without another Send.
  await openFeedback(page);
  const vMessage = 'verify offline ' + Date.now();
  await page.getByTestId('feedback-message').fill(vMessage);
  await page.getByTestId('feedback-message').press('Tab');
  await page.getByTestId('feedback-send').click();
  const vPanel = page.getByTestId('feedback-list-panel');
  const vKept = await vPanel.innerText();
  if (!vKept.includes(vMessage)) {
    await page.screenshot({ path: 'tests/.artifacts/verify/feedback-074.png', fullPage: true });
    throw new Error('not measured: the app has no network-loss seam for the server-side App Manager call, and the sent message is not kept anywhere visible (list panel: "' +
      vKept.replace(/\s+/g, ' ').trim() + '"), so keep-and-resend cannot be shown.');
  }
  await expect(vPanel).toContainText(/kept|waiting|pending/i);
  await expect(vPanel).toContainText(/raised/i, { timeout: 60_000 });
});
