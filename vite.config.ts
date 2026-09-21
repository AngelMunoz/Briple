import { defineConfig } from 'vite';
import { VitePWA } from 'vite-plugin-pwa';

export default defineConfig({
  plugins: [
    VitePWA({
      registerType: 'autoUpdate',
      injectRegister: 'auto',
      // Static files fetched at runtime or linked from the head, but not
      // manifest icons: the sample plan is fetched on demand, and the light
      // icons are only referenced by the head's media-query links (the
      // manifest cannot pick icons by color scheme). The spare light 192/512
      // icons stay out: nothing references them, so they must not fill the
      // offline cache.
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
        description:
          'Import a training plan file and browse it as a calendar. Everything runs on the device; no account or server is involved.',
        // Metro blue: metrino's default accent, and the theme-color meta
        // value on index.html; the two must match.
        theme_color: '#0078d4',
        background_color: '#ffffff',
        display: 'standalone',
        icons: [
          { src: 'pwa-64x64.png', sizes: '64x64', type: 'image/png' },
          { src: 'pwa-192x192.png', sizes: '192x192', type: 'image/png' },
          { src: 'pwa-512x512.png', sizes: '512x512', type: 'image/png' },
          { src: 'maskable-icon-512x512.png', sizes: '512x512', type: 'image/png', purpose: 'maskable' },
        ],
      },
    }),
  ],
});
