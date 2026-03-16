"use client";

import { useEffect, useMemo, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { PieChart, Pie, Tooltip, ResponsiveContainer, BarChart, Bar, XAxis, YAxis } from "recharts";
import { apiFetch } from "@/lib/api";
import type {
    AdminPathListItemDto,
    PagedResult,
    StatsOverviewDto,
    TrackingUserRowDto,
} from "@/lib/adminTypes";
import { statusBadgeClass, statusLabel } from "@/lib/statusUi";

function StatCard({ title, value }: { title: string; value: string | number }) {
    return (
        <div className="rounded-2xl border border-neutral-800 bg-neutral-950 p-4 shadow">
            <div className="text-sm text-neutral-400">{title}</div>
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

    // 1) Liste des formations (dropdown)
    const pathsQuery = useQuery({
        queryKey: ["admin-paths"],
        queryFn: () => apiFetch<AdminPathListItemDto[]>("/api/admin/paths"),
    });

    // Auto-select : 1er publié, sinon 1er
    useEffect(() => {
        if (!pathId && pathsQuery.data && pathsQuery.data.length > 0) {
            const published = pathsQuery.data.find((p) => p.publishedAt);
            setPathId((published ?? pathsQuery.data[0]).id);
        }
    }, [pathsQuery.data, pathId]);

    // 2) Overview stats
    const overviewQuery = useQuery({
        queryKey: ["admin-overview", pathId],
        enabled: !!pathId,
        queryFn: () => apiFetch<StatsOverviewDto>(`/api/admin/stats/overview?pathId=${pathId}`),
    });

    // 3) Table tracking paginée + search + status
    const usersQuery = useQuery({
        queryKey: ["admin-tracking-users", pathId, page, pageSize, search, status],
        enabled: !!pathId,
        queryFn: () => {
            const params = new URLSearchParams({
                pathId,
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
            { name: "Gris", value: o.grey },
            { name: "Jaune", value: o.yellow },
            { name: "Vert", value: o.green },
            { name: "Rouge", value: o.red },
        ];
    }, [overviewQuery.data]);

    const barData = useMemo(() => {
        const o = overviewQuery.data;
        if (!o) return [];
        return [
            { name: "Pas commencé", value: o.grey },
            { name: "En cours", value: o.yellow },
            { name: "Réussi", value: o.green },
            { name: "Échec", value: o.red },
        ];
    }, [overviewQuery.data]);

    const total = usersQuery.data?.total ?? 0;
    const totalPages = Math.max(1, Math.ceil(total / pageSize));

    const selectedPathTitle = useMemo(() => {
        const p = pathsQuery.data?.find((x) => x.id === pathId);
        return p?.title ?? "";
    }, [pathsQuery.data, pathId]);

    return (
        <div className="space-y-6">
            {/* Header */}
            <div className="rounded-2xl border border-neutral-800 bg-neutral-950 p-4">
                <div className="flex flex-col gap-3 md:flex-row md:items-end md:justify-between">
                    <div>
                        <div className="text-lg font-semibold">Dashboard Admin</div>
                        <div className="text-sm text-neutral-400">
                            Suivi employés : progression, score, statut.
                            {selectedPathTitle ? <span className="ml-2 text-neutral-300">• {selectedPathTitle}</span> : null}
                        </div>
                    </div>

                    <div className="flex flex-col gap-2 md:flex-row md:items-end">
                        {/* Dropdown formation */}
                        <div className="flex flex-col">
                            <label className="text-xs text-neutral-400">Formation</label>
                            <select
                                value={pathId}
                                onChange={(e) => {
                                    setPathId(e.target.value);
                                    setPage(1);
                                    setSearch("");
                                    setStatus("");
                                }}
                                className="w-[360px] rounded-xl border border-neutral-800 bg-neutral-950 px-3 py-2 text-sm"
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

            {/* Cards */}
            <div className="grid gap-4 md:grid-cols-4">
                <StatCard title="Employés (après filtres)" value={total} />
                <StatCard title="Progression moyenne" value={overviewQuery.data ? `${overviewQuery.data.avgProgress}%` : "-"} />
                <StatCard title="Score moyen" value={overviewQuery.data?.avgScore ?? "-"} />
                <StatCard title="Pages" value={`${page} / ${totalPages}`} />
            </div>

            {/* Graphs */}
            <div className="grid gap-4 md:grid-cols-2">
                <div className="rounded-2xl border border-neutral-800 bg-neutral-950 p-4">
                    <div className="text-sm text-neutral-300">Répartition des statuts</div>
                    <div className="mt-4 h-64">
                        <ResponsiveContainer width="100%" height="100%">
                            <PieChart>
                                <Pie data={pieData} dataKey="value" nameKey="name" outerRadius={90} />
                                <Tooltip />
                            </PieChart>
                        </ResponsiveContainer>
                    </div>
                </div>

                <div className="rounded-2xl border border-neutral-800 bg-neutral-950 p-4">
                    <div className="text-sm text-neutral-300">Statuts (bar)</div>
                    <div className="mt-4 h-64">
                        <ResponsiveContainer width="100%" height="100%">
                            <BarChart data={barData}>
                                <XAxis dataKey="name" tick={{ fill: "rgb(163 163 163)", fontSize: 12 }} />
                                <YAxis tick={{ fill: "rgb(163 163 163)", fontSize: 12 }} />
                                <Tooltip />
                                <Bar dataKey="value" />
                            </BarChart>
                        </ResponsiveContainer>
                    </div>
                </div>
            </div>

            {/* Table */}
            <div className="rounded-2xl border border-neutral-800 bg-neutral-950">
                <div className="flex flex-col gap-3 p-4 md:flex-row md:items-center md:justify-between">
                    <div>
                        <div className="text-sm font-semibold">Employés</div>
                        <div className="text-xs text-neutral-400">Recherche + filtre + pagination.</div>
                    </div>

                    <div className="flex flex-col gap-2 md:flex-row md:items-center">
                        <input
                            value={search}
                            onChange={(e) => {
                                setSearch(e.target.value);
                                setPage(1);
                            }}
                            placeholder="Rechercher (nom / email)"
                            className="w-64 rounded-xl border border-neutral-800 bg-neutral-950 px-3 py-2 text-sm outline-none focus:border-neutral-600"
                        />

                        <select
                            value={status}
                            onChange={(e) => {
                                setStatus(e.target.value);
                                setPage(1);
                            }}
                            className="rounded-xl border border-neutral-800 bg-neutral-950 px-3 py-2 text-sm"
                        >
                            <option value="">Tous</option>
                            <option value="grey">Gris (pas commencé)</option>
                            <option value="yellow">Jaune (en cours)</option>
                            <option value="green">Vert (réussi)</option>
                            <option value="red">Rouge (échec)</option>
                        </select>

                        <div className="flex items-center gap-2">
                            <label className="text-xs text-neutral-400">pageSize</label>
                            <select
                                value={pageSize}
                                onChange={(e) => {
                                    setPageSize(Number(e.target.value));
                                    setPage(1);
                                }}
                                className="rounded-xl border border-neutral-800 bg-neutral-950 px-2 py-2 text-sm"
                            >
                                {[10, 25, 50, 100, 200].map((n) => (
                                    <option key={n} value={n}>
                                        {n}
                                    </option>
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

                            <div className="text-sm text-neutral-300">
                                {page} / {totalPages}
                            </div>

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
                        <thead className="text-left text-neutral-400">
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
                                    <td className="px-4 py-6 text-neutral-400" colSpan={6}>
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
                                                    className="h-2 rounded-full bg-neutral-300"
                                                    style={{ width: `${Math.min(100, Math.max(0, u.progressPercent))}%` }}
                                                />
                                            </div>
                                            <div className="text-xs text-neutral-400">{u.progressPercent}%</div>
                                        </div>
                                    </td>

                                    <td className="px-4 py-3">{u.score}</td>

                                    <td className="px-4 py-3">
                                        <span
                                            className={`inline-flex items-center rounded-full border px-2 py-1 text-xs ${statusBadgeClass(
                                                u.statusColor
                                            )}`}
                                        >
                                            {statusLabel(u.statusColor)}
                                        </span>
                                    </td>

                                    <td className="px-4 py-3 text-neutral-400">
                                        {u.lastActivityAt ? new Date(u.lastActivityAt).toLocaleString() : "-"}
                                    </td>
                                </tr>
                            ))}

                            {usersQuery.data && usersQuery.data.items.length === 0 && !usersQuery.isLoading && (
                                <tr>
                                    <td className="px-4 py-6 text-neutral-400" colSpan={6}>
                                        Aucun résultat.
                                    </td>
                                </tr>
                            )}
                        </tbody>
                    </table>
                </div>
            </div>
        </div>
    );
}
