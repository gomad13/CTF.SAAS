"use client";

import { Suspense, useEffect, useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import Link from "next/link";
import { apiFetch } from "@/lib/api";
import { PasswordInput } from "@/components/ui/PasswordInput";
import Reveal from "@/components/Reveal";

// ── Password strength (shared logic) ─────────────────────────────────────────
const PASSWORD_CRITERIA = [
    { label: "8 caractères minimum",        test: (p: string) => p.length >= 8 },
    { label: "Une majuscule (A-Z)",          test: (p: string) => /[A-Z]/.test(p) },
    { label: "Une minuscule (a-z)",          test: (p: string) => /[a-z]/.test(p) },
    { label: "Un chiffre (0-9)",             test: (p: string) => /[0-9]/.test(p) },
    { label: "Un caractère spécial (!@#…)",  test: (p: string) => /[^A-Za-z0-9]/.test(p) },
] as const;

const STRENGTH_LEVELS = [
    { min: 0, max: 0, label: "Très faible", color: "var(--danger)",  pct: "0%"   },
    { min: 1, max: 2, label: "Faible",      color: "var(--warning)", pct: "40%"  },
    { min: 3, max: 3, label: "Moyen",       color: "var(--warning)", pct: "60%"  },
    { min: 4, max: 4, label: "Fort",        color: "var(--pr)",      pct: "80%"  },
    { min: 5, max: 5, label: "Très fort",   color: "var(--pr)",      pct: "100%" },
] as const;

function passwordScore(p: string) {
    return PASSWORD_CRITERIA.filter(c => c.test(p)).length;
}
function getLevel(score: number) {
    return STRENGTH_LEVELS.find(l => score >= l.min && score <= l.max) ?? STRENGTH_LEVELS[0];
}

const cardStyle: React.CSSProperties = {
    width: "100%",
    maxWidth: 460,
    background: "var(--bg-card)",
    border: "1px solid var(--border)",
    borderRadius: 14,
    padding: "clamp(28px, 6vw, 44px)",
    position: "relative",
    overflow: "hidden",
};

const labelStyle: React.CSSProperties = {
    display: "block",
    fontFamily: "'JetBrains Mono', monospace",
    fontSize: 11,
    fontWeight: 600,
    letterSpacing: "0.1em",
    textTransform: "uppercase",
    color: "var(--text-muted)",
    marginBottom: 6,
};

export default function ResetPasswordPage() {
    return (
        <Suspense fallback={<LoadingShell />}>
            <ResetPasswordForm />
        </Suspense>
    );
}

function Shell({ children }: { children: React.ReactNode }) {
    return (
        <div style={{ minHeight: "100vh", display: "flex", alignItems: "center", justifyContent: "center", padding: "24px 16px" }}>
            <div style={{ position: "fixed", left: 16, top: 16 }}>
                <Link href="/login" style={{ display: "inline-flex", alignItems: "center", gap: 6, fontSize: 14, fontWeight: 600, textDecoration: "none", color: "var(--text-muted)" }}>
                    <span aria-hidden>&#x2190;</span>
                    <span>Retour à la connexion</span>
                </Link>
            </div>
            <Reveal>{children}</Reveal>
        </div>
    );
}

function LoadingShell() {
    return (
        <Shell>
            <div style={{ ...cardStyle, textAlign: "center", color: "var(--text-muted)", fontSize: 14 }}>
                Vérification du lien…
            </div>
        </Shell>
    );
}

// ── Main form ─────────────────────────────────────────────────────────────────
function ResetPasswordForm() {
    const router       = useRouter();
    const searchParams = useSearchParams();
    const token        = searchParams.get("token") ?? "";

    const [tokenValid, setTokenValid] = useState<boolean | null>(null); // null = en cours
    const [password, setPassword]     = useState("");
    const [confirm, setConfirm]       = useState("");
    const [loading, setLoading]       = useState(false);
    const [error, setError]           = useState<string | null>(null);
    const [fieldError, setFieldError] = useState<string | null>(null);

    // Validation du token au montage (n'expose rien d'autre que valid: true/false).
    useEffect(() => {
        if (!token) { setTokenValid(false); return; }
        let cancelled = false;
        apiFetch<{ valid: boolean }>(`/api/auth/reset-password/validate?token=${encodeURIComponent(token)}`)
            .then(r => { if (!cancelled) setTokenValid(r.valid); })
            .catch(() => { if (!cancelled) setTokenValid(false); });
        return () => { cancelled = true; };
    }, [token]);

    if (tokenValid === null) return <LoadingShell />;
    if (!tokenValid) return <InvalidToken reason="Ce lien de réinitialisation est invalide, expiré ou a déjà été utilisé." />;

    async function onSubmit(e: React.FormEvent) {
        e.preventDefault();
        setError(null);
        setFieldError(null);

        if (!password) { setFieldError("Le mot de passe est requis."); return; }
        if (passwordScore(password) < PASSWORD_CRITERIA.length) {
            setFieldError("Le mot de passe ne remplit pas tous les critères de sécurité.");
            return;
        }
        if (password !== confirm) { setFieldError("Les mots de passe ne correspondent pas."); return; }

        setLoading(true);
        try {
            await apiFetch<void>("/api/auth/reset-password", {
                method: "POST",
                body: JSON.stringify({ token, newPassword: password }),
            });
            router.push("/login?reset=1");
        } catch (err: unknown) {
            const msg = err instanceof Error ? err.message : "";
            if (msg.includes("expiré") || msg.includes("invalide") || msg.includes("utilisé")) {
                setError("Ce lien est invalide ou a expiré. Demandez un nouveau lien de réinitialisation.");
            } else {
                setError("Une erreur est survenue. Réessayez ou demandez un nouveau lien.");
            }
        } finally {
            setLoading(false);
        }
    }

    const score = passwordScore(password);
    const level = getLevel(score);

    return (
        <Shell>
            <div style={cardStyle}>
                <div aria-hidden style={{ position: "absolute", top: 0, right: 0, width: 48, height: 48, background: "linear-gradient(225deg, var(--accent-border) 0%, transparent 60%)", clipPath: "polygon(100% 0, 0 0, 100% 100%)" }} />

                <div style={{ display: "flex", justifyContent: "center", marginBottom: 16 }}>
                    <span style={{ display: "inline-flex", alignItems: "center", gap: 8, background: "var(--accent-subtle)", border: "1px solid var(--accent-border)", borderRadius: 20, padding: "4px 12px", fontFamily: "'JetBrains Mono', monospace", fontSize: 11, letterSpacing: "0.12em", color: "var(--accent)" }}>
                        <span aria-hidden style={{ width: 6, height: 6, borderRadius: "50%", background: "var(--pr)", boxShadow: "0 0 6px var(--accent)" }} />
                        NOUVEAU MOT DE PASSE
                    </span>
                </div>

                <h1 style={{ fontSize: 24, fontWeight: 700, color: "var(--text-primary)", textAlign: "center", margin: "12px 0 0" }}>
                    Choisir un nouveau mot de passe
                </h1>
                <p style={{ fontSize: 13, color: "var(--text-muted)", textAlign: "center", margin: "8px 0 0", lineHeight: 1.5 }}>
                    Choisissez un mot de passe fort pour s&eacute;curiser votre compte.
                </p>

                <div style={{ height: 1, background: "linear-gradient(90deg, transparent, var(--border), transparent)", margin: "20px 0" }} />

                {error && (
                    <div style={{ background: "rgba(239,68,68,0.08)", border: "1px solid rgba(239,68,68,0.25)", borderRadius: 8, padding: "12px 14px", marginBottom: 16 }}>
                        <p style={{ fontSize: 13, color: "var(--danger-t)", margin: 0 }}>{error}</p>
                        <Link href="/forgot-password" style={{ display: "inline-block", marginTop: 8, fontSize: 13, color: "var(--primary)", textDecoration: "none" }}>
                            Demander un nouveau lien &rarr;
                        </Link>
                    </div>
                )}

                <form onSubmit={onSubmit} noValidate>
                    <div style={{ marginBottom: 16 }}>
                        <label htmlFor="new-password" style={labelStyle}>NOUVEAU MOT DE PASSE</label>
                        <PasswordInput
                            id="new-password"
                            value={password}
                            onChange={e => setPassword(e.target.value)}
                            placeholder="8 caractères minimum"
                            autoComplete="new-password"
                        />
                    </div>

                    {password.length > 0 && (
                        <div style={{ background: "var(--bg-input)", border: "1px solid var(--border)", borderRadius: 8, padding: 12, marginBottom: 16 }}>
                            <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
                                <div style={{ height: 6, flex: 1, overflow: "hidden", borderRadius: 999, background: "var(--border)" }}>
                                    <div style={{ height: "100%", borderRadius: 999, transition: "all 0.3s", width: level.pct, backgroundColor: level.color }} />
                                </div>
                                <span style={{ width: 68, textAlign: "right", fontSize: 12, fontWeight: 600, color: level.color }}>{level.label}</span>
                            </div>
                            <ul style={{ margin: "10px 0 0", padding: 0, listStyle: "none", display: "flex", flexDirection: "column", gap: 4 }}>
                                {PASSWORD_CRITERIA.map(c => {
                                    const ok = c.test(password);
                                    return (
                                        <li key={c.label} style={{ display: "flex", alignItems: "center", gap: 8, fontSize: 12, color: "var(--text-muted)" }}>
                                            <span aria-hidden style={{ color: ok ? "var(--pr)" : "var(--danger)" }}>{ok ? "✓" : "✗"}</span>
                                            <span>{c.label}</span>
                                        </li>
                                    );
                                })}
                            </ul>
                        </div>
                    )}

                    <div style={{ marginBottom: 16 }}>
                        <label htmlFor="confirm-password" style={labelStyle}>CONFIRMER LE MOT DE PASSE</label>
                        <PasswordInput
                            id="confirm-password"
                            value={confirm}
                            onChange={e => setConfirm(e.target.value)}
                            placeholder="Répéter le mot de passe"
                            autoComplete="new-password"
                        />
                    </div>

                    {fieldError && (
                        <div role="alert" style={{ background: "rgba(239,68,68,0.08)", border: "1px solid rgba(239,68,68,0.25)", borderRadius: 7, padding: "10px 14px", color: "var(--danger-t)", fontSize: 13, marginBottom: 16 }}>
                            &#9888; {fieldError}
                        </div>
                    )}

                    <button
                        type="submit"
                        disabled={loading}
                        style={{ width: "100%", background: loading ? "var(--accent-subtle)" : "linear-gradient(135deg, var(--accent), var(--accent-hover))", color: "var(--on-accent)", fontWeight: 700, fontSize: 14, fontFamily: "'JetBrains Mono', monospace", letterSpacing: "0.08em", textTransform: "uppercase", border: "none", borderRadius: 8, padding: "13px 0", minHeight: 44, cursor: loading ? "not-allowed" : "pointer", transition: "all 0.2s" }}
                        onMouseOver={e => { if (!loading) { e.currentTarget.style.boxShadow = "0 0 20px var(--accent-subtle)"; e.currentTarget.style.transform = "translateY(-1px)"; } }}
                        onMouseOut={e => { e.currentTarget.style.boxShadow = "none"; e.currentTarget.style.transform = "none"; }}
                    >
                        {loading ? "[ RÉINITIALISATION... ]" : "Réinitialiser mon mot de passe"}
                    </button>
                </form>
            </div>
        </Shell>
    );
}

function InvalidToken({ reason }: { reason: string }) {
    return (
        <Shell>
            <div style={{ ...cardStyle, textAlign: "center" }}>
                <div aria-hidden style={{ fontSize: 34, marginBottom: 12 }}>&#9888;&#65039;</div>
                <h1 style={{ fontSize: 20, fontWeight: 700, color: "var(--text-primary)", margin: 0 }}>Lien invalide</h1>
                <p style={{ marginTop: 10, fontSize: 13, color: "var(--text-muted)", lineHeight: 1.5 }}>{reason}</p>
                <Link href="/forgot-password" style={{ display: "inline-flex", alignItems: "center", justifyContent: "center", minHeight: 44, marginTop: 18, padding: "0 22px", borderRadius: 8, background: "linear-gradient(135deg, var(--accent), var(--accent-hover))", color: "var(--on-accent)", fontSize: 14, fontWeight: 700, fontFamily: "'JetBrains Mono', monospace", letterSpacing: "0.06em", textTransform: "uppercase", textDecoration: "none" }}>
                    Demander un nouveau lien
                </Link>
            </div>
        </Shell>
    );
}
