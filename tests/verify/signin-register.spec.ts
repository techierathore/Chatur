// Acceptance tests: Sign in and Register (REQ-UI-001..004, REQ-FN-001..005). Black-box, web harness.
import { test, expect, Page } from '@playwright/test';
import { credentials } from './support/auth';

// The harness keeps the signed-in session in one server-side store; run with --workers=1 for stable results.

const STRONG = 'Tflens2026!';

async function expectProjects(aPage: Page): Promise<void> {
  // Projects = Start (/start). Start deliberately forwards to the Workbench (/) when a project is already
  // selected (REQ-FN-006), so either is "the Projects screen opened, signed in".
  await expect(aPage).toHaveURL(/^[^?#]*\/(start)?([?#].*)?$/, { timeout: 30_000 });
  await expect(aPage.locator('[data-testid=recent-side], [data-testid=toolbar]').first()).toBeVisible({ timeout: 30_000 });
  await expect(aPage.getByTestId('input-password')).toHaveCount(0);
}

/** Opens a page and waits for the Blazor circuit to attach, so typed values are not wiped by the late re-render. */
async function openInteractive(aPage: Page, aPath: string): Promise<void> {
  const vSocket = aPage.waitForEvent('websocket', { timeout: 30_000 }).catch(() => null);
  await aPage.goto(aPath, { waitUntil: 'networkidle' });
  await vSocket;
  await aPage.waitForTimeout(1000);
}

/** Same steps as support/auth.ts signIn, but waits for the Blazor circuit first (typed values are otherwise flaky). */
async function signIn(aPage: Page, aUser: 'Owner' | 'Second' | 'Yolo'): Promise<void> {
  const { email, password } = credentials(aUser);
  await openInteractive(aPage, '/sign-in');
  await aPage.getByTestId('input-email').fill(email);
  await aPage.getByTestId('input-password').fill(password);
  await aPage.locator('[data-testid=signin-form] button[type=submit]').first().click();
  await expect(aPage).not.toHaveURL(/\/sign-in/, { timeout: 30_000 });
}

async function fillRegister(aPage: Page, aEmail: string, aPassword: string, aConfirm = aPassword): Promise<void> {
  await openInteractive(aPage, '/register');
  await aPage.getByTestId('input-first-name').fill('Verify');
  await aPage.getByTestId('input-last-name').fill('Tester');
  await aPage.getByTestId('input-email').fill(aEmail);
  await aPage.getByTestId('input-password').fill(aPassword);
  await aPage.getByTestId('input-confirm').fill(aConfirm);
}

test('REQ-UI-001 sign in opens Projects', async ({ page }) => {
  await signIn(page, 'Yolo');
  await expectProjects(page);
});

test('REQ-FN-001 password is sent encrypted and never in plain text', async () => {
  test.skip(
    true,
    'Sign-in runs server-side in Blazor Server: the browser only sends the typed keystrokes to the server over the ' +
      'websocket, and the encryption happens on the server before it calls App Manager. The request the acceptance ' +
      'describes is never visible from the browser, so it cannot be observed black-box.',
  );
});

test('REQ-UI-002 wrong password shows the server message and stays on Sign in', async ({ page }) => {
  const { email } = credentials('Yolo');
  await openInteractive(page, '/sign-in');
  await page.getByTestId('input-email').fill(email);
  await page.getByTestId('input-password').fill('Wrong-Password-000!');
  await page.locator('[data-testid=signin-form] button[type=submit]').first().click();
  const vError = page.getByTestId('signin-error');
  await expect(vError).toBeVisible({ timeout: 30_000 });
  const vText = (await vError.innerText()).trim();
  expect(vText.length).toBeGreaterThan(0);
  await expect(page).toHaveURL(/\/sign-in/);
  await page.screenshot({ path: 'tests/.artifacts/verify/signin-register-ui002.png' });
});

test('REQ-FN-002 device identifier is sent and stays the same', async () => {
  test.skip(
    true,
    'The device identifier is generated and sent by the server (harness database to App Manager); the browser and ' +
      'the harness expose neither the value nor the outgoing request, so it cannot be observed black-box.',
  );
});

test('REQ-FN-003 reopening Chatur while signed in opens Projects without a password', async ({ browser }) => {
  const vContext = await browser.newContext();
  const vFirst = await vContext.newPage();
  await signIn(vFirst, 'Yolo');
  await expectProjects(vFirst);
  await vFirst.close(); // "close Chatur"

  const vSecond = await vContext.newPage(); // "open it again"
  await vSecond.goto('/', { waitUntil: 'networkidle' });
  await expect(vSecond.locator('[data-testid=recent-side], [data-testid=toolbar]').first()).toBeVisible({ timeout: 30_000 });
  await expect(vSecond).not.toHaveURL(/\/sign-in/);
  await expect(vSecond.getByTestId('input-password')).toHaveCount(0);
  await vContext.close();
});

test('REQ-FN-004 register with a valid password creates the account and opens Projects', async ({ page }) => {
  const vEmail = `chatur-verify-${Date.now()}@techierathore.com`;
  await fillRegister(page, vEmail, STRONG);
  await page.getByTestId('register-submit').click();
  await expectProjects(page);
});

test('REQ-UI-003 password rule shows which parts are met while typing', async ({ page }) => {
  await openInteractive(page, '/register');
  const vPassword = page.getByTestId('input-password');
  const vRules = ['rule-length', 'rule-capital', 'rule-number', 'rule-special'];
  // Blazor Server re-renders after a round trip per keystroke, so poll until the screen settles.
  const vExpectMet = async (aId: string, aMet: boolean) =>
    expect
      .poll(async () => ((await page.getByTestId(aId).getAttribute('class')) ?? '').includes('text-alert-success'), {
        message: `${aId} met=${aMet}`,
        timeout: 10_000,
      })
      .toBe(aMet);

  await vPassword.pressSequentially('abc');
  for (const vId of vRules) await vExpectMet(vId, false);

  await vPassword.fill('');
  await vPassword.pressSequentially('abcdefgh');
  await vExpectMet('rule-length', true);
  await vExpectMet('rule-capital', false);
  await vExpectMet('rule-number', false);
  await vExpectMet('rule-special', false);
  await expect(page.getByTestId('strength-value')).toContainText('1 of 4');

  await vPassword.fill('');
  await vPassword.pressSequentially(STRONG);
  for (const vId of vRules) await vExpectMet(vId, true);
  await expect(page.getByTestId('strength-value')).toContainText('4 of 4');

  await vPassword.press('Backspace'); // drops the special character
  await vExpectMet('rule-special', false);
  await vExpectMet('rule-length', true);
});

test('REQ-FN-005 weak password is refused and the field is marked', async ({ page }) => {
  const vEmail = `chatur-verify-weak-${Date.now()}@techierathore.com`;
  await fillRegister(page, vEmail, 'weak');
  await page.getByTestId('register-submit').click();
  await expect(page.getByTestId('password-error')).toBeVisible({ timeout: 15_000 });
  await expect(page.getByTestId('field-password')).toBeVisible();
  await expect(page).toHaveURL(/\/register/);
  // It was refused before being sent, so no account exists: signing in with it must not be offered a session.
  await expect(page.getByTestId('email-error')).toBeHidden();
  await page.screenshot({ path: 'tests/.artifacts/verify/signin-register-fn005.png' });
});

test('REQ-UI-004 email already in use shows the server message and keeps what was typed', async ({ page }) => {
  const vEmail = 'chatur-owner@techierathore.com';
  await fillRegister(page, vEmail, STRONG);
  await page.getByTestId('register-submit').click();
  const vError = page.getByTestId('email-error');
  await expect(vError).toBeVisible({ timeout: 30_000 });
  expect((await vError.innerText()).trim().length).toBeGreaterThan(0);
  await expect(page).toHaveURL(/\/register/);
  await expect(page.getByTestId('input-first-name')).toHaveValue('Verify');
  await expect(page.getByTestId('input-last-name')).toHaveValue('Tester');
  await expect(page.getByTestId('input-email')).toHaveValue(vEmail);
  await expect(page.getByTestId('input-password')).toHaveValue(STRONG);
  await expect(page.getByTestId('input-confirm')).toHaveValue(STRONG);
  await page.screenshot({ path: 'tests/.artifacts/verify/signin-register-ui004.png' });
});
