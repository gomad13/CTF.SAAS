"use client";
import Link from "next/link";
import { useEffect, useMemo, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { ShieldCheck, ListChecks, CircleCheckBig, TrendingUp, Check, X } from "lucide-react";
import { apiFetch } from "@/lib/api";
import type { Me, AssignmentMine, RecentSubmission } from "@/lib/types";
import { useRiskScore, useRiskScoreHistory } from "@/lib/hooks/useRiskScore";
import KPICard from "@/components/premium/KPICard";
import ChartCard from "@/components/premium/ChartCard";
import AreaChartCard from "@/components/premium/AreaChartCard";
import DonutChart from "@/components/premium/DonutChart";
import Reveal from "@/components/Reveal";
import { Stagger, StaggerItem } from "@/components/Stagger";

const LEVEL_LABELS: Record<string, string> = {
    beginner: "Débutant", intermediate: "Intermédiaire", advanced: "Avancé", expert: "Expert",
};
function criBand(score: number): string {
    if (score >= 80) return "Excellent";
    if (score >= 60) return "Bon";
    if (score >= 40) return "Moyen";
    return "À renforcer";
}

export default function DashboardHome() {
    const meQ = useQuery<Me>({ queryKey: ["me"], queryFn: () => apiFetch<Me>("/api/auth/me"), staleTime: 5 * 60 * 1000 });
    const assignQ = useQuery<AssignmentMine[]>({ queryKey: ["assignments", "mine"], queryFn: () => apiFetch<AssignmentMine[]>("/api/assignments/mine") });
    const recentQ = useQuery<RecentSubmission[]>({ queryKey: ["submissions", "recent"], queryFn: () => apiFetch<RecentSubmission[]>("/api/submissions/recent") });
    const riskScoreQ = useRiskScore();
    const riskHistoryQ = useRiskScoreHistory(6);

    const stats = useMemo(() => {
        const a = assignQ.data ?? [];
        return {
            total: a.length,
            completed: a.filter(x => x.status === "completed").length,
            avgPct: a.length ? Math.round(a.reduce((s, x) => s + x.progressPercent, 0) / a.length) : 0,
            assigned: a.filter(x => x.status === "assigned").length,
            started: a.filter(x => x.status === "started").length,
        };
    }, [assignQ.data]);

    const cri = riskScoreQ.data?.score ?? null;
    const histData = useMemo(
        () => (riskHistoryQ.data ?? []).filter(p => p.score != null)
            .map(p => ({ label: new Date(p.date).toLocaleDateString("fr-FR", { month: "short" }), value: p.score as number })),
        [riskHistoryQ.data]);
    const donutData = useMemo(() => [
        { name: "Complété", value: stats.completed },
        { name: "En cours", value: stats.started },
        { name: "Assigné", value: stats.assigned },
    ].filter(d => d.value > 0), [stats]);

    const me = meQ.data;

    return (
        <div style={{ maxWidth: 1200, margin: "0 auto", padding: "32px var(--page-x) 80px" }}>
            <DemoFallbackBanner tenantName={me?.tenantName} />

            <Reveal>
                <div style={{ marginBottom: 28 }}>
                    <h1 style={{ fontSize: 26, fontWeight: 700, color: "var(--text)", letterSpacing: "-0.02em" }}>
                        Bonjour, {me?.firstName ?? "…"}
                    </h1>
                    <p style={{ marginTop: 4, fontSize: 14, color: "var(--text-2)" }}>
                        {me?.tenantName ?? "—"} · votre tableau de bord sécurité
                    </p>
                </div>
            </Reveal>

            <FirstStepsBanner assignments={assignQ.data} />

            {/* KPI row — vraies métriques, gros chiffres animés */}
            <Stagger className="mb-8 grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-4" gap={0.06}>
                {assignQ.isLoading ? (
                    <><SkeletonCard /><SkeletonCard /><SkeletonCard /><SkeletonCard /></>
                ) : (
                    <>
                        <StaggerItem><KPICard value={cri ?? 0} suffix={cri != null ? " /100" : ""} label="Cyber Resilience Index" icon={ShieldCheck} hint={cri == null ? "En attente de données" : criBand(cri)} /></StaggerItem>
                        <StaggerItem><KPICard value={stats.total} label="Parcours en cours" icon={ListChecks} /></StaggerItem>
                        <StaggerItem><KPICard value={stats.completed} label="Challenges complétés" icon={CircleCheckBig} /></StaggerItem>
                        <StaggerItem><KPICard value={stats.avgPct} suffix="%" label="Progression moyenne" icon={TrendingUp} /></StaggerItem>
                    </>
                )}
            </Stagger>

            {/* Graphiques — évolution CRI (dégradé vert→cyan) + répartition parcours */}
            <Reveal>
                <div className="mb-8 grid grid-cols-1 gap-4 lg:grid-cols-3">
                    <div className="lg:col-span-2">
                        <ChartCard title="Évolution du Cyber Resilience Index" subtitle="6 derniers mois">
                            {riskHistoryQ.isLoading
                                ? <div className="skel" style={{ height: 240, borderRadius: 12 }} />
                                : histData.length >= 2
                                    ? <AreaChartCard data={histData} />
                                    : <EmptyChart label="Pas encore assez d'historique pour tracer une courbe." />}
                        </ChartCard>
                    </div>
                    <div className="lg:col-span-1">
                        <ChartCard title="Répartition des parcours" subtitle={`${stats.total} parcours`}>
                            {donutData.length
                                ? <DonutChart data={donutData} centerLabel="parcours" height={240} />
                                : <EmptyChart label="Aucun parcours assigné." />}
                        </ChartCard>
                    </div>
                </div>
            </Reveal>

            {/* Mes parcours */}
            <section style={{ marginBottom: 32 }}>
                <SectionHeader title="Mes parcours" href="/dashboard/parcours" />
                {assignQ.isLoading && <PathListSkeleton />}
                {assignQ.isError && <ErrorBox message={(assignQ.error as Error)?.message} />}
                {assignQ.data?.length === 0 && <EmptyBox text="Aucun parcours assigné pour le moment." />}
                <Stagger className="flex flex-col gap-3" gap={0.05}>
                    {assignQ.data?.map(a => <StaggerItem key={a.pathId}><PathRow assignment={a} /></StaggerItem>)}
                </Stagger>
            </section>

            {/* Activité récente */}
            <section>
                <h2 style={{ fontSize: 18, fontWeight: 600, color: "var(--text)", marginBottom: 16 }}>Activité récente</h2>
                {recentQ.isLoading && <ActivitySkeleton />}
                {recentQ.data?.length === 0 && <EmptyBox text="Aucune activité récente." />}
                <Stagger className="flex flex-col gap-2" gap={0.04}>
                    {recentQ.data?.map(s => <StaggerItem key={s.id}><ActivityRow s={s} /></StaggerItem>)}
                </Stagger>
            </section>
        </div>
    );
}

function SectionHeader({ title, href }: { title: string; href: string }) {
    return (
        <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", marginBottom: 16 }}>
            <h2 style={{ fontSize: 18, fontWeight: 600, color: "var(--text)" }}>{title}</h2>
            <Link href={href} className="transition-colors" style={{ fontSize: 12, color: "var(--accent)", textDecoration: "none" }}>Voir tout →</Link>
        </div>
    );
}

function EmptyChart({ label }: { label: string }) {
    return <div style={{ height: 240, display: "flex", alignItems: "center", justifyContent: "center", textAlign: "center", fontSize: 13, color: "var(--text-3)", padding: 16 }}>{label}</div>;
}
function EmptyBox({ text }: { text: string }) {
    return <div style={{ background: "var(--surface)", border: "1px solid var(--border)", borderRadius: 12, padding: "36px 20px", textAlign: "center", fontSize: 14, color: "var(--text-3)" }}>{text}</div>;
}

function ActivityRow({ s }: { s: RecentSubmission }) {
    return (
        <div className="transition-colors" style={{ display: "flex", alignItems: "center", justifyContent: "space-between", gap: 12, background: "var(--surface)", border: "1px solid var(--border)", borderRadius: 10, padding: "12px 16px" }}
            onMouseEnter={e => (e.currentTarget.style.borderColor = "var(--accent-border)")}
            onMouseLeave={e => (e.currentTarget.style.borderColor = "var(--border)")}>
            <div style={{ display: "flex", alignItems: "center", gap: 10, minWidth: 0 }}>
                <span style={{ flexShrink: 0, display: "inline-flex", width: 26, height: 26, borderRadius: 999, alignItems: "center", justifyContent: "center", background: s.isCorrect ? "var(--success-subtle)" : "var(--danger-subtle)", color: s.isCorrect ? "var(--success-t)" : "var(--danger-t)" }}>
                    {s.isCorrect ? <Check size={15} /> : <X size={15} />}
                </span>
                <span style={{ fontSize: 14, color: "var(--text)", overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap" }}>{s.challengeTitle}</span>
            </div>
            <div style={{ flexShrink: 0, textAlign: "right" }}>
                {s.isCorrect && <span style={{ fontSize: 14, fontWeight: 600, fontFamily: "'JetBrains Mono', monospace", color: "var(--accent)" }}>+{s.scoreAwarded} pts</span>}
                <div style={{ fontSize: 11, fontFamily: "'JetBrains Mono', monospace", color: "var(--text-3)", marginTop: 2 }}>{new Date(s.submittedAt).toLocaleDateString("fr-FR")}</div>
            </div>
        </div>
    );
}

function PathRow({ assignment: a }: { assignment: AssignmentMine }) {
    const STATUS_LABEL: Record<string, string> = { assigned: "Assigné", started: "En cours", completed: "Complété" };
    const level = a.pathLevel ? (LEVEL_LABELS[a.pathLevel] ?? a.pathLevel) : null;
    const pct = Math.max(0, Math.min(100, a.progressPercent));
    return (
        <div className="transition-colors" style={{ background: "var(--surface)", border: "1px solid var(--border)", borderRadius: 12, padding: 20 }}
            onMouseEnter={e => (e.currentTarget.style.borderColor = "var(--accent-border)")}
            onMouseLeave={e => (e.currentTarget.style.borderColor = "var(--border)")}>
            <div style={{ display: "flex", alignItems: "flex-start", justifyContent: "space-between", gap: 16 }}>
                <div style={{ minWidth: 0, flex: 1 }}>
                    <div style={{ display: "flex", flexWrap: "wrap", gap: 6, marginBottom: 6 }}>
                        {level && <span style={{ fontSize: 11, background: "var(--surface-2)", border: "1px solid var(--border)", borderRadius: 4, padding: "2px 8px", color: "var(--text-2)" }}>{level}</span>}
                        <span style={{ fontSize: 11, borderRadius: 999, padding: "2px 10px", fontWeight: 600, background: a.status === "completed" ? "var(--success-subtle)" : a.status === "started" ? "var(--warning-subtle)" : "var(--surface-2)", color: a.status === "completed" ? "var(--success-t)" : a.status === "started" ? "var(--warning-t)" : "var(--text-3)" }}>{STATUS_LABEL[a.status] ?? a.status}</span>
                    </div>
                    <div style={{ fontSize: 15, fontWeight: 600, color: "var(--text)", marginBottom: 10 }}>{a.pathTitle ?? a.pathId}</div>
                    <div style={{ marginBottom: 4 }}>
                        <div style={{ display: "flex", justifyContent: "space-between", fontSize: 11, color: "var(--text-3)", marginBottom: 4 }}>
                            <span>Progression</span>
                            <span style={{ fontFamily: "'JetBrains Mono', monospace", color: "var(--text)" }}>{pct}%</span>
                        </div>
                        <div style={{ height: 6, background: "var(--surface-2)", borderRadius: 999, overflow: "hidden" }}>
                            <div style={{ height: "100%", width: `${pct}%`, background: "linear-gradient(90deg, var(--accent), var(--accent-2))", borderRadius: 999, transition: "width 0.6s cubic-bezier(0.16,1,0.3,1)" }} />
                        </div>
                    </div>
                    {a.dueAt && <div style={{ fontSize: 11, color: "var(--text-3)", marginTop: 4 }}>Échéance : {new Date(a.dueAt).toLocaleDateString("fr-FR")}</div>}
                </div>
                <Link href={`/dashboard/parcours/${a.pathId}`} className="transition-colors" style={{ flexShrink: 0, background: "var(--accent)", color: "var(--on-accent)", fontWeight: 600, fontSize: 12, padding: "9px 16px", borderRadius: 8, textDecoration: "none", minHeight: 40, display: "inline-flex", alignItems: "center" }}
                    onMouseOver={e => (e.currentTarget.style.background = "var(--accent-hover)")}
                    onMouseOut={e => (e.currentTarget.style.background = "var(--accent)")}>
                    Continuer
                </Link>
            </div>
        </div>
    );
}

function SkeletonCard() {
    return <div className="rounded-xl border border-border bg-surface p-6" style={{ height: 96 }}>
        <div className="skel" style={{ width: "40%", height: 12 }} />
        <div className="skel" style={{ width: "55%", height: 28, marginTop: 14 }} />
    </div>;
}
function PathListSkeleton() {
    return <div className="flex flex-col gap-3">{[0, 1].map(i => <div key={i} className="skel" style={{ height: 108, borderRadius: 12 }} />)}</div>;
}
function ActivitySkeleton() {
    return <div className="flex flex-col gap-2">{[0, 1, 2].map(i => <div key={i} className="skel" style={{ height: 50, borderRadius: 10 }} />)}</div>;
}

function FirstStepsBanner({ assignments }: { assignments?: AssignmentMine[] }) {
    const [dismissed, setDismissed] = useState(false);
    useEffect(() => { try { setDismissed(localStorage.getItem("sentys.firstSteps.dismissed") === "1"); } catch { /* noop */ } }, []);
    if (dismissed || !assignments || assignments.length === 0) return null;
    if (assignments.some(a => (a.progressPercent ?? 0) > 0)) return null;
    const first = assignments[0];
    const close = () => { try { localStorage.setItem("sentys.firstSteps.dismissed", "1"); } catch { /* noop */ } setDismissed(true); };
    return (
        <div role="region" aria-label="Premiers pas" style={{ marginBottom: 24, padding: "20px 24px", background: "var(--accent-subtle)", border: "1px solid var(--accent-border)", borderRadius: 12, display: "flex", gap: 16, alignItems: "center", flexWrap: "wrap", position: "relative" }}>
            <div style={{ flex: 1, minWidth: 240 }}>
                <h2 style={{ fontSize: 16, fontWeight: 700, color: "var(--text)", margin: 0 }}>Prêt à commencer votre formation ?</h2>
                <p style={{ fontSize: 13, color: "var(--text-2)", margin: "4px 0 0", lineHeight: 1.5 }}>Vous avez {assignments.length} parcours assigné{assignments.length > 1 ? "s" : ""}. Démarrez avec le premier.</p>
            </div>
            <Link href={`/dashboard/parcours/${first.pathId}`} style={{ background: "var(--accent)", color: "var(--on-accent)", textDecoration: "none", padding: "10px 18px", borderRadius: 8, fontSize: 13, fontWeight: 600, whiteSpace: "nowrap" }}>Commencer ma formation →</Link>
            <button type="button" onClick={close} aria-label="Masquer cet encart" style={{ position: "absolute", top: 8, right: 10, background: "transparent", border: "none", color: "var(--text-3)", cursor: "pointer", fontSize: 16, padding: 4 }}>×</button>
        </div>
    );
}

function ErrorBox({ message }: { message?: string }) {
    return <div style={{ background: "var(--danger-subtle)", border: "1px solid rgba(239,68,68,0.25)", borderRadius: 8, padding: "10px 14px", fontSize: 14, color: "var(--danger-t)" }}>{message || "Erreur de chargement"}</div>;
}

function DemoFallbackBanner({ tenantName }: { tenantName?: string | null }) {
    const [dismissed, setDismissed] = useState(false);
    const [show, setShow] = useState(false);
    useEffect(() => {
        const cookies = document.cookie.split(";").map(c => c.trim());
        const hasFallback = cookies.some(c => c.startsWith("sso_demo_fallback="));
        const wasDismissed = typeof window !== "undefined" && sessionStorage.getItem("demo_banner_dismissed") === "1";
        setShow(hasFallback && !wasDismissed);
    }, []);
    if (!show || dismissed) return null;
    return (
        <div style={{ marginBottom: 20, background: "var(--accent-subtle)", border: "1px solid var(--accent-border)", borderRadius: 10, padding: "14px 18px", display: "flex", alignItems: "center", gap: 14 }}>
            <div style={{ flex: 1, fontSize: 13, color: "var(--text-2)", lineHeight: 1.5 }}>
                <strong style={{ color: "var(--accent)" }}>Bienvenue sur Sentys en mode Démo.</strong>{" "}
                Votre organisation {tenantName ? `(${tenantName})` : ""} n&apos;est pas encore cliente —{" "}
                <a href="mailto:commercial@sentys.local?subject=Acc%C3%A8s%20Sentys%20entreprise" style={{ color: "var(--accent)", textDecoration: "underline" }}>contacter notre équipe commerciale</a>.
            </div>
            <button onClick={() => { setDismissed(true); sessionStorage.setItem("demo_banner_dismissed", "1"); }} style={{ background: "transparent", border: "none", color: "var(--text-3)", cursor: "pointer", fontSize: 18, padding: 4 }} aria-label="Fermer">✕</button>
        </div>
    );
}
