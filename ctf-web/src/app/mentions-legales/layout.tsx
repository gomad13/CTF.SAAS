import type { ReactNode } from "react";
import Link from "next/link";

export default function LegalLayout({ children }: { children: ReactNode }) {
    return (
        <main
            style={{
                minHeight: "100svh",
                background: "var(--bg)",
                color: "var(--text)",
            }}
        >
            <div
                style={{
                    maxWidth: 760,
                    margin: "0 auto",
                    padding: "clamp(32px, 7vw, 48px) clamp(16px, 5vw, 24px) 80px",
                    fontSize: 15,
                    lineHeight: 1.7,
                    color: "var(--text)",
                    overflowWrap: "break-word",
                    wordBreak: "break-word",
                }}
            >
                <Link
                    href="/"
                    className="transition-colors duration-200 hover:text-fg-heading"
                    style={{
                        display: "inline-block",
                        marginBottom: 24,
                        fontSize: 13,
                        color: "var(--text-2)",
                        textDecoration: "none",
                    }}
                >
                    ← Retour à l&apos;accueil
                </Link>
                {children}
                <hr style={{ margin: "48px 0 16px", borderColor: "var(--border)" }} />
                <nav style={{ display: "flex", gap: 16, fontSize: 13, color: "var(--text-2)" }}>
                    <Link href="/cgu" style={{ color: "inherit" }}>CGU</Link>
                    <Link href="/privacy" style={{ color: "inherit" }}>Confidentialité</Link>
                    <Link href="/mentions-legales" style={{ color: "inherit" }}>Mentions légales</Link>
                </nav>
            </div>
        </main>
    );
}
