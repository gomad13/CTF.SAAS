"use client";

import { useEffect, useState } from "react";
import { usePathname } from "next/navigation";
import Link from "next/link";

const STORAGE_KEY = "sentys.cookieConsent";
type Consent = "accepted" | "essential-only";

// Routes où le banner est inutile / parasite : zones authentifiées et page feedback.
// Logique : on n'affiche la bannière que sur les pages publiques pré-login.
const HIDDEN_PREFIXES = ["/dashboard", "/admin", "/superadmin", "/feedback", "/paths"];

export default function CookieBanner() {
    const [consent, setConsent] = useState<Consent | null | undefined>(undefined);
    const pathname = usePathname();

    useEffect(() => {
        try {
            const v = localStorage.getItem(STORAGE_KEY) as Consent | null;
            setConsent(v ?? null);
        } catch {
            setConsent(null);
        }
    }, []);

    if (consent === undefined || consent !== null) return null;
    if (pathname && HIDDEN_PREFIXES.some(p => pathname === p || pathname.startsWith(p + "/"))) return null;

    const persist = (v: Consent) => {
        try { localStorage.setItem(STORAGE_KEY, v); } catch { /* noop */ }
        setConsent(v);
    };

    return (
        <div
            role="dialog"
            aria-label="Consentement aux cookies"
            className="cookie-banner"
            style={{
                position: "fixed",
                bottom: 12,
                left: 12,
                right: 12,
                maxWidth: 640,
                margin: "0 auto",
                background: "#FFFFFF",
                color: "#0A0A0B",
                border: "1px solid #E2E8F0",
                borderRadius: 12,
                boxShadow: "0 10px 30px rgba(15, 23, 42, 0.18)",
                padding: 14,
                zIndex: 60,
                fontSize: 13,
                lineHeight: 1.45,
            }}
        >
            <p style={{ margin: 0, marginBottom: 10 }}>
                Sentys utilise uniquement des <strong>cookies strictement nécessaires</strong> à votre authentification.{" "}
                <Link href="/privacy" style={{ color: "var(--accent-hover)", textDecoration: "underline" }}>
                    En savoir plus
                </Link>
            </p>
            <div style={{ display: "flex", gap: 8, flexWrap: "wrap" }}>
                <button
                    type="button"
                    onClick={() => persist("essential-only")}
                    style={{
                        flex: "1 1 auto",
                        background: "transparent",
                        border: "1px solid #E2E8F0",
                        color: "#334155",
                        padding: "10px 12px",
                        minHeight: 44,
                        borderRadius: 8,
                        fontSize: 13,
                        fontWeight: 500,
                        cursor: "pointer",
                    }}
                >
                    Essentiels uniquement
                </button>
                <button
                    type="button"
                    onClick={() => persist("accepted")}
                    style={{
                        flex: "1 1 auto",
                        background: "var(--accent)",
                        border: "1px solid var(--accent)",
                        color: "#FFFFFF",
                        padding: "10px 14px",
                        minHeight: 44,
                        borderRadius: 8,
                        fontSize: 13,
                        fontWeight: 600,
                        cursor: "pointer",
                    }}
                >
                    Tout accepter
                </button>
            </div>
        </div>
    );
}
