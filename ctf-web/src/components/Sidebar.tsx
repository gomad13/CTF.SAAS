"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { apiFetch } from "@/lib/api";
import type { Me } from "@/lib/types";
import { useQuery } from "@tanstack/react-query";
import { useCompetitionStatus } from "@/hooks/useCompetitionStatus";
import { useModesStatus } from "@/hooks/useModesStatus";
import TenantSwitcher from "./TenantSwitcher";
import {
    LayoutDashboard, BookOpen, Trophy, Settings,
    Users, ShieldAlert, LogOut, Shield,
    BarChart3, CheckCircle2, Target, BookUser,
    Mail, Theater,
} from "lucide-react";

function Item({ href, icon, label, badge, exact = false, danger = false }: {
    href: string; icon: React.ReactNode; label: string;
    badge?: string; exact?: boolean; danger?: boolean;
}) {
    const p = usePathname();
    const active = exact ? p === href : p.startsWith(href);
    return (
        <Link href={href} style={{
            display: "flex", alignItems: "center", gap: "10px", height: "38px",
            padding: "0 12px", borderRadius: "8px", textDecoration: "none",
            fontSize: "13.5px", userSelect: "none", marginBottom: "2px",
            fontWeight: active ? "600" : "400",
            color: danger ? "#FCA5A5" : active ? "#FFFFFF" : "#94A3B8",
            background: danger ? "rgba(239,68,68,0.12)" : active ? "rgba(59,130,246,0.20)" : "transparent",
            borderLeft: danger ? "2px solid #EF4444" : active ? "2px solid var(--accent)" : "2px solid transparent",
            transition: "all 0.15s ease",
        }}
            onMouseEnter={e => { if (!active && !danger) { (e.currentTarget as HTMLElement).style.background = "#1F1F22"; (e.currentTarget as HTMLElement).style.color = "#CBD5E1"; } }}
            onMouseLeave={e => { if (!active && !danger) { (e.currentTarget as HTMLElement).style.background = "transparent"; (e.currentTarget as HTMLElement).style.color = "#94A3B8"; } }}
        >
            <span style={{ color: danger ? "#EF4444" : active ? "#60A5FA" : "#475569", display: "flex", flexShrink: 0 }}>{icon}</span>
            <span style={{ flex: 1, overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap" }}>{label}</span>
            {badge && <span style={{ fontSize: "10px", padding: "1px 7px", background: "rgba(148,163,184,0.15)", color: "#94A3B8", borderRadius: "99px" }}>{badge}</span>}
        </Link>
    );
}

export default function Sidebar({ me: meProp, mobileOpen, onClose }: {
    me?: Me; mobileOpen?: boolean; onClose?: () => void;
}) {
    const meQ = useQuery<Me>({
        queryKey: ["me"],
        queryFn: () => apiFetch<Me>("/api/auth/me"),
        staleTime: 5 * 60 * 1000,
        enabled: !meProp,
    });
    const me = meProp ?? meQ.data;
    const initials = me ? `${me.firstName?.[0] ?? ""}${me.lastName?.[0] ?? ""}`.toUpperCase() : "?";
    const isAdmin = me?.role === "admin" || me?.role === "SuperAdmin";
    const isSuperAdmin = me?.role === "SuperAdmin";
    const competitionQ = useCompetitionStatus();
    const competitionEnabled = competitionQ.data?.isEnabled === true;
    const modesQ = useModesStatus();
    const modes = modesQ.data;

    async function handleLogout() {
        await apiFetch("/api/auth/logout", { method: "POST" }).catch(() => {});
        window.location.replace("/login");
    }

    // Mobile : sidebar sortie du flow (fixed) pour ne pas écraser le contenu principal.
    // Desktop (≥ md = 768px) : sidebar dans le flow flex avec largeur 220px.
    // Bug pré-fix : sidebar en `position: sticky` + width 220px occupait l'espace flex
    // même quand translate-x-full → contenu principal compressé sur smartphone.
    const sidebarClass =
        mobileOpen !== undefined
            ? `${mobileOpen ? "translate-x-0" : "-translate-x-full md:translate-x-0"} fixed md:sticky inset-y-0 left-0 md:inset-auto`
            : "fixed md:sticky inset-y-0 left-0 md:inset-auto";

    return (
        <>
            {mobileOpen && <div style={{ position: "fixed", inset: 0, zIndex: 30, background: "rgba(0,0,0,0.5)" }} className="md:hidden" onClick={onClose} />}
            <aside style={{
                width: "220px", flexShrink: 0, height: "100vh", top: 0,
                display: "flex", flexDirection: "column", background: "#0A0A0B",
                borderRight: "1px solid #E2E8F0", zIndex: 40, transition: "transform 0.2s",
            }} className={sidebarClass}>

                {/* Logo */}
                <div style={{ height: "60px", display: "flex", alignItems: "center", gap: "10px", padding: "0 16px", borderBottom: "1px solid #E2E8F0", flexShrink: 0 }}>
                    <div style={{ width: "32px", height: "32px", background: "linear-gradient(135deg,#1D4ED8,var(--accent))", borderRadius: "9px", display: "flex", alignItems: "center", justifyContent: "center", flexShrink: 0, boxShadow: "0 0 12px rgba(59,130,246,0.4)" }}>
                        <Shield size={16} color="white" strokeWidth={2} />
                    </div>
                    <div>
                        <div style={{ fontSize: "15px", fontWeight: "800", color: "#FFFFFF", lineHeight: "1.1", letterSpacing: "-0.02em" }}>Sentys</div>
                        <div style={{ fontSize: "10px", color: "#94A3B8", lineHeight: "1.2" }}>CTF Platform</div>
                    </div>
                    {onClose && <button onClick={onClose} className="md:hidden" style={{ marginLeft: "auto", background: "none", border: "none", color: "#94A3B8", cursor: "pointer", padding: 4 }}>✕</button>}
                </div>

                {/* [MULTI-SOCIETES] Sélecteur de société (masqué si une seule) */}
                <TenantSwitcher />

                {/* Nav */}
                <nav style={{ flex: 1, overflowY: "auto", padding: "10px 10px" }}>
                    {isAdmin && (
                        <>
                            <div style={{ fontSize: "10px", fontWeight: "700", color: "#CBD5E1", textTransform: "uppercase", letterSpacing: "0.10em", padding: "14px 12px 5px" }}>Administration</div>
                            <Item href="/admin/dashboard" icon={<Users size={15} strokeWidth={1.75} />} label="Administration" />
                            <Item href="/admin/entreprise" icon={<Settings size={15} strokeWidth={1.75} />} label="Paramètres entreprise" />
                            {modes?.analytics && <Item href="/admin/analytics" icon={<BarChart3 size={15} strokeWidth={1.75} />} label="Analytics" />}
                            <Item href="/admin/catalog" icon={<BookOpen size={15} strokeWidth={1.75} />} label="Catalogue" />
                            <Item href="/admin/scenarios" icon={<Theater size={15} strokeWidth={1.75} />} label="Scénarios" />
                            <Item href="/admin/directory" icon={<BookUser size={15} strokeWidth={1.75} />} label="Annuaire" />
                            {modes?.teams && <Item href="/admin/teams" icon={<Users size={15} strokeWidth={1.75} />} label="Équipes" />}
                            {modes?.compliance && <Item href="/admin/compliance" icon={<CheckCircle2 size={15} strokeWidth={1.75} />} label="Compliance" />}
                            {modes?.campaigns && <Item href="/admin/campaigns" icon={<Target size={15} strokeWidth={1.75} />} label="Campagnes" />}
                            {isSuperAdmin && <Item href="/superadmin" icon={<ShieldAlert size={15} strokeWidth={1.75} />} label="SuperAdmin" danger />}
                            <div style={{ height: "1px", background: "rgba(255,255,255,0.06)", margin: "10px 0" }} />
                        </>
                    )}
                    <div style={{ fontSize: "10px", fontWeight: "700", color: "#CBD5E1", textTransform: "uppercase", letterSpacing: "0.10em", padding: "14px 12px 5px" }}>Menu</div>
                    <Item href="/dashboard" icon={<LayoutDashboard size={15} strokeWidth={1.75} />} label="Accueil" exact />
                    <Item href="/dashboard/parcours" icon={<BookOpen size={15} strokeWidth={1.75} />} label="Mes parcours" />
                    {modes?.teams && <Item href="/dashboard/equipes" icon={<Users size={15} strokeWidth={1.75} />} label="Mes équipes" />}
                    <Item href="/inbox" icon={<Mail size={15} strokeWidth={1.75} />} label="Inbox" />
                    {competitionEnabled && (
                        <Item href="/dashboard/competition" icon={<Trophy size={15} strokeWidth={1.75} />} label="Compétition" />
                    )}
                    <Item href="/dashboard/parametres" icon={<Settings size={15} strokeWidth={1.75} />} label="Paramètres" />
                </nav>

                {/* User */}
                <div style={{ borderTop: "1px solid #E2E8F0", padding: "12px 10px", flexShrink: 0 }}>
                    <div style={{ display: "flex", alignItems: "center", gap: "10px", padding: "8px 10px", background: "rgba(255,255,255,0.04)", borderRadius: "10px", marginBottom: "8px" }}>
                        <div style={{ width: "32px", height: "32px", borderRadius: "50%", background: "linear-gradient(135deg,#1D4ED8,var(--accent))", display: "flex", alignItems: "center", justifyContent: "center", fontSize: "11px", fontWeight: "700", color: "white", flexShrink: 0 }}>{initials}</div>
                        <div style={{ flex: 1, minWidth: 0 }}>
                            <div style={{ fontSize: "12.5px", fontWeight: "600", color: "#F1F5F9", overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap" }}>{me?.firstName} {me?.lastName}</div>
                            <div style={{ fontSize: "11px", color: "#94A3B8", overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap" }}>{me?.role}</div>
                        </div>
                    </div>
                    <button onClick={handleLogout} style={{
                        width: "100%", display: "flex", alignItems: "center", justifyContent: "center",
                        gap: "6px", height: "34px", background: "transparent",
                        border: "1px solid #E2E8F0", borderRadius: "8px",
                        color: "#94A3B8", fontSize: "12.5px", cursor: "pointer", fontFamily: "inherit", transition: "all 0.15s",
                    }}
                        onMouseEnter={e => { (e.currentTarget as HTMLElement).style.color = "#FCA5A5"; (e.currentTarget as HTMLElement).style.borderColor = "rgba(239,68,68,0.3)"; (e.currentTarget as HTMLElement).style.background = "rgba(239,68,68,0.08)"; }}
                        onMouseLeave={e => { (e.currentTarget as HTMLElement).style.color = "#94A3B8"; (e.currentTarget as HTMLElement).style.borderColor = "rgba(255,255,255,0.08)"; (e.currentTarget as HTMLElement).style.background = "transparent"; }}
                    >
                        <LogOut size={13} strokeWidth={1.75} />
                        Déconnexion
                    </button>
                </div>
            </aside>
        </>
    );
}
