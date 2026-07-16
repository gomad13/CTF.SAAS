"use client";

import { useState } from "react";
import Link from "next/link";
import { motion, AnimatePresence } from "framer-motion";
import {
    Building2, Copy, Check, QrCode, KeyRound, ShieldCheck, Users, Save, ExternalLink, AlertCircle, Globe,
} from "lucide-react";
import InvitesManager from "@/components/invites/InvitesManager";
import TenantDomainsSection from "@/components/domains/TenantDomainsSection";
import Reveal from "@/components/Reveal";
import { Stagger, StaggerItem } from "@/components/Stagger";
import { VisionSection, VisionField, VisionInput, VisionTextarea, VisionButton, VisionToggle } from "@/components/vision/VisionForm";
import { useTenantSettings, useUpdateTenantSettings } from "@/lib/hooks/useTenantSettings";
import type { TenantSettings } from "@/lib/types/tenantSettings";

export default function EntreprisePage() {
    const { data, isLoading, isError } = useTenantSettings();

    if (isLoading) {
        return (
            <div className="vision-dashboard" style={{ minHeight: "100%" }}>
                <div style={{ padding: "48px 20px", textAlign: "center", fontSize: 14, color: "var(--v-text-2)" }}>Chargement…</div>
            </div>
        );
    }
    if (isError || !data) {
        return (
            <div className="vision-dashboard" style={{ minHeight: "100%" }}>
                <div style={{ padding: "48px 20px", textAlign: "center", fontSize: 14, color: "var(--v-danger)" }}>Impossible de charger les paramètres.</div>
            </div>
        );
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
        <div className="vision-dashboard" style={{ minHeight: "100%" }}>
            <div style={{ maxWidth: 900, margin: "0 auto", padding: "24px 20px 100px" }}>
                <Reveal>
                    <div style={{ marginBottom: 20 }}>
                        <h1 style={{ fontSize: 26, fontWeight: 700, color: "var(--v-text)", letterSpacing: "-0.02em" }}>Paramètres entreprise</h1>
                        <p style={{ marginTop: 6, fontSize: 13.5, color: "var(--v-text-2)", lineHeight: 1.55, maxWidth: 640 }}>
                            Configuration générale de votre organisation. Réservé aux administrateurs ; chaque admin ne gère que sa propre entreprise.
                        </p>
                    </div>
                </Reveal>

                <Stagger className="flex flex-col gap-5" gap={0.06}>
                    {/* 1. INFOS ENTREPRISE */}
                    <StaggerItem>
                        <VisionSection icon={<Building2 size={17} />} title="Infos entreprise">
                            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                                <VisionField label="Nom de l'entreprise">
                                    <VisionInput value={name} onChange={e => setName(e.target.value)} />
                                </VisionField>
                                <VisionField label="Secteur d'activité">
                                    <VisionInput value={sector} onChange={e => setSector(e.target.value)} placeholder="ex. Santé, Finance…" />
                                </VisionField>
                            </div>
                            <VisionField label="Description">
                                <VisionTextarea value={description} onChange={e => setDescription(e.target.value)} rows={3}
                                    placeholder="Quelques mots sur votre organisation (optionnel)" />
                            </VisionField>
                            <VisionField label="Identifiant entreprise (TenantId)"
                                hint="À partager pour rattacher manuellement un membre (alternative au QR code).">
                                <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
                                    <VisionInput readOnly value={initial.tenantId} onFocus={e => e.currentTarget.select()}
                                        style={{ flex: 1, fontFamily: "ui-monospace, monospace", fontSize: 12.5 }} />
                                    <CopyButton copied={copied} onCopy={copyTenantId} />
                                </div>
                            </VisionField>
                        </VisionSection>
                    </StaggerItem>

                    {/* 2. INVITATIONS / QR — composant réutilisé */}
                    <StaggerItem>
                        <VisionSection icon={<QrCode size={17} />} title="Invitations par QR code"
                            desc="Générez un lien/QR sécurisé (durée + usages limités, révocable) pour faire rejoindre votre entreprise.">
                            <InvitesManager />
                        </VisionSection>
                    </StaggerItem>

                    {/* 3. SSO */}
                    <StaggerItem>
                        <VisionSection icon={<KeyRound size={17} />} title="Connexions SSO"
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
                        </VisionSection>
                    </StaggerItem>

                    {/* 4. DOMAINES DE L'ENTREPRISE */}
                    <StaggerItem>
                        <VisionSection icon={<Globe size={17} />} title="Domaines de l'entreprise"
                            desc="Prouvez la possession de vos domaines email via un enregistrement DNS. Fondation du rattachement automatique et du SSO.">
                            <TenantDomainsSection />
                        </VisionSection>
                    </StaggerItem>

                    {/* 5. SÉCURITÉ */}
                    <StaggerItem>
                        <VisionSection icon={<ShieldCheck size={17} />} title="Sécurité">
                            <p style={{ fontSize: 14, color: "var(--v-text)", lineHeight: 1.6 }}>
                                La <strong>double authentification par email (2FA)</strong> est disponible et activable par chaque membre
                                depuis ses paramètres personnels (Profil → Sécurité). Encouragez vos équipes à l&apos;activer.
                            </p>
                            <ul style={{ margin: 0, paddingLeft: 20, listStyle: "disc", display: "flex", flexDirection: "column", gap: 4, fontSize: 13.5, color: "var(--v-text-2)", lineHeight: 1.5 }}>
                                <li>Mots de passe forts (8+ caractères, majuscule, chiffre, caractère spécial) — déjà imposé.</li>
                                <li>2FA email recommandée pour les comptes admin.</li>
                                <li>Révoquez les invitations QR inutilisées et limitez leur durée de validité.</li>
                            </ul>
                            <Link href="/dashboard/parametres" className="v-link"
                                style={{ display: "inline-flex", alignItems: "center", gap: 6, fontSize: 13.5, fontWeight: 600, color: "var(--v-accent)", width: "fit-content" }}>
                                Gérer ma propre 2FA <ExternalLink size={13} />
                            </Link>
                        </VisionSection>
                    </StaggerItem>

                    {/* 5. ÉQUIPES */}
                    <StaggerItem>
                        <VisionSection icon={<Users size={17} />} title="Équipes"
                            desc={`${initial.teamsCount} équipe${initial.teamsCount > 1 ? "s" : ""} dans votre entreprise.`}>
                            {initial.teamsModeEnabled ? (
                                <>
                                    <ToggleRow
                                        label="Équipes ouvertes par défaut"
                                        desc="Les nouvelles équipes seront rejoignables librement par les membres (au lieu d'être sur invitation de l'admin)."
                                        on={teamsOpen} onChange={setTeamsOpen}
                                    />
                                    <Link href="/admin/teams" className="v-link"
                                        style={{ display: "inline-flex", alignItems: "center", gap: 6, fontSize: 13.5, fontWeight: 600, color: "var(--v-accent)", width: "fit-content" }}>
                                        Gérer les équipes <ExternalLink size={13} />
                                    </Link>
                                </>
                            ) : (
                                <p style={{ fontSize: 13.5, color: "var(--v-text-2)", lineHeight: 1.55 }}>
                                    Le mode Équipes est désactivé pour votre entreprise. Activez-le dans Administration → Paramètres (modes).
                                </p>
                            )}
                        </VisionSection>
                    </StaggerItem>
                </Stagger>

                {/* Barre d'enregistrement */}
                <div style={{ position: "sticky", bottom: 16, marginTop: 20, display: "flex", alignItems: "center", justifyContent: "flex-end", gap: 12, borderRadius: 16, border: "1px solid var(--v-border)", background: "color-mix(in srgb, var(--v-surface) 88%, transparent)", backdropFilter: "blur(14px)", WebkitBackdropFilter: "blur(14px)", padding: 12, boxShadow: "0 8px 30px rgba(0,0,0,0.3)" }}>
                    <AnimatePresence>
                        {toast && (
                            <motion.span initial={{ opacity: 0, x: 8 }} animate={{ opacity: 1, x: 0 }} exit={{ opacity: 0 }} transition={{ duration: 0.2 }}
                                style={{ display: "flex", alignItems: "center", gap: 6, fontSize: 13.5, color: update.isError ? "var(--v-danger)" : "var(--v-text)" }}>
                                {update.isError ? <AlertCircle size={14} style={{ color: "var(--v-danger)" }} /> : <Check size={14} style={{ color: "var(--v-success)" }} />}
                                {toast}
                            </motion.span>
                        )}
                    </AnimatePresence>
                    <VisionButton type="button" onClick={save} disabled={update.isPending || !name.trim()}>
                        <Save size={14} /> {update.isPending ? "Enregistrement…" : "Enregistrer les modifications"}
                    </VisionButton>
                </div>
            </div>
        </div>
    );
}

// Bouton « Copier » avec feedback animé (check vert Vision, pas de vert cyber).
function CopyButton({ copied, onCopy }: { copied: boolean; onCopy: () => void }) {
    return (
        <VisionButton variant="secondary" type="button" onClick={onCopy} style={{ flexShrink: 0, minWidth: 108 }}>
            <AnimatePresence mode="wait" initial={false}>
                {copied ? (
                    <motion.span key="ok" initial={{ opacity: 0, scale: 0.6 }} animate={{ opacity: 1, scale: 1 }} exit={{ opacity: 0, scale: 0.6 }} transition={{ duration: 0.18 }}
                        style={{ display: "inline-flex", alignItems: "center", gap: 6, color: "var(--v-success)" }}>
                        <Check size={14} /> Copié !
                    </motion.span>
                ) : (
                    <motion.span key="copy" initial={{ opacity: 0 }} animate={{ opacity: 1 }} exit={{ opacity: 0 }} transition={{ duration: 0.15 }}
                        style={{ display: "inline-flex", alignItems: "center", gap: 6 }}>
                        <Copy size={14} /> Copier
                    </motion.span>
                )}
            </AnimatePresence>
        </VisionButton>
    );
}

function ToggleRow({ label, desc, on, onChange, configured = true }: {
    label: string; desc?: string; on: boolean; onChange: (v: boolean) => void; configured?: boolean;
}) {
    return (
        <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", gap: 16, borderBottom: "1px solid var(--v-border)", paddingBottom: 14 }} className="v-togglerow">
            <div style={{ minWidth: 0 }}>
                <div style={{ display: "flex", alignItems: "center", gap: 8, fontSize: 14, fontWeight: 600, color: "var(--v-text)" }}>
                    {label}
                    {!configured && (
                        <span style={{ borderRadius: 999, background: "var(--v-surface-2)", padding: "2px 8px", fontSize: 10, fontWeight: 600, textTransform: "uppercase", letterSpacing: "0.04em", color: "var(--v-text-2)" }}>
                            non configuré
                        </span>
                    )}
                </div>
                {desc && <div style={{ marginTop: 3, fontSize: 12.5, color: "var(--v-text-2)", lineHeight: 1.5 }}>{desc}</div>}
            </div>
            <VisionToggle on={on} onChange={onChange} />
        </div>
    );
}
