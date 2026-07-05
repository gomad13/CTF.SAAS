"use client";

import { useCallback } from "react";
import { Target, ArrowRight } from "lucide-react";

/**
 * Bloc d'introduction pédagogique affiché AVANT que l'apprenant commence
 * l'exercice. Présente la consigne (titre + corps) et un bouton « Commencer ».
 *
 * Fallback gracieux : le parent ne doit rendre ce composant que si `body` (ou
 * `title`) est non vide. Ici on suppose qu'au moins le corps est fourni.
 */
export default function ChallengeIntro({
    title,
    body,
    onStart,
}: {
    title?: string | null;
    body: string;
    onStart: () => void;
}) {
    const handleStart = useCallback(() => onStart(), [onStart]);

    return (
        <div
            style={{
                maxWidth: 800,
                margin: "0 auto",
                background: "rgba(255,255,255,0.04)",
                border: "1px solid rgba(148,163,184,0.20)",
                borderRadius: 16,
                padding: "clamp(20px, 5vw, 32px)",
            }}
        >
            <div style={{ display: "flex", alignItems: "center", gap: 12, marginBottom: 16 }}>
                <span
                    style={{
                        display: "inline-flex",
                        alignItems: "center",
                        justifyContent: "center",
                        width: 40,
                        height: 40,
                        borderRadius: 10,
                        background: "var(--accent-subtle)",
                        flexShrink: 0,
                    }}
                >
                    <Target size={22} strokeWidth={2} color="var(--pr)" />
                </span>
                <h2
                    style={{
                        fontSize: 22,
                        fontWeight: 700,
                        color: "var(--text-on-dark)",
                        margin: 0,
                        lineHeight: 1.3,
                    }}
                >
                    {title?.trim() ? title : "Consigne de l'exercice"}
                </h2>
            </div>

            <p
                style={{
                    whiteSpace: "pre-line",
                    color: "var(--text-on-dark-muted)",
                    fontSize: 15,
                    lineHeight: 1.65,
                    margin: 0,
                    marginBottom: 28,
                }}
            >
                {body}
            </p>

            <button
                onClick={handleStart}
                style={{
                    display: "inline-flex",
                    alignItems: "center",
                    gap: 8,
                    background: "var(--pr)",
                    color: "var(--on-accent)",
                    border: "none",
                    borderRadius: 10,
                    padding: "12px 22px",
                    fontSize: 15,
                    fontWeight: 600,
                    cursor: "pointer",
                    transition: "background-color 0.2s",
                }}
                onMouseOver={(e) => { e.currentTarget.style.background = "var(--pr-h)"; }}
                onMouseOut={(e) => { e.currentTarget.style.background = "var(--pr)"; }}
            >
                Commencer l&apos;exercice
                <ArrowRight size={18} strokeWidth={2} />
            </button>
        </div>
    );
}
