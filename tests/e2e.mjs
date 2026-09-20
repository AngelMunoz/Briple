// End-to-end flows against the real app: real boot,
// real IndexedDB, real Intl locale, real UI clicks. The clock is fixed on
// Monday 2026-09-14, so every assertion is deterministic (a fresh import
// anchors to this Monday);
// Run after `dotnet fable src`: pnpm test:e2e
import { chromium } from 'playwright';
import { createServer } from 'vite';
import { fileURLToPath } from 'node:url';
import path from 'node:path';

const root = path.dirname(path.dirname(fileURLToPath(import.meta.url)));
const fixture = path.join(root, 'tests', 'fixtures', 'plan_entrenamiento_4sem.txt');

const server = await createServer({ root, logLevel: 'error', server: { port: 0 } });
await server.listen();
const url = server.resolvedUrls.local[0];

let browser;
for (const options of [{}, { channel: 'msedge' }, { channel: 'chrome' }]) {
    try {
        browser = await chromium.launch(options);
        break;
    } catch {
        // Try the next browser channel.
    }
}
if (!browser) {
    console.error('No browser available. Run `npx playwright install chromium`.');
    await server.close();
    process.exit(2);
}

const failures = [];
const pageErrors = [];

async function scenario(name, locale, run) {
    const context = await browser.newContext({ locale });
    const page = await context.newPage();
    page.on('pageerror', (err) => pageErrors.push(`${name}: ${err.message}`));
    page.on('console', (msg) => {
        const url1 = msg.location() ? msg.location().url : '';
        if (msg.type() === 'error' && !url1.includes('favicon')) {
            pageErrors.push(`${name}: [console] ${msg.text()}`);
        }
    });
    await page.clock.install({ time: new Date('2026-09-14T10:00:00') });
    try {
        await page.goto(url);
        await page.waitForSelector('metro-app-bar', { timeout: 15000 });
        await run(page);
        console.log(`PASS ${name}`);
    } catch (err) {
        failures.push(`${name}: ${err.message.split('\n')[0]}`);
        console.log(`FAIL ${name}`);
    } finally {
        await context.close();
    }
}

async function see(page, text, note) {
    try {
        await page.getByText(text, { exact: false }).first().waitFor({ timeout: 8000 });
    } catch {
        throw new Error(`expected to see "${text}" (${note})`);
    }
}

// 1. First run with no plan: the empty state.
await scenario('first run shows the empty state (es)', 'es-ES', async (page) => {
    await see(page, 'Tu calendario está vacío.', 'empty title');
    await see(page, 'Cargar un plan', 'load button');
    await see(page, 'Probar el plan de ejemplo', 'sample button');
    // The date header renders even with no data (the calendar is the product).
    await see(page, 'lunes', 'locale weekday in the date header');
});

// 2. Sample import flow: real click, real parse, real store, real toast.
// Fresh import on Monday 2026-09-14 anchors to this Monday (the variant
// trains today), so the plan line reads Semana 1 · RIR 3.
await scenario('sample import renders the session day (es)', 'es-ES', async (page) => {
    await page.getByText('Probar el plan de ejemplo').click();
    await see(page, 'Plan cargado · 3 variantes · 4 semanas', 'import toast');
    await see(page, 'Semana 1 de 4 · RIR 3', 'plan week line with mesociclo RIR');
    await see(page, 'Full Body A', 'session card title');
    await see(page, '6 ejercicios · 2 circuitos', 'card badge line');
    await see(page, 'Sentadilla con barra', 'first exercise');
    await see(page, '6-8 · 1-2 · 3 min', 'verbatim scheme line');
    await see(page, '+ 4 más · 2 circuitos', 'truncated remainder line');
    await see(page, 'Semana 38 · T3', 'ISO week and quarter in the date block');
    // Day strip: locale letters, selected Monday, dots on session days only.
    for (const letter of ['L', 'M', 'X', 'J', 'V', 'S', 'D']) {
        await page.getByRole('button', { name: letter, exact: true }).waitFor({ timeout: 8000 });
    }
});

// 3. The same flow through the real file picker input ("Load a plan").
await scenario('Load a plan via the file picker', 'es-ES', async (page) => {
    await page.setInputFiles('input[type="file"]', fixture);
    await see(page, 'Plan cargado · 3 variantes · 4 semanas', 'import toast');
    await see(page, 'Full Body A', 'session card title');
});

// 4. Reload: the import survives through IndexedDB (local-first round trip).
await scenario('Import survives a reload through IndexedDB', 'es-ES', async (page) => {
    await page.getByText('Probar el plan de ejemplo').click();
    await see(page, 'Full Body A', 'session card after import');
    await page.reload();
    await page.waitForSelector('metro-app-bar', { timeout: 15000 });
    await see(page, 'Full Body A', 'session card after reload');
    await see(page, 'Semana 1 de 4 · RIR 3', 'plan week line after reload');
});

// 5. Rest day: moving to Tuesday shows the quiet next-session line (S2 note).
await scenario('Rest day shows the quiet next-session line', 'es-ES', async (page) => {
    await page.getByText('Probar el plan de ejemplo').click();
    await see(page, 'Full Body A', 'session card before moving');
    await page.locator('metro-button:has(metro-icon[icon="forward"])').first().click();
    await see(page, 'Descanso · próxima sesión: miércoles, Full Body B', 'rest-day line');
});

// 6. Day strip selection: tapping Wednesday renders Full Body B.
await scenario('Day strip selection switches the session card', 'es-ES', async (page) => {
    await page.getByText('Probar el plan de ejemplo').click();
    await see(page, 'Full Body A', 'Monday card');
    await page.getByRole('button', { name: 'X', exact: true }).click();
    await see(page, 'Full Body B', 'Wednesday card');
    await page.getByRole('button', { name: 'L', exact: true }).click();
    await see(page, 'Full Body A', 'back to Monday card');
});

// 7. Navigation: ⋯ menu reaches the Plan page, chevron returns (F1/F2b shell).
await scenario('App-bar menu navigates to Plan and back', 'es-ES', async (page) => {
    await page.getByText('Probar el plan de ejemplo').click();
    await see(page, 'Full Body A', 'Today rendered');
    await page.getByRole('button', { name: 'More options' }).click();
    await page.getByText('Plan de entrenamiento').click();
    await page.waitForURL(/#\/plan/, { timeout: 8000 });
    await see(page, 'Plan', 'Plan page header');
    // The chevron in the page header returns to Today.
    await page.locator('header metro-hyperlink-button').first().click();
    await see(page, 'Full Body A', 'back on Today');
});

// 8. Browser back drives the same navigation (system back where it exists).
await scenario('Browser back returns from the Plan page', 'es-ES', async (page) => {
    await page.getByText('Probar el plan de ejemplo').click();
    await see(page, 'Full Body A', 'Today rendered');
    await page.getByRole('button', { name: 'More options' }).click();
    await page.getByText('Plan de entrenamiento').click();
    await page.waitForURL(/#\/plan/, { timeout: 8000 });
    await page.goBack();
    await see(page, 'Full Body A', 'back on Today through browser back');
});

// 9. English locale: the chrome strings localize, content stays verbatim.
await scenario('Empty state localizes to en', 'en-US', async (page) => {
    await see(page, 'Your calendar is empty.', 'empty title (en)');
    await see(page, 'Load a plan', 'load button (en)');
    await page.getByText('Try the sample plan').click();
    await see(page, 'Full Body A', 'session card (en chrome, es content)');
});

await browser.close();
await server.close();

if (pageErrors.length > 0) {
    for (const error of pageErrors) console.log(`PAGE ERROR ${error}`);
    failures.push(`${pageErrors.length} page error(s)`);
}

console.log(`\nE2E: ${failures.length} failure(s)`);
if (failures.length > 0) {
    for (const failure of failures) console.log(`  ${failure}`);
    process.exit(1);
}
process.exit(0);
