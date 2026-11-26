import sonarjs from 'eslint-plugin-sonarjs';
import parser from '@typescript-eslint/parser';

export default [
  { ignores: ['dist/**', 'coverage/**', 'test-results/**', 'playwright-report/**'] },
  {
    files: ['src/**/*.ts'],
    languageOptions: { parser },
    plugins: { sonarjs },
    rules: {
      ...sonarjs.configs.recommended.rules
    }
  }
];
