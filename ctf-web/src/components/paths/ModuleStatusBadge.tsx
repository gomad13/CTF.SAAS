"use client";

import { CheckCircle2, CircleDashed, Clock, Lock } from "lucide-react";

export type ModuleStatus = "todo" | "in_progress" | "completed" | "locked";

type Props = {
    status: ModuleStatus;
};

const CONFIG: Record<ModuleStatus, { label: string; bg: string; color: string; Icon: typeof CheckCircle2 }> = {
    todo: {
        label: "À faire",
        bg: "#F1F5F9",
        color: "#64748B",
        Icon: CircleDashed,
    },
    in_progress: {
        label: "En cours",
        bg: "rgba(59,130,246,0.10)",
        color: "#1E40AF",
        Icon: Clock,
    },
    completed: {
        label: "Complété",
        bg: "rgba(16,185,129,0.10)",
        color: "#065F46",
        Icon: CheckCircle2,
    },
    locked: {
        label: "Verrouillé",
        bg: "#F1F5F9",
        color: "#64748B",
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
