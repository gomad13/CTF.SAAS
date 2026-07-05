"use client";

import { motion, useReducedMotion } from "framer-motion";
import { Trophy, Clock, Check, X, RotateCcw, ArrowLeft } from "lucide-react";
import CountUp from "@/components/CountUp";

type Props = {
    correct: number;
    total: number;
    timeLabel: string;
    onReplay: () => void;
    onExit: () => void;
};

/** Récap de fin de session Épreuve : score, temps, bonnes/mauvaises, rejouer. */
export default function SessionRecap({ correct, total, timeLabel, onReplay, onExit }: Props) {
    const reduce = useReducedMotion();
    const wrong = total - correct;
    const pct = total > 0 ? Math.round((correct / total) * 100) : 0;
    const good = pct >= 70;

    return (
        <motion.div
            initial={reduce ? false : { opacity: 0, y: 8 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.25 }}
            className="mx-auto flex w-full max-w-md flex-col items-center gap-6 rounded-2xl border border-border bg-surface p-8 text-center"
        >
            <div
                className="flex h-16 w-16 items-center justify-center rounded-full"
                style={{ background: good ? "var(--accent-subtle)" : "var(--surface-2)" }}
            >
                <Trophy className="h-8 w-8" style={{ color: good ? "var(--accent)" : "var(--text-3)" }} />
            </div>

            <div>
                <h2 className="text-2xl font-bold text-[var(--text)]">Session terminée</h2>
                <p className="mt-1 text-sm text-[var(--text-2)]">
                    {good ? "Excellents réflexes !" : "Continuez à vous entraîner."}
                </p>
            </div>

            <div className="text-5xl font-bold text-[var(--text)]">
                <CountUp value={correct} />
                <span className="text-[var(--text-3)]"> / {total}</span>
            </div>

            <div className="grid w-full grid-cols-3 gap-3">
                <Stat icon={<Check className="h-4 w-4" />} tone="success" label="Justes" value={correct} />
                <Stat icon={<X className="h-4 w-4" />} tone="danger" label="Faux" value={wrong} />
                <Stat icon={<Clock className="h-4 w-4" />} tone="neutral" label="Temps" text={timeLabel} />
            </div>

            <div className="flex w-full gap-3">
                <button
                    onClick={onExit}
                    className="inline-flex flex-1 items-center justify-center gap-2 rounded-lg border border-border px-4 py-2.5 font-medium text-[var(--text)] transition-colors duration-200 hover:bg-surface-2 focus-visible:outline focus-visible:outline-2 focus-visible:outline-[var(--accent)]"
                >
                    <ArrowLeft className="h-4 w-4" /> Menu
                </button>
                <button
                    onClick={onReplay}
                    className="inline-flex flex-1 items-center justify-center gap-2 rounded-lg bg-accent px-4 py-2.5 font-medium text-[var(--on-accent)] transition-colors duration-200 hover:bg-[var(--accent-hover)] focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--accent)]"
                >
                    <RotateCcw className="h-4 w-4" /> Rejouer
                </button>
            </div>
        </motion.div>
    );
}

function Stat({
    icon,
    label,
    value,
    text,
    tone,
}: {
    icon: React.ReactNode;
    label: string;
    value?: number;
    text?: string;
    tone: "success" | "danger" | "neutral";
}) {
    const color =
        tone === "success" ? "var(--success)" : tone === "danger" ? "var(--danger)" : "var(--text-2)";
    return (
        <div className="flex flex-col items-center gap-1 rounded-xl border border-border bg-surface-2 px-2 py-3">
            <span style={{ color }}>{icon}</span>
            <span className="text-lg font-bold text-[var(--text)]">
                {text ?? <CountUp value={value ?? 0} />}
            </span>
            <span className="text-xs text-[var(--text-3)]">{label}</span>
        </div>
    );
}
