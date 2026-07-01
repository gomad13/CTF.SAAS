"use client";

import type { CSSProperties, ReactNode } from "react";

/**
 * Option de réponse partagée par les challenges à choix (multichoice, ceo_fraud, etc.).
 *
 * État sélectionné — fix 2026-04-21 : fond primary plein + texte blanc pur.
 * Le texte doit rester **parfaitement lisible** (contrast AA garanti). Aucune
 * opacité ni bleu clair sur primary lorsque l'option est active.
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

const PRIMARY = "#3B82F6";
const PRIMARY_HOVER = "#2563EB";
const TEXT_ON_LIGHT = "#1E293B";
const TEXT_ON_LIGHT_MUTED = "#64748B";

export default function ChoiceOption({ id, label, active, onToggle, letter, icon }: Props) {
    const buttonStyle: CSSProperties = {
        width: "100%",
        display: "flex",
        alignItems: "center",
        gap: 16,
        padding: "16px 20px",
        background: active ? PRIMARY : "#F1F5F9",
        border: `1px solid ${active ? PRIMARY_HOVER : "rgba(59,130,246,0.12)"}`,
        borderRadius: 10,
        cursor: "pointer",
        transition: "all 0.18s ease",
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
                    e.currentTarget.style.background = "#E2E8F0";
                    e.currentTarget.style.borderColor = "rgba(59,130,246,0.25)";
                }
            }}
            onMouseOut={e => {
                if (active) {
                    e.currentTarget.style.background = PRIMARY;
                } else {
                    e.currentTarget.style.background = "#F1F5F9";
                    e.currentTarget.style.borderColor = "rgba(59,130,246,0.12)";
                }
            }}
        >
            {/* Checkbox visuelle */}
            <div style={{
                width: 20,
                height: 20,
                borderRadius: 5,
                border: `2px solid ${active ? "#FFFFFF" : "rgba(59,130,246,0.3)"}`,
                background: active ? "#FFFFFF" : "transparent",
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
                background: active ? "rgba(255,255,255,0.15)" : "rgba(59,130,246,0.08)",
                border: `1px solid ${active ? "rgba(255,255,255,0.35)" : "rgba(59,130,246,0.15)"}`,
                display: "flex",
                alignItems: "center",
                justifyContent: "center",
                flexShrink: 0,
                color: active ? "#FFFFFF" : "#64748B",
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
                color: active ? "#FFFFFF" : TEXT_ON_LIGHT_MUTED,
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
