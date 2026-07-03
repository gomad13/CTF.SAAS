"use client";
import CountUp from "@/components/CountUp";
import KPICard from "@/components/premium/KPICard";
import { ListChecks, CircleCheckBig, TrendingUp } from "lucide-react";

import Link from "next/link";
import { useEffect, useMemo, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { apiFetch } from "@/lib/api";
import type { Me, AssignmentMine, RecentSubmission } from "@/lib/types";
import { useRiskScore, useRiskScoreHistory } from "@/lib/hooks/useRiskScore";
import { RiskScoreCard } from "@/components/risk-score/RiskScoreCard";
import { RiskScoreEvolutionChart } from "@/components/risk-score/RiskScoreEvolutionChart";

const LEVEL_LABELS: Record<string, string> = {
    beginner:     "Débutant",
    intermediate: "Intermédiaire",
    advanced:     "Avancé",
    expert:       "Expert",
};

export default function DashboardHome() {
    const meQ = useQuery<Me>({
        queryKey: ["me"],
        queryFn: () => apiFetch<Me>("/api/auth/me"),
        staleTime: 5 * 60 * 1000,
    });

    const assignQ = useQuery<AssignmentMine[]>({
        queryKey: ["assignments", "mine"],
        queryFn: () => apiFetch<AssignmentMine[]>("/api/assignments/mine"),
    });

    const recentQ = useQuery<RecentSubmission[]>({
        queryKey: ["submissions", "recent"],
        queryFn: () => apiFetch<RecentSubmission[]>("/api/submissions/recent"),
    });

    const stats = useMemo(() => {
        const a = assignQ.data ?? [];
        return {
            total:     a.length,
            completed: a.filter((x) => x.status === "completed").length,
            avgPct:    a.length ? Math.round(a.reduce((s, x) => s + x.progressPercent, 0) / a.length) : 0,
        };
    }, [assignQ.data]);

    const me = meQ.data;

    const riskScoreQ = useRiskScore();
    const riskHistoryQ = useRiskScoreHistory(6);

    return (
        <div style={{ maxWidth: 1200, margin: "0 auto", padding: "32px var(--page-x)" }}>
            <DemoFallbackBanner tenantName={me?.tenantName} />
            {/* Section 1 — Bienvenue */}
            <div style={{ marginBottom: 32 }}>
                <h1 style={{ fontSize: 24, fontWeight: 700, color: "var(--text)" }}>
                    Bonjour, {me?.firstName ?? "…"} 👋
                </h1>
                <p style={{ marginTop: 4, fontSize: 14, color: "var(--text-2)" }}>
                    {me?.tenantName ?? "—"}
                </p>
            </div>

            {/* Section 1.4 — Cyber Resilience Index (CRI) : pilier produit RSSI.
                Jauge à gauche (≥ md) ou pleine largeur en mobile, courbe à droite. */}
            <div className="mb-8 grid grid-cols-1 gap-4 md:grid-cols-3">
                <div className="md:col-span-1">
                    <RiskScoreCard
                        data={riskScoreQ.data}
                        isLoading={riskScoreQ.isLoading}
                        isError={riskScoreQ.isError}
                    />
                </div>
                <div className="md:col-span-2">
                    <RiskScoreEvolutionChart
                        data={riskHistoryQ.data}
                        isLoading={riskHistoryQ.isLoading}
                        isError={riskHistoryQ.isError}
                    />
                </div>
            </div>

            {/* Section 1.5 — Onboarding "Premiers pas" si parcours assignés mais 0 challenge complété */}
            <FirstStepsBanner assignments={assignQ.data} />

            {/* Section 2 — Stats cards */}
            <div style={{
                display: "grid",
                gridTemplateColumns: "repeat(auto-fit, minmax(200px, 1fr))",
                gap: 16,
                marginBottom: 32,
            }}>
                {assignQ.isLoading ? (
                    <>
                        <SkeletonCard />
                        <SkeletonCard />
                        <SkeletonCard />
                    </>
                ) : (
                    <>
                        <KPICard value={stats.total} label="Parcours en cours" icon={ListChecks} />
                        <KPICard value={stats.completed} label="Challenges complétés" icon={CircleCheckBig} />
                        <KPICard value={stats.avgPct} suffix="%" label="Progression moyenne" icon={TrendingUp} />
                    </>
                )}
            </div>

            {/* Section 3 — Mes parcours */}
            <section style={{ marginBottom: 32 }}>
                <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", marginBottom: 16 }}>
                    <h2 style={{ fontSize: 18, fontWeight: 600, color: "var(--text-primary)" }}>Mes parcours</h2>
                    <Link href="/dashboard/parcours" style={{
                        fontSize: 12,
                        color: "var(--primary)",
                        textDecoration: "none",
                    }}>
                        Voir tout →
                    </Link>
                </div>

                {assignQ.isLoading && <PathListSkeleton />}

                {assignQ.isError && (
                    <ErrorBox message={(assignQ.error as Error)?.message} />
                )}

                {assignQ.data?.length === 0 && (
                    <div style={{
                        background: "var(--bg-card)",
                        border: "1px solid var(--border)",
                        borderRadius: 10,
                        padding: "40px 20px",
                        textAlign: "center",
                        fontSize: 14,
                        color: "var(--text-muted)",
                    }}>
                        Aucun parcours assigné pour le moment.
                    </div>
                )}

                <div style={{ display: "flex", flexDirection: "column", gap: 12 }}>
                    {assignQ.data?.map((a) => (
                        <PathRow key={a.pathId} assignment={a} />
                    ))}
                </div>
            </section>

            {/* Section 4 — Activité récente */}
            <section>
                <h2 style={{ fontSize: 18, fontWeight: 600, color: "var(--text-primary)", marginBottom: 16 }}>Activité récente</h2>

                {recentQ.isLoading && <ActivitySkeleton />}

                {recentQ.data?.length === 0 && (
                    <div style={{
                        background: "var(--bg-card)",
                        border: "1px solid var(--border)",
                        borderRadius: 10,
                        padding: "32px 20px",
                        textAlign: "center",
                        fontSize: 14,
                        color: "var(--text-muted)",
                    }}>
                        Aucune activité récente
                    </div>
                )}

                <div style={{ display: "flex", flexDirection: "column", gap: 8 }}>
                    {recentQ.data?.map((s) => (
                        <div
                            key={s.id}
                            style={{
                                display: "flex",
                                alignItems: "center",
                                justifyContent: "space-between",
                                gap: 12,
                                background: "var(--bg-card)",
                                border: "1px solid var(--border)",
                                borderRadius: 10,
                                padding: "12px 16px",
                            }}
                        >
                            <div style={{ display: "flex", alignItems: "center", gap: 10, minWidth: 0 }}>
                                <span style={{ fontSize: 16, flexShrink: 0 }}>
                                    {s.isCorrect ? "✅" : "❌"}
                                </span>
                                <span style={{
                                    fontSize: 14,
                                    color: "var(--text-primary)",
                                    overflow: "hidden",
                                    textOverflow: "ellipsis",
                                    whiteSpace: "nowrap",
                                }}>
                                    {s.challengeTitle}
                                </span>
                            </div>
                            <div style={{ flexShrink: 0, textAlign: "right" }}>
                                {s.isCorrect && (
                                    <span style={{
                                        fontSize: 14,
                                        fontWeight: 600,
                                        fontFamily: "'JetBrains Mono', monospace",
                                        color: "var(--primary)",
                                    }}>
                                        +{s.scoreAwarded} pts
                                    </span>
                                )}
                                <div style={{
                                    fontSize: 11,
                                    fontFamily: "'JetBrains Mono', monospace",
                                    color: "var(--text-muted)",
                                    marginTop: 2,
                                }}>
                                    {new Date(s.submittedAt).toLocaleDateString("fr-FR")}
                                </div>
                            </div>
                        </div>
                    ))}
                </div>
            </section>
        </div>
    );
}

function StatCard({ value, label }: { value: string | number; label: string }) {
    return (
        <div style={{
            background: "var(--bg-card)",
            border: "1px solid var(--border)",
            borderRadius: 10,
            padding: 20,
        }}>
            <div style={{
                fontSize: 32,
                fontWeight: 700,
                fontFamily: "'JetBrains Mono', monospace",
                color: "var(--primary)",
            }}>
                {(() => { const m = String(value).match(/^(\d+)(.*)$/); return m ? <CountUp value={Number(m[1])} suffix={m[2]} /> : value; })()}
            </div>
            <div style={{
                fontSize: 11,
                letterSpacing: "0.1em",
                textTransform: "uppercase",
                color: "var(--text-muted)",
                marginTop: 4,
            }}>
                {label}
            </div>
        </div>
    );
}

function PathRow({ assignment: a }: { assignment: AssignmentMine }) {
    const STATUS_LABEL: Record<string, string> = {
        assigned:  "Assigné",
        started:   "En cours",
        completed: "Complété",
    };
    const level = a.pathLevel ? (LEVEL_LABELS[a.pathLevel] ?? a.pathLevel) : null;
    const pct = Math.max(0, Math.min(100, a.progressPercent));

    return (
        <div style={{
            background: "var(--bg-card)",
            border: "1px solid var(--border)",
            borderRadius: 10,
            padding: 20,
        }}>
            <div style={{ display: "flex", alignItems: "flex-start", justifyContent: "space-between", gap: 16 }}>
                <div style={{ minWidth: 0, flex: 1 }}>
                    <div style={{ display: "flex", flexWrap: "wrap", gap: 6, marginBottom: 6 }}>
                        {level && (
                            <span style={{
                                fontSize: 11,
                                background: "var(--accent-subtle)",
                                border: "1px solid var(--border)",
                                borderRadius: 4,
                                padding: "2px 8px",
                                color: "var(--text-secondary)",
                            }}>
                                {level}
                            </span>
                        )}
                        <span style={{
                            fontSize: 11,
                            borderRadius: 4,
                            padding: "2px 8px",
                            fontWeight: 500,
                            background: a.status === "completed" ? "var(--accent-subtle)" : a.status === "started" ? "var(--warning-subtle)" : "var(--surface-2)",
                            color: a.status === "completed" ? "var(--primary)" : a.status === "started" ? "var(--warning)" : "var(--text-muted)",
                        }}>
                            {STATUS_LABEL[a.status] ?? a.status}
                        </span>
                    </div>
                    <div style={{ fontSize: 15, fontWeight: 600, color: "var(--text-primary)", marginBottom: 10 }}>
                        {a.pathTitle ?? a.pathId}
                    </div>
                    {/* Progress bar */}
                    <div style={{ marginBottom: 4 }}>
                        <div style={{ display: "flex", justifyContent: "space-between", fontSize: 11, color: "var(--text-muted)", marginBottom: 4 }}>
                            <span>Progression</span>
                            <span style={{ fontFamily: "'JetBrains Mono', monospace", color: "var(--text-primary)" }}>{pct}%</span>
                        </div>
                        <div style={{ height: 3, background: "var(--accent-subtle)", borderRadius: 2, overflow: "hidden" }}>
                            <div style={{
                                height: "100%",
                                width: `${pct}%`,
                                background: "linear-gradient(90deg, var(--primary-dark), var(--primary))",
                                boxShadow: "0 0 6px var(--accent-subtle)",
                                borderRadius: 2,
                                transition: "width 0.5s",
                            }} />
                        </div>
                    </div>
                    {a.dueAt && (
                        <div style={{ fontSize: 11, color: "var(--text-muted)", marginTop: 4 }}>
                            Échéance : {new Date(a.dueAt).toLocaleDateString("fr-FR")}
                        </div>
                    )}
                </div>
                <Link
                    href={`/dashboard/parcours/${a.pathId}`}
                    style={{
                        flexShrink: 0,
                        background: "linear-gradient(135deg, var(--accent), var(--accent-hover))",
                        color: "#FFFFFF",
                        fontWeight: 700,
                        fontSize: 12,
                        fontFamily: "'JetBrains Mono', monospace",
                        letterSpacing: "0.05em",
                        padding: "8px 16px",
                        borderRadius: 8,
                        textDecoration: "none",
                        transition: "box-shadow 0.2s",
                    }}
                    onMouseOver={e => { e.currentTarget.style.boxShadow = "0 0 16px var(--accent-subtle)"; }}
                    onMouseOut={e => { e.currentTarget.style.boxShadow = "none"; }}
                >
                    Continuer
                </Link>
            </div>
        </div>
    );
}

function SkeletonCard() {
    return (
        <div style={{
            background: "var(--bg-card)",
            border: "1px solid var(--border)",
            borderRadius: 10,
            padding: 20,
            height: 80,
        }}>
            <div style={{ width: 60, height: 28, background: "var(--surface-2)", borderRadius: 4, marginBottom: 8 }} />
            <div style={{ width: 100, height: 12, background: "var(--surface-2)", borderRadius: 4 }} />
        </div>
    );
}

function PathListSkeleton() {
    return (
        <div style={{ display: "flex", flexDirection: "column", gap: 12 }}>
            {[0, 1].map((i) => (
                <div key={i} style={{
                    height: 96,
                    background: "var(--bg-card)",
                    border: "1px solid var(--border)",
                    borderRadius: 10,
                }} />
            ))}
        </div>
    );
}

function ActivitySkeleton() {
    return (
        <div style={{ display: "flex", flexDirection: "column", gap: 8 }}>
            {[0, 1, 2].map((i) => (
                <div key={i} style={{
                    height: 48,
                    background: "var(--bg-card)",
                    border: "1px solid var(--border)",
                    borderRadius: 10,
                }} />
            ))}
        </div>
    );
}

function FirstStepsBanner({ assignments }: { assignments?: AssignmentMine[] }) {
    const [dismissed, setDismissed] = useState(false);
    useEffect(() => {
        try {
            setDismissed(localStorage.getItem("sentys.firstSteps.dismissed") === "1");
        } catch { /* noop */ }
    }, []);

    if (dismissed) return null;
    if (!assignments || assignments.length === 0) return null;

    // On affiche l'encart si l'utilisateur a au moins 1 parcours assigné mais 0 progression.
    const anyStarted = assignments.some(a => (a.progressPercent ?? 0) > 0);
    if (anyStarted) return null;

    const first = assignments[0];
    const close = () => {
        try { localStorage.setItem("sentys.firstSteps.dismissed", "1"); } catch { /* noop */ }
        setDismissed(true);
    };

    return (
        <div
            role="region"
            aria-label="Premiers pas"
            style={{
                marginBottom: 24,
                padding: "20px 24px",
                background: "linear-gradient(90deg, var(--accent-subtle), var(--accent-subtle))",
                border: "1px solid var(--accent-border)",
                borderRadius: 12,
                display: "flex",
                gap: 16,
                alignItems: "center",
                flexWrap: "wrap",
                position: "relative",
            }}
        >
            <span aria-hidden="true" style={{ fontSize: 28 }}>🚀</span>
            <div style={{ flex: 1, minWidth: 240 }}>
                <h2 style={{ fontSize: 16, fontWeight: 700, color: "var(--text)", margin: 0 }}>
                    Prêt à commencer votre formation ?
                </h2>
                <p style={{ fontSize: 13, color: "var(--text-2)", margin: "4px 0 0", lineHeight: 1.5 }}>
                    Vous avez {assignments.length} parcours assigné{assignments.length > 1 ? "s" : ""}. Démarrez avec le premier.
                </p>
            </div>
            <Link
                href={`/dashboard/parcours/${first.pathId}`}
                style={{
                    background: "var(--accent)",
                    color: "#FFFFFF",
                    textDecoration: "none",
                    padding: "10px 18px",
                    borderRadius: 8,
                    fontSize: 13,
                    fontWeight: 600,
                    whiteSpace: "nowrap",
                }}
            >
                Commencer ma formation →
            </Link>
            <button
                type="button"
                onClick={close}
                aria-label="Masquer cet encart"
                style={{
                    position: "absolute",
                    top: 8,
                    right: 10,
                    background: "transparent",
                    border: "none",
                    color: "var(--text-3)",
                    cursor: "pointer",
                    fontSize: 16,
                    padding: 4,
                }}
            >
                ×
            </button>
        </div>
    );
}

function ErrorBox({ message }: { message?: string }) {
    return (
        <div style={{
            background: "rgba(239,68,68,0.08)",
            border: "1px solid rgba(239,68,68,0.25)",
            borderRadius: 8,
            padding: "10px 14px",
            fontSize: 14,
            color: "var(--danger-t)",
        }}>
            {message || "Erreur de chargement"}
        </div>
    );
}

function DemoFallbackBanner({ tenantName }: { tenantName?: string | null }) {
    const [dismissed, setDismissed] = useState(false);
    const [show, setShow] = useState(false);

    useEffect(() => {
        // Cookie non-HttpOnly posé par le backend après SSO fallback Demo
        const cookies = document.cookie.split(";").map(c => c.trim());
        const hasFallback = cookies.some(c => c.startsWith("sso_demo_fallback="));
        const wasDismissed = typeof window !== "undefined" && sessionStorage.getItem("demo_banner_dismissed") === "1";
        setShow(hasFallback && !wasDismissed);
    }, []);

    if (!show || dismissed) return null;

    return (
        <div style={{
            marginBottom: 20,
            background: "linear-gradient(90deg, var(--accent-subtle), var(--accent-subtle))",
            border: "1px solid var(--accent-border)",
            borderRadius: 10,
            padding: "14px 18px",
            display: "flex", alignItems: "center", gap: 14,
        }}>
            <span style={{ fontSize: 22 }}>✨</span>
            <div style={{ flex: 1, fontSize: 13, color: "var(--text-2)", lineHeight: 1.5 }}>
                <strong style={{ color: "var(--accent)" }}>Bienvenue sur Sentys en mode Démo.</strong>
                {" "}Votre organisation {tenantName ? `(${tenantName})` : ""} n'est pas encore cliente —{" "}
                <a href="mailto:commercial@sentys.local?subject=Accès%20Sentys%20entreprise"
                    style={{ color: "var(--accent)", textDecoration: "underline" }}>
                    contacter notre équipe commerciale
                </a>
                {" "}pour accéder aux formations dédiées à votre entreprise.
            </div>
            <button
                onClick={() => { setDismissed(true); sessionStorage.setItem("demo_banner_dismissed", "1"); }}
                style={{ background: "transparent", border: "none", color: "var(--text-3)", cursor: "pointer", fontSize: 18, padding: 4 }}
                aria-label="Fermer"
            >✕</button>
        </div>
    );
}
