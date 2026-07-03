"use client";

import { Suspense, useState, useEffect, useMemo } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import Link from "next/link";
import { PasswordInput } from "@/components/ui/PasswordInput";

const EMAIL_RE = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
const API_BASE = process.env.NEXT_PUBLIC_API_BASE_URL ?? "";

export default function LoginPage() {
    return (
        <Suspense fallback={<div style={{ minHeight: "100vh", background: "var(--bg-base)" }} />}>
            <LoginForm />
        </Suspense>
    );
}

function LoginForm() {
    const router = useRouter();
    const searchParams = useSearchParams();
    const justRegistered = searchParams.get("registered") === "1";
    const justReset = searchParams.get("reset") === "1";
    const ssoError = searchParams.get("error");

    const ssoErrorMsg = useMemo(() => {
        if (!ssoError) return null;
        switch (ssoError) {
            case "google_failed":          return "Échec de la connexion Google — veuillez réessayer.";
            case "microsoft_failed":       return "Échec de la connexion Microsoft — veuillez réessayer.";
            case "no_email":               return "Votre provider SSO n'a pas fourni d'email.";
            case "email_not_verified":     return "Votre email n'est pas vérifié chez votre provider SSO.";
            case "invalid_email":          return "Format d'email invalide retourné par le SSO.";
            case "account_disabled":       return "Votre compte est désactivé. Contactez votre administrateur.";
            case "provisioning_disabled":  return "Le SSO est temporairement désactivé pour votre domaine. Contactez votre administrateur.";
            case "no_tenant_mapping":      return "Aucune organisation rattachée à votre domaine email.";
            default:                       return "Une erreur est survenue lors de la connexion SSO.";
        }
    }, [ssoError]);

    const [email, setEmail] = useState("");
    const [password, setPassword] = useState("");
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);

    // M3 — étape 2FA email (si le compte l'a activée).
    const [twoFaRequired, setTwoFaRequired] = useState(false);
    const [code, setCode] = useState("");
    const [twoFaInfo, setTwoFaInfo] = useState<string | null>(null);

    // Redirection commune après authentification réussie (login direct ou après 2FA).
    function goAfterAuth(redirectTo?: string) {
        const rawReturn = searchParams.get("returnUrl");
        const safeReturn = rawReturn && rawReturn.startsWith("/") && !rawReturn.startsWith("//")
            ? rawReturn : null;
        router.push(safeReturn ?? redirectTo ?? "/dashboard");
    }

    async function onSubmit(e: React.FormEvent) {
        e.preventDefault();
        setError(null);

        const trimmedEmail = email.trim().toLowerCase();
        if (!trimmedEmail) { setError("L'email est requis."); return; }
        if (!EMAIL_RE.test(trimmedEmail)) { setError("Format d'email invalide."); return; }
        if (!password || password.length < 6) { setError("Le mot de passe doit contenir au moins 6 caractères."); return; }

        setLoading(true);
        try {
            const res = await fetch(`${API_BASE}/api/auth/login`, {
                method: "POST",
                credentials: "include",
                headers: { "Content-Type": "application/json", "X-Requested-With": "XMLHttpRequest" },
                body: JSON.stringify({ email: trimmedEmail, password }),
            });

            if (res.ok) {
                const data = await res.json();
                if (data.requiresTwoFactor) {
                    setTwoFaRequired(true);
                    setTwoFaInfo("Un code à 6 chiffres vient de vous être envoyé par email.");
                    return;
                }
                goAfterAuth(data.redirectTo);
            } else if (res.status === 429) {
                setError("Trop de tentatives. Patientez 1 minute.");
            } else {
                setError("Email ou mot de passe incorrect.");
            }
        } catch {
            setError("Impossible de joindre le serveur. Vérifiez votre connexion.");
        } finally {
            setLoading(false);
        }
    }

    async function onVerify2fa(e: React.FormEvent) {
        e.preventDefault();
        setError(null);
        if (!/^\d{6}$/.test(code)) { setError("Entrez le code à 6 chiffres."); return; }
        setLoading(true);
        try {
            const res = await fetch(`${API_BASE}/api/auth/2fa/verify`, {
                method: "POST",
                credentials: "include",
                headers: { "Content-Type": "application/json", "X-Requested-With": "XMLHttpRequest" },
                body: JSON.stringify({ code }),
            });
            if (res.ok) {
                const data = await res.json();
                goAfterAuth(data.redirectTo);
            } else if (res.status === 401) {
                setTwoFaRequired(false);
                setError("Session expirée. Reconnectez-vous.");
            } else {
                const data = await res.json().catch(() => null);
                setError(data?.error ?? "Code invalide.");
            }
        } catch {
            setError("Impossible de joindre le serveur.");
        } finally {
            setLoading(false);
        }
    }

    async function onResend2fa() {
        setError(null);
        setTwoFaInfo(null);
        try {
            const res = await fetch(`${API_BASE}/api/auth/2fa/resend`, {
                method: "POST",
                credentials: "include",
                headers: { "X-Requested-With": "XMLHttpRequest" },
            });
            setTwoFaInfo(res.ok ? "Nouveau code envoyé." : "Impossible de renvoyer le code pour l'instant.");
        } catch {
            setTwoFaInfo("Impossible de renvoyer le code pour l'instant.");
        }
    }

    return (
        <div style={{
            minHeight: "100vh",
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            padding: "24px 16px",
        }}>
            {/* Back link */}
            <div style={{ position: "fixed", left: 16, top: 16 }}>
                <Link href="/landing" style={{ display: "inline-flex", alignItems: "center", gap: 6, fontSize: 14, fontWeight: 600, textDecoration: "none" }}>
                    <span style={{ color: "var(--text-muted)" }}>&#x2190;</span>
                    <span style={{ color: "var(--primary)" }}>CTF</span>
                    <span style={{ color: "var(--text-primary)" }}>SaaS</span>
                </Link>
            </div>

            <div style={{
                width: "100%",
                maxWidth: 420,
                background: "var(--bg-card)",
                border: "1px solid var(--border)",
                borderRadius: 14,
                padding: "clamp(28px, 6vw, 48px)",
                position: "relative",
                overflow: "hidden",
            }}>
                {/* Coin décoratif */}
                <div aria-hidden style={{
                    position: "absolute",
                    top: 0,
                    right: 0,
                    width: 48,
                    height: 48,
                    background: "linear-gradient(225deg, var(--accent-border) 0%, transparent 60%)",
                    clipPath: "polygon(100% 0, 0 0, 100% 100%)",
                }} />

                {/* Badge */}
                <div style={{ display: "flex", justifyContent: "center", marginBottom: 16 }}>
                    <span style={{
                        display: "inline-flex",
                        alignItems: "center",
                        gap: 8,
                        background: "var(--accent-subtle)",
                        border: "1px solid var(--accent-border)",
                        borderRadius: 20,
                        padding: "4px 12px",
                        fontFamily: "'JetBrains Mono', monospace",
                        fontSize: 11,
                        letterSpacing: "0.12em",
                        color: "var(--accent)",
                    }}>
                        <span style={{
                            width: 6,
                            height: 6,
                            borderRadius: "50%",
                            background: "var(--pr)",
                            boxShadow: "0 0 6px var(--accent)",
                            flexShrink: 0,
                        }} />
                        ACC&Egrave;S S&Eacute;CURIS&Eacute;
                    </span>
                </div>

                {/* Titre */}
                <h1 style={{
                    fontSize: 26,
                    fontWeight: 700,
                    color: "var(--text-primary)",
                    textAlign: "center",
                    margin: "16px 0 0",
                }}>
                    Connexion
                </h1>

                {/* Séparateur */}
                <div style={{
                    height: 1,
                    background: "linear-gradient(90deg, transparent, var(--border), transparent)",
                    margin: "20px 0",
                }} />

                {/* Banners */}
                {justRegistered && (
                    <div style={{
                        background: "var(--accent-subtle)",
                        border: "1px solid var(--accent-border)",
                        borderRadius: 7,
                        padding: "10px 14px",
                        fontSize: 13,
                        color: "var(--accent)",
                        textAlign: "center",
                        marginBottom: 18,
                    }}>
                        Compte créé — connectez-vous.
                    </div>
                )}
                {justReset && (
                    <div style={{
                        background: "var(--accent-subtle)",
                        border: "1px solid var(--accent-border)",
                        borderRadius: 7,
                        padding: "10px 14px",
                        fontSize: 13,
                        color: "var(--accent)",
                        textAlign: "center",
                        marginBottom: 18,
                    }}>
                        Mot de passe modifié — connectez-vous.
                    </div>
                )}
                {ssoErrorMsg && (
                    <div style={{
                        background: "rgba(239,68,68,0.10)",
                        border: "1px solid rgba(239,68,68,0.30)",
                        borderRadius: 7,
                        padding: "10px 14px",
                        fontSize: 13,
                        color: "var(--danger-t)",
                        textAlign: "center",
                        marginBottom: 18,
                    }}>
                        {ssoErrorMsg}
                    </div>
                )}

                {/* Error */}
                {error && (
                    <div style={{
                        background: "rgba(239,68,68,0.08)",
                        border: "1px solid rgba(239,68,68,0.25)",
                        borderRadius: 7,
                        padding: "10px 14px",
                        color: "var(--danger-t)",
                        fontSize: 13,
                        marginBottom: 16,
                    }} role="alert">
                        ⚠ {error}
                    </div>
                )}

                {/* M3 — étape de vérification 2FA */}
                {twoFaInfo && twoFaRequired && (
                    <div style={{
                        background: "var(--accent-subtle)",
                        border: "1px solid var(--accent-border)",
                        borderRadius: 7, padding: "10px 14px", fontSize: 13,
                        color: "var(--accent)", textAlign: "center", marginBottom: 16,
                    }}>
                        {twoFaInfo}
                    </div>
                )}

                {twoFaRequired ? (
                    <form onSubmit={onVerify2fa} noValidate>
                        <div style={{ marginBottom: 16 }}>
                            <label htmlFor="code" style={{
                                display: "block", fontFamily: "'JetBrains Mono', monospace",
                                fontSize: 11, fontWeight: 600, letterSpacing: "0.1em",
                                textTransform: "uppercase", color: "var(--text-muted)", marginBottom: 6,
                            }}>
                                CODE REÇU PAR EMAIL
                            </label>
                            <input
                                id="code" inputMode="numeric" autoComplete="one-time-code" maxLength={6}
                                value={code}
                                onChange={e => setCode(e.target.value.replace(/\D/g, "").slice(0, 6))}
                                placeholder="••••••" autoFocus
                                style={{
                                    width: "100%", background: "var(--bg-input)", border: "1px solid var(--border)",
                                    borderRadius: 8, padding: "11px 14px", color: "var(--text-primary)",
                                    fontSize: 22, letterSpacing: "0.4em", textAlign: "center",
                                    fontFamily: "'JetBrains Mono', monospace", outline: "none", boxSizing: "border-box",
                                }}
                            />
                        </div>
                        <button type="submit" disabled={loading} style={{
                            width: "100%", background: loading ? "var(--accent-subtle)" : "linear-gradient(135deg, var(--accent), var(--accent-hover))",
                            color: "#FFFFFF", fontWeight: 700, fontSize: 14, fontFamily: "'JetBrains Mono', monospace",
                            letterSpacing: "0.08em", textTransform: "uppercase", border: "none", borderRadius: 8,
                            padding: "13px 0", minHeight: 44, cursor: loading ? "not-allowed" : "pointer", marginTop: 8,
                        }}>
                            {loading ? "[ VÉRIFICATION... ]" : "Vérifier le code"}
                        </button>
                        <p style={{ textAlign: "center", marginTop: 18, fontSize: 13, color: "var(--text-muted)" }}>
                            <button type="button" onClick={onResend2fa} style={{
                                background: "none", border: "none", color: "var(--primary)",
                                cursor: "pointer", fontSize: 13, fontWeight: 500, padding: 0,
                            }}>
                                Renvoyer le code
                            </button>
                        </p>
                    </form>
                ) : (
                <>
                {/* Form */}
                <form onSubmit={onSubmit} noValidate>
                    <div style={{ marginBottom: 16 }}>
                        <label htmlFor="email" style={{
                            display: "block",
                            fontFamily: "'JetBrains Mono', monospace",
                            fontSize: 11,
                            fontWeight: 600,
                            letterSpacing: "0.1em",
                            textTransform: "uppercase",
                            color: "var(--text-muted)",
                            marginBottom: 6,
                        }}>
                            EMAIL
                        </label>
                        <input
                            id="email"
                            type="email"
                            value={email}
                            onChange={e => setEmail(e.target.value)}
                            placeholder="alice@entreprise.com"
                            autoComplete="email"
                            required
                            style={{
                                width: "100%",
                                background: "var(--bg-input)",
                                border: "1px solid var(--border)",
                                borderRadius: 8,
                                padding: "11px 14px",
                                color: "var(--text-primary)",
                                fontSize: 14,
                                outline: "none",
                                boxSizing: "border-box",
                                transition: "border-color 0.2s, box-shadow 0.2s",
                            }}
                            onFocus={e => { e.currentTarget.style.borderColor = "var(--border-focus)"; e.currentTarget.style.boxShadow = "0 0 0 3px var(--accent-subtle)"; }}
                            onBlur={e => { e.currentTarget.style.borderColor = "var(--border)"; e.currentTarget.style.boxShadow = "none"; }}
                        />
                    </div>

                    <div style={{ marginBottom: 16 }}>
                        <label htmlFor="password" style={{
                            display: "block",
                            fontFamily: "'JetBrains Mono', monospace",
                            fontSize: 11,
                            fontWeight: 600,
                            letterSpacing: "0.1em",
                            textTransform: "uppercase",
                            color: "var(--text-muted)",
                            marginBottom: 6,
                        }}>
                            MOT DE PASSE
                        </label>
                        <PasswordInput
                            id="password"
                            value={password}
                            onChange={e => setPassword(e.target.value)}
                            placeholder="Votre mot de passe"
                            autoComplete="current-password"
                            required
                        />
                        <div style={{ textAlign: "right", marginTop: 6 }}>
                            <Link href="/forgot-password" style={{
                                color: "var(--text-muted)",
                                fontSize: 12,
                                textDecoration: "none",
                                transition: "color 0.15s",
                            }}
                                onMouseOver={e => { e.currentTarget.style.color = "var(--primary)"; }}
                                onMouseOut={e => { e.currentTarget.style.color = "var(--text-muted)"; }}
                            >
                                Mot de passe oublié ?
                            </Link>
                        </div>
                    </div>

                    <button
                        type="submit"
                        disabled={loading}
                        style={{
                            width: "100%",
                            background: loading ? "var(--accent-subtle)" : "linear-gradient(135deg, var(--accent), var(--accent-hover))",
                            color: "#FFFFFF",
                            fontWeight: 700,
                            fontSize: 14,
                            fontFamily: "'JetBrains Mono', monospace",
                            letterSpacing: "0.08em",
                            textTransform: "uppercase",
                            border: "none",
                            borderRadius: 8,
                            padding: "13px 0",
                            minHeight: 44,
                            cursor: loading ? "not-allowed" : "pointer",
                            marginTop: 8,
                            transition: "all 0.2s",
                        }}
                        onMouseOver={e => { if (!loading) { e.currentTarget.style.boxShadow = "0 0 20px var(--accent-subtle)"; e.currentTarget.style.transform = "translateY(-1px)"; } }}
                        onMouseOut={e => { e.currentTarget.style.boxShadow = "none"; e.currentTarget.style.transform = "none"; }}
                    >
                        {loading ? "[ CONNEXION... ]" : "Se connecter"}
                    </button>
                </form>

                <SsoButtons apiBase={API_BASE} />

                <p style={{
                    textAlign: "center",
                    marginTop: 24,
                    fontSize: 13,
                    color: "var(--text-muted)",
                }}>
                    Pas encore de compte ?{" "}
                    <Link href="/register" style={{
                        color: "var(--primary)",
                        textDecoration: "none",
                        fontWeight: 500,
                    }}
                        onMouseOver={e => { e.currentTarget.style.textDecoration = "underline"; }}
                        onMouseOut={e => { e.currentTarget.style.textDecoration = "none"; }}
                    >
                        S&apos;inscrire
                    </Link>
                </p>
                </>
                )}
            </div>
        </div>
    );
}

function SsoButtons({ apiBase }: { apiBase: string }) {
    const [status, setStatus] = useState<{ googleEnabled: boolean; microsoftEnabled: boolean } | null>(null);
    useEffect(() => {
        fetch(`${apiBase}/api/auth/sso-status`)
            .then(r => r.json())
            .then(setStatus)
            .catch(() => setStatus({ googleEnabled: false, microsoftEnabled: false }));
    }, [apiBase]);

    if (status === null) return null;

    // Si aucun SSO n'est configuré on cache complètement la zone "ou continuer avec".
    // Évite l'effet "produit pas fini" sur la page publique de login.
    if (!status.googleEnabled && !status.microsoftEnabled) return null;

    const baseBtn: React.CSSProperties = {
        display: "flex", alignItems: "center", justifyContent: "center", gap: 10,
        width: "100%", padding: "11px 20px", minHeight: 44, borderRadius: 8,
        fontSize: 14, fontWeight: 500, textDecoration: "none",
        transition: "all 0.2s", marginBottom: 10,
    };
    const enabledBtn: React.CSSProperties = {
        ...baseBtn,
        background: "var(--bg-card)", border: "1px solid var(--border)",
        color: "var(--text-primary)", cursor: "pointer",
    };

    return (
        <>
            <div style={{ display: "flex", alignItems: "center", gap: 12, margin: "20px 0" }}>
                <div style={{ flex: 1, height: 1, background: "var(--border)" }} />
                <span style={{ fontSize: 12, color: "var(--text-muted)", padding: "0 4px" }}>ou continuer avec</span>
                <div style={{ flex: 1, height: 1, background: "var(--border)" }} />
            </div>

            {status.googleEnabled && (
                <a href={`${apiBase}/api/auth/oauth/google/challenge`} style={enabledBtn}>
                    <GoogleIcon />
                    Continuer avec Google
                </a>
            )}

            {status.microsoftEnabled && (
                <a href={`${apiBase}/api/auth/oauth/microsoft/challenge`} style={enabledBtn}>
                    <MicrosoftIcon />
                    Continuer avec Microsoft
                </a>
            )}
        </>
    );
}

function GoogleIcon() {
    return (
        <svg width="18" height="18" viewBox="0 0 48 48">
            <path fill="#4285F4" d="M46.98 24.55c0-1.57-.15-3.09-.38-4.55H24v9.02h12.94c-.58 2.96-2.26 5.48-4.78 7.18l7.73 6c4.51-4.18 7.09-10.36 7.09-17.65z"/>
            <path fill="#34A853" d="M24 48c6.48 0 11.93-2.13 15.89-5.81l-7.73-6c-2.15 1.45-4.92 2.3-8.16 2.3-6.26 0-11.57-4.22-13.47-9.91l-7.98 6.19C6.51 42.62 14.62 48 24 48z"/>
            <path fill="#FBBC05" d="M10.53 28.59c-.48-1.45-.76-2.99-.76-4.59s.27-3.14.76-4.59l-7.98-6.19C.92 16.46 0 20.12 0 24c0 3.88.92 7.54 2.56 10.78l7.97-6.19z"/>
            <path fill="#EA4335" d="M24 9.5c3.54 0 6.71 1.22 9.21 3.6l6.85-6.85C35.9 2.38 30.47 0 24 0 14.62 0 6.51 5.38 2.56 13.22l7.98 6.19C12.43 13.72 17.74 9.5 24 9.5z"/>
        </svg>
    );
}
function MicrosoftIcon() {
    return (
        <svg width="18" height="18" viewBox="0 0 23 23">
            <path fill="#f35325" d="M1 1h10v10H1z"/>
            <path fill="#81bc06" d="M12 1h10v10H12z"/>
            <path fill="#05a6f0" d="M1 12h10v10H1z"/>
            <path fill="#ffba08" d="M12 12h10v10H12z"/>
        </svg>
    );
}
