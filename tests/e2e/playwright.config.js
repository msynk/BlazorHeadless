import { defineConfig, devices } from '@playwright/test';

const port = process.env.E2E_PORT ?? '5280';
const baseURL = process.env.E2E_BASE_URL ?? `http://localhost:${port}`;

export default defineConfig({
    testDir: './specs',
    timeout: 60_000,
    expect: { timeout: 5_000 },
    fullyParallel: true,
    forbidOnly: !!process.env.CI,
    retries: process.env.CI ? 1 : 0,
    workers: process.env.CI ? 2 : undefined,
    reporter: process.env.CI
        ? [['github'], ['list'], ['html', { open: 'never' }], ['./summary-reporter.js']]
        : [['list'], ['html', { open: 'never' }]],
    use: {
        baseURL,
        trace: 'retain-on-failure',
        // Use an already-installed browser (PLAYWRIGHT_CHROMIUM_EXECUTABLE) when provided.
        launchOptions: process.env.PLAYWRIGHT_CHROMIUM_EXECUTABLE
            ? { executablePath: process.env.PLAYWRIGHT_CHROMIUM_EXECUTABLE }
            : {},
    },
    projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],
    webServer: {
        command: `dotnet run --project ../../src/BlazorHeadless.Samples --configuration Release --urls ${baseURL}`,
        url: baseURL,
        reuseExistingServer: !process.env.CI,
        timeout: 300_000,
        stdout: 'ignore',
        stderr: 'pipe',
    },
});
