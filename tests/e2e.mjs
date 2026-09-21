// End-to-end flows against the real app: real boot,
// real IndexedDB, real Intl locale, real UI clicks. The clock is fixed on
// Monday 2026-09-14, so every assertion is deterministic (a fresh import
// anchors to this Monday);
// Run after `dotnet fable src`: pnpm test:e2e
import { chromium } from 'playwright';
import { createServer } from 'vite';
import { fileURLToPath } from 'node:url';
import { readFileSync } from 'node:fs';
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

async function scenario(name, locale, run, initScript) {
    const context = await browser.newContext({ locale });
    const page = await context.newPage();
    if (initScript) await page.addInitScript(initScript);
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
        await page
            .getByText(text, { exact: false })
            .filter({ visible: true })
            .first()
            .waitFor({ timeout: 8000 });
    } catch {
        throw new Error(`expected to see "${text}" (${note})`);
    }
}

// The import preview interposes between choosing a file and the store:
// every import flow ends by tapping the preview's commit button. The commit
// walks back through history, so the helper waits for the hash to leave the
// preview before the caller touches the page again.
async function commitPreview(page, commitText = 'Usar este plan') {
    await see(page, commitText, 'preview commit button');
    await page.getByText(commitText).click();
    await page.waitForFunction(() => !window.location.hash.includes('import'), { timeout: 8000 });
}

// All seven days' content coexists in the hub, so text presence does not
// prove selection: the `selected` attribute on the section does.
async function seeSelectedDay(page, header, note) {
    try {
        await page
            .locator(`metro-hub-section[selected][header="${header}"]`)
            .waitFor({ timeout: 8000 });
    } catch {
        throw new Error(`expected the selected day section to be "${header}" (${note})`);
    }
}

// The state lane under `key` must hold `value` before the scenario moves on:
// the attribute a control mirrors and the store write ride the same render,
// but the transaction commits a beat later, and a reload or a reset issued
// in between would race it.
function storedLane(page, key, value) {
    return page.waitForFunction(
        async ({ key, value }) => {
            const db = await new Promise((resolve, reject) => {
                const req = indexedDB.open('briple-training', 1);
                req.onsuccess = () => resolve(req.result);
                req.onerror = () => reject(req.error);
            });
            const stored = await new Promise((resolve, reject) => {
                const tx = db.transaction('state', 'readonly');
                const req = tx.objectStore('state').get(key);
                req.onsuccess = () => resolve(req.result);
                req.onerror = () => reject(req.error);
                tx.oncomplete = () => db.close();
            });
            return stored === value;
        },
        { key, value },
        { timeout: 8000 },
    );
}

// 1. First run with no plan: the empty state.
await scenario('first run shows the empty state (es)', 'es-ES', async (page) => {
    await see(page, 'Tu calendario está vacío.', 'empty title');
    await see(page, 'Cargar un plan', 'load button');
    await see(page, 'Probar el plan de ejemplo', 'sample button');
    // The date header renders even with no data (the calendar is the product).
    await see(page, 'lunes', 'locale weekday in the date header');
});

// 2. Sample import flow: real click, real parse, real preview, real store,
// real toast. Fresh import on Monday 2026-09-14 anchors to this Monday (the
// variant trains today), so the plan line reads Semana 1 · RIR 3.
await scenario('sample import renders the session day (es)', 'es-ES', async (page) => {
    await page.getByText('Probar el plan de ejemplo').click();
    await see(page, 'Importar plan', 'preview title');
    await see(page, 'Plan de Entrenamiento en Circuito - 4 Semanas', 'plan title from the file');
    await see(page, '4 semanas', 'week count');
    await see(page, '3 días — FULL BODY A / B / C (LUNES · MIÉRCOLES · VIERNES)', 'variant listed as a fact');
    await see(page, '¿Cuándo empieza?', 'start-date heading');
    await see(page, '14 de septiembre', 'default anchor on the field');
    await page.getByText('Usar este plan').click();
    await see(page, 'Plan cargado · 3 variantes · 4 semanas', 'import toast');
    await see(page, 'Semana 1 de 4 · RIR 3', 'plan week line with mesociclo RIR');
    await see(page, 'Full Body A', 'session card title');
    await see(page, '6 ejercicios · 2 circuitos', 'card badge line');
    await see(page, 'Sentadilla con barra', 'first exercise');
    await see(page, '6-8 · 1-2 · 3 min', 'verbatim scheme line');
    // The full routine renders flat: no truncation, the last exercise shows.
    await see(page, 'Elevaciones laterales', 'last exercise of the routine');
    await see(page, '12-20 · 0-1 · 90 s', 'last exercise scheme line');
    await see(page, 'Semana 38 · T3', 'ISO week and quarter in the date block');
    // Day hub: seven sections, one per day of the week; Monday is selected.
    const sections = page.locator('metro-hub > metro-hub-section');
    await sections.first().waitFor({ timeout: 8000 });
    const sectionCount = await sections.count();
    if (sectionCount !== 7) {
        throw new Error(`expected 7 day sections in the hub, saw ${sectionCount}`);
    }
    await seeSelectedDay(page, 'lun', 'Monday selected in the day hub');
});

// 3. The same flow through the real file picker ("Load a plan").
await scenario('Load a plan via the file picker', 'es-ES', async (page) => {
    await page.setInputFiles('input[type="file"]', fixture);
    await commitPreview(page);
    await see(page, 'Plan cargado · 3 variantes · 4 semanas', 'import toast');
    await see(page, 'Full Body A', 'session card title');
});

// 4. Reload: the import survives through IndexedDB (local-first round trip).
await scenario('Import survives a reload through IndexedDB', 'es-ES', async (page) => {
    await page.getByText('Probar el plan de ejemplo').click();
    await commitPreview(page);
    await see(page, 'Full Body A', 'session card after import');
    await page.reload();
    await page.waitForSelector('metro-app-bar', { timeout: 15000 });
    await see(page, 'Full Body A', 'session card after reload');
    await see(page, 'Semana 1 de 4 · RIR 3', 'plan week line after reload');
});

// 5. Rest day: the chevron settles the hub on Tuesday and the quiet
//    next-session line shows there.
await scenario('Rest day shows the quiet next-session line', 'es-ES', async (page) => {
    await page.getByText('Probar el plan de ejemplo').click();
    await commitPreview(page);
    await see(page, 'Full Body A', 'session card before moving');
    await page.locator('button.day-chevron:has(metro-icon[icon="forward"])').first().click();
    await seeSelectedDay(page, 'mar', 'chevron moved to Tuesday');
    await see(page, 'martes', 'date block moved to Tuesday');
    await see(page, 'Descanso · próxima sesión: miércoles, Full Body B', 'rest-day line');
});

// 6. Hub selection: tapping a peeking section brings its day into view.
await scenario('Tapping a hub section selects its day', 'es-ES', async (page) => {
    await page.getByText('Probar el plan de ejemplo').click();
    await commitPreview(page);
    await see(page, 'Full Body A', 'Monday card');
    await page.locator('metro-hub > metro-hub-section').nth(2).click();
    await seeSelectedDay(page, 'mié', 'tap moved to Wednesday');
    await see(page, 'miércoles', 'date block moved to Wednesday');
    await page.locator('metro-hub > metro-hub-section').nth(0).click();
    await seeSelectedDay(page, 'lun', 'tap moved back to Monday');
});

// 7. Navigation: ⋯ menu reaches the Plan page, chevron returns.
await scenario('App-bar menu navigates to Plan and back', 'es-ES', async (page) => {
    await page.getByText('Probar el plan de ejemplo').click();
    await commitPreview(page);
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
    await commitPreview(page);
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
    await commitPreview(page, 'Use this plan');
    await see(page, 'Full Body A', 'session card (en chrome, es content)');
});

// 10. Week pivot: range header, chip, rows, summary; a row tap returns to
//     Day with that date selected and the hub gliding to it.
await scenario('Week pivot: rows, summary, and row tap', 'es-ES', async (page) => {
    await page.getByText('Probar el plan de ejemplo').click();
    await commitPreview(page);
    await see(page, 'Full Body A', 'Day rendered');
    await page.getByRole('tab', { name: 'Week' }).click();
    await see(page, 'SEPTIEMBRE', 'week range header');
    await see(page, 'Hoy', 'today jump');
    await see(page, 'descanso', 'rest rows');
    await see(page, '3 sesiones esta semana', 'weekly summary');
    // Monday is today in the fixed clock: it carries the accent bar.
    await page.locator('.week-row.today').waitFor({ timeout: 8000 });
    // Tap the Tuesday row: back to Day, hub on Tuesday.
    await page.locator('.week-row').nth(1).click();
    await see(page, 'martes', 'date block moved to Tuesday');
    await page
        .locator('metro-hub-section[selected][header="mar"]')
        .waitFor({ timeout: 8000 });
});

// 11. View chip: the flyout groups the variants by Genero, a selection
//     switches the variant instantly, and the view persists across reload.
await scenario('View chip switches variant and persists', 'es-ES', async (page) => {
    await page.getByText('Probar el plan de ejemplo').click();
    await commitPreview(page);
    await see(page, 'Hombre · 3 días', 'chip shows the default view');
    await page.locator('metro-button.view-chip').click();
    await see(page, 'Mujer', 'flyout group header');
    await see(page, '5 días — DÍAS MIXTOS', 'Hombre 5dias item label');
    // Select the Mujer variant: the chip updates, the flyout closes, and the
    // day hub re-renders with the new variant's Monday session.
    await page.getByText('L-X-V TREN INFERIOR · M-J TREN SUPERIOR').click();
    await see(page, 'Mujer · 5 días', 'chip shows the new view');
    await page
        .getByText('L-X-V TREN INFERIOR · M-J TREN SUPERIOR')
        .waitFor({ state: 'hidden', timeout: 8000 });
    await see(page, 'Tren inferior · día pesado', "Monday card of the new variant");
    // The selection rode the store's ViewState lane: reload keeps the view.
    await page.reload();
    await page.waitForSelector('metro-app-bar', { timeout: 15000 });
    await see(page, 'Mujer · 5 días', 'view persisted across reload');
    await see(page, 'Tren inferior · día pesado', 'variant card after reload');
});

// 12. Boot fallback: a stored view that names a missing variant resolves to
//     the first variant in file order, and the store value is rewritten so
//     the next boot reads a valid view. The store is seeded by hand before
//     the app boots, through the same two-store shape the app writes.
const fixtureText = readFileSync(fixture, 'utf8');
await scenario('Boot rewrites a stored view naming a missing variant', 'es-ES', async (page) => {
    await page.addInitScript(
        ({ raw }) => {
            const req = indexedDB.open('briple-training', 1);
            req.onupgradeneeded = () => {
                const db = req.result;
                const imports = db.createObjectStore('imports', { keyPath: 'id' });
                imports.createIndex('importedAt', 'importedAt');
                db.createObjectStore('state');
            };
            req.onsuccess = () => {
                const db = req.result;
                const tx = db.transaction(['imports', 'state'], 'readwrite');
                tx.objectStore('imports').put({
                    id: 'seeded-import',
                    fileName: 'plan_entrenamiento_4sem.txt',
                    importedAt: '2026-09-13T10:00:00Z',
                    anchor: '2026-09-07',
                    raw,
                });
                tx.objectStore('state').put('seeded-import', 'activeImportId');
                tx.objectStore('state').put(
                    '{"genero":"hombre","opcionId":"7dias"}',
                    'viewState',
                );
                tx.oncomplete = () => db.close();
            };
        },
        { raw: fixtureText },
    );
    await page.reload();
    await page.waitForSelector('metro-app-bar', { timeout: 15000 });
    // The bogus "7dias" view resolves to the first variant in file order.
    await see(page, 'Hombre · 3 días', 'chip shows the first variant');
    await see(page, 'Full Body A', 'day hub projected the first variant');
    // The store's viewState lane now carries the resolved view.
    const rewritten = await page.waitForFunction(
        async () => {
            const db = await new Promise((resolve, reject) => {
                const req = indexedDB.open('briple-training', 1);
                req.onsuccess = () => resolve(req.result);
                req.onerror = () => reject(req.error);
            });
            const json = await new Promise((resolve, reject) => {
                const tx = db.transaction('state', 'readonly');
                const req = tx.objectStore('state').get('viewState');
                req.onsuccess = () => resolve(req.result);
                req.onerror = () => reject(req.error);
                tx.oncomplete = () => db.close();
            });
            return json ? JSON.parse(json).opcionId === '3dias' : false;
        },
        { timeout: 8000 },
    );
    if (!rewritten) throw new Error('expected the store viewState to be rewritten to 3dias');
});

// 13. Light dismiss: a tap on the backdrop closes the flyout without
//     selecting anything.
await scenario('Flyout light dismiss closes without changing the view', 'es-ES', async (page) => {
    await page.getByText('Probar el plan de ejemplo').click();
    await commitPreview(page);
    await see(page, 'Full Body A', 'default variant rendered');
    await page.locator('metro-button.view-chip').click();
    await see(page, 'Mujer', 'flyout open');
    // The backdrop covers the viewport while the flyout is open; the menu
    // sits top-left, so this tap lands on the backdrop.
    await page.mouse.click(900, 400);
    await page
        .getByText('L-X-V TREN INFERIOR · M-J TREN SUPERIOR')
        .waitFor({ state: 'hidden', timeout: 8000 });
    await see(page, 'Hombre · 3 días', 'view unchanged after dismiss');
    await see(page, 'Full Body A', 'day hub unchanged after dismiss');
});

// 14. Cancel: nothing reaches the store, not even after a reload.
await scenario('Preview cancel stores nothing', 'es-ES', async (page) => {
    await page.getByText('Probar el plan de ejemplo').click();
    await see(page, 'Usar este plan', 'preview open');
    await page.getByText('Cancelar').click();
    await see(page, 'Tu calendario está vacío.', 'back on the empty state');
    await page.reload();
    await page.waitForSelector('metro-app-bar', { timeout: 15000 });
    await see(page, 'Tu calendario está vacío.', 'store untouched after cancel');
});

// 15. Parse warnings render in the preview and do not block the commit. A
//     "Samedi" day is injected into an otherwise clean fixture copy, so the
//     preview shows exactly one unknown-day warning.
const warnedFixture = fixtureText.replace(
    '#end semana',
    '#start dia Samedi\n#meta titulo Test\n#ej A|1|1|Test|1|-|-|-\n#end dia\n#end semana',
);
await scenario('Preview shows parse warnings', 'es-ES', async (page) => {
    await page.setInputFiles('input[type="file"]', {
        name: 'warned.txt',
        mimeType: 'text/plain',
        buffer: Buffer.from(warnedFixture, 'utf8'),
    });
    await see(page, '⚠ 1 aviso del archivo', 'warning count line');
    await see(page, '· línea', 'warning carries its line number');
    await page.getByText('Usar este plan').click();
    await see(page, 'Plan cargado · 3 variantes · 4 semanas', 'import still commits');
    await see(page, 'Full Body A', 'known days still project');
});

// 16. Re-anchor: the Plan page roller writes a new anchor and every view
//     re-projects - Today sits before the new start, so it shows the quiet
//     before-plan line. The roller's gesture is a drag: one item height
//     (40 px) up moves the day one step forward.
await scenario('Re-anchor from the Plan page re-projects', 'es-ES', async (page) => {
    await page.getByText('Probar el plan de ejemplo').click();
    await commitPreview(page);
    await see(page, 'Full Body A', 'imported');
    await page.getByRole('button', { name: 'More options' }).click();
    await page.getByText('Plan de entrenamiento').click();
    await page.waitForURL(/#\/plan/, { timeout: 8000 });
    await see(page, 'Inicio del plan', 'anchor row');
    await see(page, '14 de septiembre', 'current anchor on the field');
    await page.getByText('14 de septiembre').click();
    const dayColumn = page.locator('metro-date-picker-roller .picker-column--day');
    const box = await dayColumn.boundingBox();
    const cx = box.x + box.width / 2;
    const cy = box.y + box.height / 2;
    await page.mouse.move(cx, cy);
    await page.mouse.down();
    await page.mouse.move(cx, cy - 40, { steps: 4 });
    await page.mouse.up();
    await see(page, '15 de septiembre', 'anchor re-projected on the field');
    await page.locator('header metro-hyperlink-button').first().click();
    await see(page, 'El plan aún no empieza.', 'today sits before the new anchor');
});

// 17. The fixed app bar must not cover a page's last content: on Plan, the
//     imports history row stays above the bar's top edge at maximum scroll.
await scenario('Plan imports clear the fixed app bar', 'es-ES', async (page) => {
    await page.getByText('Probar el plan de ejemplo').click();
    await commitPreview(page);
    await page.getByRole('button', { name: 'More options' }).click();
    await page.getByText('Plan de entrenamiento').click();
    await page.waitForURL(/#\/plan/, { timeout: 8000 });
    await see(page, 'Imports', 'imports heading');
    await see(page, 'plan_entrenamiento_4sem.txt', 'imports history row');
    // Scroll the row's scrollable ancestor to the end.
    await page.evaluate(() => {
        const row = [...document.querySelectorAll('.week-row')].find((el) =>
            el.textContent.includes('plan_entrenamiento_4sem.txt'),
        );
        let el = row?.parentElement;
        while (el && el.scrollHeight <= el.clientHeight) el = el.parentElement;
        if (el) el.scrollTop = el.scrollHeight;
    });
    const bar = await page.locator('metro-app-bar').boundingBox();
    const row = await page
        .getByText('plan_entrenamiento_4sem.txt')
        .filter({ visible: true })
        .first()
        .boundingBox();
    if (row.y + row.height > bar.y + 1) {
        throw new Error(
            `imports row bottom (${Math.round(row.y + row.height)}) overlaps the app bar top (${Math.round(bar.y)})`,
        );
    }
});

// 18. Settings: the theme choice applies to the document, is stored, and
//     survives a reload. System is the default: no override attributes
//     exist until the user picks one.
await scenario('Settings theme and accent apply and persist', 'es-ES', async (page) => {
    await page.getByText('Probar el plan de ejemplo').click();
    await commitPreview(page);
    await page.getByRole('button', { name: 'More options' }).click();
    await page.getByText('Ajustes').click();
    await page.waitForURL(/#\/settings/, { timeout: 8000 });
    await see(page, 'Color de énfasis', 'accent heading');
    const themeless = await page.evaluate(
        () => !document.documentElement.hasAttribute('data-theme'),
    );
    if (!themeless) throw new Error('expected no data-theme attribute by default');
    await page.locator('metro-radio-button[value="dark"] .radio').click();
    await page.waitForFunction(
        () => document.documentElement.getAttribute('data-theme') === 'dark',
        { timeout: 8000 },
    );
    await page.getByRole('button', { name: 'Rojo' }).click();
    await page.waitForFunction(
        () => document.documentElement.getAttribute('accent') === 'red',
        { timeout: 8000 },
    );
    // Both choices are on the store before the reload reads them back.
    await storedLane(page, 'theme', 'dark');
    await storedLane(page, 'accent', 'red');
    await page.reload();
    // Settings has no app bar (the shell renders none there); the accent
    // heading proves the app booted back onto the page.
    await see(page, 'Color de énfasis', 'settings page after reload');
    await page.waitForFunction(
        () =>
            document.documentElement.getAttribute('data-theme') === 'dark' &&
            document.documentElement.getAttribute('accent') === 'red',
        { timeout: 8000 },
    );
});

// 19. Reset: the confirm dialog's accept restores the default theme and
//     accent, empties the store (imports and state lanes), and lands on
//     Today's empty state.
await scenario('Reset to defaults clears settings and data', 'es-ES', async (page) => {
    await page.getByText('Probar el plan de ejemplo').click();
    await commitPreview(page);
    await see(page, 'Full Body A', 'imported before reset');
    await page.getByRole('button', { name: 'More options' }).click();
    await page.getByText('Ajustes').click();
    await page.waitForURL(/#\/settings/, { timeout: 8000 });
    // Give the reset something to clear: pick dark first, and let the
    // store absorb the choice before the wipe races it.
    await page.locator('metro-radio-button[value="dark"] .radio').click();
    await page.waitForFunction(
        () => document.documentElement.getAttribute('data-theme') === 'dark',
        { timeout: 8000 },
    );
    await storedLane(page, 'theme', 'dark');
    await page.getByText('Restablecer valores predeterminados').click();
    await page
        .locator('metro-message-dialog')
        .filter({ hasText: 'Se borrarán' })
        .waitFor({ state: 'attached', timeout: 8000 });
    await page
        .locator('metro-message-dialog metro-button')
        .filter({ hasText: 'Restablecer' })
        .click();
    await see(page, 'Tu calendario está vacío.', 'back on the empty state');
    await see(page, 'Ajustes restablecidos', 'reset toast');
    await page.waitForFunction(
        () =>
            !document.documentElement.hasAttribute('data-theme') &&
            !document.documentElement.hasAttribute('accent'),
        { timeout: 8000 },
    );
    const cleared = await page.waitForFunction(async () => {
        const db = await new Promise((resolve, reject) => {
            const req = indexedDB.open('briple-training', 1);
            req.onsuccess = () => resolve(req.result);
            req.onerror = () => reject(req.error);
        });
        const total = await new Promise((resolve, reject) => {
            const tx = db.transaction(['imports', 'state'], 'readonly');
            const imports = tx.objectStore('imports').count();
            const state = tx.objectStore('state').count();
            tx.oncomplete = () => resolve(imports.result + state.result);
            tx.onerror = () => reject(tx.error);
        });
        db.close();
        return total === 0;
    }, { timeout: 8000 });
    if (!cleared) throw new Error('expected the store to be empty after reset');
});

// 20. Session detail: the Today app bar's info command opens the selected
//     day's routine as a plan - mesociclo badge, circuit groups with the
//     A1 idiom and round counts, and the guía trailer - and back returns
//     to Today.
await scenario('Session detail shows circuits, badge, and guide trailer', 'es-ES', async (page) => {
    await page.getByText('Probar el plan de ejemplo').click();
    await commitPreview(page);
    await see(page, 'Full Body A', 'Today rendered');
    const info = page.locator('metro-app-bar-button[icon="info"]');
    await info.waitFor({ timeout: 8000 });
    await info.click();
    await page.waitForURL(/#\/session/, { timeout: 8000 });
    await see(page, 'lunes · 14 de septiembre', 'date caption in the header');
    await see(page, 'SEMANA 1 DE 4 · RIR 3', 'mesociclo badge line');
    await see(page, 'Circuito A · 4 vueltas', 'first circuit header');
    await see(page, 'Circuito B · 3 vueltas', 'second circuit header');
    await see(page, 'A1', 'circuit-order idiom');
    await see(page, 'A2', 'second idiom of circuit A');
    await see(page, 'B1', 'circuit B idiom');
    await see(page, 'Sentadilla con barra', 'first exercise');
    await see(page, '6-8 · 1-2 · 3 min', 'verbatim scheme line');
    await see(page, '· Entre ejercicios del circuito: 60-90 s. Entre circuitos: 2-3 min.', 'rests trailer line');
    await see(page, '· 1. En circuito la percepción de esfuerzo', 'circuit-rule trailer line');
    await page.locator('header metro-hyperlink-button').first().click();
    await see(page, 'Full Body A', 'back on Today');
});

// 21. The info command exists only when the selected day carries a
//     session: a rest day hides it.
await scenario('Session detail command hides on rest days', 'es-ES', async (page) => {
    await page.getByText('Probar el plan de ejemplo').click();
    await commitPreview(page);
    await see(page, 'Full Body A', 'Monday rendered');
    await page.locator('metro-app-bar-button[icon="info"]').waitFor({ timeout: 8000 });
    await page.locator('button.day-chevron:has(metro-icon[icon="forward"])').first().click();
    await seeSelectedDay(page, 'mar', 'chevron moved to Tuesday');
    await page.waitForFunction(
        () => !document.querySelector('metro-app-bar-button[icon="info"]'),
        { timeout: 8000 },
    );
    await see(page, 'Descanso · próxima sesión: miércoles, Full Body B', 'rest-day line still present');
});

// 22. Install: with a `beforeinstallprompt` faked the way Chromium fires it
//     on an installable page, the Settings install command appears, and
//     tapping it hands the captured prompt to the browser (the fake records
//     the handover). The section then goes away: the prompt is spent.
await scenario(
    'Settings install command hands over the browser prompt',
    'es-ES',
    async (page) => {
        await page.getByText('Probar el plan de ejemplo').click();
        await commitPreview(page);
        await page.getByRole('button', { name: 'More options' }).click();
        await page.getByText('Ajustes').click();
        await page.waitForURL(/#\/settings/, { timeout: 8000 });
        await see(page, 'Instalar aplicación', 'install command present');
        await page.getByText('Instalar aplicación').click();
        await page.waitForFunction(() => window.__installPrompted === true, {
            timeout: 8000,
        });
        await page
            .getByText('Instalar aplicación')
            .waitFor({ state: 'hidden', timeout: 8000 });
    },
    // Chromium fires the event once per visit; the app listener may attach a
    // beat after load, so the fake re-fires until the prompt is spent.
    () => {
        let prompted = false;
        const dispatch = () => {
            if (prompted) return;
            const event = new Event('beforeinstallprompt');
            event.prompt = () => {
                prompted = true;
                window.__installPrompted = true;
                return Promise.resolve();
            };
            window.dispatchEvent(event);
        };
        window.addEventListener('load', () => setInterval(dispatch, 300));
    },
);

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
