"use client";

import { useState } from "react";
import Link from "next/link";
import Reveal from "@/components/Reveal";

type FeedbackCategory = "bug" | "amelioration" | "question" | "autre";

const API_BASE = process.env.NEXT_PUBLIC_API_BASE_URL ?? "";

export default function FeedbackPage() {
    const [name, setName] = useState("");
    const [email, setEmail] = useState("");
    const [category, setCategory] = useState<FeedbackCategory>("amelioration");
    const [message, setMessage] = useState("");
    const [submitted, setSubmitted] = useState(false);
    const [submitting, setSubmitting] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const submit = async (e: React.FormEvent) => {
        e.preventDefault();
        setError(null);
        if (message.trim().length < 10) {
            setError("Le message doit contenir au moins 10 caractères.");
            return;
        }
        setSubmitting(true);
        try {
            const res = await fetch(`${API_BASE}/api/feedback`, {
                method: "POST",
                credentials: "include",
                headers: {
                    "Content-Type": "application/json",
                    "X-Requested-With": "XMLHttpRequest",
                },
                body: JSON.stringify({
                    name: name.trim() || null,
                    email: email.trim() || null,
                    subject: category,
                    message: message.trim(),
                    page: typeof window !== "undefined" ? window.location.href : null,
                }),
            });
            if (!res.ok) {
                let msg = "Impossible d'envoyer votre feedback. Réessayez dans quelques instants.";
                try {
                    const data: unknown = await res.json();
                    if (data && typeof data === "object" && "error" in data && typeof (data as { error: unknown }).error === "string") {
                        msg = (data as { error: string }).error;
                    }
                } catch { /* ignore */ }
                if (res.status === 429) msg = "Trop de feedbacks récents. Réessayez dans 10 minutes.";
                setError(msg);
                return;
            }
            setSubmitted(true);
        } catch {
            setError("Impossible de contacter le serveur.");
        } finally {
            setSubmitting(false);
        }
    };

    if (submitted) {
        return (
            <main style={shellStyle()}>
                <Reveal>
                    <div style={cardStyle()}>
                        <h1 style={{ fontSize: 24, fontWeight: 700, marginBottom: 12 }}>Merci 🙏</h1>
                        <p style={{ marginBottom: 8 }}>
                            Votre retour a bien été enregistré. L&apos;équipe Sentys le consultera très prochainement.
                        </p>
                        <p style={{ fontSize: 13, color: "var(--text-3)", marginBottom: 24 }}>
                            En phase bêta, vos retours sont précieux pour faire évoluer la plateforme.
                        </p>
                        <Link href="/dashboard" style={primaryLinkStyle()}>Retour au tableau de bord</Link>
                    </div>
                </Reveal>
            </main>
        );
    }

    return (
        <main style={shellStyle()}>
            <Reveal>
            <div style={cardStyle()}>
                <h1 style={{ fontSize: 26, fontWeight: 700, marginBottom: 8 }}>Envoyer un feedback</h1>
                <p style={{ fontSize: 14, color: "var(--text-3)", marginBottom: 24, lineHeight: 1.6 }}>
                    Bug, suggestion ou question : tout retour nous aide à améliorer Sentys. Vos messages sont lus par
                    l&apos;équipe produit.
                </p>

                <form onSubmit={submit} style={{ display: "flex", flexDirection: "column", gap: 16 }}>
                    <Field label="Nom (optionnel)">
                        <input
                            type="text"
                            value={name}
                            onChange={e => setName(e.target.value)}
                            placeholder="Votre nom"
                            style={inputStyle()}
                        />
                    </Field>

                    <Field label="Email (optionnel — pour qu'on vous réponde)">
                        <input
                            type="email"
                            value={email}
                            onChange={e => setEmail(e.target.value)}
                            placeholder="vous@entreprise.fr"
                            style={inputStyle()}
                        />
                    </Field>

                    <Field label="Catégorie">
                        <select
                            value={category}
                            onChange={e => setCategory(e.target.value as FeedbackCategory)}
                            style={inputStyle()}
                        >
                            <option value="bug">Bug rencontré</option>
                            <option value="amelioration">Suggestion d&apos;amélioration</option>
                            <option value="question">Question</option>
                            <option value="autre">Autre</option>
                        </select>
                    </Field>

                    <Field label="Votre message" required>
                        <textarea
                            value={message}
                            onChange={e => setMessage(e.target.value)}
                            placeholder="Décrivez ce que vous avez observé ou ce que vous proposez…"
                            rows={6}
                            required
                            style={{ ...inputStyle(), resize: "vertical", minHeight: 140 }}
                        />
                    </Field>

                    {error && (
                        <p role="alert" style={{ color: "var(--danger)", fontSize: 13, margin: 0 }}>{error}</p>
                    )}

                    <button type="submit" disabled={submitting} style={primaryBtnStyle(submitting)}>
                        {submitting ? "Envoi en cours…" : "Envoyer mon feedback"}
                    </button>
                </form>
            </div>
            </Reveal>
        </main>
    );
}

function Field({ label, required, children }: { label: string; required?: boolean; children: React.ReactNode }) {
    return (
        <label style={{ display: "flex", flexDirection: "column", gap: 6, fontSize: 13, color: "var(--text-2)" }}>
            <span style={{ fontWeight: 500 }}>
                {label} {required && <span style={{ color: "var(--danger)" }}>*</span>}
            </span>
            {children}
        </label>
    );
}

function shellStyle(): React.CSSProperties {
    return {
        maxWidth: 620,
        margin: "0 auto",
        padding: "48px 24px 80px",
    };
}

function cardStyle(): React.CSSProperties {
    return {
        background: "var(--surface)",
        border: "1px solid var(--border)",
        borderRadius: 12,
        padding: 32,
        color: "var(--text)",
    };
}

function inputStyle(): React.CSSProperties {
    return {
        width: "100%",
        background: "var(--bg)",
        border: "1px solid var(--border)",
        color: "var(--text)",
        borderRadius: 8,
        padding: "10px 14px",
        fontSize: 14,
        outline: "none",
    };
}

function primaryBtnStyle(disabled = false): React.CSSProperties {
    return {
        background: disabled ? "color-mix(in srgb, var(--accent) 55%, transparent)" : "var(--accent)",
        color: "var(--on-accent)",
        border: "none",
        borderRadius: 8,
        padding: "12px 20px",
        fontSize: 14,
        fontWeight: 600,
        cursor: disabled ? "wait" : "pointer",
        marginTop: 4,
        transition: "background 0.2s",
    };
}

function primaryLinkStyle(): React.CSSProperties {
    return {
        display: "inline-block",
        background: "var(--accent)",
        color: "var(--on-accent)",
        padding: "10px 18px",
        borderRadius: 8,
        fontSize: 14,
        fontWeight: 600,
        textDecoration: "none",
        transition: "background 0.2s",
    };
}
