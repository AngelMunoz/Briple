import { defineConfig } from 'vite';
import { VitePWA } from 'vite-plugin-pwa';

export default defineConfig({
  plugins: [
    VitePWA({
      registerType: 'autoUpdate',
      injectRegister: 'auto',
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
