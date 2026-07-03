"use client";

import { useState } from "react";
import Link from "next/link";
import {
    Building2, Copy, Check, QrCode, KeyRound, ShieldCheck, Users, Save, ExternalLink, AlertCircle,
} from "lucide-react";
import InvitesManager from "@/components/invites/InvitesManager";
import { useTenantSettings, useUpdateTenantSettings } from "@/lib/hooks/useTenantSettings";
import type { TenantSettings } from "@/lib/types/tenantSettings";

export default function EntreprisePage() {
    const { data, isLoading, isError } = useTenantSettings();

    if (isLoading) {
        return <div className="px-6 py-12 text-center text-sm text-fg-muted">Chargement…</div>;
    }
    if (isError || !data) {
        return <div className="px-6 py-12 text-center text-sm text-danger">Impossible de charger les paramètres.</div>;
    }
    return <EntrepriseForm initial={data} />;
}

function EntrepriseForm({ initial }: { initial: TenantSettings }) {
    const update = useUpdateTenantSettings();

    // État éditable initialisé depuis les données chargées (pas de setState-in-effect).
    const [name, setName] = useState(initial.name);
    const [description, setDescription] = useState(initial.description ?? "");
    const [sector, setSector] = useState(initial.sector ?? "");
    const [googleSso, setGoogleSso] = useState(initial.googleSsoEnabled);
    const [microsoftSso, setMicrosoftSso] = useState(initial.microsoftSsoEnabled);
    const [teamsOpen, setTeamsOpen] = useState(initial.defaultTeamsOpen);
    const [copied, setCopied] = useState(false);
    const [toast, setToast] = useState<string | null>(null);

    async function save() {
        try {
            await update.mutateAsync({
                name: name.trim(),
                description: description.trim() || null,
                sector: sector.trim() || null,
                googleSsoEnabled: googleSso,
                microsoftSsoEnabled: microsoftSso,
                defaultTeamsOpen: teamsOpen,
            });
            setToast("Modifications enregistrées");
            setTimeout(() => setToast(null), 2500);
        } catch (e) {
            setToast(e instanceof Error ? e.message : "Erreur lors de l'enregistrement");
            setTimeout(() => setToast(null), 3500);
        }
    }

    async function copyTenantId() {
        try {
            await navigator.clipboard.writeText(initial.tenantId);
            setCopied(true);
            setTimeout(() => setCopied(false), 2000);
        } catch { /* champ sélectionnable en repli */ }
    }

    return (
        <div className="mx-auto flex max-w-4xl flex-col gap-6 px-4 py-6 sm:px-6 sm:py-8">
            <div>
                <h1 className="text-2xl font-bold text-[#F1F5F9]">Paramètres entreprise</h1>
                <p className="mt-1 text-sm text-fg-muted">
                    Configuration générale de votre organisation. Réservé aux administrateurs ; chaque admin
                    ne gère que sa propre entreprise.
                </p>
            </div>

            {/* 1. INFOS ENTREPRISE */}
            <Section icon={<Building2 size={15} />} title="Infos entreprise">
                <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                    <Field label="Nom de l'entreprise">
                        <input value={name} onChange={e => setName(e.target.value)} className={inputCls} />
                    </Field>
                    <Field label="Secteur d'activité">
                        <input value={sector} onChange={e => setSector(e.target.value)} placeholder="ex. Santé, Finance…" className={inputCls} />
                    </Field>
                </div>
                <Field label="Description">
                    <textarea value={description} onChange={e => setDescription(e.target.value)} rows={3}
                        placeholder="Quelques mots sur votre organisation (optionnel)" className={`${inputCls} resize-y`} />
                </Field>
                <Field label="Identifiant entreprise (TenantId)">
                    <div className="flex items-center gap-2">
                        <input readOnly value={initial.tenantId} onFocus={e => e.currentTarget.select()}
                            className={`${inputCls} flex-1 font-mono text-xs`} />
                        <button type="button" onClick={copyTenantId}
                            className="inline-flex shrink-0 items-center gap-1.5 rounded-lg border border-[#E2E8F0] bg-surface px-3 py-2 text-xs font-medium text-fg-body transition-colors duration-200 hover:bg-[#F1F5F9]">
                            {copied ? <><Check size={13} className="text-success" /> Copié</> : <><Copy size={13} /> Copier</>}
                        </button>
                    </div>
                    <p className="mt-1 text-xs text-fg-muted">À partager pour rattacher manuellement un membre (alternative au QR code).</p>
                </Field>
            </Section>

            {/* 2. INVITATIONS / QR — composant réutilisé */}
            <Section icon={<QrCode size={15} />} title="Invitations par QR code"
                desc="Générez un lien/QR sécurisé (durée + usages limités, révocable) pour faire rejoindre votre entreprise.">
                <InvitesManager />
            </Section>

            {/* 3. SSO */}
            <Section icon={<KeyRound size={15} />} title="Connexions SSO"
                desc="Autorisez la connexion de vos membres via Google ou Microsoft.">
                <ToggleRow
                    label="Connexion Google"
                    desc={initial.googleSsoConfigured ? "Configuré sur le serveur." : "Non configuré côté serveur — sans effet tant que les clés ne sont pas posées."}
                    configured={initial.googleSsoConfigured}
                    on={googleSso} onChange={setGoogleSso}
                />
                <ToggleRow
                    label="Connexion Microsoft"
                    desc={initial.microsoftSsoConfigured ? "Configuré sur le serveur." : "Non configuré côté serveur — sans effet tant que les clés ne sont pas posées."}
                    configured={initial.microsoftSsoConfigured}
                    on={microsoftSso} onChange={setMicrosoftSso}
                />
            </Section>

            {/* 4. SÉCURITÉ */}
            <Section icon={<ShieldCheck size={15} />} title="Sécurité">
                <p className="text-sm text-fg-body">
                    La <strong>double authentification par email (2FA)</strong> est disponible et activable par
                    chaque membre depuis ses paramètres personnels (Profil → Sécurité). Encouragez vos équipes
                    à l’activer.
                </p>
                <ul className="mt-3 list-disc space-y-1 pl-5 text-sm text-fg-muted">
                    <li>Mots de passe forts (8+ caractères, majuscule, chiffre, caractère spécial) — déjà imposé.</li>
                    <li>2FA email recommandée pour les comptes admin.</li>
                    <li>Révoquez les invitations QR inutilisées et limitez leur durée de validité.</li>
                </ul>
                <Link href="/dashboard/parametres"
                    className="mt-4 inline-flex items-center gap-1.5 text-sm font-medium text-primary transition-colors duration-200 hover:text-primary-hover">
                    Gérer ma propre 2FA <ExternalLink size={13} />
                </Link>
            </Section>

            {/* 5. ÉQUIPES */}
            <Section icon={<Users size={15} />} title="Équipes"
                desc={`${initial.teamsCount} équipe${initial.teamsCount > 1 ? "s" : ""} dans votre entreprise.`}>
                {initial.teamsModeEnabled ? (
                    <>
                        <ToggleRow
                            label="Équipes ouvertes par défaut"
                            desc="Les nouvelles équipes seront rejoignables librement par les membres (au lieu d'être sur invitation de l'admin)."
                            on={teamsOpen} onChange={setTeamsOpen}
                        />
                        <Link href="/admin/teams"
                            className="mt-4 inline-flex items-center gap-1.5 text-sm font-medium text-primary transition-colors duration-200 hover:text-primary-hover">
                            Gérer les équipes <ExternalLink size={13} />
                        </Link>
                    </>
                ) : (
                    <p className="text-sm text-fg-muted">
                        Le mode Équipes est désactivé pour votre entreprise. Activez-le dans Administration → Paramètres (modes).
                    </p>
                )}
            </Section>

            {/* Barre d'enregistrement */}
            <div className="sticky bottom-4 flex items-center justify-end gap-3 rounded-xl border border-[#E2E8F0] bg-surface/90 p-3 shadow-sm backdrop-blur-md">
                {toast && (
                    <span className="flex items-center gap-1.5 text-sm text-fg-body">
                        {update.isError ? <AlertCircle size={14} className="text-danger" /> : <Check size={14} className="text-success" />}
                        {toast}
                    </span>
                )}
                <button type="button" onClick={save} disabled={update.isPending || !name.trim()}
                    className="inline-flex items-center gap-2 rounded-lg bg-primary px-4 py-2 text-sm font-medium text-white transition-colors duration-200 hover:bg-primary-hover disabled:opacity-50">
                    <Save size={14} /> {update.isPending ? "Enregistrement…" : "Enregistrer les modifications"}
                </button>
            </div>
        </div>
    );
}

// ── UI helpers (charte admin claire) ───────────────────────────────────────────
const inputCls = "w-full rounded-lg border border-[#E2E8F0] bg-surface px-3 py-2 text-sm text-fg-heading";

function Section({ icon, title, desc, children }: {
    icon: React.ReactNode; title: string; desc?: string; children: React.ReactNode;
}) {
    return (
        <section className="rounded-xl border border-[#E2E8F0] bg-surface p-6 shadow-sm">
            <h2 className="flex items-center gap-2 text-sm font-semibold uppercase tracking-wider text-fg-body">
                {icon} {title}
            </h2>
            {desc && <p className="mt-1 text-sm text-fg-muted">{desc}</p>}
            <div className="mt-4 flex flex-col gap-4">{children}</div>
        </section>
    );
}

function Field({ label, children }: { label: string; children: React.ReactNode }) {
    return (
        <label className="flex flex-col gap-1">
            <span className="text-xs font-medium text-fg-body">{label}</span>
            {children}
        </label>
    );
}

function ToggleRow({ label, desc, on, onChange, configured = true }: {
    label: string; desc?: string; on: boolean; onChange: (v: boolean) => void; configured?: boolean;
}) {
    return (
        <div className="flex items-center justify-between gap-4 border-b border-[#E2E8F0] pb-3 last:border-0 last:pb-0">
            <div className="min-w-0">
                <div className="flex items-center gap-2 text-sm font-medium text-fg-heading">
                    {label}
                    {!configured && (
                        <span className="rounded-full bg-[#F1F5F9] px-2 py-0.5 text-[10px] font-medium uppercase tracking-wider text-fg-muted">
                            non configuré
                        </span>
                    )}
                </div>
                {desc && <div className="mt-0.5 text-xs text-fg-muted">{desc}</div>}
            </div>
            <button type="button" role="switch" aria-checked={on} onClick={() => onChange(!on)}
                className={`relative h-6 w-11 shrink-0 rounded-full transition-colors duration-200 ${on ? "bg-primary" : "bg-[#CBD5E1]"}`}>
                <span className={`absolute top-0.5 h-5 w-5 rounded-full bg-surface transition-all duration-200 ${on ? "left-[22px]" : "left-0.5"}`} />
            </button>
        </div>
    );
}
