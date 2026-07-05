"use client";

import Link from "next/link";
import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { apiFetch } from "@/lib/api";
import type { AssignmentMine } from "@/lib/types";
import { Search, Clock, ChevronRight, Check } from "lucide-react";
import Reveal from "@/components/Reveal";
import { Stagger, StaggerItem } from "@/components/Stagger";

const LEVEL_LABELS: Record<string, string> = {
    beginner: "Débutant",
    intermediate: "Intermédiaire",
    advanced: "Avancé",
    expert: "Expert",
};

const diffBadge = (level?: string): React.CSSProperties => {
    const map: Record<string, React.CSSProperties> = {
        "Débutant": { background: "var(--success-subtle)", color: "var(--success-t)" },
        "Intermédiaire": { background: "var(--warning-subtle)", color: "var(--warning-t)" },
        "Avancé": { background: "var(--danger-subtle)", color: "var(--danger-t)" },
        "Expert": { background: "var(--accent-subtle)", color: "var(--accent)" },
    };
    return map[level ?? "Débutant"] ?? map["Débutant"];
};

const labelColor = (pct: number): string =>
    pct >= 100 ? "var(--success)" : "var(--accent)";

export default function ParcoursList() {
    const [searchQuery, setSearchQuery] = useState("");
    const assignQ = useQuery<AssignmentMine[]>({
        queryKey: ["assignments", "mine"],
        queryFn: () => apiFetch<AssignmentMine[]>("/api/assignments/mine"),
    });

    const parcours = assignQ.data ?? [];
    const filtered = parcours.filter(p =>
        (p.pathTitle ?? "").toLowerCase().includes(searchQuery.toLowerCase())
    );

    return (
        <div style={{ padding: "28px var(--page-x)", minHeight: "100%", maxWidth: 1200, margin: "0 auto" }}>
            {/* Header */}
            <Reveal>
                <div style={{ marginBottom: 24 }}>
                    <h1 style={{ fontSize: 22, fontWeight: 700, color: "var(--text)", margin: "0 0 4px", letterSpacing: "-0.01em" }}>
                        Mes parcours
                    </h1>
                    <p style={{ fontSize: 13.5, color: "var(--text-2)", margin: 0 }}>
                        Tous vos parcours de formation assignés.
                    </p>
                </div>
            </Reveal>

            {/* Filtres */}
            <div style={{ display: "flex", gap: 10, marginBottom: 24, flexWrap: "wrap", alignItems: "center" }}>
                <div style={{ position: "relative", flex: 1, minWidth: 0 }}>
                    <Search size={14} strokeWidth={1.75} color="var(--text-3)" style={{ position: "absolute", left: 11, top: "50%", transform: "translateY(-50%)", pointerEvents: "none" }} />
                    <input
                        placeholder="Rechercher un parcours..."
                        value={searchQuery}
                        onChange={e => setSearchQuery(e.target.value)}
                        style={{
                            width: "100%", height: 36, paddingLeft: 34, paddingRight: 12,
                            background: "var(--surface)", border: "1px solid var(--border)", borderRadius: 8,
                            fontSize: 13.5, color: "var(--text)", outline: "none", fontFamily: "inherit",
                            transition: "border-color 0.15s, box-shadow 0.15s",
                        }}
                        onFocus={e => { e.target.style.borderColor = "var(--accent)"; e.target.style.boxShadow = "0 0 0 3px var(--accent-border)"; }}
                        onBlur={e => { e.target.style.borderColor = "var(--border)"; e.target.style.boxShadow = "none"; }}
                    />
                </div>
            </div>

            {/* Loading */}
            {assignQ.isLoading && (
                <div style={{ display: "flex", flexDirection: "column", gap: 14 }}>
                    {[0, 1].map(i => (
                        <div key={i} className="skel" style={{ height: 100, borderRadius: 12 }} />
                    ))}
                </div>
            )}

            {/* Error */}
            {assignQ.isError && (
                <div style={{ background: "var(--danger-subtle)", color: "var(--danger-t)", border: "1px solid rgba(239,68,68,0.25)", borderRadius: 8, padding: "12px 16px", fontSize: 13.5 }}>
                    {(assignQ.error as Error)?.message || "Erreur de chargement"}
                </div>
            )}

            {/* Liste */}
            <Stagger className="flex flex-col gap-3.5" gap={0.05}>
                {filtered.map(p => {
                    const pct = Math.round(p.progressPercent ?? 0);
                    const done = pct >= 100;
                    const color = labelColor(pct);
                    const level = p.pathLevel ? (LEVEL_LABELS[p.pathLevel] ?? p.pathLevel) : null;
                    const STATUS: Record<string, string> = { assigned: "Assigné", started: "En cours", completed: "Complété" };

                    return (
                        <StaggerItem key={p.pathId}>
                        <div className="transition-colors duration-200" style={{
                            background: "var(--surface)", border: "1px solid var(--border)", borderRadius: 12,
                            padding: "22px 24px",
                            boxShadow: "0 1px 3px rgba(0,0,0,0.06)",
                            display: "flex", alignItems: "center", gap: 20,
                        }}
                            onMouseEnter={e => { (e.currentTarget as HTMLElement).style.borderColor = "var(--accent-border)"; (e.currentTarget as HTMLElement).style.transform = "translateY(-2px)"; }}
                            onMouseLeave={e => { (e.currentTarget as HTMLElement).style.borderColor = "var(--border)"; (e.currentTarget as HTMLElement).style.transform = "none"; }}
                        >
                            <div style={{ flex: 1, minWidth: 0 }}>
                                {/* Badges */}
                                <div style={{ display: "flex", gap: 7, marginBottom: 10, flexWrap: "wrap" }}>
                                    {level && (
                                        <span style={{ ...diffBadge(level), padding: "3px 10px", borderRadius: 99, fontSize: 11.5, fontWeight: 600 }}>
                                            {level}
                                        </span>
                                    )}
                                    <span style={{
                                        ...(done
                                            ? { background: "var(--success-subtle)", color: "var(--success-t)" }
                                            : p.status === "started"
                                                ? { background: "var(--warning-subtle)", color: "var(--warning-t)" }
                                                : { background: "var(--surface-2)", color: "var(--text-3)" }),
                                        padding: "3px 10px", borderRadius: 99, fontSize: 11.5, fontWeight: 600,
                                    }}>
                                        {STATUS[p.status] ?? p.status}
                                    </span>
                                </div>

                                {/* Titre */}
                                <div style={{ fontSize: 15.5, fontWeight: 700, color: "var(--text)", marginBottom: 14, lineHeight: 1.35, overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap", letterSpacing: "-0.01em" }}>
                                    {p.pathTitle ?? p.pathId}
                                </div>

                                {/* Progress bar */}
                                <div style={{ display: "flex", alignItems: "center", gap: 10, marginBottom: 8 }}>
                                    <div style={{ flex: 1, height: 6, background: "var(--surface-2)", borderRadius: 99, overflow: "hidden" }}>
                                        <div style={{ height: 6, borderRadius: 99, background: "linear-gradient(90deg, var(--accent), var(--accent-2))", width: `${pct}%`, transition: "width 0.8s cubic-bezier(0.4,0,0.2,1)" }} />
                                    </div>
                                    <span style={{ fontSize: 12.5, fontWeight: 700, color, minWidth: 36, textAlign: "right", fontFamily: "'JetBrains Mono', monospace" }}>
                                        {pct}%
                                    </span>
                                </div>

                                {/* Date */}
                                <div style={{ display: "flex", alignItems: "center", gap: 5, fontSize: 12, color: "var(--text-3)" }}>
                                    <Clock size={12} strokeWidth={1.75} color="var(--text-3)" />
                                    Assigné le {p.assignedAt ? new Date(p.assignedAt).toLocaleDateString("fr-FR") : "—"}
                                    {p.dueAt && <> · Échéance : {new Date(p.dueAt).toLocaleDateString("fr-FR")}</>}
                                </div>
                            </div>

                            {/* Bouton */}
                            {done ? (
                                <Link href={`/dashboard/parcours/${p.pathId}`} style={{
                                    display: "inline-flex", alignItems: "center", gap: 6, padding: "10px 20px",
                                    background: "var(--success-subtle)", color: "var(--success-t)",
                                    border: "1px solid rgba(34,197,94,0.25)", borderRadius: 8,
                                    textDecoration: "none", fontSize: 13, fontWeight: 600, flexShrink: 0,
                                }}>
                                    <Check size={13} strokeWidth={2.5} color="var(--success)" />
                                    Terminé
                                </Link>
                            ) : (
                                <Link href={`/dashboard/parcours/${p.pathId}`} className="transition-colors duration-200" style={{
                                    display: "inline-flex", alignItems: "center", gap: 6, padding: "10px 20px",
                                    background: "var(--accent)", color: "var(--on-accent)", borderRadius: 8,
                                    textDecoration: "none", fontSize: 13, fontWeight: 600, flexShrink: 0,
                                }}
                                    onMouseEnter={e => { (e.currentTarget as HTMLElement).style.background = "var(--accent-hover)"; (e.currentTarget as HTMLElement).style.transform = "translateY(-1px)"; }}
                                    onMouseLeave={e => { (e.currentTarget as HTMLElement).style.background = "var(--accent)"; (e.currentTarget as HTMLElement).style.transform = "none"; }}
                                >
                                    Continuer
                                    <ChevronRight size={14} strokeWidth={2.5} />
                                </Link>
                            )}
                        </div>
                        </StaggerItem>
                    );
                })}
            </Stagger>

            {/* Empty */}
            {!assignQ.isLoading && filtered.length === 0 && (
                <div style={{
                    display: "flex", flexDirection: "column", alignItems: "center", justifyContent: "center",
                    padding: "64px 24px", background: "var(--surface)", border: "1px solid var(--border)",
                    borderRadius: 12, boxShadow: "0 1px 3px rgba(0,0,0,0.06)", textAlign: "center",
                }}>
                    <div style={{ width: 56, height: 56, background: "var(--surface-2)", borderRadius: 14, display: "flex", alignItems: "center", justifyContent: "center", marginBottom: 16 }}>
                        <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="var(--text-3)" strokeWidth="1.5"><path d="M2 3h6a4 4 0 014 4v14a3 3 0 00-3-3H2z" /><path d="M22 3h-6a4 4 0 00-4 4v14a3 3 0 013-3h7z" /></svg>
                    </div>
                    <div style={{ fontSize: 16, fontWeight: 600, color: "var(--text)", marginBottom: 6 }}>Aucun parcours trouvé</div>
                    <div style={{ fontSize: 13.5, color: "var(--text-2)", maxWidth: 300 }}>Modifiez vos filtres ou contactez votre administrateur.</div>
                </div>
            )}
        </div>
    );
}
