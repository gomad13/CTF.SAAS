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
        return <div className="px-6 py-12 text-center text-[#94A3B8]">Chargement…</div>;
    }

    if (!enabled) {
        return (
            <div className="mx-auto max-w-3xl px-6 py-12 text-center">
                <div className="mx-auto flex h-14 w-14 items-center justify-center rounded-full bg-white/10 text-[#94A3B8]">
                    <Target size={22} />
                </div>
                <h1 className="mt-4 text-xl font-bold text-[#F1F5F9]">Campagnes désactivées</h1>
                <p className="mt-2 text-sm text-[#94A3B8]">
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
            <div>
                <h1 className="text-2xl font-bold text-[#F1F5F9]">Campagnes</h1>
                <p className="mt-1 text-sm text-[#94A3B8]">
                    Programmes de sensibilisation time-boxed qui combinent parcours et scénarios.
                </p>
            </div>

            {/* ── Formulaire ───────────────────────────────────────────────── */}
            <section className="rounded-xl border border-[#E2E8F0] bg-white p-6 shadow-sm text-[#1E293B]">
                <h2 className="text-sm font-semibold uppercase tracking-wider text-[#475569]">Nouvelle campagne</h2>
                <div className="mt-4 grid grid-cols-1 gap-3 md:grid-cols-2">
                    <div>
                        <label className="text-xs font-medium text-[#334155]">Nom</label>
                        <input
                            className="mt-1 w-full rounded-lg border border-[#E2E8F0] bg-white px-3 py-2 text-sm text-[#0F172A] placeholder:text-[#64748B]"
                            placeholder="Sensibilisation T2 2026"
                            value={name}
                            onChange={e => setName(e.target.value)}
                            maxLength={200}
                        />
                    </div>
                    <div>
                        <label className="text-xs font-medium text-[#334155]">Description</label>
                        <input
                            className="mt-1 w-full rounded-lg border border-[#E2E8F0] bg-white px-3 py-2 text-sm text-[#0F172A] placeholder:text-[#64748B]"
                            placeholder="Optionnel — contexte / objectifs"
                            value={description}
                            onChange={e => setDescription(e.target.value)}
                            maxLength={2000}
                        />
                    </div>
                    <div>
                        <label className="text-xs font-medium text-[#334155]">Date de début</label>
                        <input
                            type="date"
                            className="mt-1 w-full rounded-lg border border-[#E2E8F0] bg-white px-3 py-2 text-sm text-[#0F172A]"
                            value={startDate}
                            onChange={e => setStartDate(e.target.value)}
                        />
                    </div>
                    <div>
                        <label className="text-xs font-medium text-[#334155]">Date de fin</label>
                        <input
                            type="date"
                            className="mt-1 w-full rounded-lg border border-[#E2E8F0] bg-white px-3 py-2 text-sm text-[#0F172A]"
                            value={endDate}
                            onChange={e => setEndDate(e.target.value)}
                        />
                    </div>
                </div>

                {/* Contenus inclus — parcours + scénarios */}
                <div className="mt-4">
                    <label className="text-xs font-medium text-[#334155]">Parcours inclus</label>
                    <div className="mt-2 flex flex-wrap gap-2">
                        {paths.length === 0 && <span className="text-xs text-[#64748B]">Aucun parcours disponible.</span>}
                        {paths.map(p => {
                            const active = selectedContents.some(c => c.contentId === p.contentId);
                            return (
                                <button
                                    key={p.contentId}
                                    type="button"
                                    onClick={() => toggleContent({ contentType: "Parcours", contentId: p.contentId })}
                                    className={`inline-flex items-center gap-1.5 rounded-full border px-3 py-1 text-xs transition-colors duration-200 ${
                                        active
                                            ? "border-[#3B82F6] bg-[#3B82F6] text-white"
                                            : "border-[#E2E8F0] bg-white text-[#334155] hover:bg-[#F1F5F9]"
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
                    <label className="text-xs font-medium text-[#334155]">Scénarios inclus</label>
                    <div className="mt-2 flex flex-wrap gap-2">
                        {scenarios.length === 0 && <span className="text-xs text-[#64748B]">Aucun scénario disponible.</span>}
                        {scenarios.map(s => {
                            const active = selectedContents.some(c => c.contentId === s.contentId);
                            return (
                                <button
                                    key={s.contentId}
                                    type="button"
                                    onClick={() => toggleContent({ contentType: "Scenario", contentId: s.contentId })}
                                    className={`inline-flex items-center gap-1.5 rounded-full border px-3 py-1 text-xs transition-colors duration-200 ${
                                        active
                                            ? "border-[#10B981] bg-[#10B981] text-white"
                                            : "border-[#E2E8F0] bg-white text-[#334155] hover:bg-[#F1F5F9]"
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
                <div className="mt-4 rounded-lg border border-[#E2E8F0] bg-[#F8FAFC] p-3 text-[#1E293B]">
                    <label className="flex cursor-pointer items-center gap-2 text-sm">
                        <input
                            type="checkbox"
                            checked={assignToWholeTenant}
                            onChange={e => setAssignToWholeTenant(e.target.checked)}
                        />
                        <span className="font-medium">Assigner à toute l&apos;entreprise</span>
                    </label>
                    <p className="mt-1 text-xs text-[#64748B]">
                        {assignToWholeTenant
                            ? "Tous les employés actifs du tenant seront automatiquement assignés à la création."
                            : "Vous pourrez assigner des employés spécifiques depuis la page détail."}
                    </p>
                </div>

                {!validation.ok && (name || startDate || endDate || selectedContents.length > 0) && (
                    <p className="mt-3 text-xs text-[#B91C1C]">{validation.msg}</p>
                )}

                <button
                    type="button"
                    disabled={!validation.ok || createM.isPending}
                    onClick={handleCreate}
                    className="mt-4 inline-flex items-center gap-2 rounded-lg bg-[#3B82F6] px-4 py-2 text-sm font-medium text-white transition-colors duration-200 hover:bg-[#2563EB] disabled:cursor-not-allowed disabled:opacity-50"
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
                                ? "bg-[#3B82F6] text-white"
                                : "bg-white text-[#475569] hover:bg-[#F1F5F9]"
                        }`}
                    >
                        {f === "All" ? "Toutes" : STATUS_STYLES[f].label}
                    </button>
                ))}
            </div>

            {/* ── Liste ────────────────────────────────────────────────────── */}
            <section className="overflow-hidden rounded-xl border border-[#E2E8F0] bg-white shadow-sm text-[#1E293B]">
                <div className="border-b border-[#E2E8F0] bg-[#F1F5F9] px-4 py-3 text-xs font-semibold uppercase tracking-wider text-[#475569] sm:px-6">
                    Campagnes ({list.length})
                </div>
                <ul className="divide-y divide-[#E2E8F0]">
                    {list.map(c => {
                        const s = STATUS_STYLES[c.status] ?? STATUS_STYLES.Upcoming;
                        const pct = Math.max(0, Math.min(100, Math.round(c.globalCompletion)));
                        return (
                            <li key={c.id} className="px-4 py-4 hover:bg-[#F8FAFC] sm:px-6">
                                <div className="flex items-start justify-between gap-3">
                                    <div className="min-w-0 flex-1">
                                        <div className="flex items-center gap-2">
                                            <Link
                                                href={`/admin/campaigns/${c.id}`}
                                                className="truncate text-sm font-semibold text-[#1E293B] hover:text-[#2563EB]"
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
                                            <div className="mt-0.5 truncate text-xs text-[#475569]">{c.description}</div>
                                        )}
                                        <div className="mt-1 flex flex-wrap items-center gap-x-4 gap-y-1 text-xs text-[#64748B]">
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
                                            <div className="h-1.5 flex-1 overflow-hidden rounded-full bg-[#E2E8F0]">
                                                <div
                                                    className="h-full rounded-full bg-[#3B82F6] transition-all"
                                                    style={{ width: `${pct}%` }}
                                                />
                                            </div>
                                            <span className="text-xs font-medium text-[#475569]">{pct}%</span>
                                        </div>
                                    </div>
                                    <button
                                        type="button"
                                        onClick={() => handleDelete(c.id, c.name)}
                                        className="rounded-md p-1.5 text-[#EF4444] transition-colors duration-200 hover:bg-[#EF4444]/10"
                                        title={c.status === "Upcoming" ? "Supprimer" : "Archiver"}
                                    >
                                        <Trash2 size={14} />
                                    </button>
                                </div>
                            </li>
                        );
                    })}
                    {list.length === 0 && (
                        <li className="px-6 py-12 text-center text-sm text-[#64748B]">
                            Aucune campagne pour l&apos;instant.
                        </li>
                    )}
                </ul>
            </section>
        </div>
    );
}
