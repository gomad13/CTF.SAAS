"use client";

import { useState } from "react";
import Link from "next/link";

export default function Navbar() {
    const [menuOpen, setMenuOpen] = useState(false);

    return (
        <header style={{
            position: "sticky",
            top: 0,
            zIndex: 50,
            background: "rgba(2,12,12,0.88)",
            backdropFilter: "blur(14px)",
            WebkitBackdropFilter: "blur(14px)",
            borderBottom: "1px solid rgba(59,130,246,0.12)",
        }}>
            <div style={{
                maxWidth: 1200,
                margin: "0 auto",
                display: "flex",
                alignItems: "center",
                justifyContent: "space-between",
                padding: "0 var(--page-x)",
                height: 60,
            }}>
                {/* Logo */}
                <Link href="/" style={{ display: "flex", alignItems: "center", gap: 8, textDecoration: "none", flexShrink: 0, whiteSpace: "nowrap" }}>
                    <svg width="24" height="16" viewBox="0 0 28 18" fill="none" aria-hidden style={{ flexShrink: 0 }}>
                        <path
                            d="M2,9 C4,4 8,2 12,5 S18,12 22,9 S26,3 28,6"
                            fill="none" stroke="var(--pr)" strokeWidth="2.5"
                            strokeLinecap="round"
                            style={{ filter: "drop-shadow(0 0 4px #3B82F6)" }}
                        />
                        <circle cx="26" cy="7" r="2.5" fill="var(--pr-l)" style={{ filter: "drop-shadow(0 0 6px #60A5FA)" }} />
                        <circle cx="4" cy="10" r="1.5" fill="rgba(59,130,246,0.5)" />
                    </svg>
                    <span style={{ fontWeight: 800, fontSize: 20, color: "var(--pr)", whiteSpace: "nowrap" }}>
                        Sentys
                    </span>
                </Link>

                {/* Centre — Liens nav (desktop) */}
                <nav style={{ alignItems: "center", gap: 32 }} className="hidden md:flex">
                    <NavLink href="#features">Fonctionnalités</NavLink>
                    <NavLink href="#audience">Pour qui ?</NavLink>
                    <NavLink href="#contact">Contact</NavLink>
                </nav>

                {/* Droite — CTAs (desktop) */}
                <div style={{ alignItems: "center", gap: 10 }} className="hidden md:flex">
                    <Link href="/login" style={{
                        background: "transparent",
                        border: "1px solid rgba(59,130,246,0.35)",
                        color: "var(--pr)",
                        borderRadius: 7,
                        padding: "8px 18px",
                        fontSize: 13,
                        textDecoration: "none",
                        transition: "background 0.15s",
                    }}
                        onMouseOver={e => { e.currentTarget.style.background = "rgba(59,130,246,0.08)"; }}
                        onMouseOut={e => { e.currentTarget.style.background = "transparent"; }}
                    >
                        Connexion
                    </Link>
                    <Link href="/login" style={{
                        background: "linear-gradient(135deg, #3B82F6, #2563EB)",
                        color: "#FFFFFF",
                        fontWeight: 700,
                        fontSize: 13,
                        borderRadius: 7,
                        padding: "8px 18px",
                        textDecoration: "none",
                        transition: "box-shadow 0.2s",
                    }}
                        onMouseOver={e => { e.currentTarget.style.boxShadow = "0 0 16px rgba(59,130,246,0.35)"; }}
                        onMouseOut={e => { e.currentTarget.style.boxShadow = "none"; }}
                    >
                        Demander une démo
                    </Link>
                </div>

                {/* Mobile burger */}
                <button
                    onClick={() => setMenuOpen(o => !o)}
                    style={{ background: "none", border: "none", color: "#CBD5E1", cursor: "pointer", width: 44, height: 44, alignItems: "center", justifyContent: "center", flexShrink: 0 }}
                    className="flex md:hidden"
                    aria-label={menuOpen ? "Fermer le menu" : "Ouvrir le menu"}
                >
                    <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                        {menuOpen
                            ? <><line x1="18" y1="6" x2="6" y2="18" /><line x1="6" y1="6" x2="18" y2="18" /></>
                            : <><line x1="3" y1="6" x2="21" y2="6" /><line x1="3" y1="12" x2="21" y2="12" /><line x1="3" y1="18" x2="21" y2="18" /></>
                        }
                    </svg>
                </button>
            </div>

            {/* Mobile menu */}
            {menuOpen && (
                <div style={{
                    background: "var(--bg-surface)",
                    borderTop: "1px solid var(--border)",
                    padding: "16px var(--page-x)",
                    display: "flex",
                    flexDirection: "column",
                    gap: 12,
                }}>
                    <NavLink href="#features" onClick={() => setMenuOpen(false)}>Fonctionnalités</NavLink>
                    <NavLink href="#audience" onClick={() => setMenuOpen(false)}>Pour qui ?</NavLink>
                    <NavLink href="#contact" onClick={() => setMenuOpen(false)}>Contact</NavLink>
                    <Link href="/login" style={{
                        textAlign: "center",
                        padding: "13px 18px",
                        minHeight: 44,
                        color: "var(--pr)",
                        border: "1px solid rgba(59,130,246,0.35)",
                        borderRadius: 7,
                        textDecoration: "none",
                        fontSize: 13,
                    }} onClick={() => setMenuOpen(false)}>
                        Connexion
                    </Link>
                    <Link href="/login" style={{
                        textAlign: "center",
                        padding: "13px 18px",
                        minHeight: 44,
                        background: "linear-gradient(135deg, #3B82F6, #2563EB)",
                        color: "#FFFFFF",
                        borderRadius: 7,
                        textDecoration: "none",
                        fontWeight: 700,
                        fontSize: 13,
                    }} onClick={() => setMenuOpen(false)}>
                        Demander une démo
                    </Link>
                </div>
            )}
        </header>
    );
}

function NavLink({ href, children, onClick }: { href: string; children: React.ReactNode; onClick?: () => void }) {
    return (
        <a
            href={href}
            onClick={onClick}
            style={{
                color: "#CBD5E1",
                fontSize: 16,
                fontWeight: 500,
                textDecoration: "none",
                transition: "color 0.15s",
                display: "inline-flex",
                alignItems: "center",
                minHeight: 44,
                padding: "2px 2px",
            }}
            onMouseOver={e => { e.currentTarget.style.color = "var(--pr)"; }}
            onMouseOut={e => { e.currentTarget.style.color = "#CBD5E1"; }}
        >
            {children}
        </a>
    );
}
