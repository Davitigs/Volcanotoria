import tseslint from 'typescript-eslint';

export default [
  { ignores: ['dist/**', 'node_modules/**', '.angular/**'] },
  {
    files: ['src/**/*.ts'],
    languageOptions: { parser: tseslint.parser },
    rules: {
      'no-restricted-imports': [
        'error',
        {
          patterns: [{ group: ['@hazard/*/*'], message: 'Use the public entry point.' }],
        },
      ],
    },
  },
  {
    files: ['src/app/features/hazard/ui-*/**/*.ts'],
    rules: {
      'no-restricted-imports': [
        'error',
        {
          patterns: [
            {
              group: [
                '@hazard/data-access',
                '**/data-access/**',
                '@hazard/feature-dashboard',
                '**/feature-dashboard/**',
                '@angular/common/http',
              ],
              message: 'Presentation code must receive data through inputs.',
            },
          ],
        },
      ],
    },
  },
  {
    files: ['src/app/features/hazard/data-access/**/*.ts'],
    rules: {
      'no-restricted-imports': [
        'error',
        {
          patterns: [
            {
              group: [
                '@hazard/ui-*',
                '**/ui-*/**',
                '@hazard/feature-dashboard',
                '**/feature-dashboard/**',
              ],
              message: 'Data access must not depend on presentation.',
            },
          ],
        },
      ],
    },
  },
  {
    files: ['src/app/shared/**/*.ts'],
    rules: {
      'no-restricted-imports': [
        'error',
        {
          patterns: [
            {
              group: [
                '@hazard/data-access',
                '@hazard/ui-*',
                '@hazard/feature-dashboard',
                '**/features/**',
              ],
              message: 'Shared code must not import feature implementation.',
            },
          ],
        },
      ],
    },
  },
];
