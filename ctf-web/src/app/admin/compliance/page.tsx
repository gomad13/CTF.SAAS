"use client";

import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { CheckCircle2, Trash2, AlertTriangle } from "lucide-react";
import { apiFetch } from "@/lib/api";
import Reveal from "@/components/Reveal";
import { Stagger, StaggerItem } from "@/components/Stagger";
import CountUp from "@/components/CountUp";

type Assignment = {
    id: string;
    pathId: string;
    pathTitle: string;
    assignedToType: string;
    assignedToId: string | null;
    deadline: string;
    completionRatePercent: number;
    usersTargeted: number;
    usersCompleted: number;
    createdAt: string;
};
type Overview = {
    totalAssignments: number;
    overallCompliancePercent: number;
    assignments: Assignment[];
};
type Path = { id: string; title: string };

export default function CompliancePage() {
    const qc = useQueryClient();
    const statusQ = useQuery<{ isEnabled: boolean }>({
        queryKey: ["compliance", "status"],
        queryFn: () => apiFetch("/api/compliance/status"),
    });
    const overviewQ = useQuery<Overview>({
        queryKey: ["compliance", "overview"],
        queryFn: () => apiFetch<Overview>("/api/admin/compliance/overview"),
        enabled: statusQ.data?.isEnabled === true,
    });
    const pathsQ = useQuery<Path[]>({
        queryKey: ["admin", "paths"],
        queryFn: () => apiFetch<Path[]>("/api/admin/paths"),
        enabled: statusQ.data?.isEnabled === true,
    });

    const [pathId, setPathId] = useState("");
    const [deadline, setDeadline] = useState("");

    const createM = useMutation({
        mutationFn: () => apiFetch("/api/admin/compliance/assignments", {
            method: "POST",
            body: JSON.stringify({ pathId, assignedToType: "all_users", assignedToId: null, deadline: new Date(deadline).toISOString() }),
        }),
        onSuccess: () => {
            qc.invalidateQueries({ queryKey: ["compliance", "overview"] });
            setPathId(""); setDeadline("");
        },
    });
    const deleteM = useMutation({
        mutationFn: (id: string) => apiFetch(`/api/admin/compliance/assignments/${id}`, { method: "DELETE" }),
        onSuccess: () => qc.invalidateQueries({ queryKey: ["compliance", "overview"] }),
    });
    const runM = useMutation({
        mutationFn: () => apiFetch("/api/admin/compliance/run-notifications", { method: "POST" }),
        onSuccess: () => qc.invalidateQueries({ queryKey: ["compliance"] }),
    });

    if (!statusQ.data?.isEnabled) {
        return (
            <div className="mx-auto max-w-3xl px-6 py-12 text-center">
                <div className="mx-auto flex h-14 w-14 items-center justify-center rounded-full bg-surface/10 text-fg-muted"><CheckCircle2 size={22} /></div>
                <h1 className="mt-4 text-xl font-bold text-fg-heading">Compliance désactivé</h1>
                <p className="mt-2 text-sm text-fg-muted">Activez le mode « Formation obligatoire » depuis les paramètres.</p>
            </div>
        );
    }

    const o = overviewQ.data;

    return (
        <div className="mx-auto flex max-w-5xl flex-col gap-6 px-4 py-6 sm:px-6 sm:py-8">
            <Reveal>
                <div>
                    <h1 className="text-2xl font-bold text-fg-heading">Compliance</h1>
                    <p className="mt-1 text-sm text-fg-muted">Rendez des parcours obligatoires avec deadline et suivi.</p>
                </div>
            </Reveal>

            <Stagger className="grid grid-cols-1 gap-4 md:grid-cols-3">
                <StaggerItem>
                    <div className="rounded-xl border border-border bg-surface p-6 shadow-sm">
                        <div className="text-xs font-medium uppercase tracking-wider text-fg-muted">Assignations</div>
                        <div className="mt-2 text-2xl font-bold text-fg-heading"><CountUp value={o?.totalAssignments ?? 0} /></div>
                    </div>
                </StaggerItem>
                <StaggerItem>
                    <div className="rounded-xl border border-border bg-surface p-6 shadow-sm">
                        <div className="text-xs font-medium uppercase tracking-wider text-fg-muted">Compliance globale</div>
                        <div className="mt-2 text-2xl font-bold" style={{ color: (o?.overallCompliancePercent ?? 0) >= 80 ? "var(--success)" : (o?.overallCompliancePercent ?? 0) >= 50 ? "var(--warning)" : "var(--danger)" }}>
                            {o?.overallCompliancePercent ?? 0}%
                        </div>
                    </div>
                </StaggerItem>
                <StaggerItem>
                    <div className="rounded-xl border border-border bg-surface p-6 shadow-sm">
                        <button
                            type="button"
                            onClick={() => runM.mutate()}
                            disabled={runM.isPending}
                            className="inline-flex h-full w-full items-center justify-center gap-2 rounded-lg bg-primary px-4 py-2 text-sm font-medium text-white transition-colors duration-200 hover:bg-primary-hover disabled:opacity-60"
                        >
                            <AlertTriangle size={14} />
                            Générer notifications deadline
                        </button>
                    </div>
                </StaggerItem>
            </Stagger>

            <Reveal>
                <section className="rounded-xl border border-border bg-surface p-6 shadow-sm">
                    <h2 className="text-sm font-semibold uppercase tracking-wider text-fg-muted">Nouvelle assignation (tous users)</h2>
                    <div className="mt-4 grid grid-cols-1 gap-3 md:grid-cols-[1fr_220px_auto]">
                        <select value={pathId} onChange={e => setPathId(e.target.value)} className="rounded-lg border border-border bg-surface px-3 py-2 text-sm">
                            <option value="">— Parcours —</option>
                            {(pathsQ.data ?? []).map(p => <option key={p.id} value={p.id}>{p.title}</option>)}
                        </select>
                        <input type="date" value={deadline} onChange={e => setDeadline(e.target.value)} className="rounded-lg border border-border bg-surface px-3 py-2 text-sm" />
                        <button
                            type="button"
                            disabled={!pathId || !deadline || createM.isPending}
                            onClick={() => createM.mutate()}
                            className="rounded-lg bg-primary px-4 py-2 text-sm font-medium text-white transition-colors duration-200 hover:bg-primary-hover disabled:opacity-50"
                        >
                            Créer
                        </button>
                    </div>
                </section>
            </Reveal>

            <Reveal>
                <section className="overflow-hidden rounded-xl border border-border bg-surface shadow-sm">
                    <div className="border-b border-border bg-table-head px-4 py-3 text-xs font-semibold uppercase tracking-wider text-fg-body sm:px-6">
                        Assignations actives
                    </div>
                    <ul className="divide-y divide-border">
                        {(o?.assignments ?? []).map(a => (
                            <li key={a.id} className="flex items-center justify-between gap-3 px-4 py-3 transition-colors hover:bg-surface-2 sm:px-6">
                                <div className="min-w-0">
                                    <div className="truncate text-sm font-medium text-fg-heading">{a.pathTitle}</div>
                                    <div className="mt-0.5 text-xs text-fg-muted">
                                        Deadline : {new Date(a.deadline).toLocaleDateString("fr-FR")} · {a.assignedToType}
                                    </div>
                                </div>
                                <div className="flex items-center gap-4">
                                    <span className="text-sm font-semibold" style={{ color: a.completionRatePercent >= 80 ? "var(--success)" : a.completionRatePercent >= 50 ? "var(--warning)" : "var(--danger)" }}>
                                        {a.completionRatePercent}% ({a.usersCompleted}/{a.usersTargeted})
                                    </span>
                                    <button type="button" onClick={() => { if (confirm("Supprimer cette assignation ?")) deleteM.mutate(a.id); }} className="rounded-md p-1.5 text-danger transition-colors duration-200 hover:bg-danger/10">
                                        <Trash2 size={14} />
                                    </button>
                                </div>
                            </li>
                        ))}
                        {(o?.assignments?.length ?? 0) === 0 && (
                            <li className="px-6 py-8 text-center text-sm text-fg-muted">Aucune assignation obligatoire.</li>
                        )}
                    </ul>
                </section>
            </Reveal>
        </div>
    );
}
