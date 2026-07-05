"use client";

import { use } from "react";
import Link from "next/link";
import LegalDocumentViewer from "@/components/legal/LegalDocumentViewer";
import Reveal from "@/components/Reveal";

const KNOWN_SLUGS = ["politique-confidentialite", "cgu", "dpa", "mentions-legales"];

export default function LegalSlugPage({ params }: { params: Promise<{ slug: string }> }) {
    const { slug } = use(params);
    const known = KNOWN_SLUGS.includes(slug);

    return (
        <main style={{ minHeight: "100svh", background: "var(--bg)" }}>
            <header style={{
                background: "var(--surface)",
                borderBottom: "1px solid var(--border)",
                padding: "16px clamp(16px, 5vw, 24px)",
            }}>
                <div style={{ maxWidth: 800, margin: "0 auto", display: "flex", flexWrap: "wrap", alignItems: "center", justifyContent: "space-between", gap: 12 }}>
                    <Link href="/" style={{ display: "inline-flex", alignItems: "center", gap: 8, textDecoration: "none", color: "var(--text)" }}>
                        <span style={{
                            width: 28, height: 28, background: "var(--accent)", borderRadius: 6,
                            display: "inline-flex", alignItems: "center", justifyContent: "center",
                            color: "var(--on-accent)", fontWeight: 700, fontFamily: "'JetBrains Mono', monospace", fontSize: 13,
                        }}>V</span>
                        <span style={{ fontWeight: 700, fontSize: 16 }}>Sentys</span>
                    </Link>
                    <nav style={{ display: "flex", flexWrap: "wrap", gap: 16, fontSize: 13 }}>
                        <Link href="/legal/politique-confidentialite" className="transition-colors duration-200 hover:text-[var(--accent)]" style={{ color: "var(--text-2)", textDecoration: "none" }}>Politique</Link>
                        <Link href="/legal/cgu" className="transition-colors duration-200 hover:text-[var(--accent)]" style={{ color: "var(--text-2)", textDecoration: "none" }}>CGU</Link>
                        <Link href="/legal/dpa" className="transition-colors duration-200 hover:text-[var(--accent)]" style={{ color: "var(--text-2)", textDecoration: "none" }}>DPA</Link>
                        <Link href="/legal/mentions-legales" className="transition-colors duration-200 hover:text-[var(--accent)]" style={{ color: "var(--text-2)", textDecoration: "none" }}>Mentions légales</Link>
                    </nav>
                </div>
            </header>
            {!known ? (
                <Reveal>
                <div style={{ maxWidth: 800, margin: "0 auto", padding: 32, textAlign: "center" }}>
                    <h1 style={{ fontSize: 24, color: "var(--text)" }}>Document introuvable</h1>
                    <p style={{ fontSize: 14, color: "var(--text-2)" }}>
                        Le document demandé n&apos;existe pas. Voir{" "}
                        <Link href="/legal/politique-confidentialite" style={{ color: "var(--accent)" }}>la liste des documents</Link>.
                    </p>
                </div>
                </Reveal>
            ) : (
                <LegalDocumentViewer slug={slug} />
            )}
        </main>
    );
}
