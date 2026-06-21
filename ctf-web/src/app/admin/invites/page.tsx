"use client";

import { useState } from "react";
import QRCode from "react-qr-code";
import {
    QrCode, Plus, Trash2, Copy, Check, Clock, Users, AlertCircle, X,
} from "lucide-react";
import { useInvites, useCreateInvite, useRevokeInvite } from "@/lib/hooks/useInvites";
import type { InviteDto, CreatedInviteDto } from "@/lib/types/invites";

const DURATIONS: { label: string; hours: number }[] = [
    { label: "1 heure", hours: 1 },
    { label: "24 heures", hours: 24 },
    { label: "7 jours", hours: 168 },
    { label: "30 jours", hours: 720 },
];

type Status = { label: string; cls: string };

function inviteStatus(i: InviteDto): Status {
    if (i.isRevoked) return { label: "Révoquée", cls: "bg-danger/10 text-danger" };
    if (i.isExpired) return { label: "Expirée", cls: "bg-[#F1F5F9] text-[#64748B]" };
    if (i.usedCount >= i.maxUses) return { label: "Épuisée", cls: "bg-warning/10 text-warning" };
    return { label: "Active", cls: "bg-success/10 text-success" };
}

function fmt(iso: string): string {
    return new Date(iso).toLocaleString("fr-FR", {
        day: "2-digit", month: "2-digit", year: "numeric", hour: "2-digit", minute: "2-digit",
    });
}

export default function InvitesPage() {
    const listQ = useInvites();
    const createM = useCreateInvite();
    const revokeM = useRevokeInvite();

    const [hours, setHours] = useState(24);
    const [maxUses, setMaxUses] = useState("5");
    const [created, setCreated] = useState<CreatedInviteDto | null>(null);
    const [copied, setCopied] = useState(false);

    async function handleCreate() {
        const uses = Number(maxUses);
        if (!Number.isFinite(uses) || uses < 1) return;
        const res = await createM.mutateAsync({ expiresInHours: hours, maxUses: uses });
        setCreated(res);
        setCopied(false);
    }

    async function copyUrl(url: string) {
        try {
            await navigator.clipboard.writeText(url);
            setCopied(true);
            setTimeout(() => setCopied(false), 2000);
        } catch {
            /* clipboard indisponible — l'URL reste affichée et sélectionnable */
        }
    }

    return (
        <div className="mx-auto flex max-w-5xl flex-col gap-6 px-4 py-6 sm:px-6 sm:py-8">
            <div>
                <h1 className="text-2xl font-bold text-[#F1F5F9]">Invitations par QR code</h1>
                <p className="mt-1 text-sm text-[#94A3B8]">
                    Générez un QR code (ou lien) sécurisé pour qu’un collaborateur rejoigne votre entreprise.
                    Validité limitée dans le temps, nombre d’usages borné, révocable à tout moment.
                </p>
            </div>

            {/* Générateur */}
            <section className="rounded-xl border border-[#E2E8F0] bg-white p-6 shadow-sm">
                <h2 className="flex items-center gap-2 text-sm font-semibold uppercase tracking-wider text-[#475569]">
                    <Plus size={14} /> Générer une invitation
                </h2>
                <div className="mt-4 flex flex-wrap items-end gap-4">
                    <label className="flex flex-col gap-1 text-sm text-[#334155]">
                        <span className="flex items-center gap-1"><Clock size={13} /> Durée de validité</span>
                        <select
                            value={hours}
                            onChange={e => setHours(Number(e.target.value))}
                            className="rounded-lg border border-[#E2E8F0] bg-white px-3 py-2 text-sm text-[#1E293B]"
                        >
                            {DURATIONS.map(d => <option key={d.hours} value={d.hours}>{d.label}</option>)}
                        </select>
                    </label>
                    <label className="flex flex-col gap-1 text-sm text-[#334155]">
                        <span className="flex items-center gap-1"><Users size={13} /> Nombre max d’usages</span>
                        <input
                            type="number" min={1} max={1000} value={maxUses}
                            onChange={e => setMaxUses(e.target.value)}
                            className="w-[120px] rounded-lg border border-[#E2E8F0] bg-white px-3 py-2 text-sm text-[#1E293B]"
                        />
                    </label>
                    <button
                        type="button"
                        onClick={handleCreate}
                        disabled={createM.isPending}
                        className="inline-flex items-center gap-2 rounded-lg bg-primary px-4 py-2 text-sm font-medium text-white transition-colors duration-200 hover:bg-primary-hover disabled:opacity-50"
                    >
                        <QrCode size={14} /> {createM.isPending ? "Génération…" : "Générer le QR"}
                    </button>
                </div>
                {createM.isError && (
                    <div className="mt-3 flex items-center gap-2 text-xs text-danger">
                        <AlertCircle size={13} /> {createM.error.message}
                    </div>
                )}

                {created && (
                    <div className="mt-6 grid grid-cols-1 gap-6 rounded-lg border border-[#E2E8F0] bg-[#F8FAFC] p-6 sm:grid-cols-[auto_1fr]">
                        <div className="flex flex-col items-center gap-2">
                            <div className="rounded-lg bg-white p-3 shadow-sm">
                                <QRCode value={created.joinUrl} size={160} />
                            </div>
                            <span className="text-xs text-[#64748B]">Scannez pour rejoindre</span>
                        </div>
                        <div className="flex flex-col gap-3">
                            <div className="flex items-start justify-between gap-2">
                                <p className="text-sm font-medium text-[#1E293B]">Invitation prête</p>
                                <button type="button" onClick={() => setCreated(null)} title="Fermer"
                                    className="text-[#94A3B8] transition-colors duration-200 hover:text-[#475569]">
                                    <X size={16} />
                                </button>
                            </div>
                            <p className="text-xs text-[#64748B]">
                                Expire le {fmt(created.expiresAt)} · {created.maxUses} usage{created.maxUses > 1 ? "s" : ""} max.
                                Ce lien n’est affiché qu’une seule fois — copiez-le maintenant.
                            </p>
                            <div className="flex items-center gap-2">
                                <input
                                    readOnly value={created.joinUrl}
                                    onFocus={e => e.currentTarget.select()}
                                    className="min-w-0 flex-1 rounded-lg border border-[#E2E8F0] bg-white px-3 py-2 font-mono text-xs text-[#334155]"
                                />
                                <button
                                    type="button" onClick={() => copyUrl(created.joinUrl)}
                                    className="inline-flex shrink-0 items-center gap-1.5 rounded-lg border border-[#E2E8F0] bg-white px-3 py-2 text-xs font-medium text-[#334155] transition-colors duration-200 hover:bg-[#F1F5F9]"
                                >
                                    {copied ? <><Check size={13} className="text-success" /> Copié</> : <><Copy size={13} /> Copier</>}
                                </button>
                            </div>
                        </div>
                    </div>
                )}
            </section>

            {/* Liste */}
            <section className="overflow-hidden rounded-xl border border-[#E2E8F0] bg-white shadow-sm">
                <table className="w-full text-sm">
                    <thead className="bg-[#F1F5F9] text-xs uppercase tracking-wider text-[#475569]">
                        <tr>
                            <th className="px-6 py-3 text-left font-semibold">Statut</th>
                            <th className="px-6 py-3 text-left font-semibold">Usages</th>
                            <th className="px-6 py-3 text-left font-semibold">Expiration</th>
                            <th className="px-6 py-3 text-left font-semibold">Créée le</th>
                            <th className="px-6 py-3 text-right font-semibold">Actions</th>
                        </tr>
                    </thead>
                    <tbody className="divide-y divide-[#E2E8F0]">
                        {(listQ.data ?? []).map(i => {
                            const st = inviteStatus(i);
                            const canRevoke = !i.isRevoked && !i.isExpired;
                            return (
                                <tr key={i.id} className="hover:bg-[#F8FAFC]">
                                    <td className="px-6 py-4">
                                        <span className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ${st.cls}`}>
                                            {st.label}
                                        </span>
                                    </td>
                                    <td className="px-6 py-4 text-[#334155]">{i.usedCount} / {i.maxUses}</td>
                                    <td className="px-6 py-4 text-[#334155]">{fmt(i.expiresAt)}</td>
                                    <td className="px-6 py-4 text-[#64748B]">{fmt(i.createdAt)}</td>
                                    <td className="px-6 py-4 text-right">
                                        <button
                                            type="button"
                                            disabled={!canRevoke || revokeM.isPending}
                                            onClick={() => { if (confirm("Révoquer cette invitation ? Elle ne pourra plus être utilisée.")) revokeM.mutate(i.id); }}
                                            className="inline-flex items-center gap-1.5 rounded-lg border border-[#FCA5A5] bg-white px-3 py-1.5 text-xs font-medium text-danger transition-colors duration-200 hover:bg-[#FEE2E2] disabled:cursor-not-allowed disabled:opacity-40"
                                            title={canRevoke ? "Révoquer" : "Déjà inactive"}
                                        >
                                            <Trash2 size={13} /> Révoquer
                                        </button>
                                    </td>
                                </tr>
                            );
                        })}
                        {listQ.isSuccess && (listQ.data ?? []).length === 0 && (
                            <tr>
                                <td colSpan={5} className="px-6 py-12 text-center text-sm text-[#64748B]">
                                    Aucune invitation. Générez-en une ci-dessus.
                                </td>
                            </tr>
                        )}
                        {listQ.isLoading && (
                            <tr><td colSpan={5} className="px-6 py-12 text-center text-sm text-[#64748B]">Chargement…</td></tr>
                        )}
                    </tbody>
                </table>
            </section>
        </div>
    );
}
