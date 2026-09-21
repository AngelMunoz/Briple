import { defaultAssetName, defineConfig, minimal2023Preset } from '@vite-pwa/assets-generator/config';

// Primary (dark) set: white line art on black. The plain path prefix makes
// the generator write into public/ while the sources stay at the repo root,
// where Vite does not serve or bundle them.
// Outputs: public/pwa-64x64.png, public/pwa-192x192.png, public/pwa-512x512.png,
// public/maskable-icon-512x512.png, public/apple-touch-icon-180x180.png,
// public/favicon.ico.
export default defineConfig({
  images: ['Briple-bw.jpeg'],
  preset: {
    ...minimal2023Preset,
    transparent: {
      ...minimal2023Preset.transparent,
      favicons: [[48, 'public/favicon.ico']],
    },
    assetName(type, size) {
      return `public/${defaultAssetName(type, size)}`;
    },
  },
});
