"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { useQuery } from "@tanstack/react-query";
import { motion, AnimatePresence } from "framer-motion";
import { Trophy, Zap, Lock } from "lucide-react";
import { apiFetch } from "@/lib/api";
import { useCompetitionStatus } from "@/hooks/useCompetitionStatus";
import Reveal from "@/components/Reveal";
import CountUp from "@/components/CountUp";
import VisionCard from "@/components/vision/VisionCard";
import { renderTeamIcon } from "@/components/teams/teamIcons";
import PodiumBoard, { type PodiumItem } from "@/components/competition/PodiumBoard";
import type { ScoreboardEntry, TeamLeaderboardEntry, MyRank, AdminLeaderboard } from "@/components/competition/types";

type Tab = "individual" | "teams" | "admin";

export default function CompetitionPage() {
    const router = useRouter();
    const statusQ = useCompetitionStatus();
    const [tab, setTab] = useState<Tab>("individual");
    const enabled = statusQ.data?.isEnabled === true;

    const meQ = useQuery<{ role: string }>({ queryKey: ["me"], queryFn: () => apiFetch("/api/auth/me"), staleTime: 60_000 });
    const isAdmin = meQ.data?.role === "admin" || meQ.data?.role === "SuperAdmin";

    const myRankQ = useQuery<MyRank>({ queryKey: ["competition", "my-rank"], queryFn: () => apiFetch("/api/competition/my-rank"), enabled, staleTime: 10_000 });
    const topQ = useQuery<ScoreboardEntry[]>({ queryKey: ["competition", "top5"], queryFn: () => apiFetch("/api/competition/individual/top5"), enabled: enabled && tab === "individual", staleTime: 15_000 });
    const teamsQ = useQuery<TeamLeaderboardEntry[]>({ queryKey: ["competition", "teams"], queryFn: () => apiFetch("/api/competition/leaderboard/teams"), enabled: enabled && tab === "teams", staleTime: 10_000 });
    const adminQ = useQuery<AdminLeaderboard>({ queryKey: ["competition", "admin-full"], queryFn: () => apiFetch("/api/admin/competition/leaderboard?page=1&pageSize=200"), enabled: enabled && tab === "admin" && isAdmin, staleTime: 10_000 });

    if (statusQ.isLoading) {
        return <div className="vision-dashboard" style={{ minHeight: "100%" }}><div style={{ maxWidth: 960, margin: "0 auto", padding: "32px 20px" }}><SkelBlock h={160} /></div></div>;
    }

    if (!enabled) {
        return (
            <div className="vision-dashboard" style={{ minHeight: "100%" }}>
                <div style={{ maxWidth: 640, margin: "0 auto", padding: "48px 20px", textAlign: "center" }}>
                    <div style={{ margin: "0 auto", display: "flex", height: 56, width: 56, alignItems: "center", justifyContent: "center", borderRadius: 999, background: "var(--v-surface-2)", color: "var(--v-text-2)" }}><Trophy size={22} /></div>
                    <h1 style={{ marginTop: 16, fontSize: 20, fontWeight: 700, color: "var(--v-text)" }}>Mode Compétition désactivé</h1>
                    <p style={{ marginTop: 8, fontSize: 14, color: "var(--v-text-2)", lineHeight: 1.55 }}>Le mode compétition n&apos;est pas actif pour votre organisation. Demandez à un administrateur de l&apos;activer.</p>
                    <button type="button" onClick={() => router.replace("/dashboard")} style={{ marginTop: 18, display: "inline-flex", alignItems: "center", gap: 8, borderRadius: 10, background: "var(--v-grad)", color: "var(--v-text)", border: "none", padding: "10px 16px", fontWeight: 600, cursor: "pointer" }}>Retour au tableau de bord</button>
                </div>
            </div>
        );
    }

    const mr = myRankQ.data;
    const tabs: [Tab, string][] = [["individual", "Individuel"], ["teams", "Équipe"], ...(isAdmin ? [["admin", "Classement complet"] as [Tab, string]] : [])];

    return (
        <div className="vision-dashboard" style={{ minHeight: "100%" }}>
            <div style={{ maxWidth: 960, margin: "0 auto", padding: "24px 20px 80px", display: "flex", flexDirection: "column", gap: 18 }}>
                <Reveal>
                    <div style={{ display: "flex", flexWrap: "wrap", alignItems: "center", justifyContent: "space-between", gap: 12 }}>
                        <div>
                            <h1 style={{ fontSize: 26, fontWeight: 700, color: "var(--v-text)", letterSpacing: "-0.02em" }}>Classement</h1>
                            <p style={{ marginTop: 4, fontSize: 13.5, color: "var(--v-text-2)" }}>On valorise les meilleurs. Votre position exacte reste privée.</p>
                        </div>
                        <span style={{ display: "inline-flex", alignItems: "center", gap: 6, borderRadius: 999, background: "color-mix(in srgb, var(--v-success) 15%, transparent)", padding: "5px 12px", fontSize: 12, fontWeight: 600, color: "var(--v-success)" }}>
                            <span style={{ height: 7, width: 7, borderRadius: 999, background: "var(--v-success)" }} /> Compétition active
                        </span>
                    </div>
                </Reveal>

                {/* Ma position — PRIVÉ (visible uniquement par le membre connecté) */}
                {mr && (
                    <Reveal>
                        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
                            <VisionCard hover padding={18}>
                                <p style={{ fontSize: 11, textTransform: "uppercase", letterSpacing: "0.05em", color: "var(--v-text-2)" }}>Ma position (privée)</p>
                                <div style={{ marginTop: 4, display: "flex", alignItems: "flex-end", gap: 8 }}>
                                    <span style={{ fontSize: 28, fontWeight: 800, color: "var(--v-text)" }}>{mr.individualRank ? `#${mr.individualRank}` : "—"}</span>
                                    <span style={{ fontSize: 13, color: "var(--v-text-2)", marginBottom: 4 }}>/ {mr.totalParticipants}</span>
                                </div>
                                <p style={{ marginTop: 4, fontSize: 14, fontWeight: 700, color: "var(--v-accent)" }}>
                                    <CountUp value={mr.individualScore} /> pts
                                    {mr.individualSpeedBonus > 0 && <span style={{ marginLeft: 8, display: "inline-flex", alignItems: "center", gap: 3, fontSize: 12, fontWeight: 600, color: "var(--warning)" }}><Zap size={12} /> +{mr.individualSpeedBonus} rapidité</span>}
                                </p>
                            </VisionCard>
                            <VisionCard hover padding={18}>
                                <p style={{ fontSize: 11, textTransform: "uppercase", letterSpacing: "0.05em", color: "var(--v-text-2)" }}>Mon équipe</p>
                                <div style={{ marginTop: 4, display: "flex", alignItems: "center", gap: 8 }}>
                                    {mr.teamColor && <span aria-hidden style={{ display: "flex", height: 22, width: 22, alignItems: "center", justifyContent: "center", borderRadius: 6, color: "var(--v-text)", background: mr.teamColor }}>{renderTeamIcon(mr.teamIcon, 12)}</span>}
                                    <span style={{ overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap", fontSize: 17, fontWeight: 700, color: "var(--v-text)" }}>{mr.teamName ?? "Aucune équipe"}</span>
                                </div>
                                <p style={{ marginTop: 4, fontSize: 14, fontWeight: 600, color: "var(--v-text-2)" }}>{mr.teamRank ? `#${mr.teamRank} / ${mr.totalTeams}` : "—"} · {mr.teamScore} pts</p>
                            </VisionCard>
                        </div>
                    </Reveal>
                )}

                {/* Onglets */}
                <div style={{ display: "flex", gap: 6, borderRadius: 12, border: "1px solid var(--v-border)", background: "var(--v-surface-2)", padding: 4 }}>
                    {tabs.map(([k, label]) => (
                        <button key={k} type="button" onClick={() => setTab(k)} style={{
                            flex: 1, borderRadius: 9, padding: "9px 10px", fontSize: 13, fontWeight: 600, cursor: "pointer", border: "none",
                            background: tab === k ? "var(--v-grad)" : "transparent",
                            color: tab === k ? "var(--v-text)" : "var(--v-text-2)",
                            transition: "background-color .15s ease, color .15s ease",
                            display: "inline-flex", alignItems: "center", justifyContent: "center", gap: 6,
                        }}>{k === "admin" && <Lock size={13} />}{label}</button>
                    ))}
                </div>

                <AnimatePresence mode="wait">
                    <motion.div key={tab} initial={{ opacity: 0, y: 8 }} animate={{ opacity: 1, y: 0 }} exit={{ opacity: 0 }} transition={{ duration: 0.22 }}>
                        {tab === "individual" && <IndividualTop5 loading={topQ.isLoading} entries={topQ.data ?? []} />}
                        {tab === "teams" && <TeamRanking loading={teamsQ.isLoading} teams={teamsQ.data ?? []} />}
                        {tab === "admin" && isAdmin && <AdminFull loading={adminQ.isLoading} data={adminQ.data} />}
                    </motion.div>
                </AnimatePresence>
            </div>
        </div>
    );
}

// ── Podium individuel TOP 5 (public) — utilise le podium partagé ─────────────
function IndividualTop5({ loading, entries }: { loading: boolean; entries: ScoreboardEntry[] }) {
    if (loading) return <VisionCard><div className="grid grid-cols-3 gap-3">{[0, 1, 2].map(i => <SkelBlock key={i} h={150} />)}</div></VisionCard>;
    if (entries.length === 0) return <VisionCard><EmptyState label="Aucun score pour le moment. Complétez des challenges pour apparaître au classement." /></VisionCard>;

    const items: PodiumItem[] = entries.map(e => ({
        key: e.userId,
        rank: e.rank,
        name: e.displayName,
        score: e.score,
        subtitle: `${e.challengesCompleted} challenge${e.challengesCompleted > 1 ? "s" : ""}`,
        isCurrent: e.isCurrentUser,
        currentLabel: "vous",
        avatarBg: "var(--v-grad)",
        avatarContent: e.initials,
    }));
    return <PodiumBoard title="Podium — top 5" items={items} />;
}

// ── Podium par ÉQUIPE (collectif, complet) — MÊME podium partagé que l'individuel ─
function TeamRanking({ loading, teams }: { loading: boolean; teams: TeamLeaderboardEntry[] }) {
    if (loading) return <VisionCard><SkelBlock h={200} /></VisionCard>;
    if (teams.length === 0) return <VisionCard><EmptyState label="Aucune équipe classée. Créez des équipes et assignez-y des membres." /></VisionCard>;

    const items: PodiumItem[] = teams.map(t => ({
        key: t.teamId,
        rank: t.rank,
        name: t.name,
        score: t.score,
        subtitle: `${t.memberCount} membre${t.memberCount > 1 ? "s" : ""}`,
        isCurrent: t.isCurrentUserTeam,
        currentLabel: "mon équipe",
        avatarBg: t.color ?? "var(--v-accent)", // pastille couleur d'équipe (identité — conservée)
        avatarContent: renderTeamIcon(t.icon, 18),
    }));
    return <PodiumBoard title="Podium des équipes" items={items} footer="Score d'équipe = somme des scores de ses membres." />;
}

// ── Classement complet nominatif — RÉSERVÉ ADMIN ─────────────────────────────
function AdminFull({ loading, data }: { loading: boolean; data?: AdminLeaderboard }) {
    if (loading) return <VisionCard><SkelBlock h={240} /></VisionCard>;
    if (!data) return <VisionCard><EmptyState label="Classement indisponible." /></VisionCard>;
    const items = data.ranking.items;
    return (
        <VisionCard padding={0}>
            <div style={{ display: "flex", alignItems: "flex-start", gap: 8, padding: "14px 18px", borderBottom: "1px solid var(--v-border)", background: "color-mix(in srgb, var(--warning) 10%, transparent)" }}>
                <Lock size={15} style={{ color: "var(--warning)", flexShrink: 0, marginTop: 1 }} />
                <p style={{ fontSize: 12.5, color: "var(--v-text-2)", lineHeight: 1.5 }}>{data.purpose}</p>
            </div>
            <div style={{ overflowX: "auto" }}>
                <table style={{ width: "100%", borderCollapse: "collapse", fontSize: 13.5 }}>
                    <thead style={{ background: "var(--v-surface-2)" }}>
                        <tr>{["#", "Membre", "Équipe", "Score", "Challenges"].map((h, i) => <th key={h} style={{ padding: "10px 16px", textAlign: i >= 3 ? "right" : "left", fontSize: 11, fontWeight: 700, textTransform: "uppercase", letterSpacing: "0.05em", color: "var(--v-text-2)" }}>{h}</th>)}</tr>
                    </thead>
                    <tbody>
                        {items.map(e => (
                            <tr key={e.userId} className="v-row" style={{ borderTop: "1px solid var(--v-border)", background: e.isCurrentUser ? "color-mix(in srgb, var(--v-accent) 8%, transparent)" : "transparent" }}>
                                <td style={{ padding: "12px 16px", color: "var(--v-text-2)", fontWeight: 700 }}>{e.rank}</td>
                                <td style={{ padding: "12px 16px", color: "var(--v-text)" }}>{e.displayName}</td>
                                <td style={{ padding: "12px 16px", color: "var(--v-text-2)" }}>{e.teamName ?? "—"}</td>
                                <td style={{ padding: "12px 16px", textAlign: "right", fontWeight: 700, color: "var(--v-accent)" }}>{e.score}</td>
                                <td style={{ padding: "12px 16px", textAlign: "right", color: "var(--v-text-2)" }}>{e.challengesCompleted}</td>
                            </tr>
                        ))}
                        {items.length === 0 && <tr><td colSpan={5} style={{ padding: "40px 16px", textAlign: "center", color: "var(--v-text-2)" }}>Aucun participant classé.</td></tr>}
                    </tbody>
                </table>
            </div>
        </VisionCard>
    );
}

function SkelBlock({ h }: { h: number }) { return <div style={{ height: h, borderRadius: 16, background: "var(--v-surface-2)", opacity: 0.6 }} />; }
function EmptyState({ label }: { label: string }) { return <div style={{ minHeight: 120, display: "flex", alignItems: "center", justifyContent: "center", textAlign: "center", fontSize: 13.5, color: "var(--v-text-2)", padding: 16 }}>{label}</div>; }
