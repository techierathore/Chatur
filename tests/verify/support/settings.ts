// The verify tests' settings: test-user addresses and passwords, and the test model provider. They come
// from one JSON file beside the tests, tests/verify/test-settings.json (not committed; its shape is
// tests/verify/test-settings.example.json), and NEVER from environment variables or a .env file
// (owner rule, 2026-11-04).
//
// Owner rule, 2026-11-01: a verify test never creates an account on the shared App Manager. Every
// address a test types is one of the fixed UsageGuide test users below, never one built at run time,
// and a Register submit is allowed only after the same address has signed in successfully earlier in
// the same test (see signin-register.spec.ts).
import { readFileSync } from 'fs';
import * as path from 'path';

export type TestUser = 'Owner' | 'Second' | 'Yolo';

interface TestUserSettings {
  number: number;
  email: string;
  password: string;
  /** True when this user has an account on the production App Manager (UsageGuide "Test users"). */
  onAppManager: boolean;
}

interface TestSettings {
  testUsers: Record<TestUser, TestUserSettings>;
  /** The licence the web harness holds in memory when `enabled` (never the shared App Manager account). */
  testLicence?: {
    enabled: boolean;
    name: string;
    state: string;
    key: string;
    daysRemaining: number;
    deviceCap: number;
    overCapWarning: string;
    devices: { id: string; name: string; thisDevice?: boolean; blocked?: boolean }[];
  };
  testProvider: { name: string; connector?: string; baseUrl: string; model: string; key: string };
}

export const SETTINGS_PATH = path.resolve(__dirname, '..', 'test-settings.json');

let objSettings: TestSettings | undefined;

/** The whole settings file, read once. A missing file says where it should be and what it holds. */
export function testSettings(): TestSettings {
  if (objSettings) return objSettings;
  let vText: string;
  try {
    vText = readFileSync(SETTINGS_PATH, 'utf8');
  } catch {
    throw new Error(
      `The verify tests' settings file is missing: ${SETTINGS_PATH}. Copy test-settings.example.json beside it ` +
        'and fill in the passwords and the provider key (they are the user secrets Chatur:TestUsers:* and Chatur:TestProvider:*).',
    );
  }
  objSettings = JSON.parse(vText.replace(/^﻿/, '')) as TestSettings;
  return objSettings;
}

/** One UsageGuide test user's settings, exactly as listed (no fallback to another user). */
export function testUser(aUser: TestUser): TestUserSettings {
  const vUser = testSettings().testUsers[aUser];
  if (!vUser?.email || !vUser.password) throw new Error(`Test user ${aUser} has no email or password in ${SETTINGS_PATH}.`);
  return vUser;
}
