import { defaultAssetName, defineConfig, minimal2023Preset } from '@vite-pwa/assets-generator/config';

// Light set: black line art on white, for the prefers-color-scheme icon
// links on index.html. The manifest cannot pick icons by scheme, so its
// assets stay on the dark set; the light 192/512 icons have no consumer yet
// and are kept for future use. Maskable is skipped: a maskable icon is only
// read from the manifest, where scheme selection does not exist. No favicons
// entry here, so the dark set's favicon.ico is left untouched.
// Outputs: public/pwa-64x64-light.png, public/pwa-192x192-light.png,
// public/pwa-512x512-light.png, public/apple-touch-icon-180x180-light.png.
export default defineConfig({
  images: ['Briple-wb.jpeg'],
  preset: {
    ...minimal2023Preset,
    transparent: { sizes: [64, 192, 512] },
    maskable: { sizes: [] },
    apple: { sizes: [180] },
    assetName(type, size) {
      const name = defaultAssetName(type, size);
      const dot = name.lastIndexOf('.');
      return `public/${name.slice(0, dot)}-light${name.slice(dot)}`;
    },
  },
});
