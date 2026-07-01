"use client";

import { useState } from "react";
import Link from "next/link";
import { apiFetch } from "@/lib/api";

const EMAIL_RE = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

export default function ForgotPasswordPage() {
    const [email, setEmail]       = useState("");
    const [loading, setLoading]   = useState(false);
    const [sent, setSent]         = useState(false);
    const [error, setError]       = useState<string | null>(null);

    async function onSubmit(e: React.FormEvent) {
        e.preventDefault();
        setError(null);

        if (!email.trim()) { setError("L'email est requis."); return; }
        if (!EMAIL_RE.test(email.trim())) { setError("Format d'email invalide."); return; }

        setLoading(true);
        try {
            await apiFetch<void>("/api/auth/forgot-password", {
                method: "POST",
                body: JSON.stringify({ email: email.trim(), tenantId: null }),
            });
            setSent(true);
        } catch {
            // On affiche le succès même en cas d'erreur réseau (éviter énumération)
            setSent(true);
        } finally {
            setLoading(false);
        }
    }

    return (
        <div className="min-h-screen bg-background-dark text-white">
            {/* Back link */}
            <div className="fixed left-4 top-4 z-50 sm:left-6 sm:top-6">
                <Link
                    href="/login"
                    className="group inline-flex items-center gap-1.5 text-sm font-semibold transition-colors hover:text-primary-hover"
                >
                    <span className="text-neutral-300 transition-colors group-hover:text-accent">←</span>
                    <span className="text-neutral-300">Retour à la connexion</span>
                </Link>
            </div>

            <div className="mx-auto flex min-h-screen max-w-lg items-center px-4 py-16">
                <div className="w-full rounded-2xl border border-neutral-800 bg-neutral-900/40 p-6 shadow-sm">

                    {/* Icon */}
                    <div className="mb-4 flex h-12 w-12 items-center justify-center rounded-2xl border border-primary/30 bg-primary/10 text-2xl">
                        🔑
                    </div>

                    <h1 className="text-2xl font-semibold">Mot de passe oublié</h1>
                    <p className="mt-1 text-sm text-neutral-300">
                        Saisissez votre email — si un compte existe, vous recevrez un lien de réinitialisation.
                    </p>

                    {sent ? (
                        <SuccessBanner email={email} />
                    ) : (
                        <form className="mt-5 space-y-3" onSubmit={onSubmit} noValidate>
                            <div>
                                <label className="mb-1 block text-sm text-neutral-300">Email</label>
                                <input
                                    type="email"
                                    value={email}
                                    onChange={e => setEmail(e.target.value)}
                                    placeholder="alice@entreprise.com"
                                    autoComplete="email"
                                    className="w-full min-h-[44px] rounded-xl border border-neutral-800 bg-neutral-950/40 px-3 py-2 text-sm text-white placeholder-black transition-colors focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary/20"
                                />
                            </div>

                            {error && (
                                <div className="rounded-xl border border-red-700/60 bg-red-950/30 px-3 py-2 text-sm text-red-300">
                                    {error}
                                </div>
                            )}

                            <button
                                type="submit"
                                disabled={loading}
                                className="mt-1 min-h-[44px] w-full rounded-xl bg-primary px-3 py-2.5 text-sm font-semibold text-white transition-colors hover:bg-primary-dark disabled:opacity-60"
                            >
                                {loading ? "Envoi en cours…" : "Envoyer le lien de réinitialisation"}
                            </button>
                        </form>
                    )}

                    {/* DEV notice — masqué en production (RGPD + crédibilité) */}
                    {process.env.NODE_ENV !== "production" && <DevNotice />}

                    <p className="mt-5 text-center text-sm text-neutral-300">
                        Vous vous souvenez de votre mot de passe ?{" "}
                        <Link href="/login" className="text-primary hover:underline">
                            Se connecter
                        </Link>
                    </p>
                </div>
            </div>
        </div>
    );
}

function SuccessBanner({ email }: { email: string }) {
    return (
        <div className="mt-5 rounded-xl border border-primary/40 bg-primary/10 px-4 py-4">
            <div className="flex items-start gap-3">
                <span className="mt-0.5 text-lg text-primary">✓</span>
                <div>
                    <div className="text-sm font-semibold text-primary">Email envoyé</div>
                    <p className="mt-1 text-sm text-neutral-300">
                        Si un compte est associé à <span className="font-medium text-white">{email}</span>,
                        vous recevrez un lien de réinitialisation sous quelques minutes.
                    </p>
                    <p className="mt-2 text-xs text-neutral-300">
                        Vérifiez également vos spams. Le lien est valable 1 heure.
                    </p>
                </div>
            </div>
        </div>
    );
}

function DevNotice() {
    return (
        <div className="mt-5 rounded-xl border border-dashed border-neutral-700 bg-neutral-950/30 px-3 py-3">
            <div className="flex items-center gap-2">
                <span className="font-mono text-xs text-neutral-300">DEV</span>
                <span className="text-xs text-neutral-300">
                    Le token de réinitialisation apparaît dans les logs du backend (console).
                </span>
            </div>
            <p className="mt-1 text-xs text-neutral-300 font-mono">
                [DEV] Lien de réinitialisation : /reset-password?token=…
            </p>
        </div>
    );
}
