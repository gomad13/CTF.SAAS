"use client";

import { memo, useMemo } from "react";
import { TrendingUp } from "lucide-react";
import {
    Area,
    AreaChart,
    CartesianGrid,
    ResponsiveContainer,
    Tooltip,
    XAxis,
    YAxis,
} from "recharts";
import type { RiskScoreHistoryPoint } from "@/lib/types/riskScore";

// Recharts est déjà installé dans le projet (cf. package.json). Pas de
// nouvelle dépendance ajoutée. Ces littéraux hex sont passés en attributs SVG
// (stroke/fill/tick) de recharts qui NE résolvent PAS var(--x) : on garde donc
// des hex reflétant les tokens de la charte (accent vert cyber, grille/texte neutres).
const COLOR_PRIMARY = "#22C55E"; // accent vert cyber (ex-#3B82F6 bleu, interdit par la charte)
const COLOR_GRID = "#E2E8F0"; // grille SVG neutre (ne suit pas le thème)
const COLOR_TEXT_MUTED = "#64748B"; // texte des axes SVG (ne suit pas le thème)

type Props = {
    data: RiskScoreHistoryPoint[] | undefined;
    isLoading: boolean;
    isError: boolean;
};

type ChartPoint = { dateLabel: string; rawDate: string; score: number };

function formatShort(iso: string): string {
    try {
        const d = new Date(iso);
        return d.toLocaleDateString("fr-FR", { day: "2-digit", month: "short" });
    } catch {
        return "—";
    }
}

function formatLong(iso: string): string {
    try {
        const d = new Date(iso);
        return d.toLocaleDateString("fr-FR", { day: "2-digit", month: "long", year: "numeric" });
    } catch {
        return "—";
    }
}

// Recharts ne fournit pas de type stable pour TooltipProps en v3.7,
// on définit une forme minimale pour rester strictement typé sans `any`.
type RechartsTooltipPayload = { payload?: unknown };
type RechartsTooltipProps = {
    active?: boolean;
    payload?: RechartsTooltipPayload[];
};

function CustomTooltip({ active, payload }: RechartsTooltipProps) {
    if (!active || !payload || payload.length === 0) return null;
    const inner = payload[0].payload;
    if (!inner || typeof inner !== "object") return null;
    const point = inner as ChartPoint;
    if (typeof point.score !== "number" || typeof point.rawDate !== "string") return null;
    return (
        <div className="rounded-lg border border-border bg-surface px-3 py-2 shadow-sm">
            <p className="text-xs font-medium text-fg-muted">{formatLong(point.rawDate)}</p>
            <p className="mt-1 text-sm font-semibold text-fg-heading">Score : {point.score} / 100</p>
        </div>
    );
}

function RiskScoreEvolutionChartImpl({ data, isLoading, isError }: Props) {
    const points = useMemo<ChartPoint[]>(() => {
        if (!data) return [];
        // On garde uniquement les points avec un score réel (non nul)
        // pour ne pas tracer des trous dans la courbe.
        return data
            .filter((p): p is RiskScoreHistoryPoint & { score: number } => p.score !== null)
            .map((p) => ({
                dateLabel: formatShort(p.date),
                rawDate: p.date,
                score: p.score,
            }));
    }, [data]);

    return (
        <section
            aria-label="Évolution du Cyber Resilience Index"
            className="rounded-xl border border-border bg-surface p-6 shadow-[0_4px_6px_-1px_rgb(0_0_0_/_0.1)]"
        >
            <header className="mb-4 flex items-start justify-between">
                <div>
                    <p className="text-xs font-medium uppercase tracking-wider text-fg-muted">
                        Évolution
                    </p>
                    <h2 className="mt-1 text-base font-semibold text-fg-heading">
                        Votre score sur les 6 derniers mois
                    </h2>
                </div>
                <TrendingUp size={20} strokeWidth={1.5} className="text-fg-muted opacity-60" />
            </header>

            {isLoading ? (
                <ChartSkeleton />
            ) : isError ? (
                <ChartEmpty label="Historique temporairement indisponible" />
            ) : points.length < 2 ? (
                <ChartEmpty label="Pas encore assez d'historique pour afficher l'évolution" />
            ) : (
                <div className="h-64 w-full">
                    <ResponsiveContainer width="100%" height="100%">
                        <AreaChart data={points} margin={{ top: 5, right: 10, left: -10, bottom: 0 }}>
                            <defs>
                                <linearGradient id="cri-gradient" x1="0" y1="0" x2="0" y2="1">
                                    <stop offset="0%" stopColor={COLOR_PRIMARY} stopOpacity={0.35} />
                                    <stop offset="100%" stopColor={COLOR_PRIMARY} stopOpacity={0} />
                                </linearGradient>
                            </defs>
                            <CartesianGrid stroke={COLOR_GRID} strokeDasharray="3 3" vertical={false} />
                            <XAxis
                                dataKey="dateLabel"
                                tick={{ fill: COLOR_TEXT_MUTED, fontSize: 12 }}
                                axisLine={{ stroke: COLOR_GRID }}
                                tickLine={false}
                            />
                            <YAxis
                                domain={[0, 100]}
                                ticks={[0, 25, 50, 75, 100]}
                                tick={{ fill: COLOR_TEXT_MUTED, fontSize: 12 }}
                                axisLine={{ stroke: COLOR_GRID }}
                                tickLine={false}
                            />
                            <Tooltip content={<CustomTooltip />} cursor={{ stroke: COLOR_GRID }} />
                            <Area
                                type="monotone"
                                dataKey="score"
                                stroke={COLOR_PRIMARY}
                                strokeWidth={2}
                                fill="url(#cri-gradient)"
                                isAnimationActive
                            />
                        </AreaChart>
                    </ResponsiveContainer>
                </div>
            )}
        </section>
    );
}

function ChartSkeleton() {
    return <div className="h-64 w-full animate-pulse rounded-lg bg-surface-2" />;
}

function ChartEmpty({ label }: { label: string }) {
    return (
        <div className="flex h-64 w-full flex-col items-center justify-center rounded-lg border-2 border-dashed border-border text-center text-fg-muted">
            <p className="px-6 text-sm">{label}</p>
        </div>
    );
}

export const RiskScoreEvolutionChart = memo(RiskScoreEvolutionChartImpl);
