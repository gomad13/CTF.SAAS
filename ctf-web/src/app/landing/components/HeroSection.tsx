"use client";

import { useEffect, useState } from "react";
import Link from "next/link";

export default function HeroSection() {
    const [mounted, setMounted] = useState(false);
    useEffect(() => setMounted(true), []);

    return (
        <section style={{
            position: "relative",
            minHeight: "92vh",
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            padding: "60px var(--page-x) 80px",
        }}>
            <div style={{
                maxWidth: 760,
                margin: "0 auto",
                textAlign: "center",
                display: "flex",
                flexDirection: "column",
                alignItems: "center",
            }}>
                {/* Badge */}
                <div style={{
                    opacity: mounted ? 1 : 0,
                    transform: mounted ? "translateY(0)" : "translateY(12px)",
                    transition: "opacity 0.5s ease, transform 0.5s ease",
                }}>
                    <span style={{
                        display: "inline-flex",
                        alignItems: "center",
                        gap: 8,
                        background: "color-mix(in srgb, var(--accent) 7%, transparent)",
                        border: "1px solid color-mix(in srgb, var(--accent) 22%, transparent)",
                        borderRadius: 20,
                        padding: "5px 14px",
                        fontFamily: "'JetBrains Mono', monospace",
                        fontSize: 11,
                        letterSpacing: "0.14em",
                        color: "var(--pr-l)",
                    }}>
                        <span style={{
                            width: 6,
                            height: 6,
                            borderRadius: "50%",
                            background: "var(--pr)",
                            boxShadow: "0 0 8px var(--accent)",
                            animation: "pulseDot 2s infinite",
                            flexShrink: 0,
                        }} />
                        PLATEFORME CTF B2B
                    </span>
                </div>

                {/* Titre */}
                <h1 style={{
                    fontSize: "clamp(36px, 5.5vw, 68px)",
                    fontWeight: 800,
                    lineHeight: 1.08,
                    color: "var(--text)",
                    textShadow: "0 1px 2px rgba(0,0,0,0.04)",
                    marginTop: 28,
                    opacity: mounted ? 1 : 0,
                    transform: mounted ? "translateY(0)" : "translateY(12px)",
                    transition: "opacity 0.5s ease 0.1s, transform 0.5s ease 0.1s",
                }}>
                    Préparez vos équipes<br />
                    aux menaces{" "}
                    <span style={{
                        color: "var(--pr)",
                        textShadow: "0 0 24px color-mix(in srgb, var(--accent) 45%, transparent)",
                    }}>
                        cyber
                    </span>
                    <br />
                    par la mise en situation
                </h1>

                {/* Sous-titre */}
                <p style={{
                    maxWidth: 560,
                    fontSize: 17,
                    color: "var(--text-2)",
                    lineHeight: 1.65,
                    marginTop: 24,
                    opacity: mounted ? 1 : 0,
                    transform: mounted ? "translateY(0)" : "translateY(12px)",
                    transition: "opacity 0.5s ease 0.2s, transform 0.5s ease 0.2s",
                }}>
                    Plateforme B2B de formation en cybersécurité.
                    Vos collaborateurs s&apos;entraînent sur des scénarios
                    réels, progressent à leur rythme et développent
                    des réflexes durables face aux attaques.
                </p>

                {/* Boutons CTA */}
                <div style={{
                    display: "flex",
                    flexWrap: "wrap",
                    gap: 12,
                    justifyContent: "center",
                    marginTop: 36,
                    opacity: mounted ? 1 : 0,
                    transform: mounted ? "translateY(0)" : "translateY(12px)",
                    transition: "opacity 0.5s ease 0.35s, transform 0.5s ease 0.35s",
                }}>
                    <HoverBtn href="/login" primary>
                        Demander une démo →
                    </HoverBtn>
                    <HoverBtn href="#features">
                        Voir les modules
                    </HoverBtn>
                </div>
            </div>

            {/* Scroll indicator */}
            <div
                onClick={() => document.getElementById("features")?.scrollIntoView({ behavior: "smooth" })}
                role="button"
                tabIndex={0}
                aria-label="Défiler vers les fonctionnalités"
                style={{
                position: "absolute",
                bottom: 28,
                left: "50%",
                transform: "translateX(-50%)",
                display: "flex",
                flexDirection: "column",
                alignItems: "center",
                gap: 6,
                cursor: "pointer",
                textDecoration: "none",
            }}>
                <span style={{
                    fontSize: 10,
                    letterSpacing: "0.2em",
                    textTransform: "uppercase",
                    fontFamily: "'JetBrains Mono', monospace",
                    color: "var(--text-3)",
                }}>
                    DÉFILER
                </span>
                <svg
                    style={{ animation: "bounce 2s ease-in-out infinite", color: "var(--text-3)" }}
                    width="16" height="16" fill="none" stroke="currentColor" strokeWidth={2} viewBox="0 0 24 24"
                >
                    <path strokeLinecap="round" strokeLinejoin="round" d="M19 9l-7 7-7-7" />
                </svg>
            </div>

            <style>{`
                @keyframes pulseDot {
                    0%, 100% { opacity: 1; }
                    50% { opacity: 0.4; }
                }
                @keyframes bounce {
                    0%, 100% { transform: translateY(0); }
                    50% { transform: translateY(6px); }
                }
            `}</style>
        </section>
    );
}

function HoverBtn({ href, children, primary = false }: {
    href: string;
    children: React.ReactNode;
    primary?: boolean;
}) {
    const [hov, setHov] = useState(false);
    const isAnchor = href.startsWith("#");

    const style: React.CSSProperties = primary ? {
        background: hov
            ? "linear-gradient(135deg, var(--accent-hover), var(--accent))"
            : "linear-gradient(135deg, var(--accent), var(--accent-hover))",
        color: "var(--on-accent)",
        fontWeight: 700,
        fontSize: 15,
        padding: "14px 32px",
        borderRadius: 8,
        border: "none",
        cursor: "pointer",
        boxShadow: hov ? "0 0 24px color-mix(in srgb, var(--accent) 50%, transparent)" : "0 0 12px color-mix(in srgb, var(--accent) 30%, transparent)",
        transform: hov ? "translateY(-1px)" : "none",
        transition: "all 0.2s",
        textDecoration: "none",
        display: "inline-flex",
        alignItems: "center",
    } : {
        background: hov ? "color-mix(in srgb, var(--accent) 8%, transparent)" : "transparent",
        border: `1px solid ${hov ? "color-mix(in srgb, var(--accent) 60%, transparent)" : "color-mix(in srgb, var(--accent) 35%, transparent)"}`,
        color: "var(--pr)",
        fontWeight: 600,
        fontSize: 15,
        padding: "13px 28px",
        borderRadius: 8,
        cursor: "pointer",
        transition: "all 0.2s",
        textDecoration: "none",
        display: "inline-flex",
        alignItems: "center",
    };

    if (isAnchor) {
        return (
            <a href={href} style={style}
                onMouseEnter={() => setHov(true)}
                onMouseLeave={() => setHov(false)}
            >
                {children}
            </a>
        );
    }
    return (
        <Link href={href} style={style}
            onMouseEnter={() => setHov(true)}
            onMouseLeave={() => setHov(false)}
        >
            {children}
        </Link>
    );
}
