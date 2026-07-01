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
                    border: `1px solid ${pct >= 70 ? "#10B981" : pct >= 40 ? "#f97316" : "#ef4444"}`,
                    borderRadius: 10, padding: "14px 18px", display: "flex", alignItems: "center", gap: 12,
                }}>
                    <span style={{ fontSize: 24 }}>{pct === 100 ? "🔐" : pct >= 70 ? "✅" : pct >= 40 ? "⚠️" : "❌"}</span>
                    <div>
                        <p style={{ color: "#1E293B", fontWeight: 600, fontSize: 14, margin: 0 }}>
                            {pct === 100 ? "Parfait !" : pct >= 70 ? "Très bien !" : pct >= 40 ? "Partiellement correct" : "À revoir"}
                        </p>
                        <p style={{ color: "#64748B", fontSize: 12, margin: "3px 0 0" }}>
                            {result.correctCount}/{result.totalRounds} bonnes réponses —{" "}
                            <span style={{ color: "var(--pr)", fontWeight: 700, fontFamily: "monospace" }}>{result.pointsEarned}</span> / {result.maxPoints} pts ({pct}%)
                        </p>
                    </div>
                </div>

                {/* Per-round results */}
                {result.roundResults.map((r, i) => (
                    <div key={r.roundId} style={{
                        background: r.isCorrect ? "rgba(34,197,94,0.08)" : "rgba(239,68,68,0.08)",
                        border: `1px solid ${r.isCorrect ? "#10B981" : "#ef4444"}`,
                        borderRadius: 8, padding: "12px 14px",
                    }}>
                        <div style={{ display: "flex", alignItems: "flex-start", gap: 10 }}>
                            <span style={{ fontSize: 16, flexShrink: 0, marginTop: 1 }}>{r.isCorrect ? "✅" : "❌"}</span>
                            <div style={{ flex: 1, minWidth: 0 }}>
                                <p style={{ color: "#64748B", fontSize: 11, textTransform: "uppercase", letterSpacing: "0.06em", margin: "0 0 3px" }}>
                                    Question {i + 1}
                                </p>
                                <p style={{ color: "#1E293B", fontSize: 13, fontWeight: 500, margin: "0 0 6px", lineHeight: 1.4 }}>{r.question}</p>
                                {!r.isCorrect && (
                                    <p style={{ color: "#64748B", fontSize: 12, margin: "0 0 4px" }}>
                                        Votre réponse :{" "}
                                        <span style={{ color: "#ef4444", fontWeight: 600 }}>{r.selectedAnswer}</span>
                                        {" "}— Bonne réponse :{" "}
                                        <span style={{ color: "#10B981", fontWeight: 600 }}>{r.correctAnswer}</span>
                                    </p>
                                )}
                                <p style={{ color: "#64748B", fontSize: 13, margin: 0, lineHeight: 1.5 }}>{r.explanation}</p>
                            </div>
                        </div>
                    </div>
                ))}

                <button
                    onClick={() => onComplete(result.pointsEarned, result.maxPoints)}
                    style={{ width: "100%", background: "var(--pr)", color: "#FFFFFF", border: "none", borderRadius: 8, padding: "13px 0", fontSize: 14, fontWeight: 600, cursor: "pointer" }}
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
                <div style={{ background: "var(--bg-surface)", borderLeft: "4px solid #3B82F6", borderRadius: "0 8px 8px 0", padding: "14px 16px" }}>
                    <p style={{ color: "#64748B", fontSize: 13, lineHeight: 1.6, margin: 0 }}>{content.intro}</p>
                </div>
            )}

            {/* Round stepper */}
            <div style={{ display: "flex", alignItems: "center", justifyContent: "center", gap: 6 }}>
                {rounds.map((r, i) => (
                    <div key={r.id} style={{ display: "flex", alignItems: "center", gap: 6 }}>
                        <div style={{
                            width: 28, height: 28, borderRadius: "50%", display: "flex", alignItems: "center", justifyContent: "center",
                            fontSize: 12, fontWeight: 700,
                            border: `2px solid ${i < currentRound ? "#10B981" : i === currentRound ? "var(--pr)" : "#CBD5E1"}`,
                            background: i < currentRound ? "#10B981" : i === currentRound ? "rgba(59,130,246,0.15)" : "transparent",
                            color: i < currentRound ? "#fff" : i === currentRound ? "var(--pr)" : "#4b5563",
                            transition: "all 0.2s",
                        }}>
                            {i < currentRound ? "✓" : i + 1}
                        </div>
                        {i < rounds.length - 1 && (
                            <div style={{ width: 20, height: 2, borderRadius: 99, background: i < currentRound ? "#10B981" : "#CBD5E1", transition: "background 0.3s" }} />
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
                                        background: active ? "rgba(59,130,246,0.08)" : "#F1F5F9",
                                        border: active ? "1px solid rgba(59,130,246,0.55)" : "1px solid rgba(59,130,246,0.12)",
                                        borderRadius: 10,
                                        cursor: "pointer",
                                        transition: "all 0.18s ease",
                                        textAlign: "left",
                                    }}
                                    onMouseOver={e => {
                                        if (!active) {
                                            e.currentTarget.style.background = "rgba(59,130,246,0.05)";
                                            e.currentTarget.style.borderColor = "rgba(59,130,246,0.25)";
                                        }
                                    }}
                                    onMouseOut={e => {
                                        if (!active) {
                                            e.currentTarget.style.background = "#F1F5F9";
                                            e.currentTarget.style.borderColor = "rgba(59,130,246,0.12)";
                                        }
                                    }}
                                >
                                    {/* Radio visuel (cercle) */}
                                    <div style={{
                                        width: 20,
                                        height: 20,
                                        borderRadius: "50%",
                                        border: active ? "2px solid #3B82F6" : "2px solid rgba(59,130,246,0.3)",
                                        background: active ? "rgba(59,130,246,0.2)" : "transparent",
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
                                        background: "rgba(59,130,246,0.08)",
                                        border: "1px solid rgba(59,130,246,0.15)",
                                        display: "flex",
                                        alignItems: "center",
                                        justifyContent: "center",
                                        flexShrink: 0,
                                        color: active ? "var(--pr)" : "#64748B",
                                        fontFamily: "'JetBrains Mono', monospace",
                                        fontWeight: 700,
                                        fontSize: 14,
                                    }}>
                                        {c.id}
                                    </div>

                                    {/* Texte */}
                                    <span style={{
                                        fontSize: 14,
                                        color: active ? "#1E293B" : "#64748B",
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

            {error && <p style={{ color: "#f87171", fontSize: 13, textAlign: "center" }}>{error}</p>}

            <button
                onClick={handleNext}
                disabled={loading || !hasAnswer}
                style={{
                    width: "100%",
                    marginTop: 20,
                    padding: 15,
                    background: !hasAnswer
                        ? "rgba(59,130,246,0.08)"
                        : "linear-gradient(135deg, #3B82F6, #2563EB)",
                    border: !hasAnswer ? "1px solid rgba(59,130,246,0.2)" : "none",
                    borderRadius: 10,
                    color: !hasAnswer ? "#6b7280" : "var(--bg-base)",
                    fontSize: 14,
                    fontWeight: 700,
                    fontFamily: "'JetBrains Mono', monospace",
                    letterSpacing: "0.08em",
                    textTransform: "uppercase",
                    cursor: !hasAnswer || loading ? "not-allowed" : "pointer",
                    transition: "all 0.2s",
                    boxShadow: hasAnswer ? "0 0 20px rgba(59,130,246,0.25)" : "none",
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
