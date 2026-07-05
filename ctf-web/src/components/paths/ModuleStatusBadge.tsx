"use client";

import { CheckCircle2, CircleDashed, Clock, Lock } from "lucide-react";

export type ModuleStatus = "todo" | "in_progress" | "completed" | "locked";

type Props = {
    status: ModuleStatus;
};

// Tokens sémantiques : à faire / verrouillé = neutre (--surface-2/--text-3),
// en cours = --warning, complété = --success. color-mix pour les fonds à 10 %.
const CONFIG: Record<ModuleStatus, { label: string; bg: string; color: string; Icon: typeof CheckCircle2 }> = {
    todo: {
        label: "À faire",
        bg: "var(--surface-2)",
        color: "var(--text-3)",
        Icon: CircleDashed,
    },
    in_progress: {
        label: "En cours",
        bg: "color-mix(in srgb, var(--warning) 12%, transparent)",
        color: "var(--warning)",
        Icon: Clock,
    },
    completed: {
        label: "Complété",
        bg: "color-mix(in srgb, var(--success) 12%, transparent)",
        color: "var(--success)",
        Icon: CheckCircle2,
    },
    locked: {
        label: "Verrouillé",
        bg: "var(--surface-2)",
        color: "var(--text-3)",
        Icon: Lock,
    },
};

export default function ModuleStatusBadge({ status }: Props) {
    const c = CONFIG[status];
    const Icon = c.Icon;

    return (
        <span
            className="inline-flex items-center gap-1.5 rounded-full px-2.5 py-0.5 text-xs font-medium"
            style={{ background: c.bg, color: c.color }}
        >
            <Icon size={12} strokeWidth={2} />
            {c.label}
        </span>
    );
}
