"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiFetch } from "@/lib/api";
import type { Me } from "@/lib/types";

type Company = {
    id: string;
    name: string;
    sector: string;
    size: string;
    city: string;
    siret: string;
    employeeCount: number;
};

type CompanyUser = {
    id: string;
    firstName: string;
    lastName: string;
    email: string;
    role: string;
    isActive: boolean;
    createdAt: string;
    lastLoginAt: string | null;
    totalPoints: number;
    completedChallenges: number;
};

type CompanyStats = {
    totalUsers: number;
    activeUsers: number;
    totalCompletions: number;
    averageScore: number;
};

export default function AdminPage() {
    const router = useRouter();
    const qc = useQueryClient();

    const meQ = useQuery<Me>({
        queryKey: ["me"],
        queryFn: () => apiFetch<Me>("/api/auth/me"),
        staleTime: 5 * 60 * 1000,
    });

    useEffect(() => {
        if (meQ.data && meQ.data.role?.toLowerCase() !== "admin") {
            router.replace("/dashboard");
        }
    }, [meQ.data, router]);

    const companyQ = useQuery<Company>({
        queryKey: ["admin", "company"],
        queryFn: () => apiFetch<Company>("/api/admin/company"),
        enabled: !!meQ.data && meQ.data.role?.toLowerCase() === "admin",
    });

    const usersQ = useQuery<{ data: CompanyUser[]; pagination: { page: number; pageSize: number; total: number; totalPages: number } }>({
        queryKey: ["admin", "users"],
        queryFn: () => apiFetch("/api/admin/users?pageSize=200"),
        enabled: !!meQ.data && meQ.data.role?.toLowerCase() === "admin",
    });

    const statsQ = useQuery<CompanyStats>({
        queryKey: ["admin", "stats"],
        queryFn: () => apiFetch<CompanyStats>("/api/admin/stats"),
        enabled: !!meQ.data && meQ.data.role?.toLowerCase() === "admin",
    });

    const toggleM = useMutation({
        mutationFn: (userId: string) =>
            apiFetch<{ id: string; isActive: boolean }>(`/api/admin/users/${userId}/toggle-active`, { method: "PATCH" }),
        onSuccess: () => {
            qc.invalidateQueries({ queryKey: ["admin", "users"] });
            qc.invalidateQueries({ queryKey: ["admin", "stats"] });
        },
    });

    if (meQ.isLoading || !meQ.data) return <div style={{ padding: 32 }} />;
    if (meQ.data.role?.toLowerCase() !== "admin") return null;

    const company = companyQ.data;
    const users = usersQ.data?.data ?? [];
    const stats = statsQ.data;

    return (
        <div style={{ maxWidth: 1100, margin: "0 auto", padding: "32px var(--page-x) 80px" }}>
            {/* Header */}
            <div style={{ marginBottom: 32 }}>
                <h1 style={{ fontSize: 26, fontWeight: 700, color: "var(--text-primary)", margin: 0 }}>
                    Administration
                </h1>
                <p style={{ marginTop: 6, fontSize: 14, color: "var(--pr)", fontFamily: "'JetBrains Mono', monospace" }}>
                    {company?.name ?? "—"}
                </p>
                {company && (
                    <p style={{ marginTop: 4, fontSize: 12, color: "var(--text-muted)" }}>
                        {company.sector} · {company.city} · SIRET {company.siret}
                    </p>
                )}
            </div>

            {/* Stats cards */}
            <div style={{
                display: "grid",
                gridTemplateColumns: "repeat(auto-fit, minmax(200px, 1fr))",
                gap: 16,
                marginBottom: 32,
            }}>
                <StatCard value={stats?.totalUsers ?? 0} label="COLLABORATEURS" />
                <StatCard value={stats?.activeUsers ?? 0} label="ACTIFS" />
                <StatCard value={stats?.totalCompletions ?? 0} label="FORMATIONS FAITES" />
                <StatCard value={`${stats?.averageScore ?? 0}%`} label="SCORE MOYEN" />
            </div>

            {/* Users table */}
            <section>
                <h2 style={{ fontSize: 18, fontWeight: 600, color: "var(--text-primary)", marginBottom: 16 }}>
                    Mes collaborateurs
                </h2>

                <div className="resp-scroll-x" style={{
                    background: "var(--bg-card)",
                    border: "1px solid rgba(59,130,246,0.1)",
                    borderRadius: 10,
                }}>
                    <div style={{ minWidth: 720 }}>
                    {/* Header */}
                    <div style={{
                        display: "grid",
                        gridTemplateColumns: "1.5fr 2fr 0.8fr 0.8fr 0.8fr 1fr",
                        gap: 12,
                        padding: "12px 20px",
                        borderBottom: "1px solid rgba(59,130,246,0.12)",
                        background: "rgba(59,130,246,0.04)",
                        fontFamily: "'JetBrains Mono', monospace",
                        fontSize: 10,
                        letterSpacing: "0.1em",
                        textTransform: "uppercase",
                        color: "var(--text-muted)",
                    }}>
                        <div>NOM</div>
                        <div>EMAIL</div>
                        <div>RÔLE</div>
                        <div>STATUT</div>
                        <div style={{ textAlign: "right" }}>POINTS</div>
                        <div style={{ textAlign: "right" }}>ACTIONS</div>
                    </div>

                    {usersQ.isLoading && (
                        <div style={{ padding: 24, textAlign: "center", color: "var(--text-muted)", fontSize: 13 }}>
                            Chargement…
                        </div>
                    )}

                    {users.map((u) => {
                        const initials = `${u.firstName[0] ?? ""}${u.lastName[0] ?? ""}`.toUpperCase();
                        return (
                            <div
                                key={u.id}
                                style={{
                                    display: "grid",
                                    gridTemplateColumns: "1.5fr 2fr 0.8fr 0.8fr 0.8fr 1fr",
                                    gap: 12,
                                    padding: "14px 20px",
                                    borderBottom: "1px solid rgba(59,130,246,0.06)",
                                    alignItems: "center",
                                    transition: "background 0.15s",
                                }}
                                onMouseOver={e => { e.currentTarget.style.background = "rgba(59,130,246,0.04)"; }}
                                onMouseOut={e => { e.currentTarget.style.background = "transparent"; }}
                            >
                                {/* Nom + avatar */}
                                <div style={{ display: "flex", alignItems: "center", gap: 10, minWidth: 0 }}>
                                    <div style={{
                                        width: 32,
                                        height: 32,
                                        borderRadius: "50%",
                                        background: "rgba(59,130,246,0.15)",
                                        display: "flex",
                                        alignItems: "center",
                                        justifyContent: "center",
                                        fontSize: 11,
                                        fontWeight: 700,
                                        color: "var(--pr)",
                                        flexShrink: 0,
                                    }}>
                                        {initials}
                                    </div>
                                    <span style={{ color: "var(--text-primary)", fontSize: 14, fontWeight: 500, overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap" }}>
                                        {u.firstName} {u.lastName}
                                    </span>
                                </div>

                                {/* Email */}
                                <span style={{ color: "var(--text-secondary)", fontSize: 13, overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap" }}>
                                    {u.email}
                                </span>

                                {/* Rôle */}
                                <span style={{
                                    fontSize: 10,
                                    padding: "2px 8px",
                                    borderRadius: 4,
                                    background: u.role === "admin" ? "rgba(59,130,246,0.12)" : "var(--border)",
                                    border: u.role === "admin" ? "1px solid rgba(59,130,246,0.3)" : "1px solid rgba(255,255,255,0.08)",
                                    color: u.role === "admin" ? "var(--pr)" : "#a3a3a3",
                                    fontFamily: "'JetBrains Mono', monospace",
                                    width: "fit-content",
                                    textTransform: "uppercase",
                                }}>
                                    {u.role}
                                </span>

                                {/* Statut */}
                                <span style={{
                                    fontSize: 10,
                                    padding: "2px 8px",
                                    borderRadius: 4,
                                    background: u.isActive ? "rgba(34,197,94,0.1)" : "rgba(239,68,68,0.1)",
                                    border: `1px solid ${u.isActive ? "#10B981" : "#ef4444"}`,
                                    color: u.isActive ? "#4ade80" : "#f87171",
                                    fontFamily: "'JetBrains Mono', monospace",
                                    width: "fit-content",
                                    textTransform: "uppercase",
                                }}>
                                    {u.isActive ? "ACTIF" : "INACTIF"}
                                </span>

                                {/* Points */}
                                <span style={{
                                    textAlign: "right",
                                    fontFamily: "'JetBrains Mono', monospace",
                                    color: "var(--pr)",
                                    fontWeight: 600,
                                    fontSize: 14,
                                }}>
                                    {u.totalPoints}
                                </span>

                                {/* Action */}
                                <div style={{ textAlign: "right" }}>
                                    <button
                                        onClick={() => toggleM.mutate(u.id)}
                                        disabled={toggleM.isPending}
                                        style={{
                                            background: "transparent",
                                            border: `1px solid ${u.isActive ? "rgba(239,68,68,0.35)" : "rgba(34,197,94,0.35)"}`,
                                            color: u.isActive ? "#f87171" : "#4ade80",
                                            fontSize: 11,
                                            fontFamily: "'JetBrains Mono', monospace",
                                            fontWeight: 600,
                                            padding: "5px 12px",
                                            borderRadius: 6,
                                            cursor: toggleM.isPending ? "not-allowed" : "pointer",
                                            transition: "background 0.15s",
                                        }}
                                    >
                                        {u.isActive ? "Désactiver" : "Activer"}
                                    </button>
                                </div>
                            </div>
                        );
                    })}

                    {!usersQ.isLoading && users.length === 0 && (
                        <div style={{ padding: 32, textAlign: "center", color: "var(--text-muted)", fontSize: 13 }}>
                            Aucun collaborateur dans ce tenant.
                        </div>
                    )}
                    </div>
                </div>
            </section>
        </div>
    );
}

function StatCard({ value, label }: { value: string | number; label: string }) {
    return (
        <div style={{
            background: "var(--bg-card)",
            border: "1px solid var(--border)",
            borderRadius: 10,
            padding: 20,
        }}>
            <div style={{
                fontSize: 32,
                fontWeight: 700,
                fontFamily: "'JetBrains Mono', monospace",
                color: "var(--pr)",
            }}>
                {value}
            </div>
            <div style={{
                fontSize: 11,
                letterSpacing: "0.1em",
                textTransform: "uppercase",
                color: "var(--text-muted)",
                marginTop: 4,
            }}>
                {label}
            </div>
        </div>
    );
}
