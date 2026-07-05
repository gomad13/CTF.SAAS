"use client";

import { AreaChart, Area, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer } from "recharts";
import { useReducedMotion } from "framer-motion";

/** Aire premium Vision UI : dégradé violet→cyan (tokens --v-*), grille discrète, tooltip verre. */
export default function VisionAreaChart({
    data,
    xKey = "label",
    yKey = "value",
    height = 260,
    domainMax = 100,
}: {
    data: Record<string, unknown>[];
    xKey?: string;
    yKey?: string;
    height?: number;
    domainMax?: number;
}) {
    const reduce = useReducedMotion();
    return (
        <ResponsiveContainer width="100%" height={height}>
            <AreaChart data={data} margin={{ top: 10, right: 8, left: -14, bottom: 0 }}>
                <defs>
                    <linearGradient id="vAreaFill" x1="0" y1="0" x2="0" y2="1">
                        <stop offset="0%" style={{ stopColor: "var(--v-accent-2)", stopOpacity: 0.45 }} />
                        <stop offset="100%" style={{ stopColor: "var(--v-cyan)", stopOpacity: 0.02 }} />
                    </linearGradient>
                    <linearGradient id="vAreaStroke" x1="0" y1="0" x2="1" y2="0">
                        <stop offset="0%" style={{ stopColor: "var(--v-accent)" }} />
                        <stop offset="100%" style={{ stopColor: "var(--v-cyan)" }} />
                    </linearGradient>
                </defs>
                <CartesianGrid strokeDasharray="3 3" stroke="var(--v-border)" vertical={false} />
                <XAxis dataKey={xKey} tick={{ fill: "var(--v-text-3)", fontSize: 12 }} tickLine={false} axisLine={{ stroke: "var(--v-border)" }} />
                <YAxis tick={{ fill: "var(--v-text-3)", fontSize: 12 }} tickLine={false} axisLine={false} width={34} domain={[0, domainMax]} allowDecimals={false} />
                <Tooltip
                    cursor={{ stroke: "var(--v-border)" }}
                    contentStyle={{ background: "var(--v-surface-2)", border: "1px solid var(--v-border)", borderRadius: 12, fontSize: 12, color: "var(--v-text)" }}
                    labelStyle={{ color: "var(--v-text-2)" }}
                />
                <Area
                    type="monotone"
                    dataKey={yKey}
                    stroke="url(#vAreaStroke)"
                    strokeWidth={3}
                    fill="url(#vAreaFill)"
                    dot={false}
                    activeDot={{ r: 5 }}
                    isAnimationActive={!reduce}
                    animationDuration={700}
                />
            </AreaChart>
        </ResponsiveContainer>
    );
}
