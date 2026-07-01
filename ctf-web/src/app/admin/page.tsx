"use client";
import { useMemo, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { PieChart, Pie, Tooltip, ResponsiveContainer, BarChart, Bar, XAxis, YAxis, Cell } from "recharts";

// Palette des charts admin — alignée sur le design system Sentys.
// Mapping intentionnel des 4 statuts user (grey/yellow/green/red) sur les tokens.
const chartColors = {
    grey: "#6B7280",      // muted / pas commencé
    yellow: "#F59E0B",    // warning / en cours
    green: "#10B981",     // success / réussi
    red: "#EF4444",       // danger / échec
    primary: "#3B82F6",   // bleu DS
} as const;
import { apiFetch } from "@/lib/api";
import { RequireAuth } from "@/components/RequireAuth";
import type {
    AdminPathListItemDto,
    PagedResult,
    StatsOverviewDto,
    TrackingUserRowDto,
} from "@/lib/adminTypes";
import { statusBadgeClass, statusLabel } from "@/lib/statusUi";

function StatCard({ title, value }: { title: string; value: string | number }) {
    return (
        <div className="rounded-2xl border border-neutral-800 bg-background-dark p-4 shadow">
            <div className="text-sm text-neutral-300">{title}</div>
            <div className="mt-2 text-2xl font-semibold">{value}</div>
        </div>
    );
}

export default function AdminDashboardPage() {
    const [pathId, setPathId] = useState<string>("");
    const [page, setPage] = useState(1);
    const [pageSize, setPageSize] = useState(50);
    const [search, setSearch] = useState("");
    const [status, setStatus] = useState<string>("");

    const pathsQuery = useQuery({
        queryKey: ["admin-paths"],
        queryFn: () => apiFetch<AdminPathListItemDto[]>("/api/admin/paths"),
    });

    const resolvedPathId = useMemo(() => {
        if (pathId) return pathId;
        if (!pathsQuery.data || pathsQuery.data.length === 0) return "";
        const published = pathsQuery.data.find((p) => p.publishedAt);
        return (published ?? pathsQuery.data[0]).id;
    }, [pathId, pathsQuery.data]);

    const overviewQuery = useQuery({
        queryKey: ["admin-overview", resolvedPathId],
        enabled: !!resolvedPathId,
        queryFn: () => apiFetch<StatsOverviewDto>(`/api/admin/stats/overview?pathId=${resolvedPathId}`),
    });

    const usersQuery = useQuery({
        queryKey: ["admin-tracking-users", resolvedPathId, page, pageSize, search, status],
        enabled: !!resolvedPathId,
        queryFn: () => {
            const params = new URLSearchParams({
                pathId: resolvedPathId,
                page: String(page),
                pageSize: String(pageSize),
            });
            if (search.trim()) params.set("search", search.trim());
            if (status) params.set("status", status);
            return apiFetch<PagedResult<TrackingUserRowDto>>(`/api/admin/tracking/users?${params.toString()}`);
        },
    });

    const pieData = useMemo(() => {
        const o = overviewQuery.data;
        if (!o) return [];
        return [
            { name: "Pas commencé", value: o.grey,   color: chartColors.grey },
            { name: "En cours",     value: o.yellow, color: chartColors.yellow },
            { name: "Réussi",       value: o.green,  color: chartColors.green },
            { name: "Échec",        value: o.red,    color: chartColors.red },
        ].filter(d => d.value > 0); // n'affiche que les segments non-vides → empty state plus tard si tout vide
    }, [overviewQuery.data]);

    const barData = useMemo(() => {
        const o = overviewQuery.data;
        if (!o) return [];
        return [
            { name: "Pas commencé", value: o.grey,   color: chartColors.grey },
            { name: "En cours",     value: o.yellow, color: chartColors.yellow },
            { name: "Réussi",       value: o.green,  color: chartColors.green },
            { name: "Échec",        value: o.red,    color: chartColors.red },
        ];
    }, [overviewQuery.data]);

    const hasChartData = useMemo(() => {
        const o = overviewQuery.data;
        return Boolean(o && (o.grey + o.yellow + o.green + o.red) > 0);
    }, [overviewQuery.data]);

    const total = usersQuery.data?.total ?? 0;
    const totalPages = Math.max(1, Math.ceil(total / pageSize));

    const selectedPathTitle = useMemo(() => {
        const p = pathsQuery.data?.find((x) => x.id === resolvedPathId);
        return p?.title ?? "";
    }, [pathsQuery.data, resolvedPathId]);

    return (
        <RequireAuth>
            <div className="space-y-6 p-4 sm:p-6">
                <div className="rounded-2xl border border-neutral-800 bg-background-dark p-4">
                    <div className="flex flex-col gap-3 md:flex-row md:items-end md:justify-between">
                        <div>
                            <div className="text-lg font-semibold">Dashboard Admin</div>
                            <div className="text-sm text-neutral-300">
                                Suivi employés : progression, score, statut.
                                {selectedPathTitle ? <span className="ml-2 text-neutral-300">— {selectedPathTitle}</span> : null}
                            </div>
                        </div>
                        <div className="flex flex-col gap-2 md:flex-row md:items-end">
                            <div className="flex flex-col">
                                <label className="text-xs text-neutral-300">Formation</label>
                                <select
                                    value={resolvedPathId}
                                    onChange={(e) => {
                                        setPathId(e.target.value);
                                        setPage(1);
                                        setSearch("");
                                        setStatus("");
                                    }}
                                    className="w-full rounded-xl border border-neutral-800 bg-background-dark px-3 py-2 text-sm md:w-[360px]"
                                >
                                    <option value="" disabled>
                                        {pathsQuery.isLoading ? "Chargement..." : "Choisir une formation"}
                                    </option>
                                    {pathsQuery.data?.map((p) => (
                                        <option key={p.id} value={p.id}>
                                            {p.title} {p.publishedAt ? "" : "(non publié)"}
                                        </option>
                                    ))}
                                </select>
                            </div>
                        </div>
                    </div>
                </div>

                <div className="grid gap-4 md:grid-cols-4">
                    <StatCard title="Employés (après filtres)" value={total} />
                    <StatCard title="Progression moyenne" value={overviewQuery.data ? `${overviewQuery.data.avgProgress}%` : "-"} />
                    <StatCard title="Score moyen" value={overviewQuery.data?.avgScore ?? "-"} />
                    <StatCard title="Pages" value={`${page} / ${totalPages}`} />
                </div>

                <div className="grid gap-4 md:grid-cols-2">
                    <div className="rounded-2xl border border-neutral-800 bg-background-dark p-4">
                        <div className="text-sm text-neutral-300">Répartition des statuts</div>
                        <div className="mt-4 h-64">
                            {hasChartData ? (
                                <ResponsiveContainer width="100%" height="100%">
                                    <PieChart>
                                        <Pie data={pieData} dataKey="value" nameKey="name" outerRadius={90} stroke="#0F172A" strokeWidth={2}>
                                            {pieData.map((d, i) => (
                                                <Cell key={i} fill={d.color} />
                                            ))}
                                        </Pie>
                                        <Tooltip
                                            contentStyle={{ background: "#0F172A", border: "1px solid #334155", borderRadius: 8, color: "#F1F5F9", fontSize: 13 }}
                                        />
                                    </PieChart>
                                </ResponsiveContainer>
                            ) : (
                                <ChartEmptyState />
                            )}
                        </div>
                    </div>
                    <div className="rounded-2xl border border-neutral-800 bg-background-dark p-4">
                        <div className="text-sm text-neutral-300">Statuts (bar)</div>
                        <div className="mt-4 h-64">
                            {hasChartData ? (
                                <ResponsiveContainer width="100%" height="100%">
                                    <BarChart data={barData}>
                                        <XAxis dataKey="name" tick={{ fill: "rgb(163 163 163)", fontSize: 12 }} />
                                        <YAxis tick={{ fill: "rgb(163 163 163)", fontSize: 12 }} allowDecimals={false} />
                                        <Tooltip
                                            cursor={{ fill: "rgba(59,130,246,0.08)" }}
                                            contentStyle={{ background: "#0F172A", border: "1px solid #334155", borderRadius: 8, color: "#F1F5F9", fontSize: 13 }}
                                        />
                                        <Bar dataKey="value">
                                            {barData.map((d, i) => (
                                                <Cell key={i} fill={d.color} />
                                            ))}
                                        </Bar>
                                    </BarChart>
                                </ResponsiveContainer>
                            ) : (
                                <ChartEmptyState />
                            )}
                        </div>
                    </div>
                </div>

                <div className="rounded-2xl border border-neutral-800 bg-background-dark">
                    <div className="flex flex-col gap-3 p-4 md:flex-row md:items-center md:justify-between">
                        <div>
                            <div className="text-sm font-semibold">Employés</div>
                            <div className="text-xs text-neutral-300">Recherche + filtre + pagination.</div>
                        </div>
                        <div className="flex flex-col gap-2 md:flex-row md:items-center">
                            <input
                                value={search}
                                onChange={(e) => { setSearch(e.target.value); setPage(1); }}
                                placeholder="Rechercher (nom / email)"
                                className="w-64 rounded-xl border border-neutral-800 bg-background-dark px-3 py-2 text-sm outline-none focus:border-neutral-600"
                            />
                            <select
                                value={status}
                                onChange={(e) => { setStatus(e.target.value); setPage(1); }}
                                className="rounded-xl border border-neutral-800 bg-background-dark px-3 py-2 text-sm"
                            >
                                <option value="">Tous</option>
                                <option value="grey">Gris (pas commencé)</option>
                                <option value="yellow">Jaune (en cours)</option>
                                <option value="green">Vert (réussi)</option>
                                <option value="red">Rouge (échec)</option>
                            </select>
                            <div className="flex items-center gap-2">
                                <label className="text-xs text-neutral-300">pageSize</label>
                                <select
                                    value={pageSize}
                                    onChange={(e) => { setPageSize(Number(e.target.value)); setPage(1); }}
                                    className="rounded-xl border border-neutral-800 bg-background-dark px-2 py-2 text-sm"
                                >
                                    {[10, 25, 50, 100, 200].map((n) => (
                                        <option key={n} value={n}>{n}</option>
                                    ))}
                                </select>
                            </div>
                            <div className="flex items-center gap-2">
                                <button
                                    onClick={() => setPage((p) => Math.max(1, p - 1))}
                                    className="rounded-xl border border-neutral-800 px-3 py-2 text-sm hover:bg-neutral-900"
                                >
                                    ←
                                </button>
                                <div className="text-sm text-neutral-300">{page} / {totalPages}</div>
                                <button
                                    onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                                    className="rounded-xl border border-neutral-800 px-3 py-2 text-sm hover:bg-neutral-900"
                                >
                                    →
                                </button>
                            </div>
                        </div>
                    </div>
                    <div className="overflow-x-auto border-t border-neutral-800">
                        <table className="min-w-full text-sm">
                            <thead className="text-left text-neutral-300">
                                <tr className="border-b border-neutral-800">
                                    <th className="px-4 py-3">Nom</th>
                                    <th className="px-4 py-3">Email</th>
                                    <th className="px-4 py-3">Progress</th>
                                    <th className="px-4 py-3">Score</th>
                                    <th className="px-4 py-3">Statut</th>
                                    <th className="px-4 py-3">Dernière activité</th>
                                </tr>
                            </thead>
                            <tbody>
                                {usersQuery.isLoading && (
                                    <tr>
                                        <td className="px-4 py-6 text-neutral-300" colSpan={6}>
                                            Chargement...
                                        </td>
                                    </tr>
                                )}
                                {usersQuery.data?.items?.map((u) => (
                                    <tr key={u.userId} className="border-b border-neutral-900">
                                        <td className="px-4 py-3">{u.displayName}</td>
                                        <td className="px-4 py-3 text-neutral-300">{u.email}</td>
                                        <td className="px-4 py-3">
                                            <div className="flex items-center gap-2">
                                                <div className="h-2 w-40 rounded-full bg-neutral-900">
                                                    <div
                                                        className="h-2 rounded-full bg-primary"
                                                        style={{ width: `${Math.min(100, Math.max(0, u.progressPercent))}%` }}
                                                    />
                                                </div>
                                                <div className="text-xs text-neutral-300">{u.progressPercent}%</div>
                                            </div>
                                        </td>
                                        <td className="px-4 py-3">{u.score}</td>
                                        <td className="px-4 py-3">
                                            <span className={`inline-flex items-center rounded-full border px-2 py-1 text-xs ${statusBadgeClass(u.statusColor)}`}>
                                                {statusLabel(u.statusColor)}
                                            </span>
                                        </td>
                                        <td className="px-4 py-3 text-neutral-300">
                                            {u.lastActivityAt ? new Date(u.lastActivityAt).toLocaleString() : "-"}
                                        </td>
                                    </tr>
                                ))}
                                {usersQuery.data && usersQuery.data.items.length === 0 && !usersQuery.isLoading && (
                                    <tr>
                                        <td className="px-4 py-6 text-neutral-300" colSpan={6}>
                                            Aucun résultat.
                                        </td>
                                    </tr>
                                )}
                            </tbody>
                        </table>
                    </div>
                </div>
            </div>
        </RequireAuth>
    );
}

function ChartEmptyState() {
    return (
        <div className="flex h-full flex-col items-center justify-center rounded-lg border-2 border-dashed border-neutral-700 text-neutral-300">
            <svg width="40" height="40" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" className="mb-2 opacity-40">
                <path d="M3 3v18h18" />
                <path d="M7 16V9" />
                <path d="M12 16v-5" />
                <path d="M17 16v-3" />
            </svg>
            <p className="text-sm font-medium">Pas encore de données</p>
            <p className="mt-1 text-xs opacity-80">Vos premiers utilisateurs alimenteront ce graphique</p>
        </div>
    );
}