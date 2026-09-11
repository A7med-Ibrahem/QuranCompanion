import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import { VitePWA } from "vite-plugin-pwa";
import path from "path";

// Offline support (spec section 17):
//  - Quran text (surah list + each surah's ayahs) is cached the first time it's
//    read online, so it's available again with no connection - "read your Wird"
//    should never depend on a live connection once you've opened a surah before.
//  - Mutations that record what you did (reading progress, marking a Wird
//    complete) are queued via Background Sync when offline and replayed
//    automatically the moment connectivity returns - nothing is lost, it just
//    arrives late.
//  - The original Quran text itself is never touched by any of this - caching
//    only ever stores an exact copy of what the API already returned.
export default defineConfig({
  plugins: [
    react(),
    VitePWA({
      registerType: "autoUpdate",
      devOptions: { enabled: true }, // lets offline support be tested via `npm run dev`, not just a production build
      includeAssets: ["icon-192.png", "icon-512.png", "icon-512-maskable.png"],
      manifest: {
        name: "وِرْد - رفيقك في القرآن",
        short_name: "وِرْد",
        description: "اقرأ وِردك. تابع رحلتك. شجّع من تثق به.",
        lang: "ar",
        dir: "rtl",
        theme_color: "#2F5D50",
        background_color: "#F4EFE3",
        display: "standalone",
        start_url: "/",
        scope: "/",
        icons: [
          { src: "icon-192.png", sizes: "192x192", type: "image/png" },
          { src: "icon-512.png", sizes: "512x512", type: "image/png" },
          { src: "icon-512-maskable.png", sizes: "512x512", type: "image/png", purpose: "maskable" }
        ]
      },
      workbox: {
        // App shell (JS/CSS/HTML) - precached automatically by vite-plugin-pwa.
        globPatterns: ["**/*.{js,css,html,svg,png,ico}"],
        navigateFallback: "/index.html",
        runtimeCaching: [
          {
            // Surah list + each surah's full ayah text - read-only Quran content,
            // safe to keep for a long time once fetched.
            urlPattern: ({ url }) => /\/api\/quran\/surahs(\/\d+)?$/.test(url.pathname),
            handler: "CacheFirst",
            options: {
              cacheName: "quran-text-cache",
              expiration: { maxEntries: 130, maxAgeSeconds: 60 * 60 * 24 * 365 },
              cacheableResponse: { statuses: [0, 200] }
            }
          },
          {
            // Tafsir - also read-only once fetched, but from a third-party source
            // that might change, so kept for a shorter while than the Quran text itself.
            urlPattern: ({ url }) => /\/api\/quran\/surahs\/\d+\/ayahs\/\d+\/tafsir$/.test(url.pathname),
            handler: "CacheFirst",
            options: {
              cacheName: "tafsir-cache",
              expiration: { maxEntries: 300, maxAgeSeconds: 60 * 60 * 24 * 30 },
              cacheableResponse: { statuses: [0, 200] }
            }
          },
          {
            // Saving reading position while offline - queued, replayed on reconnect.
            urlPattern: ({ url }) => /\/api\/reading-progress\/me$/.test(url.pathname),
            method: "PUT",
            handler: "NetworkOnly",
            options: {
              backgroundSync: {
                name: "reading-progress-queue",
                options: { maxRetentionTime: 24 * 60 }
              }
            }
          },
          {
            // Marking today's Wird complete while offline - queued, replayed on reconnect.
            urlPattern: ({ url }) => /\/api\/wird\/complete$/.test(url.pathname),
            method: "POST",
            handler: "NetworkOnly",
            options: {
              backgroundSync: {
                name: "wird-complete-queue",
                options: { maxRetentionTime: 24 * 60 }
              }
            }
          }
        ]
      }
    })
  ],
  resolve: {
    alias: {
      "@": path.resolve(__dirname, "./src")
    }
  },
  server: {
    port: 5173
  }
});
