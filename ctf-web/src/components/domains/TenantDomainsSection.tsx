"use client";

import { useState } from "react";
import { motion, AnimatePresence, useReducedMotion } from "framer-motion";
import {
    Globe, ShieldCheck, Clock, Copy, Check, Trash2, RefreshCw, Plus, AlertCircle, Info,
} from "lucide-react";
import {
    useTenantDomains, useDeclareDomain, useVerifyDomain, useRemoveDomain,
    type TenantDomain, type VerifyDomainResult,
} from "@/lib/hooks/useTenantDomains";

/** Section « Domaines de l'entreprise » — déclaration + preuve de possession par DNS TXT. */
export default function TenantDomainsSection() {
    const { data, isLoading, isError } = useTenantDomains();
    const declare = useDeclareDomain();
    const [value, setValue] = useState("");
    const [error, setError] = useState<string | null>(null);

    async function add(e: React.FormEvent) {
        e.preventDefault();
        setError(null);
        const domain = value.trim().toLowerCase();
        if (!domain) return;
        try {
            await declare.mutateAsync(domain);
            setValue("");
        } catch (err) {
            setError(err instanceof Error ? err.message : "Échec de la déclaration du domaine.");
        }
    }

    return (
        <div style={{ display: "flex", flexDirection: "column", gap: 16 }}>
            <p style={{ margin: 0, fontSize: 13.5, color: "var(--v-text-2)", lineHeight: 1.55 }}>
                Déclarez les domaines email de votre organisation (ex. <code style={code}>clinique-saint-marc.fr</code>) et
                prouvez leur possession via un enregistrement DNS. C&apos;est la base sécurité du rattachement automatique et du SSO.
            </p>

            {/* Formulaire d'ajout */}
            <form onSubmit={add} style={{ display: "flex", gap: 8, flexWrap: "wrap" }}>
                <input
                    value={value}
                    onChange={e => setValue(e.target.value)}
                    placeholder="votre-domaine.fr"
                    aria-label="Domaine à déclarer"
                    autoCapitalize="none" autoCorrect="off" spellCheck={false}
                    style={inputStyle}
                />
                <button type="submit" disabled={declare.isPending || !value.trim()} style={primaryBtn(declare.isPending || !value.trim())}>
                    <Plus size={14} /> {declare.isPending ? "Ajout…" : "Ajouter"}
                </button>
            </form>
            <AnimatePresence>
                {error && (
                    <motion.div initial={{ opacity: 0, y: -4 }} animate={{ opacity: 1, y: 0 }} exit={{ opacity: 0 }} transition={{ duration: 0.18 }}
                        style={{ display: "flex", alignItems: "center", gap: 8, fontSize: 13, color: "var(--v-danger)" }}>
                        <AlertCircle size={14} /> {error}
                    </motion.div>
                )}
            </AnimatePresence>

            {/* États */}
            {isLoading && <div style={{ fontSize: 13.5, color: "var(--v-text-2)", padding: "8px 0" }}>Chargement des domaines…</div>}
            {isError && <div style={{ fontSize: 13.5, color: "var(--v-danger)" }}>Impossible de charger les domaines.</div>}
            {!isLoading && !isError && (data?.length ?? 0) === 0 && <EmptyState />}

            {!isLoading && !isError && (data?.length ?? 0) > 0 && (
                <div style={{ display: "flex", flexDirection: "column", gap: 12 }}>
                    {data!.map(d => <DomainCard key={d.id} domain={d} />)}
                </div>
            )}
        </div>
    );
}

function EmptyState() {
    return (
        <div style={{ borderRadius: 12, border: "1px dashed var(--v-border)", padding: "20px", textAlign: "center", display: "flex", flexDirection: "column", alignItems: "center", gap: 6 }}>
            <Globe size={22} style={{ color: "var(--v-text-2)", opacity: 0.7 }} />
            <div style={{ fontSize: 13.5, fontWeight: 600, color: "var(--v-text)" }}>Aucun domaine déclaré</div>
            <div style={{ fontSize: 12.5, color: "var(--v-text-2)", lineHeight: 1.5, maxWidth: 380 }}>
                Ajoutez le domaine email de votre organisation ci-dessus pour commencer sa vérification.
            </div>
        </div>
    );
}

function DomainCard({ domain }: { domain: TenantDomain }) {
    const verify = useVerifyDomain();
    const remove = useRemoveDomain();
    const reduce = useReducedMotion();
    const [result, setResult] = useState<VerifyDomainResult | null>(null);
    const [confirmRemove, setConfirmRemove] = useState(false);

    async function onVerify() {
        setResult(null);
        try { setResult(await verify.mutateAsync(domain.id)); }
        catch (e) { setResult({ result: "dns_unavailable", isVerified: false, message: e instanceof Error ? e.message : "Échec de la vérification." }); }
    }

    return (
        <motion.div
            initial={reduce ? false : { opacity: 0, y: 6 }} animate={{ opacity: 1, y: 0 }} transition={{ duration: 0.2 }}
            style={{ borderRadius: 12, border: "1px solid var(--v-border)", background: "var(--v-surface-2)", padding: 16, display: "flex", flexDirection: "column", gap: 12 }}
        >
            <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", gap: 12, flexWrap: "wrap" }}>
                <div style={{ display: "flex", alignItems: "center", gap: 10, minWidth: 0 }}>
                    <Globe size={16} style={{ color: "var(--v-text-2)", flexShrink: 0 }} />
                    <span style={{ fontSize: 14.5, fontWeight: 600, color: "var(--v-text)", fontFamily: "ui-monospace, monospace", overflow: "hidden", textOverflow: "ellipsis" }}>{domain.domain}</span>
                    <StatusPill verified={domain.isVerified} />
                </div>
                <RemoveControl
                    pending={remove.isPending} confirming={confirmRemove}
                    onAsk={() => setConfirmRemove(true)} onCancel={() => setConfirmRemove(false)}
                    onConfirm={() => remove.mutate(domain.id)}
                />
            </div>

            {domain.isVerified ? (
                <div style={{ display: "flex", alignItems: "center", gap: 8, fontSize: 12.5, color: "var(--v-success)" }}>
                    <ShieldCheck size={14} />
                    Domaine vérifié{domain.verifiedAt ? ` le ${new Date(domain.verifiedAt).toLocaleDateString("fr-FR")}` : ""}.
                </div>
            ) : (
                <PendingBlock domain={domain} onVerify={onVerify} verifying={verify.isPending} result={result} reduce={!!reduce} />
            )}
        </motion.div>
    );
}

function PendingBlock({ domain, onVerify, verifying, result, reduce }: {
    domain: TenantDomain; onVerify: () => void; verifying: boolean; result: VerifyDomainResult | null; reduce: boolean;
}) {
    return (
        <div style={{ display: "flex", flexDirection: "column", gap: 10 }}>
            <div style={{ display: "flex", alignItems: "flex-start", gap: 8, fontSize: 12.5, color: "var(--v-text-2)", lineHeight: 1.5 }}>
                <Info size={14} style={{ flexShrink: 0, marginTop: 2 }} />
                <span>Ajoutez cet enregistrement <strong>TXT</strong> chez votre hébergeur DNS (OVH, Gandi, Cloudflare…), puis cliquez sur Vérifier. La propagation peut prendre jusqu&apos;à 48&nbsp;h.</span>
            </div>
            <RecordRow label="Nom" value={domain.dnsRecordName} />
            <RecordRow label="Valeur" value={domain.dnsRecordValue} />
            <div style={{ display: "flex", alignItems: "center", gap: 10, flexWrap: "wrap" }}>
                <button type="button" onClick={onVerify} disabled={verifying} style={primaryBtn(verifying)}>
                    <RefreshCw size={14} />
                    {verifying ? "Vérification…" : "Vérifier"}
                </button>
                <AnimatePresence>
                    {result && (
                        <motion.span
                            initial={reduce ? false : { opacity: 0, x: 6 }} animate={{ opacity: 1, x: 0 }} exit={{ opacity: 0 }} transition={{ duration: 0.18 }}
                            style={{ display: "flex", alignItems: "center", gap: 6, fontSize: 12.5, color: result.isVerified ? "var(--v-success)" : "var(--v-warning)" }}>
                            {result.isVerified ? <Check size={14} /> : <AlertCircle size={14} />}
                            {result.message}
                        </motion.span>
                    )}
                </AnimatePresence>
            </div>
        </div>
    );
}

function RecordRow({ label, value }: { label: string; value: string }) {
    return (
        <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
            <span style={{ fontSize: 11, fontWeight: 600, textTransform: "uppercase", letterSpacing: "0.04em", color: "var(--v-text-2)", width: 52, flexShrink: 0 }}>{label}</span>
            <code style={{ ...code, flex: 1, overflow: "auto", whiteSpace: "nowrap", padding: "6px 10px" }}>{value}</code>
            <CopyChip value={value} />
        </div>
    );
}

function StatusPill({ verified }: { verified: boolean }) {
    const color = verified ? "var(--v-success)" : "var(--v-warning)";
    return (
        <span style={{
            display: "inline-flex", alignItems: "center", gap: 5, borderRadius: 999, padding: "2px 9px",
            fontSize: 10.5, fontWeight: 700, textTransform: "uppercase", letterSpacing: "0.04em",
            color, background: `color-mix(in srgb, ${color} 16%, transparent)`, flexShrink: 0,
        }}>
            {verified ? <ShieldCheck size={11} /> : <Clock size={11} />}
            {verified ? "Vérifié" : "En attente"}
        </span>
    );
}

function CopyChip({ value }: { value: string }) {
    const [copied, setCopied] = useState(false);
    async function copy() {
        try { await navigator.clipboard.writeText(value); setCopied(true); setTimeout(() => setCopied(false), 1600); }
        catch { /* champ sélectionnable en repli */ }
    }
    return (
        <button type="button" onClick={copy} aria-label="Copier"
            style={{ display: "inline-flex", alignItems: "center", justifyContent: "center", width: 34, height: 34, flexShrink: 0, borderRadius: 8, border: "1px solid var(--v-border)", background: "var(--v-surface)", color: copied ? "var(--v-success)" : "var(--v-text-2)", cursor: "pointer", transition: "color 0.2s" }}>
            {copied ? <Check size={14} /> : <Copy size={14} />}
        </button>
    );
}

function RemoveControl({ pending, confirming, onAsk, onCancel, onConfirm }: {
    pending: boolean; confirming: boolean; onAsk: () => void; onCancel: () => void; onConfirm: () => void;
}) {
    if (!confirming) {
        return (
            <button type="button" onClick={onAsk} aria-label="Retirer le domaine"
                style={{ display: "inline-flex", alignItems: "center", gap: 6, fontSize: 12.5, fontWeight: 600, color: "var(--v-text-2)", background: "transparent", border: "none", cursor: "pointer" }}>
                <Trash2 size={14} /> Retirer
            </button>
        );
    }
    return (
        <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
            <span style={{ fontSize: 12, color: "var(--v-text-2)" }}>Confirmer&nbsp;?</span>
            <button type="button" onClick={onConfirm} disabled={pending}
                style={{ fontSize: 12.5, fontWeight: 600, color: "var(--v-danger)", background: "transparent", border: "none", cursor: "pointer" }}>
                {pending ? "…" : "Oui, retirer"}
            </button>
            <button type="button" onClick={onCancel}
                style={{ fontSize: 12.5, color: "var(--v-text-2)", background: "transparent", border: "none", cursor: "pointer" }}>Annuler</button>
        </div>
    );
}

// ── styles tokens (aucune couleur en dur) ──────────────────────────────────
const code: React.CSSProperties = {
    fontFamily: "ui-monospace, monospace", fontSize: 12.5, color: "var(--v-text)",
    background: "var(--v-surface)", border: "1px solid var(--v-border)", borderRadius: 6, padding: "1px 6px",
};
const inputStyle: React.CSSProperties = {
    flex: 1, minWidth: 200, height: 40, padding: "0 12px", borderRadius: 10,
    border: "1px solid var(--v-border)", background: "var(--v-surface)", color: "var(--v-text)",
    fontSize: 14, outline: "none",
};
function primaryBtn(disabled: boolean): React.CSSProperties {
    return {
        display: "inline-flex", alignItems: "center", gap: 7, height: 40, padding: "0 16px",
        borderRadius: 10, border: "1px solid var(--v-accent)",
        background: disabled ? "color-mix(in srgb, var(--v-accent) 45%, transparent)" : "var(--v-accent)",
        color: "var(--v-text)", fontSize: 13.5, fontWeight: 600, cursor: disabled ? "not-allowed" : "pointer",
        transition: "background 0.2s", whiteSpace: "nowrap",
    };
}
