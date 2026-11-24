import { defineConfig } from '@playwright/test';

export default defineConfig({
  testDir: './specs',
  timeout: 60_000,
  expect: { timeout: 20_000 },
  fullyParallel: false,
  workers: 1,
  retries: process.env.CI ? 1 : 0,
  reporter: [['list'], ['html', { open: 'never' }]],
  use: {
    channel: 'msedge',
    baseURL: 'http://localhost:5163',
    headless: true,
    viewport: { width: 1440, height: 900 },
    trace: 'on-first-retry',
    screenshot: 'only-on-failure'
  },
  webServer: {
    command: 'dotnet run --project ../../POS.Server/POS.Server.csproj --launch-profile http',
    url: 'http://localhost:5163',
    reuseExistingServer: true,
    timeout: 120_000
  }
});
