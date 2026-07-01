"use client";

import Link from "next/link";
import { ArrowLeft } from "lucide-react";
import ParcoursProgressBar from "./ParcoursProgressBar";

type Props = {
    title: string;
    description: string | null;
    level: string | null;
    type: string;
    completedCount: number;
    totalCount: number;
    actions?: React.ReactNode;
};

export default function ParcoursHeader({
    title,
    description,
    level,
    type,
    completedCount,
    totalCount,
    actions,
}: Props) {
    return (
        <div className="flex flex-col gap-5">
            <Link
                href="/dashboard/parcours"
                className="inline-flex items-center gap-1.5 text-sm font-medium text-[#94A3B8] transition-colors duration-200 hover:text-[#60A5FA]"
            >
                <ArrowLeft size={14} />
                Mes parcours
            </Link>

            <div className="rounded-xl border border-border bg-surface p-6 shadow-sm">
                <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
                    <div className="min-w-0 flex-1">
                        <div className="flex flex-wrap items-center gap-2">
                            {level && (
                                <span className="rounded-full bg-table-head px-2.5 py-0.5 text-xs font-medium text-fg-muted">
                                    {level}
                                </span>
                            )}
                            <span className="rounded-full bg-table-head px-2.5 py-0.5 text-xs font-medium text-fg-muted">
                                {type}
                            </span>
                        </div>
                        <h1 className="mt-3 text-2xl font-bold text-fg-heading">{title}</h1>
                        {description && (
                            <p className="mt-1.5 text-sm leading-relaxed text-fg-body">{description}</p>
                        )}
                    </div>
                    {actions && <div className="flex shrink-0 gap-2">{actions}</div>}
                </div>

                <div className="mt-6 border-t border-border pt-5">
                    <ParcoursProgressBar completed={completedCount} total={totalCount} />
                </div>
            </div>
        </div>
    );
}
