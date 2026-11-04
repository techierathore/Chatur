// Shared sign-in for the acceptance tests (verify-phase step 4). Addresses and passwords are read from
// tests/verify/test-settings.json (support/settings.ts; UsageGuide "Test users") and never printed.
import { Page, expect } from '@playwright/test';
import { TestUser, testUser } from './settings';

export type { TestUser } from './settings';

/**
 * The email and password of one UsageGuide test user. A user with no account on the production App
 * Manager yet (`onAppManager: false` in the settings file) is signed in as Owner, and the test says so,
 * rather than failing at the Sign in screen.
 */
export function credentials(aRequested: TestUser): { email: string; password: string } {
  let vUser = testUser(aRequested);
  if (!vUser.onAppManager) {
    console.warn(`Test user ${aRequested} has no account on the production App Manager yet; signing in as Owner.`);
    vUser = testUser('Owner');
  }
  return { email: vUser.email, password: vUser.password };
}

/**
 * Signs in through the real Sign in screen and waits until Chatur has left it. The form can be
 * submitted before Blazor is interactive (it then posts empty and lands on `/sign-in?`), so each
 * attempt re-checks the typed values after a settle and the whole attempt is retried.
 */
export async function signIn(aPage: Page, aUser: TestUser = 'Owner'): Promise<void> {
  const { email, password } = credentials(aUser);
  let vLastError: unknown;
  for (let vAttempt = 1; vAttempt <= 3; vAttempt++) {
    try {
      await aPage.goto('/sign-in', { waitUntil: 'networkidle' });
      if (!/\/sign-in/.test(aPage.url())) return; // already signed in
      await aPage.waitForTimeout(1_500 * vAttempt); // let the circuit attach before typing
      const vEmail = aPage.getByTestId('input-email');
      const vPassword = aPage.getByTestId('input-password');
      await vEmail.fill(email);
      await vPassword.fill(password);
      await expect(vEmail).toHaveValue(email);
      await aPage.locator('[data-testid=signin-form] button[type=submit]').first().click();
      await expect(aPage).not.toHaveURL(/\/sign-in/, { timeout: 45_000 });
      return;
    } catch (aError) {
      vLastError = aError;
    }
  }
  throw vLastError;
}
