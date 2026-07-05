"use client";

import { useEffect, useState } from "react";
import { apiFetch } from "@/lib/api";
import { Building2, ChevronDown, Check } from "lucide-react";

type TenantItem = {
    tenantId: string;
    name: string;
    role: string;
    isActive: boolean;
    isDefault: boolean;
    enabled: boolean;
};
type TenantsResponse = { activeTenantId: string; tenants: TenantItem[] };

/**
 * [MULTI-SOCIETES] Sélecteur de société active.
 * Masqué si l'utilisateur n'appartient qu'à une seule société.
 */
export default function TenantSwitcher() {
    const [data, setData] = useState<TenantsResponse | null>(null);
    const [open, setOpen] = useState(false);
    const [switching, setSwitching] = useState(false);

    useEffect(() => {
        apiFetch<TenantsResponse>("/api/me/tenants")
            .then(setData)
            .catch(() => setData(null));
    }, []);

    if (!data || data.tenants.length <= 1) return null; // mono-société : masqué

    const active = data.tenants.find((t) => t.isActive) ?? data.tenants[0];

    async function switchTo(tenantId: string) {
        if (switching || tenantId === active.tenantId) {
            setOpen(false);
            return;
        }
        setSwitching(true);
        try {
            await apiFetch("/api/me/active-tenant", {
                method: "POST",
                body: JSON.stringify({ tenantId }),
            });
            window.location.reload();
        } catch {
            setSwitching(false);
            setOpen(false);
        }
    }

    return (
        <div style={{ padding: "10px 12px", borderBottom: "1px solid var(--border)", position: "relative", flexShrink: 0 }}>
            <div style={{ fontSize: "9.5px", fontWeight: 700, color: "var(--text-2)", textTransform: "uppercase", letterSpacing: "0.10em", marginBottom: 6 }}>
                Société
            </div>
            <button
                onClick={() => setOpen((o) => !o)}
                disabled={switching}
                style={{
                    width: "100%", display: "flex", alignItems: "center", gap: 8,
                    padding: "8px 10px", background: "var(--surface-2)",
                    border: "1px solid var(--border)", borderRadius: 8,
                    color: "var(--text)", fontSize: "12.5px", cursor: switching ? "wait" : "pointer",
                    fontFamily: "inherit", textAlign: "left",
                }}
            >
                <Building2 size={14} color="var(--accent)" strokeWidth={1.75} style={{ flexShrink: 0 }} />
                <span style={{ flex: 1, overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap" }}>{active.name}</span>
                <ChevronDown size={14} color="var(--text-3)" style={{ flexShrink: 0, transform: open ? "rotate(180deg)" : "none", transition: "transform .15s" }} />
            </button>
            {open && (
                <div
                    style={{
                        position: "absolute", left: 12, right: 12, top: "100%", marginTop: 4, zIndex: 50,
                        background: "var(--surface-2)", border: "1px solid var(--border)", borderRadius: 8,
                        boxShadow: "0 8px 24px rgba(0,0,0,0.4)", overflow: "hidden",
                    }}
                >
                    {data.tenants.map((t) => (
                        <button
                            key={t.tenantId}
                            onClick={() => switchTo(t.tenantId)}
                            style={{
                                width: "100%", display: "flex", alignItems: "center", gap: 8,
                                padding: "9px 11px", background: t.isActive ? "var(--accent-subtle)" : "transparent",
                                border: "none", color: t.isActive ? "var(--text)" : "var(--text-2)",
                                fontSize: "12.5px", cursor: "pointer", fontFamily: "inherit", textAlign: "left",
                            }}
                            onMouseEnter={(e) => { if (!t.isActive) (e.currentTarget as HTMLElement).style.background = "var(--surface)"; }}
                            onMouseLeave={(e) => { if (!t.isActive) (e.currentTarget as HTMLElement).style.background = "transparent"; }}
                        >
                            <span style={{ flex: 1, overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap" }}>{t.name}</span>
                            <span style={{ fontSize: "10px", color: "var(--text-3)" }}>{t.role}</span>
                            {t.isActive && <Check size={13} color="var(--accent)" style={{ flexShrink: 0 }} />}
                        </button>
                    ))}
                </div>
            )}
        </div>
    );
}
