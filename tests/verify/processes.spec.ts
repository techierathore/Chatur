// Acceptance tests: Processes (REQ-UI-045, REQ-UI-046). Black-box, web harness.
import { test, expect } from '@playwright/test';
import { signIn } from './support/auth';
import { query, run } from './support/db';
import { resetFixture, openProject } from './support/workbench-a';
import { openProcessesTab } from './support/phase2-tabs';

test.setTimeout(120_000);

async function openProcesses(aPage: import('@playwright/test').Page) {
  await signIn(aPage, 'Owner');
  resetFixture();
  await openProject(aPage);
  // Project > Processes... opens it as a tab in the main window, the conversation still on the right.
  await openProcessesTab(aPage);
  await expect(aPage.getByTestId('chat-panel')).toBeVisible();
}

test('REQ-UI-045 opening a process shows its steps, checks and owner stops in order', async ({ page }) => {
  // REQ-UI-045: opening a process shows its steps, checks and owner stops in order.
  await openProcesses(page);
  await expect(page.getByTestId('row-verify-phase')).toBeVisible();
  await page.getByTestId('open-verify-phase').click();
  await expect(page.getByTestId('steps-panel')).toContainText('verify-phase, step by step');
  const vRows = page.locator('[data-testid^="step-"]');
  expect(await vRows.count()).toBeGreaterThanOrEqual(5);
  for (let i = 1; i <= (await vRows.count()); i++) {
    await expect(page.getByTestId(`step-${i}`)).toContainText(String(i));
  }
  await expect(page.locator('[data-testid^="kind-"]', { hasText: 'check' }).first()).toBeVisible();
  await expect(page.locator('[data-testid^="kind-"]', { hasText: 'owner stops' }).first()).toBeVisible();
  await expect(page.getByTestId('steps-count')).toContainText('owner stop');
  await page.screenshot({ path: 'tests/.artifacts/verify/processes.png', fullPage: true });
});

test('REQ-UI-046 a step wording changed in the database shows without a file edit', async ({ page }) => {
  // REQ-UI-046: a reworded step shows with no file edited.
  await openProcesses(page);
  await page.getByTestId('open-verify-phase').click();
  const vOld = query("SELECT Text FROM ProcessStepText WHERE ProcessCode='verify-phase' AND StepCode='s1'")[0].Text as string;
  const vNew = 'Reworded in the database ' + Date.now();
  try {
    run("UPDATE ProcessStepText SET Text=? WHERE ProcessCode='verify-phase' AND StepCode='s1'", vNew);
    await page.getByTestId('reload-processes').click();
    await expect(page.getByTestId('wording-1')).toHaveText(vNew, { timeout: 15_000 });
  } finally {
    run("UPDATE ProcessStepText SET Text=? WHERE ProcessCode='verify-phase' AND StepCode='s1'", vOld);
  }
});

test('REQ-UI-045 a link by address opens Processes on the named process', async ({ page }) => {
  // REQ-UI-045: /processes?process=verify-phase opens the Processes tab with that process's steps showing, not the first one's.
  await signIn(page, 'Owner');
  resetFixture();
  await openProject(page);
  await page.goto('/processes?process=verify-phase');
  await expect(page.getByTestId('steps-panel')).toContainText('verify-phase, step by step', { timeout: 30_000 });
  await expect(page.getByTestId('steps-count')).toContainText('7 steps');
});

test('REQ-UI-075 a step reworded on Processes is saved, shown at once and used by the next run', async ({ page }) => {
  // REQ-UI-075: open a process, change a step's wording, save; the database holds it, Processes shows it after a reload and the run uses it.
  await openProcesses(page);
  await page.getByTestId('open-verify-phase').click();
  const vOld = query("SELECT Text FROM ProcessStepText WHERE ProcessCode='verify-phase' AND StepCode='s1'")[0].Text as string;
  const vNew = 'Read the checklist and list every requirement ' + Date.now();
  try {
    // Cancel leaves the wording alone.
    await page.getByTestId('edit-1').click();
    await page.getByTestId('edit-input-1').fill('never saved');
    await page.getByTestId('edit-cancel-1').click();
    await expect(page.getByTestId('wording-1')).toHaveText(vOld);

    // An empty wording cannot be saved.
    await page.getByTestId('edit-1').click();
    await page.getByTestId('edit-input-1').fill('');
    await page.getByTestId('edit-input-1').press('Tab');
    await expect(page.getByTestId('edit-save-1')).toBeDisabled();

    await page.getByTestId('edit-input-1').fill(vNew);
    await page.getByTestId('edit-input-1').press('Tab');
    await page.getByTestId('edit-save-1').click();
    await expect(page.getByTestId('wording-1')).toHaveText(vNew, { timeout: 15_000 });
    expect(query("SELECT Text, Version FROM ProcessStepText WHERE ProcessCode='verify-phase' AND StepCode='s1'")[0]).toMatchObject({ Text: vNew, Version: 2 });
    expect(query("SELECT Before, After FROM Correction WHERE Target='step:verify-phase/s1' ORDER BY CorrectionId DESC")[0]).toMatchObject({ Before: vOld, After: vNew });

    await page.getByTestId('reload-processes').click();
    await expect(page.getByTestId('wording-1')).toHaveText(vNew, { timeout: 15_000 });
    await page.screenshot({ path: 'tests/.artifacts/verify/processes-edited.png', fullPage: true });

    // The next run reads the new wording.
    await page.getByTestId('start-verify-phase').click();
    await expect(page.getByText(vNew).first()).toBeVisible({ timeout: 30_000 });
  } finally {
    run("UPDATE ProcessStepText SET Text=?, Version=1 WHERE ProcessCode='verify-phase' AND StepCode='s1'", vOld);
    run("DELETE FROM Correction WHERE Target='step:verify-phase/s1'");
  }
});
