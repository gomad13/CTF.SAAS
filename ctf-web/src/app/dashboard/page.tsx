"use client";

import Link from "next/link";
import { useQuery } from "@tanstack/react-query";
import { apiFetch, clearToken } from "@/lib/api";

type MyAssignment = {
    pathId: string;
    status: string;
    dueAt: string | null;
    assignedAt: string;
    startedAt: string | null;
    completedAt: string | null;
    updatedAt: string;
    progressStatus?: string;
    progressPercent?: number;
};

export default function DashboardPage() {
    const q = useQuery({
        queryKey: ["assignments", "mine"],
        queryFn: () => apiFetch<MyAssignment[]>("/api/assignments/mine"),
    });

    return (
        <div className="min-h-screen bg-neutral-950 text-neutral-100">
            <div className="mx-auto max-w-5xl px-4 py-8">
                <div className="flex items-start justify-between gap-4">
                    <div>
                        <h1 className="text-2xl font-semibold">Dashboard</h1>
                        <p className="mt-1 text-sm text-neutral-400">
                            Tes parcours assignés (V1).
                        </p>
                    </div>

                    <div className="flex gap-2">
                        <Link
                            href="/login"
                            className="rounded-xl border border-neutral-800 bg-neutral-950/40 px-3 py-2 text-sm hover:bg-neutral-800/40"
                        >
                            Login
                        </Link>

                        <button
                            className="rounded-xl bg-white px-3 py-2 text-sm font-semibold text-neutral-950 hover:opacity-90"
                            onClick={() => {
                                clearToken();
                                window.location.href = "/login";
                            }}
                        >
                            Logout
                        </button>
                    </div>
                </div>

                <div className="mt-6 space-y-3">
                    {q.isLoading && <Skeleton />}

                    {q.isError && (
                        <div className="rounded-xl border border-red-900/60 bg-red-950/30 px-3 py-2 text-sm text-red-200">
                            {(q.error as any)?.message || "Erreur"}
                        </div>
                    )}

                    {q.data?.length === 0 && (
                        <div className="rounded-xl border border-neutral-800 bg-neutral-950/30 px-3 py-6 text-sm text-neutral-300">
                            Aucun parcours assigné.
                        </div>
                    )}

                    {q.data?.map((a) => {
                        const pct = a.progressPercent ?? 0;
                        return (
                            <div key={a.pathId} className="rounded-2xl border border-neutral-800 bg-neutral-900/40 p-4">
                                <div className="flex items-start justify-between gap-4">
                                    <div>
                                        <div className="text-xs text-neutral-400">PathId</div>
                                        <div className="text-lg font-semibold">{a.pathId}</div>

                                        <div className="mt-1 text-sm text-neutral-300">
                                            Status: <span className="text-white">{a.status}</span>
                                            {"  "}• Progress: <span className="text-white">{pct}%</span>
                                        </div>
                                    </div>

                                    <Link
                                        href={`/paths/${a.pathId}`}
                                        className="rounded-xl bg-white px-3 py-2 text-sm font-semibold text-neutral-950 hover:opacity-90"
                                    >
                                        Ouvrir
                                    </Link>
                                </div>

                                <div className="mt-3 h-2 w-full rounded-full bg-neutral-800">
                                    <div
                                        className="h-2 rounded-full bg-white"
                                        style={{ width: `${Math.max(0, Math.min(100, pct))}%` }}
                                    />
                                </div>
                            </div>
                        );
                    })}
                </div>
            </div>
        </div>
    );
}

function Skeleton() {
    return (
        <div className="space-y-3">
            <div className="h-20 rounded-2xl border border-neutral-800 bg-neutral-900/30" />
            <div className="h-20 rounded-2xl border border-neutral-800 bg-neutral-900/30" />
        </div>
    );
}
