import { expect, test } from '@playwright/test';
import { loginAsAdmin } from './auth';

test.beforeEach(async ({ page, request }) => {
  await loginAsAdmin(page, request);
});

test('formulario de conductores valida la contraseña inicial', async ({ page }) => {
  await page.goto('/conductores');

  await expect(page.getByRole('button', { name: 'Nuevo conductor' })).toBeVisible();
  await page.getByRole('button', { name: 'Nuevo conductor' }).click();

  await expect(page.getByLabel('Nombre completo')).toBeVisible();
  await expect(page.getByLabel('Contraseña inicial')).toBeVisible();

  await page.getByLabel('Nombre completo').fill('Conductor Visual');
  await page.getByLabel('Correo electrónico').fill('conductor.visual@logitrack.test');
  await page.getByRole('button', { name: 'Crear conductor' }).click();

  await expect(page.getByText('Defina una contraseña inicial.')).toBeVisible();
  await page.getByRole('button', { name: 'Cancelar' }).click();

  await expect(page.getByLabel('Nombre completo')).toHaveCount(0);
  await page.screenshot({ path: 'test-results/conductores.png', fullPage: true });
});
