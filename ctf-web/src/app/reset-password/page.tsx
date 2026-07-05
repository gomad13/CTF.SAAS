"use client";

import { Suspense, useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import Link from "next/link";
import { apiFetch } from "@/lib/api";
import { PasswordInput } from "@/components/ui/PasswordInput";
import Reveal from "@/components/Reveal";

// ── Password strength (shared logic) ─────────────────────────────────────────
const PASSWORD_CRITERIA = [
    { label: "8 caractères minimum",       test: (p: string) => p.length >= 8 },
    { label: "Une majuscule (A-Z)",         test: (p: string) => /[A-Z]/.test(p) },
    { label: "Une minuscule (a-z)",         test: (p: string) => /[a-z]/.test(p) },
    { label: "Un chiffre (0-9)",            test: (p: string) => /[0-9]/.test(p) },
    { label: "Un caractère spécial (!@#…)", test: (p: string) => /[^A-Za-z0-9]/.test(p) },
] as const;

const STRENGTH_LEVELS = [
    { min: 0, max: 0, label: "Très faible", color: "var(--danger)", pct: "0%"   },
    { min: 1, max: 2, label: "Faible",      color: "var(--warning)", pct: "40%"  },
    { min: 3, max: 3, label: "Moyen",       color: "var(--warning)", pct: "60%"  },
    { min: 4, max: 4, label: "Fort",        color: "var(--pr)", pct: "80%"  },
    { min: 5, max: 5, label: "Très fort",   color: "var(--pr)", pct: "100%" },
] as const;

function passwordScore(p: string) {
    return PASSWORD_CRITERIA.filter(c => c.test(p)).length;
}
function getLevel(score: number) {
    return STRENGTH_LEVELS.find(l => score >= l.min && score <= l.max) ?? STRENGTH_LEVELS[0];
}

// ── Wrapping with Suspense for useSearchParams ────────────────────────────────
export default function ResetPasswordPage() {
    return (
        <Suspense fallback={<LoadingShell />}>
            <ResetPasswordForm />
        </Suspense>
    );
}

function LoadingShell() {
    return (
        <div className="min-h-screen bg-background-dark text-white flex items-center justify-center">
            <div className="h-8 w-48 animate-pulse rounded-lg bg-neutral-800" />
        </div>
    );
}

// ── Main form ─────────────────────────────────────────────────────────────────
function ResetPasswordForm() {
    const router       = useRouter();
    const searchParams = useSearchParams();
    const token        = searchParams.get("token") ?? "";

    const [password, setPassword]         = useState("");
    const [confirm, setConfirm]           = useState("");
    const [loading, setLoading]           = useState(false);
    const [error, setError]               = useState<string | null>(null);
    const [fieldError, setFieldError]     = useState<string | null>(null);

    if (!token) {
        return <InvalidToken reason="Lien de réinitialisation manquant ou invalide." />;
    }

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
            if (msg.includes("expiré") || msg.includes("invalide")) {
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
                <Reveal>
                <div className="w-full rounded-2xl border border-neutral-800 bg-neutral-900/40 p-6 shadow-sm">

                    {/* Icon */}
                    <div className="mb-4 flex h-12 w-12 items-center justify-center rounded-2xl border border-primary/30 bg-primary/10 text-2xl">
                        🔒
                    </div>

                    <h1 className="text-2xl font-semibold">Nouveau mot de passe</h1>
                    <p className="mt-1 text-sm text-neutral-300">
                        Choisissez un mot de passe fort pour sécuriser votre compte.
                    </p>

                    {/* Token invalid error (non-recoverable) */}
                    {error && (
                        <div className="mt-4 rounded-xl border border-red-700/60 bg-red-950/30 px-4 py-3">
                            <p className="text-sm text-red-300">{error}</p>
                            <Link
                                href="/forgot-password"
                                className="mt-2 inline-block text-sm text-primary hover:underline"
                            >
                                Demander un nouveau lien →
                            </Link>
                        </div>
                    )}

                    <form className="mt-5 space-y-4" onSubmit={onSubmit} noValidate>
                        {/* New password */}
                        <div>
                            <label className="mb-1 block text-sm text-neutral-300">Nouveau mot de passe</label>
                            <PasswordInput
                                className="w-full rounded-xl border border-neutral-800 bg-neutral-950/40 py-2 pl-3 text-sm text-white placeholder-black transition-colors focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary/20"
                                value={password}
                                onChange={e => setPassword(e.target.value)}
                                placeholder="8 caractères minimum"
                                autoComplete="new-password"
                            />
                        </div>

                        {/* Strength meter */}
                        {password.length > 0 && (
                            <div className="space-y-2 rounded-xl border border-neutral-800 bg-neutral-950/30 p-3">
                                <div className="flex items-center gap-2">
                                    <div className="h-1.5 flex-1 overflow-hidden rounded-full bg-neutral-800">
                                        <div
                                            className="h-full rounded-full transition-all duration-300"
                                            style={{ width: level.pct, backgroundColor: level.color }}
                                        />
                                    </div>
                                    <span className="w-16 text-right text-xs font-medium" style={{ color: level.color }}>
                                        {level.label}
                                    </span>
                                </div>
                                <ul className="space-y-1">
                                    {PASSWORD_CRITERIA.map(c => {
                                        const ok = c.test(password);
                                        return (
                                            <li key={c.label} className="flex items-center gap-2 text-xs">
                                                <span style={{ color: ok ? "var(--pr)" : "var(--danger)" }}>
                                                    {ok ? "✓" : "✗"}
                                                </span>
                                                <span className={ok ? "text-neutral-300" : "text-neutral-300"}>
                                                    {c.label}
                                                </span>
                                            </li>
                                        );
                                    })}
                                </ul>
                            </div>
                        )}

                        {/* Confirm */}
                        <div>
                            <label className="mb-1 block text-sm text-neutral-300">Confirmer le mot de passe</label>
                            <PasswordInput
                                className="w-full rounded-xl border border-neutral-800 bg-neutral-950/40 py-2 pl-3 text-sm text-white placeholder-black transition-colors focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary/20"
                                value={confirm}
                                onChange={e => setConfirm(e.target.value)}
                                placeholder="Répéter le mot de passe"
                                autoComplete="new-password"
                            />
                        </div>

                        {/* Field error */}
                        {fieldError && (
                            <div className="rounded-xl border border-red-700/60 bg-red-950/30 px-3 py-2 text-sm text-red-300">
                                {fieldError}
                            </div>
                        )}

                        <button
                            type="submit"
                            disabled={loading}
                            className="mt-1 min-h-[44px] w-full rounded-xl bg-primary px-3 py-2.5 text-sm font-semibold text-white transition-colors hover:bg-primary-dark disabled:opacity-60"
                        >
                            {loading ? "Réinitialisation en cours…" : "Réinitialiser mon mot de passe"}
                        </button>
                    </form>
                </div>
                </Reveal>
            </div>
        </div>
    );
}

function InvalidToken({ reason }: { reason: string }) {
    return (
        <div className="min-h-screen bg-background-dark text-white flex items-center justify-center px-4">
            <div className="w-full max-w-md rounded-2xl border border-neutral-800 bg-neutral-900/40 p-6 text-center">
                <div className="mb-4 text-4xl">⚠️</div>
                <h1 className="text-xl font-semibold">Lien invalide</h1>
                <p className="mt-2 text-sm text-neutral-300">{reason}</p>
                <Link
                    href="/forgot-password"
                    className="mt-4 inline-flex min-h-[44px] items-center justify-center rounded-xl bg-primary px-5 py-2.5 text-sm font-semibold text-white hover:bg-primary-dark transition-colors"
                >
                    Demander un nouveau lien
                </Link>
            </div>
        </div>
    );
}

