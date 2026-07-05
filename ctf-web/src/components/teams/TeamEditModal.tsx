"use client";

import { useState } from "react";
import { useMutation } from "@tanstack/react-query";
import { Check } from "lucide-react";
import { apiFetch } from "@/lib/api";
import ResponsiveModal from "@/components/ui/ResponsiveModal";
import { TEAM_ICON_NAMES, renderTeamIcon } from "./teamIcons";

export type EditableTeam = {
    id: string;
    name: string;
    description: string | null;
    color: string | null;
    icon: string | null;
    maxMembers?: number | null;
    memberCount?: number;
    isOpen?: boolean;
};

/**
 * Modale d'édition d'équipe (M1) : nom, capacité (nombre max), couleur (color picker),
 * icône (grille d'icônes Lucide). Ouverte via le petit crayon sur chaque carte équipe.
 */
export default function TeamEditModal({
    team, open, onClose, onSaved,
}: {
    team: EditableTeam;
    open: boolean;
    onClose: () => void;
    onSaved: () => void;
}) {
    const [name, setName] = useState(team.name);
    const [description, setDescription] = useState(team.description ?? "");
    const [color, setColor] = useState(team.color ?? "#22C55E");
    const [icon, setIcon] = useState(team.icon ?? "Users");
    const [maxMembers, setMaxMembers] = useState<string>(team.maxMembers != null ? String(team.maxMembers) : "");
    const [isOpen, setIsOpen] = useState<boolean>(team.isOpen ?? false);
    const [error, setError] = useState<string | null>(null);

    const currentMembers = team.memberCount ?? 0;
    const maxNum = maxMembers.trim() === "" ? null : Number(maxMembers);
    const maxInvalid = maxNum != null && (!Number.isInteger(maxNum) || maxNum < 1);
    const maxBelowCurrent = maxNum != null && maxNum < currentMembers;

    const saveM = useMutation({
        mutationFn: () => apiFetch(`/api/admin/teams/${team.id}`, {
            method: "PUT",
            body: JSON.stringify({
                name: name.trim(),
                description: description.trim() || null,
                color,
                icon,
                managerId: null,
                maxMembers: maxNum,
                isOpen,
            }),
        }),
        onSuccess: () => { setError(null); onSaved(); },
        onError: (e: Error) => setError(e.message || "Échec de l'enregistrement."),
    });

    const canSave = name.trim().length >= 1 && !maxInvalid && !maxBelowCurrent && !saveM.isPending;

    return (
        <ResponsiveModal open={open} onClose={onClose} maxWidth={560}
            title={<span className="flex items-center gap-2">{renderTeamIcon(icon, 18)} Modifier l&apos;équipe</span>}
            footer={
                <>
                    <button type="button" onClick={onClose}
                        className="rounded-lg border border-border bg-surface px-4 py-2 text-sm font-medium text-fg-body transition-colors duration-200 hover:bg-surface-2">
                        Annuler
                    </button>
                    <button type="button" disabled={!canSave} onClick={() => saveM.mutate()}
                        className="inline-flex items-center gap-2 rounded-lg bg-primary px-4 py-2 text-sm font-medium text-white transition-colors duration-200 hover:bg-primary-hover disabled:opacity-50">
                        <Check size={14} /> Enregistrer
                    </button>
                </>
            }>
            <div className="flex flex-col gap-4">
                <div className="flex flex-col gap-1">
                    <label className="text-xs font-semibold uppercase tracking-wider text-fg-body">Nom de l&apos;équipe</label>
                    <input value={name} onChange={e => setName(e.target.value)} maxLength={120}
                        className="rounded-lg border border-border bg-surface px-3 py-2 text-sm text-fg-heading outline-none focus:border-primary"
                        placeholder="Nom de l'équipe" />
                </div>

                <div className="flex flex-col gap-1">
                    <label className="text-xs font-semibold uppercase tracking-wider text-fg-body">Description</label>
                    <input value={description} onChange={e => setDescription(e.target.value)} maxLength={500}
                        className="rounded-lg border border-border bg-surface px-3 py-2 text-sm text-fg-heading outline-none focus:border-primary"
                        placeholder="Description (optionnel)" />
                </div>

                <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                    <div className="flex flex-col gap-1">
                        <label className="text-xs font-semibold uppercase tracking-wider text-fg-body">Nombre max de membres</label>
                        <input type="number" min={1} value={maxMembers} onChange={e => setMaxMembers(e.target.value)}
                            className="rounded-lg border border-border bg-surface px-3 py-2 text-sm text-fg-heading outline-none focus:border-primary"
                            placeholder="Illimité" />
                        <span className="text-[11px] text-fg-muted">
                            {currentMembers} membre{currentMembers > 1 ? "s" : ""} actuellement · vide = illimité
                        </span>
                    </div>
                    <div className="flex flex-col gap-1">
                        <label className="text-xs font-semibold uppercase tracking-wider text-fg-body">Couleur</label>
                        <input type="color" value={color} onChange={e => setColor(e.target.value)}
                            className="h-[38px] w-16 cursor-pointer rounded-lg border border-border bg-surface p-1" />
                    </div>
                </div>

                <div className="flex flex-col gap-2">
                    <label className="text-xs font-semibold uppercase tracking-wider text-fg-body">Icône</label>
                    <div className="flex flex-wrap gap-1.5">
                        {TEAM_ICON_NAMES.map(k => (
                            <button key={k} type="button" onClick={() => setIcon(k)} title={k}
                                className={`flex h-9 w-9 items-center justify-center rounded-md border transition-colors duration-200 ${
                                    icon === k ? "border-primary bg-primary text-white" : "border-border bg-surface text-fg-body hover:border-fg-muted"
                                }`}>
                                {renderTeamIcon(k, 16)}
                            </button>
                        ))}
                    </div>
                </div>

                <div className="flex flex-col gap-2">
                    <label className="text-xs font-semibold uppercase tracking-wider text-fg-body">Accès à l&apos;équipe</label>
                    <div className="flex gap-2">
                        <button type="button" onClick={() => setIsOpen(false)}
                            className={`flex-1 rounded-lg border px-3 py-2 text-sm font-medium transition-colors duration-200 ${
                                !isOpen ? "border-primary bg-primary text-white" : "border-border bg-surface text-fg-body hover:bg-surface-2"
                            }`}>
                            🔒 Fermée — affectation par l&apos;admin
                        </button>
                        <button type="button" onClick={() => setIsOpen(true)}
                            className={`flex-1 rounded-lg border px-3 py-2 text-sm font-medium transition-colors duration-200 ${
                                isOpen ? "border-primary bg-primary text-white" : "border-border bg-surface text-fg-body hover:bg-surface-2"
                            }`}>
                            🔓 Ouverte — les membres peuvent la rejoindre
                        </button>
                    </div>
                </div>

                {maxBelowCurrent && (
                    <p className="text-xs text-danger">
                        Le nombre max ({maxNum}) ne peut pas être inférieur au nombre actuel de membres ({currentMembers}).
                    </p>
                )}
                {maxInvalid && <p className="text-xs text-danger">Le nombre max doit être un entier ≥ 1.</p>}
                {error && <p className="text-xs text-danger">{error}</p>}
            </div>
        </ResponsiveModal>
    );
}
