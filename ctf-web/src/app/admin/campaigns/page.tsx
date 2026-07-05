"use client";

import { useMemo, useState } from "react";
import Link from "next/link";
import { Target, Plus, Trash2, FolderOpen, Mail, Calendar, Users } from "lucide-react";
import { toast } from "@/components/Toast";
import {
    useAvailableContent,
    useCampaigns,
    useCampaignsStatus,
    useCreateCampaign,
    useDeleteCampaign,
} from "@/lib/hooks/useCampaigns";
import {
    STATUS_STYLES,
    type CampaignContentType,
    type CampaignStatus,
} from "@/lib/types/campaigns";
import Reveal from "@/components/Reveal";
import { Stagger, StaggerItem } from "@/components/Stagger";

type StatusFilter = "All" | CampaignStatus;

export default function CampaignsPage() {
    const statusQ = useCampaignsStatus();
    const enabled = statusQ.data?.isEnabled === true;

    const [filter, setFilter] = useState<StatusFilter>("All");
    const listQ = useCampaigns({ status: filter });
    const contentsQ = useAvailableContent();

    const [name, setName] = useState("");
    const [description, setDescription] = useState("");
    const [startDate, setStartDate] = useState("");
    const [endDate, setEndDate] = useState("");
    const [selectedContents, setSelectedContents] = useState<{ contentType: CampaignContentType; contentId: string }[]>([]);
    const [assignToWholeTenant, setAssignToWholeTenant] = useState(true);

    const createM = useCreateCampaign();
    const deleteM = useDeleteCampaign();

    const validation = useMemo(() => {
        if (!name.trim()) return { ok: false, msg: "Nom requis." };
        if (!startDate || !endDate) return { ok: false, msg: "Dates requises." };
        const start = new Date(startDate);
        const end = new Date(endDate);
        if (Number.isNaN(start.getTime()) || Number.isNaN(end.getTime())) return { ok: false, msg: "Dates invalides." };
        if (end <= start) return { ok: false, msg: "La date de fin doit être après la date de début." };
        if (selectedContents.length === 0) return { ok: false, msg: "Au moins un contenu requis." };
        return { ok: true, msg: "" };
    }, [name, startDate, endDate, selectedContents]);

    function toggleContent(item: { contentType: CampaignContentType; contentId: string }) {
        setSelectedContents(prev => {
            const exists = prev.some(p => p.contentId === item.contentId && p.contentType === item.contentType);
            return exists
                ? prev.filter(p => !(p.contentId === item.contentId && p.contentType === item.contentType))
                : [...prev, item];
        });
    }

    function reset() {
        setName(""); setDescription(""); setStartDate(""); setEndDate("");
        setSelectedContents([]); setAssignToWholeTenant(true);
    }

    async function handleCreate() {
        if (!validation.ok) return;
        try {
            await createM.mutateAsync({
                name: name.trim(),
                description: description.trim() || null,
                startDate: new Date(startDate).toISOString(),
                endDate: new Date(endDate).toISOString(),
                contents: selectedContents.map((c, idx) => ({ ...c, displayOrder: idx })),
                assignToWholeTenant,
                assignedUserIds: null,
            });
            toast.ok("Campagne créée");
            reset();
        } catch (e) {
            toast.er(e instanceof Error ? e.message : "Erreur création");
        }
    }

    async function handleDelete(id: string, label: string) {
        if (!confirm(`Supprimer la campagne « ${label} » ?`)) return;
        try {
            await deleteM.mutateAsync(id);
            toast.ok("Campagne supprimée");
        } catch (e) {
            toast.er(e instanceof Error ? e.message : "Erreur suppression");
        }
    }

    if (statusQ.isLoading) {
        return <div className="px-6 py-12 text-center text-fg-muted">Chargement…</div>;
    }

    if (!enabled) {
        return (
            <div className="mx-auto max-w-3xl px-6 py-12 text-center">
                <div className="mx-auto flex h-14 w-14 items-center justify-center rounded-full bg-surface/10 text-fg-muted">
                    <Target size={22} />
                </div>
                <h1 className="mt-4 text-xl font-bold text-fg-heading">Campagnes désactivées</h1>
                <p className="mt-2 text-sm text-fg-muted">
                    Activez le mode « Campagnes » depuis les paramètres pour orchestrer parcours
                    et scénarios sur une période donnée.
                </p>
            </div>
        );
    }

    const list = listQ.data ?? [];
    const contents = contentsQ.data ?? [];
    const paths = contents.filter(c => c.contentType === "Parcours");
    const scenarios = contents.filter(c => c.contentType === "Scenario");

    return (
        <div className="mx-auto flex max-w-6xl flex-col gap-6 px-4 py-6 sm:px-6 sm:py-8">
            <Reveal>
                <div>
                    <h1 className="text-2xl font-bold text-fg-heading">Campagnes</h1>
                    <p className="mt-1 text-sm text-fg-muted">
                        Programmes de sensibilisation time-boxed qui combinent parcours et scénarios.
                    </p>
                </div>
            </Reveal>

            {/* ── Formulaire ───────────────────────────────────────────────── */}
            <section className="rounded-xl border border-border bg-surface p-6 shadow-sm text-fg-heading">
                <h2 className="text-sm font-semibold uppercase tracking-wider text-fg-body">Nouvelle campagne</h2>
                <div className="mt-4 grid grid-cols-1 gap-3 md:grid-cols-2">
                    <div>
                        <label className="text-xs font-medium text-fg-body">Nom</label>
                        <input
                            className="mt-1 w-full rounded-lg border border-border bg-surface px-3 py-2 text-sm text-fg-heading placeholder:text-fg-muted"
                            placeholder="Sensibilisation T2 2026"
                            value={name}
                            onChange={e => setName(e.target.value)}
                            maxLength={200}
                        />
                    </div>
                    <div>
                        <label className="text-xs font-medium text-fg-body">Description</label>
                        <input
                            className="mt-1 w-full rounded-lg border border-border bg-surface px-3 py-2 text-sm text-fg-heading placeholder:text-fg-muted"
                            placeholder="Optionnel — contexte / objectifs"
                            value={description}
                            onChange={e => setDescription(e.target.value)}
                            maxLength={2000}
                        />
                    </div>
                    <div>
                        <label className="text-xs font-medium text-fg-body">Date de début</label>
                        <input
                            type="date"
                            className="mt-1 w-full rounded-lg border border-border bg-surface px-3 py-2 text-sm text-fg-heading"
                            value={startDate}
                            onChange={e => setStartDate(e.target.value)}
                        />
                    </div>
                    <div>
                        <label className="text-xs font-medium text-fg-body">Date de fin</label>
                        <input
                            type="date"
                            className="mt-1 w-full rounded-lg border border-border bg-surface px-3 py-2 text-sm text-fg-heading"
                            value={endDate}
                            onChange={e => setEndDate(e.target.value)}
                        />
                    </div>
                </div>

                {/* Contenus inclus — parcours + scénarios */}
                <div className="mt-4">
                    <label className="text-xs font-medium text-fg-body">Parcours inclus</label>
                    <div className="mt-2 flex flex-wrap gap-2">
                        {paths.length === 0 && <span className="text-xs text-fg-muted">Aucun parcours disponible.</span>}
                        {paths.map(p => {
                            const active = selectedContents.some(c => c.contentId === p.contentId);
                            return (
                                <button
                                    key={p.contentId}
                                    type="button"
                                    onClick={() => toggleContent({ contentType: "Parcours", contentId: p.contentId })}
                                    className={`inline-flex items-center gap-1.5 rounded-full border px-3 py-1 text-xs transition-colors duration-200 ${
                                        active
                                            ? "border-primary bg-primary text-[var(--on-accent)]"
                                            : "border-border bg-surface text-fg-body hover:bg-surface-2"
                                    }`}
                                >
                                    <FolderOpen size={12} />
                                    {p.title}
                                </button>
                            );
                        })}
                    </div>
                </div>

                <div className="mt-3">
                    <label className="text-xs font-medium text-fg-body">Scénarios inclus</label>
                    <div className="mt-2 flex flex-wrap gap-2">
                        {scenarios.length === 0 && <span className="text-xs text-fg-muted">Aucun scénario disponible.</span>}
                        {scenarios.map(s => {
                            const active = selectedContents.some(c => c.contentId === s.contentId);
                            return (
                                <button
                                    key={s.contentId}
                                    type="button"
                                    onClick={() => toggleContent({ contentType: "Scenario", contentId: s.contentId })}
                                    className={`inline-flex items-center gap-1.5 rounded-full border px-3 py-1 text-xs transition-colors duration-200 ${
                                        active
                                            ? "border-success bg-success text-[var(--on-accent)]"
                                            : "border-border bg-surface text-fg-body hover:bg-surface-2"
                                    }`}
                                >
                                    <Mail size={12} />
                                    {s.title}
                                </button>
                            );
                        })}
                    </div>
                </div>

                {/* Cible : toute l'entreprise ou assignation différée */}
                <div className="mt-4 rounded-lg border border-border bg-surface-2 p-3 text-fg-heading">
                    <label className="flex cursor-pointer items-center gap-2 text-sm">
                        <input
                            type="checkbox"
                            checked={assignToWholeTenant}
                            onChange={e => setAssignToWholeTenant(e.target.checked)}
                        />
                        <span className="font-medium">Assigner à toute l&apos;entreprise</span>
                    </label>
                    <p className="mt-1 text-xs text-fg-muted">
                        {assignToWholeTenant
                            ? "Tous les employés actifs du tenant seront automatiquement assignés à la création."
                            : "Vous pourrez assigner des employés spécifiques depuis la page détail."}
                    </p>
                </div>

                {!validation.ok && (name || startDate || endDate || selectedContents.length > 0) && (
                    <p className="mt-3 text-xs text-danger">{validation.msg}</p>
                )}

                <button
                    type="button"
                    disabled={!validation.ok || createM.isPending}
                    onClick={handleCreate}
                    className="mt-4 inline-flex items-center gap-2 rounded-lg bg-primary px-4 py-2 text-sm font-medium text-[var(--on-accent)] transition-colors duration-200 hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-50"
                >
                    <Plus size={14} />
                    {createM.isPending ? "Création…" : "Créer la campagne"}
                </button>
            </section>

            {/* ── Filtres ──────────────────────────────────────────────────── */}
            <div className="flex flex-wrap items-center gap-2">
                {(["All", "Upcoming", "Active", "Completed"] as StatusFilter[]).map(f => (
                    <button
                        key={f}
                        type="button"
                        onClick={() => setFilter(f)}
                        className={`rounded-full px-3 py-1 text-xs font-medium transition-colors duration-200 ${
                            filter === f
                                ? "bg-primary text-[var(--on-accent)]"
                                : "bg-surface text-fg-body hover:bg-surface-2"
                        }`}
                    >
                        {f === "All" ? "Toutes" : STATUS_STYLES[f].label}
                    </button>
                ))}
            </div>

            {/* ── Liste ────────────────────────────────────────────────────── */}
            <section className="overflow-hidden rounded-xl border border-border bg-surface shadow-sm text-fg-heading">
                <div className="border-b border-border bg-table-head px-4 py-3 text-xs font-semibold uppercase tracking-wider text-fg-body sm:px-6">
                    Campagnes ({list.length})
                </div>
                <Stagger className="divide-y divide-border" gap={0.04}>
                    {list.map(c => {
                        const s = STATUS_STYLES[c.status] ?? STATUS_STYLES.Upcoming;
                        const pct = Math.max(0, Math.min(100, Math.round(c.globalCompletion)));
                        return (
                            <StaggerItem key={c.id} className="px-4 py-4 transition-colors hover:bg-surface-2 sm:px-6">
                                <div className="flex items-start justify-between gap-3">
                                    <div className="min-w-0 flex-1">
                                        <div className="flex items-center gap-2">
                                            <Link
                                                href={`/admin/campaigns/${c.id}`}
                                                className="truncate text-sm font-semibold text-fg-heading hover:text-primary"
                                            >
                                                {c.name}
                                            </Link>
                                            <span
                                                className="rounded-full px-2 py-0.5 text-[10px] font-semibold uppercase"
                                                style={{ background: s.bg, color: s.color, border: `1px solid ${s.border}` }}
                                            >
                                                {s.label}
                                            </span>
                                        </div>
                                        {c.description && (
                                            <div className="mt-0.5 truncate text-xs text-fg-body">{c.description}</div>
                                        )}
                                        <div className="mt-1 flex flex-wrap items-center gap-x-4 gap-y-1 text-xs text-fg-muted">
                                            <span className="inline-flex items-center gap-1">
                                                <Calendar size={11} />
                                                {new Date(c.startDate).toLocaleDateString("fr-FR")} → {new Date(c.endDate).toLocaleDateString("fr-FR")}
                                            </span>
                                            <span>{c.contentCount} contenu{c.contentCount > 1 ? "s" : ""}</span>
                                            <span className="inline-flex items-center gap-1">
                                                <Users size={11} />
                                                {c.assignedCount} assigné{c.assignedCount > 1 ? "s" : ""}
                                            </span>
                                        </div>
                                        <div className="mt-2 flex items-center gap-2">
                                            <div className="h-1.5 flex-1 overflow-hidden rounded-full bg-surface-2">
                                                <div
                                                    className="h-full rounded-full bg-primary transition-all"
                                                    style={{ width: `${pct}%` }}
                                                />
                                            </div>
                                            <span className="text-xs font-medium text-fg-body">{pct}%</span>
                                        </div>
                                    </div>
                                    <button
                                        type="button"
                                        onClick={() => handleDelete(c.id, c.name)}
                                        className="rounded-md p-1.5 text-danger transition-colors duration-200 hover:bg-danger/10"
                                        title={c.status === "Upcoming" ? "Supprimer" : "Archiver"}
                                    >
                                        <Trash2 size={14} />
                                    </button>
                                </div>
                            </StaggerItem>
                        );
                    })}
                    {list.length === 0 && (
                        <div className="px-6 py-12 text-center text-sm text-fg-muted">
                            Aucune campagne pour l&apos;instant.
                        </div>
                    )}
                </Stagger>
            </section>
        </div>
    );
}
