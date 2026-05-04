"use client";

import { useState } from "react";
import Link from "next/link";
import { useScenarioInstances, useStopInstance } from "@/lib/hooks/useScenarios";
import { ArrowLeft, Theater, Square, Eye, Mail, MousePointerClick, ShieldCheck } from "lucide-react";
import type { ScenarioInstanceListItem } from "@/lib/types/scenarios";

const STATUS_TONES: Record<string, { bg: string; fg: string; label: string }> = {
    scheduled: { bg: "rgba(245,158,11,0.10)", fg: "#F59E0B", label: "Planifié" },
    running: { bg: "rgba(59,130,246,0.10)", fg: "#3B82F6", label: "En cours" },
    completed: { bg: "rgba(16,185,129,0.10)", fg: "#10B981", label: "Terminé" },
    stopped: { bg: "rgba(100,116,139,0.10)", fg: "#64748B", label: "Stoppé" },
    failed: { bg: "rgba(239,68,68,0.10)", fg: "#EF4444", label: "Échec" },
};

export default function InstancesPage() {
    const { data, isLoading, refetch } = useScenarioInstances(true);
    const [stoppingId, setStoppingId] = useState<string | null>(null);
    const [stopReason, setStopReason] = useState("");
    const { stop, isLoading: stopping } = useStopInstance();

    async function confirmStop() {
        if (!stoppingId) return;
        const ok = await stop(stoppingId, stopReason || "manual_admin_stop");
        setStoppingId(null);
        setStopReason("");
        if (ok) refetch();
    }

    return (
        <div style={{ padding: "32px 24px", background: "#F8FAFC", minHeight: "100%" }}>
            <Link href="/admin/scenarios" style={{ display: "inline-flex", alignItems: "center", gap: 6, color: "#64748B", fontSize: 13, textDecoration: "none", marginBottom: 12 }}>
                <ArrowLeft size={14} /> Retour catalogue
            </Link>
            <div style={{ marginBottom: 24, display: "flex", alignItems: "flex-end", justifyContent: "space-between" }}>
                <div>
                    <h1 style={{ fontSize: 24, fontWeight: 700, color: "#1E293B", margin: 0 }}>Instances de scénarios</h1>
                    <p style={{ color: "#64748B", fontSize: 14, marginTop: 4 }}>Suivi temps réel — actualisation toutes les 10 secondes.</p>
                </div>
            </div>

            <div style={{ background: "#FFFFFF", border: "1px solid #E2E8F0", borderRadius: 12, overflow: "hidden" }}>
                <table style={{ width: "100%", borderCollapse: "collapse" }}>
                    <thead>
                        <tr style={{ background: "#F1F5F9", textTransform: "uppercase", fontSize: 11, color: "#64748B", letterSpacing: "0.05em" }}>
                            <th style={{ padding: "12px 16px", textAlign: "left", fontWeight: 600 }}>Scénario</th>
                            <th style={{ padding: "12px 16px", textAlign: "left", fontWeight: 600 }}>Cible</th>
                            <th style={{ padding: "12px 16px", textAlign: "left", fontWeight: 600 }}>Statut</th>
                            <th style={{ padding: "12px 16px", textAlign: "center", fontWeight: 600 }}>Mode</th>
                            <th style={{ padding: "12px 16px", textAlign: "center", fontWeight: 600 }}>Tracking</th>
                            <th style={{ padding: "12px 16px", textAlign: "right", fontWeight: 600 }}>Actions</th>
                        </tr>
                    </thead>
                    <tbody>
                        {isLoading && <tr><td colSpan={6} style={{ padding: 24, textAlign: "center", color: "#64748B" }}>Chargement…</td></tr>}
                        {!isLoading && (data ?? []).length === 0 && <tr><td colSpan={6} style={{ padding: 24, textAlign: "center", color: "#64748B" }}>Aucune instance.</td></tr>}
                        {(data ?? []).map(i => <Row key={i.id} item={i} onStop={() => setStoppingId(i.id)} />)}
                    </tbody>
                </table>
            </div>

            {stoppingId && (
                <div style={{
                    position: "fixed", inset: 0, zIndex: 50,
                    background: "rgba(15,23,42,0.6)", display: "flex", alignItems: "center", justifyContent: "center",
                }} onClick={() => setStoppingId(null)}>
                    <div style={{
                        background: "#FFFFFF", borderRadius: 12, padding: 24, maxWidth: 480, width: "100%",
                        boxShadow: "0 20px 50px rgba(0,0,0,0.3)",
                    }} onClick={e => e.stopPropagation()}>
                        <h3 style={{ fontSize: 18, fontWeight: 700, color: "#1E293B", margin: "0 0 8px" }}>{"Arrêter l'instance ?"}</h3>
                        <p style={{ fontSize: 13, color: "#64748B", margin: "0 0 16px" }}>{"Les emails restants ne seront pas envoyés. La cible recevra une notification système l'informant de l'interruption."}</p>
                        <label style={{ display: "block", fontSize: 12, fontWeight: 600, color: "#64748B", marginBottom: 6 }}>Motif (optionnel)</label>
                        <input
                            type="text" value={stopReason} onChange={e => setStopReason(e.target.value)}
                            placeholder="Ex. employé en congé"
                            style={{ width: "100%", padding: "10px 12px", border: "1px solid #E2E8F0", borderRadius: 6, fontSize: 13 }}
                        />
                        <div style={{ display: "flex", gap: 8, justifyContent: "flex-end", marginTop: 16 }}>
                            <button onClick={() => setStoppingId(null)} style={{ padding: "10px 16px", background: "#FFFFFF", border: "1px solid #E2E8F0", borderRadius: 8, fontSize: 14, color: "#334155", cursor: "pointer" }}>Annuler</button>
                            <button onClick={confirmStop} disabled={stopping} style={{ padding: "10px 16px", background: "#EF4444", border: "none", borderRadius: 8, fontSize: 14, color: "white", cursor: stopping ? "wait" : "pointer", fontWeight: 500 }}>{stopping ? "Arrêt…" : "Arrêter"}</button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
}

function Row({ item, onStop }: { item: ScenarioInstanceListItem; onStop: () => void }) {
    const tone = STATUS_TONES[item.status] ?? STATUS_TONES.scheduled;
    const canStop = item.status === "running" || item.status === "scheduled";
    return (
        <tr style={{ borderTop: "1px solid #E2E8F0", transition: "background 0.2s" }}
            onMouseEnter={e => (e.currentTarget as HTMLElement).style.background = "#F8FAFC"}
            onMouseLeave={e => (e.currentTarget as HTMLElement).style.background = "transparent"}>
            <td style={{ padding: "12px 16px" }}>
                <div style={{ display: "flex", alignItems: "center", gap: 10 }}>
                    <div style={{ width: 32, height: 32, borderRadius: 6, background: "rgba(59,130,246,0.10)", display: "flex", alignItems: "center", justifyContent: "center" }}>
                        <Theater size={16} color="#3B82F6" strokeWidth={1.75} />
                    </div>
                    <div>
                        <div style={{ fontSize: 13, fontWeight: 500, color: "#1E293B" }}>{item.templateName}</div>
                        <div style={{ fontSize: 11, color: "#64748B" }}>{item.category}</div>
                    </div>
                </div>
            </td>
            <td style={{ padding: "12px 16px" }}>
                <div style={{ fontSize: 13, color: "#1E293B" }}>{item.targetFullName}</div>
                <div style={{ fontSize: 11, color: "#64748B" }}>{item.targetEmail}</div>
            </td>
            <td style={{ padding: "12px 16px" }}>
                <span style={{ fontSize: 12, fontWeight: 500, padding: "4px 10px", borderRadius: 99, background: tone.bg, color: tone.fg }}>{tone.label}</span>
                {item.stopReason && item.status !== "running" && item.status !== "scheduled" && (
                    <div style={{ fontSize: 10, color: "#94A3B8", marginTop: 4 }}>{item.stopReason}</div>
                )}
            </td>
            <td style={{ padding: "12px 16px", textAlign: "center", fontSize: 12, color: "#64748B" }}>
                {item.mode === "demo" ? "Démo" : "Normal"}
            </td>
            <td style={{ padding: "12px 16px", textAlign: "center" }}>
                <div style={{ display: "inline-flex", gap: 12, fontSize: 12, color: "#64748B" }}>
                    <span title="Emails envoyés" style={{ display: "inline-flex", alignItems: "center", gap: 3 }}><Mail size={13} />{item.emailsSent}</span>
                    <span title="Ouvertures" style={{ display: "inline-flex", alignItems: "center", gap: 3 }}><Eye size={13} />{item.openedCount}</span>
                    <span title="Clics" style={{ display: "inline-flex", alignItems: "center", gap: 3, color: item.clickedCount > 0 ? "#EF4444" : "#64748B" }}><MousePointerClick size={13} />{item.clickedCount}</span>
                    <span title="Signalements" style={{ display: "inline-flex", alignItems: "center", gap: 3, color: item.reportedCount > 0 ? "#10B981" : "#64748B" }}><ShieldCheck size={13} />{item.reportedCount}</span>
                </div>
            </td>
            <td style={{ padding: "12px 16px", textAlign: "right" }}>
                <Link href={`/admin/scenarios/instances/${item.id}`} style={{ fontSize: 12, color: "#3B82F6", textDecoration: "none", marginRight: 8 }}>Détails</Link>
                {canStop && (
                    <button onClick={onStop} style={{
                        display: "inline-flex", alignItems: "center", gap: 4,
                        padding: "6px 10px", border: "1px solid #FCA5A5", background: "#FEF2F2",
                        borderRadius: 6, color: "#B91C1C", fontSize: 11, cursor: "pointer",
                    }}><Square size={12} /> Stop</button>
                )}
            </td>
        </tr>
    );
}
