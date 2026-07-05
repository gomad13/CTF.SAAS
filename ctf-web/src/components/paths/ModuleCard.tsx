"use client";

import { useMemo } from "react";
import { BookOpen, Play, RotateCcw } from "lucide-react";
import type { ChallengeItem } from "@/lib/types";
import ModuleStatusBadge, { type ModuleStatus } from "./ModuleStatusBadge";
import ParcoursProgressBar from "./ParcoursProgressBar";

type Props = {
    title: string;
    challenges: ChallengeItem[];
    completedIds: Set<string>;
    locked?: boolean;
    onOpenChallenge: (challengeId: string) => void;
};

export default function ModuleCard({
    title,
    challenges,
    completedIds,
    locked = false,
    onOpenChallenge,
}: Props) {
    const { completedCount, totalCount, status, nextChallenge } = useMemo(() => {
        const total = challenges.length;
        const completed = challenges.filter(c => completedIds.has(c.id)).length;
        let s: ModuleStatus;
        if (locked) s = "locked";
        else if (completed === 0) s = "todo";
        else if (completed < total) s = "in_progress";
        else s = "completed";
        const next = challenges.find(c => !completedIds.has(c.id)) ?? challenges[0];
        return { completedCount: completed, totalCount: total, status: s, nextChallenge: next };
    }, [challenges, completedIds, locked]);

    const isCompleted = status === "completed";
    const isInProgress = status === "in_progress";

    const actionLabel = isCompleted ? "Revoir" : isInProgress ? "Reprendre" : "Commencer";
    const ActionIcon = isCompleted ? RotateCcw : Play;

    const cardClassName = [
        "group flex flex-col gap-4 rounded-xl border bg-surface p-6 shadow-sm transition-all duration-200",
        isInProgress ? "border-primary ring-1 ring-primary/10" : "border-border",
        isCompleted ? "opacity-80" : "",
        locked ? "opacity-60" : "hover:shadow-md",
    ].join(" ");

    return (
        <div className={cardClassName}>
            <div className="flex items-start justify-between gap-3">
                <div className="flex min-w-0 items-start gap-3">
                    <div
                        className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-primary/10 text-primary"
                        aria-hidden
                    >
                        <BookOpen size={18} strokeWidth={2} />
                    </div>
                    <div className="min-w-0">
                        <h3 className="truncate text-base font-semibold text-fg-heading">{title}</h3>
                        <p className="mt-0.5 text-xs text-fg-muted">
                            {totalCount} challenge{totalCount > 1 ? "s" : ""}
                        </p>
                    </div>
                </div>
                <ModuleStatusBadge status={status} />
            </div>

            <ParcoursProgressBar completed={completedCount} total={totalCount} />

            <div className="flex items-center justify-between gap-3">
                <span className="text-xs text-fg-muted">
                    {completedCount}/{totalCount} complétés
                </span>
                <button
                    type="button"
                    disabled={locked || !nextChallenge}
                    onClick={() => nextChallenge && onOpenChallenge(nextChallenge.id)}
                    className="inline-flex items-center gap-1.5 rounded-lg bg-primary px-4 py-2 text-sm font-medium text-white transition-colors duration-200 hover:bg-primary-hover disabled:cursor-not-allowed disabled:bg-border disabled:text-fg-muted"
                    title={locked ? "Ce module est verrouillé" : undefined}
                >
                    <ActionIcon size={14} strokeWidth={2.5} />
                    {actionLabel}
                </button>
            </div>
        </div>
    );
}
