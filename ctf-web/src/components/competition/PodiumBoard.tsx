"use client";

import type { ReactNode } from "react";
import { Crown } from "lucide-react";
import Reveal from "@/components/Reveal";
import { Stagger, StaggerItem } from "@/components/Stagger";
import CountUp from "@/components/CountUp";
import VisionCard from "@/components/vision/VisionCard";

// Couleurs médailles or / argent / bronze — exception charte explicitement autorisée (cahier des charges).
const MEDAL: Record<1 | 2 | 3, string> = { 1: "#F5C451", 2: "#C3CAD4", 3: "#CD7F44" };
export function medalColor(rank: number): string { return MEDAL[rank as 1 | 2 | 3] ?? "var(--v-text-2)"; }

/** Une place du podium/classement — commune à l'individuel et à l'équipe. Seul l'avatar diffère. */
export type PodiumItem = {
    key: string;
    rank: number;
    name: string;
    score: number;
    subtitle?: string;        // ex. "3 challenges" (indiv.) ou "5 membres" (équipe)
    isCurrent: boolean;       // surligne l'entrée du membre / de son équipe
    currentLabel?: string;    // libellé si isCurrent (ex. "vous" / "mon équipe")
    avatarBg: string;         // fond de l'avatar : "var(--v-grad)" (indiv.) ou couleur d'équipe (pastille)
    avatarContent: ReactNode; // initiales (indiv.) ou icône d'équipe (pastille)
};

/**
 * Podium visuel partagé (DRY) : estrade top 3 (2e à gauche, 1er surélevé au centre, 3e à droite —
 * le 1er apparaît EN DERNIER) + reste du classement en lignes. Réutilisé par l'individuel ET l'équipe.
 * Animations framer-motion (Reveal/Stagger/CountUp) qui respectent prefers-reduced-motion.
 */
export default function PodiumBoard({ title, items, footer }: { title: string; items: PodiumItem[]; footer?: ReactNode }) {
    const [first, second, third, ...rest] = items;
    // 1er au centre et EN DERNIER (delay le plus élevé) pour l'effet.
    const spots = [
        { item: second, place: 2, height: 128, delay: 0.15 },
        { item: first, place: 1, height: 160, delay: 0.32 },
        { item: third, place: 3, height: 108, delay: 0.05 },
    ];
    return (
        <VisionCard>
            <h2 style={{ fontSize: 13, fontWeight: 700, textTransform: "uppercase", letterSpacing: "0.05em", color: "var(--v-text-2)", marginBottom: 6 }}>{title}</h2>
            <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr 1fr", gap: 10, alignItems: "end", marginTop: 8 }}>
                {spots.map(s => <PodiumSpot key={s.place} item={s.item} place={s.place} height={s.height} delay={s.delay} />)}
            </div>
            {rest.length > 0 && (
                <Stagger className="mt-3 flex flex-col gap-2" gap={0.06}>
                    {rest.map(it => <StaggerItem key={it.key}><PodiumRow item={it} /></StaggerItem>)}
                </Stagger>
            )}
            {footer && <p style={{ marginTop: 12, fontSize: 12, color: "var(--v-text-3)" }}>{footer}</p>}
        </VisionCard>
    );
}

function Avatar({ bg, content, size, ring }: { bg: string; content: ReactNode; size: number; ring?: string }) {
    return (
        <span aria-hidden style={{ position: "relative", flexShrink: 0, display: "inline-flex", height: size, width: size, alignItems: "center", justifyContent: "center", borderRadius: "50%", background: bg, color: "var(--v-text)", fontSize: size >= 48 ? 15 : 11, fontWeight: 800, boxShadow: ring }}>
            {content}
        </span>
    );
}

function PodiumSpot({ item, place, height, delay }: { item?: PodiumItem; place: number; height: number; delay: number }) {
    const color = medalColor(place);
    if (!item) return <div style={{ opacity: 0.35, textAlign: "center", fontSize: 12, color: "var(--v-text-3)", paddingBottom: 12 }}>—</div>;
    return (
        <Reveal delay={delay}>
            <div className="v-hover" style={{ display: "flex", flexDirection: "column", alignItems: "center", gap: 8, borderRadius: 16, border: "1px solid " + (item.isCurrent ? "var(--v-accent)" : "var(--v-border)"), background: "var(--v-surface-2)", padding: "14px 8px 0" }}>
                <span style={{ position: "relative" }}>
                    <Avatar bg={item.avatarBg} content={item.avatarContent} size={52} ring={`0 0 0 3px color-mix(in srgb, ${color} 55%, transparent)`} />
                    {place === 1 && <Crown size={18} style={{ position: "absolute", top: -14, left: "50%", transform: "translateX(-50%)", color }} />}
                </span>
                <span style={{ fontSize: 13, fontWeight: 700, color: "var(--v-text)", textAlign: "center", overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap", maxWidth: "100%" }}>{item.name}</span>
                <span style={{ fontSize: 15, fontWeight: 800, color }}><CountUp value={item.score} /></span>
                {item.subtitle && <span style={{ fontSize: 10, color: "var(--v-text-3)", marginBottom: 6 }}>{item.subtitle}</span>}
                <div style={{ width: "100%", height, borderRadius: "10px 10px 0 0", background: `linear-gradient(180deg, color-mix(in srgb, ${color} 30%, var(--v-surface)), var(--v-surface))`, borderTop: `2px solid ${color}`, display: "flex", alignItems: "flex-start", justifyContent: "center", paddingTop: 8, fontSize: 22, fontWeight: 800, color }}>{place}</div>
            </div>
        </Reveal>
    );
}

function PodiumRow({ item }: { item: PodiumItem }) {
    return (
        <div className="v-row" style={{ display: "flex", alignItems: "center", gap: 12, borderRadius: 12, border: "1px solid " + (item.isCurrent ? "var(--v-accent)" : "var(--v-border)"), background: "var(--v-surface-2)", padding: "10px 14px" }}>
            <span style={{ flexShrink: 0, width: 26, height: 26, borderRadius: 999, display: "inline-flex", alignItems: "center", justifyContent: "center", fontSize: 12, fontWeight: 700, background: "color-mix(in srgb, var(--v-accent) 16%, transparent)", color: "var(--v-accent)" }}>{item.rank}</span>
            <Avatar bg={item.avatarBg} content={item.avatarContent} size={30} />
            <div style={{ flex: 1, minWidth: 0 }}>
                <div style={{ fontSize: 14, color: "var(--v-text)", overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap" }}>{item.name}{item.isCurrent && <span style={{ marginLeft: 8, fontSize: 11, color: "var(--v-accent)" }}>· {item.currentLabel ?? "vous"}</span>}</div>
                {item.subtitle && <div style={{ fontSize: 11.5, color: "var(--v-text-3)" }}>{item.subtitle}</div>}
            </div>
            <span style={{ flexShrink: 0, fontSize: 14, fontWeight: 700, color: "var(--v-accent)" }}><CountUp value={item.score} /> pts</span>
        </div>
    );
}
