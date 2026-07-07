"use client";

// Analytics admin — 4 onglets Entreprise / Groupe / Individuel / Rapport financier.
// Rapport financier = estimation de pertes potentielles évitées (N & couverture réels, hypothèses p/C/h/r éditables).

import { useState, useEffect, useRef } from "react";
import { useQuery } from "@tanstack/react-query";
import { apiFetch } from "@/lib/api";
import Reveal from "@/components/Reveal";
import { Stagger, StaggerItem } from "@/components/Stagger";
import VisionKpiCard from "@/components/vision/VisionKpiCard";
import VisionAreaChart from "@/components/vision/VisionAreaChart";
import VisionBarChart from "@/components/vision/VisionBarChart";
import VisionGauge from "@/components/vision/VisionGauge";
import {
    AlertTriangle, Users2, UserCheck, UserX, CheckCircle2, Activity,
    Download, BarChart3, Lock, ArrowLeft, ChevronRight,
    ShieldCheck, ShieldAlert, Euro, Settings2, Info, RotateCcw, GraduationCap, Fingerprint,
} from "lucide-react";

type WeakTopic = { theme: string; avgScore: number; completionRate: number; mastery: number; completions: number };
type WeakTopics = { topics: WeakTopic[]; themesEvaluated: number };
type RiskPoint = { label: string; value: number };
type Risk = { globalScore: number | null; band: string; trend: RiskPoint[]; usersScored: number };
type Engagement = { totalUsers: number; active7d: number; active30d: number; neverConnected: number; participationRate: number; totalCompletions: number; avgCompletionsPerActiveUser: number };
type GroupRow = { teamId: string; team: string; memberCount: number; mastery: number; avgScore: number; completionRate: number; avgRisk: number | null; riskBand: string; participationRate: number };
type Groups = { groups: GroupRow[] };
type AnalyticsUser = { userId: string; name: string };
type Users = { users: AnalyticsUser[] };
type Profile = { name: string; completions: number; avgScore: number; themesAttempted: number; lastActivityAt: string | null; lastLoginAt: string | null; createdAt: string };
type FinancialPoint = { label: string; completions: number; cumulativeParticipation: number; cri: number; coverage: number };
type FinancialData = { employeeCount: number; participationRate: number; avgCri: number; coverage: number; totalCompletions: number; trend: FinancialPoint[] };
type Hypotheses = { p: number; C: number; h: number; r: number };
type BehaviorRow = { behavior: string; errorRate: number; avgScore: number; attempts: number; failedAttempts: number };
type BehaviorErrors = { behaviors: BehaviorRow[]; totalAttempts: number };

function GlassCard({ children, style }: { children: React.ReactNode; style?: React.CSSProperties }) {
    return (
        <div style={{ background: "color-mix(in srgb, var(--v-surface) 82%, transparent)", backdropFilter: "blur(14px)", border: "1px solid var(--v-border)", borderRadius: 20, boxShadow: "0 8px 30px rgba(0,0,0,0.25)", padding: 24, ...style }}>
            {children}
        </div>
    );
}
function EmptyState({ label }: { label: string }) {
    return <div style={{ minHeight: 120, display: "flex", alignItems: "center", justifyContent: "center", textAlign: "center", fontSize: 13, color: "var(--v-text-3)", padding: 16 }}>{label}</div>;
}
function SkelBlock({ h }: { h: number }) {
    return <div style={{ height: h, borderRadius: 16, background: "var(--v-surface-2)", opacity: 0.6 }} />;
}
function masteryColor(m: number) { return m < 40 ? "var(--danger)" : m < 60 ? "var(--warning)" : "var(--v-accent)"; }

export default function AnalyticsPage() {
    const [tab, setTab] = useState<"entreprise" | "groupe" | "individuel" | "financier">("entreprise");

    const statusQ = useQuery<{ isEnabled: boolean }>({
        queryKey: ["analytics", "status"],
        queryFn: () => apiFetch("/api/analytics/status"),
    });

    if (statusQ.data && !statusQ.data.isEnabled) {
        return (
            <div className="vision-dashboard" style={{ minHeight: "100%" }}>
                <div style={{ maxWidth: 1160, margin: "0 auto", padding: "48px 20px" }}>
                    <GlassCard style={{ textAlign: "center", padding: 48 }}>
                        <Lock size={28} style={{ color: "var(--v-text-3)" }} />
                        <h1 style={{ marginTop: 12, fontSize: 20, fontWeight: 700, color: "var(--v-text)" }}>Analytics désactivés</h1>
                        <p style={{ marginTop: 6, fontSize: 14, color: "var(--v-text-2)" }}>Ce module n&apos;est pas activé pour votre organisation.</p>
                    </GlassCard>
                </div>
            </div>
        );
    }

    return (
        <div className="vision-dashboard" style={{ minHeight: "100%" }}>
            <div style={{ maxWidth: 1160, margin: "0 auto", padding: "24px 20px 80px" }}>
                <Reveal>
                    <div style={{ display: "flex", alignItems: "center", gap: 10, marginBottom: 16 }}>
                        <BarChart3 size={22} style={{ color: "var(--v-accent)" }} />
                        <h1 style={{ fontSize: 26, fontWeight: 700, color: "var(--v-text)", letterSpacing: "-0.02em" }}>Analytics</h1>
                    </div>
                </Reveal>

                <div style={{ display: "flex", gap: 8, marginBottom: 24, borderBottom: "1px solid var(--v-border)", overflowX: "auto" }}>
                    {([["entreprise", "Entreprise"], ["groupe", "Groupe"], ["individuel", "Individuel"], ["financier", "Rapport financier"]] as const).map(([key, label]) => (
                        <button key={key} onClick={() => setTab(key)} style={{
                            padding: "10px 18px", marginBottom: "-1px", fontSize: 14, background: "transparent", border: "none", cursor: "pointer",
                            fontWeight: tab === key ? 600 : 500,
                            color: tab === key ? "var(--v-cyan)" : "var(--v-text-3)",
                            borderBottom: tab === key ? "2px solid var(--v-cyan)" : "2px solid transparent",
                        }}>{label}</button>
                    ))}
                </div>

                {tab === "entreprise" && <EntrepriseTab />}
                {tab === "groupe" && <GroupeTab />}
                {tab === "individuel" && <IndividuelTab />}
                {tab === "financier" && <FinancialTab />}
            </div>
        </div>
    );
}

// ── Onglet ENTREPRISE : détail standard + erreurs par comportement à risque ───
function EntrepriseTab() {
    return (
        <div style={{ display: "flex", flexDirection: "column", gap: 20 }}>
            <AnalyticsDetail basePath="/api/analytics/enterprise" keyPrefix="ent" showExport />
            <BehaviorBlock />
        </div>
    );
}

function behaviorColor(e: number) { return e >= 60 ? "var(--danger)" : e >= 40 ? "var(--warning)" : "var(--v-accent)"; }

function BehaviorBlock() {
    const q = useQuery<BehaviorErrors>({ queryKey: ["behaviors"], queryFn: () => apiFetch("/api/analytics/enterprise/behaviors") });
    const data = q.data;
    return (
        <Reveal>
            <GlassCard>
                <div style={{ display: "flex", alignItems: "center", gap: 10, marginBottom: 4 }}>
                    <span style={{ display: "inline-flex", width: 34, height: 34, borderRadius: 10, alignItems: "center", justifyContent: "center", background: "var(--v-accent-subtle)", color: "var(--v-cyan)" }}>
                        <Fingerprint size={18} />
                    </span>
                    <div>
                        <h2 style={{ fontSize: 18, fontWeight: 700, color: "var(--v-text)" }}>Erreurs par comportement à risque</h2>
                        <p style={{ fontSize: 12, color: "var(--v-text-3)" }}>Où vos équipes se trompent le plus (taux d&apos;échec = complétions sous 50&nbsp;%)</p>
                    </div>
                </div>
                {q.isLoading ? <div style={{ marginTop: 16 }}><SkelBlock h={200} /></div>
                    : (data && data.behaviors.length > 0)
                        ? <div style={{ display: "flex", flexDirection: "column", gap: 10, marginTop: 16 }}>
                            {data.behaviors.map(b => <BehaviorRowCard key={b.behavior} b={b} />)}
                        </div>
                        : <EmptyState label="Pas encore de complétions rattachées à un comportement pour établir un classement." />}
            </GlassCard>
        </Reveal>
    );
}

function BehaviorRowCard({ b }: { b: BehaviorRow }) {
    const color = behaviorColor(b.errorRate);
    return (
        <div style={{ display: "flex", alignItems: "center", gap: 14, background: "var(--v-surface-2)", border: "1px solid var(--v-border)", borderRadius: 14, padding: "12px 16px" }}>
            <span style={{ flexShrink: 0, width: 40, height: 40, borderRadius: 10, display: "inline-flex", alignItems: "center", justifyContent: "center", fontSize: 13, fontWeight: 800, background: "color-mix(in srgb, " + color + " 16%, transparent)", color }}>{b.errorRate}%</span>
            <div style={{ flex: 1, minWidth: 0 }}>
                <div style={{ display: "flex", justifyContent: "space-between", gap: 10, marginBottom: 6 }}>
                    <span style={{ fontSize: 14, fontWeight: 600, color: "var(--v-text)", overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap" }}>{b.behavior}</span>
                    <span style={{ fontSize: 12, fontWeight: 600, color, flexShrink: 0 }}>{b.errorRate >= 60 ? "Critique" : b.errorRate >= 40 ? "À surveiller" : "Maîtrisé"}</span>
                </div>
                <div style={{ height: 6, background: "var(--v-surface)", borderRadius: 999, overflow: "hidden" }}>
                    <div style={{ width: `${b.errorRate}%`, height: "100%", background: color, borderRadius: 999 }} />
                </div>
                <div style={{ display: "flex", gap: 14, marginTop: 6, fontSize: 11, color: "var(--v-text-3)", flexWrap: "wrap" }}>
                    <span>Score moyen&nbsp;: <b style={{ color: "var(--v-text-2)" }}>{b.avgScore}%</b></span>
                    <span><b style={{ color: "var(--v-text-2)" }}>{b.failedAttempts}</b>/{b.attempts} échec{b.failedAttempts > 1 ? "s" : ""}</span>
                </div>
            </div>
        </div>
    );
}

// ── Onglet GROUPE : classement des équipes + drill-down ───────────────────────
function GroupeTab() {
    const [selected, setSelected] = useState<GroupRow | null>(null);
    const groupsQ = useQuery<Groups>({ queryKey: ["groups"], queryFn: () => apiFetch("/api/analytics/groups") });

    if (selected) {
        return (
            <div style={{ display: "flex", flexDirection: "column", gap: 16 }}>
                <button onClick={() => setSelected(null)} style={{ alignSelf: "flex-start", display: "inline-flex", alignItems: "center", gap: 6, fontSize: 13, fontWeight: 500, color: "var(--v-text-2)", background: "transparent", border: "1px solid var(--v-border)", borderRadius: 8, padding: "7px 12px", cursor: "pointer" }}>
                    <ArrowLeft size={15} /> Toutes les équipes
                </button>
                <h2 style={{ fontSize: 20, fontWeight: 700, color: "var(--v-text)" }}>{selected.team} <span style={{ fontSize: 13, fontWeight: 400, color: "var(--v-text-3)" }}>· {selected.memberCount} membre{selected.memberCount > 1 ? "s" : ""}</span></h2>
                <AnalyticsDetail basePath={`/api/analytics/groups/${selected.teamId}`} keyPrefix={`grp-${selected.teamId}`} />
            </div>
        );
    }

    return (
        <Reveal>
            <GlassCard>
                <h2 style={{ fontSize: 18, fontWeight: 700, color: "var(--v-text)" }}>Classement des équipes</h2>
                <p style={{ fontSize: 12, color: "var(--v-text-3)", marginBottom: 16 }}>De la plus faible à la plus forte (maîtrise = score × complétion). Cliquez une équipe pour son détail.</p>
                {groupsQ.isLoading ? <SkelBlock h={200} />
                    : (groupsQ.data && groupsQ.data.groups.length > 0)
                        ? <div style={{ display: "flex", flexDirection: "column", gap: 10 }}>
                            {groupsQ.data.groups.map((g, i) => <GroupRowCard key={g.teamId} rank={i + 1} g={g} onClick={() => setSelected(g)} />)}
                        </div>
                        : <EmptyState label="Aucune équipe pour le moment. Créez des équipes et assignez-y des membres pour voir leurs analytics." />}
            </GlassCard>
        </Reveal>
    );
}

function GroupRowCard({ rank, g, onClick }: { rank: number; g: GroupRow; onClick: () => void }) {
    const color = masteryColor(g.mastery);
    return (
        <button onClick={onClick} className="v-hover" style={{ textAlign: "left", display: "flex", alignItems: "center", gap: 14, background: "var(--v-surface-2)", border: "1px solid var(--v-border)", borderRadius: 14, padding: "12px 16px", cursor: "pointer", width: "100%" }}>
            <span style={{ flexShrink: 0, width: 26, height: 26, borderRadius: 999, display: "inline-flex", alignItems: "center", justifyContent: "center", fontSize: 12, fontWeight: 700, background: "color-mix(in srgb, " + color + " 16%, transparent)", color }}>{rank}</span>
            <div style={{ flex: 1, minWidth: 0 }}>
                <div style={{ display: "flex", justifyContent: "space-between", gap: 10, marginBottom: 6 }}>
                    <span style={{ fontSize: 14, fontWeight: 600, color: "var(--v-text)", overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap" }}>{g.team}</span>
                    <span style={{ fontSize: 13, fontWeight: 700, color, flexShrink: 0 }}>{g.mastery}<span style={{ fontSize: 11, color: "var(--v-text-3)" }}> /100</span></span>
                </div>
                <div style={{ height: 6, background: "var(--v-surface)", borderRadius: 999, overflow: "hidden" }}>
                    <div style={{ width: `${g.mastery}%`, height: "100%", background: color, borderRadius: 999 }} />
                </div>
                <div style={{ display: "flex", gap: 14, marginTop: 6, fontSize: 11, color: "var(--v-text-3)", flexWrap: "wrap" }}>
                    <span>{g.memberCount} membre{g.memberCount > 1 ? "s" : ""}</span>
                    <span>Participation&nbsp;: <b style={{ color: "var(--v-text-2)" }}>{g.participationRate}%</b></span>
                    <span>Risque&nbsp;: <b style={{ color: "var(--v-text-2)" }}>{g.avgRisk != null ? `${g.avgRisk} (${g.riskBand})` : "—"}</b></span>
                </div>
            </div>
            <ChevronRight size={16} style={{ color: "var(--v-text-3)", flexShrink: 0 }} />
        </button>
    );
}

// ── Blocs analytics réutilisables (Entreprise OU équipe scopée) ───────────────
function AnalyticsDetail({ basePath, keyPrefix, showExport = false, engagementOverride }: { basePath: string; keyPrefix: string; showExport?: boolean; engagementOverride?: React.ReactNode }) {
    const [months, setMonths] = useState(6);
    const [exporting, setExporting] = useState(false);

    const weakQ = useQuery<WeakTopics>({ queryKey: [keyPrefix, "weak"], queryFn: () => apiFetch(`${basePath}/weak-topics?top=5`) });
    const riskQ = useQuery<Risk>({ queryKey: [keyPrefix, "risk", months], queryFn: () => apiFetch(`${basePath}/risk?months=${months}`) });
    const engQ = useQuery<Engagement>({ queryKey: [keyPrefix, "eng"], queryFn: () => apiFetch(`${basePath}/engagement`), enabled: !engagementOverride });

    async function handleExport() {
        setExporting(true);
        try {
            const res = await fetch(`${basePath}/export?months=${months}`, { credentials: "include" });
            if (!res.ok) throw new Error("export");
            const blob = await res.blob();
            const url = URL.createObjectURL(blob);
            const a = document.createElement("a");
            a.href = url; a.download = "analytics-entreprise.csv"; a.click();
            URL.revokeObjectURL(url);
        } catch { /* silencieux */ } finally { setExporting(false); }
    }

    const risk = riskQ.data;
    const eng = engQ.data;

    return (
        <div style={{ display: "flex", flexDirection: "column", gap: 20 }}>
            <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", gap: 12, flexWrap: "wrap" }}>
                <div style={{ display: "flex", gap: 6, alignItems: "center" }}>
                    <span style={{ fontSize: 12, color: "var(--v-text-3)" }}>Période :</span>
                    {[3, 6, 12].map(m => (
                        <button key={m} onClick={() => setMonths(m)} style={{
                            fontSize: 12, padding: "5px 12px", borderRadius: 8, cursor: "pointer",
                            border: "1px solid " + (months === m ? "var(--v-accent)" : "var(--v-border)"),
                            background: months === m ? "var(--v-accent-subtle)" : "transparent",
                            color: months === m ? "var(--v-cyan)" : "var(--v-text-2)",
                        }}>{m} mois</button>
                    ))}
                </div>
                {showExport && (
                    <button onClick={handleExport} disabled={exporting} style={{
                        display: "inline-flex", alignItems: "center", gap: 6, fontSize: 13, fontWeight: 600,
                        padding: "8px 16px", borderRadius: 10, cursor: exporting ? "default" : "pointer",
                        background: "linear-gradient(135deg, var(--v-accent), var(--v-accent-2))", color: "var(--v-text)",
                        border: "none", boxShadow: "0 6px 16px color-mix(in srgb, var(--v-accent) 40%, transparent)", opacity: exporting ? 0.7 : 1,
                    }}>
                        <Download size={15} /> {exporting ? "Export…" : "Exporter (CSV)"}
                    </button>
                )}
            </div>

            {/* BLOC PHARE — Points faibles à renforcer */}
            <Reveal>
                <GlassCard style={{ borderColor: "color-mix(in srgb, var(--danger) 40%, var(--v-border))" }}>
                    <div style={{ display: "flex", alignItems: "center", gap: 10, marginBottom: 4 }}>
                        <span style={{ display: "inline-flex", width: 34, height: 34, borderRadius: 10, alignItems: "center", justifyContent: "center", background: "var(--danger-subtle)", color: "var(--danger)" }}>
                            <AlertTriangle size={18} />
                        </span>
                        <div>
                            <h2 style={{ fontSize: 18, fontWeight: 700, color: "var(--v-text)" }}>Points faibles à renforcer</h2>
                            <p style={{ fontSize: 12, color: "var(--v-text-3)" }}>Thèmes les plus faibles (score × taux de complétion)</p>
                        </div>
                    </div>
                    {weakQ.isLoading ? <div style={{ marginTop: 16 }}><SkelBlock h={180} /></div>
                        : (weakQ.data && weakQ.data.topics.length > 0)
                            ? <div style={{ display: "flex", flexDirection: "column", gap: 10, marginTop: 16 }}>
                                {weakQ.data.topics.map((t, i) => <WeakRow key={t.theme} rank={i + 1} t={t} />)}
                            </div>
                            : <EmptyState label="Pas encore assez de données par thème (≥ 3 complétions requises) pour identifier des points faibles." />}
                </GlassCard>
            </Reveal>

            {/* Risque global + courbe */}
            <Reveal>
                <div className="grid grid-cols-1 gap-5 lg:grid-cols-3">
                    <div className="lg:col-span-1">
                        <GlassCard>
                            <h2 style={{ fontSize: 16, fontWeight: 600, color: "var(--v-text)", marginBottom: 4 }}>Risque cyber</h2>
                            <p style={{ fontSize: 12, color: "var(--v-text-3)", marginBottom: 8 }}>{risk ? `${risk.usersScored} utilisateur${risk.usersScored > 1 ? "s" : ""} évalué${risk.usersScored > 1 ? "s" : ""}` : " "}</p>
                            {riskQ.isLoading ? <SkelBlock h={200} />
                                : (risk && risk.globalScore != null)
                                    ? <div style={{ display: "flex", justifyContent: "center", padding: "8px 0" }}><VisionGauge value={risk.globalScore} band={risk.band} /></div>
                                    : <EmptyState label="Aucun score de risque calculé." />}
                        </GlassCard>
                    </div>
                    <div className="lg:col-span-2">
                        <GlassCard>
                            <h2 style={{ fontSize: 16, fontWeight: 600, color: "var(--v-text)", marginBottom: 4 }}>Progression du risque</h2>
                            <p style={{ fontSize: 12, color: "var(--v-text-3)", marginBottom: 8 }}>{months} derniers mois</p>
                            {riskQ.isLoading ? <SkelBlock h={220} />
                                : (risk && risk.trend.length >= 2)
                                    ? <VisionAreaChart data={risk.trend} domainMax={100} height={220} />
                                    : <EmptyState label="Historique insuffisant pour tracer une courbe." />}
                        </GlassCard>
                    </div>
                </div>
            </Reveal>

            {/* Engagement (ou bloc surchargé — ex. profil individuel) */}
            {engagementOverride ? <Reveal>{engagementOverride}</Reveal> : (
                <Reveal>
                    <h2 style={{ fontSize: 16, fontWeight: 600, color: "var(--v-text)", marginBottom: 12 }}>Engagement</h2>
                    {engQ.isLoading ? (
                        <div className="grid grid-cols-2 gap-4 md:grid-cols-4">{[0, 1, 2, 3].map(i => <SkelBlock key={i} h={112} />)}</div>
                    ) : (
                        <Stagger className="grid grid-cols-2 gap-4 md:grid-cols-4" gap={0.06}>
                            <StaggerItem><VisionKpiCard value={eng?.active7d ?? 0} label="Actifs 7 jours" icon={UserCheck} hint={eng ? `sur ${eng.totalUsers}` : ""} /></StaggerItem>
                            <StaggerItem><VisionKpiCard value={eng?.active30d ?? 0} label="Actifs 30 jours" icon={Users2} /></StaggerItem>
                            <StaggerItem><VisionKpiCard value={eng?.participationRate ?? 0} suffix="%" label="Participation" icon={CheckCircle2} hint="ont complété ≥1 challenge" /></StaggerItem>
                            <StaggerItem><VisionKpiCard value={eng?.neverConnected ?? 0} label="Jamais connectés" icon={UserX} /></StaggerItem>
                        </Stagger>
                    )}
                </Reveal>
            )}
        </div>
    );
}

function WeakRow({ rank, t }: { rank: number; t: WeakTopic }) {
    const color = masteryColor(t.mastery);
    return (
        <div style={{ display: "flex", alignItems: "center", gap: 14, background: "var(--v-surface-2)", border: "1px solid var(--v-border)", borderRadius: 14, padding: "12px 16px" }}>
            <span style={{ flexShrink: 0, width: 26, height: 26, borderRadius: 999, display: "inline-flex", alignItems: "center", justifyContent: "center", fontSize: 12, fontWeight: 700, background: "color-mix(in srgb, " + color + " 16%, transparent)", color }}>{rank}</span>
            <div style={{ flex: 1, minWidth: 0 }}>
                <div style={{ display: "flex", justifyContent: "space-between", gap: 10, marginBottom: 6 }}>
                    <span style={{ fontSize: 14, fontWeight: 600, color: "var(--v-text)", overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap" }}>{t.theme}</span>
                    <span style={{ fontSize: 13, fontWeight: 700, color, flexShrink: 0 }}>{t.mastery}<span style={{ fontSize: 11, color: "var(--v-text-3)" }}> /100 maîtrise</span></span>
                </div>
                <div style={{ height: 6, background: "var(--v-surface)", borderRadius: 999, overflow: "hidden" }}>
                    <div style={{ width: `${t.mastery}%`, height: "100%", background: color, borderRadius: 999 }} />
                </div>
                <div style={{ display: "flex", gap: 14, marginTop: 6, fontSize: 11, color: "var(--v-text-3)" }}>
                    <span>Score moyen&nbsp;: <b style={{ color: "var(--v-text-2)" }}>{t.avgScore}%</b></span>
                    <span>Complétion&nbsp;: <b style={{ color: "var(--v-text-2)" }}>{t.completionRate}%</b></span>
                    <span>{t.completions} complétion{t.completions > 1 ? "s" : ""}</span>
                </div>
            </div>
        </div>
    );
}

// ── Onglet INDIVIDUEL : sélecteur d'utilisateur + détail perso ────────────────
function IndividuelTab() {
    const [search, setSearch] = useState("");
    const [selected, setSelected] = useState<AnalyticsUser | null>(null);

    const usersQ = useQuery<Users>({ queryKey: ["an-users"], queryFn: () => apiFetch("/api/analytics/users") });
    const profileQ = useQuery<Profile>({ queryKey: ["an-profile", selected?.userId], queryFn: () => apiFetch(`/api/analytics/users/${selected!.userId}/profile`), enabled: !!selected });

    if (selected) {
        return (
            <div style={{ display: "flex", flexDirection: "column", gap: 16 }}>
                <button onClick={() => setSelected(null)} style={{ alignSelf: "flex-start", display: "inline-flex", alignItems: "center", gap: 6, fontSize: 13, fontWeight: 500, color: "var(--v-text-2)", background: "transparent", border: "1px solid var(--v-border)", borderRadius: 8, padding: "7px 12px", cursor: "pointer" }}>
                    <ArrowLeft size={15} /> Tous les collaborateurs
                </button>
                <h2 style={{ fontSize: 20, fontWeight: 700, color: "var(--v-text)" }}>{selected.name}</h2>
                <AnalyticsDetail basePath={`/api/analytics/users/${selected.userId}`} keyPrefix={`usr-${selected.userId}`} engagementOverride={<ProfileBlock loading={profileQ.isLoading} p={profileQ.data} />} />
            </div>
        );
    }

    const list = (usersQ.data?.users ?? []).filter(u => u.name.toLowerCase().includes(search.trim().toLowerCase()));
    return (
        <Reveal>
            <GlassCard>
                <h2 style={{ fontSize: 18, fontWeight: 700, color: "var(--v-text)" }}>Analytics par collaborateur</h2>
                <p style={{ fontSize: 12, color: "var(--v-text-3)", marginBottom: 14 }}>Choisissez une personne pour voir ses points faibles, son risque et son profil.</p>
                <input value={search} onChange={e => setSearch(e.target.value)} placeholder="Rechercher un collaborateur…"
                    style={{ width: "100%", marginBottom: 14, padding: "10px 14px", fontSize: 14, borderRadius: 10, background: "var(--v-surface-2)", border: "1px solid var(--v-border)", color: "var(--v-text)", outline: "none" }} />
                {usersQ.isLoading ? <SkelBlock h={200} />
                    : list.length > 0
                        ? <div style={{ display: "flex", flexDirection: "column", gap: 8 }}>
                            {list.map(u => {
                                const initials = u.name.split(" ").filter(Boolean).map(n => n[0]).join("").slice(0, 2).toUpperCase() || "?";
                                return (
                                    <button key={u.userId} onClick={() => setSelected(u)} className="v-hover" style={{ textAlign: "left", display: "flex", alignItems: "center", gap: 12, background: "var(--v-surface-2)", border: "1px solid var(--v-border)", borderRadius: 12, padding: "10px 14px", cursor: "pointer", width: "100%" }}>
                                        <span style={{ flexShrink: 0, width: 32, height: 32, borderRadius: "50%", background: "linear-gradient(135deg, var(--v-accent), var(--v-accent-2))", color: "var(--v-text)", display: "inline-flex", alignItems: "center", justifyContent: "center", fontSize: 12, fontWeight: 700 }}>{initials}</span>
                                        <span style={{ flex: 1, fontSize: 14, color: "var(--v-text)", overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap" }}>{u.name}</span>
                                        <ChevronRight size={16} style={{ color: "var(--v-text-3)", flexShrink: 0 }} />
                                    </button>
                                );
                            })}
                        </div>
                        : <EmptyState label="Aucun collaborateur trouvé." />}
            </GlassCard>
        </Reveal>
    );
}

// ── Onglet RAPPORT FINANCIER : perte potentielle évitée (ESTIMATION) ──────────
// N/t = vraies données du tenant ; p/C/h/r = hypothèses éditables (jamais présentées comme des faits).
const FIN_DEFAULTS: Hypotheses = { p: 10, C: 466000, h: 68, r: 25 };
const eur = new Intl.NumberFormat("fr-FR", { style: "currency", currency: "EUR", maximumFractionDigits: 0 });

/** Compteur animé formaté (respecte prefers-reduced-motion). */
function useAnimatedNumber(value: number, duration = 900) {
    const [n, setN] = useState(0);
    const raf = useRef<number | undefined>(undefined);
    useEffect(() => {
        const reduce = typeof window !== "undefined" && window.matchMedia?.("(prefers-reduced-motion: reduce)").matches;
        if (reduce) { const id = requestAnimationFrame(() => setN(value)); return () => cancelAnimationFrame(id); }
        const startT = performance.now();
        const tick = (t: number) => {
            const prog = Math.min((t - startT) / duration, 1);
            const eased = 1 - Math.pow(1 - prog, 3);
            setN(Math.round(value * eased));
            if (prog < 1) raf.current = requestAnimationFrame(tick);
        };
        raf.current = requestAnimationFrame(tick);
        return () => { if (raf.current) cancelAnimationFrame(raf.current); };
    }, [value, duration]);
    return n;
}

function FinancialTab() {
    const [months, setMonths] = useState(6);
    const [hyp, setHyp] = useState<Hypotheses>(FIN_DEFAULTS);
    const finQ = useQuery<FinancialData>({ queryKey: ["fin", months], queryFn: () => apiFetch(`/api/analytics/enterprise/financial?months=${months}`) });

    const d = finQ.data;
    const N = d?.employeeCount ?? 0;
    const t = d?.coverage ?? 0;
    const factor = (hyp.p / 100) * hyp.C * (hyp.h / 100);   // exposition unitaire × facteur humain
    const exposure = N * factor;                            // exposition annuelle estimée (avant sensibilisation)
    const avoided = exposure * (hyp.r / 100) * t;           // perte potentielle évitée (estimation)
    const expectedIncidents = N * (hyp.p / 100);            // incidents attendus / an (estimation)

    const avoidedTrend = (d?.trend ?? []).map(pt => ({ label: pt.label, value: Math.round(N * factor * (hyp.r / 100) * pt.coverage) }));
    const coverageTrend = (d?.trend ?? []).map(pt => ({ label: pt.label, value: Math.round(pt.coverage * 100) }));
    const activityTrend = (d?.trend ?? []).map(pt => ({ label: pt.label, value: pt.completions }));
    const avoidedMax = Math.max(100, ...avoidedTrend.map(x => x.value)) * 1.2;

    function exportCsv() {
        if (!d) return;
        const L: string[] = [];
        L.push("Rapport financier - estimation de pertes potentielles evitees");
        L.push(`Genere le;${new Date().toLocaleString("fr-FR")}`);
        L.push("AVERTISSEMENT;Estimation indicative basee sur des hypotheses - PAS une valeur mesuree ni garantie");
        L.push("");
        L.push("Hypotheses de calcul (modifiables par l'admin)");
        L.push(`Proba incident par salarie et par an (p);${hyp.p};%`);
        L.push(`Cout moyen d'un incident (C);${hyp.C};EUR`);
        L.push(`Part des incidents liee au facteur humain (h);${hyp.h};%`);
        L.push(`Reduction du risque via sensibilisation (r);${hyp.r};%`);
        L.push("");
        L.push("Donnees reelles du tenant");
        L.push(`Salaries enregistres (N);${d.employeeCount}`);
        L.push(`Participation;${d.participationRate};%`);
        L.push(`CRI moyen;${d.avgCri}`);
        L.push(`Couverture de formation (t);${Math.round(d.coverage * 100)};%`);
        L.push("");
        L.push(`Perte potentielle evitee (estimation) par an;${Math.round(avoided)};EUR`);
        L.push(`Exposition annuelle estimee (avant sensibilisation);${Math.round(exposure)};EUR`);
        L.push("");
        L.push("Mois;Completions;Participation cumulee %;CRI;Couverture %;Perte evitee estimee EUR");
        d.trend.forEach(pt => L.push(`${pt.label};${pt.completions};${pt.cumulativeParticipation};${pt.cri};${Math.round(pt.coverage * 100)};${Math.round(N * factor * (hyp.r / 100) * pt.coverage)}`));
        const blob = new Blob(["﻿" + L.join("\r\n")], { type: "text/csv;charset=utf-8" });
        const url = URL.createObjectURL(blob);
        const a = document.createElement("a");
        a.href = url; a.download = `rapport-financier-${new Date().toISOString().slice(0, 10)}.csv`; a.click();
        URL.revokeObjectURL(url);
    }

    if (finQ.isLoading) {
        return <div style={{ display: "flex", flexDirection: "column", gap: 20 }}><SkelBlock h={150} /><SkelBlock h={260} /><SkelBlock h={220} /></div>;
    }
    if (!d || d.employeeCount === 0) {
        return <Reveal><GlassCard><EmptyState label="Aucun salarié enregistré pour estimer un impact financier. Ajoutez des collaborateurs et lancez des formations pour alimenter le rapport." /></GlassCard></Reveal>;
    }

    return (
        <div style={{ display: "flex", flexDirection: "column", gap: 20 }}>
            {/* Période + export */}
            <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", gap: 12, flexWrap: "wrap" }}>
                <div style={{ display: "flex", gap: 6, alignItems: "center" }}>
                    <span style={{ fontSize: 12, color: "var(--v-text-3)" }}>Période :</span>
                    {[3, 6, 12].map(m => (
                        <button key={m} onClick={() => setMonths(m)} style={{
                            fontSize: 12, padding: "5px 12px", borderRadius: 8, cursor: "pointer",
                            border: "1px solid " + (months === m ? "var(--v-accent)" : "var(--v-border)"),
                            background: months === m ? "var(--v-accent-subtle)" : "transparent",
                            color: months === m ? "var(--v-cyan)" : "var(--v-text-2)",
                        }}>{m} mois</button>
                    ))}
                </div>
                <button onClick={exportCsv} style={{
                    display: "inline-flex", alignItems: "center", gap: 6, fontSize: 13, fontWeight: 600, padding: "8px 16px", borderRadius: 10, cursor: "pointer",
                    background: "linear-gradient(135deg, var(--v-accent), var(--v-accent-2))", color: "var(--v-text)", border: "none",
                    boxShadow: "0 6px 16px color-mix(in srgb, var(--v-accent) 40%, transparent)",
                }}><Download size={15} /> Exporter (CSV)</button>
            </div>

            {/* KPI phare + secondaires */}
            <Reveal><MoneyHeadline avoided={avoided} coverage={t} /></Reveal>
            <Reveal>
                <Stagger className="grid grid-cols-2 gap-4 md:grid-cols-4" gap={0.06}>
                    <StaggerItem><VisionKpiCard value={N} label="Salariés enregistrés" icon={Users2} hint="base du calcul (réel)" /></StaggerItem>
                    <StaggerItem><VisionKpiCard value={Math.round(t * 100)} suffix="%" label="Couverture formation" icon={GraduationCap} hint="participation × CRI (réel)" /></StaggerItem>
                    <StaggerItem><VisionKpiCard value={hyp.C} suffix=" €" label="Coût moyen / incident" icon={Euro} hint="hypothèse éditable" /></StaggerItem>
                    <StaggerItem><VisionKpiCard value={Math.round(expectedIncidents * 10) / 10} label="Incidents attendus / an" icon={ShieldAlert} hint="estimation (N × p)" /></StaggerItem>
                </Stagger>
            </Reveal>

            {/* Graphes animés */}
            <Reveal>
                <GlassCard>
                    <h2 style={{ fontSize: 16, fontWeight: 600, color: "var(--v-text)", marginBottom: 2 }}>Évolution de la perte potentielle évitée <span style={{ fontSize: 12, fontWeight: 500, color: "var(--v-text-3)" }}>(estimation)</span></h2>
                    <p style={{ fontSize: 12, color: "var(--v-text-3)", marginBottom: 10 }}>Estimation mensuelle, croît avec la couverture réelle de formation · {months} mois</p>
                    {avoidedTrend.length >= 2 ? <VisionAreaChart data={avoidedTrend} yKey="value" domainMax={avoidedMax} height={230} /> : <EmptyState label="Historique insuffisant pour tracer une courbe." />}
                </GlassCard>
            </Reveal>
            <Reveal>
                <div className="grid grid-cols-1 gap-5 lg:grid-cols-2">
                    <GlassCard>
                        <h2 style={{ fontSize: 16, fontWeight: 600, color: "var(--v-text)", marginBottom: 2 }}>Couverture de formation réelle</h2>
                        <p style={{ fontSize: 12, color: "var(--v-text-3)", marginBottom: 10 }}>Participation cumulée × CRI moyen (vraie donnée)</p>
                        {coverageTrend.length >= 2 ? <VisionAreaChart data={coverageTrend} yKey="value" domainMax={100} height={210} /> : <EmptyState label="Historique insuffisant." />}
                    </GlassCard>
                    <GlassCard>
                        <h2 style={{ fontSize: 16, fontWeight: 600, color: "var(--v-text)", marginBottom: 2 }}>Activité de formation</h2>
                        <p style={{ fontSize: 12, color: "var(--v-text-3)", marginBottom: 10 }}>Complétions par mois (vraie donnée)</p>
                        {activityTrend.some(x => x.value > 0) ? <VisionBarChart data={activityTrend} yKey="value" height={210} /> : <EmptyState label="Aucune complétion sur la période." />}
                    </GlassCard>
                </div>
            </Reveal>

            {/* Hypothèses éditables + méthodologie */}
            <Reveal><HypothesesPanel hyp={hyp} setHyp={setHyp} /></Reveal>
            <Reveal><MethodoNote /></Reveal>
        </div>
    );
}

function MoneyHeadline({ avoided, coverage }: { avoided: number; coverage: number }) {
    const n = useAnimatedNumber(Math.round(avoided));
    return (
        <GlassCard style={{ borderColor: "color-mix(in srgb, var(--v-accent) 40%, var(--v-border))" }}>
            <div style={{ display: "flex", alignItems: "flex-start", gap: 14, flexWrap: "wrap" }}>
                <span style={{ display: "inline-flex", width: 44, height: 44, borderRadius: 14, alignItems: "center", justifyContent: "center", background: "var(--v-grad)", color: "var(--v-text)", boxShadow: "0 6px 16px color-mix(in srgb, var(--v-accent) 45%, transparent)", flexShrink: 0 }}><ShieldCheck size={22} /></span>
                <div style={{ minWidth: 0 }}>
                    <div style={{ fontSize: 13, color: "var(--v-text-2)", fontWeight: 500 }}>Perte potentielle évitée <b style={{ color: "var(--v-text)" }}>(estimation)</b> · sur 1 an</div>
                    <div style={{ marginTop: 4, fontSize: 40, fontWeight: 800, color: "var(--v-text)", letterSpacing: "-0.03em", lineHeight: 1.05 }}>{eur.format(n)}</div>
                    <div style={{ marginTop: 6, fontSize: 12, color: "var(--v-text-3)" }}>
                        Estimation indicative, pondérée par votre couverture de formation réelle (<b style={{ color: "var(--v-text-2)" }}>{Math.round(coverage * 100)}%</b>). Ce n&apos;est pas une valeur mesurée ni garantie.
                    </div>
                </div>
            </div>
        </GlassCard>
    );
}

function HypInput({ label, unit, value, min, max, onChange }: { label: string; unit: string; value: number; min: number; max: number; onChange: (v: number) => void }) {
    return (
        <div>
            <label style={{ display: "block", fontSize: 12, color: "var(--v-text-2)", fontWeight: 500, marginBottom: 6 }}>{label}</label>
            <div style={{ display: "flex", alignItems: "center", gap: 8, background: "var(--v-surface-2)", border: "1px solid var(--v-border)", borderRadius: 10, padding: "8px 12px" }}>
                <input type="number" value={value} min={min} max={max}
                    onChange={e => { const v = Number(e.target.value); onChange(Number.isFinite(v) ? Math.min(max, Math.max(min, v)) : min); }}
                    style={{ width: "100%", background: "transparent", border: "none", outline: "none", color: "var(--v-text)", fontSize: 15, fontWeight: 600 }} />
                <span style={{ fontSize: 12, color: "var(--v-text-3)", flexShrink: 0 }}>{unit}</span>
            </div>
        </div>
    );
}

function HypothesesPanel({ hyp, setHyp }: { hyp: Hypotheses; setHyp: (h: Hypotheses) => void }) {
    return (
        <GlassCard>
            <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", gap: 10, marginBottom: 4, flexWrap: "wrap" }}>
                <div style={{ display: "flex", alignItems: "center", gap: 10 }}>
                    <span style={{ display: "inline-flex", width: 34, height: 34, borderRadius: 10, alignItems: "center", justifyContent: "center", background: "var(--v-accent-subtle)", color: "var(--v-cyan)" }}><Settings2 size={18} /></span>
                    <div>
                        <h2 style={{ fontSize: 16, fontWeight: 700, color: "var(--v-text)" }}>Hypothèses de calcul</h2>
                        <p style={{ fontSize: 12, color: "var(--v-text-3)" }}>Ajustez ces valeurs à votre contexte — le rapport se recalcule en direct.</p>
                    </div>
                </div>
                <button onClick={() => setHyp(FIN_DEFAULTS)} style={{ display: "inline-flex", alignItems: "center", gap: 6, fontSize: 12, fontWeight: 500, color: "var(--v-text-2)", background: "transparent", border: "1px solid var(--v-border)", borderRadius: 8, padding: "7px 12px", cursor: "pointer" }}>
                    <RotateCcw size={14} /> Réinitialiser
                </button>
            </div>
            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4" style={{ marginTop: 14 }}>
                <HypInput label="Proba. incident / salarié / an (p)" unit="%" value={hyp.p} min={0} max={100} onChange={v => setHyp({ ...hyp, p: v })} />
                <HypInput label="Coût moyen d'un incident (C)" unit="€" value={hyp.C} min={0} max={100000000} onChange={v => setHyp({ ...hyp, C: v })} />
                <HypInput label="Part facteur humain (h)" unit="%" value={hyp.h} min={0} max={100} onChange={v => setHyp({ ...hyp, h: v })} />
                <HypInput label="Réduction via sensibilisation (r)" unit="%" value={hyp.r} min={0} max={100} onChange={v => setHyp({ ...hyp, r: v })} />
            </div>
        </GlassCard>
    );
}

function MethodoNote() {
    return (
        <GlassCard style={{ background: "color-mix(in srgb, var(--v-surface) 82%, transparent)" }}>
            <div style={{ display: "flex", alignItems: "center", gap: 10, marginBottom: 10 }}>
                <span style={{ display: "inline-flex", width: 30, height: 30, borderRadius: 9, alignItems: "center", justifyContent: "center", background: "var(--v-surface-2)", color: "var(--v-text-2)" }}><Info size={16} /></span>
                <h2 style={{ fontSize: 15, fontWeight: 700, color: "var(--v-text)" }}>Méthodologie &amp; avertissement</h2>
            </div>
            <div style={{ fontSize: 13, color: "var(--v-text-2)", lineHeight: 1.6 }}>
                <p style={{ marginBottom: 8 }}>
                    Formule&nbsp;: <span style={{ fontFamily: "ui-monospace, monospace", fontSize: 12, color: "var(--v-text)", background: "var(--v-surface-2)", padding: "2px 8px", borderRadius: 6, display: "inline-block" }}>Perte évitée (est.) = N × p × C × h × r × t</span>
                </p>
                <ul style={{ margin: "0 0 8px 0", paddingLeft: 18, listStyle: "disc" }}>
                    <li><b style={{ color: "var(--v-text)" }}>N</b> = salariés enregistrés, <b style={{ color: "var(--v-text)" }}>t</b> = couverture réelle de formation (participation × CRI) — <b style={{ color: "var(--v-cyan)" }}>vraies données de votre organisation</b>.</li>
                    <li><b style={{ color: "var(--v-text)" }}>p, C, h, r</b> = hypothèses que vous ajustez ci-dessus. Valeurs par défaut : p&nbsp;10&nbsp;%/an, C&nbsp;466&nbsp;000&nbsp;€, h&nbsp;68&nbsp;%, r&nbsp;25&nbsp;%.</li>
                </ul>
                <p style={{ marginBottom: 8, fontSize: 12, color: "var(--v-text-3)" }}>
                    Repères publics indicatifs&nbsp;: coût moyen d&apos;une cyberattaque pour une <b style={{ color: "var(--v-text-2)" }}>PME française ≈ 466&nbsp;000&nbsp;€</b> (études sectorielles France, ordre de grandeur tous coûts confondus) ; Verizon <i>DBIR 2024</i> (~68&nbsp;% des compromissions impliquent le facteur humain) ; IBM <i>Cost of a Data Breach 2024</i>. Ajustez à votre secteur et à votre taille.
                </p>
                <p style={{ fontSize: 12, color: "var(--warning)", fontWeight: 500 }}>
                    ⚠ Il s&apos;agit d&apos;une <b>estimation indicative</b> destinée à objectiver un ordre de grandeur, <b>pas d&apos;une mesure comptable ni d&apos;une garantie</b> d&apos;économies réalisées.
                </p>
            </div>
        </GlassCard>
    );
}

function ProfileBlock({ loading, p }: { loading: boolean; p?: Profile }) {
    const fmt = (s: string | null | undefined) => s ? new Date(s).toLocaleDateString("fr-FR") : "—";
    return (
        <div>
            <h2 style={{ fontSize: 16, fontWeight: 600, color: "var(--v-text)", marginBottom: 12 }}>Profil</h2>
            {loading ? (
                <div className="grid grid-cols-2 gap-4 md:grid-cols-3">{[0, 1, 2].map(i => <SkelBlock key={i} h={112} />)}</div>
            ) : (
                <>
                    <Stagger className="grid grid-cols-2 gap-4 md:grid-cols-3" gap={0.06}>
                        <StaggerItem><VisionKpiCard value={p?.completions ?? 0} label="Challenges complétés" icon={CheckCircle2} /></StaggerItem>
                        <StaggerItem><VisionKpiCard value={p?.avgScore ?? 0} suffix="%" label="Score moyen" icon={BarChart3} /></StaggerItem>
                        <StaggerItem><VisionKpiCard value={p?.themesAttempted ?? 0} label="Thèmes abordés" icon={Activity} /></StaggerItem>
                    </Stagger>
                    <div style={{ display: "flex", gap: 20, flexWrap: "wrap", marginTop: 14, fontSize: 12, color: "var(--v-text-3)" }}>
                        <span>Dernière activité&nbsp;: <b style={{ color: "var(--v-text-2)" }}>{fmt(p?.lastActivityAt)}</b></span>
                        <span>Dernière connexion&nbsp;: <b style={{ color: "var(--v-text-2)" }}>{fmt(p?.lastLoginAt)}</b></span>
                        <span>Membre depuis&nbsp;: <b style={{ color: "var(--v-text-2)" }}>{fmt(p?.createdAt)}</b></span>
                    </div>
                </>
            )}
        </div>
    );
}
