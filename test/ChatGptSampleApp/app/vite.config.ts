import { defineConfig } from "vite";
import { viteSingleFile } from "vite-plugin-singlefile";
import { resolve, dirname } from "node:path";
import { fileURLToPath } from "node:url";

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);

// Get the target from command line args or default to start-workout
const target = process.env.BUILD_TARGET || "start-workout";

export default defineConfig({
    plugins: [viteSingleFile()],
    root: "src",
    build: {
        outDir: "../dist",
        emptyOutDir: false, // Don't clear so we can build multiple files
        target: "esnext",
        rollupOptions: {
            input: resolve(__dirname, `src/${target}.html`),
        },
    },
});
