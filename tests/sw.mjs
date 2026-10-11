// Service-worker lane: the dev server has no service worker, so the share
// target, the boot handoff, and offline precache drive the real worker
// against the production build.
// Run after a production build: pnpm test:sw
import { chromium } from 'playwright';
import { preview } from 'vite';
import { fileURLToPath } from 'node:url';
import { readFileSync } from 'node:fs';
import path from 'node:path';

const root = path.dirname(path.dirname(fileURLToPath(import.meta.url)));
const fixture = path.join(root, 'tests', 'fixtures', 'plan_entrenamiento_4sem.txt');
const fixtureText = readFileSync(fixture, 'utf8');

const server = await preview({ root, logLevel: 'error', preview: { port: 0 } });
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

async function scenario(name, run) {
    const context = await browser.newContext({ locale: 'es-ES' });
    const page = await context.newPage();
    // The same pinned Monday the e2e lane uses: the default anchor and the
    // projected day stay deterministic.
    await page.clock.install({ time: new Date('2026-09-14T10:00:00') });
    try {
        await page.goto(url);
        await page.waitForSelector('metro-app-bar', { timeout: 15000 });
        await run(page, context);
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
        await page
            .getByText(text, { exact: false })
            .filter({ visible: true })
            .first()
            .waitFor({ timeout: 8000 });
    } catch {
        throw new Error(`expected to see "${text}" (${note})`);
    }
}

// The registering page is not controlled until the next navigation: the
// worker installs and activates on the first load, and every scenario
// reloads before it exercises controlled behavior.
async function controlled(page) {
    await page.evaluate(() => navigator.serviceWorker.ready);
    await page.reload();
    await page.waitForSelector('metro-app-bar', { timeout: 15000 });
}

// A share from another app is a multipart POST navigation: the browser
// submits it, the worker answers with the redirect, and the window lands on
// the start URL, where the boot reads the handoff. The scenario builds the
// same request with a real form.
async function submitShare(page, file, text) {
    await page.evaluate(({ fileText, fileName, sharedText }) => {
        const form = document.createElement('form');
        form.method = 'POST';
        form.action = 'share-target';
        form.enctype = 'multipart/form-data';
        if (fileText !== null) {
            const input = document.createElement('input');
            input.type = 'file';
            input.name = 'plan';
            const transfer = new DataTransfer();
            transfer.items.add(new File([fileText], fileName, { type: 'text/plain' }));
            input.files = transfer.files;
            form.appendChild(input);
        }
        if (sharedText !== null) {
            const field = document.createElement('input');
            field.type = 'hidden';
            field.name = 'text';
            field.value = sharedText;
            form.appendChild(field);
        }
        document.body.appendChild(form);
        form.submit();
    }, { fileText: file?.text ?? null, fileName: file?.name ?? null, sharedText: text });
}

// 1. Registration: the worker activates on the production build.
await scenario('the service worker registers and controls the page', async (page) => {
    await controlled(page);
    const isControlled = await page.evaluate(() => navigator.serviceWorker.controller !== null);
    if (!isControlled) throw new Error('expected the page to be controlled after reload');
});

// 2. Share target, file: the POST lands in the worker, the handoff stages
//    the file through the preview, and the commit loads the plan.
await scenario('a shared plan file lands in the preview', async (page) => {
    await controlled(page);
    await submitShare(page, { name: 'plan.briple', text: fixtureText }, null);
    await see(page, 'Importar plan', 'preview opened for the shared file');
    await see(page, 'plan.briple', 'the shared file name in the preview');
    await see(page, 'Plan de Entrenamiento en Circuito - 4 Semanas', 'the shared plan title');
    await page.getByText('Usar este plan').click();
    await page.waitForFunction(() => !window.location.hash.includes('import'), { timeout: 8000 });
    await see(page, 'Plan cargado · 3 variantes · 4 semanas', 'import toast');
    // The handoff is one-shot: another reload shows Today, not the preview.
    await page.reload();
    await page.waitForSelector('metro-app-bar', { timeout: 15000 });
    await see(page, 'Full Body A', 'plan restored after reload');
    const hash = await page.evaluate(() => window.location.hash);
    if (hash.includes('import')) throw new Error('the preview re-opened after the handoff was consumed');
});

// 3. Share target, text: no file, so the text field stages under the
//    default name.
await scenario('shared text lands in the preview under the default name', async (page) => {
    await controlled(page);
    await submitShare(page, null, fixtureText);
    await see(page, 'Importar plan', 'preview opened for the shared text');
    await see(page, 'shared.briple', 'the default file name');
    await see(page, 'Plan de Entrenamiento en Circuito - 4 Semanas', 'the shared plan title');
});

// 4. Offline: the precached shell renders with the network cut.
await scenario('the precached shell renders offline', async (page, context) => {
    await controlled(page);
    await context.setOffline(true);
    await page.reload();
    await page.waitForSelector('metro-app-bar', { timeout: 15000 });
    await see(page, 'Tu calendario está vacío.', 'empty state rendered offline');
    await context.setOffline(false);
});

await browser.close();
await server.close();

console.log(`\nSW: ${failures.length} failure(s)`);
if (failures.length > 0) {
    for (const failure of failures) console.log(`  ${failure}`);
    process.exit(1);
}
process.exit(0);
