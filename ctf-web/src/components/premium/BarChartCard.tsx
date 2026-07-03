"use client";
import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer } from "recharts";
import { CHART } from "@/lib/chart-colors";

/** Barres premium (charte vert cyber), responsive, coins arrondis. */
export default function BarChartCard({ data, xKey, yKey, height = 260 }: {
    data: Record<string, unknown>[]; xKey: string; yKey: string; height?: number;
}) {
    return (
        <ResponsiveContainer width="100%" height={height}>
            <BarChart data={data} margin={{ top: 8, right: 8, left: -12, bottom: 0 }}>
                <CartesianGrid strokeDasharray="3 3" stroke={CHART.grid} vertical={false} />
                <XAxis dataKey={xKey} tick={{ fill: CHART.axis, fontSize: 12 }} tickLine={false} axisLine={{ stroke: CHART.grid }} />
                <YAxis tick={{ fill: CHART.axis, fontSize: 12 }} tickLine={false} axisLine={false} width={36} allowDecimals={false} />
                <Tooltip cursor={{ fill: "var(--surface-2)" }} contentStyle={{ background: "var(--surface)", border: "1px solid var(--border)", borderRadius: 10, fontSize: 12, color: "var(--text)" }} />
                <Bar dataKey={yKey} fill={CHART.accent} radius={[6, 6, 0, 0]} maxBarSize={48} />
            </BarChart>
        </ResponsiveContainer>
    );
}
