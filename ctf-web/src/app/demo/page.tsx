"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { apiFetch } from "@/lib/api";
import CeoFraudChallenge from "@/components/challenges/CeoFraudChallenge";
import MailboxChallenge from "@/components/challenges/MailboxChallenge";
import MultichoiceChallenge from "@/components/challenges/MultichoiceChallenge";
import PasswordQuizChallenge from "@/components/challenges/PasswordQuizChallenge";
import DemoProgress from "@/components/DemoProgress";
import Reveal from "@/components/Reveal";
import { Stagger, StaggerItem } from "@/components/Stagger";

// ── Config ────────────────────────────────────────────────────────────────────

const DEMO_CHALLENGES = [
    {
        id:          "10000000-0000-0000-0000-000000000001",
        contentType: "ceo_fraud",
        title:       "Arnaque au Président",
        icon:        "🎭",
        accent:      "var(--danger)",
        borderColor: "color-mix(in srgb, var(--danger) 25%, transparent)",
    },
    {
        id:          "10000000-0000-0000-0000-000000000002",
        contentType: "mailbox",
        title:       "La Boîte Mail Piégée",
        icon:        "📬",
        accent:      "var(--warning)",
        borderColor: "color-mix(in srgb, var(--warning) 25%, transparent)",
    },
    {
        id:          "10000000-0000-0000-0000-000000000003",
        contentType: "multichoice",
        title:       "Décryptez le Phishing",
        icon:        "🎣",
        accent:      "var(--pr-l)",
        borderColor: "color-mix(in srgb, var(--accent) 25%, transparent)",
    },
    {
        id:          "10000000-0000-0000-0000-000000000004",
        contentType: "multichoice",
        title:       "RGPD en Pratique",
        icon:        "🔒",
        accent:      "var(--pr-l)",
        borderColor: "color-mix(in srgb, var(--accent) 25%, transparent)",
    },
    {
        id:          "10000000-0000-0000-0000-000000000005",
        contentType: "password_quiz",
        title:       "Mots de Passe",
        icon:        "🔑",
        accent:      "var(--success)",
        borderColor: "color-mix(in srgb, var(--success) 25%, transparent)",
    },
];

// ── Types ─────────────────────────────────────────────────────────────────────

// eslint-disable-next-line @typescript-eslint/no-explicit-any
type AnyContent = Record<string, any>;

interface ChallengeData {
    id: string;
    contentType: string;
    title: string;
    instructions: string;
    points: number;
    content: AnyContent;
}

interface ProgressCompletion {
    challengeId: string;
    challengeTitle: string;
    maxPoints: number;
    pointsEarned: number;
    scorePercent: number;
    completed: boolean;
}

interface ProgressData {
    totalPoints: number;
    maxPoints: number;
    completions: ProgressCompletion[];
    allCompleted: boolean;
}

type StepScore = { score: number; maxScore: number } | null;

// ── Page ──────────────────────────────────────────────────────────────────────

export default function DemoPage() {
    const [step, setStep]             = useState<number>(0);
    const [scores, setScores]         = useState<StepScore[]>([null, null, null, null, null]);
    const [challenges, setChallenges] = useState<(ChallengeData | null)[]>([null, null, null, null, null]);
    const [loadError, setLoadError]   = useState<string | null>(null);
    const [progressKey, setProgressKey] = useState(0);
    const [progress, setProgress]     = useState<ProgressData | null>(null);

    // Load challenge content
    useEffect(() => {
        DEMO_CHALLENGES.forEach((cfg, i) => {
            apiFetch<ChallengeData>(`/api/challenges/interactive/${cfg.id}/content`)
                .then(data => setChallenges(prev => { const n = [...prev]; n[i] = data; return n; }))
                .catch(err => setLoadError(err instanceof Error ? err.message : "Impossible de charger les missions."));
        });
    }, []);

    // Load progress
    useEffect(() => {
        apiFetch<ProgressData>("/api/progress/demo")
            .then(setProgress)
            .catch(() => {/* silent */});
    }, [progressKey]);

    function handleComplete(stepIndex: number, score: number, maxScore: number) {
        setScores(prev => { const n = [...prev]; n[stepIndex] = { score, maxScore }; return n; });
        setProgressKey(k => k + 1);  // refresh progress bar
        setStep(stepIndex + 1);
    }

    function handleReset() {
        setStep(0);
        setScores([null, null, null, null, null]);
        setProgressKey(k => k + 1);
    }

    // ── Final / allCompleted screen ────────────────────────────────────────────
    if (step === 5 || progress?.allCompleted) {
        const pts    = progress?.totalPoints ?? scores.reduce((a, s) => a + (s?.score ?? 0), 0);
        const maxPts = progress?.maxPoints  ?? scores.reduce((a, s) => a + (s?.maxScore ?? 0), 0);
        const pct    = maxPts > 0 ? Math.round((pts / maxPts) * 100) : 0;

        return (
            <div style={{ minHeight: "100vh", background: "var(--bg)", display: "flex", flexDirection: "column", alignItems: "center", justifyContent: "center", padding: "48px 16px" }}>
                <Reveal>
                <div style={{ width: "100%", maxWidth: 520, background: "var(--surface)", border: "1px solid color-mix(in srgb, var(--accent) 40%, transparent)", borderRadius: 16, padding: "32px 28px" }}>
                    <div style={{ textAlign: "center", marginBottom: 24 }}>
                        <div style={{ fontSize: 48, marginBottom: 8 }}>🏆</div>
                        <h1 style={{ color: "var(--text)", fontSize: 24, fontWeight: 700, margin: 0 }}>Parcours terminé !</h1>
                        <p style={{ color: "var(--text-3)", fontSize: 14, marginTop: 6 }}>
                            {pct >= 80 ? "Excellent niveau de vigilance !" : pct >= 50 ? "Bon début — continuez à progresser." : "Quelques axes d'amélioration importants."}
                        </p>
                    </div>

                    {/* Score */}
                    <div style={{ textAlign: "center", marginBottom: 20 }}>
                        <p style={{ color: "var(--pr)", fontSize: 48, fontWeight: 700, lineHeight: 1, margin: 0 }}>{pts}</p>
                        <p style={{ color: "var(--text-3)", fontSize: 14, margin: "4px 0 12px" }}>/ {maxPts} points</p>
                        <div style={{ height: 8, background: "var(--surface-2)", borderRadius: 99, overflow: "hidden" }}>
                            <div style={{ height: "100%", width: `${pct}%`, background: "var(--pr)", borderRadius: 99, transition: "width 0.8s ease" }} />
                        </div>
                        <p style={{ color: "var(--pr)", fontSize: 13, marginTop: 4 }}>{pct}%</p>
                    </div>

                    {/* Per-mission */}
                    <Stagger className="flex flex-col gap-2 mb-6" gap={0.05}>
                        {(progress?.completions ?? DEMO_CHALLENGES.map((c, i) => ({
                            challengeTitle: c.title,
                            maxPoints: [100, 150, 200, 175, 125][i],
                            pointsEarned: scores[i]?.score ?? 0,
                            completed: scores[i] !== null,
                        }))).map((c, i) => {
                            const cfg = DEMO_CHALLENGES[i];
                            return (
                                <StaggerItem key={i}>
                                <div style={{ display: "flex", alignItems: "center", gap: 12, background: "var(--surface-2)", borderRadius: 8, padding: "10px 14px" }}>
                                    <span style={{ fontSize: 20 }}>{cfg.icon}</span>
                                    <div style={{ flex: 1, minWidth: 0 }}>
                                        <p style={{ color: "var(--text)", fontSize: 13, fontWeight: 500, margin: 0 }}>{c.challengeTitle}</p>
                                        <div style={{ marginTop: 4, height: 3, background: "var(--surface)", borderRadius: 99, overflow: "hidden" }}>
                                            <div style={{ height: "100%", width: `${c.maxPoints > 0 ? Math.round(c.pointsEarned / c.maxPoints * 100) : 0}%`, background: "var(--pr)", borderRadius: 99 }} />
                                        </div>
                                    </div>
                                    <span style={{ color: c.completed ? "var(--pr)" : "var(--text-3)", fontSize: 13, fontWeight: 600, whiteSpace: "nowrap" }}>
                                        {c.pointsEarned} / {c.maxPoints}
                                    </span>
                                </div>
                                </StaggerItem>
                            );
                        })}
                    </Stagger>

                    {/* CTAs */}
                    <div style={{ display: "flex", flexDirection: "column", gap: 8 }}>
                        <Link
                            href="/register"
                            className="transition-colors duration-200"
                            style={{ display: "block", textAlign: "center", background: "var(--pr)", color: "var(--on-accent)", borderRadius: 8, padding: "13px 0", fontSize: 14, fontWeight: 600, textDecoration: "none" }}
                        >
                            Créer un compte gratuit →
                        </Link>
                        <button
                            onClick={async () => {
                                await apiFetch("/api/progress/demo/reset", { method: "POST" });
                                handleReset();
                            }}
                            className="transition-colors duration-200"
                            style={{ background: "transparent", border: "1px solid var(--border)", color: "var(--text-3)", borderRadius: 8, padding: "12px 0", fontSize: 13, cursor: "pointer" }}
                        >
                            ↺ Recommencer le parcours
                        </button>
                    </div>
                </div>
                </Reveal>
            </div>
        );
    }

    const cfg       = DEMO_CHALLENGES[step];
    const challenge = challenges[step];

    // ── Loading ────────────────────────────────────────────────────────────────
    if (!challenge) {
        return (
            <div style={{ minHeight: "100vh", background: "var(--bg)", display: "flex", alignItems: "center", justifyContent: "center" }}>
                {loadError ? (
                    <div style={{ textAlign: "center" }}>
                        <p style={{ color: "var(--danger)", marginBottom: 12 }}>{loadError}</p>
                        <Link href="/login" style={{ color: "var(--pr)", fontSize: 13 }}>Se connecter pour accéder</Link>
                    </div>
                ) : (
                    <div style={{ display: "flex", alignItems: "center", gap: 10, color: "var(--text-3)" }}>
                        <span style={{ width: 20, height: 20, borderRadius: "50%", border: "2px solid var(--surface-2)", borderTopColor: "var(--pr)", display: "inline-block", animation: "spin 1s linear infinite" }} />
                        <style>{`@keyframes spin { to { transform: rotate(360deg); } }`}</style>
                        <span style={{ fontSize: 14 }}>Chargement des missions…</span>
                    </div>
                )}
            </div>
        );
    }

    // ── Challenge step ─────────────────────────────────────────────────────────
    return (
        <div style={{ minHeight: "100vh", background: "var(--bg)", color: "var(--text)", display: "flex", flexDirection: "column" }}>
            {/* Back link */}
            <div style={{ position: "fixed", left: 16, top: 16, zIndex: 50 }}>
                <Link href="/landing" style={{ display: "inline-flex", alignItems: "center", gap: 6, fontSize: 14, fontWeight: 600, textDecoration: "none" }}>
                    <span style={{ color: "var(--text-3)" }}>←</span>
                    <span style={{ color: "var(--pr)" }}>CTF</span>
                    <span style={{ color: "var(--text)" }}>SaaS</span>
                </Link>
            </div>

            <div style={{ flex: 1, maxWidth: 680, margin: "0 auto", width: "100%", padding: "56px 16px 24px" }}>
                {/* Stepper */}
                <div style={{ display: "flex", alignItems: "center", justifyContent: "center", gap: 8, marginBottom: 28 }}>
                    {DEMO_CHALLENGES.map((c, i) => (
                        <div key={c.id} style={{ display: "flex", alignItems: "center", gap: 8 }}>
                            <div style={{
                                display: "flex", alignItems: "center", justifyContent: "center",
                                width: 30, height: 30, borderRadius: "50%", fontSize: 13, fontWeight: 700,
                                border: `2px solid ${i < step ? "var(--pr)" : i === step ? cfg.accent : "var(--surface-2)"}`,
                                background: i < step ? "var(--pr)" : i === step ? `color-mix(in srgb, ${cfg.accent} 12%, transparent)` : "transparent",
                                color: i < step ? "var(--on-accent)" : i === step ? cfg.accent : "var(--text-3)",
                                transition: "all 0.2s",
                            }}>
                                {i < step ? "✓" : i + 1}
                            </div>
                            <span style={{ fontSize: 11, display: "none", color: i === step ? "var(--text)" : "var(--text-3)", fontWeight: i === step ? 600 : 400 }}
                                className="sm:block"
                            >{c.title}</span>
                            {i < DEMO_CHALLENGES.length - 1 && (
                                <div style={{ width: 24, height: 2, borderRadius: 99, background: i < step ? "var(--pr)" : "var(--surface-2)", transition: "background 0.3s" }} />
                            )}
                        </div>
                    ))}
                </div>

                {/* Card */}
                <Reveal>
                <div style={{ background: "var(--surface)", border: `1px solid ${cfg.borderColor}`, borderRadius: 14, padding: "20px 20px 24px" }}>
                    {/* Header */}
                    <div style={{ display: "flex", alignItems: "flex-start", gap: 14, marginBottom: 18, borderBottom: "1px solid var(--border)", paddingBottom: 16 }}>
                        <span style={{ fontSize: 36 }}>{cfg.icon}</span>
                        <div style={{ flex: 1, minWidth: 0 }}>
                            <p style={{ color: "var(--text-3)", fontSize: 11, textTransform: "uppercase", letterSpacing: "0.08em", margin: 0 }}>
                                Mission {step + 1} / {DEMO_CHALLENGES.length}
                            </p>
                            <h1 style={{ color: "var(--text)", fontSize: 20, fontWeight: 700, margin: "4px 0 6px" }}>{challenge.title}</h1>
                            <p style={{ color: "var(--text-3)", fontSize: 13, margin: 0, lineHeight: 1.5 }}>{challenge.instructions}</p>
                        </div>
                        <div style={{ flexShrink: 0, background: "var(--surface-2)", border: "1px solid var(--border)", borderRadius: 8, padding: "6px 10px", textAlign: "center" }}>
                            <p style={{ color: "var(--text-3)", fontSize: 10, margin: 0 }}>Points</p>
                            <p style={{ color: "var(--pr)", fontSize: 15, fontWeight: 700, margin: 0 }}>{challenge.points}</p>
                        </div>
                    </div>

                    {/* Challenge component */}
                    {challenge.contentType === "ceo_fraud" && (
                        <CeoFraudChallenge
                            challengeId={challenge.id}
                            content={challenge.content as Parameters<typeof CeoFraudChallenge>[0]["content"]}
                            onComplete={(s, m) => handleComplete(step, s ?? 0, m ?? 0)}
                        />
                    )}
                    {challenge.contentType === "mailbox" && (
                        <MailboxChallenge
                            challengeId={challenge.id}
                            content={challenge.content as Parameters<typeof MailboxChallenge>[0]["content"]}
                            onComplete={(s, m) => handleComplete(step, s ?? 0, m ?? 0)}
                        />
                    )}
                    {challenge.contentType === "multichoice" && (
                        <MultichoiceChallenge
                            challengeId={challenge.id}
                            content={challenge.content as Parameters<typeof MultichoiceChallenge>[0]["content"]}
                            onComplete={(s, m) => handleComplete(step, s ?? 0, m ?? 0)}
                        />
                    )}
                    {challenge.contentType === "password_quiz" && (
                        <PasswordQuizChallenge
                            challengeId={challenge.id}
                            content={challenge.content as Parameters<typeof PasswordQuizChallenge>[0]["content"]}
                            onComplete={(s, m) => handleComplete(step, s ?? 0, m ?? 0)}
                        />
                    )}
                </div>
                </Reveal>

                {/* Running score */}
                {scores.some(s => s !== null) && (
                    <p style={{ textAlign: "center", color: "var(--text-3)", fontSize: 12, marginTop: 10 }}>
                        Score en cours :{" "}
                        <span style={{ color: "var(--pr)", fontFamily: "monospace", fontWeight: 700 }}>
                            {scores.reduce((a, s) => a + (s?.score ?? 0), 0)}
                        </span> pts
                    </p>
                )}
            </div>

            {/* Sticky progress bar */}
            <DemoProgress refreshKey={progressKey} onReset={handleReset} />
        </div>
    );
}
