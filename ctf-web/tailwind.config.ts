import type { Config } from 'tailwindcss'

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
        primary: '#3B82F6',
        'primary-hover': '#2563EB',
        'primary-dark': '#2563EB',
        'background-dark': '#0F172A',
        accent: '#10B981',
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
