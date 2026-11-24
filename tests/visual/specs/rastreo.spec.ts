import { expect, test } from '@playwright/test';
import { stubOsrm } from './osrm';

test('rastreo público muestra mapa, distancia y timeline', async ({ page }) => {
  await stubOsrm(page);
  await page.goto('/rastreo');
  await page.getByLabel('Código de guía').fill('LT-00000002');
  await page.getByRole('button', { name: 'Rastrear' }).click();

  await expect(page.getByText('LT-00000002', { exact: true })).toBeVisible();
  await expect(page.getByText('En Tránsito', { exact: true }).first()).toBeVisible();
  await expect(page.locator('.lt-mapa')).toBeVisible({ timeout: 20_000 });
  await expect(page.getByText(/2\.70\s*km/)).toBeVisible();
  await expect(page.getByText(/12\s*min/)).toBeVisible();

  await page.screenshot({ path: 'test-results/rastreo.png', fullPage: true });
});
