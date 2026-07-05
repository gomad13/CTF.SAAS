"use client";

import { useState } from "react";
import { apiFetch } from "@/lib/api";
import ChoiceOption from "./ChoiceOption";

// ── Types ─────────────────────────────────────────────────────────────────────

interface Choice { id: string; label: string; }

interface Email {
    from_name: string;
    from_address: string;
    to?: string;
    subject: string;
    sent_at: string;
    body: string;
}

interface Scenario {
    title: string;
    context: string;
    icon?: string;
}

interface MultichoiceContent {
    email?: Email;
    scenario?: Scenario;
    question: string;
    choices: Choice[];
}

interface ChoiceResult {
    choiceId: string;
    label: string;
    isCorrect: boolean;
    wasSelected: boolean;
    explanation: string;
}

interface SubmitResult {
    results: ChoiceResult[];
    scorePercent: number;
    pointsEarned: number;
    maxPoints: number;
    redFlags: string[];
    savoirPlus?: string;
}

interface Props {
    challengeId: string;
    content: MultichoiceContent;
    variantIndex?: number | null;
    onComplete: (score?: number, maxScore?: number) => void;
}

// ── Component ─────────────────────────────────────────────────────────────────

export default function MultichoiceChallenge({ challengeId, content, variantIndex = null, onComplete }: Props) {
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
        if (selected.size === 0) { setError("Sélectionnez au moins une réponse."); return; }
        setError(null);
        setLoading(true);
        try {
            const res = await apiFetch<SubmitResult>(
                `/api/challenges/interactive/${challengeId}/submit-multichoice`,
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
                {/* Score */}
                <div style={{
                    background: result.scorePercent >= 70 ? "rgba(34,197,94,0.1)" : result.scorePercent >= 40 ? "rgba(249,115,22,0.1)" : "rgba(239,68,68,0.1)",
                    border: `1px solid ${result.scorePercent >= 70 ? "var(--success)" : result.scorePercent >= 40 ? "var(--warning)" : "var(--danger)"}`,
                    borderRadius: 10, padding: "14px 18px", display: "flex", alignItems: "center", gap: 12
                }}>
                    <span style={{ fontSize: 24 }}>{result.scorePercent >= 70 ? "✅" : result.scorePercent >= 40 ? "⚠️" : "❌"}</span>
                    <div>
                        <p style={{ color: "var(--text)", fontWeight: 600, fontSize: 14, margin: 0 }}>
                            {result.scorePercent === 100 ? "Parfait !" : result.scorePercent >= 70 ? "Très bien !" : result.scorePercent >= 40 ? "Partiellement correct" : "À revoir"}
                        </p>
                        <p style={{ color: "var(--text-2)", fontSize: 12, margin: "3px 0 0" }}>
                            <span style={{ color: "var(--pr)", fontWeight: 700, fontFamily: "monospace" }}>{result.pointsEarned}</span> / {result.maxPoints} pts ({result.scorePercent}%)
                        </p>
                    </div>
                </div>

                {/* Choice results */}
                {result.results.map(r => {
                    const isGood    = r.isCorrect && r.wasSelected;
                    const isMissed  = r.isCorrect && !r.wasSelected;
                    const isBad     = !r.isCorrect && r.wasSelected;
                    const isNeutral = !r.isCorrect && !r.wasSelected;

                    if (isNeutral) return null;

                    const borderColor = isGood ? "var(--success)" : isMissed ? "var(--warning)" : "var(--danger)";
                    const bgColor     = isGood ? "rgba(34,197,94,0.1)" : isMissed ? "rgba(249,115,22,0.1)" : "rgba(239,68,68,0.1)";
                    const icon        = isGood ? "✅" : isMissed ? "⚠️" : "❌";
                    const tag         = isGood ? "Bonne réponse" : isMissed ? "Vous auriez dû cocher ceci" : "Mauvaise réponse";
                    const tagColor    = isGood ? "var(--success)" : isMissed ? "var(--warning)" : "var(--danger)";

                    return (
                        <div key={r.choiceId} style={{ background: bgColor, border: `1px solid ${borderColor}`, borderRadius: 8, padding: "12px 14px" }}>
                            <div style={{ display: "flex", alignItems: "flex-start", gap: 10 }}>
                                <span style={{ fontSize: 16, flexShrink: 0, marginTop: 1 }}>{icon}</span>
                                <div>
                                    <p style={{ color: "var(--text)", fontSize: 13, fontWeight: 500, margin: 0 }}>{r.label}</p>
                                    <p style={{ color: tagColor, fontSize: 12, fontWeight: 600, margin: "3px 0 4px" }}>{tag}</p>
                                    <p style={{ color: "var(--text-2)", fontSize: 13, margin: 0, lineHeight: 1.5 }}>{r.explanation}</p>
                                </div>
                            </div>
                        </div>
                    );
                })}

                {/* Red flags */}
                {result.redFlags?.length > 0 && (
                    <div style={{ background: "var(--bg-surface)", border: "1px solid var(--accent-border)", borderRadius: 8, padding: "14px 16px" }}>
                        <p style={{ color: "var(--pr)", fontSize: 13, fontWeight: 600, marginBottom: 10 }}>⚠️ Signaux à retenir</p>
                        <ul style={{ padding: 0, margin: 0, listStyle: "none", display: "flex", flexDirection: "column", gap: 5 }}>
                            {result.redFlags.map((f, i) => (
                                <li key={i} style={{ fontSize: 13, color: "var(--text-2)", display: "flex", gap: 8 }}>
                                    <span>•</span><span>{f}</span>
                                </li>
                            ))}
                        </ul>
                    </div>
                )}

                {/* Savoir plus */}
                {result.savoirPlus && (
                    <div style={{ background: "var(--accent-subtle)", border: "1px solid var(--accent-border)", borderRadius: 8, padding: "12px 14px" }}>
                        <p style={{ color: "var(--pr)", fontSize: 12, fontWeight: 600, marginBottom: 5 }}>📖 Pour aller plus loin</p>
                        <p style={{ color: "var(--text-2)", fontSize: 13, margin: 0, lineHeight: 1.6 }}>{result.savoirPlus}</p>
                    </div>
                )}

                <button
                    onClick={() => onComplete(result.pointsEarned, result.maxPoints)}
                    style={{ width: "100%", background: "var(--pr)", color: "var(--on-accent)", border: "none", borderRadius: 8, padding: "13px 0", fontSize: 14, fontWeight: 600, cursor: "pointer", transition: "background-color 0.2s" }}
                    onMouseOver={e => (e.currentTarget.style.background = "var(--pr-h)")}
                    onMouseOut={e => (e.currentTarget.style.background = "var(--pr)")}
                >
                    Continuer →
                </button>
            </div>
        );
    }

    // ── Reading view ────────────────────────────────────────────────────────────
    return (
        <div className="space-y-5">
            {/* Email or Scenario display */}
            {content.email && (
                <div style={{ background: "var(--bg-card)", border: "1px solid var(--accent-border)", borderRadius: 10, overflow: "hidden" }}>
                    <div style={{ background: "var(--bg-surface)", borderBottom: "1px solid var(--accent-border)", padding: "14px 18px" }}>
                        <div style={{ display: "flex", justifyContent: "space-between", gap: 12 }}>
                            <div>
                                <p style={{ color: "var(--text)", fontWeight: 600, fontSize: 14, margin: 0 }}>{content.email.from_name}</p>
                                <p style={{ color: "var(--danger)", fontSize: 12, fontFamily: "monospace", margin: "3px 0 0" }}>{content.email.from_address}</p>
                            </div>
                            <span style={{ color: "var(--text-2)", fontSize: 11, whiteSpace: "nowrap" }}>{content.email.sent_at}</span>
                        </div>
                        <p style={{ color: "var(--text)", fontWeight: 600, fontSize: 14, margin: "10px 0 2px" }}>{content.email.subject}</p>
                        {content.email.to && <p style={{ color: "var(--text-2)", fontSize: 12, margin: 0 }}>À : {content.email.to}</p>}
                    </div>
                    <div style={{ padding: "16px 18px" }}>
                        <div style={{ fontSize: 14, lineHeight: 1.7, color: "var(--text-2)" }}>
                            {content.email.body.split("\n").map((line, i) => {
                                const urlMatch = line.match(/(https?:\/\/[^\s]+)/);
                                if (urlMatch) return (
                                    <p key={i} style={{ margin: "4px 0" }}>
                                        {line.replace(urlMatch[1], "")}
                                        <span style={{ color: "var(--pr)", fontFamily: "monospace", fontSize: 13 }}>{urlMatch[1]}</span>
                                    </p>
                                );
                                return <p key={i} style={{ margin: line.trim() === "" ? "8px 0" : "3px 0" }}>{line}</p>;
                            })}
                        </div>
                    </div>
                </div>
            )}

            {content.scenario && !content.email && (
                <div style={{ background: "var(--bg-surface)", borderLeft: "4px solid var(--warning)", borderRadius: "0 8px 8px 0", padding: "16px 18px", position: "relative" }}>
                    <span style={{ position: "absolute", top: 12, right: 14, fontSize: 20 }}>⚠️</span>
                    <p style={{ color: "var(--warning)", fontSize: 12, fontWeight: 700, textTransform: "uppercase", letterSpacing: "0.06em", margin: "0 0 6px" }}>
                        Scénario
                    </p>
                    <p style={{ color: "var(--text)", fontSize: 15, fontWeight: 700, margin: "0 0 10px" }}>{content.scenario.title}</p>
                    <div style={{ fontSize: 14, lineHeight: 1.7, color: "var(--text-2)" }}>
                        {content.scenario.context.split("\n\n").map((para, i) => (
                            <p key={i} style={{ margin: i > 0 ? "10px 0 0" : "0" }}>{para}</p>
                        ))}
                    </div>
                </div>
            )}

            {/* Question */}
            <p style={{ color: "var(--pr)", fontSize: 15, fontWeight: 700, margin: 0 }}>{content.question}</p>

            {/* Choices */}
            <div style={{ display: "flex", flexDirection: "column", gap: 10 }}>
                {content.choices.map(c => (
                    <ChoiceOption
                        key={c.id}
                        id={c.id}
                        label={c.label}
                        active={selected.has(c.id)}
                        onToggle={toggle}
                        letter={c.id}
                    />
                ))}
            </div>

            {error && <p style={{ color: "var(--danger-t)", fontSize: 13, textAlign: "center" }}>{error}</p>}

            <button
                onClick={handleSubmit}
                disabled={loading || selected.size === 0}
                style={{
                    width: "100%",
                    marginTop: 20,
                    padding: 15,
                    background: selected.size === 0
                        ? "var(--accent-subtle)"
                        : "linear-gradient(135deg, var(--accent), var(--accent-hover))",
                    border: selected.size === 0 ? "1px solid var(--accent-border)" : "none",
                    borderRadius: 10,
                    color: selected.size === 0 ? "var(--text-3)" : "var(--on-accent)",
                    fontSize: 14,
                    fontWeight: 700,
                    fontFamily: "'JetBrains Mono', monospace",
                    letterSpacing: "0.08em",
                    textTransform: "uppercase",
                    cursor: selected.size === 0 || loading ? "not-allowed" : "pointer",
                    transition: "all 0.2s",
                    boxShadow: selected.size > 0 ? "0 0 20px var(--accent-border)" : "none",
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
