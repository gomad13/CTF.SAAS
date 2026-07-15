"use client";

import { useState } from "react";
import Link from "next/link";
import Reveal from "@/components/Reveal";

const EMAIL_RE = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
const API_BASE = process.env.NEXT_PUBLIC_API_BASE_URL ?? "";
const MSG_MAX = 5000;

const cardStyle: React.CSSProperties = {
    width: "100%",
    maxWidth: 520,
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

const inputStyle: React.CSSProperties = {
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
};

function focusOn(e: React.FocusEvent<HTMLElement>) {
    e.currentTarget.style.borderColor = "var(--border-focus)";
    e.currentTarget.style.boxShadow = "0 0 0 3px var(--accent-subtle)";
}
function focusOff(e: React.FocusEvent<HTMLElement>) {
    e.currentTarget.style.borderColor = "var(--border)";
    e.currentTarget.style.boxShadow = "none";
}

export default function SupportPage() {
    const [email, setEmail]     = useState("");
    const [subject, setSubject] = useState("");
    const [message, setMessage] = useState("");
    const [loading, setLoading] = useState(false);
    const [sent, setSent]       = useState(false);
    const [error, setError]     = useState<string | null>(null);

    async function onSubmit(e: React.FormEvent) {
        e.preventDefault();
        setError(null);

        const em = email.trim();
        const su = subject.trim();
        const ms = message.trim();
        if (!EMAIL_RE.test(em)) { setError("Adresse email invalide."); return; }
        if (su.length < 3) { setError("Le sujet doit contenir au moins 3 caractères."); return; }
        if (ms.length < 10) { setError("Le message doit contenir au moins 10 caractères."); return; }

        setLoading(true);
        try {
            const res = await fetch(`${API_BASE}/api/support/contact`, {
                method: "POST",
                credentials: "include",
                headers: { "Content-Type": "application/json", "X-Requested-With": "XMLHttpRequest" },
                body: JSON.stringify({ email: em, subject: su, message: ms }),
            });
            if (res.ok) {
                setSent(true);
            } else if (res.status === 429) {
                setError("Trop de messages envoyés. Merci de patienter avant de réessayer.");
            } else {
                const data = await res.json().catch(() => null);
                setError(data?.error ?? "Impossible d'envoyer le message. Réessayez plus tard.");
            }
        } catch {
            setError("Impossible de joindre le serveur. Vérifiez votre connexion.");
        } finally {
            setLoading(false);
        }
    }

    return (
        <div style={{ minHeight: "100vh", display: "flex", alignItems: "center", justifyContent: "center", padding: "24px 16px" }}>
            <div style={{ position: "fixed", left: 16, top: 16 }}>
                <Link href="/login" style={{ display: "inline-flex", alignItems: "center", gap: 6, fontSize: 14, fontWeight: 600, textDecoration: "none", color: "var(--text-muted)" }}>
                    <span aria-hidden>&#x2190;</span>
                    <span>Retour</span>
                </Link>
            </div>

            <Reveal>
            <div style={cardStyle}>
                <div aria-hidden style={{ position: "absolute", top: 0, right: 0, width: 48, height: 48, background: "linear-gradient(225deg, var(--accent-border) 0%, transparent 60%)", clipPath: "polygon(100% 0, 0 0, 100% 100%)" }} />

                <div style={{ display: "flex", justifyContent: "center", marginBottom: 16 }}>
                    <span style={{ display: "inline-flex", alignItems: "center", gap: 8, background: "var(--accent-subtle)", border: "1px solid var(--accent-border)", borderRadius: 20, padding: "4px 12px", fontFamily: "'JetBrains Mono', monospace", fontSize: 11, letterSpacing: "0.12em", color: "var(--accent)" }}>
                        <span aria-hidden style={{ width: 6, height: 6, borderRadius: "50%", background: "var(--pr)", boxShadow: "0 0 6px var(--accent)" }} />
                        SUPPORT
                    </span>
                </div>

                <h1 style={{ fontSize: 24, fontWeight: 700, color: "var(--text-primary)", textAlign: "center", margin: "12px 0 0" }}>
                    Contacter le support
                </h1>
                <p style={{ fontSize: 13, color: "var(--text-muted)", textAlign: "center", margin: "8px 0 0", lineHeight: 1.5 }}>
                    Une question, un probl&egrave;me&nbsp;? Notre &eacute;quipe vous r&eacute;pond rapidement.
                </p>

                <div style={{ height: 1, background: "linear-gradient(90deg, transparent, var(--border), transparent)", margin: "20px 0" }} />

                {sent ? (
                    <div style={{ background: "var(--accent-subtle)", border: "1px solid var(--accent-border)", borderRadius: 10, padding: "18px 18px" }}>
                        <div style={{ display: "flex", alignItems: "flex-start", gap: 10 }}>
                            <span aria-hidden style={{ color: "var(--accent)", fontSize: 18, lineHeight: 1.2 }}>&#10003;</span>
                            <div>
                                <div style={{ fontSize: 14, fontWeight: 600, color: "var(--accent)" }}>Message envoy&eacute;</div>
                                <p style={{ marginTop: 6, fontSize: 13, color: "var(--text-muted)", lineHeight: 1.5 }}>
                                    Merci&nbsp;! Votre message a bien &eacute;t&eacute; transmis. Nous vous r&eacute;pondrons &agrave; l&apos;adresse indiqu&eacute;e.
                                </p>
                            </div>
                        </div>
                    </div>
                ) : (
                    <form onSubmit={onSubmit} noValidate>
                        <div style={{ marginBottom: 16 }}>
                            <label htmlFor="email" style={labelStyle}>VOTRE EMAIL</label>
                            <input id="email" type="email" value={email} onChange={e => setEmail(e.target.value)} placeholder="alice@entreprise.com" autoComplete="email" required style={inputStyle} onFocus={focusOn} onBlur={focusOff} />
                        </div>

                        <div style={{ marginBottom: 16 }}>
                            <label htmlFor="subject" style={labelStyle}>SUJET</label>
                            <input id="subject" type="text" value={subject} onChange={e => setSubject(e.target.value.slice(0, 150))} placeholder="Résumé de votre demande" maxLength={150} required style={inputStyle} onFocus={focusOn} onBlur={focusOff} />
                        </div>

                        <div style={{ marginBottom: 16 }}>
                            <label htmlFor="message" style={labelStyle}>MESSAGE</label>
                            <textarea id="message" value={message} onChange={e => setMessage(e.target.value.slice(0, MSG_MAX))} placeholder="Décrivez votre demande en détail…" rows={6} maxLength={MSG_MAX} required style={{ ...inputStyle, resize: "vertical", minHeight: 120, fontFamily: "inherit", lineHeight: 1.5 }} onFocus={focusOn} onBlur={focusOff} />
                            <div style={{ textAlign: "right", marginTop: 4, fontSize: 11, color: "var(--text-muted)" }}>
                                {message.length}/{MSG_MAX}
                            </div>
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
                            {loading ? "[ ENVOI... ]" : "Envoyer le message"}
                        </button>
                    </form>
                )}
            </div>
            </Reveal>
        </div>
    );
}
