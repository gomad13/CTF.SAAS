"use client";

import { useRef, useState } from "react";
import type { CSSProperties } from "react";
import { useQuery } from "@tanstack/react-query";
import { motion, AnimatePresence } from "framer-motion";
import QRCode from "react-qr-code";
import {
    QrCode, Plus, Trash2, Copy, Check, Clock, Users, AlertCircle, X, Download, Building2, AppWindow, UserPlus,
} from "lucide-react";
import { apiFetch } from "@/lib/api";
import { VisionInput, VisionSelect, VisionButton } from "@/components/vision/VisionForm";
import { useInvites, useCreateInvite, useRevokeInvite } from "@/lib/hooks/useInvites";
import type { InviteDto, CreatedInviteDto, InviteType } from "@/lib/types/invites";

const DURATIONS: { label: string; hours: number }[] = [
    { label: "1 heure", hours: 1 },
    { label: "24 heures", hours: 24 },
    { label: "7 jours", hours: 168 },
    { label: "30 jours", hours: 720 },
];

// Métadonnées des 3 types de QR (libellés clairs + couleur token Vision, pas de vert cyber).
const TYPE_META: Record<InviteType, { short: string; label: string; help: string; color: string }> = {
    app: {
        short: "Application",
        label: "Application — inscription générale",
        help: "Invite à créer un compte Sentys, SANS rattachement à une entreprise. Mène à la page d'inscription générale.",
        color: "var(--v-cyan)",
    },
    enterprise_signup: {
        short: "Inscription",
        label: "Entreprise — inscription (nouveau compte)",
        help: "Une personne SANS compte s'inscrit avec l'entreprise pré-remplie et verrouillée, puis est rattachée automatiquement (rôle Membre).",
        color: "var(--v-success)",
    },
    enterprise_join: {
        short: "Rejoindre",
        label: "Entreprise — rejoindre (compte existant)",
        help: "Une personne qui a DÉJÀ un compte scanne le QR et rejoint l'entreprise (rôle Membre). Ses autres sociétés sont conservées.",
        color: "var(--v-accent)",
    },
};

const TYPE_ICON: Record<InviteType, typeof Building2> = {
    app: AppWindow,
    enterprise_signup: UserPlus,
    enterprise_join: Building2,
};

function inviteStatus(i: InviteDto): { label: string; color: string } {
    if (i.isRevoked) return { label: "Révoquée", color: "var(--v-danger)" };
    if (i.isExpired) return { label: "Expirée", color: "var(--v-text-3)" };
    if (i.usedCount >= i.maxUses) return { label: "Épuisée", color: "var(--warning)" };
    return { label: "Active", color: "var(--v-success)" };
}

// Pastille de statut/type — fond teinté 15 % + texte à la couleur token.
function pill(color: string): CSSProperties {
    return { display: "inline-flex", alignItems: "center", borderRadius: 999, padding: "3px 10px", fontSize: 11.5, fontWeight: 600, whiteSpace: "nowrap", background: "color-mix(in srgb, " + color + " 15%, transparent)", color };
}

function fmt(iso: string): string {
    return new Date(iso).toLocaleString("fr-FR", {
        day: "2-digit", month: "2-digit", year: "numeric", hour: "2-digit", minute: "2-digit",
    });
}

const th: CSSProperties = { padding: "12px 20px", textAlign: "left", fontSize: 11, fontWeight: 700, textTransform: "uppercase", letterSpacing: "0.05em", color: "var(--v-text-2)" };
const td: CSSProperties = { padding: "14px 20px", fontSize: 13.5, color: "var(--v-text)", verticalAlign: "middle" };

/**
 * Gestion des invitations QR en 3 types (voir QR_3_TYPES).
 * Génération + QR + téléchargement PNG + URL copiable + liste typée + révocation.
 * Réutilisé UNIQUEMENT par les Paramètres entreprise (/admin/entreprise). Thème Vision UI (tokens --v-*).
 */
export default function InvitesManager() {
    const listQ = useInvites();
    const createM = useCreateInvite();
    const revokeM = useRevokeInvite();
    const meQ = useQuery({ queryKey: ["me"], queryFn: () => apiFetch<{ role: string }>("/api/auth/me"), staleTime: 60_000 });
    const isSuperAdmin = meQ.data?.role === "SuperAdmin";

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
        } catch { /* clipboard indisponible — l'URL reste affichée et sélectionnable */ }
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
            ctx.fillStyle = "#ffffff"; // fond blanc du PNG QR (hors UI — nécessaire au scan)
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
    const invites = listQ.data ?? [];

    return (
        <div style={{ display: "flex", flexDirection: "column", gap: 20 }}>
            {/* Générateur */}
            <div style={{ display: "flex", flexDirection: "column", gap: 16 }}>
                <h3 style={{ display: "flex", alignItems: "center", gap: 8, fontSize: 13, fontWeight: 700, textTransform: "uppercase", letterSpacing: "0.05em", color: "var(--v-text-2)" }}>
                    <Plus size={14} /> Générer une invitation QR
                </h3>

                {/* Sélecteur de type (cartes cliquables) */}
                <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
                    {availableTypes.map(t => {
                        const meta = TYPE_META[t];
                        const Icon = TYPE_ICON[t];
                        const active = type === t;
                        return (
                            <button key={t} type="button" onClick={() => setType(t)} className="v-hover"
                                style={{ display: "flex", flexDirection: "column", gap: 6, borderRadius: 12, padding: 14, textAlign: "left", cursor: "pointer",
                                    border: "1px solid " + (active ? "var(--v-accent)" : "var(--v-border)"),
                                    background: active ? "color-mix(in srgb, var(--v-accent) 12%, transparent)" : "var(--v-surface-2)" }}>
                                <span style={{ display: "flex", alignItems: "center", gap: 8, fontSize: 13.5, fontWeight: 600, color: "var(--v-text)" }}>
                                    <Icon size={15} style={{ color: active ? "var(--v-accent)" : "var(--v-text-2)" }} /> {meta.short}
                                </span>
                                <span style={{ fontSize: 12, lineHeight: 1.45, color: "var(--v-text-2)" }}>{meta.help}</span>
                            </button>
                        );
                    })}
                </div>

                <div style={{ display: "flex", flexWrap: "wrap", alignItems: "flex-end", gap: 16 }}>
                    <label style={{ display: "flex", flexDirection: "column", gap: 6, fontSize: 12.5, fontWeight: 600, color: "var(--v-text)" }}>
                        <span style={{ display: "flex", alignItems: "center", gap: 6 }}><Clock size={13} /> Durée de validité</span>
                        <VisionSelect value={hours} onChange={e => setHours(Number(e.target.value))} style={{ width: "auto" }}>
                            {DURATIONS.map(d => <option key={d.hours} value={d.hours}>{d.label}</option>)}
                        </VisionSelect>
                    </label>
                    <label style={{ display: "flex", flexDirection: "column", gap: 6, fontSize: 12.5, fontWeight: 600, color: "var(--v-text)" }}>
                        <span style={{ display: "flex", alignItems: "center", gap: 6 }}><Users size={13} /> Nombre max d&apos;usages</span>
                        <VisionInput type="number" min={1} max={1000} value={maxUses} onChange={e => setMaxUses(e.target.value)} style={{ width: 120 }} />
                    </label>
                    <VisionButton type="button" onClick={handleCreate} disabled={createM.isPending}>
                        <QrCode size={14} /> {createM.isPending ? "Génération…" : "Générer le QR"}
                    </VisionButton>
                </div>
                <p style={{ fontSize: 12, color: "var(--v-text-2)", lineHeight: 1.5 }}>{isEnterprise
                    ? "Ce QR est scellé à votre société active : il ne fait rejoindre que celle-ci."
                    : "Ce QR mène à l'inscription générale Sentys, sans rattachement à une entreprise."}</p>
                {createM.isError && (
                    <div style={{ display: "flex", alignItems: "center", gap: 6, fontSize: 12.5, color: "var(--v-danger)" }}>
                        <AlertCircle size={13} /> {createM.error.message}
                    </div>
                )}

                <AnimatePresence>
                    {created && (
                        <motion.div initial={{ opacity: 0, y: 8 }} animate={{ opacity: 1, y: 0 }} exit={{ opacity: 0 }} transition={{ duration: 0.25 }}
                            className="grid grid-cols-1 gap-6 sm:grid-cols-[auto_1fr]"
                            style={{ borderRadius: 14, border: "1px solid var(--v-border)", background: "var(--v-surface-2)", padding: 20 }}>
                            <div style={{ display: "flex", flexDirection: "column", alignItems: "center", gap: 8 }}>
                                <div ref={qrRef} style={{ borderRadius: 12, background: "#fff", padding: 12 }}>
                                    <QRCode value={created.joinUrl} size={160} />
                                </div>
                                <VisionButton variant="ghost" type="button" onClick={downloadQrPng} style={{ padding: "6px 10px", fontSize: 12.5, color: "var(--v-accent)" }}>
                                    <Download size={13} /> Télécharger le QR
                                </VisionButton>
                                <span style={{ fontSize: 12, color: "var(--v-text-2)" }}>{created.type === "app" ? "Scannez pour vous inscrire" : "Scannez pour rejoindre"}</span>
                            </div>
                            <div style={{ display: "flex", flexDirection: "column", gap: 12 }}>
                                <div style={{ display: "flex", alignItems: "flex-start", justifyContent: "space-between", gap: 8 }}>
                                    <span style={pill(TYPE_META[created.type].color)}>{TYPE_META[created.type].label}</span>
                                    <button type="button" onClick={() => setCreated(null)} title="Fermer"
                                        style={{ background: "transparent", border: "none", cursor: "pointer", color: "var(--v-text-2)", display: "inline-flex" }}>
                                        <X size={16} />
                                    </button>
                                </div>
                                <p style={{ fontSize: 12, color: "var(--v-text-2)", lineHeight: 1.5 }}>
                                    Expire le {fmt(created.expiresAt)} · {created.maxUses} usage{created.maxUses > 1 ? "s" : ""} max.
                                    Ce lien n&apos;est affiché qu&apos;une seule fois — copiez-le maintenant.
                                </p>
                                <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
                                    <VisionInput readOnly value={created.joinUrl} onFocus={e => e.currentTarget.select()}
                                        style={{ flex: 1, fontFamily: "ui-monospace, monospace", fontSize: 12 }} />
                                    <VisionButton variant="secondary" type="button" onClick={() => copyUrl(created.joinUrl)} style={{ minWidth: 104 }}>
                                        <AnimatePresence mode="wait" initial={false}>
                                            {copied ? (
                                                <motion.span key="ok" initial={{ opacity: 0, scale: 0.6 }} animate={{ opacity: 1, scale: 1 }} exit={{ opacity: 0, scale: 0.6 }} transition={{ duration: 0.18 }} style={{ display: "inline-flex", alignItems: "center", gap: 6, color: "var(--v-success)" }}>
                                                    <Check size={13} /> Copié !
                                                </motion.span>
                                            ) : (
                                                <motion.span key="copy" initial={{ opacity: 0 }} animate={{ opacity: 1 }} exit={{ opacity: 0 }} transition={{ duration: 0.15 }} style={{ display: "inline-flex", alignItems: "center", gap: 6 }}>
                                                    <Copy size={13} /> Copier
                                                </motion.span>
                                            )}
                                        </AnimatePresence>
                                    </VisionButton>
                                </div>
                            </div>
                        </motion.div>
                    )}
                </AnimatePresence>
            </div>

            {/* Liste */}
            <div style={{ borderRadius: 16, border: "1px solid var(--v-border)", overflow: "hidden", background: "color-mix(in srgb, var(--v-surface) 82%, transparent)" }}>
                <div style={{ overflowX: "auto" }}>
                    <table style={{ width: "100%", borderCollapse: "collapse" }}>
                        <thead style={{ background: "var(--v-surface-2)" }}>
                            <tr>
                                <th style={th}>Type</th>
                                <th style={th}>Statut</th>
                                <th style={th}>Usages</th>
                                <th style={th}>Expiration</th>
                                <th style={th}>Créée le</th>
                                <th style={{ ...th, textAlign: "right" }}>Actions</th>
                            </tr>
                        </thead>
                        <tbody>
                            {invites.map(i => {
                                const st = inviteStatus(i);
                                const tm = TYPE_META[i.type];
                                const canRevoke = !i.isRevoked && !i.isExpired;
                                return (
                                    <tr key={i.id} className="v-row" style={{ borderTop: "1px solid var(--v-border)" }}>
                                        <td style={td}>
                                            <span style={pill(tm.color)}>{tm.short}</span>
                                            {i.tenantName && <span style={{ marginLeft: 8, fontSize: 12, color: "var(--v-text-2)" }}>{i.tenantName}</span>}
                                        </td>
                                        <td style={td}><span style={pill(st.color)}>{st.label}</span></td>
                                        <td style={td}>{i.usedCount} / {i.maxUses}</td>
                                        <td style={td}>{fmt(i.expiresAt)}</td>
                                        <td style={{ ...td, color: "var(--v-text-2)" }}>{fmt(i.createdAt)}</td>
                                        <td style={{ ...td, textAlign: "right" }}>
                                            <button type="button" disabled={!canRevoke || revokeM.isPending}
                                                onClick={() => { if (confirm("Révoquer cette invitation ? Elle ne pourra plus être utilisée.")) revokeM.mutate(i.id); }}
                                                title={canRevoke ? "Révoquer" : "Déjà inactive"}
                                                style={{ display: "inline-flex", alignItems: "center", gap: 6, borderRadius: 8, padding: "6px 12px", fontSize: 12.5, fontWeight: 600, cursor: canRevoke ? "pointer" : "not-allowed", opacity: canRevoke ? 1 : 0.4, color: "var(--v-danger)", background: "color-mix(in srgb, var(--v-danger) 10%, transparent)", border: "1px solid color-mix(in srgb, var(--v-danger) 40%, transparent)", transition: "background-color .15s ease" }}>
                                                <Trash2 size={13} /> Révoquer
                                            </button>
                                        </td>
                                    </tr>
                                );
                            })}
                            {listQ.isSuccess && invites.length === 0 && (
                                <tr><td colSpan={6} style={{ ...td, textAlign: "center", padding: "48px 20px", color: "var(--v-text-2)" }}>Aucune invitation. Générez-en une ci-dessus.</td></tr>
                            )}
                            {listQ.isLoading && (
                                <tr><td colSpan={6} style={{ ...td, textAlign: "center", padding: "48px 20px", color: "var(--v-text-2)" }}>Chargement…</td></tr>
                            )}
                        </tbody>
                    </table>
                </div>
            </div>
        </div>
    );
}
