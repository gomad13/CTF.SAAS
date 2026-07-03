"use client";
import { PieChart, Pie, Cell, ResponsiveContainer, Tooltip } from "recharts";
import { SERIES } from "@/lib/chart-colors";

/** Donut premium (charte vert cyber), avec libellé central. */
export default function DonutChart({ data, height = 240, centerLabel }: {
    data: { name: string; value: number }[]; height?: number; centerLabel?: string;
}) {
    const total = data.reduce((s, d) => s + d.value, 0);
    return (
        <div style={{ position: "relative", width: "100%" }}>
            <ResponsiveContainer width="100%" height={height}>
                <PieChart>
                    <Pie data={data} dataKey="value" nameKey="name" innerRadius="62%" outerRadius="88%" paddingAngle={2} stroke="var(--surface)" strokeWidth={2}>
                        {data.map((_, i) => <Cell key={i} fill={SERIES[i % SERIES.length]} />)}
                    </Pie>
                    <Tooltip contentStyle={{ background: "var(--surface)", border: "1px solid var(--border)", borderRadius: 10, fontSize: 12, color: "var(--text)" }} />
                </PieChart>
            </ResponsiveContainer>
            <div style={{ position: "absolute", inset: 0, display: "flex", flexDirection: "column", alignItems: "center", justifyContent: "center", pointerEvents: "none" }}>
                <div style={{ fontSize: 26, fontWeight: 700, color: "var(--text)" }}>{total}</div>
                {centerLabel && <div style={{ fontSize: 11, color: "var(--text-3)", textTransform: "uppercase", letterSpacing: "0.08em" }}>{centerLabel}</div>}
            </div>
        </div>
    );
}
