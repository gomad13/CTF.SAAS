"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { Search, Download, UserPlus, Filter, MoreVertical, X } from "lucide-react";
import { apiFetch } from "@/lib/api";
import { useIsMobile } from "@/hooks/useMediaQuery";

type Row = {
    id: string;
    firstName: string;
    lastName: string;
    email: string;
    role: string;
    isActive: boolean;
    teamId: string | null;
    teamName: string | null;
    teamColor: string | null;
    createdAt: string;
    lastActivityAt: string | null;
    lastLoginAt: string | null;
    assignedPathsCount: number;
    completedPathsCount: number;
};
type Aggregations = { total: number; activeCount: number; suspendedCount: number; adminCount: number; byTeam: Record<string, number> };
type ListResp = { items: Row[]; page: number; pageSize: number; total: number; aggregations: Aggregations };

type Team = { id: string; name: string; color?: string };
type Me = { email: string; role: string };

const API_BASE = process.env.NEXT_PUBLIC_API_BASE_URL ?? "";

const card: React.CSSProperties = {
    background: "#fff",
    border: "1px solid #E2E8F0",
    borderRadius: 12,
    padding: 20,
    boxShadow: "0 1px 2px rgba(0,0,0,0.04)"
};

function initials(f: string, l: string) {
    return ((f?.[0] ?? "") + (l?.[0] ?? "")).toUpperCase() || "?";
}
function relTime(iso: string | null): string {
    if (!iso) return "Jamais";
    const diff = Date.now() - new Date(iso).getTime();
    const mn = Math.floor(diff / 60000);
    if (mn < 1) return "À l'instant";
    if (mn < 60) return `Il y a ${mn} min`;
    const h = Math.floor(mn / 60);
    if (h < 24) return `Il y a ${h}h`;
    const d = Math.floor(h / 24);
    if (d < 30) return `Il y a ${d}j`;
    return new Date(iso).toLocaleDateString("fr-FR");
}

export default function DirectoryPage() {
    const qc = useQueryClient();
    const isMobile = useIsMobile();
    const [search, setSearch] = useState("");
    const [searchDebounced, setSearchDebounced] = useState("");
    const [teamFilter, setTeamFilter] = useState<string>("");
    const [roleFilter, setRoleFilter] = useState<string>("");
    const [statusFilter, setStatusFilter] = useState<string>("");
    const [sortBy, setSortBy] = useState("lastName");
    const [sortOrder, setSortOrder] = useState<"asc" | "desc">("asc");
    const [page, setPage] = useState(1);
    const [pageSize, setPageSize] = useState(25);
    const [selectedIds, setSelectedIds] = useState<Set<string>>(new Set());
    const [detailOpen, setDetailOpen] = useState<string | null>(null);
    const [showInvite, setShowInvite] = useState(false);

    useEffect(() => {
        const t = setTimeout(() => setSearchDebounced(search), 300);
        return () => clearTimeout(t);
    }, [search]);

    useEffect(() => { setPage(1); }, [searchDebounced, teamFilter, roleFilter, statusFilter, sortBy, sortOrder, pageSize]);

    const listKey = ["directory", searchDebounced, teamFilter, roleFilter, statusFilter, sortBy, sortOrder, page, pageSize] as const;

    const { data, isLoading } = useQuery<ListResp>({
        queryKey: listKey,
        queryFn: () => {
            const p = new URLSearchParams();
            if (searchDebounced) p.set("search", searchDebounced);
            if (teamFilter) p.append("teamIds", teamFilter);
            if (roleFilter) p.append("roles", roleFilter);
            if (statusFilter) p.append("statuses", statusFilter);
            p.set("sortBy", sortBy);
            p.set("sortOrder", sortOrder);
            p.set("page", String(page));
            p.set("pageSize", String(pageSize));
            return apiFetch<ListResp>(`/api/admin/directory?${p.toString()}`);
        },
    });

    const teamsQ = useQuery<Team[]>({
        queryKey: ["admin-teams-light"],
        queryFn: () => apiFetch<Team[]>("/api/admin/teams"),
    });

    const patchM = useMutation({
        mutationFn: ({ id, body }: { id: string; body: Record<string, unknown> }) =>
            apiFetch(`/api/admin/directory/${id}`, { method: "PATCH", body: JSON.stringify(body) }),
        onSuccess: () => {
            qc.invalidateQueries({ queryKey: ["directory"] });
            qc.invalidateQueries({ queryKey: ["directory-detail"] });
        },
    });
    const bulkM = useMutation({
        mutationFn: (body: { userIds: string[]; action: string; params?: Record<string, unknown> }) =>
            apiFetch(`/api/admin/directory/bulk-action`, { method: "POST", body: JSON.stringify(body) }),
        onSuccess: () => {
            setSelectedIds(new Set());
            qc.invalidateQueries({ queryKey: ["directory"] });
        },
    });
    const inviteM = useMutation({
        mutationFn: (body: Record<string, unknown>) =>
            apiFetch(`/api/admin/directory/invite`, { method: "POST", body: JSON.stringify(body) }),
        onSuccess: () => { setShowInvite(false); qc.invalidateQueries({ queryKey: ["directory"] }); }
    });

    const onToggleRow = (id: string) => {
        setSelectedIds(prev => {
            const n = new Set(prev);
            if (n.has(id)) n.delete(id); else n.add(id);
            return n;
        });
    };
    const onSelectAllPage = () => {
        const allIds = (data?.items ?? []).map(r => r.id);
        const allSelected = allIds.every(id => selectedIds.has(id));
        setSelectedIds(prev => {
            const n = new Set(prev);
            if (allSelected) allIds.forEach(id => n.delete(id));
            else allIds.forEach(id => n.add(id));
            return n;
        });
    };

    const downloadCsv = async () => {
        const p = new URLSearchParams();
        if (searchDebounced) p.set("search", searchDebounced);
        if (teamFilter) p.append("teamIds", teamFilter);
        const res = await fetch(`${API_BASE}/api/admin/directory/export?${p.toString()}`, { credentials: "include", headers: { "X-Requested-With": "XMLHttpRequest" } });
        const blob = await res.blob();
        const url = URL.createObjectURL(blob);
        const a = document.createElement("a");
        a.href = url; a.download = `annuaire_${new Date().toISOString().slice(0, 10)}.csv`;
        a.click();
        URL.revokeObjectURL(url);
    };

    const totalPages = Math.max(1, Math.ceil((data?.total ?? 0) / pageSize));

    return (
        <div style={{ padding: isMobile ? "16px var(--page-x)" : 24, background: "#F8FAFC", minHeight: "100%" }}>
            <div style={{ display: "flex", justifyContent: "space-between", alignItems: "flex-end", marginBottom: 20, flexWrap: "wrap", gap: 12 }}>
                <div>
                    <h1 style={{ fontSize: 24, fontWeight: 700, color: "#1E293B", margin: 0 }}>Annuaire</h1>
                    <div style={{ fontSize: 13, color: "#64748B", marginTop: 4 }}>
                        {data?.aggregations.total ?? "…"} collaborateur{(data?.aggregations.total ?? 0) > 1 ? "s" : ""}
                        {data && ` · ${data.aggregations.activeCount} actif${data.aggregations.activeCount > 1 ? "s" : ""} · ${data.aggregations.adminCount} admin${data.aggregations.adminCount > 1 ? "s" : ""}`}
                    </div>
                </div>
                <div style={{ display: "flex", gap: 8, width: isMobile ? "100%" : "auto" }}>
                    <button onClick={() => setShowInvite(true)} style={{ ...btnPrimary, flex: isMobile ? 1 : undefined, justifyContent: "center" }}>
                        <UserPlus size={15} /> Inviter
                    </button>
                    <button onClick={downloadCsv} style={{ ...btnSecondary, flex: isMobile ? 1 : undefined, justifyContent: "center" }}>
                        <Download size={15} /> Export CSV
                    </button>
                </div>
            </div>

            {/* Filtres */}
            <div style={{ ...card, marginBottom: 16, padding: 16, display: "flex", gap: 10, flexWrap: "wrap", alignItems: "center" }}>
                <div style={{ position: "relative", flex: "1 1 280px", minWidth: 240 }}>
                    <Search size={14} style={{ position: "absolute", left: 10, top: 10, color: "#64748B" }} />
                    <input
                        placeholder="Rechercher nom, prénom, email"
                        value={search} onChange={e => setSearch(e.target.value)}
                        style={{ width: "100%", padding: "8px 12px 8px 32px", borderRadius: 8, border: "1px solid #E2E8F0", fontSize: 13 }}
                    />
                </div>
                <select value={teamFilter} onChange={e => setTeamFilter(e.target.value)} style={filterSelect}>
                    <option value="">Toutes équipes</option>
                    {(teamsQ.data ?? []).map(t => <option key={t.id} value={t.id}>{t.name}</option>)}
                </select>
                <select value={roleFilter} onChange={e => setRoleFilter(e.target.value)} style={filterSelect}>
                    <option value="">Tous rôles</option>
                    <option value="user">User</option>
                    <option value="admin">Admin</option>
                </select>
                <select value={statusFilter} onChange={e => setStatusFilter(e.target.value)} style={filterSelect}>
                    <option value="">Tous statuts</option>
                    <option value="active">Actif</option>
                    <option value="suspended">Suspendu</option>
                    <option value="never_logged">Jamais connecté</option>
                </select>
                <select value={`${sortBy}:${sortOrder}`} onChange={e => { const [b, o] = e.target.value.split(":"); setSortBy(b); setSortOrder(o as "asc"|"desc"); }} style={filterSelect}>
                    <option value="lastName:asc">Nom A-Z</option>
                    <option value="lastName:desc">Nom Z-A</option>
                    <option value="createdAt:desc">Date d'ajout (récent)</option>
                    <option value="createdAt:asc">Date d'ajout (ancien)</option>
                    <option value="lastActivity:desc">Activité récente</option>
                </select>
                {(search || teamFilter || roleFilter || statusFilter) && (
                    <button
                        onClick={() => { setSearch(""); setTeamFilter(""); setRoleFilter(""); setStatusFilter(""); }}
                        style={{ ...btnSecondary, padding: "6px 10px", fontSize: 12 }}
                    ><X size={12} /> Reset</button>
                )}
            </div>

            {/* Bulk actions */}
            {selectedIds.size > 0 && (
                <div style={{ ...card, marginBottom: 12, padding: 12, background: "#EFF6FF", border: "1px solid #BFDBFE", display: "flex", flexWrap: "wrap", alignItems: "center", gap: 12 }}>
                    <span style={{ fontSize: 13, color: "#1E40AF", fontWeight: 500 }}>
                        {selectedIds.size} sélectionné{selectedIds.size > 1 ? "s" : ""}
                    </span>
                    <div style={{ display: "flex", flexWrap: "wrap", gap: 8 }}>
                        <button onClick={() => {
                            const tId = prompt("Team ID (vide = aucune équipe)");
                            if (tId !== null) bulkM.mutate({ userIds: Array.from(selectedIds), action: "assign_team", params: { teamId: tId || null } });
                        }} style={btnBulk}>Assigner équipe</button>
                        <button onClick={() => bulkM.mutate({ userIds: Array.from(selectedIds), action: "suspend" })} style={btnBulkDanger}>Suspendre</button>
                        <button onClick={() => bulkM.mutate({ userIds: Array.from(selectedIds), action: "reactivate" })} style={btnBulk}>Réactiver</button>
                        <button onClick={() => setSelectedIds(new Set())} style={btnBulk}>✕ Désélectionner</button>
                    </div>
                </div>
            )}

            {/* Liste : cartes empilées sur mobile, tableau sur desktop */}
            <div style={{ ...card, padding: 0, overflow: "hidden" }}>
                {isMobile ? (
                    <div>
                        {isLoading && <div style={{ padding: 32, textAlign: "center", color: "#64748B" }}>Chargement…</div>}
                        {!isLoading && (data?.items?.length ?? 0) === 0 && (
                            <div style={{ padding: 32, textAlign: "center", color: "#64748B" }}>Aucun collaborateur trouvé — ajustez vos filtres</div>
                        )}
                        {(data?.items ?? []).map(r => (
                            <div key={r.id} style={{ borderTop: "1px solid #E2E8F0", padding: 14, background: selectedIds.has(r.id) ? "#EFF6FF" : "#fff", display: "flex", gap: 10 }}>
                                <input type="checkbox" checked={selectedIds.has(r.id)} onChange={() => onToggleRow(r.id)} style={{ marginTop: 6 }} />
                                <div style={{ flex: 1, minWidth: 0 }} onClick={() => setDetailOpen(r.id)}>
                                    <div style={{ display: "flex", alignItems: "center", gap: 10 }}>
                                        <div style={{
                                            width: 32, height: 32, borderRadius: "50%", flexShrink: 0,
                                            background: r.teamColor ?? "#64748B", color: "#fff",
                                            display: "flex", alignItems: "center", justifyContent: "center",
                                            fontSize: 12, fontWeight: 600,
                                        }}>{initials(r.firstName, r.lastName)}</div>
                                        <div style={{ minWidth: 0 }}>
                                            <div style={{ fontWeight: 500, color: "#1E293B" }}>{r.firstName} {r.lastName}</div>
                                            <div style={{ fontSize: 12, color: "#64748B", overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap" }}>{r.email}</div>
                                        </div>
                                    </div>
                                    <div style={{ display: "flex", flexWrap: "wrap", gap: 6, marginTop: 8 }}>
                                        <span style={{ padding: "2px 8px", borderRadius: 999, fontSize: 11, fontWeight: 500,
                                            background: r.role === "admin" ? "rgba(245,158,11,0.12)" : "rgba(59,130,246,0.12)",
                                            color: r.role === "admin" ? "#D97706" : "#1E40AF" }}>
                                            {r.role === "admin" ? "Admin" : "User"}
                                        </span>
                                        {r.teamName && (
                                            <span style={{ padding: "2px 8px", borderRadius: 999, background: (r.teamColor ?? "#64748B") + "22", color: r.teamColor ?? "#64748B", fontSize: 11, fontWeight: 500 }}>{r.teamName}</span>
                                        )}
                                        {!r.isActive && <span style={{ padding: "2px 8px", borderRadius: 999, fontSize: 11, color: "#EF4444", background: "rgba(239,68,68,0.10)" }}>Suspendu</span>}
                                    </div>
                                    <div style={{ fontSize: 12, color: "#64748B", marginTop: 6 }}>
                                        {relTime(r.lastActivityAt ?? r.lastLoginAt)} · {r.completedPathsCount}/{r.assignedPathsCount} parcours
                                    </div>
                                </div>
                                <div onClick={e => e.stopPropagation()} style={{ flexShrink: 0 }}>
                                    <RowMenu user={r} onPatch={(body) => patchM.mutate({ id: r.id, body })} />
                                </div>
                            </div>
                        ))}
                    </div>
                ) : (
                <table style={{ width: "100%", borderCollapse: "collapse", fontSize: 13 }}>
                    <thead>
                        <tr style={{ background: "#F1F5F9" }}>
                            <th style={{ padding: 10, width: 36, textAlign: "center" }}>
                                <input type="checkbox"
                                    checked={(data?.items?.length ?? 0) > 0 && (data?.items ?? []).every(r => selectedIds.has(r.id))}
                                    onChange={onSelectAllPage} />
                            </th>
                            <th style={thStyle}>Collaborateur</th>
                            <th style={thStyle}>Email</th>
                            <th style={thStyle}>Équipe</th>
                            <th style={thStyle}>Rôle</th>
                            <th style={thStyle}>Activité</th>
                            <th style={thStyle}>Progression</th>
                            <th style={{ ...thStyle, width: 40 }}></th>
                        </tr>
                    </thead>
                    <tbody>
                        {isLoading && (
                            <tr><td colSpan={8} style={{ padding: 40, textAlign: "center", color: "#64748B" }}>Chargement…</td></tr>
                        )}
                        {!isLoading && (data?.items?.length ?? 0) === 0 && (
                            <tr><td colSpan={8} style={{ padding: 40, textAlign: "center", color: "#64748B" }}>
                                Aucun collaborateur trouvé — ajustez vos filtres
                            </td></tr>
                        )}
                        {(data?.items ?? []).map(r => (
                            <tr key={r.id}
                                style={{ borderTop: "1px solid #E2E8F0", background: selectedIds.has(r.id) ? "#EFF6FF" : "#fff" }}>
                                <td style={{ padding: 10, textAlign: "center" }} onClick={e => e.stopPropagation()}>
                                    <input type="checkbox" checked={selectedIds.has(r.id)} onChange={() => onToggleRow(r.id)} />
                                </td>
                                <td style={tdStyle} onClick={() => setDetailOpen(r.id)}>
                                    <div style={{ display: "flex", alignItems: "center", gap: 10, cursor: "pointer" }}>
                                        <div style={{
                                            width: 32, height: 32, borderRadius: "50%",
                                            background: r.teamColor ?? "#64748B", color: "#fff",
                                            display: "flex", alignItems: "center", justifyContent: "center",
                                            fontSize: 12, fontWeight: 600
                                        }}>{initials(r.firstName, r.lastName)}</div>
                                        <div>
                                            <div style={{ fontWeight: 500, color: "#1E293B" }}>{r.firstName} {r.lastName}</div>
                                            {!r.isActive && <div style={{ fontSize: 10, color: "#EF4444" }}>Suspendu</div>}
                                        </div>
                                    </div>
                                </td>
                                <td style={tdStyle} onClick={() => setDetailOpen(r.id)}>
                                    <span style={{ color: "#64748B", cursor: "pointer" }}>{r.email}</span>
                                </td>
                                <td style={tdStyle}>
                                    {r.teamName ? (
                                        <span style={{ padding: "2px 8px", borderRadius: 999, background: (r.teamColor ?? "#64748B") + "22", color: r.teamColor ?? "#64748B", fontSize: 11, fontWeight: 500 }}>
                                            {r.teamName}
                                        </span>
                                    ) : <span style={{ color: "#64748B" }}>—</span>}
                                </td>
                                <td style={tdStyle}>
                                    <span style={{ padding: "2px 8px", borderRadius: 999, fontSize: 11, fontWeight: 500,
                                        background: r.role === "admin" ? "rgba(245,158,11,0.12)" : "rgba(59,130,246,0.12)",
                                        color: r.role === "admin" ? "#D97706" : "#1E40AF" }}>
                                        {r.role === "admin" ? "Admin" : "User"}
                                    </span>
                                </td>
                                <td style={{ ...tdStyle, color: "#64748B", fontSize: 12 }}>{relTime(r.lastActivityAt ?? r.lastLoginAt)}</td>
                                <td style={{ ...tdStyle, color: "#64748B", fontSize: 12 }}>
                                    {r.completedPathsCount}/{r.assignedPathsCount} parcours
                                </td>
                                <td style={tdStyle} onClick={e => e.stopPropagation()}>
                                    <RowMenu user={r} onPatch={(body) => patchM.mutate({ id: r.id, body })} />
                                </td>
                            </tr>
                        ))}
                    </tbody>
                </table>
                )}

                {/* Pagination */}
                <div style={{ padding: 12, borderTop: "1px solid #E2E8F0", display: "flex", justifyContent: "space-between", alignItems: "center", fontSize: 12, color: "#64748B", flexWrap: "wrap", gap: 8 }}>
                    <div>
                        <select value={pageSize} onChange={e => setPageSize(Number(e.target.value))} style={{ padding: "4px 8px", fontSize: 12, border: "1px solid #E2E8F0", borderRadius: 6 }}>
                            <option value={25}>25 / page</option>
                            <option value={50}>50 / page</option>
                            <option value={100}>100 / page</option>
                        </select>
                        <span style={{ marginLeft: 12 }}>
                            {((page - 1) * pageSize) + 1} — {Math.min(page * pageSize, data?.total ?? 0)} sur {data?.total ?? 0}
                        </span>
                    </div>
                    <div style={{ display: "flex", gap: 4 }}>
                        <button disabled={page === 1} onClick={() => setPage(p => p - 1)} style={btnPagination}>‹</button>
                        <span style={{ padding: "6px 12px" }}>Page {page} / {totalPages}</span>
                        <button disabled={page >= totalPages} onClick={() => setPage(p => p + 1)} style={btnPagination}>›</button>
                    </div>
                </div>
            </div>

            {detailOpen && <UserDetailPanel userId={detailOpen} onClose={() => setDetailOpen(null)} />}
            {showInvite && <InviteModal onClose={() => setShowInvite(false)} onSubmit={(body) => inviteM.mutate(body)} teams={teamsQ.data ?? []} isSubmitting={inviteM.isPending} />}
        </div>
    );
}

// ── Row menu ──
function RowMenu({ user, onPatch }: { user: Row; onPatch: (body: Record<string, unknown>) => void }) {
    const [open, setOpen] = useState(false);
    return (
        <div style={{ position: "relative" }}>
            <button onClick={() => setOpen(v => !v)} style={{ background: "transparent", border: "none", cursor: "pointer", color: "#64748B", padding: 6 }}>
                <MoreVertical size={16} />
            </button>
            {open && (
                <div onMouseLeave={() => setOpen(false)} style={{
                    position: "absolute", top: "100%", right: 0, minWidth: 180, zIndex: 10,
                    background: "#fff", border: "1px solid #E2E8F0", borderRadius: 8, boxShadow: "0 4px 12px rgba(0,0,0,0.08)",
                    padding: 4
                }}>
                    {user.role === "admin"
                        ? <button style={menuItem} onClick={() => { onPatch({ role: "user" }); setOpen(false); }}>Rétrograder User</button>
                        : <button style={menuItem} onClick={() => { onPatch({ role: "admin" }); setOpen(false); }}>Promouvoir Admin</button>}
                    {user.isActive
                        ? <button style={menuItem} onClick={() => { onPatch({ isActive: false }); setOpen(false); }}>Suspendre</button>
                        : <button style={menuItem} onClick={() => { onPatch({ isActive: true }); setOpen(false); }}>Réactiver</button>}
                    <div style={{ height: 1, background: "#E2E8F0", margin: "4px 0" }} />
                    <button style={{ ...menuItem, color: "#EF4444" }} onClick={() => {
                        if (confirm(`Supprimer définitivement ${user.firstName} ${user.lastName} ? Cette action est irréversible.`)) {
                            apiFetch(`/api/admin/directory/bulk-action`, { method: "POST", body: JSON.stringify({ userIds: [user.id], action: "delete" }) })
                                .then(() => window.location.reload());
                        }
                    }}>Supprimer</button>
                </div>
            )}
        </div>
    );
}

// ── Detail panel ──
type DetailResp = {
    id: string; firstName: string; lastName: string; email: string; role: string; isActive: boolean;
    teamName: string | null; teamColor: string | null;
    createdAt: string; updatedAt: string | null; lastActivityAt: string | null; lastLoginAt: string | null;
    parcours: Array<{ pathId: string; title: string; sector: string | null; level: string | null; status: string; percent: number; dueAt: string | null; isMandatory: boolean; source: string }>;
    auditLog: Array<{ id: string; action: string; details: string | null; actorEmail: string | null; createdAt: string }>;
};

function UserDetailPanel({ userId, onClose }: { userId: string; onClose: () => void }) {
    const { data } = useQuery<DetailResp>({
        queryKey: ["directory-detail", userId],
        queryFn: () => apiFetch<DetailResp>(`/api/admin/directory/${userId}`),
    });
    const [tab, setTab] = useState<"parcours" | "activite" | "admin">("parcours");

    return (
        <div onClick={onClose} style={{ position: "fixed", inset: 0, background: "rgba(15,23,42,0.5)", zIndex: 100, display: "flex", justifyContent: "flex-end" }}>
            <div onClick={e => e.stopPropagation()} style={{ width: 560, maxWidth: "100%", background: "#fff", height: "100%", overflowY: "auto", padding: 24 }}>
                {!data ? (
                    <div>Chargement…</div>
                ) : (
                    <>
                        <div style={{ display: "flex", justifyContent: "space-between", alignItems: "flex-start", marginBottom: 16 }}>
                            <div>
                                <div style={{ fontSize: 20, fontWeight: 700, color: "#1E293B" }}>{data.firstName} {data.lastName}</div>
                                <div style={{ fontSize: 13, color: "#64748B" }}>{data.email}</div>
                            </div>
                            <button onClick={onClose} style={{ background: "transparent", border: "none", cursor: "pointer" }}><X size={20} /></button>
                        </div>

                        <div style={{ display: "grid", gridTemplateColumns: "repeat(2, 1fr)", gap: 8, fontSize: 12, color: "#64748B", marginBottom: 16 }}>
                            <div><strong>Rôle :</strong> {data.role}</div>
                            <div><strong>Statut :</strong> {data.isActive ? "Actif" : "Suspendu"}</div>
                            <div><strong>Équipe :</strong> {data.teamName ?? "—"}</div>
                            <div><strong>Dernière activité :</strong> {relTime(data.lastActivityAt ?? data.lastLoginAt)}</div>
                            <div><strong>Inscrit le :</strong> {new Date(data.createdAt).toLocaleDateString("fr-FR")}</div>
                            <div><strong>Maj :</strong> {data.updatedAt ? new Date(data.updatedAt).toLocaleDateString("fr-FR") : "—"}</div>
                        </div>

                        <div style={{ display: "flex", gap: 4, borderBottom: "1px solid #E2E8F0", marginBottom: 12 }}>
                            {(["parcours", "activite", "admin"] as const).map(t => (
                                <button key={t} onClick={() => setTab(t)} style={{
                                    padding: "8px 14px", background: "transparent", border: "none",
                                    borderBottom: tab === t ? "2px solid #3B82F6" : "2px solid transparent",
                                    color: tab === t ? "#3B82F6" : "#64748B",
                                    fontSize: 13, fontWeight: 500, cursor: "pointer", textTransform: "capitalize"
                                }}>{t === "parcours" ? "Parcours" : t === "activite" ? "Activité" : "Administration"}</button>
                            ))}
                        </div>

                        {tab === "parcours" && (
                            <div>
                                {data.parcours.length === 0 ? (
                                    <div style={{ color: "#64748B", fontSize: 13 }}>Aucun parcours assigné.</div>
                                ) : data.parcours.map(p => (
                                    <div key={p.pathId} style={{ padding: 12, border: "1px solid #E2E8F0", borderRadius: 8, marginBottom: 8 }}>
                                        <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
                                            <div style={{ fontWeight: 500, color: "#1E293B", fontSize: 13 }}>{p.title}</div>
                                            <span style={{ fontSize: 10, padding: "2px 8px", borderRadius: 999, background: "#F1F5F9", color: "#64748B" }}>{p.source}</span>
                                        </div>
                                        <div style={{ fontSize: 11, color: "#64748B", marginTop: 4, display: "flex", gap: 10 }}>
                                            <span>{p.level}</span><span>{p.sector}</span>
                                            {p.isMandatory && p.dueAt && <span style={{ color: "#F59E0B" }}>⏰ {new Date(p.dueAt).toLocaleDateString("fr-FR")}</span>}
                                        </div>
                                        <div style={{ marginTop: 8, height: 4, background: "#E2E8F0", borderRadius: 999 }}>
                                            <div style={{ width: `${p.percent}%`, height: "100%", background: p.status === "completed" ? "#10B981" : "#3B82F6", borderRadius: 999 }} />
                                        </div>
                                        <div style={{ fontSize: 10, color: "#64748B", marginTop: 4 }}>{p.status} — {p.percent}%</div>
                                    </div>
                                ))}
                            </div>
                        )}

                        {tab === "activite" && (
                            <div style={{ fontSize: 12, color: "#64748B" }}>
                                Timeline d'activité (à enrichir avec Analytics). Dernière connexion : {relTime(data.lastLoginAt)}
                            </div>
                        )}

                        {tab === "admin" && (
                            <div>
                                <div style={{ fontWeight: 500, color: "#1E293B", fontSize: 13, marginBottom: 8 }}>Journal d'audit</div>
                                {data.auditLog.length === 0 ? <div style={{ color: "#64748B", fontSize: 12 }}>Aucune action.</div> : (
                                    <table style={{ width: "100%", fontSize: 11 }}>
                                        <tbody>
                                            {data.auditLog.map(a => (
                                                <tr key={a.id} style={{ borderBottom: "1px solid #F1F5F9" }}>
                                                    <td style={{ padding: 6, color: "#64748B", whiteSpace: "nowrap" }}>{new Date(a.createdAt).toLocaleDateString("fr-FR")}</td>
                                                    <td style={{ padding: 6, fontWeight: 500 }}>{a.action}</td>
                                                    <td style={{ padding: 6, color: "#64748B" }}>{a.details ?? ""}</td>
                                                    <td style={{ padding: 6, color: "#64748B" }}>{a.actorEmail ?? "—"}</td>
                                                </tr>
                                            ))}
                                        </tbody>
                                    </table>
                                )}
                            </div>
                        )}
                    </>
                )}
            </div>
        </div>
    );
}

// ── Invite modal ──
function InviteModal({ onClose, onSubmit, teams, isSubmitting }: { onClose: () => void; onSubmit: (body: Record<string, unknown>) => void; teams: Team[]; isSubmitting: boolean }) {
    const isMobile = useIsMobile();
    const [email, setEmail] = useState(""); const [firstName, setFirstName] = useState(""); const [lastName, setLastName] = useState("");
    const [role, setRole] = useState("user"); const [teamId, setTeamId] = useState("");
    const twoCol: React.CSSProperties = { display: "grid", gridTemplateColumns: isMobile ? "1fr" : "1fr 1fr", gap: 10 };
    return (
        <div className="modal-overlay" onClick={onClose} role="dialog" aria-modal="true">
            <div className="modal-box" onClick={e => e.stopPropagation()} style={{ maxWidth: 480 }}>
                <h2 style={{ fontSize: 18, fontWeight: 600, margin: "0 0 16px", color: "#1E293B" }}>Inviter un collaborateur</h2>
                <form onSubmit={e => { e.preventDefault(); onSubmit({ email, firstName, lastName, role, teamId: teamId || null }); }}>
                    <div style={{ ...twoCol, marginBottom: 10 }}>
                        <input required placeholder="Prénom" value={firstName} onChange={e => setFirstName(e.target.value)} style={{ ...inputStyle, width: "100%" }} />
                        <input required placeholder="Nom" value={lastName} onChange={e => setLastName(e.target.value)} style={{ ...inputStyle, width: "100%" }} />
                    </div>
                    <input required type="email" placeholder="email@exemple.com" value={email} onChange={e => setEmail(e.target.value)} style={{ ...inputStyle, width: "100%", marginBottom: 10 }} />
                    <div style={{ ...twoCol, marginBottom: 16 }}>
                        <select value={role} onChange={e => setRole(e.target.value)} style={{ ...inputStyle, width: "100%" }}>
                            <option value="user">User</option>
                            <option value="admin">Admin</option>
                        </select>
                        <select value={teamId} onChange={e => setTeamId(e.target.value)} style={{ ...inputStyle, width: "100%" }}>
                            <option value="">Aucune équipe</option>
                            {teams.map(t => <option key={t.id} value={t.id}>{t.name}</option>)}
                        </select>
                    </div>
                    <div style={{ fontSize: 11, color: "#64748B", marginBottom: 16 }}>
                        Mot de passe temporaire par défaut : <strong>Employe@2026</strong>
                    </div>
                    <div style={{ display: "flex", gap: 8, justifyContent: "flex-end", flexDirection: isMobile ? "column-reverse" : "row" }}>
                        <button type="button" onClick={onClose} style={{ ...btnSecondary, justifyContent: "center", width: isMobile ? "100%" : undefined }}>Annuler</button>
                        <button type="submit" disabled={isSubmitting} style={{ ...btnPrimary, justifyContent: "center", width: isMobile ? "100%" : undefined }}>{isSubmitting ? "..." : "Inviter"}</button>
                    </div>
                </form>
            </div>
        </div>
    );
}

// ── Styles ──
const btnPrimary: React.CSSProperties = { padding: "8px 14px", background: "#3B82F6", color: "#fff", border: "none", borderRadius: 8, fontSize: 13, fontWeight: 500, cursor: "pointer", display: "inline-flex", alignItems: "center", gap: 6, transition: "background 200ms" };
const btnSecondary: React.CSSProperties = { padding: "8px 14px", background: "#fff", color: "#334155", border: "1px solid #E2E8F0", borderRadius: 8, fontSize: 13, cursor: "pointer", display: "inline-flex", alignItems: "center", gap: 6 };
const btnBulk: React.CSSProperties = { padding: "6px 12px", background: "#fff", color: "#1E40AF", border: "1px solid #BFDBFE", borderRadius: 6, fontSize: 12, cursor: "pointer" };
const btnBulkDanger: React.CSSProperties = { padding: "6px 12px", background: "#fff", color: "#DC2626", border: "1px solid #FECACA", borderRadius: 6, fontSize: 12, cursor: "pointer" };
const btnPagination: React.CSSProperties = { padding: "6px 10px", background: "transparent", border: "1px solid #E2E8F0", borderRadius: 6, cursor: "pointer" };
const filterSelect: React.CSSProperties = { padding: "8px 12px", borderRadius: 8, border: "1px solid #E2E8F0", fontSize: 13 };
const inputStyle: React.CSSProperties = { padding: "8px 12px", borderRadius: 8, border: "1px solid #E2E8F0", fontSize: 13 };
const thStyle: React.CSSProperties = { padding: "10px 12px", textAlign: "left", fontSize: 11, fontWeight: 600, color: "#64748B", textTransform: "uppercase", letterSpacing: "0.05em" };
const tdStyle: React.CSSProperties = { padding: 10 };
const menuItem: React.CSSProperties = { display: "block", width: "100%", textAlign: "left", padding: "8px 12px", background: "transparent", border: "none", fontSize: 12, color: "#334155", cursor: "pointer", borderRadius: 4 };
