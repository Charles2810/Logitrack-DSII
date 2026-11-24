import { APIRequestContext, Page } from '@playwright/test';

export async function loginAsAdmin(page: Page, request: APIRequestContext): Promise<void> {
  const response = await request.post('/api/auth/login', {
    data: {
      correo: 'luis.mendez@logitrack.test',
      password: 'LogiTrack2026!'
    }
  });

  const body = await response.json();
  if (!response.ok()) {
    throw new Error(`Login failed: ${JSON.stringify(body)}`);
  }

  await page.addInitScript(([token, auth]) => {
    try {
      localStorage.setItem('logitrack_token', token as string);
      localStorage.setItem('logitrack_auth', JSON.stringify(auth));
    } catch {
      return;
    }
  }, [body.token, body]);
}
