import { defineConfig } from 'vite';
import { VitePWA } from 'vite-plugin-pwa';

export default defineConfig({
  plugins: [
    VitePWA({
      registerType: 'autoUpdate',
      injectRegister: 'auto',
      // A hand-written worker: the share target POST must reach the fetch
      // handler; workbox precaching keeps the generated worker's behavior.
      strategies: 'injectManifest',
      srcDir: 'src',
      filename: 'sw.ts',
      includeAssets: [
        'favicon.ico',
        'pwa-64x64-light.png',
        'apple-touch-icon-180x180-light.png',
        'apple-touch-icon-180x180.png',
        'samples/*.txt',
      ],
      manifest: {
        name: 'Briple',
        short_name: 'Briple',
        lang: 'es-MX',
        categories: ['fitness', 'training', 'personal'],
        description:
          'Import a training plan file and browse it as a calendar. Everything runs on the device; no account or server is involved.',
        // Metro blue: metrino's default accent, and the theme-color meta
        // value on index.html; the two must match.
        theme_color: '#4a00c8',
        background_color: '#000000',
        display: 'standalone',
        // A repeat launch (file open, deep link) lands in the running
        // window instead of a new one; the launchQueue consumer re-fires.
        launch_handler: { client_mode: 'focus-existing' },
        // Desktop Chromium only: opening a .briple file from the file
        // manager launches the app and hands it the file.
        file_handlers: [
          { action: './', accept: { 'text/plain': ['.briple'] } },
        ],
        // The %s token is mandatory; the handled link arrives as the
        // `link` query value and the app routes it (today, plan, session,
        // import).
        protocol_handlers: [
          { protocol: 'web+bripple', url: './?link=%s' },
        ],
        // Installed apps on Android and Windows appear in the share sheet:
        // the browser POSTs the shared content to the action URL and the
        // service worker answers it.
        share_target: {
          action: './share-target',
          method: 'POST',
          enctype: 'multipart/form-data',
          params: {
            files: [
              { name: 'plan', accept: ['text/plain', '.briple', '.txt'] },
            ],
            text: 'text',
            title: 'name',
          },
        },
        // The rich install UI shows at most 8 narrow screenshots, in array
        // order; keep the strongest ones first.
        screenshots: [
          {
            src: 'screenshots/today-light.jpg',
            sizes: '930x2046',
            type: 'image/jpeg',
            form_factor: 'narrow',
            label: "Today screen showing the day's workout",
          },
          {
            src: 'screenshots/today-dark.jpg',
            sizes: '930x2046',
            type: 'image/jpeg',
            form_factor: 'narrow',
            label: 'Today screen in dark theme',
          },
          {
            src: 'screenshots/today-week-light.jpg',
            sizes: '930x2046',
            type: 'image/jpeg',
            form_factor: 'narrow',
            label: 'Week view of the training plan',
          },
          {
            src: 'screenshots/today-multi-plan-light.jpg',
            sizes: '930x2046',
            type: 'image/jpeg',
            form_factor: 'narrow',
            label: 'Switching between the variants of an imported plan',
          },
          {
            src: 'screenshots/settings-dark.jpg',
            sizes: '930x2046',
            type: 'image/jpeg',
            form_factor: 'narrow',
            label: 'Settings: theme, accent color and reset',
          },
        ],
        icons: [
          { src: 'pwa-64x64.png', sizes: '64x64', type: 'image/png' },
          { src: 'pwa-192x192.png', sizes: '192x192', type: 'image/png' },
          { src: 'pwa-512x512.png', sizes: '512x512', type: 'image/png' },
          {
            src: 'maskable-icon-512x512.png',
            sizes: '512x512',
            type: 'image/png',
            purpose: 'maskable',
          },
        ],
      },
    }),
  ],
});
