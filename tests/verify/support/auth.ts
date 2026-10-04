// Shared sign-in for the acceptance tests (verify-phase step 4). Passwords are read from Chatur's
// user secrets (Visual Studio ▸ Manage User Secrets; UsageGuide "Test users") and never printed.
import { Page, expect } from '@playwright/test';
import { readFileSync } from 'fs';

const SECRETS_PATH =
  process.env.CHATUR_SECRETS ??
  '/mnt/c/Users/srkra/AppData/Roaming/Microsoft/UserSecrets/chatur-3f1c9a52-0d4e-4b7a-9c1e-5a8e2b7d6f10/secrets.json';

export type TestUser = 'Owner' | 'Second' | 'Yolo';

const EMAILS: Record<TestUser, string> = {
  Owner: 'chatur-owner@techierathore.com',
  Second: 'chatur-second@techierathore.com',
  Yolo: 'chatur-yolo-tester@techierathore.com',
};

/** The email and password of one UsageGuide test user. */
export function credentials(aUser: TestUser): { email: string; password: string } {
  const vSecrets = JSON.parse(readFileSync(SECRETS_PATH, 'utf8').replace(/^﻿/, ''));
  const vPassword = vSecrets[`Chatur:TestUsers:${aUser}`];
  if (!vPassword) throw new Error(`No password for Chatur:TestUsers:${aUser} in user secrets.`);
  return { email: EMAILS[aUser], password: vPassword };
}

/**
 * Signs in through the real Sign in screen and waits until Chatur has left it. The form can be
 * submitted before Blazor is interactive (it then posts empty and lands on `/sign-in?`), so each
 * attempt re-checks the typed values after a settle and the whole attempt is retried.
 */
export async function signIn(aPage: Page, aUser: TestUser = 'Yolo'): Promise<void> {
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
