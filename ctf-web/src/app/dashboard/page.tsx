"use client";
import Link from "next/link";
import { useEffect, useMemo, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { ShieldCheck, ListChecks, CircleCheckBig, TrendingUp, Check, X } from "lucide-react";
import { apiFetch } from "@/lib/api";
import type { Me, AssignmentMine, RecentSubmission } from "@/lib/types";
import { useRiskScore, useRiskScoreHistory } from "@/lib/hooks/useRiskScore";
import Reveal from "@/components/Reveal";
import { Stagger, StaggerItem } from "@/components/Stagger";
import VisionCard from "@/components/vision/VisionCard";
import VisionKpiCard from "@/components/vision/VisionKpiCard";
import VisionAreaChart from "@/components/vision/VisionAreaChart";
import VisionBarChart from "@/components/vision/VisionBarChart";
import VisionGauge from "@/components/vision/VisionGauge";

// ─────────────────────────────────────────────────────────────────────────────
// ÉCRAN TÉMOIN — Dashboard style Vision UI (thème violet SCOPÉ .vision-dashboard).
// Vraies données uniquement (CRI + historique, assignments, submissions). Aucune autre page touchée.
// ─────────────────────────────────────────────────────────────────────────────

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
    // Variation RÉELLE du CRI = dernier mois vs précédent (null si < 2 points d'historique).
    const criDelta = histData.length >= 2 ? histData[histData.length - 1].value - histData[histData.length - 2].value : null;

    const barData = useMemo(() => [
        { label: "Complété", value: stats.completed },
        { label: "En cours", value: stats.started },
        { label: "Assigné", value: stats.assigned },
    ], [stats]);

    const me = meQ.data;

    return (
        <div className="vision-dashboard" style={{ minHeight: "100%" }}>
            <div style={{ maxWidth: 1200, margin: "0 auto", padding: "32px var(--page-x) 80px" }}>
                <DemoFallbackBanner tenantName={me?.tenantName} />

                <Reveal>
                    <div style={{ marginBottom: 28 }}>
                        <h1 style={{ fontSize: 26, fontWeight: 700, color: "var(--v-text)", letterSpacing: "-0.02em" }}>
                            Bonjour, {me?.firstName ?? "…"}
                        </h1>
                        <p style={{ marginTop: 4, fontSize: 14, color: "var(--v-text-2)" }}>
                            {me?.tenantName ?? "—"} · votre tableau de bord sécurité
                        </p>
                    </div>
                </Reveal>

                <FirstStepsBanner assignments={assignQ.data} />

                {/* KPI — vraies métriques, gros chiffres count-up, variation réelle sur le CRI */}
                <Stagger className="mb-6 grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-4" gap={0.06}>
                    {assignQ.isLoading ? (
                        <><VSkeleton h={112} /><VSkeleton h={112} /><VSkeleton h={112} /><VSkeleton h={112} /></>
                    ) : (
                        <>
                            <StaggerItem><VisionKpiCard value={cri ?? 0} suffix={cri != null ? " /100" : ""} label="Cyber Resilience Index" icon={ShieldCheck} delta={criDelta} hint={cri == null ? "En attente de données" : criBand(cri)} /></StaggerItem>
                            <StaggerItem><VisionKpiCard value={stats.total} label="Parcours en cours" icon={ListChecks} hint={`${stats.started} démarré${stats.started > 1 ? "s" : ""}`} /></StaggerItem>
                            <StaggerItem><VisionKpiCard value={stats.completed} label="Challenges complétés" icon={CircleCheckBig} hint={`sur ${stats.total} parcours`} /></StaggerItem>
                            <StaggerItem><VisionKpiCard value={stats.avgPct} suffix="%" label="Progression moyenne" icon={TrendingUp} /></StaggerItem>
                        </>
                    )}
                </Stagger>

                {/* Rangée : aire principale (CRI, dégradé violet→cyan) + jauge de score */}
                <Reveal>
                    <div className="mb-6 grid grid-cols-1 gap-4 lg:grid-cols-3">
                        <div className="lg:col-span-2">
                            <VisionCard>
                                <CardTitle title="Évolution du Cyber Resilience Index" subtitle="6 derniers mois" />
                                {riskHistoryQ.isLoading
                                    ? <VSkeleton h={260} />
                                    : histData.length >= 2
                                        ? <VisionAreaChart data={histData} />
                                        : <EmptyChart label="Pas encore assez d'historique pour tracer une courbe." />}
                            </VisionCard>
                        </div>
                        <div className="lg:col-span-1">
                            <VisionCard>
                                <CardTitle title="Score de résilience" subtitle="niveau actuel" />
                                <div style={{ display: "flex", justifyContent: "center", padding: "12px 0 4px" }}>
                                    <VisionGauge value={cri} band={cri != null ? criBand(cri) : undefined} />
                                </div>
                            </VisionCard>
                        </div>
                    </div>
                </Reveal>

                {/* Rangée : barres (répartition parcours) + activité récente */}
                <Reveal>
                    <div className="mb-8 grid grid-cols-1 gap-4 lg:grid-cols-3">
                        <div className="lg:col-span-1">
                            <VisionCard>
                                <CardTitle title="Répartition des parcours" subtitle={`${stats.total} parcours`} />
                                {stats.total > 0
                                    ? <VisionBarChart data={barData} height={240} />
                                    : <EmptyChart label="Aucun parcours assigné." />}
                            </VisionCard>
                        </div>
                        <div className="lg:col-span-2">
                            <VisionCard>
                                <CardTitle title="Activité récente" subtitle="dernières soumissions" />
                                {recentQ.isLoading && <div className="flex flex-col gap-2">{[0, 1, 2].map(i => <VSkeleton key={i} h={50} />)}</div>}
                                {recentQ.data?.length === 0 && <EmptyBox text="Aucune activité récente." />}
                                <Stagger className="flex flex-col gap-2" gap={0.04}>
                                    {recentQ.data?.map(s => <StaggerItem key={s.id}><ActivityRow s={s} /></StaggerItem>)}
                                </Stagger>
                            </VisionCard>
                        </div>
                    </div>
                </Reveal>

                {/* Mes parcours */}
                <Reveal>
                    <VisionCard>
                        <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", marginBottom: 16 }}>
                            <CardTitle title="Mes parcours" />
                            <Link href="/dashboard/parcours" style={{ fontSize: 12, color: "var(--v-cyan)", textDecoration: "none" }}>Voir tout →</Link>
                        </div>
                        {assignQ.isLoading && <div className="flex flex-col gap-3">{[0, 1].map(i => <VSkeleton key={i} h={108} />)}</div>}
                        {assignQ.isError && <ErrorBox message={(assignQ.error as Error)?.message} />}
                        {assignQ.data?.length === 0 && <EmptyBox text="Aucun parcours assigné pour le moment." />}
                        <Stagger className="flex flex-col gap-3" gap={0.05}>
                            {assignQ.data?.map(a => <StaggerItem key={a.pathId}><PathRow assignment={a} /></StaggerItem>)}
                        </Stagger>
                    </VisionCard>
                </Reveal>
            </div>
        </div>
    );
}

function CardTitle({ title, subtitle }: { title: string; subtitle?: string }) {
    return (
        <div style={{ marginBottom: 8 }}>
            <h2 style={{ fontSize: 16, fontWeight: 600, color: "var(--v-text)" }}>{title}</h2>
            {subtitle && <p style={{ fontSize: 12, color: "var(--v-text-3)", marginTop: 2 }}>{subtitle}</p>}
        </div>
    );
}

function VSkeleton({ h }: { h: number }) {
    return <div style={{ height: h, borderRadius: 16, background: "var(--v-surface-2)", opacity: 0.6 }} />;
}

function EmptyChart({ label }: { label: string }) {
    return <div style={{ height: 220, display: "flex", alignItems: "center", justifyContent: "center", textAlign: "center", fontSize: 13, color: "var(--v-text-3)", padding: 16 }}>{label}</div>;
}
function EmptyBox({ text }: { text: string }) {
    return <div style={{ background: "var(--v-surface-2)", border: "1px solid var(--v-border)", borderRadius: 12, padding: "32px 20px", textAlign: "center", fontSize: 14, color: "var(--v-text-3)" }}>{text}</div>;
}

function ActivityRow({ s }: { s: RecentSubmission }) {
    return (
        <div className="v-hover" style={{ display: "flex", alignItems: "center", justifyContent: "space-between", gap: 12, background: "var(--v-surface-2)", border: "1px solid var(--v-border)", borderRadius: 12, padding: "12px 16px" }}>
            <div style={{ display: "flex", alignItems: "center", gap: 10, minWidth: 0 }}>
                <span style={{ flexShrink: 0, display: "inline-flex", width: 28, height: 28, borderRadius: 999, alignItems: "center", justifyContent: "center", background: s.isCorrect ? "color-mix(in srgb, var(--v-success) 18%, transparent)" : "color-mix(in srgb, var(--v-danger) 18%, transparent)", color: s.isCorrect ? "var(--v-success)" : "var(--v-danger)" }}>
                    {s.isCorrect ? <Check size={15} /> : <X size={15} />}
                </span>
                <span style={{ fontSize: 14, color: "var(--v-text)", overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap" }}>{s.challengeTitle}</span>
            </div>
            <div style={{ flexShrink: 0, textAlign: "right" }}>
                {s.isCorrect && <span style={{ fontSize: 14, fontWeight: 600, color: "var(--v-cyan)" }}>+{s.scoreAwarded} pts</span>}
                <div style={{ fontSize: 11, color: "var(--v-text-3)", marginTop: 2 }}>{new Date(s.submittedAt).toLocaleDateString("fr-FR")}</div>
            </div>
        </div>
    );
}

function PathRow({ assignment: a }: { assignment: AssignmentMine }) {
    const STATUS_LABEL: Record<string, string> = { assigned: "Assigné", started: "En cours", completed: "Complété" };
    const level = a.pathLevel ? (LEVEL_LABELS[a.pathLevel] ?? a.pathLevel) : null;
    const pct = Math.max(0, Math.min(100, a.progressPercent));
    const statusBg = a.status === "completed"
        ? "color-mix(in srgb, var(--v-success) 16%, transparent)"
        : a.status === "started"
            ? "color-mix(in srgb, var(--v-cyan) 16%, transparent)"
            : "var(--v-surface)";
    const statusColor = a.status === "completed" ? "var(--v-success)" : a.status === "started" ? "var(--v-cyan)" : "var(--v-text-3)";
    return (
        <div className="v-hover" style={{ background: "var(--v-surface-2)", border: "1px solid var(--v-border)", borderRadius: 16, padding: 20 }}>
            <div style={{ display: "flex", alignItems: "flex-start", justifyContent: "space-between", gap: 16 }}>
                <div style={{ minWidth: 0, flex: 1 }}>
                    <div style={{ display: "flex", flexWrap: "wrap", gap: 6, marginBottom: 6 }}>
                        {level && <span style={{ fontSize: 11, background: "var(--v-surface)", border: "1px solid var(--v-border)", borderRadius: 6, padding: "2px 8px", color: "var(--v-text-2)" }}>{level}</span>}
                        <span style={{ fontSize: 11, borderRadius: 999, padding: "2px 10px", fontWeight: 600, background: statusBg, color: statusColor }}>{STATUS_LABEL[a.status] ?? a.status}</span>
                    </div>
                    <div style={{ fontSize: 15, fontWeight: 600, color: "var(--v-text)", marginBottom: 10 }}>{a.pathTitle ?? a.pathId}</div>
                    <div style={{ marginBottom: 4 }}>
                        <div style={{ display: "flex", justifyContent: "space-between", fontSize: 11, color: "var(--v-text-3)", marginBottom: 4 }}>
                            <span>Progression</span>
                            <span style={{ color: "var(--v-text)" }}>{pct}%</span>
                        </div>
                        <div style={{ height: 8, background: "var(--v-surface)", borderRadius: 999, overflow: "hidden" }}>
                            <div style={{ height: "100%", width: `${pct}%`, background: "linear-gradient(90deg, var(--v-accent), var(--v-cyan))", borderRadius: 999, transition: "width 0.6s cubic-bezier(0.16,1,0.3,1)" }} />
                        </div>
                    </div>
                    {a.dueAt && <div style={{ fontSize: 11, color: "var(--v-text-3)", marginTop: 6 }}>Échéance : {new Date(a.dueAt).toLocaleDateString("fr-FR")}</div>}
                </div>
                <Link href={`/dashboard/parcours/${a.pathId}`} style={{ flexShrink: 0, background: "var(--v-grad)", color: "var(--v-text)", fontWeight: 600, fontSize: 12, padding: "9px 16px", borderRadius: 10, textDecoration: "none", minHeight: 40, display: "inline-flex", alignItems: "center", boxShadow: "0 6px 16px color-mix(in srgb, var(--v-accent) 40%, transparent)" }}>
                    Continuer
                </Link>
            </div>
        </div>
    );
}

function FirstStepsBanner({ assignments }: { assignments?: AssignmentMine[] }) {
    const [dismissed, setDismissed] = useState(false);
    useEffect(() => { try { setDismissed(localStorage.getItem("sentys.firstSteps.dismissed") === "1"); } catch { /* noop */ } }, []);
    if (dismissed || !assignments || assignments.length === 0) return null;
    if (assignments.some(a => (a.progressPercent ?? 0) > 0)) return null;
    const first = assignments[0];
    const close = () => { try { localStorage.setItem("sentys.firstSteps.dismissed", "1"); } catch { /* noop */ } setDismissed(true); };
    return (
        <div role="region" aria-label="Premiers pas" style={{ marginBottom: 24, padding: "20px 24px", background: "color-mix(in srgb, var(--v-accent) 14%, transparent)", border: "1px solid color-mix(in srgb, var(--v-accent) 40%, transparent)", borderRadius: 16, display: "flex", gap: 16, alignItems: "center", flexWrap: "wrap", position: "relative" }}>
            <div style={{ flex: 1, minWidth: 240 }}>
                <h2 style={{ fontSize: 16, fontWeight: 700, color: "var(--v-text)", margin: 0 }}>Prêt à commencer votre formation ?</h2>
                <p style={{ fontSize: 13, color: "var(--v-text-2)", margin: "4px 0 0", lineHeight: 1.5 }}>Vous avez {assignments.length} parcours assigné{assignments.length > 1 ? "s" : ""}. Démarrez avec le premier.</p>
            </div>
            <Link href={`/dashboard/parcours/${first.pathId}`} style={{ background: "var(--v-grad)", color: "var(--v-text)", textDecoration: "none", padding: "10px 18px", borderRadius: 10, fontSize: 13, fontWeight: 600, whiteSpace: "nowrap" }}>Commencer ma formation →</Link>
            <button type="button" onClick={close} aria-label="Masquer cet encart" style={{ position: "absolute", top: 8, right: 10, background: "transparent", border: "none", color: "var(--v-text-3)", cursor: "pointer", fontSize: 16, padding: 4 }}>×</button>
        </div>
    );
}

function ErrorBox({ message }: { message?: string }) {
    return <div style={{ background: "color-mix(in srgb, var(--v-danger) 14%, transparent)", border: "1px solid color-mix(in srgb, var(--v-danger) 35%, transparent)", borderRadius: 10, padding: "10px 14px", fontSize: 14, color: "var(--v-danger)" }}>{message || "Erreur de chargement"}</div>;
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
        <div style={{ marginBottom: 20, background: "color-mix(in srgb, var(--v-accent) 14%, transparent)", border: "1px solid color-mix(in srgb, var(--v-accent) 40%, transparent)", borderRadius: 12, padding: "14px 18px", display: "flex", alignItems: "center", gap: 14 }}>
            <div style={{ flex: 1, fontSize: 13, color: "var(--v-text-2)", lineHeight: 1.5 }}>
                <strong style={{ color: "var(--v-cyan)" }}>Bienvenue sur Sentys en mode Démo.</strong>{" "}
                Votre organisation {tenantName ? `(${tenantName})` : ""} n&apos;est pas encore cliente —{" "}
                <a href="mailto:commercial@sentys.local?subject=Acc%C3%A8s%20Sentys%20entreprise" style={{ color: "var(--v-cyan)", textDecoration: "underline" }}>contacter notre équipe commerciale</a>.
            </div>
            <button onClick={() => { setDismissed(true); sessionStorage.setItem("demo_banner_dismissed", "1"); }} style={{ background: "transparent", border: "none", color: "var(--v-text-3)", cursor: "pointer", fontSize: 18, padding: 4 }} aria-label="Fermer">✕</button>
        </div>
    );
}
