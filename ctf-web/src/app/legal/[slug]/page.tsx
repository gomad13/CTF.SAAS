"use client";

import { use } from "react";
import Link from "next/link";
import LegalDocumentViewer from "@/components/legal/LegalDocumentViewer";

const KNOWN_SLUGS = ["politique-confidentialite", "cgu", "dpa", "mentions-legales"];

export default function LegalSlugPage({ params }: { params: Promise<{ slug: string }> }) {
    const { slug } = use(params);
    const known = KNOWN_SLUGS.includes(slug);

    return (
        <main style={{ minHeight: "100svh", background: "#F8FAFC" }}>
            <header style={{
                background: "#FFFFFF",
                borderBottom: "1px solid #E2E8F0",
                padding: "16px clamp(16px, 5vw, 24px)",
            }}>
                <div style={{ maxWidth: 800, margin: "0 auto", display: "flex", flexWrap: "wrap", alignItems: "center", justifyContent: "space-between", gap: 12 }}>
                    <Link href="/" style={{ display: "inline-flex", alignItems: "center", gap: 8, textDecoration: "none", color: "#1E293B" }}>
                        <span style={{
                            width: 28, height: 28, background: "#03b5aa", borderRadius: 6,
                            display: "inline-flex", alignItems: "center", justifyContent: "center",
                            color: "#FFFFFF", fontWeight: 700, fontFamily: "'JetBrains Mono', monospace", fontSize: 13,
                        }}>V</span>
                        <span style={{ fontWeight: 700, fontSize: 16 }}>Sentys</span>
                    </Link>
                    <nav style={{ display: "flex", flexWrap: "wrap", gap: 16, fontSize: 13 }}>
                        <Link href="/legal/politique-confidentialite" style={{ color: "#64748B", textDecoration: "none" }}>Politique</Link>
                        <Link href="/legal/cgu" style={{ color: "#64748B", textDecoration: "none" }}>CGU</Link>
                        <Link href="/legal/dpa" style={{ color: "#64748B", textDecoration: "none" }}>DPA</Link>
                        <Link href="/legal/mentions-legales" style={{ color: "#64748B", textDecoration: "none" }}>Mentions légales</Link>
                    </nav>
                </div>
            </header>
            {!known ? (
                <div style={{ maxWidth: 800, margin: "0 auto", padding: 32, textAlign: "center" }}>
                    <h1 style={{ fontSize: 24, color: "#1E293B" }}>Document introuvable</h1>
                    <p style={{ fontSize: 14, color: "#64748B" }}>
                        Le document demandé n&apos;existe pas. Voir{" "}
                        <Link href="/legal/politique-confidentialite" style={{ color: "#03b5aa" }}>la liste des documents</Link>.
                    </p>
                </div>
            ) : (
                <LegalDocumentViewer slug={slug} />
            )}
        </main>
    );
}
