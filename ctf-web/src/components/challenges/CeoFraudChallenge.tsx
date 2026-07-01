"use client";

import { useState } from "react";
import { apiFetch } from "@/lib/api";
import ChoiceOption from "./ChoiceOption";

// ── Types ─────────────────────────────────────────────────────────────────────

interface Choice {
    id: string;
    label: string;
    icon: string | null;
}

interface CeoFraudEmail {
    from_name: string;
    from_address: string;
    to: string;
    subject: string;
    sent_at: string;
    body: string;
}

interface CeoFraudContent {
    email: CeoFraudEmail;
    choices: Choice[];
}

interface ChoiceResult {
    id: string;
    label: string;
    icon: string | null;
    selected: boolean;
    isCorrect: boolean;
    explanation: string;
}

interface SubmitResult {
    results: ChoiceResult[];
    allCorrect: boolean;
    score: number;
    maxScore: number;
    redFlags: string[];
}

interface Props {
    challengeId: string;
    content: CeoFraudContent;
    variantIndex?: number | null;
    onComplete: (score?: number, maxScore?: number) => void;
}

// ── SVG Icons ─────────────────────────────────────────────────────────────────

function IconBank() {
    return (
        <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
            <rect x="2" y="7" width="20" height="14" rx="2"/>
            <path d="M16 7V5a2 2 0 0 0-2-2h-4a2 2 0 0 0-2 2v2"/>
            <line x1="12" y1="12" x2="12" y2="16"/>
            <line x1="8" y1="12" x2="16" y2="12"/>
        </svg>
    );
}

function IconFlag() {
    return (
        <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
            <path d="M4 15s1-1 4-1 5 2 8 2 4-1 4-1V3s-1 1-4 1-5-2-8-2-4 1-4 1z"/>
            <line x1="4" y1="22" x2="4" y2="15"/>
        </svg>
    );
}

function IconX() {
    return (
        <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
            <circle cx="12" cy="12" r="10"/>
            <line x1="15" y1="9" x2="9" y2="15"/>
            <line x1="9" y1="9" x2="15" y2="15"/>
        </svg>
    );
}

function IconPhone() {
    return (
        <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
            <path d="M22 16.92v3a2 2 0 0 1-2.18 2 19.79 19.79 0 0 1-8.63-3.07A19.5 19.5 0 0 1 4.69 14a19.79 19.79 0 0 1-3.07-8.67A2 2 0 0 1 3.6 3.18h3a2 2 0 0 1 2 1.72c.127.96.361 1.903.7 2.81a2 2 0 0 1-.45 2.11L7.91 10.91a16 16 0 0 0 6 6l.92-.92a2 2 0 0 1 2.11-.45c.907.339 1.85.573 2.81.7A2 2 0 0 1 22 18.92z"/>
        </svg>
    );
}

const ICONS: Record<string, React.ReactNode> = {
    bank:    <IconBank />,
    flag:    <IconFlag />,
    x:       <IconX />,
    nothing: <IconX />,
    phone:   <IconPhone />,
};

// ── Component ─────────────────────────────────────────────────────────────────

export default function CeoFraudChallenge({ challengeId, content, variantIndex = null, onComplete }: Props) {
    const [selected, setSelected] = useState<Set<string>>(new Set());
    const [result, setResult]     = useState<SubmitResult | null>(null);
    const [loading, setLoading]   = useState(false);
    const [error, setError]       = useState<string | null>(null);

    function toggle(id: string) {
        setSelected(prev => {
            const next = new Set(prev);
            next.has(id) ? next.delete(id) : next.add(id);
            return next;
        });
    }

    async function handleSubmit() {
        if (selected.size === 0) { setError("Sélectionnez au moins une réaction."); return; }
        setError(null);
        setLoading(true);
        try {
            const res = await apiFetch<SubmitResult>(
                `/api/challenges/interactive/${challengeId}/submit-ceo-fraud`,
                { method: "POST", body: JSON.stringify({ selectedChoices: [...selected], variantIndex }) }
            );
            setResult(res);
        } catch (e: unknown) {
            setError(e instanceof Error ? e.message : "Erreur lors de l'évaluation.");
        } finally {
            setLoading(false);
        }
    }

    // ── Corrected view ──────────────────────────────────────────────────────────
    if (result) {
        return (
            <div className="space-y-4">
                {/* Score banner */}
                <div style={{
                    background: result.allCorrect ? "rgba(34,197,94,0.12)" : "rgba(249,115,22,0.12)",
                    border: `1px solid ${result.allCorrect ? "#10B981" : "#f97316"}`,
                    borderRadius: 10, padding: "14px 18px",
                    display: "flex", alignItems: "center", gap: 12,
                }}>
                    <span style={{ fontSize: 24 }}>{result.allCorrect ? "✅" : "⚠️"}</span>
                    <div>
                        <p style={{ color: "#1E293B", fontWeight: 600, fontSize: 14 }}>
                            {result.allCorrect ? "Toutes les bonnes réactions !" : "Partiellement correct"}
                        </p>
                        <p style={{ color: "#64748B", fontSize: 12 }}>
                            Score : <span style={{ color: "var(--pr)", fontFamily: "monospace", fontWeight: 700 }}>{result.score}</span> / {result.maxScore} pts
                        </p>
                    </div>
                </div>

                {/* Choice results */}
                {result.results.map(r => {
                    const isGood    = r.isCorrect && r.selected;
                    const isMissed  = r.isCorrect && !r.selected;
                    const isBad     = !r.isCorrect && r.selected;
                    const isNeutral = !r.isCorrect && !r.selected;

                    const borderColor = isGood ? "#10B981" : isMissed ? "#f97316" : isBad ? "#ef4444" : "var(--border)";
                    const bgColor     = isGood ? "rgba(34,197,94,0.12)" : isMissed ? "rgba(249,115,22,0.12)" : isBad ? "rgba(239,68,68,0.12)" : "rgba(255,255,255,0.02)";
                    const statusColor = isGood ? "#10B981" : isMissed ? "#f97316" : isBad ? "#ef4444" : "#4b5563";
                    const statusIcon  = isGood ? "✓" : isMissed ? "⚠" : isBad ? "✗" : "—";
                    const statusText  = isGood ? "Bonne réaction !" : isMissed ? "Vous auriez dû choisir ceci" : isBad ? "Mauvaise réaction" : "";

                    return (
                        <div key={r.id} style={{ background: bgColor, border: `1px solid ${borderColor}`, borderRadius: 8, padding: "14px 16px", opacity: isNeutral ? 0.7 : 1 }}>
                            <div style={{ display: "flex", alignItems: "flex-start", gap: 12 }}>
                                <span style={{ color: statusColor, fontWeight: 700, fontSize: 16, lineHeight: 1, marginTop: 2 }}>{statusIcon}</span>
                                <div style={{ flex: 1 }}>
                                    <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
                                        <span style={{ color: statusColor }}>{r.icon && ICONS[r.icon] ? ICONS[r.icon] : null}</span>
                                        <p style={{ color: "#1E293B", fontSize: 14, fontWeight: 500 }}>{r.label}</p>
                                    </div>
                                    {statusText && <p style={{ color: statusColor, fontSize: 12, marginTop: 4, fontWeight: 600 }}>{statusText}</p>}
                                    {(isGood || isMissed || isBad) && (
                                        <p style={{ color: "#64748B", fontSize: 13, marginTop: 6, lineHeight: 1.6 }}>{r.explanation}</p>
                                    )}
                                </div>
                            </div>
                        </div>
                    );
                })}

                {/* Red flags */}
                {result.redFlags.length > 0 && (
                    <div style={{ background: "var(--bg-surface)", border: "1px solid rgba(59,130,246,0.3)", borderRadius: 8, padding: "14px 16px" }}>
                        <p style={{ color: "var(--pr)", fontSize: 13, fontWeight: 600, marginBottom: 10 }}>
                            🚩 Signaux d&apos;alarme dans cet email
                        </p>
                        <ul style={{ listStyle: "none", padding: 0, margin: 0, display: "flex", flexDirection: "column", gap: 6 }}>
                            {result.redFlags.map((f, i) => (
                                <li key={i} style={{ display: "flex", gap: 8, fontSize: 13, color: "#334155" }}>
                                    <span>⚠️</span><span>{f}</span>
                                </li>
                            ))}
                        </ul>
                    </div>
                )}

                <button
                    onClick={() => onComplete(result.score, result.maxScore)}
                    style={{ width: "100%", background: "var(--pr)", color: "#FFFFFF", border: "none", borderRadius: 8, padding: "12px 0", fontSize: 14, fontWeight: 600, cursor: "pointer" }}
                    onMouseOver={e => (e.currentTarget.style.background = "var(--pr-h)")}
                    onMouseOut={e => (e.currentTarget.style.background = "var(--pr)")}
                >
                    Continuer vers le prochain challenge →
                </button>
            </div>
        );
    }

    // ── Reading view ────────────────────────────────────────────────────────────
    return (
        <div className="space-y-5">
            {/* Email card */}
            <div style={{ background: "var(--bg-card)", border: "1px solid rgba(59,130,246,0.2)", borderRadius: 10, overflow: "hidden" }}>
                {/* Header */}
                <div style={{ background: "var(--bg-surface)", borderBottom: "1px solid rgba(59,130,246,0.15)", padding: "14px 18px" }}>
                    <div style={{ display: "flex", justifyContent: "space-between", alignItems: "flex-start", gap: 12 }}>
                        <div style={{ minWidth: 0 }}>
                            <p style={{ color: "#1E293B", fontWeight: 600, fontSize: 14 }}>{content.email.from_name}</p>
                            <p style={{ color: "#ef4444", fontSize: 12, fontFamily: "monospace", marginTop: 2, wordBreak: "break-all" }}>{content.email.from_address}</p>
                        </div>
                        <span style={{ color: "#64748B", fontSize: 11, whiteSpace: "nowrap", flexShrink: 0 }}>{content.email.sent_at}</span>
                    </div>
                    <p style={{ color: "#1E293B", fontWeight: 600, fontSize: 14, marginTop: 10 }}>{content.email.subject}</p>
                    <p style={{ color: "#64748B", fontSize: 12, marginTop: 2 }}>À : {content.email.to}</p>
                </div>
                {/* Body */}
                <div style={{ padding: "16px 18px" }}>
                    <pre style={{ whiteSpace: "pre-wrap", overflowWrap: "break-word", wordBreak: "break-word", fontFamily: "inherit", fontSize: 14, lineHeight: 1.7, color: "#334155", margin: 0 }}>
                        {content.email.body.replace(
                            /(IBAN\s*:\s*[A-Z]{2}\d{2}[\s\d]+)/g,
                            '$1'
                        )}
                    </pre>
                    {/* IBAN highlighted */}
                    {content.email.body.match(/IBAN\s*:\s*([A-Z]{2}\d{2}[\s\w]+)/)?.[1] && (
                        <div style={{ marginTop: 8, padding: "6px 10px", background: "rgba(59,130,246,0.08)", borderRadius: 6, fontFamily: "monospace", fontSize: 13, color: "var(--pr-l)", wordBreak: "break-all" }}>
                            IBAN : {content.email.body.match(/IBAN\s*:\s*([A-Z]{2}\d{2}[\s\w]+)/)?.[1]?.trim()}
                        </div>
                    )}
                </div>
            </div>

            {/* Choices */}
            <div>
                <div style={{ marginTop: 28, marginBottom: 16 }}>
                    <h3 style={{ fontSize: 16, fontWeight: 600, color: "var(--text-primary)", display: "inline" }}>
                        Que faites-vous ?
                    </h3>
                    <span style={{ fontSize: 13, color: "var(--text-muted)", marginLeft: 10, fontStyle: "italic" }}>
                        (plusieurs réponses possibles)
                    </span>
                </div>
                <div style={{ display: "flex", flexDirection: "column", gap: 10 }}>
                    {content.choices.map(c => (
                        <ChoiceOption
                            key={c.id}
                            id={c.id}
                            label={c.label}
                            active={selected.has(c.id)}
                            onToggle={toggle}
                            icon={c.icon && ICONS[c.icon] ? ICONS[c.icon] : <IconX />}
                        />
                    ))}
                </div>
            </div>

            {error && <p style={{ color: "#f87171", fontSize: 13, textAlign: "center" }}>{error}</p>}

            <button
                onClick={handleSubmit}
                disabled={loading || selected.size === 0}
                style={{
                    width: "100%",
                    marginTop: 20,
                    padding: 15,
                    background: selected.size === 0
                        ? "rgba(59,130,246,0.08)"
                        : "linear-gradient(135deg, #3B82F6, #2563EB)",
                    border: selected.size === 0 ? "1px solid rgba(59,130,246,0.2)" : "none",
                    borderRadius: 10,
                    color: selected.size === 0 ? "#6b7280" : "var(--bg-base)",
                    fontSize: 14,
                    fontWeight: 700,
                    fontFamily: "'JetBrains Mono', monospace",
                    letterSpacing: "0.08em",
                    textTransform: "uppercase",
                    cursor: selected.size === 0 || loading ? "not-allowed" : "pointer",
                    transition: "all 0.2s",
                    boxShadow: selected.size > 0 ? "0 0 20px rgba(59,130,246,0.25)" : "none",
                }}
            >
                {loading
                    ? "Évaluation en cours…"
                    : selected.size === 0
                        ? "Sélectionnez au moins une réponse"
                        : `Valider ma réponse (${selected.size} sélection${selected.size > 1 ? "s" : ""})`}
            </button>
        </div>
    );
}
