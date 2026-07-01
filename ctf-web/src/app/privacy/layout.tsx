import type { ReactNode } from "react";
import Link from "next/link";

export default function LegalLayout({ children }: { children: ReactNode }) {
    return (
        <main
            style={{
                minHeight: "100svh",
                background: "#F8FAFC",
                color: "#1E293B",
            }}
        >
            <div
                style={{
                    maxWidth: 760,
                    margin: "0 auto",
                    padding: "clamp(32px, 7vw, 48px) clamp(16px, 5vw, 24px) 80px",
                    fontSize: 15,
                    lineHeight: 1.7,
                    color: "#1E293B",
                    overflowWrap: "break-word",
                    wordBreak: "break-word",
                }}
            >
                <Link
                    href="/"
                    style={{
                        display: "inline-block",
                        marginBottom: 24,
                        fontSize: 13,
                        color: "#475569",
                        textDecoration: "none",
                    }}
                >
                    ← Retour à l&apos;accueil
                </Link>
                {children}
                <hr style={{ margin: "48px 0 16px", borderColor: "#E2E8F0" }} />
                <nav style={{ display: "flex", gap: 16, fontSize: 13, color: "#475569" }}>
                    <Link href="/cgu" style={{ color: "inherit" }}>CGU</Link>
                    <Link href="/privacy" style={{ color: "inherit" }}>Confidentialité</Link>
                    <Link href="/mentions-legales" style={{ color: "inherit" }}>Mentions légales</Link>
                </nav>
            </div>
        </main>
    );
}
