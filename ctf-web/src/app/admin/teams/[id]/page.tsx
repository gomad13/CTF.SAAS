"use client";

import { use, useState } from "react";
import Link from "next/link";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
    ArrowLeft, BookOpen, BarChart3, Plus, Trash2, UserPlus,
    Pencil, ArrowRightLeft, Users,
} from "lucide-react";
import { apiFetch } from "@/lib/api";
import { renderTeamIcon } from "@/components/teams/teamIcons";
import TeamEditModal from "@/components/teams/TeamEditModal";
import Reveal from "@/components/Reveal";
import CountUp from "@/components/CountUp";

type Team = {
    id: string; name: string; description: string | null; color: string | null; icon: string | null;
    memberCount: number; parcoursCount: number; compliancePercent: number; createdAt: string;
    maxMembers: number | null; isOpen: boolean;
};
type Member = { userId: string; email: string; firstName: string; lastName: string; role: string | null; isActive: boolean; joinedAt: string | null };
type Candidate = { userId: string; email: string; firstName: string; lastName: string; isActive: boolean; createdAt: string };
type TeamParcours = {
    pathId: string; title: string; level: string | null; moduleCount: number; challengeCount: number;
    deadline: string | null; isMandatory: boolean; avgCompletionPercent: number; assignedAt: string;
};
type Stats = {
    teamId: string; teamName: string; memberCount: number; parcoursCount: number; overallCompletionPercent: number;
    parcoursProgress: TeamParcours[];
    topMembers: { userId: string; email: string; firstName: string; lastName: string; challengesCompleted: number; avgScore: number }[];
};

type TabKey = "members" | "parcours" | "stats";

function complianceColor(pct: number) {
    if (pct >= 80) return "var(--success)";
    if (pct >= 50) return "var(--warning)";
    return "var(--danger)";
}

export default function TeamDetailPage({ params }: { params: Promise<{ id: string }> }) {
    const { id } = use(params);
    const qc = useQueryClient();
    const [tab, setTab] = useState<TabKey>("members");
    const [editing, setEditing] = useState(false);

    const teamQ = useQuery<Team>({
        queryKey: ["team", id], queryFn: () => apiFetch<Team>(`/api/admin/teams/${id}`),
    });

    if (teamQ.isLoading) return <div className="mx-auto max-w-5xl px-6 py-8 text-fg-muted">Chargement…</div>;
    if (teamQ.isError || !teamQ.data) return <div className="mx-auto max-w-5xl px-6 py-8 text-danger">Équipe introuvable.</div>;

    const team = teamQ.data;

    return (
        <div className="mx-auto flex max-w-6xl flex-col gap-6 px-4 py-6 sm:px-6 sm:py-8">
            <Reveal>
            <div>
                <Link href="/admin/teams" className="inline-flex items-center gap-1.5 text-sm font-medium text-fg-muted transition-colors duration-200 hover:text-primary">
                    <ArrowLeft size={14} /> Équipes
                </Link>
                <div className="mt-3 flex items-start gap-3">
                    <span className="flex h-12 w-12 flex-shrink-0 items-center justify-center rounded-xl text-[var(--on-accent)]"
                        style={{ background: team.color ?? "var(--text-2)" }}>
                        {renderTeamIcon(team.icon, 22)}
                    </span>
                    <div className="min-w-0 flex-1">
                        <div className="flex items-center gap-2">
                            <h1 className="truncate text-2xl font-bold text-fg-heading">{team.name}</h1>
                            <button type="button" onClick={() => setEditing(true)}
                                className="inline-flex flex-shrink-0 items-center gap-1.5 rounded-lg border border-border px-2.5 py-1 text-xs font-medium text-fg-muted transition-colors duration-200 hover:border-primary hover:text-primary"
                                title="Modifier l'équipe (nom, capacité, couleur, icône)">
                                <Pencil size={13} /> Modifier
                            </button>
                        </div>
                        {team.description && <p className="text-sm text-fg-muted">{team.description}</p>}
                    </div>
                </div>

                <TeamEditModal team={team} open={editing} onClose={() => setEditing(false)}
                    onSaved={() => { setEditing(false); qc.invalidateQueries({ queryKey: ["team", id] }); qc.invalidateQueries({ queryKey: ["admin-teams"] }); qc.invalidateQueries({ queryKey: ["teams", "list"] }); }} />

                <div className="mt-3 flex flex-wrap items-center gap-3 text-xs text-fg-muted">
                    <span>{team.maxMembers != null ? `${team.memberCount} / ${team.maxMembers} membres` : `${team.memberCount} membre${team.memberCount > 1 ? "s" : ""}`}</span>
                    <span>·</span>
                    <span>{team.parcoursCount} parcours</span>
                    <span>·</span>
                    <span>Compliance <strong style={{ color: complianceColor(team.compliancePercent) }}>{team.compliancePercent}%</strong></span>
                    <span>·</span>
                    <span className={`rounded-full px-2 py-0.5 text-[10px] font-semibold uppercase tracking-wider ${team.isOpen ? "bg-success/15 text-success" : "bg-surface-2 text-fg-muted"}`}>
                        {team.isOpen ? "🔓 Ouverte" : "🔒 Fermée"}
                    </span>
                </div>
            </div>
            </Reveal>

            <div className="flex gap-2 overflow-x-auto border-b border-border">
                {([
                    ["members",  "Membres",           <Users key="u" size={14} />],
                    ["parcours", "Parcours assignés", <BookOpen key="b" size={14} />],
                    ["stats",    "Statistiques",      <BarChart3 key="c" size={14} />],
                ] as [TabKey, string, React.ReactNode][]).map(([key, label, icon]) => (
                    <button key={key} type="button" onClick={() => setTab(key)}
                        className={`inline-flex items-center gap-2 border-b-2 px-4 py-2 text-sm font-medium transition-colors duration-200 ${
                            tab === key ? "border-primary text-fg-heading" : "border-transparent text-fg-muted hover:text-fg-body"
                        }`}>
                        {icon}{label}
                    </button>
                ))}
            </div>

            {tab === "members"  && <MembersTab teamId={id} onChange={() => qc.invalidateQueries({ queryKey: ["team", id] })} />}
            {tab === "parcours" && <ParcoursTab teamId={id} onChange={() => qc.invalidateQueries({ queryKey: ["team", id] })} />}
            {tab === "stats"    && <StatsTab teamId={id} />}
        </div>
    );
}

// ── Onglet Membres ──
function MembersTab({ teamId, onChange }: { teamId: string; onChange: () => void }) {
    const qc = useQueryClient();
    const membersQ = useQuery<Member[]>({ queryKey: ["team-members", teamId], queryFn: () => apiFetch(`/api/admin/teams/${teamId}/members`) });
    // Candidats scopés sur le TENANT DE L'ÉQUIPE et déjà filtrés (non-membres) côté serveur.
    // Corrige « Aucun utilisateur disponible » dû à un tenant divergent côté /api/admin/users.
    const candidatesQ = useQuery<Candidate[]>({
        queryKey: ["team-candidates", teamId], queryFn: () => apiFetch(`/api/admin/teams/${teamId}/candidates`),
    });
    const [showAdd, setShowAdd] = useState(false);
    const [selected, setSelected] = useState<Set<string>>(new Set());
    const [memberError, setMemberError] = useState<string | null>(null);

    type AddResult = { added: number; rejected: number; currentCount: number; maxMembers: number | null; error: string | null };
    const addM = useMutation({
        mutationFn: () => apiFetch<AddResult>(`/api/admin/teams/${teamId}/members`, {
            method: "POST", body: JSON.stringify({ userIds: [...selected] }),
        }),
        onSuccess: (res) => {
            qc.invalidateQueries({ queryKey: ["team-members", teamId] });
            qc.invalidateQueries({ queryKey: ["team-candidates", teamId] });
            qc.invalidateQueries({ queryKey: ["teams", "unassigned"] });
            onChange();
            // M4 : ajout partiel possible si capacité dépassée → on informe sans bloquer.
            setMemberError(res?.error ?? null);
            if (!res?.error) { setShowAdd(false); setSelected(new Set()); }
        },
        // 409 (équipe pleine, rien ajouté) → message clair
        onError: (e: Error) => setMemberError(e.message || "Ajout impossible."),
    });
    const removeM = useMutation({
        mutationFn: (userId: string) => apiFetch(`/api/admin/teams/${teamId}/members/${userId}`, { method: "DELETE" }),
        onSuccess: () => { qc.invalidateQueries({ queryKey: ["team-members", teamId] }); qc.invalidateQueries({ queryKey: ["team-candidates", teamId] }); qc.invalidateQueries({ queryKey: ["teams", "unassigned"] }); onChange(); },
    });

    // Many-to-many : ajouter le membre à une AUTRE équipe (sans le retirer de celle-ci).
    // POST sur l'équipe cible → crée une appartenance supplémentaire.
    const teamsQ = useQuery<Team[]>({ queryKey: ["admin-teams"], queryFn: () => apiFetch("/api/admin/teams") });
    const moveM = useMutation({
        mutationFn: ({ userId, targetTeamId }: { userId: string; targetTeamId: string }) =>
            apiFetch(`/api/admin/teams/${targetTeamId}/members`, {
                method: "POST", body: JSON.stringify({ userIds: [userId] }),
            }),
        onSuccess: () => { setMemberError(null); qc.invalidateQueries({ queryKey: ["team-members", teamId] }); qc.invalidateQueries({ queryKey: ["admin-teams"] }); qc.invalidateQueries({ queryKey: ["teams", "list"] }); qc.invalidateQueries({ queryKey: ["teams", "unassigned"] }); onChange(); },
        onError: (e: Error) => setMemberError(e.message || "Ajout impossible (équipe pleine ?)."),
    });
    const otherTeams = (teamsQ.data ?? []).filter(t => t.id !== teamId);

    const candidateUsers = candidatesQ.data ?? [];

    return (
        <div className="flex flex-col gap-4">
            {memberError && (
                <div className="rounded-lg border border-danger/40 bg-danger/10 px-3 py-2 text-xs text-danger">{memberError}</div>
            )}
            <div className="flex justify-end">
                <button type="button" onClick={() => { setMemberError(null); setShowAdd(v => !v); }}
                    className="inline-flex items-center gap-2 rounded-lg bg-primary px-4 py-2 text-sm font-medium text-[var(--on-accent)] transition-colors duration-200 hover:bg-primary-hover">
                    <UserPlus size={14} /> Ajouter des membres
                </button>
            </div>

            {showAdd && (
                <section className="rounded-xl border border-border bg-surface p-4 shadow-sm">
                    <p className="mb-3 text-sm text-fg-body">Sélectionne les collaborateurs à intégrer dans cette équipe.</p>
                    <div className="grid max-h-64 grid-cols-1 gap-1 overflow-y-auto md:grid-cols-2">
                        {candidateUsers.map(u => {
                            const ok = selected.has(u.userId);
                            return (
                                <button key={u.userId} type="button" onClick={() => {
                                    const next = new Set(selected);
                                    if (ok) next.delete(u.userId); else next.add(u.userId);
                                    setSelected(next);
                                }}
                                    className={`flex items-center justify-between rounded-lg border px-3 py-2 text-left text-sm transition-colors duration-200 ${
                                        ok ? "border-primary bg-primary text-[var(--on-accent)]" : "border-border bg-surface text-fg-body hover:bg-surface-2"
                                    }`}>
                                    <div>
                                        <div className="font-medium">{u.firstName} {u.lastName}</div>
                                        <div className={`text-xs ${ok ? "text-[var(--on-accent)]/80" : "text-fg-muted"}`}>{u.email}</div>
                                    </div>
                                    {ok && <span className="text-xs">✓</span>}
                                </button>
                            );
                        })}
                        {candidateUsers.length === 0 && <p className="text-sm text-fg-muted">Aucun utilisateur disponible.</p>}
                    </div>
                    <div className="mt-4 flex gap-2">
                        <button type="button" disabled={selected.size === 0 || addM.isPending}
                            onClick={() => addM.mutate()}
                            className="rounded-lg bg-primary px-4 py-2 text-sm font-medium text-[var(--on-accent)] transition-colors duration-200 hover:bg-primary-hover disabled:opacity-50">
                            Ajouter ({selected.size})
                        </button>
                        <button type="button" onClick={() => { setShowAdd(false); setSelected(new Set()); }}
                            className="rounded-lg border border-border bg-surface px-4 py-2 text-sm font-medium text-fg-body transition-colors duration-200 hover:bg-surface-2">
                            Annuler
                        </button>
                    </div>
                </section>
            )}

            <section className="overflow-hidden rounded-xl border border-border bg-surface shadow-sm">
                <div className="border-b border-border bg-table-head px-4 py-3 text-xs font-semibold uppercase tracking-wider text-fg-body sm:px-6">
                    Membres ({membersQ.data?.length ?? 0})
                </div>
                <ul className="divide-y divide-border">
                    {(membersQ.data ?? []).map(m => (
                        <li key={m.userId} className="flex flex-wrap items-center justify-between gap-3 px-4 py-3 sm:px-6">
                            <div className="min-w-0">
                                <div className="text-sm font-medium text-fg-heading">{m.firstName} {m.lastName}</div>
                                <div className="text-xs text-fg-body">{m.email}</div>
                            </div>
                            <div className="flex items-center gap-2">
                                {otherTeams.length > 0 && (
                                    <div className="inline-flex items-center gap-1.5 rounded-lg border border-border bg-surface pl-2 text-fg-body">
                                        <ArrowRightLeft size={13} className="flex-shrink-0" />
                                        <select
                                            value=""
                                            disabled={moveM.isPending}
                                            onChange={e => { const t = e.target.value; if (t) moveM.mutate({ userId: m.userId, targetTeamId: t }); }}
                                            className="max-w-[150px] cursor-pointer rounded-lg bg-surface py-1.5 pr-2 text-xs text-fg-body outline-none"
                                            title="Ajouter ce membre à une autre équipe (multi-équipes)">
                                            <option value="">Ajouter à…</option>
                                            {otherTeams.map(t => <option key={t.id} value={t.id}>{t.name}</option>)}
                                        </select>
                                    </div>
                                )}
                                <button type="button"
                                    onClick={() => { if (confirm(`Retirer ${m.firstName} ${m.lastName} de l'équipe ?`)) removeM.mutate(m.userId); }}
                                    className="rounded-lg border border-danger/40 bg-surface p-1.5 text-danger transition-colors duration-200 hover:bg-danger/10" title="Retirer">
                                    <Trash2 size={14} />
                                </button>
                            </div>
                        </li>
                    ))}
                    {(membersQ.data?.length ?? 0) === 0 && (
                        <li className="px-6 py-8 text-center text-sm text-fg-muted">Aucun membre pour l&apos;instant.</li>
                    )}
                </ul>
            </section>
        </div>
    );
}

// ── Onglet Parcours ──
function ParcoursTab({ teamId, onChange }: { teamId: string; onChange: () => void }) {
    const qc = useQueryClient();
    const parcoursQ = useQuery<TeamParcours[]>({ queryKey: ["team-parcours", teamId], queryFn: () => apiFetch(`/api/admin/teams/${teamId}/parcours`) });
    const allPathsQ = useQuery<{ id: string; title: string; level: string | null }[]>({
        queryKey: ["admin-paths-list"], queryFn: () => apiFetch("/api/admin/paths"),
    });
    const [showAssign, setShowAssign] = useState(false);
    const [selectedPath, setSelectedPath] = useState<string>("");
    const [deadline, setDeadline] = useState<string>("");
    const [isMandatory, setIsMandatory] = useState(false);

    const assignedIds = new Set((parcoursQ.data ?? []).map(p => p.pathId));
    const candidatePaths = (allPathsQ.data ?? []).filter(p => !assignedIds.has(p.id));

    const assignM = useMutation({
        mutationFn: () => apiFetch(`/api/admin/teams/${teamId}/parcours`, {
            method: "POST",
            body: JSON.stringify({
                pathId: selectedPath,
                deadline: deadline ? new Date(deadline).toISOString() : null,
                isMandatory,
            }),
        }),
        onSuccess: () => {
            qc.invalidateQueries({ queryKey: ["team-parcours", teamId] });
            onChange();
            setShowAssign(false); setSelectedPath(""); setDeadline(""); setIsMandatory(false);
        },
    });
    const removeM = useMutation({
        mutationFn: (pathId: string) => apiFetch(`/api/admin/teams/${teamId}/parcours/${pathId}`, { method: "DELETE" }),
        onSuccess: () => { qc.invalidateQueries({ queryKey: ["team-parcours", teamId] }); onChange(); },
    });

    return (
        <div className="flex flex-col gap-4">
            <div className="flex justify-end">
                <button type="button" onClick={() => setShowAssign(v => !v)}
                    className="inline-flex items-center gap-2 rounded-lg bg-primary px-4 py-2 text-sm font-medium text-[var(--on-accent)] transition-colors duration-200 hover:bg-primary-hover">
                    <Plus size={14} /> Assigner un parcours
                </button>
            </div>

            {showAssign && (
                <section className="rounded-xl border border-border bg-surface p-4 shadow-sm">
                    <div className="grid grid-cols-1 gap-3 md:grid-cols-[1fr_180px_auto]">
                        <select value={selectedPath} onChange={e => setSelectedPath(e.target.value)}
                            className="rounded-lg border border-border bg-surface px-3 py-2 text-sm text-fg-heading">
                            <option value="">— Choisir un parcours —</option>
                            {candidatePaths.map(p => <option key={p.id} value={p.id}>{p.title}</option>)}
                        </select>
                        <input type="date" value={deadline} onChange={e => setDeadline(e.target.value)}
                            className="rounded-lg border border-border bg-surface px-3 py-2 text-sm text-fg-heading" />
                        <label className="flex items-center gap-2 px-3 text-sm text-fg-body">
                            <input type="checkbox" checked={isMandatory} onChange={e => setIsMandatory(e.target.checked)} />
                            Obligatoire
                        </label>
                    </div>
                    <div className="mt-4 flex gap-2">
                        <button type="button" disabled={!selectedPath || assignM.isPending}
                            onClick={() => assignM.mutate()}
                            className="rounded-lg bg-primary px-4 py-2 text-sm font-medium text-[var(--on-accent)] transition-colors duration-200 hover:bg-primary-hover disabled:opacity-50">
                            Assigner
                        </button>
                        <button type="button" onClick={() => setShowAssign(false)}
                            className="rounded-lg border border-border bg-surface px-4 py-2 text-sm font-medium text-fg-body transition-colors duration-200 hover:bg-surface-2">
                            Annuler
                        </button>
                    </div>
                </section>
            )}

            <section className="overflow-hidden rounded-xl border border-border bg-surface shadow-sm">
                <div className="border-b border-border bg-table-head px-4 py-3 text-xs font-semibold uppercase tracking-wider text-fg-body sm:px-6">
                    Parcours assignés ({parcoursQ.data?.length ?? 0})
                </div>
                <ul className="divide-y divide-border">
                    {(parcoursQ.data ?? []).map(p => (
                        <li key={p.pathId} className="flex flex-col gap-3 px-4 py-3 sm:grid sm:grid-cols-[1fr_auto_auto_auto] sm:items-center sm:gap-4 sm:px-6">
                            <div className="min-w-0">
                                <div className="flex items-center gap-2">
                                    <span className="text-sm font-medium text-fg-heading">{p.title}</span>
                                    {p.isMandatory && (
                                        <span className="rounded-full bg-danger/10 px-2 py-0.5 text-[10px] font-semibold uppercase tracking-wider text-danger">
                                            Obligatoire
                                        </span>
                                    )}
                                </div>
                                <div className="text-xs text-fg-body">
                                    {p.moduleCount} module{p.moduleCount > 1 ? "s" : ""} · {p.challengeCount} challenges
                                    {p.deadline && ` · deadline ${new Date(p.deadline).toLocaleDateString("fr-FR")}`}
                                </div>
                            </div>
                            <div className="flex items-center gap-3 sm:contents">
                                <div className="h-1.5 flex-1 overflow-hidden rounded-full bg-surface-2 sm:w-40 sm:flex-none">
                                    <div className="h-full rounded-full bg-primary" style={{ width: `${p.avgCompletionPercent}%` }} />
                                </div>
                                <span className="shrink-0 text-sm font-semibold" style={{ color: complianceColor(p.avgCompletionPercent) }}>
                                    {p.avgCompletionPercent}%
                                </span>
                                <button type="button" onClick={() => { if (confirm(`Retirer l'assignation ?`)) removeM.mutate(p.pathId); }}
                                    className="shrink-0 rounded-lg border border-danger/40 bg-surface p-1.5 text-danger transition-colors duration-200 hover:bg-danger/10" title="Retirer">
                                    <Trash2 size={14} />
                                </button>
                            </div>
                        </li>
                    ))}
                    {(parcoursQ.data?.length ?? 0) === 0 && (
                        <li className="px-6 py-8 text-center text-sm text-fg-muted">
                            Aucun parcours assigné. Cliquez sur « Assigner un parcours » pour commencer.
                        </li>
                    )}
                </ul>
            </section>
        </div>
    );
}

// ── Onglet Stats ──
function StatsTab({ teamId }: { teamId: string }) {
    const statsQ = useQuery<Stats>({ queryKey: ["team-stats", teamId], queryFn: () => apiFetch(`/api/admin/teams/${teamId}/stats`) });

    if (!statsQ.data) return <div className="text-sm text-fg-muted">Chargement…</div>;
    const s = statsQ.data;
    return (
        <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
            <section className="rounded-xl border border-border bg-surface p-6 shadow-sm">
                <h3 className="text-sm font-semibold uppercase tracking-wider text-fg-body">Compliance globale</h3>
                <div className="mt-2 text-3xl font-bold" style={{ color: complianceColor(s.overallCompletionPercent) }}>
                    <CountUp value={s.overallCompletionPercent} suffix="%" />
                </div>
                <p className="mt-1 text-xs text-fg-muted">
                    {s.memberCount} membre{s.memberCount > 1 ? "s" : ""} · {s.parcoursCount} parcours
                </p>
            </section>

            <section className="rounded-xl border border-border bg-surface p-6 shadow-sm">
                <h3 className="text-sm font-semibold uppercase tracking-wider text-fg-body">Top collaborateurs</h3>
                <ul className="mt-3 flex flex-col gap-2">
                    {s.topMembers.map((m, i) => (
                        <li key={m.userId} className="flex items-center justify-between text-sm">
                            <span className="text-fg-heading">#{i + 1} {m.firstName} {m.lastName}</span>
                            <span className="font-semibold text-primary">{m.challengesCompleted} complétés · {m.avgScore}%</span>
                        </li>
                    ))}
                    {s.topMembers.length === 0 && <li className="text-sm text-fg-muted">Aucune activité pour l&apos;instant.</li>}
                </ul>
            </section>

            <section className="rounded-xl border border-border bg-surface p-6 shadow-sm md:col-span-2">
                <h3 className="text-sm font-semibold uppercase tracking-wider text-fg-body">Progression par parcours</h3>
                <ul className="mt-3 flex flex-col gap-3">
                    {s.parcoursProgress.map(p => (
                        <li key={p.pathId} className="flex flex-col gap-2 text-sm sm:grid sm:grid-cols-[1fr_auto_auto] sm:items-center sm:gap-4">
                            <span className="text-fg-heading">{p.title}</span>
                            <div className="flex items-center gap-3 sm:contents">
                                <div className="h-1.5 flex-1 overflow-hidden rounded-full bg-surface-2 sm:w-48 sm:flex-none">
                                    <div className="h-full rounded-full bg-primary" style={{ width: `${p.avgCompletionPercent}%` }} />
                                </div>
                                <span className="shrink-0 font-semibold" style={{ color: complianceColor(p.avgCompletionPercent) }}>
                                    {p.avgCompletionPercent}%
                                </span>
                            </div>
                        </li>
                    ))}
                    {s.parcoursProgress.length === 0 && <li className="text-sm text-fg-muted">Aucun parcours assigné.</li>}
                </ul>
            </section>
        </div>
    );
}
