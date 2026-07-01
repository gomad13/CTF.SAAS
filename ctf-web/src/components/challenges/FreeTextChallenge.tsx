"use client";

import { useState } from "react";
import { apiFetch } from "@/lib/api";

interface Question {
    id: string;
    question: string;
    hint?: string;
    min_chars: number;
}

interface QuestionResult {
    questionId: string;
    score: number;
    appreciation: string;
    resume: string;
    pointsForts: string[];
    pointsManques: string[];
    conseilExpert: string;
    pointsEarned: number;
    aiAvailable: boolean;
}

type Phase = "writing" | "evaluating" | "result" | "summary";

export default function FreeTextChallenge({
    challengeId,
    content,
    onComplete,
}: {
    challengeId: string;
    content: { questions: Question[] };
    onComplete: (score?: number, maxScore?: number) => void;
}) {
    const questions = content.questions ?? [];
    const [currentQ, setCurrentQ] = useState(0);
    const [answers, setAnswers] = useState<Record<string, string>>({});
    const [results, setResults] = useState<QuestionResult[]>([]);
    const [loading, setLoading] = useState(false);
    const [phase, setPhase] = useState<Phase>("writing");
    const [allDone, setAllDone] = useState(false);

    const question = questions[currentQ];
    const minChars = question?.min_chars ?? 80;
    const charCount = (answers[question?.id] ?? "").length;
    const canSubmit = charCount >= minChars && !loading;

    async function handleSubmit() {
        if (!canSubmit || !question) return;
        setPhase("evaluating");
        setLoading(true);
        try {
            const data = await apiFetch<QuestionResult>(`/api/challenges/interactive/${challengeId}/submit-free-text`, {
                method: "POST",
                body: JSON.stringify({
                    questionId: question.id,
                    answer: answers[question.id] ?? "",
                }),
            });
            const newResults = [...results, data];
            setResults(newResults);
            setPhase("result");
            if (currentQ >= questions.length - 1) setAllDone(true);
        } catch {
            setPhase("writing");
        } finally {
            setLoading(false);
        }
    }

    async function handleNext() {
        if (allDone) {
            const scores = results.map(r => r.score);
            try {
                await apiFetch(`/api/challenges/interactive/${challengeId}/complete-free-text`, {
                    method: "POST",
                    body: JSON.stringify({ questionScores: scores }),
                });
            } catch {}
            setPhase("summary");
        } else {
            setCurrentQ(q => q + 1);
            setPhase("writing");
        }
    }

    const globalScore = results.length
        ? Math.round(results.reduce((s, r) => s + r.score, 0) / results.length)
        : 0;

    function scoreColor(score: number) {
        if (score >= 90) return "#4ade80";
        if (score >= 70) return "#a3e635";
        if (score >= 50) return "#fbbf24";
        return "#f87171";
    }
    function appreciationLabel(score: number) {
        if (score >= 90) return "Excellent";
        if (score >= 70) return "Bien";
        if (score >= 50) return "Moyen";
        return "Insuffisant";
    }

    // ── ÉCRAN BILAN FINAL ──
    if (phase === "summary") {
        return (
            <div style={{ padding: 24 }}>
                <div style={{ textAlign: "center", marginBottom: 32 }}>
                    <div style={{ position: "relative", display: "inline-block", width: 140, height: 140, marginBottom: 16 }}>
                        <svg width="140" height="140" viewBox="0 0 140 140">
                            <circle cx="70" cy="70" r="60" fill="none" stroke="var(--bg-elevated)" strokeWidth="10" />
                            <circle cx="70" cy="70" r="60" fill="none"
                                stroke={scoreColor(globalScore)}
                                strokeWidth="10"
                                strokeDasharray={`${2 * Math.PI * 60 * globalScore / 100} ${2 * Math.PI * 60}`}
                                strokeLinecap="round"
                                transform="rotate(-90 70 70)"
                                style={{ transition: "stroke-dasharray 1s" }}
                            />
                        </svg>
                        <div style={{
                            position: "absolute", inset: 0, display: "flex",
                            flexDirection: "column", alignItems: "center", justifyContent: "center",
                        }}>
                            <span style={{ fontSize: 32, fontWeight: 700, color: scoreColor(globalScore), fontFamily: "'JetBrains Mono', monospace" }}>{globalScore}</span>
                            <span style={{ fontSize: 12, color: "var(--text-muted)" }}>/100</span>
                        </div>
                    </div>
                    <div style={{ fontSize: 20, fontWeight: 700, color: "var(--text-primary)" }}>
                        {appreciationLabel(globalScore)}
                    </div>
                    <div style={{ color: "var(--text-muted)", fontSize: 14, marginTop: 4 }}>
                        Analyse IA complète
                    </div>
                </div>

                {results.map((r, i) => (
                    <ResultBlock key={i} index={i} result={r} />
                ))}

                <button
                    onClick={() => onComplete(globalScore, 100)}
                    style={{
                        width: "100%",
                        padding: 14,
                        background: "linear-gradient(135deg, #3B82F6, #2563EB)",
                        border: "none",
                        borderRadius: 10,
                        color: "#ffffff",
                        fontSize: 14,
                        fontWeight: 700,
                        cursor: "pointer",
                        marginTop: 8,
                    }}
                >
                    Continuer →
                </button>
            </div>
        );
    }

    // ── ÉCRAN RÉSULTAT D'UNE QUESTION ──
    if (phase === "result") {
        const result = results[results.length - 1];
        return (
            <div style={{ padding: 24 }}>
                <div style={{ textAlign: "center", marginBottom: 24 }}>
                    <div style={{ fontSize: 48, fontWeight: 700, color: scoreColor(result.score), fontFamily: "'JetBrains Mono', monospace" }}>
                        {result.score}<span style={{ fontSize: 20, color: "var(--text-muted)" }}>/100</span>
                    </div>
                    <div style={{ fontSize: 16, fontWeight: 600, color: scoreColor(result.score), marginTop: 4 }}>
                        {result.appreciation}
                    </div>
                </div>

                <p style={{ color: "var(--text-secondary)", fontSize: 14, fontStyle: "italic", textAlign: "center", lineHeight: 1.6, marginBottom: 20 }}>
                    {result.resume}
                </p>

                {result.pointsForts.length > 0 && <PointsBlock title="✅ CE QUE TU AS BIEN IDENTIFIÉ" items={result.pointsForts} color="success" />}
                {result.pointsManques.length > 0 && <PointsBlock title="📝 CE QUI MANQUAIT" items={result.pointsManques} color="warning" />}
                <ConseilBlock text={result.conseilExpert} />

                {!result.aiAvailable && (
                    <div style={{
                        background: "rgba(239,68,68,0.08)",
                        border: "1px solid rgba(239,68,68,0.2)",
                        borderRadius: 8,
                        padding: "10px 14px",
                        marginBottom: 16,
                        fontSize: 12,
                        color: "#f87171",
                    }}>
                        ⚠️ Évaluation simplifiée — Ollama hors ligne. Lancez `ollama serve` pour une analyse complète.
                    </div>
                )}

                <button
                    onClick={handleNext}
                    style={{
                        width: "100%",
                        padding: 14,
                        background: "linear-gradient(135deg, #3B82F6, #2563EB)",
                        border: "none",
                        borderRadius: 10,
                        color: "#ffffff",
                        fontSize: 14,
                        fontWeight: 700,
                        cursor: "pointer",
                    }}
                >
                    {allDone ? "Voir le bilan complet →" : `Question suivante (${currentQ + 2}/${questions.length}) →`}
                </button>
            </div>
        );
    }

    // ── ÉCRAN ÉVALUATION EN COURS ──
    if (phase === "evaluating") {
        return (
            <div style={{ padding: "60px 24px", textAlign: "center" }}>
                <div style={{
                    width: 56, height: 56,
                    border: "3px solid var(--bg-elevated)",
                    borderTop: "3px solid #3B82F6",
                    borderRadius: "50%",
                    animation: "spin 1s linear infinite",
                    margin: "0 auto 24px",
                }} />
                <div style={{ fontSize: 16, fontWeight: 600, color: "var(--text-primary)", marginBottom: 8 }}>
                    L&apos;IA analyse ta réponse...
                </div>
                <div style={{ fontSize: 13, color: "var(--text-muted)" }}>
                    Cela peut prendre quelques secondes
                </div>
                <style>{`@keyframes spin { to { transform: rotate(360deg); } }`}</style>
            </div>
        );
    }

    // ── ÉCRAN ÉCRITURE ──
    return (
        <div style={{ padding: 24 }}>
            <div style={{ display: "flex", justifyContent: "center", gap: 8, marginBottom: 24 }}>
                {questions.map((_, i) => (
                    <div key={i} style={{
                        width: i === currentQ ? 24 : 8,
                        height: 8,
                        borderRadius: 4,
                        background: i <= currentQ ? "var(--pr)" : "var(--bg-elevated)",
                        transition: "all 0.3s",
                    }} />
                ))}
            </div>

            <div style={{
                fontSize: 11,
                color: "var(--text-muted)",
                marginBottom: 8,
                fontFamily: "'JetBrains Mono', monospace",
                letterSpacing: "0.08em",
            }}>
                QUESTION {currentQ + 1} / {questions.length}
            </div>

            <div style={{
                background: "var(--bg-card)",
                border: "1px solid var(--border)",
                borderLeft: "3px solid #3B82F6",
                borderRadius: "0 10px 10px 0",
                padding: "16px 20px",
                marginBottom: 20,
            }}>
                <p style={{ color: "var(--text-primary)", fontSize: 15, fontWeight: 500, lineHeight: 1.65, margin: 0 }}>
                    {question.question}
                </p>
            </div>

            {question.hint && (
                <div style={{
                    fontSize: 12,
                    color: "var(--text-muted)",
                    fontStyle: "italic",
                    marginBottom: 12,
                    display: "flex",
                    alignItems: "center",
                    gap: 6,
                }}>
                    💡 {question.hint}
                </div>
            )}

            <div style={{ position: "relative" }}>
                <textarea
                    value={answers[question.id] ?? ""}
                    onChange={e => setAnswers(prev => ({ ...prev, [question.id]: e.target.value }))}
                    placeholder={`Rédigez votre analyse ici...\n\n(minimum ${minChars} caractères)`}
                    style={{
                        width: "100%",
                        minHeight: 180,
                        background: "var(--bg-input)",
                        border: "1px solid var(--border)",
                        borderRadius: 10,
                        padding: "14px 16px",
                        color: "var(--text-primary)",
                        fontSize: 14,
                        lineHeight: 1.65,
                        resize: "vertical",
                        outline: "none",
                        fontFamily: "Inter, sans-serif",
                        boxSizing: "border-box",
                    }}
                    onFocus={e => {
                        e.currentTarget.style.borderColor = "rgba(59,130,246,0.5)";
                        e.currentTarget.style.boxShadow = "0 0 0 3px rgba(59,130,246,0.1)";
                    }}
                    onBlur={e => {
                        e.currentTarget.style.borderColor = "var(--border)";
                        e.currentTarget.style.boxShadow = "none";
                    }}
                />
                <div style={{
                    position: "absolute",
                    bottom: 10,
                    right: 12,
                    fontSize: 11,
                    color: charCount >= minChars ? "#4ade80" : "var(--text-muted)",
                    fontFamily: "'JetBrains Mono', monospace",
                }}>
                    {charCount} / {minChars} min
                </div>
            </div>

            <div style={{ height: 3, background: "var(--bg-elevated)", borderRadius: 2, margin: "8px 0 20px", overflow: "hidden" }}>
                <div style={{
                    height: "100%",
                    background: charCount >= minChars ? "#10B981" : "var(--pr)",
                    borderRadius: 2,
                    width: `${Math.min(charCount / minChars * 100, 100)}%`,
                    transition: "width 0.3s, background 0.3s",
                }} />
            </div>

            <button
                onClick={handleSubmit}
                disabled={!canSubmit}
                style={{
                    width: "100%",
                    padding: 14,
                    background: canSubmit
                        ? "linear-gradient(135deg, #3B82F6, #2563EB)"
                        : "var(--bg-elevated)",
                    border: "none",
                    borderRadius: 10,
                    color: canSubmit ? "#ffffff" : "var(--text-muted)",
                    fontSize: 14,
                    fontWeight: 700,
                    cursor: canSubmit ? "pointer" : "not-allowed",
                    transition: "all 0.2s",
                    boxShadow: canSubmit ? "0 4px 16px rgba(59,130,246,0.3)" : "none",
                }}
            >
                {charCount < minChars
                    ? `Encore ${minChars - charCount} caractères...`
                    : "🤖 Soumettre à l'analyse IA"}
            </button>
        </div>
    );
}

function PointsBlock({ title, items, color }: { title: string; items: string[]; color: "success" | "warning" }) {
    const colors = color === "success"
        ? { bg: "rgba(34,197,94,0.08)", border: "rgba(34,197,94,0.25)", left: "#10B981", title: "#4ade80", text: "#86efac" }
        : { bg: "rgba(245,158,11,0.08)", border: "rgba(245,158,11,0.25)", left: "#f59e0b", title: "#fbbf24", text: "#fcd34d" };
    return (
        <div style={{
            background: colors.bg,
            border: `1px solid ${colors.border}`,
            borderLeft: `3px solid ${colors.left}`,
            borderRadius: "0 8px 8px 0",
            padding: "12px 16px",
            marginBottom: 12,
        }}>
            <div style={{ color: colors.title, fontWeight: 600, fontSize: 12, letterSpacing: "0.05em", marginBottom: 8 }}>
                {title}
            </div>
            {items.map((p, i) => (
                <div key={i} style={{ color: colors.text, fontSize: 13, lineHeight: 1.5, marginBottom: 3 }}>• {p}</div>
            ))}
        </div>
    );
}

function ConseilBlock({ text }: { text: string }) {
    return (
        <div style={{
            background: "rgba(59,130,246,0.08)",
            border: "1px solid rgba(59,130,246,0.2)",
            borderLeft: "3px solid #3B82F6",
            borderRadius: "0 8px 8px 0",
            padding: "12px 16px",
            marginBottom: 20,
        }}>
            <div style={{ color: "var(--pr-l)", fontWeight: 600, fontSize: 12, letterSpacing: "0.05em", marginBottom: 8 }}>
                💡 CONSEIL EXPERT
            </div>
            <div style={{ color: "var(--text-secondary)", fontSize: 13, lineHeight: 1.6 }}>{text}</div>
        </div>
    );
}

function ResultBlock({ index, result }: { index: number; result: QuestionResult }) {
    function scoreColor(score: number) {
        if (score >= 90) return "#4ade80";
        if (score >= 70) return "#a3e635";
        if (score >= 50) return "#fbbf24";
        return "#f87171";
    }
    return (
        <div style={{
            background: "var(--bg-card)",
            border: "1px solid var(--border)",
            borderRadius: 10,
            padding: 20,
            marginBottom: 16,
        }}>
            <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 12 }}>
                <span style={{ color: "var(--text-muted)", fontSize: 13 }}>Question {index + 1}</span>
                <span style={{ fontSize: 18, fontWeight: 700, color: scoreColor(result.score), fontFamily: "'JetBrains Mono', monospace" }}>
                    {result.score}/100
                </span>
            </div>
            <p style={{ color: "var(--text-secondary)", fontSize: 13, fontStyle: "italic", marginBottom: 12, lineHeight: 1.6 }}>
                {result.resume}
            </p>
            {result.pointsForts.length > 0 && <PointsBlock title="✅ POINTS FORTS" items={result.pointsForts} color="success" />}
            {result.pointsManques.length > 0 && <PointsBlock title="📝 À APPROFONDIR" items={result.pointsManques} color="warning" />}
            <ConseilBlock text={result.conseilExpert} />
        </div>
    );
}
