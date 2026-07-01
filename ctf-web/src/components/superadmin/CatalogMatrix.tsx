"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { apiFetch } from "@/lib/api";

type CatalogPath = {
    id: string;
    title: string;
    sector: string | null;
    level: string | null;
};

type TenantRow = {
    id: string;
    name: string;
    isActive: boolean;
};

type MatrixData = {
    paths: CatalogPath[];
    tenants: TenantRow[];
    access: Record<string, Set<string>>; // tenantId -> Set<pathId>
};

const card: React.CSSProperties = {
    background: "#0d0000",
    border: "1px solid rgba(239,68,68,0.15)",
    borderRadius: 8,
    padding: 20,
};

const btnGhost: React.CSSProperties = {
    padding: "6px 12px", background: "transparent",
    color: "#94A3B8", border: "1px solid rgba(239,68,68,0.25)",
    borderRadius: 6, fontSize: 12, cursor: "pointer", transition: "all 200ms"
};

const btnPrimary: React.CSSProperties = {
    padding: "8px 14px", background: "#ef4444",
    color: "#fff", border: "none",
    borderRadius: 6, fontSize: 13, fontWeight: 500, cursor: "pointer", transition: "background 200ms"
};

const chip: React.CSSProperties = {
    padding: "4px 10px", borderRadius: 999, fontSize: 11,
    background: "rgba(239,68,68,0.08)", color: "#f87171",
    border: "1px solid rgba(239,68,68,0.2)", cursor: "pointer",
    fontFamily: "'JetBrains Mono', monospace"
};

export default function CatalogMatrix() {
    const [data, setData] = useState<MatrixData | null>(null);
    const [loading, setLoading] = useState(true);
    const [saving, setSaving] = useState(false);
    const [msg, setMsg] = useState<string | null>(null);
    const [filterSector, setFilterSector] = useState<string>("");
    const [search, setSearch] = useState("");
    // diff: Map<"tenantId|pathId", desired bool>
    const [diff, setDiff] = useState<Record<string, boolean>>({});

    const load = useCallback(async () => {
        setLoading(true);
        try {
            const [pathList, tenantsRaw] = await Promise.all([
                apiFetch<{ items: CatalogPath[] }>("/api/superadmin/catalog/parcours?pageSize=200"),
                apiFetch<TenantRow[]>("/api/superadmin/tenants"),
            ]);
            const paths = pathList.items;
            const tenants = tenantsRaw.filter(t => t.id !== "00000000-0000-0000-0000-000000000000" || true);

            // Pour chaque tenant, appeler /tenants/{id}/catalog parallèlement
            const results = await Promise.all(tenants.map(t =>
                apiFetch<{ pathId: string; hasAccess: boolean }[]>(`/api/superadmin/tenants/${t.id}/catalog`)
                    .then(r => ({ tenantId: t.id, items: r }))
            ));
            const access: Record<string, Set<string>> = {};
            for (const r of results) {
                access[r.tenantId] = new Set(r.items.filter(i => i.hasAccess).map(i => i.pathId));
            }
            setData({ paths, tenants, access });
            setDiff({});
        } finally {
            setLoading(false);
        }
    }, []);

    useEffect(() => { load(); }, [load]);

    const isChecked = (tenantId: string, pathId: string): boolean => {
        const key = `${tenantId}|${pathId}`;
        if (key in diff) return diff[key];
        return data?.access[tenantId]?.has(pathId) ?? false;
    };

    const toggle = (tenantId: string, pathId: string) => {
        const key = `${tenantId}|${pathId}`;
        const original = data?.access[tenantId]?.has(pathId) ?? false;
        setDiff(prev => {
            const clone = { ...prev };
            const current = key in clone ? clone[key] : original;
            const next = !current;
            if (next === original) delete clone[key];
            else clone[key] = next;
            return clone;
        });
    };

    const filtered = useMemo(() => {
        if (!data) return { paths: [], tenants: [] };
        const paths = data.paths.filter(p => {
            if (filterSector && p.sector !== filterSector) return false;
            if (search && !p.title.toLowerCase().includes(search.toLowerCase())) return false;
            return true;
        });
        const tenants = data.tenants.filter(t => {
            if (search && !t.name.toLowerCase().includes(search.toLowerCase())) return false;
            return true;
        });
        // If search matches a tenant but no path (or vice versa), keep both unless both filters are orthogonal.
        // Simpler: if search matches any path OR tenant, include respective subset.
        return { paths, tenants };
    }, [data, filterSector, search]);

    const dirtyCount = Object.keys(diff).length;

    const save = async () => {
        if (!data || dirtyCount === 0) return;
        setSaving(true); setMsg(null);
        const grants: { tenantId: string; parcoursId: string }[] = [];
        const revokes: { tenantId: string; parcoursId: string }[] = [];
        for (const [key, desired] of Object.entries(diff)) {
            const [tenantId, pathId] = key.split("|");
            if (desired) grants.push({ tenantId, parcoursId: pathId });
            else revokes.push({ tenantId, parcoursId: pathId });
        }
        try {
            const res = await apiFetch<{ granted: number; revoked: number }>(
                "/api/superadmin/catalog/assignments/batch",
                { method: "POST", body: JSON.stringify({ grants, revokes }) }
            );
            setMsg(`✓ ${res.granted} accordés, ${res.revoked} révoqués`);
            await load();
        } catch (e) {
            setMsg("✗ Erreur : " + (e instanceof Error ? e.message : "inconnue"));
        } finally {
            setSaving(false);
        }
    };

    const bulkGrantColumn = (pathId: string) => {
        if (!data) return;
        const d = { ...diff };
        for (const t of data.tenants) {
            const orig = data.access[t.id]?.has(pathId) ?? false;
            if (!orig) d[`${t.id}|${pathId}`] = true;
        }
        setDiff(d);
    };
    const bulkRevokeColumn = (pathId: string) => {
        if (!data) return;
        const d = { ...diff };
        for (const t of data.tenants) {
            const orig = data.access[t.id]?.has(pathId) ?? false;
            if (orig) d[`${t.id}|${pathId}`] = false;
        }
        setDiff(d);
    };
    const bulkGrantRow = (tenantId: string, sector?: string) => {
        if (!data) return;
        const d = { ...diff };
        for (const p of data.paths) {
            if (sector && p.sector !== sector) continue;
            const orig = data.access[tenantId]?.has(p.id) ?? false;
            if (!orig) d[`${tenantId}|${p.id}`] = true;
        }
        setDiff(d);
    };
    const bulkRevokeRow = (tenantId: string) => {
        if (!data) return;
        const d = { ...diff };
        for (const p of data.paths) {
            const orig = data.access[tenantId]?.has(p.id) ?? false;
            if (orig) d[`${tenantId}|${p.id}`] = false;
        }
        setDiff(d);
    };

    const presetPackSante = () => {
        if (!data) return;
        const santeIds = data.paths.filter(p => p.sector === "sante").map(p => p.id);
        const d = { ...diff };
        for (const t of data.tenants) {
            for (const pid of santeIds) {
                const orig = data.access[t.id]?.has(pid) ?? false;
                if (!orig) d[`${t.id}|${pid}`] = true;
            }
        }
        setDiff(d);
    };
    const presetPackCyber = () => {
        if (!data) return;
        const cyberIds = data.paths.filter(p => p.sector === "cyber-general").map(p => p.id);
        const d = { ...diff };
        for (const t of data.tenants) {
            for (const pid of cyberIds) {
                const orig = data.access[t.id]?.has(pid) ?? false;
                if (!orig) d[`${t.id}|${pid}`] = true;
            }
        }
        setDiff(d);
    };
    const presetResetDemo = () => {
        if (!data) return;
        const demo = data.tenants.find(t => t.id === "00000000-0000-0000-0000-000000000000");
        if (!demo) return;
        const d = { ...diff };
        for (const p of data.paths) {
            const orig = data.access[demo.id]?.has(p.id) ?? false;
            if (!orig) d[`${demo.id}|${p.id}`] = true;
        }
        setDiff(d);
    };

    return (
        <div style={{ padding: "var(--page-x)", color: "#94A3B8" }}>
            <div style={{ marginBottom: 18 }}>
                <h2 style={{ color: "#f87171", fontSize: 22, margin: 0, marginBottom: 6, fontFamily: "'JetBrains Mono', monospace" }}>
                    ▸ Matrice d'attribution catalogue
                </h2>
                <div style={{ fontSize: 13, color: "#94A3B8" }}>
                    Tenants (lignes) × parcours (colonnes). Toggle par cellule, Enregistrer envoie le diff en une fois.
                </div>
            </div>

            {/* Presets */}
            <div style={{ display: "flex", gap: 8, marginBottom: 14, flexWrap: "wrap" }}>
                <button style={chip} onClick={presetPackSante}>[Pack Santé]</button>
                <button style={chip} onClick={presetPackCyber}>[Pack Cyber]</button>
                <button style={chip} onClick={presetResetDemo}>[Reset Demo]</button>
                <div style={{ flex: 1 }} />
                <input placeholder="Recherche tenant/parcours" value={search} onChange={e => setSearch(e.target.value)}
                    style={{ padding: "6px 12px", background: "#0d0000", color: "#94A3B8", border: "1px solid rgba(239,68,68,0.25)", borderRadius: 6, fontSize: 12 }} />
                <select value={filterSector} onChange={e => setFilterSector(e.target.value)}
                    style={{ padding: "6px 12px", background: "#0d0000", color: "#94A3B8", border: "1px solid rgba(239,68,68,0.25)", borderRadius: 6, fontSize: 12 }}>
                    <option value="">Tous secteurs</option>
                    <option value="sante">Santé</option>
                    <option value="cyber-general">Cyber</option>
                    <option value="comptabilite">Comptabilité</option>
                    <option value="finance">Finance</option>
                </select>
            </div>

            {/* Actions toolbar */}
            <div style={{ display: "flex", alignItems: "center", gap: 10, marginBottom: 14, flexWrap: "wrap" }}>
                {dirtyCount > 0 && (
                    <span style={{ fontSize: 12, color: "#F59E0B" }}>
                        {dirtyCount} modification{dirtyCount > 1 ? "s" : ""} en attente
                    </span>
                )}
                <div style={{ flex: 1 }} />
                {msg && <span style={{ fontSize: 12, color: msg.startsWith("✓") ? "#34d399" : "#F87171" }}>{msg}</span>}
                <button
                    disabled={dirtyCount === 0 || saving}
                    onClick={save}
                    style={{ ...btnPrimary, opacity: dirtyCount > 0 ? 1 : 0.4, cursor: dirtyCount > 0 ? "pointer" : "not-allowed" }}
                >{saving ? "Enregistrement..." : `Enregistrer (${dirtyCount})`}</button>
            </div>

            {loading || !data ? (
                <div style={{ ...card, textAlign: "center", padding: 40 }}>Chargement...</div>
            ) : (
                <div style={{ ...card, overflowX: "auto" }}>
                    <table style={{ borderCollapse: "collapse", fontSize: 12, minWidth: "100%" }}>
                        <thead>
                            <tr>
                                <th style={{
                                    position: "sticky", left: 0, background: "#0d0000",
                                    padding: "10px 14px", textAlign: "left", borderBottom: "1px solid rgba(239,68,68,0.15)",
                                    color: "#94A3B8", textTransform: "uppercase", fontSize: 10, letterSpacing: "0.08em",
                                    minWidth: 180
                                }}>Tenant</th>
                                {filtered.paths.map(p => (
                                    <th key={p.id} title={p.title}
                                        onClick={() => { if (confirm(`Accorder "${p.title}" à TOUS les tenants ?`)) bulkGrantColumn(p.id); }}
                                        onContextMenu={e => { e.preventDefault(); if (confirm(`Révoquer "${p.title}" de TOUS les tenants ?`)) bulkRevokeColumn(p.id); }}
                                        style={{
                                            padding: "10px 8px", borderBottom: "1px solid rgba(239,68,68,0.15)",
                                            color: "#f87171", fontSize: 10, writingMode: "vertical-rl",
                                            transform: "rotate(180deg)", whiteSpace: "nowrap",
                                            height: 160, cursor: "pointer",
                                            fontFamily: "'JetBrains Mono', monospace"
                                        }}>
                                        {p.title.length > 30 ? p.title.slice(0, 30) + "..." : p.title}
                                    </th>
                                ))}
                            </tr>
                        </thead>
                        <tbody>
                            {filtered.tenants.map(t => (
                                <tr key={t.id} style={{ borderBottom: "1px solid rgba(239,68,68,0.05)" }}>
                                    <td style={{
                                        position: "sticky", left: 0, background: "#0d0000",
                                        padding: "8px 14px", color: "#94A3B8", fontSize: 12,
                                        display: "flex", alignItems: "center", gap: 6
                                    }}>
                                        <span>{t.name}</span>
                                        <button title="Accorder tous" onClick={() => bulkGrantRow(t.id)}
                                            style={{ marginLeft: "auto", ...btnGhost, padding: "2px 6px", fontSize: 10 }}>+all</button>
                                        <button title="Révoquer tous" onClick={() => bulkRevokeRow(t.id)}
                                            style={{ ...btnGhost, padding: "2px 6px", fontSize: 10 }}>−all</button>
                                    </td>
                                    {filtered.paths.map(p => {
                                        const key = `${t.id}|${p.id}`;
                                        const checked = isChecked(t.id, p.id);
                                        const changed = key in diff;
                                        return (
                                            <td key={p.id} style={{
                                                padding: "6px 8px", textAlign: "center",
                                                background: changed ? "rgba(245,158,11,0.08)" : "transparent",
                                                borderBottom: "1px solid rgba(239,68,68,0.05)"
                                            }}>
                                                <input
                                                    type="checkbox" checked={checked}
                                                    onChange={() => toggle(t.id, p.id)}
                                                    style={{
                                                        width: 16, height: 16, cursor: "pointer",
                                                        accentColor: "#10b981"
                                                    }}
                                                />
                                            </td>
                                        );
                                    })}
                                </tr>
                            ))}
                        </tbody>
                    </table>
                    <div style={{ fontSize: 10, color: "#94A3B8", marginTop: 12 }}>
                        Astuces : clic sur en-tête parcours = accorder à tous. Clic droit = révoquer à tous. +all / −all par tenant.
                    </div>
                </div>
            )}
        </div>
    );
}
