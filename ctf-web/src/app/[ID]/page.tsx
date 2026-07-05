"use client";

import Link from "next/link";
import { useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiFetch } from "@/lib/api";
import Reveal from "@/components/Reveal";

type PathDto = {
    id: string;
    title?: string;
    description?: string;
    modules: Array<{
        id: string;
        title: string;
        sortOrder?: number;
        challenges: Array<{
            id: string;
            type: string;
            title: string;
            instructions: string;
            difficulty?: number | null;
            points: number;
            status: string;
        }>;
    }>;
};

type MyAssignment = {
    pathId: string;
    status: string;
    progressPercent?: number;
    progressStatus?: string;
};

export default function PathPage({ params }: { params: { id: string } }) {
    const pathId = params.id;
    const qc = useQueryClient();

    // 1) Charger le parcours complet
    const pathQ = useQuery({
        queryKey: ["path", pathId],
        queryFn: () => apiFetch<PathDto>(`/api/paths/${pathId}`),
    });

    // 2) Charger l'état assignment + progress (mine)
    const mineQ = useQuery({
        queryKey: ["assignments", "mine"],
        queryFn: () => apiFetch<MyAssignment[]>("/api/assignments/mine"),
    });

    const my = useMemo(() => {
        const items = mineQ.data ?? [];
        return items.find((x) => x.pathId === pathId) ?? null;
    }, [mineQ.data, pathId]);

    const progressPct = my?.progressPercent ?? 0;

    // Aide: flatten challenges
    const allChallenges = useMemo(() => {
        const p = pathQ.data;
        if (!p) return [];
        return p.modules
            .slice()
            .sort((a, b) => (a.sortOrder ?? 0) - (b.sortOrder ?? 0))
            .flatMap((m) =>
                m.challenges.map((c) => ({
                    moduleId: m.id,
                    moduleTitle: m.title,
                    ...c,
                }))
            );
    }, [pathQ.data]);

    const [activeId, setActiveId] = useState<string | null>(null);
    const active = useMemo(
        () => allChallenges.find((c) => c.id === activeId) ?? allChallenges[0] ?? null,
        [allChallenges, activeId]
    );

    // --- Mutations
    const startM = useMutation({
        mutationFn: () => apiFetch<void>(`/api/assignments/${pathId}/start`, { method: "POST" }),
        onSuccess: async () => {
            await qc.invalidateQueries({ queryKey: ["assignments", "mine"] });
        },
    });

    const completeM = useMutation({
        mutationFn: () => apiFetch<void>(`/api/assignments/${pathId}/complete`, { method: "POST" }),
        onSuccess: async () => {
            await qc.invalidateQueries({ queryKey: ["assignments", "mine"] });
        },
    });

    const submitM = useMutation({
        mutationFn: (payload: { challengeId: string; isCorrect: boolean }) =>
            apiFetch<any>("/api/submissions", {
                method: "POST",
                body: JSON.stringify(payload),
            }),
        onSuccess: async () => {
            // Progress recalculé côté backend => on refresh "mine"
            await qc.invalidateQueries({ queryKey: ["assignments", "mine"] });
        },
    });

    // --- UI states
    if (pathQ.isLoading) return <PageShell>Chargement du parcours…</PageShell>;
    if (pathQ.isError)
        return (
            <PageShell>
                <div className="rounded-xl border border-[var(--danger-subtle)] bg-[var(--danger-subtle)] px-3 py-2 text-sm text-[var(--danger-t)]">
                    {(pathQ.error as any)?.message || "Erreur chargement parcours"}
                </div>
            </PageShell>
        );

    const path = pathQ.data!;

    return (
        <div className="min-h-screen bg-[var(--bg)] text-[var(--text)]">
            <div className="mx-auto max-w-6xl px-4 py-8">
                <Reveal>
                <div className="flex items-start justify-between gap-4">
                    <div>
                        <Link href="/dashboard" className="text-sm text-[var(--text-3)] hover:text-[var(--text)] transition-colors duration-200">
                            ← Retour dashboard
                        </Link>
                        <h1 className="mt-2 text-2xl font-semibold">{path.title ?? "Parcours"}</h1>
                        {path.description && <p className="mt-1 text-sm text-[var(--text-3)]">{path.description}</p>}
                    </div>

                    <div className="flex items-center gap-2">
                        <div className="min-w-[160px]">
                            <div className="text-xs text-[var(--text-3)]">Progress</div>
                            <div className="text-lg font-semibold">{progressPct}%</div>
                            <div className="mt-2 h-2 w-full rounded-full bg-[var(--surface-2)]">
                                <div className="h-2 rounded-full bg-[var(--accent)] transition-[width] duration-500" style={{ width: `${progressPct}%` }} />
                            </div>
                        </div>

                        <div className="flex flex-col gap-2">
                            <button
                                className="rounded-xl border border-[var(--border)] bg-[var(--surface-2)] px-3 py-2 text-sm hover:bg-[var(--surface)] transition-colors duration-200 disabled:opacity-60"
                                onClick={() => startM.mutate()}
                                disabled={startM.isPending || my?.status === "started" || my?.status === "completed"}
                                title="Démarre l'assignment (status = started)"
                            >
                                {startM.isPending ? "Start…" : my?.status === "started" ? "Démarré" : "Start"}
                            </button>

                            <button
                                className="rounded-xl bg-[var(--accent)] px-3 py-2 text-sm font-semibold text-[var(--on-accent)] hover:bg-[var(--accent-hover)] transition-colors duration-200 disabled:opacity-60"
                                onClick={() => completeM.mutate()}
                                disabled={completeM.isPending || progressPct < 100 || my?.status === "completed"}
                                title="Termine si progress = 100%"
                            >
                                {completeM.isPending ? "Complete…" : my?.status === "completed" ? "Complété" : "Complete"}
                            </button>
                        </div>
                    </div>
                </div>
                </Reveal>

                <div className="mt-8 grid grid-cols-12 gap-4">
                    {/* Left: modules + challenges */}
                    <aside className="col-span-12 md:col-span-4">
                        <div className="rounded-2xl border border-[var(--border)] bg-[var(--surface)] p-4">
                            <div className="text-sm font-semibold">Modules</div>
                            <div className="mt-3 space-y-3">
                                {path.modules
                                    .slice()
                                    .sort((a, b) => (a.sortOrder ?? 0) - (b.sortOrder ?? 0))
                                    .map((m) => (
                                        <div key={m.id}>
                                            <div className="text-sm text-[var(--text-2)]">{m.title}</div>
                                            <div className="mt-2 space-y-1">
                                                {m.challenges.map((c) => {
                                                    const isActive = (active?.id ?? allChallenges[0]?.id) === c.id;
                                                    return (
                                                        <button
                                                            key={c.id}
                                                            onClick={() => setActiveId(c.id)}
                                                            className={[
                                                                "w-full rounded-xl border px-3 py-2 text-left text-sm transition-colors duration-200",
                                                                isActive
                                                                    ? "border-[var(--accent-border)] bg-[var(--surface-2)] text-[var(--text)]"
                                                                    : "border-[var(--border)] bg-[var(--surface-2)] text-[var(--text-2)] hover:bg-[var(--surface)] hover:border-[var(--accent-border)]",
                                                            ].join(" ")}
                                                        >
                                                            <div className="font-medium">{c.title}</div>
                                                            <div className="mt-1 text-xs text-[var(--text-3)]">
                                                                {c.type} • {c.points} pts
                                                            </div>
                                                        </button>
                                                    );
                                                })}
                                            </div>
                                        </div>
                                    ))}
                            </div>
                        </div>
                    </aside>

                    {/* Right: challenge runner */}
                    <main className="col-span-12 md:col-span-8">
                        <div className="rounded-2xl border border-[var(--border)] bg-[var(--surface)] p-5">
                            {!active ? (
                                <div className="text-[var(--text-2)]">Aucun challenge.</div>
                            ) : (
                                <>
                                    <div className="flex items-start justify-between gap-4">
                                        <div>
                                            <div className="text-xs text-[var(--text-3)]">Challenge</div>
                                            <h2 className="text-xl font-semibold">{active.title}</h2>
                                            <div className="mt-1 text-sm text-[var(--text-3)]">
                                                {active.type} • {active.points} pts
                                            </div>
                                        </div>
                                    </div>

                                    <div className="mt-5 whitespace-pre-wrap rounded-xl border border-[var(--border)] bg-[var(--surface-2)] p-4 text-sm text-[var(--text-2)]">
                                        {active.instructions}
                                    </div>

                                    <div className="mt-5 flex flex-wrap gap-2">
                                        <button
                                            className="rounded-xl bg-[var(--accent)] px-4 py-2 text-sm font-semibold text-[var(--on-accent)] hover:bg-[var(--accent-hover)] transition-colors duration-200 disabled:opacity-60"
                                            disabled={submitM.isPending}
                                            onClick={() => submitM.mutate({ challengeId: active.id, isCorrect: true })}
                                            title="Démo: envoie une réponse correcte"
                                        >
                                            {submitM.isPending ? "Envoi…" : "Réponse correcte ?"}
                                        </button>

                                        <button
                                            className="rounded-xl border border-[var(--border)] bg-[var(--surface-2)] px-4 py-2 text-sm hover:bg-[var(--surface)] transition-colors duration-200 disabled:opacity-60"
                                            disabled={submitM.isPending}
                                            onClick={() => submitM.mutate({ challengeId: active.id, isCorrect: false })}
                                            title="Démo: envoie une réponse fausse"
                                        >
                                            Réponse fausse ?
                                        </button>
                                    </div>

                                    {submitM.isError && (
                                        <div className="mt-4 rounded-xl border border-[var(--danger-subtle)] bg-[var(--danger-subtle)] px-3 py-2 text-sm text-[var(--danger-t)]">
                                            {(submitM.error as any)?.message || "Erreur submission"}
                                        </div>
                                    )}

                                    <div className="mt-6 border-t border-[var(--border)] pt-4 text-xs text-[var(--text-3)]">
                                        Astuce: le progress se met à jour automatiquement après une submission correcte.
                                    </div>
                                </>
                            )}
                        </div>
                    </main>
                </div>
            </div>
        </div>
    );
}

function PageShell({ children }: { children: React.ReactNode }) {
    return (
        <div className="min-h-screen bg-[var(--bg)] text-[var(--text)]">
            <div className="mx-auto max-w-4xl px-4 py-10">{children}</div>
        </div>
    );
}
