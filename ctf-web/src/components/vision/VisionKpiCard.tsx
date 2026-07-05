"use client";

import type { LucideIcon } from "lucide-react";
import { ArrowUpRight, ArrowDownRight } from "lucide-react";
import CountUp from "@/components/CountUp";
import VisionCard from "./VisionCard";

/** Carte KPI Vision UI : pastille dégradé violet, gros chiffre count-up, variation +/-% optionnelle (réelle). */
export default function VisionKpiCard({
    value,
    suffix = "",
    label,
    icon: Icon,
    delta = null,
    hint,
}: {
    value: number;
    suffix?: string;
    label: string;
    icon: LucideIcon;
    delta?: number | null; // variation réelle en points/%, ou null si non disponible
    hint?: string;
}) {
    const up = (delta ?? 0) >= 0;
    return (
        <VisionCard hover padding={22}>
            <div style={{ display: "flex", alignItems: "flex-start", justifyContent: "space-between", gap: 12 }}>
                <div style={{ minWidth: 0 }}>
                    <div style={{ fontSize: 13, color: "var(--v-text-2)", fontWeight: 500 }}>{label}</div>
                    <div style={{ marginTop: 8, fontSize: 28, fontWeight: 700, color: "var(--v-text)", letterSpacing: "-0.02em" }}>
                        <CountUp value={value} />
                        {suffix && <span style={{ fontSize: 16, color: "var(--v-text-2)", fontWeight: 600 }}>{suffix}</span>}
                    </div>
                </div>
                <span
                    aria-hidden
                    style={{
                        flexShrink: 0,
                        display: "inline-flex",
                        alignItems: "center",
                        justifyContent: "center",
                        width: 44,
                        height: 44,
                        borderRadius: 14,
                        background: "var(--v-grad)",
                        color: "var(--v-text)",
                        boxShadow: "0 6px 16px color-mix(in srgb, var(--v-accent) 45%, transparent)",
                    }}
                >
                    <Icon size={20} />
                </span>
            </div>
            <div style={{ marginTop: 10, minHeight: 18, display: "flex", alignItems: "center", gap: 8 }}>
                {delta != null ? (
                    <span
                        style={{
                            display: "inline-flex",
                            alignItems: "center",
                            gap: 2,
                            fontSize: 12,
                            fontWeight: 600,
                            color: up ? "var(--v-success)" : "var(--v-danger)",
                        }}
                    >
                        {up ? <ArrowUpRight size={14} /> : <ArrowDownRight size={14} />}
                        {up ? "+" : ""}
                        {delta}
                        {suffix.includes("%") ? "%" : " pts"}
                    </span>
                ) : null}
                {hint && <span style={{ fontSize: 12, color: "var(--v-text-3)" }}>{hint}</span>}
            </div>
        </VisionCard>
    );
}
