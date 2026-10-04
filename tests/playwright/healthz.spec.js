// @ts-check
const { test, expect } = require('@playwright/test');

test('the web harness answers /healthz', async ({ request }) => {
  const response = await request.get('/healthz');
  expect(response.status()).toBe(200);
});
