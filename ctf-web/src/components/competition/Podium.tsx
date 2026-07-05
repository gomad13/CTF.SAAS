"use client";

import { Crown, Medal } from "lucide-react";
import type { ScoreboardEntry } from "./types";

type Props = {
    first?: ScoreboardEntry | null;
    second?: ScoreboardEntry | null;
    third?: ScoreboardEntry | null;
};

// Couleurs de médailles (or / argent / bronze) : couleurs IDENTITAIRES du podium,
// volontairement conservées en hex (elles ne suivent pas le thème sombre/clair).
const TIER_STYLES = {
    1: { border: "#F59E0B", accent: "#F59E0B", label: "1er", IconElem: Crown, minH: 190 },
    2: { border: "#94A3B8", accent: "#64748B", label: "2e", IconElem: Medal, minH: 168 },
    3: { border: "#CD7F32", accent: "#CD7F32", label: "3e", IconElem: Medal, minH: 152 },
} as const;

function PodiumCard({ entry, tier }: { entry?: ScoreboardEntry | null; tier: 1 | 2 | 3 }) {
    const cfg = TIER_STYLES[tier];
    const Icon = cfg.IconElem;

    if (!entry) {
        return (
            <div
                className="flex flex-col items-center justify-center rounded-xl border border-dashed border-border bg-surface/50 px-2 py-6 text-center text-xs text-fg-muted"
                style={{ minHeight: cfg.minH }}
            >
                Pas de {cfg.label}
            </div>
        );
    }

    const isCurrent = entry.isCurrentUser;

    return (
        <div
            className="relative flex flex-col items-center gap-1 rounded-xl bg-surface px-2 pt-7 pb-4 text-center shadow-sm transition-transform duration-200 hover:-translate-y-0.5 sm:px-3"
            style={{
                minHeight: cfg.minH,
                border: `2px solid ${cfg.border}`,
                boxShadow: isCurrent
                    ? "0 0 0 3px color-mix(in srgb, var(--accent) 20%, transparent)"
                    : undefined,
            }}
        >
            {/* Couronne / médaille : badge décoratif au-dessus de la carte (ne chevauche plus le contenu) */}
            <div
                className="absolute -top-3.5 left-1/2 flex h-7 w-7 -translate-x-1/2 items-center justify-center rounded-full shadow"
                style={{ background: cfg.accent, color: "var(--on-accent)" }}
            >
                <Icon size={15} strokeWidth={2.5} />
            </div>

            {/* Avatar (initiales) */}
            <div className="flex h-11 w-11 flex-shrink-0 items-center justify-center rounded-full bg-primary text-xs font-bold text-white shadow-sm sm:h-14 sm:w-14 sm:text-sm">
                {entry.initials}
            </div>

            {/* Nom — toujours entièrement visible (tronqué si trop long, jamais masqué) */}
            <div
                className="w-full truncate text-xs font-semibold text-fg-heading sm:text-sm"
                title={entry.displayName}
            >
                {entry.displayName}
            </div>

            <div className="text-sm font-bold sm:text-base" style={{ color: cfg.accent }}>
                {entry.score.toLocaleString("fr-FR")} pts
            </div>

            <div className="text-[10px] leading-tight text-fg-muted sm:text-[11px]">
                {entry.challengesCompleted} challenge{entry.challengesCompleted > 1 ? "s" : ""}
            </div>

            {isCurrent && (
                <span className="rounded-full bg-primary/10 px-2 py-0.5 text-[9px] font-medium uppercase tracking-wider text-primary sm:text-[10px]">
                    C&apos;est toi
                </span>
            )}
        </div>
    );
}

export default function Podium({ first, second, third }: Props) {
    return (
        <div className="grid grid-cols-3 items-end gap-2 pt-6 sm:gap-4">
            <PodiumCard entry={second} tier={2} />
            <PodiumCard entry={first} tier={1} />
            <PodiumCard entry={third} tier={3} />
        </div>
    );
}
