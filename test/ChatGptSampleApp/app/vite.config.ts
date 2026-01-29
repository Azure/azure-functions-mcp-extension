import { defineConfig } from "vite";
import { viteSingleFile } from "vite-plugin-singlefile";
import { resolve, dirname } from "node:path";
import { fileURLToPath } from "node:url";

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);

// Build configuration for single-file output
// Note: vite-plugin-singlefile requires building one entry at a time
// Use BUILD_ENTRY env var to select which page to build
const entry = process.env["BUILD_ENTRY"] || "start-workout";

export default defineConfig({
    plugins: [viteSingleFile()],
    root: "src",
    build: {
        outDir: "../dist",
        emptyOutDir: entry === "start-workout", // Only empty on first build
        target: "esnext",
        rollupOptions: {
            input: resolve(__dirname, `src/${entry}.html`),
        },
    },
});
