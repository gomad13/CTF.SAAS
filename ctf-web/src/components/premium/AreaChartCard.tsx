"use client";
import { AreaChart, Area, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer } from "recharts";

/** Aire premium à dégradé vert→cyan (tokens --accent → --accent-2), grille fine, tooltip sombre.
 *  Couleurs via CSS var (stop-color en style pour que var() résolve dans le SVG). */
export default function AreaChartCard({ data, xKey = "label", yKey = "value", height = 240, domainMax = 100 }: {
    data: Record<string, unknown>[]; xKey?: string; yKey?: string; height?: number; domainMax?: number;
}) {
    return (
        <ResponsiveContainer width="100%" height={height}>
            <AreaChart data={data} margin={{ top: 8, right: 8, left: -14, bottom: 0 }}>
                <defs>
                    <linearGradient id="areaFill" x1="0" y1="0" x2="0" y2="1">
                        <stop offset="0%" style={{ stopColor: "var(--accent)", stopOpacity: 0.35 }} />
                        <stop offset="100%" style={{ stopColor: "var(--accent-2)", stopOpacity: 0.02 }} />
                    </linearGradient>
                    <linearGradient id="areaStroke" x1="0" y1="0" x2="1" y2="0">
                        <stop offset="0%" style={{ stopColor: "var(--accent)" }} />
                        <stop offset="100%" style={{ stopColor: "var(--accent-2)" }} />
                    </linearGradient>
                </defs>
                <CartesianGrid strokeDasharray="3 3" stroke="var(--border)" vertical={false} />
                <XAxis dataKey={xKey} tick={{ fill: "var(--text-3)", fontSize: 12 }} tickLine={false} axisLine={{ stroke: "var(--border)" }} />
                <YAxis tick={{ fill: "var(--text-3)", fontSize: 12 }} tickLine={false} axisLine={false} width={34} domain={[0, domainMax]} allowDecimals={false} />
                <Tooltip cursor={{ stroke: "var(--border)" }} contentStyle={{ background: "var(--surface)", border: "1px solid var(--border)", borderRadius: 10, fontSize: 12, color: "var(--text)" }} />
                <Area type="monotone" dataKey={yKey} stroke="url(#areaStroke)" strokeWidth={2.5} fill="url(#areaFill)" dot={false} activeDot={{ r: 5 }} />
            </AreaChart>
        </ResponsiveContainer>
    );
}
