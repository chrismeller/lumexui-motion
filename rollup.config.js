import { defineConfig } from 'rollup';
import resolve from '@rollup/plugin-node-resolve';
import replace from '@rollup/plugin-replace';
import terser from '@rollup/plugin-terser';

// Bundles the interop module with the parts of `motion` it uses, so the browser never fetches
// the library from a CDN. The output is committed: building the .NET package doesn't need Node.
export default defineConfig({
    input: 'src/Scripts/motion.js',
    output: {
        file: 'src/wwwroot/motion.js',
        format: 'esm',
    },
    plugins: [
        // motion reads process.env.NODE_ENV, which doesn't exist in the browser.
        replace({ preventAssignment: true, values: { 'process.env.NODE_ENV': JSON.stringify('production') } }),
        resolve({ browser: true }),
        terser(),
    ],
});
