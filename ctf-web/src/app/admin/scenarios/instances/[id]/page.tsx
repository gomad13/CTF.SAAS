"use client";

import Link from "next/link";
import { useParams } from "next/navigation";
import { useScenarioInstance } from "@/lib/hooks/useScenarios";
import { ArrowLeft, Mail, Eye, MousePointerClick, ShieldCheck, Clock, AlertTriangle } from "lucide-react";

export default function InstanceDetailPage() {
    const params = useParams<{ id: string }>();
    const { data, isLoading } = useScenarioInstance(params.id);

    if (isLoading) return <div style={{ padding: 24, color: "#64748B" }}>Chargement…</div>;
    if (!data) return <div style={{ padding: 24, color: "#EF4444" }}>Instance introuvable.</div>;

    return (
        <div style={{ padding: "var(--page-x)", background: "#F8FAFC", minHeight: "100%" }}>
            <Link href="/admin/scenarios/instances" style={{ display: "inline-flex", alignItems: "center", gap: 6, color: "#64748B", fontSize: 13, textDecoration: "none", marginBottom: 12 }}>
                <ArrowLeft size={14} /> Retour
            </Link>

            <div style={{ background: "#FFFFFF", border: "1px solid #E2E8F0", borderRadius: 12, padding: 24, marginBottom: 24 }}>
                <h1 style={{ fontSize: 22, fontWeight: 700, color: "#1E293B", margin: 0 }}>{data.templateName}</h1>
                <p style={{ fontSize: 13, color: "#64748B", margin: "4px 0 16px" }}>{data.category} — Mode {data.mode}</p>

                <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fit, minmax(200px, 1fr))", gap: 12 }}>
                    <Field label="Cible" value={`${data.targetFullName} (${data.targetEmail})`} />
                    <Field label="Expéditeur fictif" value={`${data.senderFullName} (${data.senderEmail})`} />
                    <Field label="Statut" value={data.status} />
                    <Field label="Démarré le" value={data.startedAt ? new Date(data.startedAt).toLocaleString("fr-FR") : "—"} />
                    <Field label="Terminé le" value={data.completedAt ? new Date(data.completedAt).toLocaleString("fr-FR") : "—"} />
                    {data.stopReason && <Field label="Motif arrêt" value={data.stopReason} />}
                </div>
            </div>

            <div style={{ background: "#FFFFFF", border: "1px solid #E2E8F0", borderRadius: 12, padding: 24, marginBottom: 24 }}>
                <h2 style={{ fontSize: 16, fontWeight: 600, color: "#1E293B", margin: "0 0 16px", display: "flex", alignItems: "center", gap: 8 }}><Clock size={18} /> Étapes planifiées</h2>
                <div className="resp-scroll-x">
                <table style={{ width: "100%", borderCollapse: "collapse", minWidth: 620 }}>
                    <thead>
                        <tr style={{ background: "#F1F5F9", textTransform: "uppercase", fontSize: 11, color: "#64748B", letterSpacing: "0.05em" }}>
                            <th style={{ padding: "10px 12px", textAlign: "left", fontWeight: 600 }}>Ordre</th>
                            <th style={{ padding: "10px 12px", textAlign: "left", fontWeight: 600 }}>Step ID</th>
                            <th style={{ padding: "10px 12px", textAlign: "left", fontWeight: 600 }}>Statut</th>
                            <th style={{ padding: "10px 12px", textAlign: "left", fontWeight: 600 }}>Planifié le</th>
                            <th style={{ padding: "10px 12px", textAlign: "left", fontWeight: 600 }}>Envoyé le</th>
                        </tr>
                    </thead>
                    <tbody>
                        {data.steps.map(s => (
                            <tr key={s.id} style={{ borderTop: "1px solid #E2E8F0" }}>
                                <td style={{ padding: "8px 12px", fontSize: 13 }}>{s.stepOrder}</td>
                                <td style={{ padding: "8px 12px", fontSize: 12, fontFamily: "monospace", color: "#475569" }}>{s.stepId}</td>
                                <td style={{ padding: "8px 12px", fontSize: 12 }}>{s.status}</td>
                                <td style={{ padding: "8px 12px", fontSize: 12, color: "#64748B" }}>{new Date(s.scheduledAt).toLocaleString("fr-FR")}</td>
                                <td style={{ padding: "8px 12px", fontSize: 12, color: "#64748B" }}>{s.sentAt ? new Date(s.sentAt).toLocaleString("fr-FR") : "—"}</td>
                            </tr>
                        ))}
                    </tbody>
                </table>
                </div>
            </div>

            <div style={{ background: "#FFFFFF", border: "1px solid #E2E8F0", borderRadius: 12, padding: 24 }}>
                <h2 style={{ fontSize: 16, fontWeight: 600, color: "#1E293B", margin: "0 0 16px", display: "flex", alignItems: "center", gap: 8 }}><Mail size={18} /> Emails envoyés ({data.emails.length})</h2>
                <div style={{ display: "flex", flexDirection: "column", gap: 8 }}>
                    {data.emails.map(e => (
                        <div key={e.id} style={{
                            border: "1px solid #E2E8F0", borderRadius: 8, padding: 12,
                            background: e.isAttackStep ? "rgba(239,68,68,0.04)" : "#FFFFFF",
                        }}>
                            <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between" }}>
                                <div style={{ flex: 1 }}>
                                    <div style={{ fontSize: 13, fontWeight: 500, color: "#1E293B", display: "flex", alignItems: "center", gap: 6 }}>
                                        {e.isAttackStep && <AlertTriangle size={13} color="#EF4444" />}
                                        {e.subject}
                                    </div>
                                    <div style={{ fontSize: 11, color: "#64748B" }}>De {e.fromName} &lt;{e.fromEmail}&gt; · {new Date(e.sentAt).toLocaleString("fr-FR")}</div>
                                </div>
                                <div style={{ display: "flex", gap: 14, fontSize: 11, color: "#64748B" }}>
                                    <span style={{ color: e.firstReadAt ? "#3B82F6" : "#CBD5E1" }} title="Ouvert"><Eye size={14} /></span>
                                    <span style={{ color: e.firstClickAt ? "#EF4444" : "#CBD5E1" }} title="Cliqué"><MousePointerClick size={14} /></span>
                                    <span style={{ color: e.reportedAt ? "#10B981" : "#CBD5E1" }} title="Signalé"><ShieldCheck size={14} /></span>
                                </div>
                            </div>
                        </div>
                    ))}
                </div>
            </div>
        </div>
    );
}

function Field({ label, value }: { label: string; value: string }) {
    return (
        <div style={{ padding: 12, background: "#F8FAFC", borderRadius: 8, border: "1px solid #E2E8F0" }}>
            <div style={{ fontSize: 11, fontWeight: 600, color: "#64748B", textTransform: "uppercase", letterSpacing: "0.05em" }}>{label}</div>
            <div style={{ fontSize: 13, color: "#1E293B", marginTop: 4 }}>{value}</div>
        </div>
    );
}
