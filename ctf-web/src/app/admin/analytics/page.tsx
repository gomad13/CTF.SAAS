"use client";

import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { BarChart3, TrendingUp, Users2, CheckCircle2 } from "lucide-react";
import { apiFetch } from "@/lib/api";
import Reveal from "@/components/Reveal";
import { Stagger, StaggerItem } from "@/components/Stagger";
import CountUp from "@/components/CountUp";

type Kpis = {
    activeUsers7d: number;
    activeUsers30d: number;
    totalCompletions: number;
    averageScore: number;
    averageCompletionPercent: number;
};
type ActivityPoint = { date: string; completions: number };
type PathStat = { pathId: string; title: string; completions: number; averageScore: number };
type TypeStat = { type: string; completions: number };
type Overview = {
    kpis: Kpis;
    activity: ActivityPoint[];
    byPath: PathStat[];
    byType: TypeStat[];
};

const TYPE_LABELS: Record<string, string> = {
    multichoice: "Multichoice",
    ceo_fraud: "CEO Fraud",
    mailbox: "Mailbox",
    password_quiz: "Password Quiz",
    phishing_ai: "Phishing AI",
    free_text: "Texte libre",
};

export default function AnalyticsPage() {
    const [days, setDays] = useState(30);
    const statusQ = useQuery<{ isEnabled: boolean }>({
        queryKey: ["analytics", "status"],
        queryFn: () => apiFetch("/api/analytics/status"),
        staleTime: 30_000,
    });
    const overviewQ = useQuery<Overview>({
        queryKey: ["analytics", "overview", days],
        queryFn: () => apiFetch<Overview>(`/api/analytics/overview?days=${days}`),
        enabled: statusQ.data?.isEnabled === true,
        staleTime: 60_000,
    });

    if (statusQ.isLoading) {
        return <div className="mx-auto max-w-6xl px-6 py-8"><div className="h-40 animate-pulse rounded-xl bg-surface/5" /></div>;
    }
    if (!statusQ.data?.isEnabled) {
        return (
            <div className="mx-auto max-w-3xl px-6 py-12 text-center">
                <div className="mx-auto flex h-14 w-14 items-center justify-center rounded-full bg-surface/10 text-fg-muted">
                    <BarChart3 size={22} />
                </div>
                <h1 className="mt-4 text-xl font-bold text-fg-heading">Analytics désactivés</h1>
                <p className="mt-2 text-sm text-fg-muted">Activez le mode « Analytics avancés » depuis les paramètres d&apos;administration.</p>
            </div>
        );
    }

    const o = overviewQ.data;
    const maxActivity = Math.max(1, ...(o?.activity?.map(a => a.completions) ?? [0]));

    return (
        <Reveal className="mx-auto flex max-w-6xl flex-col gap-6 px-4 py-6 sm:px-6 sm:py-8">
            <div className="flex flex-wrap items-center justify-between gap-3">
                <div>
                    <h1 className="text-2xl font-bold text-fg-heading">Analytics</h1>
                    <p className="mt-1 text-sm text-fg-muted">Usage détaillé de la plateforme par votre organisation.</p>
                </div>
                <select
                    value={days}
                    onChange={e => setDays(parseInt(e.target.value, 10))}
                    className="rounded-lg border border-border bg-surface px-3 py-2 text-sm text-fg-body transition-colors duration-200"
                >
                    <option value={7}>7 jours</option>
                    <option value={30}>30 jours</option>
                    <option value={90}>90 jours</option>
                </select>
            </div>

            <Stagger className="grid grid-cols-2 gap-4 md:grid-cols-4">
                <StaggerItem><Kpi label="Actifs 7j" value={o ? <CountUp value={o.kpis.activeUsers7d} /> : "—"} icon={<Users2 size={16} />} /></StaggerItem>
                <StaggerItem><Kpi label="Actifs 30j" value={o ? <CountUp value={o.kpis.activeUsers30d} /> : "—"} icon={<Users2 size={16} />} /></StaggerItem>
                <StaggerItem><Kpi label="Complétions" value={o ? <CountUp value={o.kpis.totalCompletions} /> : "—"} icon={<CheckCircle2 size={16} />} /></StaggerItem>
                <StaggerItem><Kpi label="Score moyen" value={o?.kpis.averageScore ? <CountUp value={o.kpis.averageScore} suffix="%" /> : "—"} icon={<TrendingUp size={16} />} /></StaggerItem>
            </Stagger>

            <section className="rounded-xl border border-border bg-surface p-6 shadow-sm">
                <h2 className="text-sm font-semibold uppercase tracking-wider text-fg-muted">Activité ({days} jours)</h2>
                <div className="mt-4 flex h-40 items-end gap-1">
                    {(o?.activity ?? []).map((p, i) => (
                        <div key={i} className="flex-1 rounded-t bg-primary transition-colors duration-200"
                            style={{ height: `${Math.max(4, (p.completions / maxActivity) * 100)}%` }}
                            title={`${new Date(p.date).toLocaleDateString("fr-FR")} — ${p.completions}`}
                        />
                    ))}
                    {(!o || o.activity.length === 0) && (
                        <p className="m-auto text-sm text-fg-muted">Aucune donnée sur la période.</p>
                    )}
                </div>
            </section>

            <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
                <section className="rounded-xl border border-border bg-surface p-6 shadow-sm">
                    <h2 className="text-sm font-semibold uppercase tracking-wider text-fg-muted">Complétions par parcours</h2>
                    <ul className="mt-4 flex flex-col gap-3">
                        {(o?.byPath ?? []).map(p => (
                            <li key={p.pathId} className="flex items-center justify-between text-sm transition-colors hover:bg-surface-2">
                                <span className="truncate text-fg-heading">{p.title}</span>
                                <span className="ml-3 shrink-0 font-semibold text-primary">{p.completions}</span>
                            </li>
                        ))}
                        {(o?.byPath?.length ?? 0) === 0 && <li className="text-sm text-fg-muted">Aucune donnée.</li>}
                    </ul>
                </section>

                <section className="rounded-xl border border-border bg-surface p-6 shadow-sm">
                    <h2 className="text-sm font-semibold uppercase tracking-wider text-fg-muted">Répartition par type</h2>
                    <ul className="mt-4 flex flex-col gap-3">
                        {(o?.byType ?? []).map(t => (
                            <li key={t.type} className="flex items-center justify-between text-sm transition-colors hover:bg-surface-2">
                                <span className="text-fg-heading">{TYPE_LABELS[t.type] ?? t.type}</span>
                                <span className="ml-3 shrink-0 font-semibold text-primary">{t.completions}</span>
                            </li>
                        ))}
                        {(o?.byType?.length ?? 0) === 0 && <li className="text-sm text-fg-muted">Aucune donnée.</li>}
                    </ul>
                </section>
            </div>
        </Reveal>
    );
}

function Kpi({ label, value, icon }: { label: string; value: React.ReactNode; icon: React.ReactNode }) {
    return (
        <div className="rounded-xl border border-border bg-surface p-6 shadow-sm">
            <div className="flex items-center justify-between gap-2">
                <span className="text-xs font-medium uppercase tracking-wider text-fg-muted">{label}</span>
                <span className="text-fg-muted" aria-hidden>{icon}</span>
            </div>
            <div className="mt-2 text-2xl font-bold text-fg-heading">{value}</div>
        </div>
    );
}
