"use client";

import { useEffect, useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { useLegalDocuments } from "@/lib/hooks/useLegalDocuments";
import ConsentSection, {
    initialConsentsState,
    areMandatoryConsentsAccepted,
    type RegistrationConsentsState,
} from "@/components/legal/ConsentSection";
import type { ConsentItem } from "@/lib/types/legal";
import { PasswordInput } from "@/components/ui/PasswordInput";

const API_BASE = process.env.NEXT_PUBLIC_API_BASE_URL ?? "";

export default function RegisterPage() {
    const router = useRouter();
    const [activeTab, setActiveTab] = useState<"demo" | "enterprise">("demo");
    const [firstName, setFirstName] = useState("");
    const [lastName, setLastName] = useState("");
    const [email, setEmail] = useState("");
    const [password, setPassword] = useState("");
    const [confirm, setConfirm] = useState("");
    const [accessCode, setAccessCode] = useState("");
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState("");

    const { documents: legalDocs } = useLegalDocuments();
    const [consents, setConsents] = useState<RegistrationConsentsState>(initialConsentsState);
    const [showConsentsErrors, setShowConsentsErrors] = useState(false);

    // Bêta DSI : on bascule en mode "fermé" si le backend dit que l'inscription publique est désactivée.
    // null = en cours de chargement (on ne flashe pas le formulaire avant la réponse).
    const [registrationOpen, setRegistrationOpen] = useState<boolean | null>(null);
    useEffect(() => {
        let cancelled = false;
        fetch(`${API_BASE}/api/auth/registration-status`, { credentials: "include" })
            .then(r => r.json())
            .then((d: { open?: boolean }) => { if (!cancelled) setRegistrationOpen(Boolean(d?.open)); })
            .catch(() => { if (!cancelled) setRegistrationOpen(false); });
        return () => { cancelled = true; };
    }, []);

    // [MULTI-SOCIETES] Flux QR : token d'invitation -> société verrouillée (preview, non saisie).
    const [inviteToken, setInviteToken] = useState<string | null>(null);
    const [inviteTenantName, setInviteTenantName] = useState<string | null>(null);
    useEffect(() => {
        const t = typeof window !== "undefined" ? new URLSearchParams(window.location.search).get("token") : null;
        if (!t) return;
        setInviteToken(t);
        fetch(`${API_BASE}/api/auth/invite-preview?token=${encodeURIComponent(t)}`, { credentials: "include" })
            .then(r => r.json())
            .then((d: { valid?: boolean; tenantName?: string | null }) => {
                if (d?.valid && d.tenantName) setInviteTenantName(d.tenantName);
            })
            .catch(() => {});
    }, []);

    const getStrength = (pwd: string) => {
        let score = 0;
        if (pwd.length >= 8) score++;
        if (/[A-Z]/.test(pwd)) score++;
        if (/[a-z]/.test(pwd)) score++;
        if (/[0-9]/.test(pwd)) score++;
        if (/[^A-Za-z0-9]/.test(pwd)) score++;
        return score;
    };

    const strength = getStrength(password);
    const strengthLabel = ["", "Très faible", "Faible", "Moyen", "Fort", "Très fort"];
    const strengthColor = ["", "var(--er)", "var(--er)", "var(--wa)", "var(--ok)", "var(--ok)"];

    const consentsOk = useMemo(
        () => areMandatoryConsentsAccepted(consents, true),
        [consents],
    );

    const canSubmit = !loading
        && email && password && firstName && lastName
        && confirm === password && password.length >= 8
        && consentsOk;

    const buildConsentsPayload = (): ConsentItem[] => {
        // On envoie les consentements pour TOUS les documents actifs connus :
        //   - obligatoires (politique, cgu, dpa si admin) avec accepted=true
        //   - mentions-legales : pas obligatoire mais on logue l'acceptation
        //     implicite à la version courante pour traçabilité
        //   - optionnels (newsletter, commercial) : pas des LegalDocuments,
        //     ne sont pas envoyés (pas de versioning RGPD pour ces choix marketing).
        const result: ConsentItem[] = [];
        const polDoc = legalDocs.find(d => d.slug === "politique-confidentialite");
        const cguDoc = legalDocs.find(d => d.slug === "cgu");
        const dpaDoc = legalDocs.find(d => d.slug === "dpa");
        if (polDoc) result.push({ documentSlug: polDoc.slug, documentVersion: polDoc.version, accepted: consents.politiqueConfidentialite });
        if (cguDoc) result.push({ documentSlug: cguDoc.slug, documentVersion: cguDoc.version, accepted: consents.cgu });
        if (dpaDoc) {
            result.push({ documentSlug: dpaDoc.slug, documentVersion: dpaDoc.version, accepted: consents.dpa });
        }
        return result;
    };

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        if (password !== confirm) { setError("Les mots de passe ne correspondent pas."); return; }
        if (password.length < 8) { setError("Mot de passe trop court (8 minimum)."); return; }
        if (!consentsOk) {
            setShowConsentsErrors(true);
            setError("Merci d'accepter les consentements obligatoires avant de poursuivre.");
            return;
        }
        setLoading(true);
        setError("");
        try {
            const res = await fetch(`${API_BASE}/api/auth/register`, {
                method: "POST",
                credentials: "include",
                headers: { "Content-Type": "application/json", "X-Requested-With": "XMLHttpRequest" },
                body: JSON.stringify({
                    email: email.trim().toLowerCase(),
                    password,
                    firstName: firstName.trim(),
                    lastName: lastName.trim(),
                    consents: buildConsentsPayload(),
                    ...(activeTab === "enterprise" && accessCode ? { tenantId: accessCode } : {}),
                }),
            });
            const data = await res.json();
            if (!res.ok) { setError(data.error || data.message || "Erreur lors de la création."); return; }
            // [MULTI-SOCIETES] QR : le cookie d'auth est posé -> rejoindre la société via le token vérifié.
            if (inviteToken) router.push(`/join?token=${encodeURIComponent(inviteToken)}`);
            else router.push("/login?registered=1");
        } catch {
            setError("Impossible de contacter le serveur.");
        } finally {
            setLoading(false);
        }
    };

    // Pendant le chargement de l'état d'inscription, on ne rend rien (évite flash du formulaire).
    if (registrationOpen === null) {
        return (
            <main style={{ minHeight: "100svh", background: "var(--bg-0)" }} aria-busy="true" />
        );
    }

    // Inscription publique fermée (mode bêta DSI par défaut).
    if (registrationOpen === false) {
        return (
            <main style={{ minHeight: "100svh", background: "var(--bg-0)", display: "flex", alignItems: "center", justifyContent: "center", padding: "40px 24px" }}>
                <div style={{
                    maxWidth: 520, width: "100%",
                    background: "var(--bg-card, #1E293B)",
                    border: "1px solid var(--border, #334155)",
                    borderRadius: 14, padding: "clamp(24px, 6vw, 36px)", color: "var(--text-primary, #E2E8F0)",
                }}>
                    <div style={{ fontSize: 11, fontFamily: "'JetBrains Mono', monospace", letterSpacing: "0.12em", textTransform: "uppercase", color: "var(--text-muted, #94A3B8)", marginBottom: 14 }}>
                        Bêta privée
                    </div>
                    <h1 style={{ fontSize: 26, fontWeight: 700, margin: "0 0 16px" }}>
                        Inscription réservée aux organisations partenaires
                    </h1>
                    <p style={{ fontSize: 14, lineHeight: 1.7, color: "var(--text-secondary, #CBD5E1)", margin: "0 0 14px" }}>
                        Sentys est actuellement en bêta fermée. Les comptes sont créés directement par
                        l&apos;administrateur de votre organisation, ou par notre équipe si votre entreprise
                        rejoint le programme partenaire.
                    </p>
                    <p style={{ fontSize: 14, lineHeight: 1.7, color: "var(--text-secondary, #CBD5E1)", margin: "0 0 26px" }}>
                        Vous avez déjà reçu vos identifiants ?{" "}
                        <a href="/login" style={{ color: "var(--pr-l, #60A5FA)", fontWeight: 600 }}>Se connecter</a>.
                    </p>
                    <div style={{ display: "flex", flexDirection: "column", gap: 10 }}>
                        <a
                            href="mailto:contact@sentys.fr?subject=Demande%20d%27acc%C3%A8s%20bêta%20Sentys"
                            style={{
                                display: "inline-flex", alignItems: "center", justifyContent: "center",
                                background: "#3B82F6", color: "#FFFFFF", textDecoration: "none",
                                padding: "12px 20px", minHeight: 44, borderRadius: 10, fontSize: 14, fontWeight: 600,
                            }}
                        >
                            Contacter l&apos;équipe Sentys
                        </a>
                        <a
                            href="/landing"
                            style={{
                                display: "inline-flex", alignItems: "center", justifyContent: "center",
                                background: "transparent", color: "var(--text-secondary, #CBD5E1)",
                                textDecoration: "none", padding: "10px 20px", minHeight: 44, borderRadius: 10,
                                fontSize: 13, border: "1px solid var(--border, #334155)",
                            }}
                        >
                            ← Retour à l&apos;accueil
                        </a>
                    </div>
                </div>
            </main>
        );
    }

    return (
        <div style={{ display: "flex", minHeight: "100svh", background: "var(--bg-0)" }}>
            {/* ── GAUCHE ── */}
            <div className="register-left" style={{
                width: "42%", flexShrink: 0,
                background: "linear-gradient(145deg,#1E40AF,#2563EB 40%,#3B82F6)",
                padding: "40px 48px",
                display: "flex", flexDirection: "column", justifyContent: "space-between",
                position: "relative", overflow: "hidden",
            }}>
                <div style={{ position: "absolute", top: -60, right: -60, width: 280, height: 280, borderRadius: "50%", background: "rgba(255,255,255,0.05)" }} />
                <div style={{ position: "absolute", bottom: -40, left: -40, width: 200, height: 200, borderRadius: "50%", background: "rgba(255,255,255,0.04)" }} />

                <div style={{ position: "relative", zIndex: 1, display: "flex", alignItems: "center", gap: 10 }}>
                    <div style={{ width: 32, height: 32, background: "rgba(255,255,255,0.2)", borderRadius: 8, display: "flex", alignItems: "center", justifyContent: "center", fontSize: 15, fontWeight: 700, color: "white", fontFamily: "'JetBrains Mono', monospace" }}>V</div>
                    <span style={{ fontSize: 17, fontWeight: 700, color: "white" }}>Sentys</span>
                </div>

                <div style={{ position: "relative", zIndex: 1 }}>
                    <h1 style={{ fontSize: 28, fontWeight: 700, color: "#fff", lineHeight: 1.25, margin: 0 }}>
                        Rejoignez la plateforme de formation cyber.
                    </h1>
                    <p style={{ fontSize: 14, color: "rgba(255,255,255,0.70)", marginTop: 16, lineHeight: 1.65 }}>
                        Créez votre compte en quelques secondes et accédez aux simulations immersives de cyberattaques.
                    </p>
                </div>

                <div style={{ fontSize: 11, color: "rgba(255,255,255,0.35)", position: "relative", zIndex: 1 }}>
                    © 2026 Sentys
                </div>
            </div>

            {/* ── DROITE ── */}
            <div className="register-right" style={{
                flex: 1, display: "flex", alignItems: "center", justifyContent: "center",
                padding: "40px 24px", background: "var(--bg-0)",
            }}>
                <div style={{ width: "100%", maxWidth: 420 }}>
                    <h2 style={{ fontSize: 22, fontWeight: 700, color: "var(--tx-1)", margin: "0 0 4px" }}>
                        Créer un compte
                    </h2>
                    <p style={{ fontSize: 13, color: "var(--tx-2)", margin: "0 0 22px" }}>
                        Déjà inscrit ?{" "}
                        <a href="/login" style={{ color: "var(--pr-l)", textDecoration: "none", fontWeight: 500 }}>Se connecter</a>
                    </p>

                    {/* Tabs */}
                    <div className="tabs" style={{ width: "100%", marginBottom: 18 }}>
                        <button className={`tab ${activeTab === "demo" ? "tab-active" : ""}`} onClick={() => setActiveTab("demo")} style={{ flex: 1, minHeight: 44 }}>Compte Demo</button>
                        <button className={`tab ${activeTab === "enterprise" ? "tab-active" : ""}`} onClick={() => setActiveTab("enterprise")} style={{ flex: 1, minHeight: 44 }}>Compte Entreprise</button>
                    </div>

                    {/* Banner */}
                    {activeTab === "demo" && (
                        <div className="alert alert-pr" style={{ marginBottom: 18, fontSize: 12 }}>
                            <span style={{ flexShrink: 0 }}>ℹ</span>
                            Accès gratuit à un parcours de démonstration — aucun code requis.
                        </div>
                    )}
                    {activeTab === "enterprise" && (
                        <div className="alert alert-wa" style={{ marginBottom: 18, fontSize: 12 }}>
                            <span style={{ flexShrink: 0 }}>⚠</span>
                            Votre administrateur vous fournira un code d&apos;accès entreprise.
                        </div>
                    )}

                    <form onSubmit={handleSubmit}>
                        {/* [MULTI-SOCIETES] Société verrouillée issue du QR (non saisie manuellement) */}
                        {inviteToken && inviteTenantName && (
                            <div style={{ marginBottom: 16, padding: "12px 14px", background: "rgba(59,130,246,0.10)", border: "1px solid rgba(59,130,246,0.35)", borderRadius: 8, fontSize: 13, lineHeight: 1.5, color: "var(--text-primary, #E2E8F0)" }}>
                                Vous créez un compte pour rejoindre la société <strong>« {inviteTenantName} »</strong>.
                                Elle sera automatiquement associée à votre compte après l&apos;inscription.
                            </div>
                        )}
                        {/* Prénom + Nom */}
                        <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fit, minmax(150px, 1fr))", gap: 12, marginBottom: 13 }}>
                            <div className="field" style={{ margin: 0 }}>
                                <label className="label">Prénom</label>
                                <input className="input input-md" type="text" placeholder="Alice" value={firstName} onChange={e => setFirstName(e.target.value)} required />
                            </div>
                            <div className="field" style={{ margin: 0 }}>
                                <label className="label">Nom</label>
                                <input className="input input-md" type="text" placeholder="Dupont" value={lastName} onChange={e => setLastName(e.target.value)} required />
                            </div>
                        </div>

                        {/* Email */}
                        <div className="field">
                            <label className="label">Email professionnel</label>
                            <input className="input input-md" type="email" placeholder="vous@entreprise.com" value={email} onChange={e => setEmail(e.target.value)} autoComplete="email" required />
                        </div>

                        {/* Mot de passe */}
                        <div className="field">
                            <label className="label">Mot de passe</label>
                            <PasswordInput
                                className="input input-md"
                                placeholder="8 caractères minimum"
                                value={password}
                                onChange={e => setPassword(e.target.value)}
                                autoComplete="new-password"
                                required
                            />
                            {password.length > 0 && (
                                <div style={{ marginTop: 8 }}>
                                    <div style={{ display: "flex", gap: 3, marginBottom: 5 }}>
                                        {[1, 2, 3, 4, 5].map(i => (
                                            <div key={i} style={{ flex: 1, height: 3, borderRadius: 99, background: i <= strength ? strengthColor[strength] : "var(--bg-4)", transition: "background 0.3s" }} />
                                        ))}
                                    </div>
                                    <div style={{ display: "flex", justifyContent: "flex-end", marginBottom: 6 }}>
                                        <span style={{ fontSize: 11, fontWeight: 600, color: strengthColor[strength] }}>{strengthLabel[strength]}</span>
                                    </div>
                                    <div style={{ display: "flex", flexWrap: "wrap", gap: "4px 12px" }}>
                                        {[
                                            { ok: password.length >= 8, label: "8 caractères minimum" },
                                            { ok: /[a-z]/.test(password), label: "Une minuscule" },
                                            { ok: /[A-Z]/.test(password), label: "Une majuscule" },
                                            { ok: /[0-9]/.test(password), label: "Un chiffre" },
                                            { ok: /[^A-Za-z0-9]/.test(password), label: "Un caractère spécial" },
                                        ].map((c, i) => (
                                            <span key={i} style={{ fontSize: 11, fontWeight: 500, color: c.ok ? "var(--ok)" : "var(--tx-3)" }}>
                                                {c.ok ? "✓" : "○"} {c.label}
                                            </span>
                                        ))}
                                    </div>
                                </div>
                            )}
                        </div>

                        {/* Confirmer */}
                        <div className="field">
                            <label className="label">Confirmer le mot de passe</label>
                            <PasswordInput
                                className={`input input-md ${confirm && confirm !== password ? "input-error" : ""}`}
                                placeholder="Répéter le mot de passe"
                                value={confirm}
                                onChange={e => setConfirm(e.target.value)}
                                autoComplete="new-password"
                                required
                            />
                            {confirm && confirm !== password && <div className="input-hint err">Les mots de passe ne correspondent pas.</div>}
                            {confirm && confirm === password && <div style={{ fontSize: 11, color: "var(--ok-l)", marginTop: 5 }}>✓ Les mots de passe correspondent.</div>}
                        </div>

                        {/* Code entreprise */}
                        {activeTab === "enterprise" && (
                            <div className="field">
                                <label className="label">Code d&apos;accès entreprise</label>
                                <input className="input input-md" type="text" placeholder="XXXX-XXXX-XXXX" value={accessCode} onChange={e => setAccessCode(e.target.value.toUpperCase())} style={{ fontFamily: "'JetBrains Mono', monospace", letterSpacing: "0.08em" }} />
                            </div>
                        )}

                        <ConsentSection
                            documents={legalDocs}
                            isAdmin={true}
                            state={consents}
                            onChange={setConsents}
                            showErrors={showConsentsErrors}
                        />

                        {error && (
                            <div className="alert alert-er" style={{ marginBottom: 14 }}>
                                <span style={{ flexShrink: 0 }}>✕</span>
                                {error}
                            </div>
                        )}

                        <button type="submit" className="btn btn-primary btn-lg btn-full" style={{ marginBottom: 20 }} disabled={!canSubmit}>
                            {loading ? (
                                <span className="spin" style={{ display: "inline-block", width: 16, height: 16, border: "2px solid rgba(255,255,255,0.3)", borderTop: "2px solid white", borderRadius: "50%" }} />
                            ) : activeTab === "demo" ? "Créer mon compte demo →" : "Créer mon compte →"}
                        </button>
                    </form>

                    {/* SSO */}
                    <div style={{ display: "flex", alignItems: "center", gap: 10, marginBottom: 14 }}>
                        <div className="divider" style={{ flex: 1 }} />
                        <span style={{ fontSize: 12, color: "var(--tx-4)", whiteSpace: "nowrap" }}>ou s&apos;inscrire avec</span>
                        <div className="divider" style={{ flex: 1 }} />
                    </div>
                    <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 8 }}>
                        <a href={`${API_BASE}/api/auth/google`} className="btn btn-secondary btn-md" style={{ textDecoration: "none", justifyContent: "center" }}>
                            <svg width="14" height="14" viewBox="0 0 48 48"><path fill="#4285F4" d="M46.98 24.55c0-1.57-.15-3.09-.38-4.55H24v9.02h12.94c-.58 2.96-2.26 5.48-4.78 7.18l7.73 6c4.51-4.18 7.09-10.36 7.09-17.65z" /><path fill="#34A853" d="M24 48c6.48 0 11.93-2.13 15.89-5.81l-7.73-6c-2.15 1.45-4.92 2.3-8.16 2.3-6.26 0-11.57-4.22-13.47-9.91l-7.98 6.19C6.51 42.62 14.62 48 24 48z" /><path fill="#FBBC05" d="M10.53 28.59c-.48-1.45-.76-2.99-.76-4.59s.27-3.14.76-4.59l-7.98-6.19C.92 16.46 0 20.12 0 24c0 3.88.92 7.54 2.56 10.78l7.97-6.19z" /><path fill="#EA4335" d="M24 9.5c3.54 0 6.71 1.22 9.21 3.6l6.85-6.85C35.9 2.38 30.47 0 24 0 14.62 0 6.51 5.38 2.56 13.22l7.98 6.19C12.43 13.72 17.74 9.5 24 9.5z" /></svg>
                            Google
                        </a>
                        <a href={`${API_BASE}/api/auth/microsoft`} className="btn btn-secondary btn-md" style={{ textDecoration: "none", justifyContent: "center" }}>
                            <svg width="14" height="14" viewBox="0 0 23 23"><path fill="#f35325" d="M1 1h10v10H1z" /><path fill="#81bc06" d="M12 1h10v10H12z" /><path fill="#05a6f0" d="M1 12h10v10H1z" /><path fill="#ffba08" d="M12 12h10v10H12z" /></svg>
                            Microsoft
                        </a>
                    </div>
                </div>
            </div>

            <style>{`
                @media (max-width: 768px) {
                    .register-left { display: none !important; }
                    .register-right { padding: 28px 20px !important; align-items: flex-start !important; }
                }
            `}</style>
        </div>
    );
}
