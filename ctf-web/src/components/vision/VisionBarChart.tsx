"use client";

import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, Cell } from "recharts";
import { useReducedMotion } from "framer-motion";

/** Barres sobres Vision UI : dégradé violet, grille discrète, coins arrondis. */
export default function VisionBarChart({
    data,
    xKey = "label",
    yKey = "value",
    height = 260,
}: {
    data: Record<string, unknown>[];
    xKey?: string;
    yKey?: string;
    height?: number;
}) {
    const reduce = useReducedMotion();
    return (
        <ResponsiveContainer width="100%" height={height}>
            <BarChart data={data} margin={{ top: 10, right: 8, left: -14, bottom: 0 }}>
                <defs>
                    <linearGradient id="vBarGrad" x1="0" y1="0" x2="0" y2="1">
                        <stop offset="0%" style={{ stopColor: "var(--v-accent)" }} />
                        <stop offset="100%" style={{ stopColor: "var(--v-accent-2)" }} />
                    </linearGradient>
                </defs>
                <CartesianGrid strokeDasharray="3 3" stroke="var(--v-border)" vertical={false} />
                <XAxis dataKey={xKey} tick={{ fill: "var(--v-text-3)", fontSize: 12 }} tickLine={false} axisLine={{ stroke: "var(--v-border)" }} />
                <YAxis tick={{ fill: "var(--v-text-3)", fontSize: 12 }} tickLine={false} axisLine={false} width={34} allowDecimals={false} />
                <Tooltip
                    cursor={{ fill: "color-mix(in srgb, var(--v-accent) 12%, transparent)" }}
                    contentStyle={{ background: "var(--v-surface-2)", border: "1px solid var(--v-border)", borderRadius: 12, fontSize: 12, color: "var(--v-text)" }}
                    labelStyle={{ color: "var(--v-text-2)" }}
                />
                <Bar dataKey={yKey} radius={[8, 8, 0, 0]} isAnimationActive={!reduce} animationDuration={700} maxBarSize={54}>
                    {data.map((_, i) => (
                        <Cell key={i} fill="url(#vBarGrad)" />
                    ))}
                </Bar>
            </BarChart>
        </ResponsiveContainer>
    );
}
