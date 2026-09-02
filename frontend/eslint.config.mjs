// @ts-check
import withNuxt from './.nuxt/eslint.config.mjs'

export default withNuxt({
    rules: {
        '@typescript-eslint/no-unused-vars': 'warn',
        '@typescript-eslint/no-explicit-any': 'warn',
        'vue/html-self-closing': [
            'warn',
            {
                html: { void: 'always', normal: 'always', component: 'always' },
                svg: 'always',
                math: 'always',
            },
        ],
    },
})
