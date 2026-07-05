"use client";

import { useState } from "react";
import { apiFetch } from "@/lib/api";

// ── Types ─────────────────────────────────────────────────────────────────────

interface QuizChoice { id: string; label: string; }

interface QuizRound {
    id: string;         // "round1" | "round2" | "round3"
    question: string;
    choices: QuizChoice[];
    correct_answer: string;
    explanation: string;
}

interface PasswordQuizContent {
    intro?: string;
    rounds: QuizRound[];
}

interface RoundResult {
    roundId: string;
    question: string;
    selectedAnswer: string;
    correctAnswer: string;
    isCorrect: boolean;
    explanation: string;
}

interface SubmitResult {
    roundResults: RoundResult[];
    correctCount: number;
    totalRounds: number;
    scorePercent: number;
    pointsEarned: number;
    maxPoints: number;
}

interface Props {
    challengeId: string;
    content: PasswordQuizContent;
    variantIndex?: number | null;
    onComplete: (score?: number, maxScore?: number) => void;
}

// ── Component ─────────────────────────────────────────────────────────────────

export default function PasswordQuizChallenge({ challengeId, content, variantIndex = null, onComplete }: Props) {
    const rounds = content.rounds ?? [];
    const [currentRound, setCurrentRound] = useState(0);
    const [answers, setAnswers]           = useState<Record<string, string>>({});
    const [result, setResult]             = useState<SubmitResult | null>(null);
    const [loading, setLoading]           = useState(false);
    const [error, setError]               = useState<string | null>(null);

    const round       = rounds[currentRound];
    const isLast      = currentRound === rounds.length - 1;
    const hasAnswer   = round && answers[round.id] !== undefined;

    function selectAnswer(roundId: string, choiceId: string) {
        setAnswers(prev => ({ ...prev, [roundId]: choiceId }));
    }

    async function handleNext() {
        if (!hasAnswer) { setError("Sélectionnez une réponse."); return; }
        setError(null);
        if (!isLast) {
            setCurrentRound(r => r + 1);
            return;
        }
        // Final round: submit
        setLoading(true);
        try {
            const res = await apiFetch<SubmitResult>(
                `/api/challenges/interactive/${challengeId}/submit-password-quiz`,
                { method: "POST", body: JSON.stringify({ answers, variantIndex }) }
            );
            setResult(res);
        } catch (e: unknown) {
            setError(e instanceof Error ? e.message : "Erreur lors de la soumission.");
        } finally {
            setLoading(false);
        }
    }

    // ── Results view ────────────────────────────────────────────────────────────
    if (result) {
        const pct = result.scorePercent;
        return (
            <div className="space-y-4">
                {/* Global score */}
                <div style={{
                    background: pct >= 70 ? "rgba(34,197,94,0.1)" : pct >= 40 ? "rgba(249,115,22,0.1)" : "rgba(239,68,68,0.1)",
                    border: `1px solid ${pct >= 70 ? "var(--success)" : pct >= 40 ? "var(--warning)" : "var(--danger)"}`,
                    borderRadius: 10, padding: "14px 18px", display: "flex", alignItems: "center", gap: 12,
                }}>
                    <span style={{ fontSize: 24 }}>{pct === 100 ? "🔐" : pct >= 70 ? "✅" : pct >= 40 ? "⚠️" : "❌"}</span>
                    <div>
                        <p style={{ color: "var(--text)", fontWeight: 600, fontSize: 14, margin: 0 }}>
                            {pct === 100 ? "Parfait !" : pct >= 70 ? "Très bien !" : pct >= 40 ? "Partiellement correct" : "À revoir"}
                        </p>
                        <p style={{ color: "var(--text-2)", fontSize: 12, margin: "3px 0 0" }}>
                            {result.correctCount}/{result.totalRounds} bonnes réponses —{" "}
                            <span style={{ color: "var(--pr)", fontWeight: 700, fontFamily: "monospace" }}>{result.pointsEarned}</span> / {result.maxPoints} pts ({pct}%)
                        </p>
                    </div>
                </div>

                {/* Per-round results */}
                {result.roundResults.map((r, i) => (
                    <div key={r.roundId} style={{
                        background: r.isCorrect ? "rgba(34,197,94,0.08)" : "rgba(239,68,68,0.08)",
                        border: `1px solid ${r.isCorrect ? "var(--success)" : "var(--danger)"}`,
                        borderRadius: 8, padding: "12px 14px",
                    }}>
                        <div style={{ display: "flex", alignItems: "flex-start", gap: 10 }}>
                            <span style={{ fontSize: 16, flexShrink: 0, marginTop: 1 }}>{r.isCorrect ? "✅" : "❌"}</span>
                            <div style={{ flex: 1, minWidth: 0 }}>
                                <p style={{ color: "var(--text-2)", fontSize: 11, textTransform: "uppercase", letterSpacing: "0.06em", margin: "0 0 3px" }}>
                                    Question {i + 1}
                                </p>
                                <p style={{ color: "var(--text)", fontSize: 13, fontWeight: 500, margin: "0 0 6px", lineHeight: 1.4 }}>{r.question}</p>
                                {!r.isCorrect && (
                                    <p style={{ color: "var(--text-2)", fontSize: 12, margin: "0 0 4px" }}>
                                        Votre réponse :{" "}
                                        <span style={{ color: "var(--danger)", fontWeight: 600 }}>{r.selectedAnswer}</span>
                                        {" "}— Bonne réponse :{" "}
                                        <span style={{ color: "var(--success)", fontWeight: 600 }}>{r.correctAnswer}</span>
                                    </p>
                                )}
                                <p style={{ color: "var(--text-2)", fontSize: 13, margin: 0, lineHeight: 1.5 }}>{r.explanation}</p>
                            </div>
                        </div>
                    </div>
                ))}

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

    // ── Quiz view ───────────────────────────────────────────────────────────────
    return (
        <div className="space-y-5">
            {/* Intro */}
            {content.intro && currentRound === 0 && (
                <div style={{ background: "var(--bg-surface)", borderLeft: "4px solid var(--accent)", borderRadius: "0 8px 8px 0", padding: "14px 16px" }}>
                    <p style={{ color: "var(--text-2)", fontSize: 13, lineHeight: 1.6, margin: 0 }}>{content.intro}</p>
                </div>
            )}

            {/* Round stepper */}
            <div style={{ display: "flex", alignItems: "center", justifyContent: "center", gap: 6 }}>
                {rounds.map((r, i) => (
                    <div key={r.id} style={{ display: "flex", alignItems: "center", gap: 6 }}>
                        <div style={{
                            width: 28, height: 28, borderRadius: "50%", display: "flex", alignItems: "center", justifyContent: "center",
                            fontSize: 12, fontWeight: 700,
                            border: `2px solid ${i < currentRound ? "var(--success)" : i === currentRound ? "var(--pr)" : "var(--border)"}`,
                            background: i < currentRound ? "var(--success)" : i === currentRound ? "var(--accent-subtle)" : "transparent",
                            color: i < currentRound ? "var(--on-accent)" : i === currentRound ? "var(--pr)" : "var(--text-3)",
                            transition: "all 0.2s",
                        }}>
                            {i < currentRound ? "✓" : i + 1}
                        </div>
                        {i < rounds.length - 1 && (
                            <div style={{ width: 20, height: 2, borderRadius: 99, background: i < currentRound ? "var(--success)" : "var(--border)", transition: "background 0.3s" }} />
                        )}
                    </div>
                ))}
            </div>

            {/* Question */}
            {round && (
                <>
                    <p style={{ color: "var(--pr)", fontSize: 15, fontWeight: 700, margin: 0 }}>
                        {round.question}
                    </p>

                    {/* Radio choices */}
                    <div style={{ display: "flex", flexDirection: "column", gap: 10 }}>
                        {round.choices.map(c => {
                            const active = answers[round.id] === c.id;
                            return (
                                <button
                                    key={c.id}
                                    onClick={() => selectAnswer(round.id, c.id)}
                                    style={{
                                        width: "100%",
                                        display: "flex",
                                        alignItems: "center",
                                        gap: 16,
                                        padding: "16px 20px",
                                        background: active ? "var(--accent-subtle)" : "var(--surface)",
                                        border: active ? "1px solid var(--accent)" : "1px solid var(--border)",
                                        borderRadius: 10,
                                        cursor: "pointer",
                                        transition: "all 0.2s ease",
                                        textAlign: "left",
                                    }}
                                    onMouseOver={e => {
                                        if (!active) {
                                            e.currentTarget.style.background = "var(--accent-subtle)";
                                            e.currentTarget.style.borderColor = "var(--accent-border)";
                                        }
                                    }}
                                    onMouseOut={e => {
                                        if (!active) {
                                            e.currentTarget.style.background = "var(--surface)";
                                            e.currentTarget.style.borderColor = "var(--border)";
                                        }
                                    }}
                                >
                                    {/* Radio visuel (cercle) */}
                                    <div style={{
                                        width: 20,
                                        height: 20,
                                        borderRadius: "50%",
                                        border: active ? "2px solid var(--accent)" : "2px solid var(--border)",
                                        background: active ? "var(--accent-subtle)" : "transparent",
                                        flexShrink: 0,
                                        display: "flex",
                                        alignItems: "center",
                                        justifyContent: "center",
                                        transition: "all 0.15s",
                                    }}>
                                        {active && (
                                            <span style={{ width: 8, height: 8, borderRadius: "50%", background: "var(--pr)", display: "block" }} />
                                        )}
                                    </div>

                                    {/* Lettre */}
                                    <div style={{
                                        width: 36,
                                        height: 36,
                                        borderRadius: 8,
                                        background: "var(--accent-subtle)",
                                        border: "1px solid var(--accent-border)",
                                        display: "flex",
                                        alignItems: "center",
                                        justifyContent: "center",
                                        flexShrink: 0,
                                        color: active ? "var(--pr)" : "var(--text-2)",
                                        fontFamily: "'JetBrains Mono', monospace",
                                        fontWeight: 700,
                                        fontSize: 14,
                                    }}>
                                        {c.id}
                                    </div>

                                    {/* Texte */}
                                    <span style={{
                                        fontSize: 14,
                                        color: active ? "var(--text)" : "var(--text-2)",
                                        lineHeight: 1.5,
                                        fontWeight: active ? 500 : 400,
                                        transition: "color 0.15s",
                                    }}>
                                        {c.label}
                                    </span>
                                </button>
                            );
                        })}
                    </div>
                </>
            )}

            {error && <p style={{ color: "var(--danger-t)", fontSize: 13, textAlign: "center" }}>{error}</p>}

            <button
                onClick={handleNext}
                disabled={loading || !hasAnswer}
                style={{
                    width: "100%",
                    marginTop: 20,
                    padding: 15,
                    background: !hasAnswer
                        ? "var(--accent-subtle)"
                        : "linear-gradient(135deg, var(--accent), var(--accent-hover))",
                    border: !hasAnswer ? "1px solid var(--accent-border)" : "none",
                    borderRadius: 10,
                    color: !hasAnswer ? "var(--text-3)" : "var(--on-accent)",
                    fontSize: 14,
                    fontWeight: 700,
                    fontFamily: "'JetBrains Mono', monospace",
                    letterSpacing: "0.08em",
                    textTransform: "uppercase",
                    cursor: !hasAnswer || loading ? "not-allowed" : "pointer",
                    transition: "all 0.2s",
                    boxShadow: hasAnswer ? "0 0 20px var(--accent-border)" : "none",
                }}
            >
                {loading
                    ? "Évaluation en cours…"
                    : !hasAnswer
                        ? "Sélectionnez une réponse"
                        : isLast
                            ? "Terminer le quiz"
                            : "Question suivante →"}
            </button>
        </div>
    );
}
