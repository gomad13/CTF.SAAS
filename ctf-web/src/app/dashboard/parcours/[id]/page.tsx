"use client";

import { use, useMemo } from "react";
import { useRouter } from "next/navigation";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { CheckCircle2, Play, RotateCcw } from "lucide-react";
import { apiFetch } from "@/lib/api";
import ParcoursHeader from "@/components/paths/ParcoursHeader";
import ModuleCard from "@/components/paths/ModuleCard";
import type { AssignmentMine, PathDetail } from "@/lib/types";

type PathProgress = {
    pathId: string;
    totalChallenges: number;
    completedChallengesCount: number;
    percent: number;
    completedChallengeIds: string[];
    modules: Array<{ moduleId: string; title: string; sortOrder: number; total: number; completed: number; percent: number }>;
};

const LEVEL_LABELS: Record<string, string> = {
    beginner: "Débutant",
    intermediate: "Intermédiaire",
    advanced: "Avancé",
    expert: "Expert",
};

export default function ParcoursDetail({ params }: { params: Promise<{ id: string }> }) {
    const { id: pathId } = use(params);
    const qc = useQueryClient();
    const router = useRouter();

    const pathQ = useQuery<PathDetail>({
        queryKey: ["path", pathId],
        queryFn: () => apiFetch<PathDetail>(`/api/paths/${pathId}`),
    });

    const assignQ = useQuery<AssignmentMine[]>({
        queryKey: ["assignments", "mine"],
        queryFn: () => apiFetch<AssignmentMine[]>("/api/assignments/mine"),
        staleTime: 30_000,
    });

    const progressQ = useQuery<PathProgress>({
        queryKey: ["path", pathId, "progress"],
        queryFn: () => apiFetch<PathProgress>(`/api/paths/${pathId}/progress`),
        staleTime: 10_000,
    });

    const completedIds = useMemo(() => {
        return new Set<string>(progressQ.data?.completedChallengeIds ?? []);
    }, [progressQ.data]);

    const myAssignment = assignQ.data?.find(a => a.pathId === pathId);

    const startM = useMutation({
        mutationFn: () => apiFetch<void>(`/api/assignments/${pathId}/start`, { method: "POST" }),
        onSuccess: () => qc.invalidateQueries({ queryKey: ["assignments", "mine"] }),
    });

    const completeM = useMutation({
        mutationFn: () => apiFetch<void>(`/api/assignments/${pathId}/complete`, { method: "POST" }),
        onSuccess: () => qc.invalidateQueries({ queryKey: ["assignments", "mine"] }),
    });

    const { allChallenges, completedCount, firstUncompleted, totalCount } = useMemo(() => {
        const all = (pathQ.data?.modules ?? []).flatMap(m => m.challenges);
        const first = all.find(c => !completedIds.has(c.id));
        // Source de vérité : back-end. Fallback client uniquement si la query n'a pas encore chargé.
        const count = progressQ.data?.completedChallengesCount
            ?? all.filter(c => completedIds.has(c.id)).length;
        const total = progressQ.data?.totalChallenges ?? all.length;
        return { allChallenges: all, completedCount: count, firstUncompleted: first, totalCount: total };
    }, [pathQ.data, completedIds, progressQ.data]);

    if (pathQ.isLoading) {
        return (
            <div className="mx-auto min-w-0 max-w-5xl px-6 py-8" style={{ flexShrink: 0 }}>
                <div className="h-6 w-36 animate-pulse rounded-lg bg-table-head" />
                <div className="mt-5 h-40 animate-pulse rounded-xl border border-border bg-surface" />
                <div className="mt-6 grid grid-cols-1 gap-4 md:grid-cols-2">
                    {[0, 1, 2, 3].map(i => (
                        <div key={i} className="h-44 animate-pulse rounded-xl border border-border bg-surface" />
                    ))}
                </div>
            </div>
        );
    }

    if (pathQ.isError) {
        return (
            <div className="mx-auto max-w-5xl px-6 py-8">
                <div className="rounded-xl border border-danger/25 bg-danger/10 px-4 py-3 text-sm text-danger">
                    {(pathQ.error as Error)?.message || "Erreur lors du chargement du parcours."}
                </div>
            </div>
        );
    }

    const { path, modules } = pathQ.data!;
    const level = path.level ? LEVEL_LABELS[path.level] ?? path.level : null;
    const canComplete = totalCount > 0 && completedCount === totalCount && myAssignment?.status === "started";

    const startTarget = firstUncompleted ?? allChallenges[0];
    let startLabel = "Démarrer le parcours";
    let StartIcon = Play;
    if (completedCount > 0 && completedCount < totalCount) {
        startLabel = "Reprendre le parcours";
        StartIcon = Play;
    } else if (completedCount > 0 && completedCount === totalCount) {
        startLabel = "Recommencer";
        StartIcon = RotateCcw;
    }

    function handleStart() {
        if (myAssignment?.status === "assigned") startM.mutate();
        if (startTarget) router.push(`/dashboard/challenge/${startTarget.id}?path=${pathId}`);
    }

    function openChallenge(challengeId: string) {
        if (myAssignment?.status === "assigned") startM.mutate();
        router.push(`/dashboard/challenge/${challengeId}?path=${pathId}`);
    }

    const headerActions = (
        <>
            {startTarget && (
                <button
                    type="button"
                    onClick={handleStart}
                    className="inline-flex items-center gap-2 rounded-lg bg-primary px-4 py-2 text-sm font-medium text-white transition-colors duration-200 hover:bg-primary-hover"
                >
                    <StartIcon size={14} strokeWidth={2.5} />
                    {startLabel}
                </button>
            )}
            {canComplete && (
                <button
                    type="button"
                    onClick={() => completeM.mutate()}
                    disabled={completeM.isPending}
                    className="inline-flex items-center gap-2 rounded-lg bg-success px-4 py-2 text-sm font-medium text-white transition-colors duration-200 hover:brightness-110 disabled:opacity-60"
                >
                    <CheckCircle2 size={14} strokeWidth={2.5} />
                    {completeM.isPending ? "Finalisation…" : "Terminer le parcours"}
                </button>
            )}
        </>
    );

    return (
        <div className="mx-auto flex min-w-0 max-w-5xl flex-col gap-6 px-6 py-8" style={{ flexShrink: 0 }}>
            <ParcoursHeader
                title={path.title}
                description={path.description}
                level={level}
                type={path.type}
                completedCount={completedCount}
                totalCount={totalCount}
                actions={headerActions}
            />

            <div className="flex flex-col gap-5">
                {modules.map((m, index) => {
                    const prevModuleChallenges = modules.slice(0, index).flatMap(pm => pm.challenges);
                    const prevAllCompleted =
                        prevModuleChallenges.length === 0 ||
                        prevModuleChallenges.every(c => completedIds.has(c.id));
                    const locked = !prevAllCompleted && index > 0;

                    return (
                        <section key={m.id} className="flex flex-col gap-3">
                            <div className="flex items-baseline justify-between">
                                <h2 className="text-sm font-semibold uppercase tracking-wider text-fg-muted">
                                    Module {index + 1}
                                </h2>
                                <span className="text-xs text-fg-muted">
                                    {m.challenges.length} challenge{m.challenges.length > 1 ? "s" : ""}
                                </span>
                            </div>
                            <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
                                <ModuleCard
                                    title={m.title}
                                    challenges={m.challenges}
                                    completedIds={completedIds}
                                    locked={locked}
                                    onOpenChallenge={openChallenge}
                                />
                            </div>
                        </section>
                    );
                })}
            </div>
        </div>
    );
}
