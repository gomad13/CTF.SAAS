"use client";

import { useEffect, useMemo, useRef, useState } from "react";
import { useRouter } from "next/navigation";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiFetch } from "@/lib/api";
import type { Me } from "@/lib/types";
import ModesSettings from "@/components/admin/ModesSettings";
import { useIsMobile } from "@/hooks/useMediaQuery";
import Reveal from "@/components/Reveal";
import { Stagger, StaggerItem } from "@/components/Stagger";
import CountUp from "@/components/CountUp";
import PremiumChartCard from "@/components/premium/ChartCard";
import AreaChartCard from "@/components/premium/AreaChartCard";
import DonutChart from "@/components/premium/DonutChart";

// Blocs Vision UI (VRAIES données) — types + états chargement/vide
type AdminMonthlyPoint = { label: string; value: number };
type AdminUsersByStatus = { actifs: number; suspendus: number; jamaisConnectes: number; total: number };
function OverviewSkel({ h }: { h: number }) {
    return <div style={{ height: h, borderRadius: 12, background: "var(--surface-2)", opacity: 0.6 }} />;
}
function OverviewEmpty({ label }: { label: string }) {
    return <div style={{ height: 240, display: "flex", alignItems: "center", justifyContent: "center", textAlign: "center", fontSize: 13, color: "var(--text-3)", padding: 16 }}>{label}</div>;
}

type Company = {
    id: string;
    name: string;
    sector: string;
    city: string;
    siret: string;
    employeeCount: number;
};

type CompanyUser = {
    id: string;
    firstName: string;
    lastName: string;
    email: string;
    role: string;
    isActive: boolean;
    createdAt: string;
    lastLoginAt: string | null;
    totalPoints: number;
    maxPoints: number;
    completedChallenges: number;
    progressPercent: number;
    averageScore: number;
    completedParcours: number;
    totalParcours: number;
};

type CompletionDay = { date: string; count: number };
type ScoreBucket   = { range: string; count: number };
type Hardest       = { title: string; averageScore: number; attempts: number };
type TopPerformer  = { name: string; initials: string; totalPoints: number; averageScore: number; completedChallenges: number };
type ParcoursStat  = { title: string; completionRate: number; averageScore: number; totalCompletions: number };

type CompanyStats = {
    totalUsers: number;
    activeUsers: number;
    inactiveUsers: number;
    totalCompletions: number;
    averageScore: number;
    averageProgress: number;
    totalPointsEarned: number;
    completionsByDay: CompletionDay[];
    scoreDistribution: ScoreBucket[];
    hardestChallenges: Hardest[];
    topPerformers: TopPerformer[];
    parcoursStats: ParcoursStat[];
};

// eslint-disable-next-line @typescript-eslint/no-explicit-any
declare global { interface Window { Chart?: any } }

function scoreColor(score: number) {
    // Palette ajustée pour WCAG AA (>= 4.5:1) sur fond blanc.
    if (score >= 80) return "var(--success)"; // emerald-700
    if (score >= 50) return "var(--warning)"; // amber-700
    return "var(--danger)";                   // red-700
}

export default function AdminPage() {
    const router = useRouter();
    const qc = useQueryClient();
    const isMobile = useIsMobile();
    const [tab, setTab] = useState<"users" | "stats" | "settings">("users");

    const meQ = useQuery<Me>({
        queryKey: ["me"],
        queryFn: () => apiFetch<Me>("/api/auth/me"),
        staleTime: 5 * 60 * 1000,
    });

    useEffect(() => {
        if (meQ.data && meQ.data.role !== "admin" && meQ.data.role !== "SuperAdmin") router.replace("/dashboard");
    }, [meQ.data, router]);

    const enabled = !!meQ.data && (meQ.data.role === "admin" || meQ.data.role === "SuperAdmin");

    const companyQ = useQuery<Company>({
        queryKey: ["admin", "company"],
        queryFn: () => apiFetch<Company>("/api/admin/company"),
        enabled,
    });
    const usersQ = useQuery<{ data: CompanyUser[]; pagination: { page: number; pageSize: number; total: number; totalPages: number } }>({
        queryKey: ["admin", "users"],
        queryFn: () => apiFetch("/api/admin/users?pageSize=200"),
        enabled,
    });
    const statsQ = useQuery<CompanyStats>({
        queryKey: ["admin", "stats"],
        queryFn: () => apiFetch<CompanyStats>("/api/admin/stats"),
        enabled,
    });

    const toggleM = useMutation({
        mutationFn: (userId: string) =>
            apiFetch(`/api/admin/users/${userId}/toggle-active`, { method: "PATCH" }),
        onSuccess: () => {
            qc.invalidateQueries({ queryKey: ["admin", "users"] });
            qc.invalidateQueries({ queryKey: ["admin", "stats"] });
        },
    });

    if (meQ.isLoading || !meQ.data) return <div style={{ padding: 32 }} />;
    if (meQ.data.role !== "admin" && meQ.data.role !== "SuperAdmin") return null;

    const company = companyQ.data;
    const users = usersQ.data?.data ?? [];
    const stats = statsQ.data;

    return (
        <div style={{ maxWidth: 1400, margin: "0 auto", padding: "var(--page-x) var(--page-x) 80px" }}>
            <Reveal>
                <h1 style={{ fontSize: 26, fontWeight: 700, color: "var(--text)", margin: 0 }}>
                    Administration
                </h1>
                <p style={{ marginTop: 6, fontSize: 13, color: "var(--text-2)" }}>
                    {company?.name ?? "—"} · {users.length} collaborateur{users.length > 1 ? "s" : ""}
                </p>
            </Reveal>

            <div style={{ display: "flex", gap: 8, marginTop: 24, borderBottom: "1px solid rgba(255,255,255,0.08)", paddingBottom: 0, overflowX: "auto" }}>
                <TabBtn active={tab === "users"} onClick={() => setTab("users")}>Collaborateurs</TabBtn>
                <TabBtn active={tab === "stats"} onClick={() => setTab("stats")}>Statistiques</TabBtn>
                <TabBtn active={tab === "settings"} onClick={() => setTab("settings")}>Paramètres</TabBtn>
            </div>

            {tab === "users" && <UsersTab users={users} stats={stats} onToggle={(id) => toggleM.mutate(id)} isMobile={isMobile} />}
            {tab === "stats" && stats && <StatsTab stats={stats} isMobile={isMobile} />}
            {tab === "settings" && (
                <div style={{ marginTop: 24 }}>
                    <ModesSettings />
                </div>
            )}
        </div>
    );
}

function TabBtn({ active, onClick, children }: { active: boolean; onClick: () => void; children: React.ReactNode }) {
    return (
        <button
            onClick={onClick}
            style={{
                padding: "10px 18px",
                borderRadius: 0,
                background: "transparent",
                color: active ? "var(--accent)" : "var(--text-3)",
                border: "none",
                borderBottom: active ? "2px solid var(--accent)" : "2px solid transparent",
                marginBottom: "-1px",
                cursor: "pointer",
                fontSize: 14,
                fontWeight: active ? 600 : 500,
                transition: "color 0.15s, border-color 0.15s",
            }}
            onMouseEnter={e => { if (!active) (e.currentTarget as HTMLButtonElement).style.color = "var(--text-2)"; }}
            onMouseLeave={e => { if (!active) (e.currentTarget as HTMLButtonElement).style.color = "var(--text-3)"; }}
        >
            {children}
        </button>
    );
}

// ── ONGLET COLLABORATEURS ───────────────────────────────────────────────────
function UsersTab({ users, stats, onToggle, isMobile }: {
    users: CompanyUser[];
    stats: CompanyStats | undefined;
    onToggle: (id: string) => void;
    isMobile: boolean;
}) {
    const [search, setSearch] = useState("");
    const [sortBy, setSortBy] = useState<"points" | "score" | "name">("points");
    const [detailUser, setDetailUser] = useState<CompanyUser | null>(null);

    const filtered = useMemo(() => {
        const q = search.toLowerCase();
        return users
            .filter(u => `${u.firstName} ${u.lastName} ${u.email}`.toLowerCase().includes(q))
            .sort((a, b) => {
                if (sortBy === "points") return b.totalPoints - a.totalPoints;
                if (sortBy === "score")  return b.averageScore - a.averageScore;
                return a.lastName.localeCompare(b.lastName);
            });
    }, [users, search, sortBy]);

    return (
        <div style={{ marginTop: 32 }}>
            <Stagger className="mb-6 grid grid-cols-2 gap-4 md:grid-cols-4" gap={0.06}>
                <StaggerItem><Kpi label="COLLABORATEURS" value={stats?.totalUsers ?? users.length} color="var(--text)" /></StaggerItem>
                <StaggerItem><Kpi label="ACTIFS" value={stats?.activeUsers ?? 0} color="var(--success)" /></StaggerItem>
                <StaggerItem><Kpi label="FORMATIONS FAITES" value={stats?.totalCompletions ?? 0} color="var(--accent)" /></StaggerItem>
                <StaggerItem><Kpi label="SCORE MOYEN" value={`${stats?.averageScore ?? 0}%`} color={scoreColor(stats?.averageScore ?? 0)} /></StaggerItem>
            </Stagger>

            <ImportExportBar />

            <div style={{ display: "flex", justifyContent: "space-between", marginBottom: 16, gap: 12, flexWrap: "wrap" }}>
                <input
                    value={search}
                    onChange={e => setSearch(e.target.value)}
                    placeholder="Rechercher un collaborateur..."
                    style={{
                        background: "var(--surface)",
                        border: "1px solid var(--border)",
                        borderRadius: 7,
                        padding: "9px 14px",
                        color: "var(--text)",
                        fontSize: 13,
                        width: isMobile ? "100%" : 280,
                        flex: isMobile ? "1 1 100%" : undefined,
                        outline: "none",
                    }}
                    onFocus={e => { e.currentTarget.style.borderColor = "rgba(255,255,255,0.25)"; }}
                    onBlur={e => { e.currentTarget.style.borderColor = "rgba(255,255,255,0.08)"; }}
                />
                <select
                    value={sortBy}
                    onChange={e => setSortBy(e.target.value as "points" | "score" | "name")}
                    style={{
                        background: "var(--surface)",
                        border: "1px solid var(--border)",
                        borderRadius: 7,
                        padding: "9px 14px",
                        color: "var(--text)",
                        fontSize: 13,
                        outline: "none",
                        cursor: "pointer",
                    }}
                >
                    <option value="points">Trier par points</option>
                    <option value="score">Trier par score</option>
                    <option value="name">Trier par nom</option>
                </select>
            </div>

            <div style={{
                background: "var(--surface)",
                border: "1px solid var(--border)",
                borderRadius: 10,
                overflow: "hidden",
            }}>
                {!isMobile && (
                <div style={{
                    display: "grid",
                    gridTemplateColumns: "1.6fr 0.9fr 1.2fr 0.8fr 0.7fr 0.7fr 0.9fr",
                    gap: 12,
                    padding: "12px 18px",
                    background: "var(--surface)",
                    borderBottom: "1px solid var(--border)",
                    fontFamily: "'JetBrains Mono', monospace",
                    fontSize: 10,
                    letterSpacing: "0.1em",
                    textTransform: "uppercase",
                    color: "var(--text-2)",
                }}>
                    <div>NOM</div>
                    <div>PARCOURS</div>
                    <div>PROGRESSION</div>
                    <div>SCORE MOY.</div>
                    <div style={{ textAlign: "right" }}>POINTS</div>
                    <div>STATUT</div>
                    <div style={{ textAlign: "right" }}>ACTIONS</div>
                </div>
                )}

                <Stagger gap={0.03}>
                {filtered.map(u => {
                    const initials = `${(u.firstName[0] ?? "").toUpperCase()}${(u.lastName[0] ?? "").toUpperCase()}`;
                    const progColor = scoreColor(u.progressPercent);
                    const scoreCol  = scoreColor(u.averageScore);
                    return (
                        <StaggerItem key={u.id}><div style={{
                            display: isMobile ? "flex" : "grid",
                            flexDirection: isMobile ? "column" : undefined,
                            gridTemplateColumns: "1.6fr 0.9fr 1.2fr 0.8fr 0.7fr 0.7fr 0.9fr",
                            gap: 12,
                            padding: "14px 18px",
                            borderBottom: "1px solid var(--border)",
                            alignItems: isMobile ? "stretch" : "center",
                            transition: "background 0.15s",
                        }}
                            onMouseOver={e => { e.currentTarget.style.background = "rgba(255,255,255,0.02)"; }}
                            onMouseOut={e => { e.currentTarget.style.background = "transparent"; }}
                        >
                            <div style={{ display: "flex", alignItems: "center", gap: 10, minWidth: 0 }}>
                                <div style={{
                                    width: 36, height: 36, borderRadius: "50%",
                                    background: "linear-gradient(135deg, var(--accent), var(--accent))",
                                    display: "flex", alignItems: "center", justifyContent: "center",
                                    fontSize: 12, fontWeight: 700, color: "var(--on-accent)",
                                    flexShrink: 0,
                                }}>{initials}</div>
                                <div style={{ minWidth: 0 }}>
                                    <div style={{ fontSize: 14, fontWeight: 500, color: "var(--text)", overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap" }}>
                                        {u.firstName} {u.lastName}
                                    </div>
                                    <div style={{ fontSize: 11, color: "var(--text-2)", overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap" }}>
                                        {u.email}
                                    </div>
                                </div>
                            </div>

                            <div style={{ display: "flex", alignItems: "center", gap: 6 }}>
                                <span style={{ fontSize: 13, color: "var(--text-2)" }}>
                                    {u.completedParcours}/{u.totalParcours}
                                </span>
                                <div style={{ display: "flex", gap: 3 }}>
                                    {Array.from({ length: u.totalParcours }).map((_, i) => (
                                        <span key={i} style={{
                                            width: 6, height: 6, borderRadius: "50%",
                                            background: i < u.completedParcours ? "var(--success)" : "var(--surface-2)",
                                        }} />
                                    ))}
                                </div>
                            </div>

                            <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
                                <div style={{ flex: 1, height: 4, background: "var(--border)", borderRadius: 2, overflow: "hidden" }}>
                                    <div style={{ width: `${u.progressPercent}%`, height: "100%", background: progColor, transition: "width 0.3s" }} />
                                </div>
                                <span style={{ fontSize: 11, color: "var(--text-2)", fontFamily: "'JetBrains Mono', monospace", minWidth: 30, textAlign: "right" }}>
                                    {u.progressPercent}%
                                </span>
                            </div>

                            <span style={{ fontSize: 14, fontWeight: 600, color: scoreCol, fontFamily: "'JetBrains Mono', monospace" }}>
                                {u.averageScore}%
                            </span>

                            <span style={{ fontSize: 14, fontWeight: 700, color: "var(--text)", fontFamily: "'JetBrains Mono', monospace", textAlign: "right" }}>
                                {u.totalPoints}
                            </span>

                            <span style={{
                                fontSize: 10,
                                padding: "2px 8px",
                                borderRadius: 4,
                                background: u.isActive ? "rgba(117,81,255,0.10)" : "rgba(239,68,68,0.10)",
                                border: `1px solid ${u.isActive ? "rgba(117,81,255,0.35)" : "rgba(239,68,68,0.35)"}`,
                                color: u.isActive ? "var(--success-t)" : "var(--danger-t)",
                                fontFamily: "'JetBrains Mono', monospace",
                                width: "fit-content",
                                textTransform: "uppercase",
                            }}>
                                {u.isActive ? "ACTIF" : "INACTIF"}
                            </span>

                            <div style={{ display: "flex", gap: 6, justifyContent: "flex-end" }}>
                                <button
                                    onClick={() => setDetailUser(u)}
                                    style={{
                                        background: "transparent",
                                        border: "1px solid rgba(255,255,255,0.1)",
                                        color: "var(--text-2)",
                                        fontSize: 11,
                                        padding: "5px 10px",
                                        borderRadius: 5,
                                        cursor: "pointer",
                                    }}
                                >Détail</button>
                                <button
                                    onClick={() => onToggle(u.id)}
                                    style={{
                                        background: "transparent",
                                        border: `1px solid ${u.isActive ? "rgba(239,68,68,0.3)" : "rgba(117,81,255,0.3)"}`,
                                        color: u.isActive ? "var(--danger)" : "var(--success)",
                                        fontSize: 11,
                                        padding: "5px 10px",
                                        borderRadius: 5,
                                        cursor: "pointer",
                                    }}
                                >{u.isActive ? "Désactiver" : "Activer"}</button>
                            </div>
                        </div></StaggerItem>
                    );
                })}
                </Stagger>

                {filtered.length === 0 && (
                    <div style={{ padding: 32, textAlign: "center", color: "var(--text-2)", fontSize: 13 }}>
                        Aucun collaborateur trouvé.
                    </div>
                )}
            </div>

            {detailUser && <UserDetailModal user={detailUser} onClose={() => setDetailUser(null)} />}
        </div>
    );
}

function Kpi({ label, value, color }: { label: string; value: string | number; color: string }) {
    return (
        <div style={{
            background: "var(--surface)",
            border: "1px solid var(--border)",
            borderRadius: 8,
            padding: 20,
        }}>
            <div style={{ fontSize: 32, fontWeight: 700, fontFamily: "'JetBrains Mono', monospace", color }}>
                {typeof value === "number" ? <CountUp value={value} /> : value}
            </div>
            <div style={{ fontSize: 11, letterSpacing: "0.1em", textTransform: "uppercase", color: "var(--text-2)", marginTop: 4 }}>
                {label}
            </div>
        </div>
    );
}

function UserDetailModal({ user, onClose }: { user: CompanyUser; onClose: () => void }) {
    const initials = `${(user.firstName[0] ?? "").toUpperCase()}${(user.lastName[0] ?? "").toUpperCase()}`;
    return (
        <div style={{
            position: "fixed", inset: 0, background: "rgba(0,0,0,0.7)",
            display: "flex", alignItems: "center", justifyContent: "center",
            zIndex: 1000, padding: 16,
        }} onClick={onClose}>
            <div onClick={e => e.stopPropagation()} style={{
                background: "var(--surface)",
                border: "1px solid rgba(255,255,255,0.1)",
                borderRadius: 12,
                padding: 28,
                maxWidth: 480,
                width: "100%",
            }}>
                <div style={{ display: "flex", alignItems: "center", gap: 14, marginBottom: 20 }}>
                    <div style={{
                        width: 64, height: 64, borderRadius: "50%",
                        background: "linear-gradient(135deg, var(--accent), var(--accent))",
                        display: "flex", alignItems: "center", justifyContent: "center",
                        fontSize: 22, fontWeight: 700, color: "var(--on-accent)",
                    }}>{initials}</div>
                    <div>
                        <h3 style={{ fontSize: 18, fontWeight: 700, color: "var(--text)", margin: 0 }}>
                            {user.firstName} {user.lastName}
                        </h3>
                        <p style={{ fontSize: 13, color: "var(--text-2)", margin: "2px 0 0" }}>{user.email}</p>
                        <span style={{
                            display: "inline-block",
                            marginTop: 6,
                            fontSize: 10,
                            padding: "2px 8px",
                            borderRadius: 4,
                            background: "rgba(117,81,255,0.12)",
                            border: "1px solid rgba(117,81,255,0.3)",
                            color: "var(--pr)",
                            fontFamily: "'JetBrains Mono', monospace",
                            textTransform: "uppercase",
                        }}>{user.role}</span>
                    </div>
                </div>

                <div style={{ height: 1, background: "rgba(255,255,255,0.08)", margin: "16px 0" }} />

                <div style={{ display: "grid", gridTemplateColumns: "repeat(3, 1fr)", gap: 12 }}>
                    <DetailStat label="POINTS" value={user.totalPoints} />
                    <DetailStat label="SCORE MOY." value={`${user.averageScore}%`} color={scoreColor(user.averageScore)} />
                    <DetailStat label="MODULES" value={user.completedChallenges} />
                </div>

                <div style={{ marginTop: 20 }}>
                    <div style={{ fontSize: 11, color: "var(--text-2)", marginBottom: 8, letterSpacing: "0.1em", textTransform: "uppercase" }}>
                        PROGRESSION GLOBALE
                    </div>
                    <div style={{ display: "flex", alignItems: "center", gap: 10 }}>
                        <div style={{ flex: 1, height: 6, background: "var(--border)", borderRadius: 3, overflow: "hidden" }}>
                            <div style={{ width: `${user.progressPercent}%`, height: "100%", background: scoreColor(user.progressPercent) }} />
                        </div>
                        <span style={{ fontSize: 13, color: "var(--text)", fontFamily: "'JetBrains Mono', monospace" }}>{user.progressPercent}%</span>
                    </div>
                    <div style={{ marginTop: 4, fontSize: 11, color: "var(--text-2)" }}>
                        {user.completedParcours}/{user.totalParcours} parcours complétés
                    </div>
                </div>

                <div style={{ display: "flex", justifyContent: "flex-end", marginTop: 24 }}>
                    <button onClick={onClose} style={{
                        background: "linear-gradient(135deg, var(--accent), var(--accent))",
                        color: "var(--on-accent)", border: "none",
                        padding: "9px 18px", borderRadius: 7,
                        fontSize: 13, fontWeight: 600, cursor: "pointer",
                    }}>Fermer</button>
                </div>
            </div>
        </div>
    );
}

function DetailStat({ label, value, color = "var(--text)" }: { label: string; value: string | number; color?: string }) {
    return (
        <div style={{ background: "var(--surface)", border: "1px solid var(--border)", borderRadius: 7, padding: 12, textAlign: "center" }}>
            <div style={{ fontSize: 20, fontWeight: 700, color, fontFamily: "'JetBrains Mono', monospace" }}>{value}</div>
            <div style={{ fontSize: 9, color: "var(--text-2)", marginTop: 2, letterSpacing: "0.1em" }}>{label}</div>
        </div>
    );
}

// ── ONGLET STATISTIQUES ─────────────────────────────────────────────────────
function StatsTab({ stats, isMobile }: { stats: CompanyStats; isMobile: boolean }) {
    const [chartReady, setChartReady] = useState(false);
    const lineRef = useRef<HTMLCanvasElement>(null);
    const barRef  = useRef<HTMLCanvasElement>(null);
    const donutRef = useRef<HTMLCanvasElement>(null);
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    const chartsRef = useRef<any[]>([]);

    // Blocs Vision UI — VRAIES données du tenant (activité mensuelle + répartition par statut)
    const activityQ = useQuery<AdminMonthlyPoint[]>({ queryKey: ["admin", "activity-monthly"], queryFn: () => apiFetch<AdminMonthlyPoint[]>("/api/admin/stats/activity-monthly?months=6") });
    const statusQ = useQuery<AdminUsersByStatus>({ queryKey: ["admin", "users-by-status"], queryFn: () => apiFetch<AdminUsersByStatus>("/api/admin/stats/users-by-status") });
    const activityData = activityQ.data ?? [];
    const activityHasData = activityData.some(p => p.value > 0);
    const activityMax = Math.max(4, ...activityData.map(p => p.value), 0);
    const statusDonut = statusQ.data ? [
        { name: "Actifs", value: statusQ.data.actifs },
        { name: "Suspendus", value: statusQ.data.suspendus },
        { name: "Jamais connectés", value: statusQ.data.jamaisConnectes },
    ].filter(d => d.value > 0) : [];

    useEffect(() => {
        if (window.Chart) { setChartReady(true); return; }
        const script = document.createElement("script");
        script.src = "https://cdnjs.cloudflare.com/ajax/libs/Chart.js/4.4.1/chart.umd.min.js";
        script.onload = () => setChartReady(true);
        document.head.appendChild(script);
    }, []);

    useEffect(() => {
        if (!chartReady || !window.Chart) return;
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        const Chart = window.Chart as any;
        chartsRef.current.forEach(c => c?.destroy?.());
        chartsRef.current = [];

        const gridColor = "rgba(255,255,255,0.05)";
        const tickColor = "var(--text-3)";

        if (lineRef.current) {
            chartsRef.current.push(new Chart(lineRef.current, {
                type: "line",
                data: {
                    labels: stats.completionsByDay.map(d => d.date.slice(5)),
                    datasets: [{
                        label: "Formations",
                        data: stats.completionsByDay.map(d => d.count),
                        borderColor: "var(--pr)",
                        backgroundColor: "rgba(117,81,255,0.08)",
                        fill: true,
                        tension: 0.4,
                        pointBackgroundColor: "var(--pr-l)",
                    }],
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    plugins: { legend: { display: false } },
                    scales: {
                        x: { grid: { color: gridColor }, ticks: { color: tickColor } },
                        y: { grid: { color: gridColor }, ticks: { color: tickColor, stepSize: 1 }, beginAtZero: true },
                    },
                },
            }));
        }

        if (barRef.current) {
            chartsRef.current.push(new Chart(barRef.current, {
                type: "bar",
                data: {
                    labels: stats.scoreDistribution.map(s => `${s.range}%`),
                    datasets: [{
                        label: "Soumissions",
                        data: stats.scoreDistribution.map(s => s.count),
                        backgroundColor: ["var(--danger)", "var(--warning)", "var(--warning-t)", "var(--success)"],
                        borderRadius: 4,
                    }],
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    plugins: { legend: { display: false } },
                    scales: {
                        x: { grid: { color: gridColor }, ticks: { color: tickColor } },
                        y: { grid: { color: gridColor }, ticks: { color: tickColor, stepSize: 1 }, beginAtZero: true },
                    },
                },
            }));
        }

        if (donutRef.current && stats.parcoursStats.length > 0) {
            chartsRef.current.push(new Chart(donutRef.current, {
                type: "doughnut",
                data: {
                    labels: stats.parcoursStats.map(p => p.title),
                    datasets: [{
                        data: stats.parcoursStats.map(p => p.completionRate),
                        backgroundColor: ["var(--pr)", "var(--success)", "var(--warning)", "var(--danger)"],
                        borderColor: "var(--surface)",
                        borderWidth: 2,
                    }],
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    cutout: "65%",
                    plugins: {
                        legend: {
                            position: "bottom",
                            labels: { color: "var(--text-2)", font: { size: 11 } },
                        },
                    },
                },
            }));
        }

        return () => { chartsRef.current.forEach(c => c?.destroy?.()); chartsRef.current = []; };
    }, [chartReady, stats]);

    const medals = ["🥇", "🥈", "🥉"];

    return (
        <div style={{ marginTop: 32, display: "flex", flexDirection: "column", gap: 24 }}>
            {/* Blocs Vision UI — vraies données (activité de formation mensuelle + utilisateurs par statut) */}
            <div style={{ display: "grid", gridTemplateColumns: isMobile ? "1fr" : "2fr 1fr", gap: 24 }}>
                <PremiumChartCard title="Activité de formation" subtitle="6 derniers mois">
                    {activityQ.isLoading
                        ? <OverviewSkel h={260} />
                        : activityHasData
                            ? <AreaChartCard data={activityData} domainMax={activityMax} height={260} />
                            : <OverviewEmpty label="Pas encore de données d'activité sur la période." />}
                </PremiumChartCard>
                <PremiumChartCard title="Utilisateurs par statut" subtitle={statusQ.data ? `${statusQ.data.total} collaborateur${statusQ.data.total > 1 ? "s" : ""}` : undefined}>
                    {statusQ.isLoading
                        ? <OverviewSkel h={260} />
                        : (statusQ.data && statusQ.data.total > 0)
                            ? <DonutChart data={statusDonut} centerLabel="collaborateurs" height={260} />
                            : <OverviewEmpty label="Aucun utilisateur à afficher pour le moment." />}
                </PremiumChartCard>
            </div>

            <ChartCard title="Formations complétées — 7 derniers jours">
                <div style={{ height: 240 }}>
                    <canvas ref={lineRef} />
                </div>
            </ChartCard>

            <div style={{ display: "grid", gridTemplateColumns: isMobile ? "1fr" : "1fr 1fr", gap: 24 }}>
                <ChartCard title="Distribution des scores">
                    <div style={{ height: 240 }}>
                        <canvas ref={barRef} />
                    </div>
                </ChartCard>
                <ChartCard title="Avancement par parcours">
                    <div style={{ height: 240 }}>
                        <canvas ref={donutRef} />
                    </div>
                </ChartCard>
            </div>

            <div>
                <h3 style={{ fontSize: 15, fontWeight: 600, color: "var(--text)", marginBottom: 12 }}>Top 3 collaborateurs</h3>
                <div style={{ display: "grid", gridTemplateColumns: isMobile ? "1fr" : "repeat(3, 1fr)", gap: 16 }}>
                    {stats.topPerformers.map((p, i) => (
                        <div key={i} style={{
                            background: "var(--surface)",
                            border: "1px solid var(--border)",
                            borderRadius: 10,
                            padding: 18,
                            position: "relative",
                        }}>
                            <div style={{ position: "absolute", top: 12, right: 14, fontSize: 22 }}>{medals[i]}</div>
                            <div style={{ display: "flex", alignItems: "center", gap: 12 }}>
                                <div style={{
                                    width: 44, height: 44, borderRadius: "50%",
                                    background: "linear-gradient(135deg, var(--accent), var(--accent))",
                                    display: "flex", alignItems: "center", justifyContent: "center",
                                    fontSize: 14, fontWeight: 700, color: "var(--on-accent)",
                                }}>{p.initials}</div>
                                <div>
                                    <div style={{ fontSize: 14, fontWeight: 600, color: "var(--text)" }}>{p.name}</div>
                                    <div style={{ fontSize: 11, color: "var(--text-2)" }}>{p.completedChallenges} modules</div>
                                </div>
                            </div>
                            <div style={{ marginTop: 14, fontSize: 24, fontWeight: 700, fontFamily: "'JetBrains Mono', monospace", color: "var(--text)" }}>
                                {p.totalPoints}
                                <span style={{ fontSize: 11, color: "var(--text-2)", marginLeft: 6, fontWeight: 400 }}>pts</span>
                            </div>
                            <div style={{ fontSize: 12, fontWeight: 600, color: scoreColor(p.averageScore), fontFamily: "'JetBrains Mono', monospace" }}>
                                {p.averageScore}% moyenne
                            </div>
                        </div>
                    ))}
                </div>
            </div>

            <div>
                <h3 style={{ fontSize: 15, fontWeight: 600, color: "var(--text)", marginBottom: 12 }}>Modules avec les scores les plus bas</h3>
                <div style={{ display: "flex", flexDirection: "column", gap: 10 }}>
                    {stats.hardestChallenges.map((h, i) => (
                        <div key={i} style={{
                            background: "var(--surface)",
                            border: "1px solid var(--border)",
                            borderLeft: "3px solid var(--danger)",
                            borderRadius: 8,
                            padding: "14px 18px",
                            display: "flex",
                            alignItems: "center",
                            justifyContent: "space-between",
                            gap: 16,
                        }}>
                            <div>
                                <div style={{ fontSize: 14, fontWeight: 500, color: "var(--text)" }}>{h.title}</div>
                                <div style={{ fontSize: 11, color: "var(--text-2)", marginTop: 2 }}>{h.attempts} tentative{h.attempts > 1 ? "s" : ""}</div>
                            </div>
                            <div style={{ display: "flex", alignItems: "center", gap: 12 }}>
                                <div style={{ width: 100, height: 4, background: "var(--border)", borderRadius: 2, overflow: "hidden" }}>
                                    <div style={{ width: `${h.averageScore}%`, height: "100%", background: "var(--danger)" }} />
                                </div>
                                <span style={{ fontSize: 14, fontWeight: 700, color: "var(--danger)", fontFamily: "'JetBrains Mono', monospace", minWidth: 40, textAlign: "right" }}>
                                    {h.averageScore}%
                                </span>
                            </div>
                        </div>
                    ))}
                    {stats.hardestChallenges.length === 0 && (
                        <div style={{ padding: 16, color: "var(--text-2)", fontSize: 13, textAlign: "center" }}>Aucune donnée</div>
                    )}
                </div>
            </div>

            <div>
                <h3 style={{ fontSize: 15, fontWeight: 600, color: "var(--text)", marginBottom: 12 }}>Résumé par parcours</h3>
                <div style={{ display: "grid", gridTemplateColumns: isMobile ? "1fr" : "1fr 1fr", gap: 16 }}>
                    {stats.parcoursStats.map((p, i) => (
                        <div key={i} style={{
                            background: "var(--surface)",
                            border: "1px solid var(--border)",
                            borderRadius: 10,
                            padding: 20,
                        }}>
                            <div style={{ fontSize: 14, fontWeight: 600, color: "var(--text)", marginBottom: 12 }}>{p.title}</div>
                            <div style={{ display: "flex", alignItems: "baseline", gap: 8, marginBottom: 12 }}>
                                <span style={{ fontSize: 28, fontWeight: 700, color: "var(--pr)", fontFamily: "'JetBrains Mono', monospace" }}>
                                    {p.completionRate}%
                                </span>
                                <span style={{ fontSize: 11, color: "var(--text-2)" }}>de complétion</span>
                            </div>
                            <div style={{ height: 4, background: "var(--border)", borderRadius: 2, overflow: "hidden", marginBottom: 12 }}>
                                <div style={{ width: `${p.completionRate}%`, height: "100%", background: "linear-gradient(90deg, var(--accent), var(--accent))" }} />
                            </div>
                            <div style={{ display: "flex", justifyContent: "space-between", fontSize: 11, color: "var(--text-2)" }}>
                                <span>Score moy. : <span style={{ color: scoreColor(p.averageScore) }}>{p.averageScore}%</span></span>
                                <span>{p.totalCompletions} formations</span>
                            </div>
                        </div>
                    ))}
                </div>
            </div>
        </div>
    );
}

function ChartCard({ title, children }: { title: string; children: React.ReactNode }) {
    return (
        <div style={{
            background: "var(--surface)",
            border: "1px solid var(--border)",
            borderRadius: 10,
            padding: 20,
        }}>
            <h3 style={{ fontSize: 14, fontWeight: 600, color: "var(--text)", marginBottom: 16 }}>{title}</h3>
            {children}
        </div>
    );
}

// ── IMPORT/EXPORT BAR ───────────────────────────────────────────────────────
const API_BASE = process.env.NEXT_PUBLIC_API_BASE_URL ?? "";

function ImportExportBar() {
    const qc = useQueryClient();
    const [showImport, setShowImport] = useState(false);
    const [file, setFile] = useState<File | null>(null);
    const [updateExisting, setUpdateExisting] = useState(true);
    const [importing, setImporting] = useState(false);
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    const [result, setResult] = useState<any>(null);

    async function downloadFile(endpoint: string, filename: string) {
        const res = await fetch(`${API_BASE}${endpoint}`, { credentials: "include" });
        if (!res.ok) { alert("Erreur de téléchargement"); return; }
        const blob = await res.blob();
        const url = URL.createObjectURL(blob);
        const a = document.createElement("a");
        a.href = url;
        a.download = filename;
        a.click();
        URL.revokeObjectURL(url);
    }

    async function doImport() {
        if (!file) return;
        setImporting(true);
        setResult(null);
        try {
            const fd = new FormData();
            fd.append("file", file);
            const isExcel = file.name.endsWith(".xlsx") || file.name.endsWith(".xls");
            const endpoint = isExcel ? "/api/admin/users/import-excel" : "/api/admin/users/import-csv";
            const res = await fetch(`${API_BASE}${endpoint}?updateExisting=${updateExisting}`, {
                method: "POST",
                credentials: "include",
                headers: { "X-Requested-With": "XMLHttpRequest" },
                body: fd,
            });
            const data = await res.json();
            setResult(data);
            if (data.success || data.created > 0 || data.updated > 0) {
                qc.invalidateQueries({ queryKey: ["admin", "users"] });
                qc.invalidateQueries({ queryKey: ["admin", "stats"] });
            }
        } catch (e) {
            setResult({ error: (e as Error).message });
        } finally {
            setImporting(false);
        }
    }

    return (
        <div style={{ marginBottom: 16, display: "flex", gap: 10, flexWrap: "wrap" }}>
            <button onClick={() => setShowImport(true)} style={btnStyle("var(--pr)")}>📥 Importer CSV</button>
            <button onClick={() => downloadFile("/api/admin/users/export", `utilisateurs_${new Date().toISOString().slice(0, 10)}.csv`)} style={btnStyle("var(--surface-2)")}>📤 Exporter CSV</button>
            <button onClick={() => downloadFile("/api/admin/users/template", "template_import.csv")} style={btnStyle("var(--surface-2)")}>📋 Template</button>

            {showImport && (
                <div style={{
                    position: "fixed", inset: 0, background: "rgba(0,0,0,0.7)",
                    display: "flex", alignItems: "center", justifyContent: "center",
                    zIndex: 1000, padding: 16,
                }} onClick={() => { if (!importing) setShowImport(false); }}>
                    <div onClick={e => e.stopPropagation()} style={{
                        background: "var(--surface)", border: "1px solid rgba(255,255,255,0.1)",
                        borderRadius: 12, padding: 28, maxWidth: 520, width: "100%",
                    }}>
                        <h3 style={{ fontSize: 18, fontWeight: 700, color: "var(--text)", marginBottom: 16 }}>
                            Importer des utilisateurs
                        </h3>

                        <div style={{
                            border: "2px dashed rgba(255,255,255,0.15)",
                            borderRadius: 10,
                            padding: 24,
                            textAlign: "center",
                            marginBottom: 16,
                            cursor: "pointer",
                        }}
                            onDragOver={e => { e.preventDefault(); e.currentTarget.style.borderColor = "rgba(117,81,255,0.5)"; }}
                            onDragLeave={e => { e.currentTarget.style.borderColor = "rgba(255,255,255,0.15)"; }}
                            onDrop={e => {
                                e.preventDefault();
                                e.currentTarget.style.borderColor = "rgba(255,255,255,0.15)";
                                if (e.dataTransfer.files[0]) setFile(e.dataTransfer.files[0]);
                            }}
                        >
                            <div style={{ fontSize: 32, marginBottom: 8 }}>📥</div>
                            <div style={{ color: "var(--text-2)", fontSize: 13, marginBottom: 12 }}>
                                {file ? file.name : "Déposer votre fichier CSV ici"}
                            </div>
                            <label style={{
                                display: "inline-block",
                                padding: "6px 14px",
                                background: "var(--surface-2)",
                                color: "var(--text-2)",
                                border: "1px solid var(--border)",
                                borderRadius: 6,
                                fontSize: 12,
                                cursor: "pointer",
                            }}>
                                Parcourir
                                <input type="file" accept=".csv,.xlsx,.xls" style={{ display: "none" }}
                                    onChange={e => setFile(e.target.files?.[0] ?? null)} />
                            </label>
                        </div>

                        <label style={{ display: "flex", alignItems: "center", gap: 8, fontSize: 13, color: "var(--text-2)", marginBottom: 16 }}>
                            <input type="checkbox" checked={updateExisting} onChange={e => setUpdateExisting(e.target.checked)} />
                            Mettre à jour les utilisateurs existants
                        </label>

                        {result && !result.error && (
                            <div style={{
                                background: "var(--surface-2)",
                                border: "1px solid var(--border)",
                                borderRadius: 8,
                                padding: 16,
                                marginBottom: 16,
                                fontSize: 13,
                            }}>
                                <div style={{ color: "var(--success)", marginBottom: 4 }}>✓ {result.created} créés</div>
                                <div style={{ color: "var(--pr-l)", marginBottom: 4 }}>~ {result.updated} mis à jour</div>
                                <div style={{ color: "var(--text-2)", marginBottom: 4 }}>○ {result.skipped} ignorés</div>
                                {result.errors > 0 && <div style={{ color: "var(--danger)", marginBottom: 4 }}>✗ {result.errors} erreurs</div>}
                                {result.defaultPassword && (
                                    <div style={{
                                        background: "rgba(245,158,11,0.10)",
                                        border: "1px solid rgba(245,158,11,0.35)",
                                        borderRadius: 6,
                                        padding: 10,
                                        marginTop: 10,
                                        color: "var(--warning-t)",
                                        fontSize: 12,
                                    }}>
                                        Mot de passe par défaut : <strong>{result.defaultPassword}</strong>
                                        <br />Communiquez-le à vos nouveaux collaborateurs et demandez-leur de le modifier.
                                    </div>
                                )}
                                {result.errorMessages && result.errorMessages.length > 0 && (
                                    <details style={{ marginTop: 10 }}>
                                        <summary style={{ color: "var(--danger)", cursor: "pointer", fontSize: 12 }}>Voir les erreurs ({result.errorMessages.length})</summary>
                                        <ul style={{ marginTop: 8, paddingLeft: 16, color: "var(--danger)", fontSize: 11 }}>
                                            {result.errorMessages.map((m: string, i: number) => <li key={i}>{m}</li>)}
                                        </ul>
                                    </details>
                                )}
                            </div>
                        )}
                        {result?.error && (
                            <div style={{ color: "var(--danger)", fontSize: 12, marginBottom: 12 }}>{result.error}</div>
                        )}

                        <div style={{ display: "flex", justifyContent: "flex-end", gap: 8 }}>
                            <button onClick={() => { setShowImport(false); setFile(null); setResult(null); }} disabled={importing} style={btnStyle("var(--surface-2)")}>
                                Fermer
                            </button>
                            <button onClick={doImport} disabled={!file || importing} style={{
                                ...btnStyle("var(--pr)"),
                                opacity: !file || importing ? 0.5 : 1,
                                cursor: !file || importing ? "not-allowed" : "pointer",
                            }}>
                                {importing ? "Import en cours..." : "Lancer l'import"}
                            </button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
}

function btnStyle(bg: string): React.CSSProperties {
    // Boutons accent -> texte on-accent ; boutons neutres -> texte principal (contraste AA dans les 2 modes).
    const isAccent = bg === "var(--pr)";
    return {
        background: bg,
        color: isAccent ? "var(--on-accent)" : "var(--text)",
        border: "none",
        padding: "9px 16px",
        borderRadius: 7,
        fontSize: 13,
        fontWeight: 600,
        cursor: "pointer",
    };
}
