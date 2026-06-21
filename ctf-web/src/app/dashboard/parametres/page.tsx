"use client";

import { useEffect, useState } from "react";
import { useTheme } from "@/hooks/useTheme";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { apiFetch } from "@/lib/api";
import type { Me } from "@/lib/types";
import { useSenderConsent, useUpdateSenderConsent } from "@/lib/hooks/useScenarios";
import { PasswordInput } from "@/components/ui/PasswordInput";

type TabKey = "profil" | "notifications" | "apparence" | "securite";
type ToastType = "success" | "error" | "warning";
type ToastState = { message: string; type: ToastType } | null;

export default function ParametresPage() {
    const [tab, setTab] = useState<TabKey>("profil");
    const [toast, setToast] = useState<ToastState>(null);

    function showToast(message: string, type: ToastType = "success") {
        setToast({ message, type });
        setTimeout(() => setToast(null), 3000);
    }

    const meQ = useQuery<Me>({
        queryKey: ["me"],
        queryFn: () => apiFetch<Me>("/api/auth/me"),
        staleTime: 5 * 60 * 1000,
    });

    const tabs: { key: TabKey; label: string }[] = [
        { key: "profil", label: "Profil" },
        { key: "notifications", label: "Notifications" },
        { key: "apparence", label: "Apparence" },
        { key: "securite", label: "Sécurité" },
    ];

    return (
        <div style={{ maxWidth: 900, margin: "0 auto", padding: "32px var(--page-x) 80px" }}>
            <h1 style={{ fontSize: 24, fontWeight: 700, color: "#F1F5F9", marginBottom: 24 }}>
                Paramètres
            </h1>

            {/* Tabs */}
            <div style={{ display: "flex", gap: 6, marginBottom: 32, borderBottom: "1px solid rgba(255,255,255,0.08)", paddingBottom: 0 }}>
                {tabs.map(t => (
                    <button
                        key={t.key}
                        onClick={() => setTab(t.key)}
                        style={{
                            background: tab === t.key ? "#3B82F6" : "transparent",
                            color: tab === t.key ? "#ffffff" : "#94A3B8",
                            border: "none",
                            padding: "8px 16px",
                            fontSize: 13,
                            fontWeight: 500,
                            borderRadius: 6,
                            cursor: "pointer",
                            transition: "all 0.15s",
                            marginBottom: 8,
                        }}
                        onMouseOver={e => { if (tab !== t.key) e.currentTarget.style.color = "#E2E8F0"; }}
                        onMouseOut={e => { if (tab !== t.key) e.currentTarget.style.color = "#000000"; }}
                    >
                        {t.label}
                    </button>
                ))}
            </div>

            {tab === "profil" && <ProfilTab me={meQ.data} showToast={showToast} />}
            {tab === "notifications" && <NotificationsTab showToast={showToast} />}
            {tab === "apparence" && <ApparenceTab showToast={showToast} />}
            {tab === "securite" && <SecuriteTab me={meQ.data} showToast={showToast} />}

            {toast && <Toast {...toast} onClose={() => setToast(null)} />}
        </div>
    );
}

// ── PROFIL ──────────────────────────────────────────────────────────────────
function ProfilTab({ me, showToast }: { me: Me | undefined; showToast: (m: string, t?: ToastType) => void }) {
    const [firstName, setFirstName] = useState("");
    const [lastName, setLastName] = useState("");
    const [currentPwd, setCurrentPwd] = useState("");
    const [newPwd, setNewPwd] = useState("");
    const [confirmPwd, setConfirmPwd] = useState("");
    const [pwdError, setPwdError] = useState<string | null>(null);

    useEffect(() => {
        if (me) {
            setFirstName(me.firstName ?? "");
            setLastName(me.lastName ?? "");
        }
    }, [me]);

    const initials = `${(me?.firstName?.[0] ?? "").toUpperCase()}${(me?.lastName?.[0] ?? "").toUpperCase()}`;

    const pwdStrength = (() => {
        if (!newPwd) return null;
        if (newPwd.length < 8) return { label: "FAIBLE", color: "#f87171" };
        const hasUpper = /[A-Z]/.test(newPwd);
        const hasNum = /[0-9]/.test(newPwd);
        const hasSpecial = /[^A-Za-z0-9]/.test(newPwd);
        if (newPwd.length >= 12 && hasUpper && hasNum && hasSpecial) return { label: "FORT", color: "#4ade80" };
        if (hasUpper && hasNum) return { label: "MOYEN", color: "#fbbf24" };
        return { label: "FAIBLE", color: "#f87171" };
    })();

    async function handleChangePassword() {
        setPwdError(null);
        if (newPwd !== confirmPwd) {
            setPwdError("Les deux mots de passe ne correspondent pas.");
            return;
        }
        try {
            await apiFetch("/api/auth/change-password", {
                method: "PUT",
                body: JSON.stringify({ currentPassword: currentPwd, newPassword: newPwd }),
            });
            showToast("Mot de passe modifié avec succès");
            setCurrentPwd(""); setNewPwd(""); setConfirmPwd("");
        } catch (e) {
            const msg = e instanceof Error ? e.message : "Erreur lors du changement";
            setPwdError(msg);
            showToast(msg, "error");
        }
    }

    return (
        <div style={{ display: "flex", flexDirection: "column", gap: 32 }}>
            {/* Infos perso */}
            <Card title="Informations personnelles">
                <div style={{ display: "flex", gap: 24, alignItems: "flex-start" }}>
                    <div style={{ display: "flex", flexDirection: "column", alignItems: "center", gap: 10 }}>
                        <div style={{
                            width: 80, height: 80, borderRadius: "50%",
                            background: "var(--bg-elevated)",
                            display: "flex", alignItems: "center", justifyContent: "center",
                            fontSize: 28, fontWeight: 700, color: "var(--text-primary)",
                        }}>{initials}</div>
                        <button style={ghostBtnStyle()}>Modifier</button>
                    </div>
                    <div style={{ flex: 1, display: "flex", flexDirection: "column", gap: 14 }}>
                        <Field label="Prénom" value={firstName} onChange={setFirstName} />
                        <Field label="Nom" value={lastName} onChange={setLastName} />
                        <Field label="Email" value={me?.email ?? ""} disabled />
                        <div>
                            <FieldLabel>Rôle</FieldLabel>
                            <span style={{
                                display: "inline-block",
                                fontSize: 11,
                                padding: "3px 10px",
                                borderRadius: 4,
                                background: "rgba(59,130,246,0.12)",
                                border: "1px solid rgba(59,130,246,0.3)",
                                color: "var(--pr)",
                                fontFamily: "'JetBrains Mono', monospace",
                                textTransform: "uppercase",
                            }}>{me?.role ?? "—"}</span>
                        </div>
                    </div>
                </div>
                <div style={{ marginTop: 20 }}>
                    <button style={primaryBtnStyle()} onClick={() => showToast("Profil mis à jour")}>
                        Enregistrer les modifications
                    </button>
                </div>
            </Card>

            {/* Consentement expéditeur fictif (Pilier 1) */}
            <SenderConsentCard showToast={showToast} />

            {/* Mot de passe */}
            <Card title="Changer le mot de passe">
                <div style={{ display: "flex", flexDirection: "column", gap: 14 }}>
                    <PasswordInput
                        label="Mot de passe actuel"
                        value={currentPwd}
                        onChange={e => setCurrentPwd(e.target.value)}
                        autoComplete="current-password"
                    />
                    <div>
                        <PasswordInput
                            label="Nouveau mot de passe"
                            value={newPwd}
                            onChange={e => setNewPwd(e.target.value)}
                            autoComplete="new-password"
                        />
                        {pwdStrength && (
                            <div style={{ marginTop: 6, fontSize: 11, fontFamily: "'JetBrains Mono', monospace", color: pwdStrength.color, letterSpacing: "0.08em" }}>
                                FORCE : {pwdStrength.label}
                            </div>
                        )}
                    </div>
                    <PasswordInput
                        label="Confirmer le nouveau mot de passe"
                        value={confirmPwd}
                        onChange={e => setConfirmPwd(e.target.value)}
                        autoComplete="new-password"
                    />
                    {pwdError && (
                        <div style={{ fontSize: 12, color: "#f87171" }}>⚠ {pwdError}</div>
                    )}
                </div>
                <div style={{ marginTop: 20 }}>
                    <button style={primaryBtnStyle()} onClick={handleChangePassword}>
                        Changer le mot de passe
                    </button>
                </div>
            </Card>
        </div>
    );
}

// ── CONSENTEMENT EXPÉDITEUR FICTIF (Pilier 1) ───────────────────────────────
function SenderConsentCard({ showToast }: { showToast: (m: string, t?: ToastType) => void }) {
    const { data, isLoading } = useSenderConsent();
    const { update, isLoading: updating } = useUpdateSenderConsent();
    const qc = useQueryClient();

    async function toggle(next: boolean) {
        try {
            await update(next);
            qc.invalidateQueries({ queryKey: ["sender-consent", "me"] });
            showToast(next ? "Consentement activé" : "Consentement désactivé");
        } catch {
            showToast("Erreur lors de la mise à jour", "error");
        }
    }

    return (
        <Card title="Sensibilisation phishing — Consentement expéditeur">
            <p style={{ fontSize: 13, color: "var(--text-secondary)", lineHeight: 1.6, marginTop: 0 }}>
                {"Pour rendre les exercices de sensibilisation plus crédibles, ton administrateur peut utiliser "}<strong>{"ton prénom et ton nom"}</strong>{" comme expéditeur fictif d'un email simulé envoyé à un autre collègue. Aucun email réel ne part de ton adresse, aucune donnée personnelle n'est exposée."}
            </p>
            <p style={{ fontSize: 12, color: "var(--text-muted)", lineHeight: 1.6, marginTop: 8 }}>
                Tu peux activer ou désactiver ce consentement à tout moment.
            </p>
            <div style={{ marginTop: 12 }}>
                <Toggle
                    label="J'accepte de servir d'expéditeur fictif"
                    desc="Mon prénom + nom peuvent apparaître dans le 'De' d'un email de sensibilisation envoyé à un collègue"
                    on={!!data?.consentsToBeFictionalSender}
                    onChange={toggle}
                />
                {(isLoading || updating) && <div style={{ marginTop: 8, fontSize: 12, color: "var(--text-muted)" }}>…</div>}
            </div>
        </Card>
    );
}

// ── NOTIFICATIONS ───────────────────────────────────────────────────────────
function NotificationsTab({ showToast }: { showToast: (m: string, t?: ToastType) => void }) {
    const defaults = {
        emailReminders: true,
        emailResults: true,
        emailNewPaths: false,
        inAppProgress: true,
        inAppSession: false,
    };
    const [prefs, setPrefs] = useState(defaults);

    useEffect(() => {
        try {
            const stored = localStorage.getItem("notif_prefs");
            if (stored) setPrefs({ ...defaults, ...JSON.parse(stored) });
        } catch { /* ignore */ }
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);

    function save() {
        localStorage.setItem("notif_prefs", JSON.stringify(prefs));
        showToast("Préférences enregistrées");
    }

    return (
        <div style={{ display: "flex", flexDirection: "column", gap: 32 }}>
            <Card title="Emails">
                <Toggle label="Recevoir les rappels de formation" desc="Un email hebdomadaire si aucune progression"
                    on={prefs.emailReminders} onChange={v => setPrefs({ ...prefs, emailReminders: v })} />
                <Toggle label="Recevoir les résultats de modules" desc="Un email après chaque module complété"
                    on={prefs.emailResults} onChange={v => setPrefs({ ...prefs, emailResults: v })} />
                <Toggle label="Nouveaux parcours disponibles" desc="Notification lors de l'ajout d'un nouveau parcours"
                    on={prefs.emailNewPaths} onChange={v => setPrefs({ ...prefs, emailNewPaths: v })} />
            </Card>

            <Card title="Alertes in-app">
                <Toggle label="Notifications de progression" desc="Affichage des progressions dans l'app"
                    on={prefs.inAppProgress} onChange={v => setPrefs({ ...prefs, inAppProgress: v })} />
                <Toggle label="Rappels de session" desc="Alertes avant expiration de session"
                    on={prefs.inAppSession} onChange={v => setPrefs({ ...prefs, inAppSession: v })} />
            </Card>

            <button style={primaryBtnStyle()} onClick={save}>Enregistrer les préférences</button>
        </div>
    );
}

// ── APPARENCE ───────────────────────────────────────────────────────────────
function ApparenceTab({ showToast }: { showToast: (m: string, t?: ToastType) => void }) {
    const { theme, setTheme, toggleTheme } = useTheme();
    const [lang, setLang] = useState("fr");
    const [size, setSize] = useState<"small" | "normal" | "large">("normal");

    useEffect(() => {
        const l = localStorage.getItem("lang"); if (l) setLang(l);
        const s = localStorage.getItem("text_size") as "small"|"normal"|"large"|null;
        if (s) setSize(s);
    }, []);

    function apply() {
        localStorage.setItem("lang", lang);
        localStorage.setItem("text_size", size);
        const px = size === "small" ? "13px" : size === "large" ? "17px" : "15px";
        document.documentElement.style.fontSize = px;
        showToast("Apparence mise à jour");
    }

    return (
        <div style={{ display: "flex", flexDirection: "column", gap: 32 }}>
            <Card title="Interface">
                <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 12, maxWidth: 480 }}>
                    {/* Carte sombre */}
                    <button
                        onClick={() => setTheme("dark")}
                        style={{
                            background: theme === "dark" ? "var(--primary-bg)" : "var(--bg-card)",
                            border: theme === "dark" ? "2px solid var(--primary)" : "2px solid var(--border)",
                            borderRadius: 12,
                            padding: 16,
                            cursor: "pointer",
                            transition: "all 0.2s",
                            textAlign: "left",
                            position: "relative",
                            color: "var(--text-primary)",
                        }}
                    >
                        <div style={{
                            background: "var(--bg-base)",
                            borderRadius: 8,
                            padding: 10,
                            marginBottom: 12,
                            height: 80,
                        }}>
                            <div style={{ background: "var(--bg-card)", borderRadius: 4, height: 12, width: "60%", marginBottom: 6 }} />
                            <div style={{ background: "var(--pr)", borderRadius: 4, height: 8, width: "40%", marginBottom: 6 }} />
                            <div style={{ background: "var(--bg-card-hover)", borderRadius: 4, height: 24, width: "80%" }} />
                        </div>
                        <div style={{ fontWeight: 500, fontSize: 13 }}>🌙 Sombre</div>
                        <div style={{ color: "var(--text-muted)", fontSize: 11, marginTop: 2 }}>Recommandé</div>
                        {theme === "dark" && (
                            <div style={{ position: "absolute", top: 8, right: 8, color: "var(--primary)", fontSize: 16 }}>✓</div>
                        )}
                    </button>

                    {/* Carte claire */}
                    <button
                        onClick={() => setTheme("light")}
                        style={{
                            background: theme === "light" ? "var(--primary-bg)" : "var(--bg-card)",
                            border: theme === "light" ? "2px solid var(--primary)" : "2px solid var(--border)",
                            borderRadius: 12,
                            padding: 16,
                            cursor: "pointer",
                            transition: "all 0.2s",
                            textAlign: "left",
                            position: "relative",
                            color: "var(--text-primary)",
                        }}
                    >
                        <div style={{
                            background: "#f8fafc",
                            borderRadius: 8,
                            padding: 10,
                            marginBottom: 12,
                            height: 80,
                        }}>
                            <div style={{ background: "#e2e8f0", borderRadius: 4, height: 12, width: "60%", marginBottom: 6 }} />
                            <div style={{ background: "var(--pr)", borderRadius: 4, height: 8, width: "40%", marginBottom: 6 }} />
                            <div style={{ background: "#ffffff", borderRadius: 4, height: 24, width: "80%", border: "1px solid #e2e8f0" }} />
                        </div>
                        <div style={{ fontWeight: 500, fontSize: 13 }}>☀️ Clair</div>
                        <div style={{ color: "var(--text-muted)", fontSize: 11, marginTop: 2 }}>Mode jour</div>
                        {theme === "light" && (
                            <div style={{ position: "absolute", top: 8, right: 8, color: "var(--primary)", fontSize: 16 }}>✓</div>
                        )}
                    </button>
                </div>

                <div style={{ marginTop: 16, display: "flex", alignItems: "center", gap: 12 }}>
                    <span style={{ fontSize: 13, color: "var(--text-muted)" }}>Changer rapidement :</span>
                    <button
                        onClick={toggleTheme}
                        style={{
                            background: "var(--bg-elevated)",
                            border: "1px solid var(--border)",
                            borderRadius: 20,
                            padding: "6px 16px",
                            color: "var(--text-primary)",
                            fontSize: 13,
                            cursor: "pointer",
                            display: "flex",
                            alignItems: "center",
                            gap: 6,
                        }}
                    >
                        {theme === "dark" ? "☀️ Passer en clair" : "🌙 Passer en sombre"}
                    </button>
                </div>
            </Card>

            <Card title="Langue">
                <select value={lang} onChange={e => setLang(e.target.value)} style={{
                    width: "100%",
                    background: "var(--bg-card)",
                    border: "1px solid var(--border)",
                    borderRadius: 8,
                    padding: "10px 14px",
                    color: "var(--text-primary)",
                    fontSize: 14,
                    outline: "none",
                }}>
                    <option value="fr">Français</option>
                    <option value="en">English</option>
                    <option value="es">Español</option>
                </select>
            </Card>

            <Card title="Taille du texte">
                <div style={{ display: "flex", gap: 10 }}>
                    {(["small","normal","large"] as const).map(s => {
                        const labels = { small: "Petit (13px)", normal: "Normal (15px)", large: "Grand (17px)" };
                        const active = size === s;
                        return (
                            <button key={s} onClick={() => setSize(s)} style={{
                                flex: 1,
                                padding: "10px 14px",
                                background: active ? "rgba(59,130,246,0.12)" : "var(--bg-card)",
                                border: active ? "1px solid #3B82F6" : "1px solid var(--border)",
                                borderRadius: 8,
                                color: active ? "var(--pr)" : "#a3a3a3",
                                fontSize: 13,
                                cursor: "pointer",
                            }}>{labels[s]}</button>
                        );
                    })}
                </div>
            </Card>

            <button style={primaryBtnStyle()} onClick={apply}>Appliquer</button>
        </div>
    );
}

// ── SÉCURITÉ ────────────────────────────────────────────────────────────────
function SecuriteTab({ me, showToast }: { me: Me | undefined; showToast: (m: string, t?: ToastType) => void }) {
    const [showDelete, setShowDelete] = useState(false);
    const [confirmEmail, setConfirmEmail] = useState("");

    async function logoutAll() {
        try {
            await apiFetch("/api/auth/logout-all", { method: "POST" });
            showToast("Autres sessions déconnectées");
        } catch {
            showToast("Erreur lors de la déconnexion", "error");
        }
    }

    const history = [
        { date: "Aujourd'hui 09h", device: "Chrome / Windows", status: "Succès" },
        { date: "Hier 22h",        device: "Chrome / Windows", status: "Succès" },
        { date: "05/04 14h",       device: "Firefox / Mac",    status: "Succès" },
        { date: "04/04 08h",       device: "Chrome / Windows", status: "Succès" },
        { date: "03/04 19h",       device: "Chrome / Windows", status: "Succès" },
    ];

    return (
        <div style={{ display: "flex", flexDirection: "column", gap: 32 }}>
            {/* Sessions */}
            <Card title="Sessions actives">
                <div style={{
                    background: "var(--bg-card)",
                    border: "1px solid var(--border)",
                    borderRadius: 8,
                    padding: 16,
                    marginBottom: 16,
                }}>
                    <div style={{ fontSize: 14, color: "var(--text-primary)", fontWeight: 500, marginBottom: 8 }}>
                        ◉ Navigateur web
                    </div>
                    <div style={{ fontSize: 12, color: "var(--text-secondary)", marginBottom: 4 }}>
                        Dernière connexion : {me?.email ? "il y a quelques instants" : "—"}
                    </div>
                    <span style={{
                        display: "inline-block",
                        fontSize: 10,
                        padding: "2px 8px",
                        borderRadius: 4,
                        background: "rgba(34,197,94,0.10)",
                        border: "1px solid rgba(34,197,94,0.35)",
                        color: "#4ade80",
                        fontFamily: "'JetBrains Mono', monospace",
                        textTransform: "uppercase",
                    }}>Session actuelle</span>
                </div>
                <button style={ghostBtnStyle()} onClick={logoutAll}>
                    Déconnecter tous les autres appareils
                </button>
            </Card>

            {/* M3 — Double authentification par email */}
            <TwoFactorCard showToast={showToast} />

            {/* 2FA — masqué pour la bêta privée tant que la fonctionnalité n'est pas livrée */}
            {/* TODO V1 publique : implémenter le flow TOTP (générer secret, QR code, vérification 6 chiffres). */}

            {/* Historique */}
            <Card title="Historique des connexions">
                <div style={{ display: "flex", flexDirection: "column", gap: 0 }}>
                    {history.map((h, i) => (
                        <div key={i} style={{
                            display: "grid",
                            gridTemplateColumns: "1fr 1.5fr 0.6fr",
                            gap: 12,
                            padding: "10px 0",
                            borderBottom: i < history.length - 1 ? "1px solid var(--border)" : "none",
                            fontSize: 13,
                        }}>
                            <span style={{ color: "var(--text-secondary)", fontFamily: "'JetBrains Mono', monospace" }}>{h.date}</span>
                            <span style={{ color: "var(--text-primary)" }}>{h.device}</span>
                            <span style={{ color: "#4ade80", textAlign: "right" }}>✓ {h.status}</span>
                        </div>
                    ))}
                </div>
                <p style={{ fontSize: 11, color: "var(--text-muted)", marginTop: 12, fontStyle: "italic" }}>
                    Historique des 30 derniers jours
                </p>
            </Card>

            {/*
                Suppression de compte (RGPD Article 17) — masquée pour la bêta privée.
                Le bouton existait avec un toast "non implémenté" → faute UX + risque RGPD si réclamation.
                Pour la bêta : suppression sur demande email (procédure manuelle SuperAdmin).
                TODO V1 publique : DELETE /api/users/me avec anonymisation des soumissions/progresses.
            */}

            {showDelete && (
                <div style={{
                    position: "fixed", inset: 0, background: "rgba(0,0,0,0.7)",
                    display: "flex", alignItems: "center", justifyContent: "center",
                    zIndex: 1000, padding: 16,
                }} onClick={() => setShowDelete(false)}>
                    <div onClick={e => e.stopPropagation()} style={{
                        background: "var(--bg-card)",
                        border: "1px solid rgba(239,68,68,0.4)",
                        borderRadius: 12,
                        padding: 24,
                        maxWidth: 440,
                        width: "100%",
                    }}>
                        <h3 style={{ fontSize: 18, fontWeight: 700, color: "var(--text-primary)", marginBottom: 10 }}>
                            Êtes-vous sûr ?
                        </h3>
                        <p style={{ fontSize: 13, color: "var(--text-secondary)", lineHeight: 1.6, marginBottom: 16 }}>
                            Tapez votre email <strong style={{ color: "var(--text-primary)" }}>{me?.email ?? ""}</strong> pour confirmer la suppression.
                        </p>
                        <input
                            type="email"
                            value={confirmEmail}
                            onChange={e => setConfirmEmail(e.target.value)}
                            placeholder="votre@email.com"
                            style={{
                                width: "100%",
                                background: "var(--bg-input)",
                                border: "1px solid var(--border)",
                                borderRadius: 8,
                                padding: "11px 14px",
                                color: "var(--text-primary)",
                                fontSize: 14,
                                outline: "none",
                                marginBottom: 16,
                            }}
                        />
                        <div style={{ display: "flex", gap: 10, justifyContent: "flex-end" }}>
                            <button onClick={() => setShowDelete(false)} style={ghostBtnStyle()}>Annuler</button>
                            <button
                                disabled={confirmEmail !== me?.email}
                                onClick={() => { showToast("Suppression non implémentée", "warning"); setShowDelete(false); }}
                                style={{
                                    background: confirmEmail === me?.email ? "#ef4444" : "rgba(239,68,68,0.3)",
                                    border: "none",
                                    color: "var(--text-primary)",
                                    padding: "10px 18px",
                                    borderRadius: 7,
                                    fontSize: 13,
                                    fontWeight: 600,
                                    cursor: confirmEmail === me?.email ? "pointer" : "not-allowed",
                                }}
                            >
                                Confirmer la suppression
                            </button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
}

// ── COMPONENTS UTILS ────────────────────────────────────────────────────────
function TwoFactorCard({ showToast }: { showToast: (m: string, t?: ToastType) => void }) {
    const [enabled, setEnabled] = useState<boolean | null>(null);
    const [step, setStep] = useState<"idle" | "confirming">("idle");
    const [code, setCode] = useState("");
    const [busy, setBusy] = useState(false);

    useEffect(() => {
        apiFetch<{ enabled: boolean }>("/api/auth/2fa/status")
            .then(d => setEnabled(d.enabled))
            .catch(() => setEnabled(false));
    }, []);

    async function startEnable() {
        setBusy(true);
        try {
            await apiFetch("/api/auth/2fa/enable", { method: "POST" });
            setStep("confirming");
            showToast("Code de confirmation envoyé par email");
        } catch (e) {
            showToast(e instanceof Error ? e.message : "Erreur", "error");
        } finally { setBusy(false); }
    }

    async function confirmEnable() {
        if (!/^\d{6}$/.test(code)) { showToast("Entrez le code à 6 chiffres", "warning"); return; }
        setBusy(true);
        try {
            await apiFetch("/api/auth/2fa/confirm", { method: "POST", body: JSON.stringify({ code }) });
            setEnabled(true); setStep("idle"); setCode("");
            showToast("Double authentification activée");
        } catch (e) {
            showToast(e instanceof Error ? e.message : "Code invalide", "error");
        } finally { setBusy(false); }
    }

    async function disable() {
        if (!confirm("Désactiver la double authentification par email ?")) return;
        setBusy(true);
        try {
            await apiFetch("/api/auth/2fa/disable", { method: "POST" });
            setEnabled(false); setStep("idle");
            showToast("Double authentification désactivée");
        } catch (e) {
            showToast(e instanceof Error ? e.message : "Erreur", "error");
        } finally { setBusy(false); }
    }

    return (
        <Card title="Double authentification (2FA) par email">
            <p style={{ fontSize: 13, color: "var(--text-secondary)", lineHeight: 1.6, marginTop: 0 }}>
                {"Renforcez la sécurité de votre compte : à chaque connexion par mot de passe, un code à 6 chiffres vous sera envoyé par email."}
            </p>

            {enabled === null && <div style={{ fontSize: 12, color: "var(--text-muted)" }}>…</div>}

            {enabled === false && step === "idle" && (
                <button style={ghostBtnStyle()} onClick={startEnable} disabled={busy}>
                    Activer la 2FA par email
                </button>
            )}

            {enabled === false && step === "confirming" && (
                <div style={{ display: "flex", flexDirection: "column", gap: 10, maxWidth: 280 }}>
                    <div style={{ fontSize: 13, color: "var(--text-secondary)" }}>
                        Entrez le code reçu par email pour confirmer :
                    </div>
                    <input
                        inputMode="numeric" maxLength={6} value={code}
                        onChange={e => setCode(e.target.value.replace(/\D/g, "").slice(0, 6))}
                        placeholder="••••••"
                        style={{
                            background: "var(--bg-input)", border: "1px solid var(--border)", borderRadius: 8,
                            padding: "10px 14px", color: "var(--text-primary)", fontSize: 18,
                            letterSpacing: "0.3em", textAlign: "center", fontFamily: "'JetBrains Mono', monospace",
                        }}
                    />
                    <div style={{ display: "flex", gap: 8 }}>
                        <button style={ghostBtnStyle()} onClick={confirmEnable} disabled={busy}>Confirmer</button>
                        <button style={ghostBtnStyle()} onClick={() => { setStep("idle"); setCode(""); }} disabled={busy}>Annuler</button>
                    </div>
                </div>
            )}

            {enabled === true && (
                <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", gap: 16, flexWrap: "wrap" }}>
                    <span style={{
                        display: "inline-block", fontSize: 10, padding: "2px 8px", borderRadius: 4,
                        background: "rgba(34,197,94,0.10)", border: "1px solid rgba(34,197,94,0.35)",
                        color: "#4ade80", fontFamily: "'JetBrains Mono', monospace", textTransform: "uppercase",
                    }}>Activée</span>
                    <button style={ghostBtnStyle()} onClick={disable} disabled={busy}>Désactiver</button>
                </div>
            )}
        </Card>
    );
}

function Card({ title, children }: { title: string; children: React.ReactNode }) {
    return (
        <div style={{
            background: "var(--bg-card)",
            border: "1px solid var(--border)",
            borderRadius: 10,
            padding: 24,
        }}>
            <h2 style={{ fontSize: 15, fontWeight: 600, color: "var(--text-primary)", marginBottom: 18 }}>{title}</h2>
            {children}
        </div>
    );
}

function FieldLabel({ children }: { children: React.ReactNode }) {
    return (
        <label style={{
            display: "block",
            fontFamily: "'JetBrains Mono', monospace",
            fontSize: 10,
            fontWeight: 600,
            letterSpacing: "0.1em",
            textTransform: "uppercase",
            color: "var(--text-muted)",
            marginBottom: 6,
        }}>{children}</label>
    );
}

function Field({ label, value, onChange, type = "text", disabled = false, autoComplete }: {
    label: string;
    value: string;
    onChange?: (v: string) => void;
    type?: string;
    disabled?: boolean;
    autoComplete?: string;
}) {
    return (
        <div>
            <FieldLabel>{label}</FieldLabel>
            <input
                type={type}
                value={value}
                onChange={e => onChange?.(e.target.value)}
                disabled={disabled}
                autoComplete={autoComplete}
                style={{
                    width: "100%",
                    background: "var(--bg-input)",
                    border: "1px solid var(--border)",
                    borderRadius: 8,
                    padding: "10px 14px",
                    color: disabled ? "var(--text-muted)" : "var(--text-primary)",
                    fontSize: 14,
                    outline: "none",
                    boxSizing: "border-box",
                    transition: "border-color 0.2s, box-shadow 0.2s",
                }}
                onFocus={e => {
                    if (!disabled) {
                        e.currentTarget.style.borderColor = "var(--border-focus)";
                        e.currentTarget.style.boxShadow = "0 0 0 3px rgba(59,130,246,0.1)";
                    }
                }}
                onBlur={e => {
                    e.currentTarget.style.borderColor = "var(--border)";
                    e.currentTarget.style.boxShadow = "none";
                }}
            />
        </div>
    );
}

function Toggle({ label, desc, on, onChange }: { label: string; desc?: string; on: boolean; onChange: (v: boolean) => void }) {
    return (
        <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", padding: "12px 0", borderBottom: "1px solid var(--border)" }}>
            <div style={{ flex: 1, marginRight: 16 }}>
                <div style={{ fontSize: 14, color: "var(--text-primary)", fontWeight: 500 }}>{label}</div>
                {desc && <div style={{ fontSize: 12, color: "var(--text-muted)", marginTop: 2 }}>{desc}</div>}
            </div>
            <button
                role="switch"
                aria-checked={on}
                onClick={() => onChange(!on)}
                style={{
                    width: 44,
                    height: 24,
                    borderRadius: 12,
                    background: on ? "var(--pr)" : "var(--bg-elevated)",
                    border: "1px solid rgba(255,255,255,0.1)",
                    position: "relative",
                    cursor: "pointer",
                    transition: "background 0.2s",
                    flexShrink: 0,
                }}
            >
                <span style={{
                    position: "absolute",
                    top: 2,
                    left: on ? 22 : 2,
                    width: 18,
                    height: 18,
                    borderRadius: "50%",
                    background: "#ffffff",
                    transition: "left 0.2s",
                }} />
            </button>
        </div>
    );
}

function Toast({ message, type, onClose }: { message: string; type: ToastType; onClose: () => void }) {
    const colors = {
        success: { border: "#10B981", icon: "✓", iconColor: "#4ade80" },
        error:   { border: "#ef4444", icon: "✗", iconColor: "#f87171" },
        warning: { border: "#f59e0b", icon: "⚠", iconColor: "#fbbf24" },
    }[type];

    return (
        <div style={{
            position: "fixed",
            bottom: 24,
            right: 24,
            zIndex: 999,
            background: "var(--bg-card)",
            borderLeft: `3px solid ${colors.border}`,
            border: "1px solid var(--border)",
            borderLeftWidth: 3,
            borderLeftColor: colors.border,
            borderRadius: 8,
            padding: "14px 18px",
            display: "flex",
            alignItems: "center",
            gap: 12,
            minWidth: "min(280px, calc(100vw - 24px))",
            maxWidth: "calc(100vw - 24px)",
            boxShadow: "0 8px 24px rgba(0,0,0,0.4)",
            animation: "slideUp 300ms ease",
        }}>
            <span style={{ color: colors.iconColor, fontSize: 16, fontWeight: 700 }}>{colors.icon}</span>
            <span style={{ flex: 1, fontSize: 13, color: "var(--text-primary)" }}>{message}</span>
            <button onClick={onClose} style={{
                background: "none", border: "none", color: "var(--text-muted)", cursor: "pointer",
                fontSize: 18, padding: 0, lineHeight: 1,
            }}>×</button>
        </div>
    );
}

function primaryBtnStyle(): React.CSSProperties {
    return {
        background: "linear-gradient(135deg, #3B82F6, #2563EB)",
        color: "var(--text-primary)",
        border: "none",
        padding: "11px 20px",
        borderRadius: 8,
        fontSize: 13,
        fontWeight: 600,
        cursor: "pointer",
        transition: "box-shadow 0.2s",
    };
}

function ghostBtnStyle(): React.CSSProperties {
    return {
        background: "transparent",
        border: "1px solid var(--border)",
        color: "var(--text-secondary)",
        padding: "8px 14px",
        borderRadius: 7,
        fontSize: 12,
        cursor: "pointer",
        transition: "all 0.15s",
    };
}
