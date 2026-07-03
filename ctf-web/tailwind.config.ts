import type { Config } from 'tailwindcss'

/**
 * Tailwind v4 : la source de vérité des tokens de couleur est `@theme` dans
 * src/app/globals.css (chargé par @tailwindcss/postcss). Ce fichier reste utile
 * comme référence de la charte et pour les outils qui lisent tailwind.config.
 *
 * Charte Sentys (teal) — cf. skill charte-graphique-sentys :
 *   sentys #22C55E · sentys-dark #16A34A · sentys-bg #0A0A0B · sentys-accent #22C55E
 * Utilisables via bg-sentys / text-sentys / border-sentys (définis dans globals.css @theme).
 */
const config: Config = {
  darkMode: 'class',
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
        sentys: '#22C55E',
        'sentys-dark': '#16A34A',
        'sentys-bg': '#0A0A0B',
        'sentys-accent': '#22C55E',
        // Tokens charte demandés (primary/primary-dark/background-dark/accent = teal)
        primary: '#22C55E',
        'primary-hover': '#16A34A',
        'primary-dark': '#16A34A',
        'background-dark': '#0A0A0B',
        accent: '#22C55E',
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
