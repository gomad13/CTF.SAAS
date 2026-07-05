"use client";

import { useState } from "react";
import Link from "next/link";
import { motion, useReducedMotion } from "framer-motion";
import { useScenarioInstances, useStopInstance } from "@/lib/hooks/useScenarios";
import { ArrowLeft, Theater, Square, Eye, Mail, MousePointerClick, ShieldCheck } from "lucide-react";
import type { ScenarioInstanceListItem } from "@/lib/types/scenarios";
import Reveal from "@/components/Reveal";

const STATUS_TONES: Record<string, { bg: string; fg: string; label: string }> = {
    scheduled: { bg: "var(--warning-subtle)", fg: "var(--warning)", label: "Planifié" },
    running: { bg: "var(--accent-subtle)", fg: "var(--accent)", label: "En cours" },
    completed: { bg: "var(--success-subtle)", fg: "var(--success)", label: "Terminé" },
    stopped: { bg: "var(--surface-2)", fg: "var(--text-2)", label: "Stoppé" },
    failed: { bg: "var(--danger-subtle)", fg: "var(--danger)", label: "Échec" },
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
        <div style={{ padding: "var(--page-x)", background: "var(--bg)", minHeight: "100%" }}>
            <Link href="/admin/scenarios" className="transition-colors duration-200" style={{ display: "inline-flex", alignItems: "center", gap: 6, color: "var(--text-2)", fontSize: 13, textDecoration: "none", marginBottom: 12 }}>
                <ArrowLeft size={14} /> Retour catalogue
            </Link>
            <Reveal>
                <div style={{ marginBottom: 24, display: "flex", alignItems: "flex-end", justifyContent: "space-between" }}>
                    <div>
                        <h1 style={{ fontSize: 24, fontWeight: 700, color: "var(--text)", margin: 0 }}>Instances de scénarios</h1>
                        <p style={{ color: "var(--text-2)", fontSize: 14, marginTop: 4 }}>Suivi temps réel — actualisation toutes les 10 secondes.</p>
                    </div>
                </div>
            </Reveal>

            <div className="resp-scroll-x overflow-x-auto" style={{ background: "var(--surface)", border: "1px solid var(--border)", borderRadius: 12 }}>
                <table style={{ width: "100%", borderCollapse: "collapse", minWidth: 720 }}>
                    <thead>
                        <tr style={{ background: "var(--surface-2)", textTransform: "uppercase", fontSize: 11, color: "var(--text-2)", letterSpacing: "0.05em" }}>
                            <th style={{ padding: "12px 16px", textAlign: "left", fontWeight: 600 }}>Scénario</th>
                            <th style={{ padding: "12px 16px", textAlign: "left", fontWeight: 600 }}>Cible</th>
                            <th style={{ padding: "12px 16px", textAlign: "left", fontWeight: 600 }}>Statut</th>
                            <th style={{ padding: "12px 16px", textAlign: "center", fontWeight: 600 }}>Mode</th>
                            <th style={{ padding: "12px 16px", textAlign: "center", fontWeight: 600 }}>Tracking</th>
                            <th style={{ padding: "12px 16px", textAlign: "right", fontWeight: 600 }}>Actions</th>
                        </tr>
                    </thead>
                    <tbody>
                        {isLoading && <tr><td colSpan={6} style={{ padding: 24, textAlign: "center", color: "var(--text-2)" }}>Chargement…</td></tr>}
                        {!isLoading && (data ?? []).length === 0 && <tr><td colSpan={6} style={{ padding: 24, textAlign: "center", color: "var(--text-2)" }}>Aucune instance.</td></tr>}
                        {(data ?? []).map((i, idx) => <Row key={i.id} item={i} index={idx} onStop={() => setStoppingId(i.id)} />)}
                    </tbody>
                </table>
            </div>

            {stoppingId && (
                <div style={{
                    position: "fixed", inset: 0, zIndex: 50,
                    background: "rgba(0,0,0,0.6)", display: "flex", alignItems: "center", justifyContent: "center", padding: 16,
                }} onClick={() => setStoppingId(null)}>
                    <div style={{
                        background: "var(--surface)", borderRadius: 12, padding: 24, maxWidth: 480, width: "100%",
                        boxShadow: "0 20px 50px rgba(0,0,0,0.3)",
                    }} onClick={e => e.stopPropagation()}>
                        <h3 style={{ fontSize: 18, fontWeight: 700, color: "var(--text)", margin: "0 0 8px" }}>{"Arrêter l'instance ?"}</h3>
                        <p style={{ fontSize: 13, color: "var(--text-2)", margin: "0 0 16px" }}>{"Les emails restants ne seront pas envoyés. La cible recevra une notification système l'informant de l'interruption."}</p>
                        <label style={{ display: "block", fontSize: 12, fontWeight: 600, color: "var(--text-2)", marginBottom: 6 }}>Motif (optionnel)</label>
                        <input
                            type="text" value={stopReason} onChange={e => setStopReason(e.target.value)}
                            placeholder="Ex. employé en congé"
                            style={{ width: "100%", padding: "10px 12px", border: "1px solid var(--border)", borderRadius: 6, fontSize: 13, background: "var(--bg)", color: "var(--text)" }}
                        />
                        <div style={{ display: "flex", gap: 8, justifyContent: "flex-end", marginTop: 16 }}>
                            <button onClick={() => setStoppingId(null)} className="transition-colors duration-200" style={{ padding: "10px 16px", background: "var(--surface)", border: "1px solid var(--border)", borderRadius: 8, fontSize: 14, color: "var(--text-2)", cursor: "pointer" }}>Annuler</button>
                            <button onClick={confirmStop} disabled={stopping} className="transition-colors duration-200" style={{ padding: "10px 16px", background: "var(--danger)", border: "none", borderRadius: 8, fontSize: 14, color: "var(--on-accent)", cursor: stopping ? "wait" : "pointer", fontWeight: 500 }}>{stopping ? "Arrêt…" : "Arrêter"}</button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
}

function Row({ item, index, onStop }: { item: ScenarioInstanceListItem; index: number; onStop: () => void }) {
    const reduce = useReducedMotion();
    const tone = STATUS_TONES[item.status] ?? STATUS_TONES.scheduled;
    const canStop = item.status === "running" || item.status === "scheduled";
    return (
        <motion.tr
            initial={reduce ? false : { opacity: 0, y: 6 }}
            whileInView={reduce ? undefined : { opacity: 1, y: 0 }}
            viewport={{ once: true, margin: "-20px" }}
            transition={{ duration: 0.25, delay: Math.min(index * 0.03, 0.4) }}
            className="transition-colors duration-200"
            style={{ borderTop: "1px solid var(--border)" }}
            onMouseEnter={e => (e.currentTarget as HTMLElement).style.background = "var(--surface-2)"}
            onMouseLeave={e => (e.currentTarget as HTMLElement).style.background = "transparent"}>
            <td style={{ padding: "12px 16px" }}>
                <div style={{ display: "flex", alignItems: "center", gap: 10 }}>
                    <div style={{ width: 32, height: 32, borderRadius: 6, background: "var(--accent-subtle)", display: "flex", alignItems: "center", justifyContent: "center" }}>
                        <Theater size={16} color="var(--accent)" strokeWidth={1.75} />
                    </div>
                    <div>
                        <div style={{ fontSize: 13, fontWeight: 500, color: "var(--text)" }}>{item.templateName}</div>
                        <div style={{ fontSize: 11, color: "var(--text-2)" }}>{item.category}</div>
                    </div>
                </div>
            </td>
            <td style={{ padding: "12px 16px" }}>
                <div style={{ fontSize: 13, color: "var(--text)" }}>{item.targetFullName}</div>
                <div style={{ fontSize: 11, color: "var(--text-2)" }}>{item.targetEmail}</div>
            </td>
            <td style={{ padding: "12px 16px" }}>
                <span style={{ fontSize: 12, fontWeight: 500, padding: "4px 10px", borderRadius: 99, background: tone.bg, color: tone.fg }}>{tone.label}</span>
                {item.stopReason && item.status !== "running" && item.status !== "scheduled" && (
                    <div style={{ fontSize: 10, color: "var(--text-3)", marginTop: 4 }}>{item.stopReason}</div>
                )}
            </td>
            <td style={{ padding: "12px 16px", textAlign: "center", fontSize: 12, color: "var(--text-2)" }}>
                {item.mode === "demo" ? "Démo" : "Normal"}
            </td>
            <td style={{ padding: "12px 16px", textAlign: "center" }}>
                <div style={{ display: "inline-flex", gap: 12, fontSize: 12, color: "var(--text-2)" }}>
                    <span title="Emails envoyés" style={{ display: "inline-flex", alignItems: "center", gap: 3 }}><Mail size={13} />{item.emailsSent}</span>
                    <span title="Ouvertures" style={{ display: "inline-flex", alignItems: "center", gap: 3 }}><Eye size={13} />{item.openedCount}</span>
                    <span title="Clics" style={{ display: "inline-flex", alignItems: "center", gap: 3, color: item.clickedCount > 0 ? "var(--danger)" : "var(--text-2)" }}><MousePointerClick size={13} />{item.clickedCount}</span>
                    <span title="Signalements" style={{ display: "inline-flex", alignItems: "center", gap: 3, color: item.reportedCount > 0 ? "var(--success)" : "var(--text-2)" }}><ShieldCheck size={13} />{item.reportedCount}</span>
                </div>
            </td>
            <td style={{ padding: "12px 16px", textAlign: "right" }}>
                <Link href={`/admin/scenarios/instances/${item.id}`} className="transition-colors duration-200" style={{ fontSize: 12, color: "var(--accent)", textDecoration: "none", marginRight: 8 }}>Détails</Link>
                {canStop && (
                    <button onClick={onStop} className="transition-colors duration-200" style={{
                        display: "inline-flex", alignItems: "center", gap: 4,
                        padding: "6px 10px", border: "1px solid var(--danger)", background: "var(--danger-subtle)",
                        borderRadius: 6, color: "var(--danger-t)", fontSize: 11, cursor: "pointer",
                    }}><Square size={12} /> Stop</button>
                )}
            </td>
        </motion.tr>
    );
}
