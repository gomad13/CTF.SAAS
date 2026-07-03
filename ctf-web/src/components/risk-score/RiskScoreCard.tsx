"use client";

import { memo, useMemo } from "react";
import { ShieldCheck } from "lucide-react";
import type { RiskLevel, RiskScore } from "@/lib/types/riskScore";
import { RISK_LEVEL_LABELS } from "@/lib/types/riskScore";

// Palette : tokens CLAUDE.md §5.4 (#3B82F6, #10B981, #EF4444, #F59E0B).
// Le prompt CRI mentionnait var(--accent) / var(--surface-2) mais CLAUDE.md (autorité) prescrit
// la palette ci-dessous, et tous les composants existants la respectent.
// Décision documentée dans le rapport final.
const COLOR_DANGER = "#EF4444";
const COLOR_WARNING = "#F59E0B";
const COLOR_SUCCESS = "#10B981";
const COLOR_PRIMARY = "#3B82F6";
const COLOR_TRACK = "#E2E8F0";

// Jauge SVG native — aucune dépendance ajoutée. Cercle de 200×200,
// rayon 80, circumference ≈ 502.
const SIZE = 200;
const STROKE_WIDTH = 14;
const RADIUS = (SIZE - STROKE_WIDTH) / 2;
const CIRCUMFERENCE = 2 * Math.PI * RADIUS;

type Props = {
    data: RiskScore | undefined;
    isLoading: boolean;
    isError: boolean;
};

function getRiskLevel(score: number | null): RiskLevel {
    if (score === null) return "insufficient";
    if (score < 40) return "vulnerable";
    if (score < 60) return "progressing";
    if (score < 80) return "solid";
    return "excellent";
}

function getColor(score: number | null): string {
    if (score === null) return COLOR_TRACK;
    if (score < 40) return COLOR_DANGER;
    if (score < 60) return COLOR_WARNING;
    if (score < 80) return COLOR_PRIMARY;
    return COLOR_SUCCESS;
}

function formatDate(iso: string): string {
    try {
        const d = new Date(iso);
        return d.toLocaleDateString("fr-FR", { day: "2-digit", month: "2-digit", year: "numeric" });
    } catch {
        return "—";
    }
}

function RiskScoreCardImpl({ data, isLoading, isError }: Props) {
    const level = useMemo<RiskLevel>(() => getRiskLevel(data?.score ?? null), [data?.score]);
    const color = useMemo(() => getColor(data?.score ?? null), [data?.score]);

    const score = data?.score ?? null;
    const dashOffset = useMemo(() => {
        if (score === null) return CIRCUMFERENCE; // jauge vide
        const filled = Math.max(0, Math.min(100, score)) / 100;
        return CIRCUMFERENCE * (1 - filled);
    }, [score]);

    return (
        <section
            aria-label="Cyber Resilience Index"
            className="rounded-xl border border-[#E2E8F0] bg-white p-6 shadow-[0_4px_6px_-1px_rgb(0_0_0_/_0.1)]"
        >
            <header className="mb-4 flex items-start justify-between">
                <div>
                    <p className="text-xs font-medium uppercase tracking-wider text-[#64748B]">
                        Cyber Resilience Index
                    </p>
                    <h2 className="mt-1 text-base font-semibold text-[#1E293B]">
                        Votre score de résilience
                    </h2>
                </div>
                <ShieldCheck size={20} strokeWidth={1.5} className="text-[#94A3B8] opacity-60" />
            </header>

            <div className="flex flex-col items-center gap-4">
                {isLoading ? (
                    <Skeleton />
                ) : isError ? (
                    <ErrorState />
                ) : score === null ? (
                    <InsufficientState />
                ) : (
                    <Gauge score={score} color={color} dashOffset={dashOffset} />
                )}

                <div className="text-center">
                    <p className="text-sm font-semibold text-[#334155]">
                        {RISK_LEVEL_LABELS[level]}
                    </p>
                    {data?.computedAt && (
                        <p className="mt-1 text-xs text-[#64748B]">
                            Mis à jour le {formatDate(data.computedAt)}
                        </p>
                    )}
                </div>
            </div>
        </section>
    );
}

function Gauge({ score, color, dashOffset }: { score: number; color: string; dashOffset: number }) {
    return (
        <div className="relative" style={{ width: SIZE, height: SIZE }}>
            <svg
                width={SIZE}
                height={SIZE}
                viewBox={`0 0 ${SIZE} ${SIZE}`}
                role="img"
                aria-label={`Score : ${score} sur 100`}
            >
                <circle
                    cx={SIZE / 2}
                    cy={SIZE / 2}
                    r={RADIUS}
                    fill="none"
                    stroke={COLOR_TRACK}
                    strokeWidth={STROKE_WIDTH}
                />
                <circle
                    cx={SIZE / 2}
                    cy={SIZE / 2}
                    r={RADIUS}
                    fill="none"
                    stroke={color}
                    strokeWidth={STROKE_WIDTH}
                    strokeLinecap="round"
                    strokeDasharray={CIRCUMFERENCE}
                    strokeDashoffset={dashOffset}
                    transform={`rotate(-90 ${SIZE / 2} ${SIZE / 2})`}
                    style={{ transition: "stroke-dashoffset 600ms ease-out, stroke 200ms ease-out" }}
                />
            </svg>
            <div className="absolute inset-0 flex flex-col items-center justify-center">
                <span className="text-5xl font-bold leading-none text-[#1E293B]">{score}</span>
                <span className="mt-1 text-xs font-medium text-[#64748B]">/ 100</span>
            </div>
        </div>
    );
}

function Skeleton() {
    return (
        <div
            className="animate-pulse rounded-full bg-[#F1F5F9]"
            style={{ width: SIZE, height: SIZE }}
            aria-label="Chargement du score"
        />
    );
}

function ErrorState() {
    return (
        <div
            className="flex flex-col items-center justify-center rounded-full border-2 border-dashed border-[#E2E8F0] text-center text-[#64748B]"
            style={{ width: SIZE, height: SIZE }}
        >
            <p className="px-6 text-sm">Score temporairement indisponible</p>
        </div>
    );
}

function InsufficientState() {
    return (
        <div
            className="flex flex-col items-center justify-center rounded-full border-2 border-dashed border-[#E2E8F0] px-6 text-center text-[#64748B]"
            style={{ width: SIZE, height: SIZE }}
        >
            <p className="text-sm font-medium">
                Complète au moins 3 challenges pour afficher ton score
            </p>
        </div>
    );
}

export const RiskScoreCard = memo(RiskScoreCardImpl);
