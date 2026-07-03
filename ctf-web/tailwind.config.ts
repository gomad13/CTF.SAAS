import type { Config } from 'tailwindcss'

/**
 * Tailwind v4 : la source de vérité des tokens de couleur est `@theme` dans
 * src/app/globals.css (chargé par @tailwindcss/postcss). Ce fichier reste utile
 * comme référence de la charte et pour les outils qui lisent tailwind.config.
 *
 * Charte Sentys (teal) — cf. skill charte-graphique-sentys :
 *   sentys #03b5aa · sentys-dark #037971 · sentys-bg #023436 · sentys-accent #00bfb3
 * Utilisables via bg-sentys / text-sentys / border-sentys (définis dans globals.css @theme).
 */
const config: Config = {
  content: [
    './src/pages/**/*.{js,ts,jsx,tsx,mdx}',
    './src/components/**/*.{js,ts,jsx,tsx,mdx}',
    './src/app/**/*.{js,ts,jsx,tsx,mdx}',
  ],
  theme: {
    extend: {
      colors: {
        canvas: '#F8FAFC',
        surface: '#FFFFFF',
        sidebar: '#0F172A',
        'sidebar-fg': '#94A3B8',
        // Charte Sentys (teal) — go-forward
        sentys: '#03b5aa',
        'sentys-dark': '#037971',
        'sentys-bg': '#023436',
        'sentys-accent': '#00bfb3',
        // Tokens charte demandés (primary/primary-dark/background-dark/accent = teal)
        primary: '#03b5aa',
        'primary-hover': '#037971',
        'primary-dark': '#037971',
        'background-dark': '#023436',
        accent: '#00bfb3',
        success: '#10B981',
        danger: '#EF4444',
        warning: '#F59E0B',
        border: '#E2E8F0',
        'table-head': '#F1F5F9',
        'fg-heading': '#1E293B',
        'fg-body': '#334155',
        'fg-muted': '#64748B',
        'text-on-light': '#0F172A',
        'text-on-light-muted': '#475569',
        'text-on-light-faint': '#64748B',
        'text-on-dark': '#FFFFFF',
        'text-on-dark-muted': '#CBD5E1',
        'text-on-dark-faint': '#94A3B8',
      },
      fontFamily: {
        sans: ['var(--font-inter)', 'Plus Jakarta Sans', 'system-ui', 'sans-serif'],
      },
    },
  },
  plugins: [],
}

export default config
