"use client";

import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Users, LogIn, LogOut, Lock, Unlock } from "lucide-react";
import { apiFetch } from "@/lib/api";
import { renderTeamIcon } from "@/components/teams/teamIcons";
import Reveal from "@/components/Reveal";
import { Stagger, StaggerItem } from "@/components/Stagger";

type UserTeam = {
    teamId: string; name: string; description: string | null; color: string | null; icon: string | null;
    memberCount: number; maxMembers: number | null; isOpen: boolean; isMember: boolean; isFull: boolean;
};
type MyTeamMember = { userId: string; firstName: string; lastName: string; role: string | null };
type MyTeam = {
    teamId: string; name: string; description: string | null; color: string | null; icon: string | null;
    isOpen: boolean; memberCount: number; maxMembers: number | null; members: MyTeamMember[];
};

function capacity(t: { memberCount: number; maxMembers: number | null }) {
    return t.maxMembers != null ? `${t.memberCount} / ${t.maxMembers} membres` : `${t.memberCount} membre${t.memberCount > 1 ? "s" : ""}`;
}

export default function MesEquipesPage() {
    const qc = useQueryClient();
    const [error, setError] = useState<string | null>(null);

    const browseQ = useQuery<UserTeam[]>({ queryKey: ["user-teams", "browse"], queryFn: () => apiFetch("/api/user-teams") });
    const mineQ = useQuery<MyTeam[]>({ queryKey: ["user-teams", "mine"], queryFn: () => apiFetch("/api/user-teams/mine") });

    const refresh = () => {
        qc.invalidateQueries({ queryKey: ["user-teams", "browse"] });
        qc.invalidateQueries({ queryKey: ["user-teams", "mine"] });
    };

    const joinM = useMutation({
        mutationFn: (teamId: string) => apiFetch(`/api/user-teams/${teamId}/join`, { method: "POST" }),
        onSuccess: () => { setError(null); refresh(); },
        onError: (e: Error) => setError(e.message || "Impossible de rejoindre cette équipe."),
    });
    const leaveM = useMutation({
        mutationFn: (teamId: string) => apiFetch(`/api/user-teams/${teamId}/leave`, { method: "POST" }),
        onSuccess: () => { setError(null); refresh(); },
        onError: (e: Error) => setError(e.message || "Impossible de quitter cette équipe."),
    });

    const teams = browseQ.data ?? [];
    const mine = mineQ.data ?? [];

    return (
        <div className="mx-auto flex max-w-5xl flex-col gap-6 px-4 py-6 sm:px-6 sm:py-8">
            <Reveal>
                <div>
                    <h1 className="text-2xl font-bold text-fg-heading">Mes équipes</h1>
                    <p className="mt-1 text-sm text-fg-muted">Rejoignez une équipe ouverte de votre entreprise et consultez vos équipes.</p>
                </div>
            </Reveal>

            {error && <div className="rounded-lg border border-danger/40 bg-danger/10 px-3 py-2 text-sm text-danger">{error}</div>}

            {/* ── Mes équipes (membre) ── */}
            <section className="flex flex-col gap-3">
                <h2 className="text-sm font-semibold uppercase tracking-wider text-fg-muted">Vos équipes</h2>
                {mine.length === 0 ? (
                    <p className="rounded-xl border border-border bg-surface p-6 text-center text-sm text-fg-muted">
                        Vous n&apos;êtes membre d&apos;aucune équipe pour le moment.
                    </p>
                ) : (
                    <Stagger className="grid grid-cols-1 gap-4 md:grid-cols-2" gap={0.06}>
                        {mine.map(t => (
                            <StaggerItem key={t.teamId}>
                            <div className="rounded-xl border border-border bg-surface p-6 shadow-sm transition-colors duration-200 hover:border-accent">
                                <div className="flex items-start gap-3">
                                    <span className="flex h-10 w-10 flex-shrink-0 items-center justify-center rounded-lg text-white" style={{ background: t.color ?? "var(--text-3)" }}>
                                        {renderTeamIcon(t.icon, 18)}
                                    </span>
                                    <div className="min-w-0 flex-1">
                                        <div className="flex items-center gap-2">
                                            <h3 className="truncate font-semibold text-fg-heading">{t.name}</h3>
                                            <span className={`rounded-full px-2 py-0.5 text-[10px] font-semibold uppercase tracking-wider ${t.isOpen ? "bg-success/10 text-success" : "bg-surface-2 text-fg-muted"}`}>
                                                {t.isOpen ? "Ouverte" : "Fermée"}
                                            </span>
                                        </div>
                                        {t.description && <p className="mt-0.5 text-sm text-fg-body">{t.description}</p>}
                                        <p className="mt-1 text-xs text-fg-muted">{capacity(t)}</p>
                                    </div>
                                </div>
                                <div className="mt-4">
                                    <div className="mb-1.5 text-xs font-semibold uppercase tracking-wider text-fg-muted">Membres</div>
                                    <ul className="flex flex-col gap-1">
                                        {t.members.map(m => (
                                            <li key={m.userId} className="flex items-center gap-2 text-sm text-fg-body">
                                                <span className="flex h-6 w-6 items-center justify-center rounded-full bg-surface-2 text-[10px] font-bold text-fg-body">
                                                    {(m.firstName?.[0] ?? "") + (m.lastName?.[0] ?? "")}
                                                </span>
                                                {m.firstName} {m.lastName}
                                            </li>
                                        ))}
                                    </ul>
                                </div>
                                <button type="button" onClick={() => leaveM.mutate(t.teamId)} disabled={leaveM.isPending}
                                    className="mt-4 inline-flex items-center gap-2 rounded-lg border border-danger/40 bg-surface px-3 py-1.5 text-xs font-medium text-danger transition-colors duration-200 hover:bg-danger/10 disabled:opacity-50">
                                    <LogOut size={13} /> Quitter
                                </button>
                            </div>
                            </StaggerItem>
                        ))}
                    </Stagger>
                )}
            </section>

            {/* ── Équipes de l'entreprise ── */}
            <section className="flex flex-col gap-3">
                <h2 className="text-sm font-semibold uppercase tracking-wider text-fg-muted">Équipes de l&apos;entreprise</h2>
                <div className="overflow-hidden rounded-xl border border-border bg-surface shadow-sm">
                    <ul className="divide-y divide-border">
                        {teams.map(t => (
                            <li key={t.teamId} className="flex flex-wrap items-center justify-between gap-3 p-4 hover:bg-canvas sm:px-6">
                                <div className="flex min-w-0 items-center gap-3">
                                    <span className="flex h-9 w-9 flex-shrink-0 items-center justify-center rounded-lg text-white" style={{ background: t.color ?? "var(--text-3)" }}>
                                        {renderTeamIcon(t.icon, 16)}
                                    </span>
                                    <div className="min-w-0">
                                        <div className="flex items-center gap-2">
                                            <span className="truncate font-medium text-fg-heading">{t.name}</span>
                                            <span className={`inline-flex items-center gap-1 rounded-full px-2 py-0.5 text-[10px] font-semibold uppercase tracking-wider ${t.isOpen ? "bg-success/10 text-success" : "bg-surface-2 text-fg-muted"}`}>
                                                {t.isOpen ? <Unlock size={9} /> : <Lock size={9} />}{t.isOpen ? "Ouverte" : "Fermée"}
                                            </span>
                                        </div>
                                        {t.description && <div className="truncate text-xs text-fg-muted">{t.description}</div>}
                                        <div className="text-xs text-fg-muted">{capacity(t)}</div>
                                    </div>
                                </div>
                                <div className="flex-shrink-0">
                                    {t.isMember ? (
                                        <span className="rounded-full bg-primary/10 px-2.5 py-1 text-xs font-medium text-primary">Membre ✓</span>
                                    ) : t.isOpen ? (
                                        <button type="button" onClick={() => joinM.mutate(t.teamId)} disabled={t.isFull || joinM.isPending}
                                            className="inline-flex items-center gap-1.5 rounded-lg bg-primary px-3 py-1.5 text-xs font-medium text-white transition-colors duration-200 hover:bg-primary-hover disabled:opacity-50"
                                            title={t.isFull ? "Équipe pleine" : "Rejoindre"}>
                                            <LogIn size={13} /> {t.isFull ? "Pleine" : "Rejoindre"}
                                        </button>
                                    ) : (
                                        <span className="text-xs text-fg-muted">Sur affectation</span>
                                    )}
                                </div>
                            </li>
                        ))}
                        {teams.length === 0 && (
                            <li className="px-6 py-10 text-center text-sm text-fg-muted">Aucune équipe dans votre entreprise pour le moment.</li>
                        )}
                    </ul>
                </div>
            </section>
        </div>
    );
}
