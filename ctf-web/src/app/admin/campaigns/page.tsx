"use client";

import { useMemo, useState } from "react";
import Link from "next/link";
import { Target, Plus, Trash2, FolderOpen, Mail, Calendar, Users, BarChart3, ShieldAlert, ShieldCheck, TrendingUp } from "lucide-react";
import { toast } from "@/components/Toast";
import {
    useAvailableContent,
    useCampaigns,
    useCampaignsStatus,
    useCreateCampaign,
    useDeleteCampaign,
    useCampaignsEfficacy,
    useCampaignEfficacy,
} from "@/lib/hooks/useCampaigns";
import {
    type CampaignContentType,
    type CampaignStatus,
} from "@/lib/types/campaigns";
import Reveal from "@/components/Reveal";
import { Stagger, StaggerItem } from "@/components/Stagger";
import CountUp from "@/components/CountUp";
import VisionCard from "@/components/vision/VisionCard";
import VisionKpiCard from "@/components/vision/VisionKpiCard";
import VisionAreaChart from "@/components/vision/VisionAreaChart";
import VisionBarChart from "@/components/vision/VisionBarChart";
import { VisionInput, VisionButton, VisionSelect } from "@/components/vision/VisionForm";

type StatusFilter = "All" | CampaignStatus;

// Statut → couleur token Vision (map locale, ne touche pas STATUS_STYLES partagé).
function statusVision(s: CampaignStatus): { label: string; color: string } {
    if (s === "Active") return { label: "En cours", color: "var(--v-success)" };
    if (s === "Completed") return { label: "Terminée", color: "var(--v-cyan)" };
    return { label: "À venir", color: "var(--v-text-2)" };
}
function StatusPill({ status }: { status: CampaignStatus }) {
    const { label, color } = statusVision(status);
    return <span style={{ display: "inline-flex", borderRadius: 999, padding: "2px 9px", fontSize: 10, fontWeight: 700, textTransform: "uppercase", letterSpacing: "0.04em", whiteSpace: "nowrap", background: "color-mix(in srgb, " + color + " 15%, transparent)", color, border: "1px solid color-mix(in srgb, " + color + " 35%, transparent)" }}>{label}</span>;
}

function Chip({ active, onClick, icon, label }: { active: boolean; onClick: () => void; icon?: React.ReactNode; label: string }) {
    const [h, setH] = useState(false);
    return (
        <button type="button" onClick={onClick} onMouseEnter={() => setH(true)} onMouseLeave={() => setH(false)}
            style={{
                display: "inline-flex", alignItems: "center", gap: 6, borderRadius: 999, padding: "6px 12px", fontSize: 12.5, fontWeight: 600, cursor: "pointer",
                border: "1px solid " + (active ? "var(--v-accent)" : "var(--v-border)"),
                background: active ? "color-mix(in srgb, var(--v-accent) 20%, transparent)" : (h ? "var(--v-surface-2)" : "transparent"),
                color: active ? "var(--v-accent)" : "var(--v-text-2)",
                transition: "background-color .15s ease, border-color .15s ease, color .15s ease, transform .12s ease",
                transform: h && !active ? "translateY(-1px)" : "none",
            }}>
            {icon} {label}
        </button>
    );
}

const labelStyle: React.CSSProperties = { fontSize: 12.5, fontWeight: 600, color: "var(--v-text)" };

export default function CampaignsPage() {
    const statusQ = useCampaignsStatus();
    const enabled = statusQ.data?.isEnabled === true;

    const [filter, setFilter] = useState<StatusFilter>("All");
    const listQ = useCampaigns({ status: filter });
    const contentsQ = useAvailableContent();

    const [name, setName] = useState("");
    const [description, setDescription] = useState("");
    const [startDate, setStartDate] = useState("");
    const [endDate, setEndDate] = useState("");
    const [selectedContents, setSelectedContents] = useState<{ contentType: CampaignContentType; contentId: string }[]>([]);
    const [assignToWholeTenant, setAssignToWholeTenant] = useState(true);

    const createM = useCreateCampaign();
    const deleteM = useDeleteCampaign();

    const validation = useMemo(() => {
        if (!name.trim()) return { ok: false, msg: "Nom requis." };
        if (!startDate || !endDate) return { ok: false, msg: "Dates requises." };
        const start = new Date(startDate);
        const end = new Date(endDate);
        if (Number.isNaN(start.getTime()) || Number.isNaN(end.getTime())) return { ok: false, msg: "Dates invalides." };
        if (end <= start) return { ok: false, msg: "La date de fin doit être après la date de début." };
        if (selectedContents.length === 0) return { ok: false, msg: "Au moins un contenu requis." };
        return { ok: true, msg: "" };
    }, [name, startDate, endDate, selectedContents]);

    function toggleContent(item: { contentType: CampaignContentType; contentId: string }) {
        setSelectedContents(prev => {
            const exists = prev.some(p => p.contentId === item.contentId && p.contentType === item.contentType);
            return exists
                ? prev.filter(p => !(p.contentId === item.contentId && p.contentType === item.contentType))
                : [...prev, item];
        });
    }

    function reset() {
        setName(""); setDescription(""); setStartDate(""); setEndDate("");
        setSelectedContents([]); setAssignToWholeTenant(true);
    }

    async function handleCreate() {
        if (!validation.ok) return;
        try {
            await createM.mutateAsync({
                name: name.trim(),
                description: description.trim() || null,
                startDate: new Date(startDate).toISOString(),
                endDate: new Date(endDate).toISOString(),
                contents: selectedContents.map((c, idx) => ({ ...c, displayOrder: idx })),
                assignToWholeTenant,
                assignedUserIds: null,
            });
            toast.ok("Campagne créée");
            reset();
        } catch (e) {
            toast.er(e instanceof Error ? e.message : "Erreur création");
        }
    }

    async function handleDelete(id: string, label: string) {
        if (!confirm(`Supprimer la campagne « ${label} » ?`)) return;
        try {
            await deleteM.mutateAsync(id);
            toast.ok("Campagne supprimée");
        } catch (e) {
            toast.er(e instanceof Error ? e.message : "Erreur suppression");
        }
    }

    if (statusQ.isLoading) {
        return <div className="vision-dashboard" style={{ minHeight: "100%" }}><div style={{ padding: "48px 20px", textAlign: "center", color: "var(--v-text-2)" }}>Chargement…</div></div>;
    }

    if (!enabled) {
        return (
            <div className="vision-dashboard" style={{ minHeight: "100%" }}>
                <div style={{ maxWidth: 720, margin: "0 auto", padding: "48px 20px", textAlign: "center" }}>
                    <div style={{ margin: "0 auto", display: "flex", height: 56, width: 56, alignItems: "center", justifyContent: "center", borderRadius: 999, background: "var(--v-surface-2)", color: "var(--v-text-2)" }}><Target size={22} /></div>
                    <h1 style={{ marginTop: 16, fontSize: 20, fontWeight: 700, color: "var(--v-text)" }}>Campagnes désactivées</h1>
                    <p style={{ marginTop: 8, fontSize: 14, color: "var(--v-text-2)" }}>Activez le mode « Campagnes » depuis les paramètres pour orchestrer parcours et scénarios sur une période donnée.</p>
                </div>
            </div>
        );
    }

    const list = listQ.data ?? [];
    const contents = contentsQ.data ?? [];
    const paths = contents.filter(c => c.contentType === "Parcours");
    const scenarios = contents.filter(c => c.contentType === "Scenario");

    return (
        <div className="vision-dashboard" style={{ minHeight: "100%" }}>
            <div style={{ maxWidth: 1120, margin: "0 auto", padding: "24px 20px 80px", display: "flex", flexDirection: "column", gap: 20 }}>
                <Reveal>
                    <div>
                        <h1 style={{ fontSize: 26, fontWeight: 700, color: "var(--v-text)", letterSpacing: "-0.02em" }}>Campagnes</h1>
                        <p style={{ marginTop: 6, fontSize: 13.5, color: "var(--v-text-2)", lineHeight: 1.55 }}>Programmes de sensibilisation time-boxed qui combinent parcours et scénarios.</p>
                    </div>
                </Reveal>

                {/* ── Formulaire ── */}
                <Reveal>
                    <VisionCard>
                        <h2 style={{ fontSize: 13, fontWeight: 700, textTransform: "uppercase", letterSpacing: "0.05em", color: "var(--v-text-2)" }}>Nouvelle campagne</h2>
                        <Stagger className="flex flex-col gap-4" gap={0.05}>
                            <StaggerItem>
                                <div className="grid grid-cols-1 gap-3 md:grid-cols-2" style={{ marginTop: 16 }}>
                                    <div style={{ display: "flex", flexDirection: "column", gap: 6 }}>
                                        <span style={labelStyle}>Nom</span>
                                        <VisionInput placeholder="Sensibilisation T2 2026" value={name} onChange={e => setName(e.target.value)} maxLength={200} />
                                    </div>
                                    <div style={{ display: "flex", flexDirection: "column", gap: 6 }}>
                                        <span style={labelStyle}>Description</span>
                                        <VisionInput placeholder="Optionnel — contexte / objectifs" value={description} onChange={e => setDescription(e.target.value)} maxLength={2000} />
                                    </div>
                                    <div style={{ display: "flex", flexDirection: "column", gap: 6 }}>
                                        <span style={labelStyle}>Date de début</span>
                                        <VisionInput type="date" value={startDate} onChange={e => setStartDate(e.target.value)} style={{ colorScheme: "dark" }} />
                                    </div>
                                    <div style={{ display: "flex", flexDirection: "column", gap: 6 }}>
                                        <span style={labelStyle}>Date de fin</span>
                                        <VisionInput type="date" value={endDate} onChange={e => setEndDate(e.target.value)} style={{ colorScheme: "dark" }} />
                                    </div>
                                </div>
                            </StaggerItem>

                            <StaggerItem>
                                <div style={{ display: "flex", flexDirection: "column", gap: 6 }}>
                                    <span style={labelStyle}>Parcours inclus</span>
                                    <div style={{ display: "flex", flexWrap: "wrap", gap: 8 }}>
                                        {paths.length === 0 && <span style={{ fontSize: 12, color: "var(--v-text-2)" }}>Aucun parcours disponible.</span>}
                                        {paths.map(p => (
                                            <Chip key={p.contentId} active={selectedContents.some(c => c.contentId === p.contentId)}
                                                onClick={() => toggleContent({ contentType: "Parcours", contentId: p.contentId })}
                                                icon={<FolderOpen size={12} />} label={p.title} />
                                        ))}
                                    </div>
                                </div>
                            </StaggerItem>

                            <StaggerItem>
                                <div style={{ display: "flex", flexDirection: "column", gap: 6 }}>
                                    <span style={labelStyle}>Scénarios inclus</span>
                                    <div style={{ display: "flex", flexWrap: "wrap", gap: 8 }}>
                                        {scenarios.length === 0 && <span style={{ fontSize: 12, color: "var(--v-text-2)" }}>Aucun scénario disponible.</span>}
                                        {scenarios.map(s => (
                                            <Chip key={s.contentId} active={selectedContents.some(c => c.contentId === s.contentId)}
                                                onClick={() => toggleContent({ contentType: "Scenario", contentId: s.contentId })}
                                                icon={<Mail size={12} />} label={s.title} />
                                        ))}
                                    </div>
                                </div>
                            </StaggerItem>

                            <StaggerItem>
                                <div style={{ borderRadius: 12, border: "1px solid var(--v-border)", background: "var(--v-surface-2)", padding: 14 }}>
                                    <label style={{ display: "flex", cursor: "pointer", alignItems: "center", gap: 8, fontSize: 14, color: "var(--v-text)" }}>
                                        <input type="checkbox" checked={assignToWholeTenant} onChange={e => setAssignToWholeTenant(e.target.checked)} style={{ accentColor: "var(--v-accent)", width: 16, height: 16 }} />
                                        <span style={{ fontWeight: 600 }}>Assigner à toute l&apos;entreprise</span>
                                    </label>
                                    <p style={{ marginTop: 6, fontSize: 12, color: "var(--v-text-2)", lineHeight: 1.5 }}>
                                        {assignToWholeTenant
                                            ? "Tous les employés actifs du tenant seront automatiquement assignés à la création."
                                            : "Vous pourrez assigner des employés spécifiques depuis la page détail."}
                                    </p>
                                </div>
                            </StaggerItem>
                        </Stagger>

                        {!validation.ok && (name || startDate || endDate || selectedContents.length > 0) && (
                            <p style={{ marginTop: 12, fontSize: 12.5, color: "var(--v-danger)" }}>{validation.msg}</p>
                        )}
                        <VisionButton type="button" disabled={!validation.ok || createM.isPending} onClick={handleCreate} style={{ marginTop: 16 }}>
                            <Plus size={14} /> {createM.isPending ? "Création…" : "Créer la campagne"}
                        </VisionButton>
                    </VisionCard>
                </Reveal>

                {/* ── Filtres ── */}
                <div style={{ display: "flex", flexWrap: "wrap", alignItems: "center", gap: 8 }}>
                    {(["All", "Upcoming", "Active", "Completed"] as StatusFilter[]).map(f => (
                        <Chip key={f} active={filter === f} onClick={() => setFilter(f)}
                            label={f === "All" ? "Toutes" : statusVision(f as CampaignStatus).label} />
                    ))}
                </div>

                {/* ── Liste ── */}
                <div style={{ overflow: "hidden", borderRadius: 20, border: "1px solid var(--v-border)", background: "color-mix(in srgb, var(--v-surface) 82%, transparent)", backdropFilter: "blur(14px)", WebkitBackdropFilter: "blur(14px)", boxShadow: "0 8px 30px rgba(0,0,0,0.25)" }}>
                    <div style={{ borderBottom: "1px solid var(--v-border)", background: "var(--v-surface-2)", padding: "12px 24px", fontSize: 12, fontWeight: 700, textTransform: "uppercase", letterSpacing: "0.05em", color: "var(--v-text-2)" }}>
                        Campagnes ({list.length})
                    </div>
                    <Stagger className="flex flex-col" gap={0.04}>
                        {list.map(c => {
                            const pct = Math.max(0, Math.min(100, Math.round(c.globalCompletion)));
                            return (
                                <StaggerItem key={c.id}>
                                    <div className="v-row" style={{ padding: "16px 24px", borderTop: "1px solid var(--v-border)" }}>
                                        <div style={{ display: "flex", alignItems: "flex-start", justifyContent: "space-between", gap: 12 }}>
                                            <div style={{ minWidth: 0, flex: 1 }}>
                                                <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
                                                    <Link href={`/admin/campaigns/${c.id}`} className="v-link" style={{ overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap", fontSize: 14, fontWeight: 600, color: "var(--v-text)", textDecoration: "none" }}>{c.name}</Link>
                                                    <StatusPill status={c.status} />
                                                </div>
                                                {c.description && <div style={{ marginTop: 2, overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap", fontSize: 12, color: "var(--v-text-2)" }}>{c.description}</div>}
                                                <div style={{ marginTop: 4, display: "flex", flexWrap: "wrap", gap: "4px 16px", fontSize: 12, color: "var(--v-text-2)" }}>
                                                    <span style={{ display: "inline-flex", alignItems: "center", gap: 4 }}><Calendar size={11} /> {new Date(c.startDate).toLocaleDateString("fr-FR")} → {new Date(c.endDate).toLocaleDateString("fr-FR")}</span>
                                                    <span>{c.contentCount} contenu{c.contentCount > 1 ? "s" : ""}</span>
                                                    <span style={{ display: "inline-flex", alignItems: "center", gap: 4 }}><Users size={11} /> {c.assignedCount} assigné{c.assignedCount > 1 ? "s" : ""}</span>
                                                </div>
                                                <div style={{ marginTop: 8, display: "flex", alignItems: "center", gap: 8 }}>
                                                    <div style={{ height: 6, flex: 1, overflow: "hidden", borderRadius: 999, background: "var(--v-surface-2)" }}>
                                                        <div style={{ height: "100%", width: `${pct}%`, borderRadius: 999, background: "var(--v-grad)", transition: "width .4s ease" }} />
                                                    </div>
                                                    <span style={{ fontSize: 12, fontWeight: 600, color: "var(--v-text)" }}>{pct}%</span>
                                                </div>
                                            </div>
                                            <button type="button" onClick={() => handleDelete(c.id, c.name)} title={c.status === "Upcoming" ? "Supprimer" : "Archiver"} className="v-act-danger"
                                                style={{ display: "inline-flex", borderRadius: 8, padding: 6, color: "var(--v-danger)", background: "transparent", border: "1px solid color-mix(in srgb, var(--v-danger) 40%, transparent)", cursor: "pointer" }}>
                                                <Trash2 size={14} />
                                            </button>
                                        </div>
                                    </div>
                                </StaggerItem>
                            );
                        })}
                        {list.length === 0 && <div style={{ padding: "48px 24px", textAlign: "center", fontSize: 14, color: "var(--v-text-2)" }}>Aucune campagne pour l&apos;instant.</div>}
                    </Stagger>
                </div>

                {/* ── Efficacité ── */}
                <EfficacySection enabled={enabled} />
            </div>
        </div>
    );
}

// ── Section Efficacité (VRAIES données ; 3 états ; code couleur charte) ────────
function efficacyColor(pct: number) { return pct >= 70 ? "var(--v-success)" : pct >= 40 ? "var(--v-warning)" : "var(--v-danger)"; }

function EfficacySection({ enabled }: { enabled: boolean }) {
    const summaryQ = useCampaignsEfficacy(enabled);
    const rows = summaryQ.data?.campaigns ?? [];
    const [sel, setSel] = useState("");
    const selectedId = sel || rows[0]?.campaignId || "";
    const detailQ = useCampaignEfficacy(selectedId || null);
    const d = detailQ.data;

    const compareData = rows.map(r => ({ label: r.name.length > 14 ? r.name.slice(0, 13) + "…" : r.name, value: r.completionRate }));

    return (
        <Reveal>
            <VisionCard>
                <div style={{ display: "flex", alignItems: "center", gap: 10, marginBottom: 4 }}>
                    <span style={{ display: "inline-flex", width: 34, height: 34, borderRadius: 10, alignItems: "center", justifyContent: "center", background: "color-mix(in srgb, var(--v-accent) 16%, transparent)", color: "var(--v-accent)" }}><BarChart3 size={18} /></span>
                    <div>
                        <h2 style={{ fontSize: 18, fontWeight: 700, color: "var(--v-text)" }}>Efficacité des campagnes</h2>
                        <p style={{ fontSize: 12, color: "var(--v-text-2)" }}>Participation, complétion et résultats scénarios — vraies données du tenant.</p>
                    </div>
                </div>

                {summaryQ.isLoading ? (
                    <div style={{ marginTop: 16, display: "flex", flexDirection: "column", gap: 16 }}>
                        <div style={{ height: 200, borderRadius: 16, background: "var(--v-surface-2)", opacity: 0.6 }} />
                    </div>
                ) : rows.length === 0 ? (
                    <div style={{ marginTop: 8, minHeight: 120, display: "flex", alignItems: "center", justifyContent: "center", fontSize: 13, color: "var(--v-text-2)", textAlign: "center" }}>
                        Aucune campagne à analyser. Créez une campagne et assignez des collaborateurs pour voir son efficacité.
                    </div>
                ) : (
                    <div style={{ marginTop: 16, display: "flex", flexDirection: "column", gap: 20 }}>
                        {/* Synthèse : comparaison de la complétion entre campagnes */}
                        {rows.length > 1 && (
                            <div>
                                <h3 style={{ fontSize: 13, fontWeight: 600, color: "var(--v-text)", marginBottom: 8 }}>Comparaison — taux de complétion</h3>
                                <VisionBarChart data={compareData} yKey="value" height={200} />
                            </div>
                        )}

                        {/* Sélecteur de campagne */}
                        <div style={{ display: "flex", alignItems: "center", gap: 10, flexWrap: "wrap" }}>
                            <span style={{ fontSize: 12.5, color: "var(--v-text-2)" }}>Détail de :</span>
                            <VisionSelect value={selectedId} onChange={e => setSel(e.target.value)} style={{ width: "auto", maxWidth: 320 }}>
                                {rows.map(r => <option key={r.campaignId} value={r.campaignId}>{r.name}</option>)}
                            </VisionSelect>
                        </div>

                        {detailQ.isLoading ? (
                            <div style={{ height: 220, borderRadius: 16, background: "var(--v-surface-2)", opacity: 0.6 }} />
                        ) : d ? (
                            <>
                                <Stagger className="grid grid-cols-2 gap-4 md:grid-cols-3" gap={0.06}>
                                    <StaggerItem><VisionKpiCard value={d.participationRate} suffix="%" label="Participation" icon={Users} hint={`${d.started}/${d.totalAssigned} ont démarré`} /></StaggerItem>
                                    <StaggerItem><VisionKpiCard value={d.completionRate} suffix="%" label="Complétion" icon={TrendingUp} hint={`${d.completedUsers}/${d.totalAssigned} ont terminé`} /></StaggerItem>
                                    <StaggerItem><VisionKpiCard value={d.averageSuccessRate} suffix="%" label="Réussite moyenne" icon={ShieldCheck} /></StaggerItem>
                                </Stagger>

                                <div>
                                    <h3 style={{ fontSize: 13, fontWeight: 600, color: "var(--v-text)", marginBottom: 8 }}>Évolution des complétions</h3>
                                    {d.completionTrend.length >= 2
                                        ? <VisionAreaChart data={d.completionTrend} yKey="value" domainMax={Math.max(1, ...d.completionTrend.map(p => p.value)) * 1.15} height={200} />
                                        : <div style={{ minHeight: 100, display: "flex", alignItems: "center", justifyContent: "center", fontSize: 13, color: "var(--v-text-2)" }}>Pas encore assez de complétions datées pour tracer une courbe.</div>}
                                </div>

                                {/* Résultats scénarios (phishing) */}
                                <div>
                                    <h3 style={{ fontSize: 13, fontWeight: 600, color: "var(--v-text)", marginBottom: 8 }}>Résultats scénarios (phishing)</h3>
                                    {d.scenario && d.scenario.attackEmails > 0 ? (
                                        <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
                                            <ScenarioStat label="E-mails d'attaque envoyés" value={d.scenario.attackEmails} icon={<Mail size={16} />} color="var(--v-text-2)" suffix="" />
                                            <ScenarioStat label="Ont cliqué (à risque)" value={d.scenario.clickRate} suffix="%" icon={<ShieldAlert size={16} />} color={d.scenario.clickRate >= 30 ? "var(--v-danger)" : d.scenario.clickRate >= 10 ? "var(--v-warning)" : "var(--v-success)"} hint={`${d.scenario.clicked}/${d.scenario.attackEmails}`} />
                                            <ScenarioStat label="Ont signalé (bon réflexe)" value={d.scenario.reportRate} suffix="%" icon={<ShieldCheck size={16} />} color={efficacyColor(d.scenario.reportRate)} hint={`${d.scenario.reported}/${d.scenario.attackEmails}`} />
                                        </div>
                                    ) : (
                                        <div style={{ minHeight: 90, display: "flex", alignItems: "center", justifyContent: "center", fontSize: 13, color: "var(--v-text-2)", textAlign: "center" }}>
                                            {d.scenario ? "Aucun e-mail d'attaque envoyé pour l'instant sur cette campagne." : "Cette campagne ne contient pas de scénario."}
                                        </div>
                                    )}
                                </div>
                            </>
                        ) : null}
                    </div>
                )}
            </VisionCard>
        </Reveal>
    );
}

function ScenarioStat({ label, value, suffix = "", icon, color, hint }: { label: string; value: number; suffix?: string; icon: React.ReactNode; color: string; hint?: string }) {
    return (
        <div style={{ borderRadius: 14, border: "1px solid var(--v-border)", background: "var(--v-surface-2)", padding: 16 }}>
            <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", gap: 8 }}>
                <span style={{ fontSize: 12.5, color: "var(--v-text-2)", fontWeight: 500 }}>{label}</span>
                <span style={{ color, display: "inline-flex" }}>{icon}</span>
            </div>
            <div style={{ marginTop: 8, fontSize: 26, fontWeight: 700, color }}>
                <CountUp value={value} />{suffix}
            </div>
            {hint && <div style={{ marginTop: 2, fontSize: 11, color: "var(--v-text-3)" }}>{hint}</div>}
        </div>
    );
}
