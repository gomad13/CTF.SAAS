"use client";

import type { CSSProperties, ReactNode } from "react";

/**
 * Option de réponse partagée par les challenges à choix (multichoice, ceo_fraud, etc.).
 *
 * État sélectionné — fix 2026-04-21 : fond primary plein + texte on-accent.
 * Le texte doit rester **parfaitement lisible** (contrast AA garanti). Aucune
 * opacité ni couleur claire sur primary lorsque l'option est active.
 */
type Props = {
    id: string;
    label: string;
    active: boolean;
    onToggle: (id: string) => void;
    /** Affiché dans la pastille à gauche. `letter` OU `icon`, pas les deux. */
    letter?: string;
    icon?: ReactNode;
};

const PRIMARY = "var(--accent)";
const PRIMARY_HOVER = "var(--accent-hover)";
const TEXT_ON_LIGHT = "var(--text)";
const TEXT_ON_LIGHT_MUTED = "var(--text-2)";

export default function ChoiceOption({ id, label, active, onToggle, letter, icon }: Props) {
    const buttonStyle: CSSProperties = {
        width: "100%",
        display: "flex",
        alignItems: "center",
        gap: 16,
        padding: "16px 20px",
        background: active ? PRIMARY : "var(--surface)",
        border: `1px solid ${active ? PRIMARY_HOVER : "var(--border)"}`,
        borderRadius: 10,
        cursor: "pointer",
        transition: "all 0.2s ease",
        textAlign: "left",
    };

    return (
        <button
            type="button"
            onClick={() => onToggle(id)}
            aria-pressed={active}
            data-state={active ? "checked" : "unchecked"}
            style={buttonStyle}
            onMouseOver={e => {
                if (active) {
                    e.currentTarget.style.background = PRIMARY_HOVER;
                } else {
                    e.currentTarget.style.background = "var(--surface-2)";
                    e.currentTarget.style.borderColor = "var(--accent-border)";
                }
            }}
            onMouseOut={e => {
                if (active) {
                    e.currentTarget.style.background = PRIMARY;
                } else {
                    e.currentTarget.style.background = "var(--surface)";
                    e.currentTarget.style.borderColor = "var(--border)";
                }
            }}
        >
            {/* Checkbox visuelle */}
            <div style={{
                width: 20,
                height: 20,
                borderRadius: 5,
                border: `2px solid ${active ? "var(--on-accent)" : "var(--accent-border)"}`,
                background: active ? "var(--on-accent)" : "transparent",
                flexShrink: 0,
                display: "flex",
                alignItems: "center",
                justifyContent: "center",
                transition: "all 0.15s",
            }}>
                {active && <span style={{ color: PRIMARY, fontSize: 12, fontWeight: 700, lineHeight: 1 }}>✓</span>}
            </div>

            {/* Lettre ou icône du choix */}
            <div style={{
                width: 36,
                height: 36,
                borderRadius: 8,
                background: active ? "rgba(255,255,255,0.15)" : "var(--accent-subtle)",
                border: `1px solid ${active ? "rgba(255,255,255,0.35)" : "var(--accent-border)"}`,
                display: "flex",
                alignItems: "center",
                justifyContent: "center",
                flexShrink: 0,
                color: active ? "var(--on-accent)" : "var(--text-2)",
                fontFamily: "'JetBrains Mono', monospace",
                fontWeight: 700,
                fontSize: 14,
                transition: "all 0.15s",
            }}>
                {letter ?? icon}
            </div>

            {/* Texte principal */}
            <span style={{
                fontSize: 14,
                color: active ? "var(--on-accent)" : TEXT_ON_LIGHT_MUTED,
                lineHeight: 1.5,
                fontWeight: active ? 600 : 400,
                transition: "color 0.15s",
            }}>
                {label}
            </span>
        </button>
    );
}

export { TEXT_ON_LIGHT };
