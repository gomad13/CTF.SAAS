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
    exitLabel?: string;
};

/** Récap de fin de session Épreuve : score, temps, bonnes/mauvaises, rejouer. Charte violet Vision. */
export default function SessionRecap({ correct, total, timeLabel, onReplay, onExit, exitLabel = "Menu" }: Props) {
    const reduce = useReducedMotion();
    const wrong = total - correct;
    const pct = total > 0 ? Math.round((correct / total) * 100) : 0;
    const good = pct >= 70;
    const btnFocus = "focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2";

    return (
        <motion.div initial={reduce ? false : { opacity: 0, y: 8 }} animate={{ opacity: 1, y: 0 }} transition={{ duration: 0.25 }}
            className="mx-auto flex w-full max-w-md flex-col items-center gap-6 text-center"
            style={{ borderRadius: 18, border: "1px solid var(--v-border)", background: "var(--v-surface)", padding: 32 }}>
            <div className="flex h-16 w-16 items-center justify-center rounded-full"
                style={{ background: good ? "color-mix(in srgb, var(--v-accent) 16%, transparent)" : "var(--v-surface-2)" }}>
                <Trophy className="h-8 w-8" style={{ color: good ? "var(--v-accent)" : "var(--v-text-3)" }} />
            </div>

            <div>
                <h2 style={{ fontSize: 24, fontWeight: 700, color: "var(--v-text)" }}>Épreuve terminée</h2>
                <p style={{ marginTop: 4, fontSize: 14, color: "var(--v-text-2)" }}>{good ? "Excellents réflexes !" : "Continuez à vous entraîner."}</p>
            </div>

            <div style={{ fontSize: 44, fontWeight: 800, color: "var(--v-text)", letterSpacing: "-0.02em" }}>
                <CountUp value={correct} /><span style={{ color: "var(--v-text-3)" }}> / {total}</span>
                <span style={{ marginLeft: 10, fontSize: 18, fontWeight: 600, color: "var(--v-text-2)" }}>({pct}%)</span>
            </div>

            <div className="grid w-full grid-cols-3 gap-3">
                <Stat icon={<Check className="h-4 w-4" />} color="var(--v-success)" label="Justes" value={correct} />
                <Stat icon={<X className="h-4 w-4" />} color="var(--v-danger)" label="Faux" value={wrong} />
                <Stat icon={<Clock className="h-4 w-4" />} color="var(--v-text-2)" label="Temps" text={timeLabel} />
            </div>

            <div className="flex w-full gap-3">
                <button onClick={onExit} className={btnFocus}
                    style={{ flex: 1, display: "inline-flex", alignItems: "center", justifyContent: "center", gap: 8, borderRadius: 10, border: "1px solid var(--v-border)", padding: "10px 16px", fontWeight: 500, color: "var(--v-text)", background: "transparent", cursor: "pointer" }}>
                    <ArrowLeft className="h-4 w-4" /> {exitLabel}
                </button>
                <button onClick={onReplay} className={btnFocus}
                    style={{ flex: 1, display: "inline-flex", alignItems: "center", justifyContent: "center", gap: 8, borderRadius: 10, background: "var(--v-grad)", padding: "10px 16px", fontWeight: 600, color: "var(--v-text)", border: "none", cursor: "pointer", boxShadow: "0 6px 16px color-mix(in srgb, var(--v-accent) 40%, transparent)" }}>
                    <RotateCcw className="h-4 w-4" /> Rejouer
                </button>
            </div>
        </motion.div>
    );
}

function Stat({ icon, label, value, text, color }: { icon: React.ReactNode; label: string; value?: number; text?: string; color: string }) {
    return (
        <div className="flex flex-col items-center gap-1" style={{ borderRadius: 12, border: "1px solid var(--v-border)", background: "var(--v-surface-2)", padding: "12px 8px" }}>
            <span style={{ color }}>{icon}</span>
            <span style={{ fontSize: 18, fontWeight: 700, color: "var(--v-text)" }}>{text ?? <CountUp value={value ?? 0} />}</span>
            <span style={{ fontSize: 12, color: "var(--v-text-3)" }}>{label}</span>
        </div>
    );
}
