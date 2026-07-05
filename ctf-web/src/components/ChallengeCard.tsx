"use client";

import { useRouter } from "next/navigation";
import type { ChallengeItem } from "@/lib/types";

const DIFF_LABELS: Record<number, string> = {
    1: "Facile",
    2: "Moyen",
    3: "Difficile",
    4: "Expert",
    5: "Maître",
};

type Props = {
    challenge: ChallengeItem;
    completed?: boolean;
    onClick?: () => void;
};

export default function ChallengeCard({ challenge, completed = false, onClick }: Props) {
    const router = useRouter();
    const difficulty = challenge.difficulty ? DIFF_LABELS[challenge.difficulty] : "Facile";

    const isFacile    = difficulty === "Facile";
    const isMoyen     = difficulty === "Moyen";

    const diffBg     = isFacile ? "rgba(34,197,94,0.12)" : isMoyen ? "rgba(234,179,8,0.12)" : "rgba(239,68,68,0.12)";
    const diffBorder = isFacile ? "rgba(34,197,94,0.3)"  : isMoyen ? "rgba(234,179,8,0.3)"  : "rgba(239,68,68,0.3)";
    const diffColor  = isFacile ? "var(--success-t)"     : isMoyen ? "var(--warning-t)"     : "var(--danger-t)";

    function handleRowClick() {
        if (onClick) onClick();
        else router.push(`/dashboard/challenge/${challenge.id}`);
    }

    function handleButtonClick(e: React.MouseEvent) {
        e.stopPropagation();
        if (onClick) onClick();
        else router.push(`/dashboard/challenge/${challenge.id}`);
    }

    return (
        <div
            style={{
                display: "flex",
                alignItems: "center",
                justifyContent: "space-between",
                flexWrap: "wrap",
                padding: "16px 20px",
                background: "var(--bg-card)",
                border: "1px solid var(--border)",
                borderBottom: "1px solid var(--border)",
                borderRadius: 10,
                cursor: "pointer",
                transition: "background 0.2s, border-color 0.2s",
                gap: 16,
            }}
            onMouseEnter={e => { e.currentTarget.style.background = "var(--accent-subtle)"; e.currentTarget.style.borderColor = "var(--border-focus)"; }}
            onMouseLeave={e => { e.currentTarget.style.background = "var(--bg-card)"; e.currentTarget.style.borderColor = "var(--border)"; }}
            onClick={handleRowClick}
        >
            {/* Gauche : badges + titre */}
            <div style={{ display: "flex", flexDirection: "column", gap: 6, minWidth: 160, flex: 1 }}>
                <div style={{ display: "flex", gap: 8, alignItems: "center", flexWrap: "wrap" }}>
                    <span style={{
                        fontSize: 10,
                        padding: "2px 8px",
                        background: "var(--surface-2)",
                        border: "1px solid var(--border)",
                        borderRadius: 4,
                        color: "var(--text-secondary)",
                        fontFamily: "'JetBrains Mono', monospace",
                    }}>
                        interactive
                    </span>

                    <span style={{
                        fontSize: 10,
                        padding: "2px 8px",
                        borderRadius: 4,
                        background: diffBg,
                        border: `1px solid ${diffBorder}`,
                        color: diffColor,
                        fontFamily: "'JetBrains Mono', monospace",
                    }}>
                        {difficulty}
                    </span>

                    {completed && (
                        <span style={{
                            fontSize: 10,
                            padding: "2px 8px",
                            borderRadius: 4,
                            background: "var(--accent-subtle)",
                            border: "1px solid var(--accent-border)",
                            color: "var(--pr)",
                            fontFamily: "'JetBrains Mono', monospace",
                        }}>
                            ✓ Complété
                        </span>
                    )}
                </div>

                <span style={{
                    fontSize: 15,
                    fontWeight: 600,
                    color: "var(--text-primary)",
                    overflow: "hidden",
                    textOverflow: "ellipsis",
                    whiteSpace: "nowrap",
                }}>
                    {challenge.title}
                </span>
            </div>

            {/* Droite : points + bouton Lancer */}
            <div style={{ display: "flex", alignItems: "center", gap: 20, flexShrink: 0 }}>
                <div style={{ textAlign: "right" }}>
                    <div style={{
                        fontSize: 18,
                        fontWeight: 700,
                        fontFamily: "'JetBrains Mono', monospace",
                        color: "var(--pr)",
                    }}>
                        {challenge.points}
                    </div>
                    <div style={{
                        fontSize: 10,
                        color: "var(--text-muted)",
                        letterSpacing: "0.08em",
                        fontFamily: "'JetBrains Mono', monospace",
                    }}>
                        pts
                    </div>
                </div>

                <button
                    onClick={handleButtonClick}
                    style={{
                        background: completed
                            ? "transparent"
                            : "linear-gradient(135deg, var(--accent), var(--accent-hover))",
                        border: completed
                            ? "1px solid var(--accent-border)"
                            : "none",
                        color: completed ? "var(--pr)" : "var(--on-accent)",
                        fontWeight: 700,
                        fontSize: 12,
                        fontFamily: "'JetBrains Mono', monospace",
                        letterSpacing: "0.06em",
                        padding: "8px 18px",
                        borderRadius: 7,
                        cursor: "pointer",
                        whiteSpace: "nowrap",
                        transition: "all 0.2s",
                    }}
                    onMouseEnter={e => {
                        e.currentTarget.style.boxShadow = "0 0 14px var(--accent-border)";
                        e.currentTarget.style.transform = "translateY(-1px)";
                    }}
                    onMouseLeave={e => {
                        e.currentTarget.style.boxShadow = "none";
                        e.currentTarget.style.transform = "translateY(0)";
                    }}
                >
                    {completed ? "↺ Refaire" : "▶ Lancer"}
                </button>
            </div>
        </div>
    );
}
