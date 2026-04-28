"use client";

import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { apiFetch } from "@/lib/api";
import Sidebar from "@/components/Sidebar";
import Footer from "@/components/Footer";
import type { Me } from "@/lib/types";

export default function AdminLayout({ children }: { children: React.ReactNode }) {
    const [mobileOpen, setMobileOpen] = useState(false);

    const { data: me, isLoading, isError } = useQuery<Me>({
        queryKey: ["me"],
        queryFn: () => apiFetch<Me>("/api/auth/me"),
        retry: false,
        staleTime: 5 * 60 * 1000,
    });

    if (isLoading) {
        return (
            <div style={{ display: "flex", minHeight: "100vh", alignItems: "center", justifyContent: "center" }}>
                <div style={{
                    width: 32,
                    height: 32,
                    border: "2px solid var(--primary)",
                    borderTopColor: "transparent",
                    borderRadius: "50%",
                    animation: "spin 0.8s linear infinite",
                }} />
                <style>{`@keyframes spin { to { transform: rotate(360deg); } }`}</style>
            </div>
        );
    }

    if (isError || !me) return null;

    return (
        <div style={{ display: "flex", minHeight: "100vh", background: "#0F172A" }}>
            <Sidebar me={me} mobileOpen={mobileOpen} onClose={() => setMobileOpen(false)} />

            <div style={{ flex: 1, display: "flex", flexDirection: "column", minWidth: 0 }}>
                <header style={{
                    position: "sticky",
                    top: 0,
                    zIndex: 20,
                    background: "#0F172A",
                    borderBottom: "1px solid rgba(255,255,255,0.06)",
                    padding: "0 24px",
                    height: 56,
                    display: "flex",
                    alignItems: "center",
                    justifyContent: "space-between",
                }}>
                    <div style={{ display: "flex", alignItems: "center", gap: 12 }}>
                        <button
                            onClick={() => setMobileOpen(true)}
                            style={{ background: "none", border: "none", color: "#64748B", cursor: "pointer", padding: 4 }}
                            className="md:hidden"
                            aria-label="Ouvrir le menu"
                        >
                            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                                <line x1="3" y1="6" x2="21" y2="6" />
                                <line x1="3" y1="12" x2="21" y2="12" />
                                <line x1="3" y1="18" x2="21" y2="18" />
                            </svg>
                        </button>
                        <span style={{ fontWeight: 600, color: "#F1F5F9", fontSize: 15 }}>
                            Administration
                        </span>
                    </div>
                </header>

                <main style={{ flex: 1, minWidth: 0, overflowX: "hidden" }}>
                    {children}
                    <Footer compact />
                </main>
            </div>
        </div>
    );
}
