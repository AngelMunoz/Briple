/// <reference lib="webworker" />
// The hand-written worker: workbox precaching keeps the generated worker's
// behavior, and the share-target handler receives other apps' shares.
// The handoff cache name, key, and format must stay in sync with
// App/Intents.fs.
import { cleanupOutdatedCaches, precacheAndRoute } from 'workbox-precaching';

declare let self: ServiceWorkerGlobalScope;

precacheAndRoute(self.__WB_MANIFEST);
cleanupOutdatedCaches();

const HANDOFF_CACHE = 'briple-handoff';
const HANDOFF_KEY = '/__pending-share';
const DEFAULT_NAME = 'shared.briple';

function startUrl(): string {
  return new URL('./', self.registration.scope).href;
}

async function handleShare(request: Request): Promise<Response> {
  const form = await request.formData();
  let name = DEFAULT_NAME;
  let text = '';

  const sharedName = form.get('name');
  if (typeof sharedName === 'string' && sharedName !== '') name = sharedName;

  // One shared file wins over a text field; the file's own name travels
  // with it. The entry stores the name on the first line and the plan text
  // after it.
  const file = form.get('plan');
  if (file && typeof file !== 'string') {
    name = file.name;
    text = await file.text();
  } else {
    const sharedText = form.get('text');
    if (typeof sharedText === 'string') text = sharedText;
  }

  if (text !== '') {
    const cache = await caches.open(HANDOFF_CACHE);
    await cache.put(HANDOFF_KEY, new Response(`${name}\n${text}`));
  }
  return Response.redirect(startUrl(), 303);
}

self.addEventListener('fetch', (event) => {
  const url = new URL(event.request.url);
  if (event.request.method === 'POST' && url.pathname.endsWith('/share-target')) {
    event.respondWith(handleShare(event.request));
  }
});
