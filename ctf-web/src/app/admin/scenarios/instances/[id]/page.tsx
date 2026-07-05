"use client";

import Link from "next/link";
import { useParams } from "next/navigation";
import { motion, useReducedMotion } from "framer-motion";
import { useScenarioInstance } from "@/lib/hooks/useScenarios";
import { ArrowLeft, Mail, Eye, MousePointerClick, ShieldCheck, Clock, AlertTriangle } from "lucide-react";
import Reveal from "@/components/Reveal";
import { Stagger, StaggerItem } from "@/components/Stagger";

export default function InstanceDetailPage() {
    const params = useParams<{ id: string }>();
    const { data, isLoading } = useScenarioInstance(params.id);
    const reduce = useReducedMotion();

    if (isLoading) return <div style={{ padding: 24, color: "var(--text-2)" }}>Chargement…</div>;
    if (!data) return <div style={{ padding: 24, color: "var(--danger)" }}>Instance introuvable.</div>;

    return (
        <div style={{ padding: "var(--page-x)", background: "var(--bg)", minHeight: "100%" }}>
            <Link href="/admin/scenarios/instances" className="transition-colors duration-200" style={{ display: "inline-flex", alignItems: "center", gap: 6, color: "var(--text-2)", fontSize: 13, textDecoration: "none", marginBottom: 12 }}>
                <ArrowLeft size={14} /> Retour
            </Link>

            <Reveal>
                <div style={{ background: "var(--surface)", border: "1px solid var(--border)", borderRadius: 12, padding: 24, marginBottom: 24 }}>
                    <h1 style={{ fontSize: 22, fontWeight: 700, color: "var(--text)", margin: 0 }}>{data.templateName}</h1>
                    <p style={{ fontSize: 13, color: "var(--text-2)", margin: "4px 0 16px" }}>{data.category} — Mode {data.mode}</p>

                    <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fit, minmax(200px, 1fr))", gap: 12 }}>
                        <Field label="Cible" value={`${data.targetFullName} (${data.targetEmail})`} />
                        <Field label="Expéditeur fictif" value={`${data.senderFullName} (${data.senderEmail})`} />
                        <Field label="Statut" value={data.status} />
                        <Field label="Démarré le" value={data.startedAt ? new Date(data.startedAt).toLocaleString("fr-FR") : "—"} />
                        <Field label="Terminé le" value={data.completedAt ? new Date(data.completedAt).toLocaleString("fr-FR") : "—"} />
                        {data.stopReason && <Field label="Motif arrêt" value={data.stopReason} />}
                    </div>
                </div>
            </Reveal>

            <Reveal delay={0.05}>
                <div style={{ background: "var(--surface)", border: "1px solid var(--border)", borderRadius: 12, padding: 24, marginBottom: 24 }}>
                    <h2 style={{ fontSize: 16, fontWeight: 600, color: "var(--text)", margin: "0 0 16px", display: "flex", alignItems: "center", gap: 8 }}><Clock size={18} /> Étapes planifiées</h2>
                    <div className="resp-scroll-x overflow-x-auto">
                    <table style={{ width: "100%", borderCollapse: "collapse", minWidth: 620 }}>
                        <thead>
                            <tr style={{ background: "var(--surface-2)", textTransform: "uppercase", fontSize: 11, color: "var(--text-2)", letterSpacing: "0.05em" }}>
                                <th style={{ padding: "10px 12px", textAlign: "left", fontWeight: 600 }}>Ordre</th>
                                <th style={{ padding: "10px 12px", textAlign: "left", fontWeight: 600 }}>Step ID</th>
                                <th style={{ padding: "10px 12px", textAlign: "left", fontWeight: 600 }}>Statut</th>
                                <th style={{ padding: "10px 12px", textAlign: "left", fontWeight: 600 }}>Planifié le</th>
                                <th style={{ padding: "10px 12px", textAlign: "left", fontWeight: 600 }}>Envoyé le</th>
                            </tr>
                        </thead>
                        <tbody>
                            {data.steps.map((s, idx) => (
                                <motion.tr key={s.id}
                                    initial={reduce ? false : { opacity: 0, y: 6 }}
                                    whileInView={reduce ? undefined : { opacity: 1, y: 0 }}
                                    viewport={{ once: true, margin: "-20px" }}
                                    transition={{ duration: 0.25, delay: Math.min(idx * 0.03, 0.4) }}
                                    style={{ borderTop: "1px solid var(--border)" }}>
                                    <td style={{ padding: "8px 12px", fontSize: 13, color: "var(--text)" }}>{s.stepOrder}</td>
                                    <td style={{ padding: "8px 12px", fontSize: 12, fontFamily: "monospace", color: "var(--text-2)" }}>{s.stepId}</td>
                                    <td style={{ padding: "8px 12px", fontSize: 12, color: "var(--text)" }}>{s.status}</td>
                                    <td style={{ padding: "8px 12px", fontSize: 12, color: "var(--text-2)" }}>{new Date(s.scheduledAt).toLocaleString("fr-FR")}</td>
                                    <td style={{ padding: "8px 12px", fontSize: 12, color: "var(--text-2)" }}>{s.sentAt ? new Date(s.sentAt).toLocaleString("fr-FR") : "—"}</td>
                                </motion.tr>
                            ))}
                        </tbody>
                    </table>
                    </div>
                </div>
            </Reveal>

            <Reveal delay={0.1}>
                <div style={{ background: "var(--surface)", border: "1px solid var(--border)", borderRadius: 12, padding: 24 }}>
                    <h2 style={{ fontSize: 16, fontWeight: 600, color: "var(--text)", margin: "0 0 16px", display: "flex", alignItems: "center", gap: 8 }}><Mail size={18} /> Emails envoyés ({data.emails.length})</h2>
                    <Stagger className="flex flex-col gap-2" gap={0.04}>
                        {data.emails.map(e => (
                            <StaggerItem key={e.id}>
                                <div style={{
                                    border: "1px solid var(--border)", borderRadius: 8, padding: 12,
                                    background: e.isAttackStep ? "var(--danger-subtle)" : "var(--surface)",
                                }}>
                                    <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between" }}>
                                        <div style={{ flex: 1 }}>
                                            <div style={{ fontSize: 13, fontWeight: 500, color: "var(--text)", display: "flex", alignItems: "center", gap: 6 }}>
                                                {e.isAttackStep && <AlertTriangle size={13} color="var(--danger)" />}
                                                {e.subject}
                                            </div>
                                            <div style={{ fontSize: 11, color: "var(--text-2)" }}>De {e.fromName} &lt;{e.fromEmail}&gt; · {new Date(e.sentAt).toLocaleString("fr-FR")}</div>
                                        </div>
                                        <div style={{ display: "flex", gap: 14, fontSize: 11, color: "var(--text-2)" }}>
                                            <span style={{ color: e.firstReadAt ? "var(--accent)" : "var(--text-3)" }} title="Ouvert"><Eye size={14} /></span>
                                            <span style={{ color: e.firstClickAt ? "var(--danger)" : "var(--text-3)" }} title="Cliqué"><MousePointerClick size={14} /></span>
                                            <span style={{ color: e.reportedAt ? "var(--success)" : "var(--text-3)" }} title="Signalé"><ShieldCheck size={14} /></span>
                                        </div>
                                    </div>
                                </div>
                            </StaggerItem>
                        ))}
                    </Stagger>
                </div>
            </Reveal>
        </div>
    );
}

function Field({ label, value }: { label: string; value: string }) {
    return (
        <div style={{ padding: 12, background: "var(--surface-2)", borderRadius: 8, border: "1px solid var(--border)" }}>
            <div style={{ fontSize: 11, fontWeight: 600, color: "var(--text-2)", textTransform: "uppercase", letterSpacing: "0.05em" }}>{label}</div>
            <div style={{ fontSize: 13, color: "var(--text)", marginTop: 4 }}>{value}</div>
        </div>
    );
}
