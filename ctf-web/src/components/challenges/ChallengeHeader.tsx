"use client";

import { useMemo } from "react";
import { useIsMobile } from "@/hooks/useMediaQuery";
import type { ChallengeItem } from "@/lib/types";

const DIFF_LABELS: Record<number, string> = {
    1: "Facile",
    2: "Moyen",
    3: "Difficile",
    4: "Expert",
    5: "Maître",
};

/**
 * En-tête de la page challenge : badges (difficulté, points, interactif) + titre.
 *
 * Corrige le bug de contraste : le titre était en gris foncé sur le fond sombre
 * du dashboard → quasi invisible. Il est désormais en `--text-on-dark`, grand
 * format et bold.
 */
export default function ChallengeHeader({ challenge: c }: { challenge: ChallengeItem }) {
    const isMobile = useIsMobile();
    const diff = useMemo(() => {
        const label = c.difficulty ? DIFF_LABELS[c.difficulty] ?? "Facile" : "Facile";
        const isFacile = label === "Facile";
        const isMoyen = label === "Moyen";
        return {
            label,
            bg: isFacile ? "rgba(34,197,94,0.12)" : isMoyen ? "rgba(234,179,8,0.12)" : "rgba(239,68,68,0.12)",
            border: isFacile ? "rgba(34,197,94,0.35)" : isMoyen ? "rgba(234,179,8,0.35)" : "rgba(239,68,68,0.35)",
            color: isFacile ? "var(--success-t)" : isMoyen ? "var(--warning-t)" : "var(--danger-t)",
        };
    }, [c.difficulty]);

    return (
        <div style={{ marginBottom: isMobile ? 16 : 24 }}>
            <div style={{ display: "flex", gap: 8, alignItems: "center", marginBottom: isMobile ? 10 : 14, flexWrap: "wrap" }}>
                <span style={{
                    fontSize: 11,
                    padding: "3px 10px",
                    borderRadius: 5,
                    background: diff.bg,
                    border: `1px solid ${diff.border}`,
                    color: diff.color,
                    fontWeight: 600,
                    fontFamily: "'JetBrains Mono', monospace",
                }}>
                    {diff.label}
                </span>
                <span style={{
                    fontSize: 11,
                    padding: "3px 10px",
                    borderRadius: 5,
                    background: "var(--accent-subtle)",
                    border: "1px solid var(--accent-border)",
                    color: "var(--pr-t)",
                    fontFamily: "'JetBrains Mono', monospace",
                    fontWeight: 600,
                }}>
                    {c.points} pts
                </span>
                <span style={{
                    fontSize: 11,
                    padding: "3px 10px",
                    borderRadius: 5,
                    background: "var(--surface-2)",
                    border: "1px solid var(--border)",
                    color: "var(--text-on-dark-muted)",
                    letterSpacing: "0.06em",
                    textTransform: "uppercase",
                    fontFamily: "'JetBrains Mono', monospace",
                }}>
                    Interactif
                </span>
            </div>

            {/* Titre — règle de contraste : clair grand format sur fond sombre */}
            <h1 style={{
                fontSize: isMobile ? 22 : 32,
                fontWeight: 700,
                color: "var(--text-on-dark)",
                margin: 0,
                lineHeight: 1.25,
                letterSpacing: "-0.01em",
                overflowWrap: "break-word",
            }}>
                {c.title}
            </h1>
        </div>
    );
}
