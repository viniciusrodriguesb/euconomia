import { resolve } from 'node:path';
import angular from '@analogjs/vite-plugin-angular';
import { defineConfig } from 'vitest/config';

export default defineConfig({
  plugins: [angular({ tsconfig: resolve(import.meta.dirname, 'tsconfig.spec.json'), jit: true })],
  ssr: { noExternal: [/^@angular\//] },
  test: {
    environment: 'jsdom',
    include: ['src/**/*.spec.ts'],
    setupFiles: ['test/setup.ts'],
    maxWorkers: 3,
    pool: 'forks',
    coverage: {
      provider: 'v8',
      include: ['src/**/*.ts'],
      // Interfaces são removidas pelo TypeScript e não têm código executável.
      exclude: ['**/*.spec.ts', '**/*.dto.ts'],
      reporter: ['text', 'html', 'lcov', 'json-summary'],
      thresholds: { lines: 90, functions: 90, branches: 90, statements: 90, perFile: true }
    }
  }
});
