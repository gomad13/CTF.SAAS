"use client";

import { Moon, Sun } from "lucide-react";
import { useTheme } from "@/hooks/useTheme";

/**
 * Bascule mode sombre/clair (charte Sentys). Accessible (aria-label, focus visible,
 * cible tactile 40px). Persiste via useTheme (localStorage `ctf_theme` + data-theme + .dark).
 */
export default function ThemeToggle({ className = "" }: { className?: string }) {
    const { theme, toggleTheme } = useTheme();
    const dark = theme === "dark";
    return (
        <button
            type="button"
            onClick={toggleTheme}
            aria-label={dark ? "Passer en mode clair" : "Passer en mode sombre"}
            title={dark ? "Mode clair" : "Mode sombre"}
            className={`inline-flex items-center justify-center rounded-lg transition-colors duration-200 ${className}`}
            style={{
                width: 40, height: 40, minWidth: 40, minHeight: 40,
                background: "var(--surface)",
                border: "1px solid var(--border)",
                color: "var(--text-2)",
            }}
            onMouseEnter={(e) => { e.currentTarget.style.color = "var(--accent)"; e.currentTarget.style.borderColor = "var(--accent)"; }}
            onMouseLeave={(e) => { e.currentTarget.style.color = "var(--text-2)"; e.currentTarget.style.borderColor = "var(--border)"; }}
        >
            {dark ? <Sun size={18} /> : <Moon size={18} />}
        </button>
    );
}
