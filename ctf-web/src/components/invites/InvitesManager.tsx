"use client";

import { useRef, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import QRCode from "react-qr-code";
import {
    QrCode, Plus, Trash2, Copy, Check, Clock, Users, AlertCircle, X, Download, Building2, AppWindow, UserPlus,
} from "lucide-react";
import { apiFetch } from "@/lib/api";
import { useInvites, useCreateInvite, useRevokeInvite } from "@/lib/hooks/useInvites";
import type { InviteDto, CreatedInviteDto, InviteType } from "@/lib/types/invites";

const DURATIONS: { label: string; hours: number }[] = [
    { label: "1 heure", hours: 1 },
    { label: "24 heures", hours: 24 },
    { label: "7 jours", hours: 168 },
    { label: "30 jours", hours: 720 },
];

// Métadonnées des 3 types de QR (libellés clairs pour l'admin).
const TYPE_META: Record<InviteType, { short: string; label: string; help: string; cls: string }> = {
    app: {
        short: "Application",
        label: "Application — inscription générale",
        help: "Invite à créer un compte Sentys, SANS rattachement à une entreprise. Mène à la page d'inscription générale.",
        cls: "bg-info/10 text-info",
    },
    enterprise_signup: {
        short: "Inscription",
        label: "Entreprise — inscription (nouveau compte)",
        help: "Une personne SANS compte s'inscrit avec l'entreprise pré-remplie et verrouillée, puis est rattachée automatiquement (rôle Membre).",
        cls: "bg-success/10 text-success",
    },
    enterprise_join: {
        short: "Rejoindre",
        label: "Entreprise — rejoindre (compte existant)",
        help: "Une personne qui a DÉJÀ un compte scanne le QR et rejoint l'entreprise (rôle Membre). Ses autres sociétés sont conservées.",
        cls: "bg-primary/10 text-primary",
    },
};

const TYPE_ICON: Record<InviteType, typeof Building2> = {
    app: AppWindow,
    enterprise_signup: UserPlus,
    enterprise_join: Building2,
};

type Status = { label: string; cls: string };

function inviteStatus(i: InviteDto): Status {
    if (i.isRevoked) return { label: "Révoquée", cls: "bg-danger/10 text-danger" };
    if (i.isExpired) return { label: "Expirée", cls: "bg-surface-2 text-fg-muted" };
    if (i.usedCount >= i.maxUses) return { label: "Épuisée", cls: "bg-warning/10 text-warning" };
    return { label: "Active", cls: "bg-success/10 text-success" };
}

function fmt(iso: string): string {
    return new Date(iso).toLocaleString("fr-FR", {
        day: "2-digit", month: "2-digit", year: "numeric", hour: "2-digit", minute: "2-digit",
    });
}

/**
 * Gestion des invitations QR en 3 types (voir QR_3_TYPES) :
 *  - Application (SuperAdmin) : inscription générale, sans entreprise.
 *  - Entreprise-Inscription (Type 2) et Entreprise-Rejoindre (Type 3) : scellées au tenant.
 * Génération + QR + téléchargement PNG + URL copiable + liste typée + révocation.
 * Réutilisé par les Paramètres entreprise (/admin/entreprise). Zéro duplication d'appels API.
 */
export default function InvitesManager() {
    const listQ = useInvites();
    const createM = useCreateInvite();
    const revokeM = useRevokeInvite();
    const meQ = useQuery({ queryKey: ["me"], queryFn: () => apiFetch<{ role: string }>("/api/auth/me"), staleTime: 60_000 });
    const isSuperAdmin = meQ.data?.role === "SuperAdmin";

    // Types générables selon le rôle : App réservé au SuperAdmin.
    const availableTypes: InviteType[] = isSuperAdmin
        ? ["app", "enterprise_signup", "enterprise_join"]
        : ["enterprise_signup", "enterprise_join"];

    const [type, setType] = useState<InviteType>("enterprise_signup");
    const [hours, setHours] = useState(24);
    const [maxUses, setMaxUses] = useState("5");
    const [created, setCreated] = useState<CreatedInviteDto | null>(null);
    const [copied, setCopied] = useState(false);
    const qrRef = useRef<HTMLDivElement>(null);

    async function handleCreate() {
        const uses = Number(maxUses);
        if (!Number.isFinite(uses) || uses < 1) return;
        const res = await createM.mutateAsync({ expiresInHours: hours, maxUses: uses, type });
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

    // Sérialise le SVG du QR, le dessine sur un canvas HD et télécharge un PNG.
    function downloadQrPng() {
        const svg = qrRef.current?.querySelector("svg");
        if (!svg) return;
        const size = 512;
        const xml = new XMLSerializer().serializeToString(svg);
        const svgUrl = "data:image/svg+xml;base64," + window.btoa(unescape(encodeURIComponent(xml)));
        const img = new Image();
        img.onload = () => {
            const canvas = document.createElement("canvas");
            canvas.width = size;
            canvas.height = size;
            const ctx = canvas.getContext("2d");
            if (!ctx) return;
            ctx.fillStyle = "#ffffff";
            ctx.fillRect(0, 0, size, size);
            ctx.drawImage(img, 0, 0, size, size);
            const a = document.createElement("a");
            a.href = canvas.toDataURL("image/png");
            a.download = `invitation-sentys-${created?.type ?? "qr"}.png`;
            a.click();
        };
        img.src = svgUrl;
    }

    const isEnterprise = type !== "app";

    return (
        <div className="flex flex-col gap-6">
            {/* Générateur */}
            <section className="rounded-xl border border-border bg-surface p-6 shadow-sm">
                <h2 className="flex items-center gap-2 text-sm font-semibold uppercase tracking-wider text-fg-body">
                    <Plus size={14} /> Générer une invitation QR
                </h2>

                {/* Sélecteur de type (cartes cliquables) */}
                <div className="mt-4 grid grid-cols-1 gap-3 sm:grid-cols-3">
                    {availableTypes.map(t => {
                        const meta = TYPE_META[t];
                        const Icon = TYPE_ICON[t];
                        const active = type === t;
                        return (
                            <button
                                key={t} type="button" onClick={() => setType(t)}
                                className={`flex flex-col gap-1.5 rounded-lg border p-4 text-left transition-colors duration-200 ${active ? "border-primary bg-primary/10" : "border-border bg-surface hover:bg-surface-2"}`}
                            >
                                <span className="flex items-center gap-2 text-sm font-semibold text-fg-heading">
                                    <Icon size={15} className={active ? "text-primary" : "text-fg-muted"} /> {meta.short}
                                </span>
                                <span className="text-xs leading-snug text-fg-muted">{meta.help}</span>
                            </button>
                        );
                    })}
                </div>

                <div className="mt-4 flex flex-wrap items-end gap-4">
                    <label className="flex flex-col gap-1 text-sm text-fg-body">
                        <span className="flex items-center gap-1"><Clock size={13} /> Durée de validité</span>
                        <select
                            value={hours}
                            onChange={e => setHours(Number(e.target.value))}
                            className="rounded-lg border border-border bg-surface px-3 py-2 text-sm text-fg-heading"
                        >
                            {DURATIONS.map(d => <option key={d.hours} value={d.hours}>{d.label}</option>)}
                        </select>
                    </label>
                    <label className="flex flex-col gap-1 text-sm text-fg-body">
                        <span className="flex items-center gap-1"><Users size={13} /> Nombre max d’usages</span>
                        <input
                            type="number" min={1} max={1000} value={maxUses}
                            onChange={e => setMaxUses(e.target.value)}
                            className="w-[120px] rounded-lg border border-border bg-surface px-3 py-2 text-sm text-fg-heading"
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
                <p className="mt-2 text-xs text-fg-muted">{isEnterprise
                    ? "Ce QR est scellé à votre société active : il ne fait rejoindre que celle-ci."
                    : "Ce QR mène à l'inscription générale Sentys, sans rattachement à une entreprise."}</p>
                {createM.isError && (
                    <div className="mt-3 flex items-center gap-2 text-xs text-danger">
                        <AlertCircle size={13} /> {createM.error.message}
                    </div>
                )}

                {created && (
                    <div className="mt-6 grid grid-cols-1 gap-6 rounded-lg border border-border bg-surface-2 p-6 sm:grid-cols-[auto_1fr]">
                        <div className="flex flex-col items-center gap-2">
                            <div ref={qrRef} className="rounded-lg bg-surface p-3 shadow-sm">
                                <QRCode value={created.joinUrl} size={160} />
                            </div>
                            <button
                                type="button" onClick={downloadQrPng}
                                className="inline-flex items-center gap-1.5 text-xs font-medium text-primary transition-colors duration-200 hover:text-primary-hover"
                            >
                                <Download size={13} /> Télécharger le QR
                            </button>
                            <span className="text-xs text-fg-muted">{created.type === "app" ? "Scannez pour vous inscrire" : "Scannez pour rejoindre"}</span>
                        </div>
                        <div className="flex flex-col gap-3">
                            <div className="flex items-start justify-between gap-2">
                                <span className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ${TYPE_META[created.type].cls}`}>
                                    {TYPE_META[created.type].label}
                                </span>
                                <button type="button" onClick={() => setCreated(null)} title="Fermer"
                                    className="text-fg-muted transition-colors duration-200 hover:text-fg-body">
                                    <X size={16} />
                                </button>
                            </div>
                            <p className="text-xs text-fg-muted">
                                Expire le {fmt(created.expiresAt)} · {created.maxUses} usage{created.maxUses > 1 ? "s" : ""} max.
                                Ce lien n’est affiché qu’une seule fois — copiez-le maintenant.
                            </p>
                            <div className="flex items-center gap-2">
                                <input
                                    readOnly value={created.joinUrl}
                                    onFocus={e => e.currentTarget.select()}
                                    className="min-w-0 flex-1 rounded-lg border border-border bg-surface px-3 py-2 font-mono text-xs text-fg-body"
                                />
                                <button
                                    type="button" onClick={() => copyUrl(created.joinUrl)}
                                    className="inline-flex shrink-0 items-center gap-1.5 rounded-lg border border-border bg-surface px-3 py-2 text-xs font-medium text-fg-body transition-colors duration-200 hover:bg-surface-2"
                                >
                                    {copied ? <><Check size={13} className="text-success" /> Copié</> : <><Copy size={13} /> Copier</>}
                                </button>
                            </div>
                        </div>
                    </div>
                )}
            </section>

            {/* Liste */}
            <section className="overflow-hidden rounded-xl border border-border bg-surface shadow-sm">
                <div className="overflow-x-auto">
                <table className="w-full text-sm">
                    <thead className="bg-table-head text-xs uppercase tracking-wider text-fg-body">
                        <tr>
                            <th className="px-6 py-3 text-left font-semibold">Type</th>
                            <th className="px-6 py-3 text-left font-semibold">Statut</th>
                            <th className="px-6 py-3 text-left font-semibold">Usages</th>
                            <th className="px-6 py-3 text-left font-semibold">Expiration</th>
                            <th className="px-6 py-3 text-left font-semibold">Créée le</th>
                            <th className="px-6 py-3 text-right font-semibold">Actions</th>
                        </tr>
                    </thead>
                    <tbody className="divide-y divide-border">
                        {(listQ.data ?? []).map(i => {
                            const st = inviteStatus(i);
                            const tm = TYPE_META[i.type];
                            const canRevoke = !i.isRevoked && !i.isExpired;
                            return (
                                <tr key={i.id} className="hover:bg-surface-2">
                                    <td className="px-6 py-4">
                                        <span className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ${tm.cls}`}>
                                            {tm.short}
                                        </span>
                                        {i.tenantName && <span className="ml-2 text-xs text-fg-muted">{i.tenantName}</span>}
                                    </td>
                                    <td className="px-6 py-4">
                                        <span className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ${st.cls}`}>
                                            {st.label}
                                        </span>
                                    </td>
                                    <td className="px-6 py-4 text-fg-body">{i.usedCount} / {i.maxUses}</td>
                                    <td className="px-6 py-4 text-fg-body">{fmt(i.expiresAt)}</td>
                                    <td className="px-6 py-4 text-fg-muted">{fmt(i.createdAt)}</td>
                                    <td className="px-6 py-4 text-right">
                                        <button
                                            type="button"
                                            disabled={!canRevoke || revokeM.isPending}
                                            onClick={() => { if (confirm("Révoquer cette invitation ? Elle ne pourra plus être utilisée.")) revokeM.mutate(i.id); }}
                                            className="inline-flex items-center gap-1.5 rounded-lg border border-danger/40 bg-surface px-3 py-1.5 text-xs font-medium text-danger transition-colors duration-200 hover:bg-danger/10 disabled:cursor-not-allowed disabled:opacity-40"
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
                                <td colSpan={6} className="px-6 py-12 text-center text-sm text-fg-muted">
                                    Aucune invitation. Générez-en une ci-dessus.
                                </td>
                            </tr>
                        )}
                        {listQ.isLoading && (
                            <tr><td colSpan={6} className="px-6 py-12 text-center text-sm text-fg-muted">Chargement…</td></tr>
                        )}
                    </tbody>
                </table>
                </div>
            </section>
        </div>
    );
}
