"use client";

import { ChevronLeft, ChevronRight, Trophy } from "lucide-react";
import type { ScoreboardPage } from "./types";

type Props = {
    data: ScoreboardPage;
    isLoading?: boolean;
    onPageChange: (page: number) => void;
};

export default function ScoreboardTable({ data, isLoading, onPageChange }: Props) {
    const totalPages = Math.max(1, Math.ceil(data.total / data.pageSize));
    const { page } = data;

    return (
        <div className="overflow-hidden rounded-xl border border-border bg-surface shadow-sm">
            <div className="flex items-center justify-between gap-3 border-b border-border bg-table-head px-6 py-3">
                <div className="flex items-center gap-2 text-xs font-semibold uppercase tracking-wider text-fg-muted">
                    <Trophy size={14} />
                    Classement complet
                </div>
                <span className="text-xs text-fg-muted">
                    {data.total} participant{data.total > 1 ? "s" : ""}
                </span>
            </div>

            {isLoading ? (
                <div className="divide-y divide-border">
                    {[0, 1, 2, 3, 4].map(i => (
                        <div key={i} className="h-14 animate-pulse bg-table-head/50" />
                    ))}
                </div>
            ) : data.items.length === 0 ? (
                <div className="px-6 py-12 text-center text-sm text-fg-muted">
                    Aucun participant pour cette page.
                </div>
            ) : (
                <div className="resp-scroll-x">
                <table className="w-full min-w-[560px]">
                    <thead>
                        <tr className="border-b border-border text-left text-xs uppercase tracking-wider text-fg-muted">
                            <th className="w-20 px-6 py-3 font-medium">Rang</th>
                            <th className="px-6 py-3 font-medium">Utilisateur</th>
                            <th className="w-32 px-6 py-3 font-medium">Score</th>
                            <th className="w-40 px-6 py-3 font-medium">Challenges</th>
                        </tr>
                    </thead>
                    <tbody className="divide-y divide-border">
                        {data.items.map(entry => (
                            <tr
                                key={entry.userId}
                                className={`transition-colors duration-150 hover:bg-canvas ${
                                    entry.isCurrentUser ? "bg-primary/5" : ""
                                }`}
                            >
                                <td className="px-6 py-3">
                                    <span
                                        className={`inline-flex h-7 min-w-[28px] items-center justify-center rounded-full px-2 text-xs font-semibold ${
                                            entry.rank === 1
                                                ? "bg-warning/10 text-warning"
                                                : entry.rank === 2
                                                ? "bg-fg-muted/10 text-fg-muted"
                                                : entry.rank === 3
                                                ? "bg-[#CD7F32]/10 text-[#CD7F32]"
                                                : "bg-table-head text-fg-muted"
                                        }`}
                                    >
                                        #{entry.rank}
                                    </span>
                                </td>
                                <td className="px-6 py-3">
                                    <div className="flex items-center gap-3">
                                        <div className="flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-primary text-xs font-bold text-white">
                                            {entry.initials}
                                        </div>
                                        <div className="flex flex-col">
                                            <span className="text-sm font-medium text-fg-heading">
                                                {entry.displayName}
                                            </span>
                                            {entry.isCurrentUser && (
                                                <span className="text-[10px] font-medium uppercase tracking-wider text-primary">
                                                    C&apos;est toi
                                                </span>
                                            )}
                                        </div>
                                    </div>
                                </td>
                                <td className="px-6 py-3">
                                    <span className="text-sm font-semibold text-primary">
                                        {entry.score.toLocaleString("fr-FR")}
                                    </span>
                                </td>
                                <td className="px-6 py-3 text-sm text-fg-body">
                                    {entry.challengesCompleted}
                                </td>
                            </tr>
                        ))}
                    </tbody>
                </table>
                </div>
            )}

            {totalPages > 1 && (
                <div className="flex items-center justify-between border-t border-border bg-canvas px-6 py-3">
                    <span className="text-xs text-fg-muted">
                        Page {page} / {totalPages}
                    </span>
                    <div className="flex gap-2">
                        <button
                            type="button"
                            onClick={() => onPageChange(page - 1)}
                            disabled={page <= 1}
                            className="inline-flex h-8 items-center gap-1 rounded-lg border border-border bg-surface px-3 text-xs font-medium text-fg-body transition-colors duration-200 hover:bg-table-head disabled:cursor-not-allowed disabled:opacity-40"
                        >
                            <ChevronLeft size={14} />
                            Précédent
                        </button>
                        <button
                            type="button"
                            onClick={() => onPageChange(page + 1)}
                            disabled={page >= totalPages}
                            className="inline-flex h-8 items-center gap-1 rounded-lg border border-border bg-surface px-3 text-xs font-medium text-fg-body transition-colors duration-200 hover:bg-table-head disabled:cursor-not-allowed disabled:opacity-40"
                        >
                            Suivant
                            <ChevronRight size={14} />
                        </button>
                    </div>
                </div>
            )}
        </div>
    );
}
