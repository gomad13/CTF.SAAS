"use client";

import { useState, useMemo } from "react";
import Link from "next/link";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Plus, Trash2, Users, Search, Pencil, UserPlus } from "lucide-react";
import { apiFetch } from "@/lib/api";
import { useIsMobile } from "@/hooks/useMediaQuery";
import { renderTeamIcon, TEAM_ICON_NAMES } from "@/components/teams/teamIcons";
import TeamEditModal from "@/components/teams/TeamEditModal";
import Reveal from "@/components/Reveal";
import { Stagger, StaggerItem } from "@/components/Stagger";

type Team = {
    id: string;
    name: string;
    description: string | null;
    color: string | null;
    icon: string | null;
    managerId: string | null;
    memberCount: number;
    parcoursCount: number;
    compliancePercent: number;
    createdAt: string;
    maxMembers: number | null;
    isOpen: boolean;
};

type UnassignedUser = {
    userId: string;
    email: string;
    firstName: string;
    lastName: string;
    isActive: boolean;
    createdAt: string;
};

/** « x / max membres » ou « x membres » si pas de limite. */
function capacityLabel(t: Team) {
    if (t.maxMembers != null) return `${t.memberCount} / ${t.maxMembers} membres`;
    return `${t.memberCount} membre${t.memberCount > 1 ? "s" : ""}`;
}

function complianceColor(pct: number) {
    if (pct >= 80) return "var(--success)";
    if (pct >= 50) return "var(--warning)";
    return "var(--danger)";
}

export default function TeamsPage() {
    const qc = useQueryClient();
    const isMobile = useIsMobile();
    const statusQ = useQuery<{ isEnabled: boolean }>({
        queryKey: ["teams", "status"],
        queryFn: () => apiFetch("/api/teams/status"),
    });
    const listQ = useQuery<Team[]>({
        queryKey: ["teams", "list"],
        queryFn: () => apiFetch<Team[]>("/api/admin/teams"),
        enabled: statusQ.data?.isEnabled === true,
    });

    const [showCreate, setShowCreate] = useState(false);
    const [search, setSearch] = useState("");
    const [name, setName] = useState("");
    const [description, setDescription] = useState("");
    const [color, setColor] = useState("#7551FF");
    const [icon, setIcon] = useState("Users");
    const [maxMembers, setMaxMembers] = useState("");
    const [formError, setFormError] = useState<string | null>(null);
    const [editTeam, setEditTeam] = useState<Team | null>(null);

    const createM = useMutation({
        mutationFn: () => apiFetch<Team>("/api/admin/teams", {
            method: "POST",
            body: JSON.stringify({
                name, description, color, icon,
                maxMembers: maxMembers.trim() === "" ? null : Number(maxMembers),
            }),
        }),
        onSuccess: () => {
            qc.invalidateQueries({ queryKey: ["teams", "list"] });
            setShowCreate(false); setName(""); setDescription(""); setColor("#7551FF"); setIcon("Users"); setMaxMembers("");
            setFormError(null);
        },
        onError: (e: Error) => setFormError(e.message || "Erreur"),
    });

    const deleteM = useMutation({
        mutationFn: (id: string) => apiFetch(`/api/admin/teams/${id}`, { method: "DELETE" }),
        onSuccess: () => qc.invalidateQueries({ queryKey: ["teams", "list"] }),
    });

    const filtered = useMemo(() => {
        const q = search.trim().toLowerCase();
        if (!q) return listQ.data ?? [];
        return (listQ.data ?? []).filter(t =>
            t.name.toLowerCase().includes(q) || (t.description ?? "").toLowerCase().includes(q));
    }, [listQ.data, search]);

    if (!statusQ.data?.isEnabled) {
        return (
            <div className="mx-auto max-w-3xl px-6 py-12 text-center">
                <div className="mx-auto flex h-14 w-14 items-center justify-center rounded-full bg-surface/10 text-fg-muted"><Users size={22} /></div>
                <h1 className="mt-4 text-xl font-bold text-fg-heading">Mode Équipes désactivé</h1>
                <p className="mt-2 text-sm text-fg-muted">Activez-le depuis Administration → Paramètres.</p>
            </div>
        );
    }

    return (
        <div className="mx-auto flex max-w-6xl flex-col gap-6 px-4 py-6 sm:px-6 sm:py-8">
            <Reveal className="flex flex-wrap items-end justify-between gap-4">
                <div>
                    <h1 className="text-2xl font-bold text-fg-heading">Gestion des équipes</h1>
                    <p className="mt-1 text-sm text-fg-muted">
                        Segmentez vos collaborateurs par département et assignez-leur des parcours spécifiques.
                    </p>
                </div>
                <button
                    type="button"
                    onClick={() => setShowCreate(v => !v)}
                    className="inline-flex w-full items-center justify-center gap-2 rounded-lg bg-primary px-4 py-2 text-sm font-medium text-[var(--on-accent)] transition-colors duration-200 hover:bg-primary-hover sm:w-auto"
                >
                    <Plus size={14} />
                    Nouvelle équipe
                </button>
            </Reveal>

            {showCreate && (
                <section className="rounded-xl border border-border bg-surface p-6 shadow-sm">
                    <h2 className="text-sm font-semibold uppercase tracking-wider text-fg-body">Créer une équipe</h2>
                    <div className="mt-4 grid grid-cols-1 gap-3 md:grid-cols-[1fr_1fr]">
                        <input className="rounded-lg border border-border bg-surface px-3 py-2 text-sm text-fg-heading"
                            placeholder="Nom (ex. « Comptabilité »)" value={name} onChange={e => setName(e.target.value)} />
                        <input className="rounded-lg border border-border bg-surface px-3 py-2 text-sm text-fg-heading"
                            placeholder="Description (optionnelle)" value={description} onChange={e => setDescription(e.target.value)} />
                    </div>
                    <div className="mt-3 flex flex-wrap items-center gap-4">
                        <label className="flex items-center gap-2 text-sm text-fg-body">
                            Couleur
                            <input type="color" className="h-[34px] w-[48px] rounded border border-border" value={color} onChange={e => setColor(e.target.value)} />
                        </label>
                        <label className="flex items-center gap-2 text-sm text-fg-body">
                            Nombre max
                            <input type="number" min={1} value={maxMembers} onChange={e => setMaxMembers(e.target.value)}
                                placeholder="∞"
                                className="h-[34px] w-[80px] rounded border border-border px-2 text-sm text-fg-heading" />
                        </label>
                        <div className="flex flex-wrap items-center gap-1">
                            <span className="mr-2 text-sm text-fg-body">Icône</span>
                            {TEAM_ICON_NAMES.map(k => (
                                <button key={k} type="button" onClick={() => setIcon(k)}
                                    className={`flex h-8 w-8 items-center justify-center rounded-md border transition-colors duration-200 ${
                                        icon === k ? "border-primary bg-primary text-[var(--on-accent)]" : "border-border bg-surface text-fg-body hover:border-border"
                                    }`}
                                    title={k}>{renderTeamIcon(k, 16)}</button>
                            ))}
                        </div>
                    </div>
                    {formError && <div className="mt-3 text-xs text-danger">{formError}</div>}
                    <div className="mt-4 flex gap-2">
                        <button type="button" disabled={!name.trim() || createM.isPending}
                            onClick={() => createM.mutate()}
                            className="rounded-lg bg-primary px-4 py-2 text-sm font-medium text-[var(--on-accent)] transition-colors duration-200 hover:bg-primary-hover disabled:opacity-50">
                            Créer
                        </button>
                        <button type="button" onClick={() => { setShowCreate(false); setFormError(null); }}
                            className="rounded-lg border border-border bg-surface px-4 py-2 text-sm font-medium text-fg-body transition-colors duration-200 hover:bg-surface-2">
                            Annuler
                        </button>
                    </div>
                </section>
            )}

            <div className="relative">
                <Search size={14} className="absolute left-3 top-1/2 -translate-y-1/2 text-fg-muted" />
                <input className="w-full rounded-lg border border-border bg-surface py-2 pl-9 pr-3 text-sm text-fg-heading"
                    placeholder="Rechercher une équipe…" value={search} onChange={e => setSearch(e.target.value)} />
            </div>

            <Reveal>
            <section className="overflow-hidden rounded-xl border border-border bg-surface shadow-sm">
                {isMobile ? (
                    <Stagger className="divide-y divide-border" gap={0.04}>
                        {filtered.map(t => (
                            <StaggerItem key={t.id} className="flex items-start gap-3 p-4">
                                <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg text-[var(--on-accent)]"
                                    style={{ background: t.color ?? "var(--text-2)" }}>
                                    {renderTeamIcon(t.icon, 16)}
                                </span>
                                <div className="min-w-0 flex-1">
                                    <div className="font-medium text-fg-heading">{t.name}</div>
                                    {t.description && <div className="truncate text-xs text-fg-body">{t.description}</div>}
                                    <div className="mt-1 flex flex-wrap gap-x-4 gap-y-1 text-xs text-fg-muted">
                                        <span>{capacityLabel(t)}</span>
                                        <span>{t.parcoursCount} parcours</span>
                                        <span className="font-semibold" style={{ color: complianceColor(t.compliancePercent) }}>{t.compliancePercent}% compliance</span>
                                    </div>
                                    <div className="mt-2 flex items-center gap-2">
                                        <Link href={`/admin/teams/${t.id}`}
                                            className="rounded-lg border border-border bg-surface px-3 py-1.5 text-xs font-medium text-fg-body transition-colors duration-200 hover:bg-surface-2">
                                            Voir
                                        </Link>
                                        <button type="button" onClick={() => setEditTeam(t)}
                                            className="rounded-lg border border-border bg-surface p-1.5 text-fg-body transition-colors duration-200 hover:border-primary hover:text-primary"
                                            title="Modifier (nom, capacité, couleur, icône)">
                                            <Pencil size={14} />
                                        </button>
                                        <button type="button"
                                            onClick={() => { if (confirm(`Supprimer « ${t.name} » ? Les membres seront détachés (pas supprimés).`)) deleteM.mutate(t.id); }}
                                            className="rounded-lg border border-danger/40 bg-surface p-1.5 text-danger transition-colors duration-200 hover:bg-danger/10"
                                            title="Supprimer">
                                            <Trash2 size={14} />
                                        </button>
                                    </div>
                                </div>
                            </StaggerItem>
                        ))}
                        {filtered.length === 0 && (
                            <div className="px-4 py-12 text-center text-sm text-fg-muted">
                                {search ? "Aucune équipe ne correspond." : "Aucune équipe pour l'instant. Cliquez sur « Nouvelle équipe »."}
                            </div>
                        )}
                    </Stagger>
                ) : (
                <table className="w-full text-sm">
                    <thead className="bg-table-head text-xs uppercase tracking-wider text-fg-body">
                        <tr>
                            <th className="px-6 py-3 text-left font-semibold">Équipe</th>
                            <th className="px-6 py-3 text-left font-semibold">Membres</th>
                            <th className="px-6 py-3 text-left font-semibold">Parcours assignés</th>
                            <th className="px-6 py-3 text-left font-semibold">Compliance</th>
                            <th className="px-6 py-3 text-right font-semibold">Actions</th>
                        </tr>
                    </thead>
                    <tbody className="divide-y divide-border">
                        {filtered.map(t => (
                            <tr key={t.id} className="transition-colors hover:bg-surface-2">
                                <td className="px-6 py-4">
                                    <div className="flex items-center gap-3">
                                        <span className="flex h-9 w-9 items-center justify-center rounded-lg text-[var(--on-accent)]"
                                            style={{ background: t.color ?? "var(--text-2)" }}>
                                            {renderTeamIcon(t.icon, 16)}
                                        </span>
                                        <div>
                                            <div className="font-medium text-fg-heading">{t.name}</div>
                                            {t.description && <div className="text-xs text-fg-body">{t.description}</div>}
                                        </div>
                                    </div>
                                </td>
                                <td className="px-6 py-4 text-fg-body">
                                    {t.maxMembers != null ? `${t.memberCount} / ${t.maxMembers}` : t.memberCount}
                                </td>
                                <td className="px-6 py-4 text-fg-body">{t.parcoursCount}</td>
                                <td className="px-6 py-4">
                                    <span className="font-semibold" style={{ color: complianceColor(t.compliancePercent) }}>
                                        {t.compliancePercent}%
                                    </span>
                                </td>
                                <td className="px-6 py-4 text-right">
                                    <div className="inline-flex items-center gap-2">
                                        <Link href={`/admin/teams/${t.id}`}
                                            className="rounded-lg border border-border bg-surface px-3 py-1.5 text-xs font-medium text-fg-body transition-colors duration-200 hover:bg-surface-2">
                                            Voir
                                        </Link>
                                        <button type="button" onClick={() => setEditTeam(t)}
                                            className="rounded-lg border border-border bg-surface p-1.5 text-fg-body transition-colors duration-200 hover:border-primary hover:text-primary"
                                            title="Modifier (nom, capacité, couleur, icône)">
                                            <Pencil size={14} />
                                        </button>
                                        <button type="button"
                                            onClick={() => { if (confirm(`Supprimer « ${t.name} » ? Les membres seront détachés (pas supprimés).`)) deleteM.mutate(t.id); }}
                                            className="rounded-lg border border-danger/40 bg-surface p-1.5 text-danger transition-colors duration-200 hover:bg-danger/10"
                                            title="Supprimer">
                                            <Trash2 size={14} />
                                        </button>
                                    </div>
                                </td>
                            </tr>
                        ))}
                        {filtered.length === 0 && (
                            <tr>
                                <td colSpan={5} className="px-6 py-12 text-center text-sm text-fg-muted">
                                    {search ? "Aucune équipe ne correspond." : "Aucune équipe pour l'instant. Cliquez sur « Nouvelle équipe »."}
                                </td>
                            </tr>
                        )}
                    </tbody>
                </table>
                )}
            </section>
            </Reveal>

            <UnassignedMembersSection teams={listQ.data ?? []} />

            {editTeam && (
                <TeamEditModal
                    team={editTeam}
                    open={!!editTeam}
                    onClose={() => setEditTeam(null)}
                    onSaved={() => {
                        setEditTeam(null);
                        qc.invalidateQueries({ queryKey: ["teams", "list"] });
                        qc.invalidateQueries({ queryKey: ["admin-teams"] });
                    }}
                />
            )}
        </div>
    );
}

// ── M4 : Membres sans équipe (affectation à l'arrivée) ──
function UnassignedMembersSection({ teams }: { teams: Team[] }) {
    const qc = useQueryClient();
    const unassignedQ = useQuery<UnassignedUser[]>({
        queryKey: ["teams", "unassigned"],
        queryFn: () => apiFetch<UnassignedUser[]>("/api/admin/teams/unassigned"),
    });
    const [error, setError] = useState<string | null>(null);

    const assignM = useMutation({
        mutationFn: ({ userId, teamId }: { userId: string; teamId: string }) =>
            apiFetch(`/api/admin/teams/${teamId}/members`, {
                method: "POST", body: JSON.stringify({ userIds: [userId] }),
            }),
        onSuccess: () => {
            setError(null);
            qc.invalidateQueries({ queryKey: ["teams", "unassigned"] });
            qc.invalidateQueries({ queryKey: ["teams", "list"] });
            qc.invalidateQueries({ queryKey: ["admin-teams"] });
        },
        // 409 (équipe pleine) → apiFetch throw avec le message « Équipe pleine … »
        onError: (e: Error) => setError(e.message || "Affectation impossible."),
    });

    const list = unassignedQ.data ?? [];

    return (
        <section className="overflow-hidden rounded-xl border border-border bg-surface shadow-sm">
            <div className="flex items-center gap-2 border-b border-border bg-table-head px-4 py-3 text-xs font-semibold uppercase tracking-wider text-fg-body sm:px-6">
                <UserPlus size={14} /> Membres sans équipe — à affecter ({list.length})
            </div>
            {error && <div className="border-b border-danger/40 bg-danger/10 px-4 py-2 text-xs text-danger sm:px-6">{error}</div>}
            <ul className="divide-y divide-border">
                {list.map(u => (
                    <li key={u.userId} className="flex flex-wrap items-center justify-between gap-3 px-4 py-3 sm:px-6">
                        <div className="min-w-0">
                            <div className="text-sm font-medium text-fg-heading">{u.firstName} {u.lastName}</div>
                            <div className="text-xs text-fg-body">{u.email}</div>
                        </div>
                        <div className="inline-flex items-center gap-1.5 rounded-lg border border-border bg-surface pl-2 text-fg-body">
                            <UserPlus size={13} className="flex-shrink-0" />
                            <select
                                value=""
                                disabled={assignM.isPending || teams.length === 0}
                                onChange={e => { const t = e.target.value; if (t) assignM.mutate({ userId: u.userId, teamId: t }); }}
                                className="max-w-[180px] cursor-pointer rounded-lg bg-surface py-1.5 pr-2 text-xs text-fg-body outline-none"
                                title="Affecter à une équipe">
                                <option value="">Affecter à…</option>
                                {teams.map(t => (
                                    <option key={t.id} value={t.id}
                                        disabled={t.maxMembers != null && t.memberCount >= t.maxMembers}>
                                        {t.name}{t.maxMembers != null ? ` (${t.memberCount}/${t.maxMembers})` : ""}
                                        {t.maxMembers != null && t.memberCount >= t.maxMembers ? " — pleine" : ""}
                                    </option>
                                ))}
                            </select>
                        </div>
                    </li>
                ))}
                {list.length === 0 && (
                    <li className="px-6 py-8 text-center text-sm text-fg-muted">
                        Tous les collaborateurs sont affectés à une équipe. 🎉
                    </li>
                )}
            </ul>
        </section>
    );
}
