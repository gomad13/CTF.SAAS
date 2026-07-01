"use client";

import { Users, Crown } from "lucide-react";
import type { TeamLeaderboardEntry } from "./types";
import { renderTeamIcon } from "@/components/teams/teamIcons";

const MEDAL = ["#FACC15", "#CBD5E1", "#D97706"]; // or, argent, bronze

export default function TeamLeaderboard({
    teams,
    isLoading,
}: {
    teams: TeamLeaderboardEntry[];
    isLoading: boolean;
}) {
    if (isLoading) {
        return (
            <div className="flex flex-col gap-3">
                {[0, 1, 2, 3, 4].map(i => (
                    <div key={i} className="h-16 animate-pulse rounded-xl bg-table-head" />
                ))}
            </div>
        );
    }

    if (teams.length === 0) {
        return (
            <p className="rounded-xl border border-border bg-surface p-6 text-center text-sm text-fg-muted">
                Aucune équipe pour le moment.
            </p>
        );
    }

    return (
        <div className="flex flex-col gap-2.5">
            {teams.map(t => {
                const isPodium = t.rank <= 3;
                const accent = t.color || "#3B82F6";
                return (
                    <div
                        key={t.teamId}
                        className="flex items-center gap-3 rounded-xl border bg-surface p-3.5 shadow-sm transition-colors"
                        style={{
                            borderColor: t.isCurrentUserTeam ? accent : "var(--border)",
                            background: t.isCurrentUserTeam ? `${accent}14` : "var(--bg-surface)",
                            borderWidth: t.isCurrentUserTeam ? 2 : 1,
                        }}
                    >
                        {/* Rang */}
                        <div
                            className="flex h-9 w-9 flex-shrink-0 items-center justify-center rounded-full text-sm font-bold"
                            style={{
                                background: isPodium ? MEDAL[t.rank - 1] : "var(--bg-muted)",
                                color: isPodium ? "#1E293B" : "var(--fg-body)",
                            }}
                        >
                            {isPodium ? <Crown size={16} /> : t.rank}
                        </div>

                        {/* Pastille couleur + icône équipe */}
                        <span
                            className="flex h-8 w-8 flex-shrink-0 items-center justify-center rounded-lg text-white"
                            style={{ background: accent }}
                            aria-hidden
                        >
                            {renderTeamIcon(t.icon, 16)}
                        </span>

                        {/* Nom + membres */}
                        <div className="min-w-0 flex-1">
                            <div className="flex items-center gap-2">
                                <p className="truncate text-sm font-semibold text-fg-heading">{t.name}</p>
                                {t.isCurrentUserTeam && (
                                    <span className="flex-shrink-0 rounded-full bg-primary/10 px-2 py-0.5 text-[10px] font-semibold text-primary">
                                        Mon équipe
                                    </span>
                                )}
                            </div>
                            <p className="mt-0.5 flex items-center gap-1 text-xs text-fg-muted">
                                <Users size={12} />
                                {t.memberCount} membre{t.memberCount > 1 ? "s" : ""}
                            </p>
                        </div>

                        {/* Score */}
                        <div className="flex-shrink-0 text-right">
                            <p className="text-lg font-bold leading-none" style={{ color: accent }}>
                                {t.score}
                            </p>
                            <p className="mt-1 text-[10px] uppercase tracking-wider text-fg-muted">pts</p>
                        </div>
                    </div>
                );
            })}
        </div>
    );
}
