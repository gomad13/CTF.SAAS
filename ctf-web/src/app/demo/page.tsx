"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { apiFetch } from "@/lib/api";
import CeoFraudChallenge from "@/components/challenges/CeoFraudChallenge";
import MailboxChallenge from "@/components/challenges/MailboxChallenge";
import MultichoiceChallenge from "@/components/challenges/MultichoiceChallenge";
import PasswordQuizChallenge from "@/components/challenges/PasswordQuizChallenge";
import DemoProgress from "@/components/DemoProgress";

// ── Config ────────────────────────────────────────────────────────────────────

const DEMO_CHALLENGES = [
    {
        id:          "10000000-0000-0000-0000-000000000001",
        contentType: "ceo_fraud",
        title:       "Arnaque au Président",
        icon:        "🎭",
        accent:      "#ef4444",
        borderColor: "rgba(239,68,68,0.25)",
    },
    {
        id:          "10000000-0000-0000-0000-000000000002",
        contentType: "mailbox",
        title:       "La Boîte Mail Piégée",
        icon:        "📬",
        accent:      "#f97316",
        borderColor: "rgba(249,115,22,0.25)",
    },
    {
        id:          "10000000-0000-0000-0000-000000000003",
        contentType: "multichoice",
        title:       "Décryptez le Phishing",
        icon:        "🎣",
        accent:      "var(--pr-l)",
        borderColor: "rgba(167,139,250,0.25)",
    },
    {
        id:          "10000000-0000-0000-0000-000000000004",
        contentType: "multichoice",
        title:       "RGPD en Pratique",
        icon:        "🔒",
        accent:      "var(--pr-l)",
        borderColor: "rgba(96,165,250,0.25)",
    },
    {
        id:          "10000000-0000-0000-0000-000000000005",
        contentType: "password_quiz",
        title:       "Mots de Passe",
        icon:        "🔑",
        accent:      "#34d399",
        borderColor: "rgba(52,211,153,0.25)",
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
            <div style={{ minHeight: "100vh", background: "#0a0f0f", display: "flex", flexDirection: "column", alignItems: "center", justifyContent: "center", padding: "48px 16px" }}>
                <div style={{ width: "100%", maxWidth: 520, background: "#FFFFFF", border: "1px solid rgba(59,130,246,0.4)", borderRadius: 16, padding: "32px 28px" }}>
                    <div style={{ textAlign: "center", marginBottom: 24 }}>
                        <div style={{ fontSize: 48, marginBottom: 8 }}>🏆</div>
                        <h1 style={{ color: "#1E293B", fontSize: 24, fontWeight: 700, margin: 0 }}>Parcours terminé !</h1>
                        <p style={{ color: "#94A3B8", fontSize: 14, marginTop: 6 }}>
                            {pct >= 80 ? "Excellent niveau de vigilance !" : pct >= 50 ? "Bon début — continuez à progresser." : "Quelques axes d'amélioration importants."}
                        </p>
                    </div>

                    {/* Score */}
                    <div style={{ textAlign: "center", marginBottom: 20 }}>
                        <p style={{ color: "var(--pr)", fontSize: 48, fontWeight: 700, lineHeight: 1, margin: 0 }}>{pts}</p>
                        <p style={{ color: "#94A3B8", fontSize: 14, margin: "4px 0 12px" }}>/ {maxPts} points</p>
                        <div style={{ height: 8, background: "#1e293b", borderRadius: 99, overflow: "hidden" }}>
                            <div style={{ height: "100%", width: `${pct}%`, background: "var(--pr)", borderRadius: 99, transition: "width 0.8s ease" }} />
                        </div>
                        <p style={{ color: "var(--pr)", fontSize: 13, marginTop: 4 }}>{pct}%</p>
                    </div>

                    {/* Per-mission */}
                    <div style={{ display: "flex", flexDirection: "column", gap: 8, marginBottom: 24 }}>
                        {(progress?.completions ?? DEMO_CHALLENGES.map((c, i) => ({
                            challengeTitle: c.title,
                            maxPoints: [100, 150, 200, 175, 125][i],
                            pointsEarned: scores[i]?.score ?? 0,
                            completed: scores[i] !== null,
                        }))).map((c, i) => {
                            const cfg = DEMO_CHALLENGES[i];
                            return (
                                <div key={i} style={{ display: "flex", alignItems: "center", gap: 12, background: "rgba(255,255,255,0.03)", borderRadius: 8, padding: "10px 14px" }}>
                                    <span style={{ fontSize: 20 }}>{cfg.icon}</span>
                                    <div style={{ flex: 1, minWidth: 0 }}>
                                        <p style={{ color: "#e2e8f0", fontSize: 13, fontWeight: 500, margin: 0 }}>{c.challengeTitle}</p>
                                        <div style={{ marginTop: 4, height: 3, background: "#1e293b", borderRadius: 99, overflow: "hidden" }}>
                                            <div style={{ height: "100%", width: `${c.maxPoints > 0 ? Math.round(c.pointsEarned / c.maxPoints * 100) : 0}%`, background: "var(--pr)", borderRadius: 99 }} />
                                        </div>
                                    </div>
                                    <span style={{ color: c.completed ? "var(--pr)" : "#4b5563", fontSize: 13, fontWeight: 600, whiteSpace: "nowrap" }}>
                                        {c.pointsEarned} / {c.maxPoints}
                                    </span>
                                </div>
                            );
                        })}
                    </div>

                    {/* CTAs */}
                    <div style={{ display: "flex", flexDirection: "column", gap: 8 }}>
                        <Link
                            href="/register"
                            style={{ display: "block", textAlign: "center", background: "var(--pr)", color: "#fff", borderRadius: 8, padding: "13px 0", fontSize: 14, fontWeight: 600, textDecoration: "none" }}
                        >
                            Créer un compte gratuit →
                        </Link>
                        <button
                            onClick={async () => {
                                await apiFetch("/api/progress/demo/reset", { method: "POST" });
                                handleReset();
                            }}
                            style={{ background: "transparent", border: "1px solid rgba(255,255,255,0.1)", color: "#94A3B8", borderRadius: 8, padding: "12px 0", fontSize: 13, cursor: "pointer" }}
                        >
                            ↺ Recommencer le parcours
                        </button>
                    </div>
                </div>
            </div>
        );
    }

    const cfg       = DEMO_CHALLENGES[step];
    const challenge = challenges[step];

    // ── Loading ────────────────────────────────────────────────────────────────
    if (!challenge) {
        return (
            <div style={{ minHeight: "100vh", background: "#0a0f0f", display: "flex", alignItems: "center", justifyContent: "center" }}>
                {loadError ? (
                    <div style={{ textAlign: "center" }}>
                        <p style={{ color: "#f87171", marginBottom: 12 }}>{loadError}</p>
                        <Link href="/login" style={{ color: "var(--pr)", fontSize: 13 }}>Se connecter pour accéder</Link>
                    </div>
                ) : (
                    <div style={{ display: "flex", alignItems: "center", gap: 10, color: "#94A3B8" }}>
                        <span style={{ width: 20, height: 20, borderRadius: "50%", border: "2px solid #1e293b", borderTopColor: "var(--pr)", display: "inline-block", animation: "spin 1s linear infinite" }} />
                        <style>{`@keyframes spin { to { transform: rotate(360deg); } }`}</style>
                        <span style={{ fontSize: 14 }}>Chargement des missions…</span>
                    </div>
                )}
            </div>
        );
    }

    // ── Challenge step ─────────────────────────────────────────────────────────
    return (
        <div style={{ minHeight: "100vh", background: "#0a0f0f", color: "#fff", display: "flex", flexDirection: "column" }}>
            {/* Back link */}
            <div style={{ position: "fixed", left: 16, top: 16, zIndex: 50 }}>
                <Link href="/landing" style={{ display: "inline-flex", alignItems: "center", gap: 6, fontSize: 14, fontWeight: 600, textDecoration: "none" }}>
                    <span style={{ color: "#94A3B8" }}>←</span>
                    <span style={{ color: "var(--pr)" }}>CTF</span>
                    <span style={{ color: "#fff" }}>SaaS</span>
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
                                border: `2px solid ${i < step ? "var(--pr)" : i === step ? cfg.accent : "#1e293b"}`,
                                background: i < step ? "var(--pr)" : i === step ? `${cfg.accent}20` : "transparent",
                                color: i < step ? "#fff" : i === step ? cfg.accent : "#4b5563",
                                transition: "all 0.2s",
                            }}>
                                {i < step ? "✓" : i + 1}
                            </div>
                            <span style={{ fontSize: 11, display: "none", color: i === step ? "#fff" : "#4b5563", fontWeight: i === step ? 600 : 400 }}
                                className="sm:block"
                            >{c.title}</span>
                            {i < DEMO_CHALLENGES.length - 1 && (
                                <div style={{ width: 24, height: 2, borderRadius: 99, background: i < step ? "var(--pr)" : "#1e293b", transition: "background 0.3s" }} />
                            )}
                        </div>
                    ))}
                </div>

                {/* Card */}
                <div style={{ background: "#FFFFFF", border: `1px solid ${cfg.borderColor}`, borderRadius: 14, padding: "20px 20px 24px" }}>
                    {/* Header */}
                    <div style={{ display: "flex", alignItems: "flex-start", gap: 14, marginBottom: 18, borderBottom: "1px solid #E2E8F0", paddingBottom: 16 }}>
                        <span style={{ fontSize: 36 }}>{cfg.icon}</span>
                        <div style={{ flex: 1, minWidth: 0 }}>
                            <p style={{ color: "#94A3B8", fontSize: 11, textTransform: "uppercase", letterSpacing: "0.08em", margin: 0 }}>
                                Mission {step + 1} / {DEMO_CHALLENGES.length}
                            </p>
                            <h1 style={{ color: "#fff", fontSize: 20, fontWeight: 700, margin: "4px 0 6px" }}>{challenge.title}</h1>
                            <p style={{ color: "#94A3B8", fontSize: 13, margin: 0, lineHeight: 1.5 }}>{challenge.instructions}</p>
                        </div>
                        <div style={{ flexShrink: 0, background: "rgba(255,255,255,0.04)", border: "1px solid #E2E8F0", borderRadius: 8, padding: "6px 10px", textAlign: "center" }}>
                            <p style={{ color: "#94A3B8", fontSize: 10, margin: 0 }}>Points</p>
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

                {/* Running score */}
                {scores.some(s => s !== null) && (
                    <p style={{ textAlign: "center", color: "#94A3B8", fontSize: 12, marginTop: 10 }}>
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
