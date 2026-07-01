"use client";

import { useCallback, useEffect, useState } from "react";
import { apiFetch } from "@/lib/api";

// ── Types ─────────────────────────────────────────────────────────────────────

interface ChallengeCompletion {
    challengeId: string;
    challengeTitle: string;
    maxPoints: number;
    pointsEarned: number;
    scorePercent: number;
    completed: boolean;
    completedAt: string | null;
}

interface DemoProgressData {
    totalPoints: number;
    maxPoints: number;
    completions: ChallengeCompletion[];
    allCompleted: boolean;
}

interface Props {
    /** Increment to trigger a refetch (pass a counter that changes after each submit) */
    refreshKey?: number;
    /** Called after a successful reset */
    onReset?: () => void;
}

// ── Component ─────────────────────────────────────────────────────────────────

export default function DemoProgress({ refreshKey = 0, onReset }: Props) {
    const [data, setData]         = useState<DemoProgressData | null>(null);
    const [resetting, setResetting] = useState(false);
    const [confirmReset, setConfirmReset] = useState(false);

    const load = useCallback(async () => {
        try {
            const res = await apiFetch<DemoProgressData>("/api/progress/demo");
            setData(res);
        } catch {
            // silent — user might not be logged in
        }
    }, []);

    useEffect(() => { load(); }, [load, refreshKey]);

    async function handleReset() {
        if (!confirmReset) { setConfirmReset(true); return; }
        setResetting(true);
        try {
            await apiFetch("/api/progress/demo/reset", { method: "POST" });
            setConfirmReset(false);
            await load();
            onReset?.();
        } finally {
            setResetting(false);
        }
    }

    if (!data) return null;

    const pct = data.maxPoints > 0 ? Math.round((data.totalPoints / data.maxPoints) * 100) : 0;

    return (
        <div style={{
            position: "sticky", bottom: 0, zIndex: 40,
            background: "var(--bg-surface)",
            borderTop: "1px solid rgba(59,130,246,0.2)",
            padding: "8px var(--page-x)",
            minHeight: 56,
            display: "flex", alignItems: "center", flexWrap: "wrap", gap: 12,
        }}>
            {/* Progress bar + score */}
            <div style={{ display: "flex", alignItems: "center", gap: 10, flex: 1, minWidth: 0 }}>
                <span style={{ color: "#64748B", fontSize: 12, whiteSpace: "nowrap" }}>Parcours Demo</span>

                {/* Bar */}
                <div style={{ flex: 1, height: 6, background: "#1e293b", borderRadius: 99, overflow: "hidden", maxWidth: 160 }}>
                    <div style={{
                        height: "100%", width: `${pct}%`,
                        background: "var(--pr)", borderRadius: 99,
                        transition: "width 0.5s ease",
                    }} />
                </div>

                {/* Score */}
                <span style={{ fontSize: 13, fontWeight: 700, color: "var(--pr)", whiteSpace: "nowrap" }}>
                    {data.totalPoints} <span style={{ color: "#64748B", fontWeight: 400 }}>/ {data.maxPoints} pts</span>
                </span>
            </div>

            {/* Per-challenge pastilles */}
            <div style={{ display: "flex", gap: 6, alignItems: "center" }}>
                {data.completions.map((c, i) => (
                    <div
                        key={c.challengeId}
                        title={`${c.challengeTitle} — ${c.pointsEarned}/${c.maxPoints} pts`}
                        style={{
                            display: "flex", alignItems: "center", gap: 4,
                            fontSize: 11, color: c.completed ? "#86efac" : "#4b5563",
                        }}
                    >
                        <span style={{
                            width: 16, height: 16, borderRadius: "50%",
                            background: c.completed ? "#10B981" : "#1e293b",
                            border: `1.5px solid ${c.completed ? "#10B981" : "#334155"}`,
                            display: "flex", alignItems: "center", justifyContent: "center",
                            fontSize: 9, color: "#fff", fontWeight: 700,
                        }}>
                            {c.completed ? "✓" : i + 1}
                        </span>
                        <span className="hidden sm:inline">{c.challengeTitle.split(" ").slice(0, 2).join(" ")}</span>
                    </div>
                ))}
            </div>

            {/* Reset button */}
            <button
                onClick={handleReset}
                disabled={resetting}
                style={{
                    display: "flex", alignItems: "center", gap: 4,
                    background: "transparent", border: "none",
                    color: confirmReset ? "#f87171" : "#4b5563",
                    fontSize: 11, cursor: "pointer", padding: "4px 6px",
                    borderRadius: 4, whiteSpace: "nowrap",
                    transition: "color 0.15s",
                }}
                onMouseOver={e => { e.currentTarget.style.color = confirmReset ? "#ef4444" : "#94a3b8"; }}
                onMouseOut={e => { e.currentTarget.style.color = confirmReset ? "#f87171" : "#4b5563"; }}
                onBlur={() => setTimeout(() => setConfirmReset(false), 2000)}
            >
                <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                    <polyline points="1 4 1 10 7 10"/>
                    <path d="M3.51 15a9 9 0 1 0 .49-4.02"/>
                </svg>
                {resetting ? "…" : confirmReset ? "Confirmer ?" : "Réinitialiser"}
            </button>
        </div>
    );
}
