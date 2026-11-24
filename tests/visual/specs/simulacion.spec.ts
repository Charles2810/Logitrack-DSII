import { expect, test } from '@playwright/test';
import { loginAsAdmin } from './auth';
import { stubOsrm } from './osrm';

test.beforeEach(async ({ page, request }) => {
  await loginAsAdmin(page, request);
  await stubOsrm(page);
});

test('simulación de rutas permite iniciar, pausar, reanudar y reiniciar', async ({ page }) => {
  await page.goto('/simulacion-rutas');

  await expect(page.getByText('Envíos con ruta')).toBeVisible();
  await page.getByText('LT-00000002', { exact: true }).first().click();

  await expect(page.getByRole('button', { name: 'Iniciar simulación' })).toBeVisible();
  await expect(page.getByText('2.70 km', { exact: true }).first()).toBeVisible();
  await expect(page.getByText('12 min', { exact: true }).first()).toBeVisible();

  await page.getByRole('button', { name: 'Iniciar simulación' }).click();
  await expect(page.getByRole('button', { name: 'Pausar' })).toBeEnabled({ timeout: 30_000 });

  await page.getByRole('button', { name: 'Pausar' }).click();
  await expect(page.getByRole('button', { name: 'Reanudar' })).toBeVisible();

  await page.getByRole('button', { name: 'Reanudar' }).click();
  await page.getByRole('button', { name: 'Reiniciar' }).click();
  await expect(page.locator('.lt-mapa')).toBeVisible({ timeout: 20_000 });

  await page.screenshot({ path: 'test-results/simulacion.png', fullPage: true });
});
