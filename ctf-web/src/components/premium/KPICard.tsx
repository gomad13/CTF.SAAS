"use client";
import type { LucideIcon } from "lucide-react";
import CountUp from "@/components/CountUp";

/** Carte KPI premium : label, valeur animée (count-up), icône, delta optionnel. */
export default function KPICard({ label, value, suffix = "", icon: Icon, delta, hint }: {
    label: string; value: number; suffix?: string; icon?: LucideIcon; delta?: number; hint?: string;
}) {
    const up = (delta ?? 0) >= 0;
    return (
        <div className="relative overflow-hidden rounded-2xl border border-border bg-surface/80 p-6 shadow-[0_8px_30px_rgba(0,0,0,0.25)] backdrop-blur-md transition-transform duration-200 hover:-translate-y-0.5">
            {Icon && (
                <span aria-hidden className="absolute right-4 top-4 inline-flex h-10 w-10 items-center justify-center rounded-xl" style={{ background: "linear-gradient(135deg, var(--accent), var(--accent-hover))", color: "var(--on-accent)", boxShadow: "0 6px 16px color-mix(in srgb, var(--accent) 40%, transparent)" }}>
                    <Icon size={18} />
                </span>
            )}
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
