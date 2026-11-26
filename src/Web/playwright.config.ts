import { defineConfig } from '@playwright/test';
import { dirname } from 'node:path';

const url = process.env['EUCONOMIA_URL'] ?? 'http://127.0.0.1:5278';
const dll = process.env['EUCONOMIA_E2E_DLL'];

export default defineConfig({
  testDir: './e2e',
  workers: 1,
  timeout: 60_000,
  use: { baseURL: url, channel: process.platform === 'win32' ? 'msedge' : 'chromium', trace: 'retain-on-failure' },
  webServer: dll ? { command: `dotnet "${dll}"`, cwd: dirname(dll), url, timeout: 30000 } : undefined
});
