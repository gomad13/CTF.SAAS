"use client";
import type { LucideIcon } from "lucide-react";
import CountUp from "@/components/CountUp";

/** Carte KPI premium : label, valeur animée (count-up), icône, delta optionnel. */
export default function KPICard({ label, value, suffix = "", icon: Icon, delta, hint }: {
    label: string; value: number; suffix?: string; icon?: LucideIcon; delta?: number; hint?: string;
}) {
    const up = (delta ?? 0) >= 0;
    return (
        <div className="relative overflow-hidden rounded-xl border border-border bg-surface p-6 shadow-sm transition-transform duration-200 hover:-translate-y-0.5">
            {Icon && <Icon size={40} className="absolute right-4 top-4 opacity-10" style={{ color: "var(--accent)" }} />}
            <div className="text-xs font-medium uppercase tracking-wider" style={{ color: "var(--text-3)" }}>{label}</div>
            <div className="mt-2 text-3xl font-bold" style={{ color: "var(--text)", fontFamily: "'JetBrains Mono', monospace" }}>
                <CountUp value={value} suffix={suffix} />
            </div>
            {(delta !== undefined || hint) && (
                <div className="mt-1 text-xs" style={{ color: delta !== undefined ? (up ? "var(--success-t)" : "var(--danger-t)") : "var(--text-3)" }}>
                    {delta !== undefined ? `${up ? "▲" : "▼"} ${Math.abs(delta)}% ` : ""}{hint ?? ""}
                </div>
            )}
        </div>
    );
}
