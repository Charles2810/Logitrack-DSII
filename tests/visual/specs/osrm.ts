import { Page } from '@playwright/test';

export async function stubOsrm(page: Page): Promise<void> {
  await page.route('**/route/**', route => route.fulfill({
    status: 200,
    contentType: 'application/json',
    json: {
      routes: [{
        distance: 270,
        duration: 720,
        geometry: {
          type: 'LineString',
          coordinates: [
            [-63.180426, -17.7837],
            [-63.174, -17.789],
            [-63.1649, -17.7961]
          ]
        }
      }]
    }
  }));
}
