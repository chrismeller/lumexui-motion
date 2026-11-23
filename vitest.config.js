import { defineConfig } from 'vitest/config';

// Unit tests for the interop module in src/Scripts (pnpm test). jsdom stands in for the browser; `motion` itself is
// mocked in the tests, so they check what the module asks it to animate, not the animation.
export default defineConfig({
    test: {
        environment: 'jsdom',
        include: ['src/Scripts/**/*.test.js'],
        // The layout registry test checks that an element can be garbage collected, which needs gc().
        pool: 'forks',
        execArgv: ['--expose-gc'],
    },
});
