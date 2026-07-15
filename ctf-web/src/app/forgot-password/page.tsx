"use client";

import { useState } from "react";
import Link from "next/link";
import { apiFetch } from "@/lib/api";
import Reveal from "@/components/Reveal";

const EMAIL_RE = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

const cardStyle: React.CSSProperties = {
    width: "100%",
    maxWidth: 440,
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

export default function ForgotPasswordPage() {
    const [email, setEmail]     = useState("");
    const [loading, setLoading] = useState(false);
    const [sent, setSent]       = useState(false);
    const [error, setError]     = useState<string | null>(null);

    async function onSubmit(e: React.FormEvent) {
        e.preventDefault();
        setError(null);

        const trimmed = email.trim().toLowerCase();
        if (!trimmed) { setError("L'email est requis."); return; }
        if (!EMAIL_RE.test(trimmed)) { setError("Format d'email invalide."); return; }

        setLoading(true);
        try {
            await apiFetch<void>("/api/auth/forgot-password", {
                method: "POST",
                body: JSON.stringify({ email: trimmed }),
            });
            setSent(true);
        } catch {
            // Succès affiché quoi qu'il arrive (anti-énumération de comptes).
            setSent(true);
        } finally {
            setLoading(false);
        }
    }

    return (
        <div style={{ minHeight: "100vh", display: "flex", alignItems: "center", justifyContent: "center", padding: "24px 16px" }}>
            <div style={{ position: "fixed", left: 16, top: 16 }}>
                <Link href="/login" style={{ display: "inline-flex", alignItems: "center", gap: 6, fontSize: 14, fontWeight: 600, textDecoration: "none", color: "var(--text-muted)" }}>
                    <span aria-hidden>&#x2190;</span>
                    <span>Retour à la connexion</span>
                </Link>
            </div>

            <Reveal>
            <div style={cardStyle}>
                <div aria-hidden style={{ position: "absolute", top: 0, right: 0, width: 48, height: 48, background: "linear-gradient(225deg, var(--accent-border) 0%, transparent 60%)", clipPath: "polygon(100% 0, 0 0, 100% 100%)" }} />

                <div style={{ display: "flex", justifyContent: "center", marginBottom: 16 }}>
                    <span style={{ display: "inline-flex", alignItems: "center", gap: 8, background: "var(--accent-subtle)", border: "1px solid var(--accent-border)", borderRadius: 20, padding: "4px 12px", fontFamily: "'JetBrains Mono', monospace", fontSize: 11, letterSpacing: "0.12em", color: "var(--accent)" }}>
                        <span aria-hidden style={{ width: 6, height: 6, borderRadius: "50%", background: "var(--pr)", boxShadow: "0 0 6px var(--accent)" }} />
                        R&Eacute;INITIALISATION
                    </span>
                </div>

                <h1 style={{ fontSize: 24, fontWeight: 700, color: "var(--text-primary)", textAlign: "center", margin: "12px 0 0" }}>
                    Mot de passe oubli&eacute;
                </h1>
                <p style={{ fontSize: 13, color: "var(--text-muted)", textAlign: "center", margin: "8px 0 0", lineHeight: 1.5 }}>
                    Saisissez votre email&nbsp;: si un compte existe, vous recevrez un lien de r&eacute;initialisation.
                </p>

                <div style={{ height: 1, background: "linear-gradient(90deg, transparent, var(--border), transparent)", margin: "20px 0" }} />

                {sent ? (
                    <SuccessBanner email={email.trim().toLowerCase()} />
                ) : (
                    <form onSubmit={onSubmit} noValidate>
                        <div style={{ marginBottom: 16 }}>
                            <label htmlFor="email" style={labelStyle}>EMAIL</label>
                            <input
                                id="email"
                                type="email"
                                value={email}
                                onChange={e => setEmail(e.target.value)}
                                placeholder="alice@entreprise.com"
                                autoComplete="email"
                                required
                                style={{ width: "100%", background: "var(--bg-input)", border: "1px solid var(--border)", borderRadius: 8, padding: "11px 14px", color: "var(--text-primary)", fontSize: 14, outline: "none", boxSizing: "border-box", transition: "border-color 0.2s, box-shadow 0.2s" }}
                                onFocus={e => { e.currentTarget.style.borderColor = "var(--border-focus)"; e.currentTarget.style.boxShadow = "0 0 0 3px var(--accent-subtle)"; }}
                                onBlur={e => { e.currentTarget.style.borderColor = "var(--border)"; e.currentTarget.style.boxShadow = "none"; }}
                            />
                        </div>

                        {error && (
                            <div role="alert" style={{ background: "rgba(239,68,68,0.08)", border: "1px solid rgba(239,68,68,0.25)", borderRadius: 7, padding: "10px 14px", color: "var(--danger-t)", fontSize: 13, marginBottom: 16 }}>
                                &#9888; {error}
                            </div>
                        )}

                        <button
                            type="submit"
                            disabled={loading}
                            style={{ width: "100%", background: loading ? "var(--accent-subtle)" : "linear-gradient(135deg, var(--accent), var(--accent-hover))", color: "var(--on-accent)", fontWeight: 700, fontSize: 14, fontFamily: "'JetBrains Mono', monospace", letterSpacing: "0.08em", textTransform: "uppercase", border: "none", borderRadius: 8, padding: "13px 0", minHeight: 44, cursor: loading ? "not-allowed" : "pointer", transition: "all 0.2s" }}
                            onMouseOver={e => { if (!loading) { e.currentTarget.style.boxShadow = "0 0 20px var(--accent-subtle)"; e.currentTarget.style.transform = "translateY(-1px)"; } }}
                            onMouseOut={e => { e.currentTarget.style.boxShadow = "none"; e.currentTarget.style.transform = "none"; }}
                        >
                            {loading ? "[ ENVOI... ]" : "Envoyer le lien"}
                        </button>
                    </form>
                )}

                <p style={{ textAlign: "center", marginTop: 24, fontSize: 13, color: "var(--text-muted)" }}>
                    Vous vous souvenez de votre mot de passe&nbsp;?{" "}
                    <Link href="/login" style={{ color: "var(--primary)", textDecoration: "none", fontWeight: 500 }}>
                        Se connecter
                    </Link>
                </p>
            </div>
            </Reveal>
        </div>
    );
}

function SuccessBanner({ email }: { email: string }) {
    return (
        <div style={{ background: "var(--accent-subtle)", border: "1px solid var(--accent-border)", borderRadius: 10, padding: "16px 18px" }}>
            <div style={{ display: "flex", alignItems: "flex-start", gap: 10 }}>
                <span aria-hidden style={{ color: "var(--accent)", fontSize: 18, lineHeight: 1.2 }}>&#10003;</span>
                <div>
                    <div style={{ fontSize: 14, fontWeight: 600, color: "var(--accent)" }}>Email envoy&eacute;</div>
                    <p style={{ marginTop: 6, fontSize: 13, color: "var(--text-muted)", lineHeight: 1.5 }}>
                        Si un compte est associ&eacute; &agrave;{" "}
                        <span style={{ fontWeight: 600, color: "var(--text-primary)" }}>{email}</span>, vous recevrez un lien de r&eacute;initialisation d&apos;ici quelques minutes.
                    </p>
                    <p style={{ marginTop: 8, fontSize: 12, color: "var(--text-muted)" }}>
                        Pensez &agrave; v&eacute;rifier vos spams. Le lien est valable <strong>30&nbsp;minutes</strong>.
                    </p>
                </div>
            </div>
        </div>
    );
}
