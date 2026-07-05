"use client";

import { Suspense, useCallback, useEffect, useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import Link from "next/link";
import Reveal from "@/components/Reveal";

const API_BASE = process.env.NEXT_PUBLIC_API_BASE_URL ?? "";

type Phase = "checking" | "anon" | "ready" | "joining" | "success" | "error" | "notoken";

export default function JoinPage() {
    return (
        <Suspense fallback={<Shell><p style={msg}>Chargement…</p></Shell>}>
            <JoinFlow />
        </Suspense>
    );
}

function JoinFlow() {
    const router = useRouter();
    const searchParams = useSearchParams();
    const token = searchParams.get("token");

    // Phase initiale dérivée du token (évite un setState synchrone dans l'effet).
    const [phase, setPhase] = useState<Phase>(token ? "checking" : "notoken");
    const [error, setError] = useState<string | null>(null);
    const [tenantName, setTenantName] = useState<string | null>(null);

    // 1. Si un token est présent, vérifier l'état d'authentification (setState async only).
    useEffect(() => {
        if (!token) return;
        let cancelled = false;
        (async () => {
            try {
                const res = await fetch(`${API_BASE}/api/auth/me`, { credentials: "include" });
                if (cancelled) return;
                setPhase(res.ok ? "ready" : "anon");
            } catch {
                if (!cancelled) setPhase("anon");
            }
        })();
        return () => { cancelled = true; };
    }, [token]);

    // 2. Rejoindre (utilisateur connecté).
    const join = useCallback(async () => {
        if (!token) return;
        setPhase("joining");
        setError(null);
        try {
            const res = await fetch(`${API_BASE}/api/invites/redeem`, {
                method: "POST",
                credentials: "include",
                headers: { "Content-Type": "application/json", "X-Requested-With": "XMLHttpRequest" },
                body: JSON.stringify({ token }),
            });

            if (res.status === 401) { setPhase("anon"); return; }

            const data = await res.json().catch(() => null);

            if (!res.ok) {
                setError(
                    res.status === 429
                        ? "Trop de tentatives. Patientez une minute."
                        : (data?.error || "Cette invitation est invalide, expirée ou épuisée.")
                );
                setPhase("error");
                return;
            }

            setTenantName(data?.tenantName ?? null);
            // [MULTI-SOCIETES] Basculer la société active sur celle qu'on vient de rejoindre.
            if (data?.tenantId) {
                await fetch(`${API_BASE}/api/me/active-tenant`, {
                    method: "POST",
                    credentials: "include",
                    headers: { "Content-Type": "application/json", "X-Requested-With": "XMLHttpRequest" },
                    body: JSON.stringify({ tenantId: data.tenantId }),
                }).catch(() => {});
            }
            setPhase("success");
            setTimeout(() => router.push("/dashboard"), 1800);
        } catch {
            setError("Impossible de joindre le serveur. Réessayez.");
            setPhase("error");
        }
    }, [token, router]);

    // [MULTI-SOCIETES] Auto-adhésion dès qu'on est authentifié avec un token (sans clic).
    const [autoJoined, setAutoJoined] = useState(false);
    useEffect(() => {
        if (phase === "ready" && !autoJoined) {
            setAutoJoined(true);
            join();
        }
    }, [phase, autoJoined, join]);

    const loginHref = `/login?returnUrl=${encodeURIComponent(`/join?token=${token ?? ""}`)}`;
    const registerHref = `/register?token=${encodeURIComponent(token ?? "")}`;

    if (phase === "notoken") {
        return <Shell><Title>Lien d’invitation invalide</Title>
            <p style={msg}>Ce lien ne contient pas de jeton d’invitation. Demandez un nouveau QR code à votre administrateur.</p>
            <Link href="/login" style={primaryBtn}>Aller à la connexion</Link>
        </Shell>;
    }

    if (phase === "checking") {
        return <Shell><Title>Rejoindre une entreprise</Title><p style={msg}>Vérification…</p></Shell>;
    }

    if (phase === "anon") {
        return <Shell><Title>Rejoindre une entreprise</Title>
            <p style={msg}>Connectez-vous pour rejoindre cette entreprise avec votre compte.</p>
            <Link href={loginHref} style={primaryBtn}>Se connecter pour rejoindre</Link>
            <p style={{ ...msg, marginTop: 14, fontSize: 13 }}>
                Pas encore de compte ? <Link href={registerHref} style={{ color: "var(--primary)" }}>Créer un compte</Link>
            </p>
        </Shell>;
    }

    if (phase === "success") {
        return <Shell><Title>Bienvenue 🎉</Title>
            <p style={msg}>Vous avez rejoint{tenantName ? ` « ${tenantName} »` : " l'entreprise"}. Redirection vers votre espace…</p>
        </Shell>;
    }

    if (phase === "error") {
        return <Shell><Title>Invitation refusée</Title>
            <p style={{ ...msg, color: "var(--danger)" }}>{error}</p>
            <Link href="/dashboard" style={primaryBtn}>Retour à mon espace</Link>
        </Shell>;
    }

    // phase === "ready" | "joining"
    return <Shell><Title>Rejoindre une entreprise</Title>
        <p style={msg}>Vous êtes sur le point de rattacher votre compte à une nouvelle entreprise.</p>
        <button type="button" onClick={join} disabled={phase === "joining"} style={{ ...primaryBtn, opacity: phase === "joining" ? 0.6 : 1, cursor: phase === "joining" ? "not-allowed" : "pointer" }}>
            {phase === "joining" ? "Adhésion en cours…" : "Rejoindre l'entreprise"}
        </button>
    </Shell>;
}

// ── UI helpers (style aligné sur /login : inline + variables CSS) ──────────────
function Shell({ children }: { children: React.ReactNode }) {
    return (
        <div style={{ minHeight: "100svh", display: "flex", alignItems: "center", justifyContent: "center", padding: "24px 16px" }}>
            <Reveal>
            <div style={{
                width: "100%", maxWidth: 440, background: "var(--bg-card)",
                border: "1px solid var(--border)", borderRadius: 14,
                padding: "clamp(28px, 6vw, 44px)", textAlign: "center",
            }}>
                {children}
            </div>
            </Reveal>
        </div>
    );
}

function Title({ children }: { children: React.ReactNode }) {
    return <h1 style={{ fontSize: 24, fontWeight: 700, color: "var(--text-primary)", margin: "0 0 12px" }}>{children}</h1>;
}

const msg: React.CSSProperties = {
    fontSize: 14, lineHeight: 1.6, color: "var(--text-secondary, var(--text-3))", margin: "0 0 20px",
};

const primaryBtn: React.CSSProperties = {
    display: "inline-flex", alignItems: "center", justifyContent: "center",
    width: "100%", minHeight: 44, padding: "12px 20px",
    background: "linear-gradient(135deg, var(--accent), var(--accent-hover))", color: "var(--on-accent)",
    fontWeight: 600, fontSize: 14, borderRadius: 8, border: "none",
    textDecoration: "none", transition: "all 0.2s",
};
