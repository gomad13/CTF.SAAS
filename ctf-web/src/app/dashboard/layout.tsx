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
                background: "#0F172A",
            }}>
                <div style={{
                    width: 32, height: 32,
                    border: "2px solid #3B82F6",
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
            background: "#0F172A",
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
                    background: "#0F172A",
                    borderBottom: "1px solid rgba(255,255,255,0.06)",
                    position: "sticky",
                    top: 0,
                    zIndex: 10,
                }}>
                    <div style={{ display: "flex", alignItems: "center", gap: 12 }}>
                        <button
                            onClick={() => setMobileOpen(true)}
                            className="md:hidden"
                            style={{ background: "none", border: "none", color: "#64748B", cursor: "pointer", padding: 4 }}
                            aria-label="Menu"
                        >
                            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                                <line x1="3" y1="6" x2="21" y2="6" />
                                <line x1="3" y1="12" x2="21" y2="12" />
                                <line x1="3" y1="18" x2="21" y2="18" />
                            </svg>
                        </button>
                        <span style={{ fontWeight: 600, color: "#F1F5F9", fontSize: 14 }}>
                            Dashboard
                        </span>
                    </div>
                    {me.tenantId === "00000000-0000-0000-0000-000000000000" && (
                        <span style={{
                            background: "rgba(59,130,246,0.10)",
                            color: "#1E40AF",
                            border: "1px solid rgba(59,130,246,0.20)",
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
                "viper:coaching:trigger" et monte la modal en lazy load. */}
            <CoachingHost />
        </div>
    );
}
