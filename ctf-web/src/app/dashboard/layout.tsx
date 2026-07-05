"use client";

import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { apiFetch } from "@/lib/api";
import Sidebar from "@/components/Sidebar";
import ChatWidget from "@/components/ChatWidget";
import Footer from "@/components/Footer";
import { CoachingHost } from "@/components/coaching/CoachingHost";
import type { Me } from "@/lib/types";

export default function DashboardLayout({ children }: { children: React.ReactNode }) {
    const [mobileOpen, setMobileOpen] = useState(false);

    const { data: me, isLoading, isError } = useQuery<Me>({
        queryKey: ["me"],
        queryFn: () => apiFetch<Me>("/api/auth/me"),
        retry: false,
        staleTime: 5 * 60 * 1000,
    });

    if (isLoading) {
        return (
            <div style={{
                display: "flex", minHeight: "100svh",
                alignItems: "center", justifyContent: "center",
                background: "var(--bg)",
            }}>
                <div style={{
                    width: 32, height: 32,
                    border: "2px solid var(--accent)",
                    borderTopColor: "transparent",
                    borderRadius: "50%",
                    animation: "spin 0.8s linear infinite",
                }} />
            </div>
        );
    }

    if (isError || !me) return null;

    return (
        <div style={{
            display: "flex",
            height: "100svh",
            background: "var(--bg)",
            overflow: "hidden",
        }}>
            <Sidebar me={me} mobileOpen={mobileOpen} onClose={() => setMobileOpen(false)} />

            <div style={{
                flex: 1,
                display: "flex",
                flexDirection: "column",
                minWidth: 0,
                overflow: "hidden",
            }}>
                {/* Topbar */}
                <header style={{
                    height: 56,
                    flexShrink: 0,
                    display: "flex",
                    alignItems: "center",
                    justifyContent: "space-between",
                    padding: "0 24px",
                    background: "var(--bg)",
                    borderBottom: "1px solid var(--border)",
                    position: "sticky",
                    top: 0,
                    zIndex: 10,
                }}>
                    <div style={{ display: "flex", alignItems: "center", gap: 12 }}>
                        <button
                            onClick={() => setMobileOpen(true)}
                            className="md:hidden transition-colors duration-200 hover:text-fg-heading"
                            style={{ background: "none", border: "none", color: "var(--text-2)", cursor: "pointer", padding: 4 }}
                            aria-label="Menu"
                        >
                            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                                <line x1="3" y1="6" x2="21" y2="6" />
                                <line x1="3" y1="12" x2="21" y2="12" />
                                <line x1="3" y1="18" x2="21" y2="18" />
                            </svg>
                        </button>
                        <span style={{ fontWeight: 600, color: "var(--text)", fontSize: 14 }}>
                            Dashboard
                        </span>
                    </div>
                    {me.tenantId === "00000000-0000-0000-0000-000000000000" && (
                        <span style={{
                            background: "color-mix(in srgb, var(--accent) 10%, transparent)",
                            color: "var(--accent)",
                            border: "1px solid color-mix(in srgb, var(--accent) 20%, transparent)",
                            fontSize: 11,
                            fontWeight: 600,
                            padding: "3px 10px",
                            borderRadius: 99,
                        }}>
                            DEMO
                        </span>
                    )}
                </header>

                <main style={{ flex: 1, overflowY: "auto", overflowX: "hidden" }}>
                    {children}
                    <Footer compact />
                </main>
            </div>
            <ChatWidget />
            {/* Host global du coaching post-incident : écoute l'event window
                "sentys:coaching:trigger" et monte la modal en lazy load. */}
            <CoachingHost />
        </div>
    );
}
