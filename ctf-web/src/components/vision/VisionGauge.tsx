"use client";

import { motion, useReducedMotion } from "framer-motion";
import CountUp from "@/components/CountUp";

/** Jauge circulaire de score (0-100) — arc dégradé violet→cyan, chiffre count-up au centre. */
export default function VisionGauge({
    value,
    max = 100,
    band,
    size = 180,
}: {
    value: number | null;
    max?: number;
    band?: string;
    size?: number;
}) {
    const reduce = useReducedMotion();
    const stroke = 14;
    const r = (size - stroke) / 2;
    const c = 2 * Math.PI * r;
    const pct = value == null ? 0 : Math.max(0, Math.min(1, value / max));
    const offset = c * (1 - pct);

    return (
        <div style={{ display: "flex", flexDirection: "column", alignItems: "center", gap: 10 }}>
            <div style={{ position: "relative", width: size, height: size }}>
                <svg width={size} height={size} style={{ transform: "rotate(-90deg)" }}>
                    <defs>
                        <linearGradient id="vGaugeGrad" x1="0" y1="0" x2="1" y2="1">
                            <stop offset="0%" style={{ stopColor: "var(--v-accent)" }} />
                            <stop offset="100%" style={{ stopColor: "var(--v-cyan)" }} />
                        </linearGradient>
                    </defs>
                    <circle cx={size / 2} cy={size / 2} r={r} fill="none" stroke="var(--v-surface-2)" strokeWidth={stroke} />
                    <motion.circle
                        cx={size / 2}
                        cy={size / 2}
                        r={r}
                        fill="none"
                        stroke="url(#vGaugeGrad)"
                        strokeWidth={stroke}
                        strokeLinecap="round"
                        strokeDasharray={c}
                        initial={{ strokeDashoffset: reduce ? offset : c }}
                        animate={{ strokeDashoffset: offset }}
                        transition={{ duration: reduce ? 0 : 1, ease: "easeOut" }}
                    />
                </svg>
                <div style={{ position: "absolute", inset: 0, display: "flex", flexDirection: "column", alignItems: "center", justifyContent: "center" }}>
                    {value == null ? (
                        <span style={{ fontSize: 13, color: "var(--v-text-3)", textAlign: "center", padding: "0 16px" }}>En attente de données</span>
                    ) : (
                        <>
                            <span style={{ fontSize: 34, fontWeight: 700, color: "var(--v-text)", lineHeight: 1 }}>
                                <CountUp value={value} />
                            </span>
                            <span style={{ fontSize: 12, color: "var(--v-text-3)", marginTop: 2 }}>/ {max}</span>
                        </>
                    )}
                </div>
            </div>
            {band && value != null && (
                <span
                    style={{
                        fontSize: 12,
                        fontWeight: 600,
                        padding: "4px 12px",
                        borderRadius: 999,
                        background: "color-mix(in srgb, var(--v-accent) 16%, transparent)",
                        color: "var(--v-cyan)",
                    }}
                >
                    {band}
                </span>
            )}
        </div>
    );
}
