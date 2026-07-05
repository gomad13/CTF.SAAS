"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { useQuery } from "@tanstack/react-query";
import { Trophy, Zap } from "lucide-react";
import { apiFetch } from "@/lib/api";
import { useCompetitionStatus } from "@/hooks/useCompetitionStatus";
import Reveal from "@/components/Reveal";
import CountUp from "@/components/CountUp";
import Podium from "@/components/competition/Podium";
import ScoreboardTable from "@/components/competition/ScoreboardTable";
import TeamLeaderboard from "@/components/competition/TeamLeaderboard";
import { renderTeamIcon } from "@/components/teams/teamIcons";
import type {
    Podium as PodiumData,
    ScoreboardPage,
    TeamLeaderboardEntry,
    MyRank,
} from "@/components/competition/types";

const PAGE_SIZE = 20;
type Tab = "individual" | "teams";

export default function CompetitionPage() {
    const router = useRouter();
    const statusQ = useCompetitionStatus();
    const [page, setPage] = useState(1);
    const [tab, setTab] = useState<Tab>("individual");

    const enabled = statusQ.data?.isEnabled === true;

    const myRankQ = useQuery<MyRank>({
        queryKey: ["competition", "my-rank"],
        queryFn: () => apiFetch<MyRank>("/api/competition/my-rank"),
        enabled,
        staleTime: 10_000,
    });

    const podiumQ = useQuery<PodiumData>({
        queryKey: ["competition", "podium"],
        queryFn: () => apiFetch<PodiumData>("/api/competition/podium"),
        enabled: enabled && tab === "individual",
        staleTime: 15_000,
    });

    const boardQ = useQuery<ScoreboardPage>({
        queryKey: ["competition", "scoreboard", page],
        queryFn: () =>
            apiFetch<ScoreboardPage>(`/api/competition/scoreboard?page=${page}&pageSize=${PAGE_SIZE}`),
        enabled: enabled && tab === "individual",
        staleTime: 10_000,
    });

    const teamsQ = useQuery<TeamLeaderboardEntry[]>({
        queryKey: ["competition", "teams"],
        queryFn: () => apiFetch<TeamLeaderboardEntry[]>("/api/competition/leaderboard/teams"),
        enabled: enabled && tab === "teams",
        staleTime: 10_000,
    });

    if (statusQ.isLoading) {
        return (
            <div className="mx-auto max-w-5xl px-4 py-8 sm:px-6">
                <div className="h-8 w-40 animate-pulse rounded-lg bg-table-head" />
                <div className="mt-6 h-40 animate-pulse rounded-xl border border-border bg-surface" />
            </div>
        );
    }

    if (!enabled) {
        return (
            <div className="mx-auto max-w-3xl px-4 py-12 text-center sm:px-6">
                <div className="mx-auto flex h-14 w-14 items-center justify-center rounded-full bg-surface/10 text-fg-muted">
                    <Trophy size={22} />
                </div>
                <h1 className="mt-4 text-xl font-bold text-fg-heading">Mode Compétition désactivé</h1>
                <p className="mt-2 text-sm leading-relaxed text-fg-muted">
                    Le mode compétition n&apos;est pas actif pour votre organisation. Demandez à un
                    administrateur de l&apos;activer pour accéder au classement.
                </p>
                <button
                    type="button"
                    onClick={() => router.replace("/dashboard")}
                    className="mt-5 inline-flex min-h-[44px] items-center gap-2 rounded-lg bg-primary px-4 py-2 text-sm font-medium text-white transition-colors duration-200 hover:bg-primary-hover"
                >
                    Retour au tableau de bord
                </button>
            </div>
        );
    }

    const mr = myRankQ.data;

    return (
        <div className="mx-auto flex min-w-0 max-w-5xl flex-col gap-5 px-4 py-8 sm:px-6" style={{ flexShrink: 0 }}>
            <Reveal className="flex flex-wrap items-center justify-between gap-3">
                <div>
                    <h1 className="text-2xl font-bold text-fg-heading">Classement</h1>
                    <p className="mt-1 text-sm text-fg-muted">
                        Compétition par équipes et classement individuel.
                    </p>
                </div>
                <span className="inline-flex items-center gap-2 rounded-full bg-success/10 px-3 py-1 text-xs font-medium text-success">
                    <span className="h-2 w-2 rounded-full bg-success" />
                    Mode Compétition actif
                </span>
            </Reveal>

            {/* Bannière "mon classement" */}
            {mr && (
                <Reveal className="grid grid-cols-1 gap-3 sm:grid-cols-2">
                    <div className="rounded-xl border border-border bg-surface p-4 shadow-sm transition-colors duration-200 hover:border-accent">
                        <p className="text-xs uppercase tracking-wider text-fg-muted">Mon rang individuel</p>
                        <div className="mt-1 flex items-end gap-2">
                            <span className="text-2xl font-bold text-fg-heading">
                                {mr.individualRank ? `#${mr.individualRank}` : "—"}
                            </span>
                            <span className="text-sm text-fg-muted">/ {mr.totalParticipants}</span>
                        </div>
                        <p className="mt-1 text-sm font-semibold text-primary">
                            <CountUp value={mr.individualScore} /> pts
                            {mr.individualSpeedBonus > 0 && (
                                <span className="ml-2 inline-flex items-center gap-1 text-xs font-medium text-warning">
                                    <Zap size={12} /> +{mr.individualSpeedBonus} bonus rapidité
                                </span>
                            )}
                        </p>
                    </div>
                    <div
                        className="rounded-xl border border-border bg-surface p-4 shadow-sm transition-colors duration-200 hover:border-accent"
                        style={mr.teamColor ? { borderColor: mr.teamColor } : undefined}
                    >
                        <p className="text-xs uppercase tracking-wider text-fg-muted">Mon équipe</p>
                        <div className="mt-1 flex items-center gap-2">
                            {mr.teamColor && (
                                <span className="flex h-5 w-5 items-center justify-center rounded text-white" style={{ background: mr.teamColor }} aria-hidden>
                                    {renderTeamIcon(mr.teamIcon, 12)}
                                </span>
                            )}
                            <span className="truncate text-lg font-bold text-fg-heading">
                                {mr.teamName ?? "Aucune équipe"}
                            </span>
                        </div>
                        <p className="mt-1 text-sm font-semibold text-fg-body">
                            {mr.teamRank ? `#${mr.teamRank} / ${mr.totalTeams}` : "—"} · {mr.teamScore} pts
                        </p>
                    </div>
                </Reveal>
            )}

            {/* Onglets */}
            <div className="flex gap-2 rounded-xl border border-border bg-surface p-1">
                {(["individual", "teams"] as Tab[]).map(t => (
                    <button
                        key={t}
                        type="button"
                        onClick={() => setTab(t)}
                        className={`flex-1 rounded-lg px-3 py-2.5 text-sm font-medium transition-colors duration-200 ${
                            tab === t
                                ? "bg-primary text-white"
                                : "text-fg-muted hover:bg-table-head"
                        }`}
                    >
                        {t === "individual" ? "Classement individuel" : "Classement par équipe"}
                    </button>
                ))}
            </div>

            {tab === "individual" ? (
                <>
                    <div className="rounded-xl border border-border bg-surface p-4 shadow-sm sm:p-6">
                        <h2 className="text-sm font-semibold uppercase tracking-wider text-fg-muted">Podium</h2>
                        {podiumQ.isLoading ? (
                            <div className="mt-4 grid grid-cols-3 gap-2 sm:gap-4">
                                {[0, 1, 2].map(i => (
                                    <div key={i} className="h-32 animate-pulse rounded-xl bg-table-head" />
                                ))}
                            </div>
                        ) : (
                            <Podium
                                first={podiumQ.data?.first}
                                second={podiumQ.data?.second}
                                third={podiumQ.data?.third}
                            />
                        )}
                    </div>

                    <ScoreboardTable
                        data={boardQ.data ?? { items: [], page, pageSize: PAGE_SIZE, total: 0 }}
                        isLoading={boardQ.isLoading}
                        onPageChange={p => {
                            setPage(p);
                            window.scrollTo({ top: 0, behavior: "smooth" });
                        }}
                    />
                </>
            ) : (
                <div className="rounded-xl border border-border bg-surface p-4 shadow-sm sm:p-6">
                    <h2 className="mb-4 text-sm font-semibold uppercase tracking-wider text-fg-muted">
                        Classement des équipes
                    </h2>
                    <TeamLeaderboard teams={teamsQ.data ?? []} isLoading={teamsQ.isLoading} />
                    <p className="mt-4 text-xs text-fg-muted">
                        Score d&apos;équipe = somme des scores de ses membres.
                    </p>
                </div>
            )}
        </div>
    );
}
