"use client";

import { use, useMemo, useState } from "react";
import Link from "next/link";
import { useQuery } from "@tanstack/react-query";
import {
    ArrowLeft, Calendar, FolderOpen, Mail, Users, AlertTriangle,
    CheckCircle2, Clock, Activity, UserPlus,
} from "lucide-react";
import { apiFetch } from "@/lib/api";
import { toast } from "@/components/Toast";
import {
    useAssignEmployees,
    useCampaign,
    useCampaignDashboard,
} from "@/lib/hooks/useCampaigns";
import { STATUS_STYLES, type CampaignDetail, type CampaignDashboard } from "@/lib/types/campaigns";
import Reveal from "@/components/Reveal";
import CountUp from "@/components/CountUp";

type UserListItem = { id: string; firstName: string; lastName: string; email: string; role: string | null; isActive: boolean };
type UserListResponse = { items: UserListItem[]; total: number };

export default function CampaignDetailPage({ params }: { params: Promise<{ id: string }> }) {
    const { id } = use(params);
    const detailQ = useCampaign(id);
    const dashboardQ = useCampaignDashboard(id);
    const [assignOpen, setAssignOpen] = useState(false);

    if (detailQ.isLoading) {
        return <div className="px-6 py-12 text-center text-fg-muted">Chargement…</div>;
    }
    if (detailQ.isError) {
        return (
            <div className="mx-auto max-w-3xl px-6 py-12 text-center">
                <p className="text-sm text-danger">Campagne introuvable.</p>
                <Link href="/admin/campaigns" className="mt-3 inline-block text-sm text-primary">← Retour</Link>
            </div>
        );
    }

    const detail = detailQ.data!;
    const dashboard = dashboardQ.data;
    const s = STATUS_STYLES[detail.status];

    return (
        <div className="mx-auto flex max-w-6xl flex-col gap-6 px-4 py-6 sm:px-6 sm:py-8">
            <Reveal>
            <div>
                <Link href="/admin/campaigns" className="inline-flex items-center gap-1 text-xs text-fg-muted transition-colors duration-200 hover:text-fg-heading">
                    <ArrowLeft size={12} /> Toutes les campagnes
                </Link>
                <div className="mt-2 flex flex-col items-start gap-3 sm:flex-row sm:justify-between">
                    <div>
                        <h1 className="text-2xl font-bold text-fg-heading">{detail.name}</h1>
                        {detail.description && <p className="mt-1 text-sm text-fg-muted">{detail.description}</p>}
                        <div className="mt-2 flex flex-wrap items-center gap-2 text-xs text-fg-muted">
                            <span
                                className="rounded-full px-2 py-0.5 text-[10px] font-semibold uppercase"
                                style={{ background: s.bg, color: s.color, border: `1px solid ${s.border}` }}
                            >
                                {s.label}
                            </span>
                            <span className="inline-flex items-center gap-1">
                                <Calendar size={11} />
                                {new Date(detail.startDate).toLocaleDateString("fr-FR")} → {new Date(detail.endDate).toLocaleDateString("fr-FR")}
                            </span>
                            {detail.assignedToWholeTenant && (
                                <span className="rounded-full bg-primary/15 px-2 py-0.5 text-[10px] font-medium text-primary">
                                    TOUTE L&apos;ENTREPRISE
                                </span>
                            )}
                        </div>
                    </div>
                    <button
                        type="button"
                        onClick={() => setAssignOpen(true)}
                        className="inline-flex w-full shrink-0 items-center justify-center gap-2 rounded-lg bg-primary px-3 py-2 text-sm font-medium text-[var(--on-accent)] transition-colors duration-200 hover:bg-primary-hover sm:w-auto"
                    >
                        <UserPlus size={14} /> Assigner des employés
                    </button>
                </div>
            </div>
            </Reveal>

            {/* Dashboard stats */}
            {dashboard && <DashboardStatsCards d={dashboard} />}

            {/* Contenus inclus */}
            <ContentsSection detail={detail} />

            {/* Employés assignés + progression */}
            {dashboard && <EmployeeProgressTable d={dashboard} />}

            {/* Modal d'assignation */}
            {assignOpen && (
                <AssignModal
                    campaignId={id}
                    initialWholeTenant={detail.assignedToWholeTenant}
                    onClose={() => setAssignOpen(false)}
                />
            )}
        </div>
    );
}

function DashboardStatsCards({ d }: { d: CampaignDashboard }) {
    const pct = Math.max(0, Math.min(100, Math.round(d.globalCompletionPercentage)));
    return (
        <Reveal className="grid grid-cols-2 gap-3 md:grid-cols-5">
            <StatCard label="Assignés" value={d.totalAssigned} icon={<Users size={14} />} color="var(--accent)" />
            <StatCard label="Pas commencé" value={d.notStarted} icon={<Clock size={14} />} color="var(--text-2)" />
            <StatCard label="En cours" value={d.inProgress} icon={<Activity size={14} />} color="var(--warning)" />
            <StatCard label="Terminé" value={d.completed} icon={<CheckCircle2 size={14} />} color="var(--success)" />
            <StatCard label="En retard" value={d.lateEmployeesCount} icon={<AlertTriangle size={14} />} color="var(--danger)" highlight={d.lateEmployeesCount > 0} />

            <div className="col-span-2 rounded-xl border border-border bg-surface p-4 text-fg-heading shadow-sm md:col-span-3">
                <div className="text-xs font-medium uppercase tracking-wider text-fg-body">Complétion globale</div>
                <div className="mt-2 flex items-center gap-3">
                    <div className="h-2.5 flex-1 overflow-hidden rounded-full bg-surface-2">
                        <div className="h-full rounded-full bg-primary transition-all" style={{ width: `${pct}%` }} />
                    </div>
                    <span className="text-sm font-semibold text-fg-heading"><CountUp value={pct} suffix="%" /></span>
                </div>
            </div>
            <div className="col-span-2 rounded-xl border border-border bg-surface p-4 text-fg-heading shadow-sm">
                <div className="text-xs font-medium uppercase tracking-wider text-fg-body">Taux de réussite scénarios</div>
                <div className="mt-2 text-2xl font-bold text-success"><CountUp value={Math.round(d.averageSuccessRate)} suffix="%" /></div>
                <div className="text-[11px] text-fg-muted">moyenne sur les scénarios terminés</div>
            </div>
        </Reveal>
    );
}

function StatCard({ label, value, icon, color, highlight = false }: {
    label: string; value: number; icon: React.ReactNode; color: string; highlight?: boolean;
}) {
    return (
        <div className={`rounded-xl border bg-surface p-4 text-fg-heading shadow-sm transition-colors ${highlight ? "border-danger" : "border-border"}`}>
            <div className="flex items-center gap-2 text-xs font-medium uppercase tracking-wider text-fg-body">
                <span style={{ color }}>{icon}</span> {label}
            </div>
            <div className="mt-1 text-2xl font-bold" style={{ color: highlight ? "var(--danger)" : "var(--text)" }}><CountUp value={value} /></div>
        </div>
    );
}

function ContentsSection({ detail }: { detail: CampaignDetail }) {
    return (
        <section className="rounded-xl border border-border bg-surface p-6 text-fg-heading shadow-sm">
            <h2 className="text-sm font-semibold uppercase tracking-wider text-fg-body">
                Contenus inclus ({detail.contents.length})
            </h2>
            <ul className="mt-3 divide-y divide-border">
                {detail.contents.map(c => (
                    <li key={c.id} className="flex items-center gap-3 py-2.5">
                        <span
                            className="inline-flex h-7 w-7 items-center justify-center rounded-full"
                            style={{
                                background: c.contentType === "Parcours" ? "var(--accent-subtle)" : "var(--success-subtle)",
                                color: c.contentType === "Parcours" ? "var(--accent)" : "var(--success)",
                            }}
                        >
                            {c.contentType === "Parcours" ? <FolderOpen size={14} /> : <Mail size={14} />}
                        </span>
                        <div className="min-w-0 flex-1">
                            <div className="truncate text-sm font-medium text-fg-heading">{c.title}</div>
                            <div className="text-[11px] text-fg-muted">
                                {c.contentType === "Parcours" ? "Parcours" : "Scénario"}
                                {c.category && ` · ${c.category}`}
                            </div>
                        </div>
                    </li>
                ))}
                {detail.contents.length === 0 && (
                    <li className="py-6 text-center text-sm text-fg-muted">Aucun contenu.</li>
                )}
            </ul>
        </section>
    );
}

function EmployeeProgressTable({ d }: { d: CampaignDashboard }) {
    return (
        <section className="rounded-xl border border-border bg-surface text-fg-heading shadow-sm">
            <div className="border-b border-border bg-table-head px-4 py-3 text-xs font-semibold uppercase tracking-wider text-fg-body sm:px-6">
                Employés assignés ({d.employeeProgress.length})
            </div>
            <div className="resp-scroll-x">
            <table className="w-full">
                <thead>
                    <tr className="text-left text-[11px] uppercase tracking-wider text-fg-muted">
                        <th className="px-6 py-2.5">Employé</th>
                        <th className="px-6 py-2.5">Statut</th>
                        <th className="px-6 py-2.5">Progression</th>
                    </tr>
                </thead>
                <tbody>
                    {d.employeeProgress.map(e => {
                        const statusLabel = { NotStarted: "Pas commencé", InProgress: "En cours", Completed: "Terminé" }[e.status] ?? e.status;
                        const statusColor = { NotStarted: "var(--text-2)", InProgress: "var(--warning)", Completed: "var(--success)" }[e.status] ?? "var(--text-2)";
                        return (
                            <tr key={e.userId} className="border-t border-border transition-colors hover:bg-surface-2">
                                <td className="px-6 py-2.5">
                                    <div className="text-sm font-medium text-fg-heading">{e.firstName} {e.lastName}</div>
                                    <div className="text-[11px] text-fg-muted">{e.email}</div>
                                </td>
                                <td className="px-6 py-2.5">
                                    <span className="inline-flex items-center gap-1.5 text-xs font-medium" style={{ color: statusColor }}>
                                        {statusLabel}
                                        {e.isLate && (
                                            <span className="inline-flex items-center gap-1 rounded-full bg-danger/10 px-2 py-0.5 text-[10px] font-semibold text-danger">
                                                <AlertTriangle size={10} /> En retard
                                            </span>
                                        )}
                                    </span>
                                </td>
                                <td className="px-6 py-2.5">
                                    <div className="flex items-center gap-2">
                                        <div className="h-1.5 w-32 overflow-hidden rounded-full bg-surface-2">
                                            <div
                                                className="h-full rounded-full bg-primary"
                                                style={{ width: `${Math.round(e.completionPercentage)}%` }}
                                            />
                                        </div>
                                        <span className="text-xs text-fg-body">{Math.round(e.completionPercentage)}%</span>
                                    </div>
                                </td>
                            </tr>
                        );
                    })}
                    {d.employeeProgress.length === 0 && (
                        <tr><td colSpan={3} className="px-6 py-8 text-center text-sm text-fg-muted">Aucun employé assigné.</td></tr>
                    )}
                </tbody>
            </table>
            </div>
        </section>
    );
}

function AssignModal({
    campaignId, initialWholeTenant, onClose,
}: { campaignId: string; initialWholeTenant: boolean; onClose: () => void }) {
    const [wholeTenant, setWholeTenant] = useState(initialWholeTenant);
    const [selected, setSelected] = useState<Set<string>>(new Set());
    const [search, setSearch] = useState("");
    const usersQ = useQuery<UserListResponse>({
        queryKey: ["admin-users-list-campaign"],
        queryFn: () => apiFetch("/api/admin/users?pageSize=200"),
        staleTime: 60_000,
    });
    const assignM = useAssignEmployees(campaignId);

    const filteredUsers = useMemo(() => {
        const items = usersQ.data?.items ?? [];
        if (!search.trim()) return items;
        const s = search.toLowerCase();
        return items.filter(u =>
            u.firstName.toLowerCase().includes(s) ||
            u.lastName.toLowerCase().includes(s) ||
            u.email.toLowerCase().includes(s)
        );
    }, [usersQ.data, search]);

    function toggleUser(id: string) {
        setSelected(prev => {
            const next = new Set(prev);
            if (next.has(id)) next.delete(id);
            else next.add(id);
            return next;
        });
    }

    async function handleConfirm() {
        try {
            await assignM.mutateAsync({
                assignToWholeTenant: wholeTenant,
                userIds: wholeTenant ? null : Array.from(selected),
            });
            toast.ok("Assignation effectuée");
            onClose();
        } catch (e) {
            toast.er(e instanceof Error ? e.message : "Erreur assignation");
        }
    }

    const canConfirm = wholeTenant || selected.size > 0;

    return (
        <div className="modal-overlay" onClick={onClose}>
            <div className="modal-box" style={{ maxWidth: 600 }} onClick={e => e.stopPropagation()}>
                <h2 style={{ fontSize: 18, fontWeight: 700, color: "var(--text)", margin: "0 0 8px" }}>
                    Assigner des employés
                </h2>
                <p style={{ fontSize: 13, color: "var(--text-2)", margin: "0 0 16px" }}>
                    Choisissez d&apos;assigner la campagne à toute l&apos;entreprise ou à une sélection d&apos;employés.
                </p>

                <div style={{ display: "flex", flexDirection: "column", gap: 10 }}>
                    <label className="inline-flex cursor-pointer items-center gap-2 text-sm text-fg-heading">
                        <input type="radio" name="assign-mode" checked={wholeTenant} onChange={() => setWholeTenant(true)} />
                        <span className="font-medium">Toute l&apos;entreprise</span>
                        <span className="text-xs text-fg-muted">— tous les employés actifs du tenant</span>
                    </label>
                    <label className="inline-flex cursor-pointer items-center gap-2 text-sm text-fg-heading">
                        <input type="radio" name="assign-mode" checked={!wholeTenant} onChange={() => setWholeTenant(false)} />
                        <span className="font-medium">Sélectionner des employés</span>
                    </label>
                </div>

                {!wholeTenant && (
                    <div className="mt-4">
                        <input
                            type="text"
                            placeholder="Rechercher un employé…"
                            className="w-full rounded-lg border border-border bg-surface px-3 py-2 text-sm text-fg-heading placeholder:text-fg-muted"
                            value={search}
                            onChange={e => setSearch(e.target.value)}
                        />
                        <div className="mt-2 max-h-64 overflow-y-auto rounded-lg border border-border">
                            {usersQ.isLoading ? (
                                <div className="px-4 py-3 text-sm text-fg-muted">Chargement…</div>
                            ) : filteredUsers.length === 0 ? (
                                <div className="px-4 py-3 text-sm text-fg-muted">Aucun employé trouvé.</div>
                            ) : (
                                <ul className="divide-y divide-border">
                                    {filteredUsers.map(u => (
                                        <li key={u.id} className="flex items-center gap-3 px-4 py-2.5">
                                            <input
                                                type="checkbox"
                                                checked={selected.has(u.id)}
                                                onChange={() => toggleUser(u.id)}
                                            />
                                            <div className="min-w-0 flex-1">
                                                <div className="truncate text-sm text-fg-heading">{u.firstName} {u.lastName}</div>
                                                <div className="text-[11px] text-fg-muted">{u.email}</div>
                                            </div>
                                            {!u.isActive && <span className="text-[10px] text-fg-muted">Inactif</span>}
                                        </li>
                                    ))}
                                </ul>
                            )}
                        </div>
                        <p className="mt-1 text-[11px] text-fg-muted">{selected.size} employé{selected.size > 1 ? "s" : ""} sélectionné{selected.size > 1 ? "s" : ""}.</p>
                    </div>
                )}

                <div className="mt-5 flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
                    <button type="button" onClick={onClose} className="btn btn-secondary btn-md w-full sm:w-auto">Annuler</button>
                    <button
                        type="button"
                        onClick={handleConfirm}
                        disabled={!canConfirm || assignM.isPending}
                        className="btn btn-primary btn-md w-full disabled:opacity-50 sm:w-auto"
                    >
                        {assignM.isPending ? "Assignation…" : "Confirmer l'assignation"}
                    </button>
                </div>
            </div>
        </div>
    );
}
