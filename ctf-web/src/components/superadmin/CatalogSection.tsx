"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { apiFetch } from "@/lib/api";

type Tab = "contenu" | "acces" | "stats";

type CatalogPathListItem = {
    id: string;
    title: string;
    description: string | null;
    level: string | null;
    sector: string | null;
    status: string;
    estimatedMinutes: number | null;
    tags: string | null;
    challengesCount: number;
    tenantsWithAccessCount: number;
    createdAt: string;
    publishedAt: string | null;
};

type PagedResult<T> = { items: T[]; page: number; pageSize: number; total: number };

type CatalogChallenge = {
    id: string;
    title: string;
    type: string;
    contentType: string | null;
    category: string | null;
    difficulty: number | null;
    points: number;
    sortOrder: number;
};

type CatalogModule = {
    id: string;
    title: string;
    sortOrder: number;
    challenges: CatalogChallenge[];
};

type CatalogPathDetail = {
    id: string;
    title: string;
    description: string | null;
    level: string | null;
    sector: string | null;
    status: string;
    estimatedMinutes: number | null;
    tags: string | null;
    isCatalog: boolean;
    createdAt: string;
    publishedAt: string | null;
    modules: CatalogModule[];
};

type TenantAccess = {
    tenantId: string;
    tenantName: string;
    hasAccess: boolean;
    grantedAt: string | null;
    grantedByEmail: string | null;
    revokedAt: string | null;
    revokedByEmail: string | null;
};

type PathStats = {
    pathId: string;
    tenantsWithAccess: number;
    totalUsersWithAccess: number;
    usersStarted: number;
    usersCompleted: number;
    averageCompletionPercent: number;
};

const card: React.CSSProperties = {
    background: "var(--surface)",
    border: "1px solid rgba(239,68,68,0.15)",
    borderRadius: 8,
    padding: 20,
};

const labelCss: React.CSSProperties = {
    fontSize: 11,
    color: "var(--text-3)",
    fontFamily: "'JetBrains Mono', monospace",
    letterSpacing: "0.08em",
    textTransform: "uppercase",
};

const btnPrimary: React.CSSProperties = {
    padding: "8px 14px",
    background: "var(--danger)",
    color: "rgba(255,255,255,0.95)",
    border: "none",
    borderRadius: 6,
    fontSize: 13,
    fontWeight: 500,
    cursor: "pointer",
    transition: "background 200ms",
};
const btnGhost: React.CSSProperties = {
    padding: "6px 12px",
    background: "transparent",
    color: "var(--text-3)",
    border: "1px solid rgba(239,68,68,0.25)",
    borderRadius: 6,
    fontSize: 12,
    cursor: "pointer",
    transition: "all 200ms",
};

const sectorLabel = (s: string | null): string => {
    if (!s) return "—";
    const map: Record<string, string> = {
        "sante": "Santé",
        "cyber-general": "Cyber",
        "comptabilite": "Comptabilité",
        "finance": "Finance"
    };
    return map[s] ?? s;
};

const levelLabel = (l: string | null): string => {
    if (!l) return "—";
    const map: Record<string, string> = {
        "beginner": "Débutant",
        "intermediate": "Intermédiaire",
        "advanced": "Avancé"
    };
    return map[l] ?? l;
};

export default function CatalogSection() {
    const [paths, setPaths] = useState<CatalogPathListItem[]>([]);
    const [loading, setLoading] = useState(true);
    const [selectedId, setSelectedId] = useState<string | null>(null);
    const [filterSector, setFilterSector] = useState<string>("");
    const [filterLevel, setFilterLevel] = useState<string>("");

    const loadList = useCallback(async () => {
        setLoading(true);
        try {
            const qs = new URLSearchParams();
            if (filterSector) qs.set("sector", filterSector);
            if (filterLevel) qs.set("level", filterLevel);
            qs.set("pageSize", "100");
            const data = await apiFetch<PagedResult<CatalogPathListItem>>(
                `/api/superadmin/catalog/parcours?${qs.toString()}`
            );
            setPaths(data.items);
        } finally {
            setLoading(false);
        }
    }, [filterSector, filterLevel]);

    useEffect(() => { loadList(); }, [loadList]);

    if (selectedId) {
        return (
            <CatalogDetail
                pathId={selectedId}
                onBack={() => { setSelectedId(null); loadList(); }}
            />
        );
    }

    return (
        <div style={{ padding: "var(--page-x)", color: "var(--text-3)" }}>
            <div style={{ display: "flex", alignItems: "flex-end", justifyContent: "space-between", marginBottom: 20 }}>
                <div>
                    <h2 style={{ color: "var(--danger)", fontSize: 22, margin: 0, marginBottom: 6, fontFamily: "'JetBrains Mono', monospace" }}>
                        ▸ Gestion des parcours catalogue
                    </h2>
                    <div style={{ fontSize: 13, color: "var(--text-3)" }}>
                        Contrôle quels tenants ont accès à chaque parcours de catalogue (modèle à la carte).
                    </div>
                </div>
            </div>

            <div style={{ display: "flex", gap: 10, marginBottom: 16, alignItems: "center", flexWrap: "wrap" }}>
                <label style={labelCss}>Secteur</label>
                <select value={filterSector} onChange={e => setFilterSector(e.target.value)}
                    style={{ padding: "6px 10px", background: "var(--surface)", color: "var(--text-3)", border: "1px solid rgba(239,68,68,0.25)", borderRadius: 6 }}
                >
                    <option value="">Tous</option>
                    <option value="sante">Santé</option>
                    <option value="cyber-general">Cyber général</option>
                    <option value="comptabilite">Comptabilité</option>
                    <option value="finance">Finance</option>
                </select>
                <label style={labelCss}>Niveau</label>
                <select value={filterLevel} onChange={e => setFilterLevel(e.target.value)}
                    style={{ padding: "6px 10px", background: "var(--surface)", color: "var(--text-3)", border: "1px solid rgba(239,68,68,0.25)", borderRadius: 6 }}
                >
                    <option value="">Tous</option>
                    <option value="beginner">Débutant</option>
                    <option value="intermediate">Intermédiaire</option>
                    <option value="advanced">Avancé</option>
                </select>
                <div style={{ flex: 1 }} />
                <span style={{ fontSize: 12, color: "var(--text-3)" }}>{paths.length} parcours</span>
            </div>

            {loading ? (
                <div style={{ ...card, textAlign: "center", padding: 40 }}>Chargement...</div>
            ) : paths.length === 0 ? (
                <div style={{ ...card, textAlign: "center", padding: 40, color: "var(--text-3)" }}>
                    Aucun parcours catalogue. Les seeds doivent être initialisés au démarrage du backend en mode dev.
                </div>
            ) : (
                <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fill, minmax(min(300px, 100%), 1fr))", gap: 16 }}>
                    {paths.map(p => (
                        <div key={p.id} style={{ ...card, cursor: "pointer", transition: "all 200ms" }}
                            onClick={() => setSelectedId(p.id)}
                            onMouseEnter={e => { (e.currentTarget as HTMLDivElement).style.borderColor = "rgba(239,68,68,0.45)"; }}
                            onMouseLeave={e => { (e.currentTarget as HTMLDivElement).style.borderColor = "rgba(239,68,68,0.15)"; }}
                        >
                            <div style={{ display: "flex", alignItems: "flex-start", justifyContent: "space-between", marginBottom: 10 }}>
                                <div style={{ fontSize: 15, fontWeight: 600, color: "var(--danger)", lineHeight: 1.3 }}>{p.title}</div>
                                <span style={{
                                    padding: "2px 8px", borderRadius: 999, fontSize: 10, fontWeight: 600,
                                    background: p.status === "published" ? "var(--success-subtle)" : "var(--surface-2)",
                                    color: p.status === "published" ? "var(--success)" : "var(--text-3)",
                                    whiteSpace: "nowrap"
                                }}>{p.status}</span>
                            </div>
                            {p.description && (
                                <div style={{ fontSize: 12, color: "var(--text-3)", marginBottom: 12, lineHeight: 1.45 }}>
                                    {p.description.length > 110 ? p.description.slice(0, 110) + "..." : p.description}
                                </div>
                            )}
                            <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 8, fontSize: 11, color: "var(--text-3)" }}>
                                <div><span style={labelCss}>Secteur</span><br />{sectorLabel(p.sector)}</div>
                                <div><span style={labelCss}>Niveau</span><br />{levelLabel(p.level)}</div>
                                <div><span style={labelCss}>Challenges</span><br />{p.challengesCount}</div>
                                <div><span style={labelCss}>Tenants actifs</span><br />{p.tenantsWithAccessCount}</div>
                            </div>
                            {p.tags && (
                                <div style={{ marginTop: 12, display: "flex", gap: 6, flexWrap: "wrap" }}>
                                    {p.tags.split(",").map(t => t.trim()).filter(Boolean).slice(0, 4).map(t => (
                                        <span key={t} style={{
                                            fontSize: 10, padding: "2px 8px", borderRadius: 999,
                                            background: "rgba(239,68,68,0.08)", color: "var(--danger)",
                                            border: "1px solid rgba(239,68,68,0.2)"
                                        }}>{t}</span>
                                    ))}
                                </div>
                            )}
                        </div>
                    ))}
                </div>
            )}
        </div>
    );
}

function CatalogDetail({ pathId, onBack }: { pathId: string; onBack: () => void }) {
    const [tab, setTab] = useState<Tab>("contenu");
    const [detail, setDetail] = useState<CatalogPathDetail | null>(null);

    useEffect(() => {
        apiFetch<CatalogPathDetail>(`/api/superadmin/catalog/parcours/${pathId}`)
            .then(setDetail)
            .catch(() => setDetail(null));
    }, [pathId]);

    return (
        <div style={{ padding: "var(--page-x)", color: "var(--text-3)" }}>
            <button onClick={onBack} style={{ ...btnGhost, marginBottom: 16 }}>← Retour</button>

            {!detail ? (
                <div style={{ ...card, textAlign: "center", padding: 40 }}>Chargement...</div>
            ) : (
                <>
                    <div style={{ ...card, marginBottom: 16 }}>
                        <h2 style={{ color: "var(--danger)", fontSize: 20, margin: 0, marginBottom: 4 }}>{detail.title}</h2>
                        {detail.description && <div style={{ color: "var(--text-3)", fontSize: 13, marginBottom: 10 }}>{detail.description}</div>}
                        <div style={{ display: "flex", gap: 14, fontSize: 12, color: "var(--text-3)", flexWrap: "wrap" }}>
                            <span><strong style={{ color: "var(--text-3)" }}>Secteur:</strong> {sectorLabel(detail.sector)}</span>
                            <span><strong style={{ color: "var(--text-3)" }}>Niveau:</strong> {levelLabel(detail.level)}</span>
                            <span><strong style={{ color: "var(--text-3)" }}>Durée:</strong> {detail.estimatedMinutes ?? "—"} min</span>
                            <span><strong style={{ color: "var(--text-3)" }}>Statut:</strong> {detail.status}</span>
                        </div>
                    </div>

                    <div style={{ display: "flex", gap: 4, marginBottom: 12 }}>
                        {(["contenu", "acces", "stats"] as Tab[]).map(t => (
                            <button key={t}
                                onClick={() => setTab(t)}
                                style={{
                                    padding: "8px 16px",
                                    background: tab === t ? "rgba(239,68,68,0.1)" : "transparent",
                                    color: tab === t ? "var(--danger)" : "var(--text-3)",
                                    border: "1px solid " + (tab === t ? "rgba(239,68,68,0.3)" : "rgba(239,68,68,0.1)"),
                                    borderRadius: 6,
                                    fontSize: 13,
                                    cursor: "pointer",
                                    textTransform: "capitalize",
                                    fontFamily: "'JetBrains Mono', monospace",
                                }}
                            >
                                {t === "contenu" ? "Contenu" : t === "acces" ? "Accès tenants" : "Statistiques"}
                            </button>
                        ))}
                    </div>

                    {tab === "contenu" && <ContentTab detail={detail} />}
                    {tab === "acces" && <AccessTab pathId={pathId} />}
                    {tab === "stats" && <StatsTab pathId={pathId} />}
                </>
            )}
        </div>
    );
}

function ContentTab({ detail }: { detail: CatalogPathDetail }) {
    return (
        <div style={card}>
            {detail.modules.length === 0 ? (
                <div style={{ color: "var(--text-3)", textAlign: "center", padding: 20 }}>Aucun module.</div>
            ) : (
                detail.modules.map(m => (
                    <div key={m.id} style={{ marginBottom: 18 }}>
                        <div style={{ fontSize: 14, fontWeight: 600, color: "var(--danger)", marginBottom: 8 }}>
                            Module {m.sortOrder}. {m.title}
                        </div>
                        <div className="resp-scroll-x">
                        <table style={{ width: "100%", borderCollapse: "collapse", fontSize: 12, minWidth: 560 }}>
                            <thead>
                                <tr style={{ background: "rgba(239,68,68,0.05)" }}>
                                    <th style={{ ...labelCss, padding: "8px 10px", textAlign: "left" }}>#</th>
                                    <th style={{ ...labelCss, padding: "8px 10px", textAlign: "left" }}>Titre</th>
                                    <th style={{ ...labelCss, padding: "8px 10px", textAlign: "left" }}>Type</th>
                                    <th style={{ ...labelCss, padding: "8px 10px", textAlign: "left" }}>Catégorie</th>
                                    <th style={{ ...labelCss, padding: "8px 10px", textAlign: "right" }}>Points</th>
                                </tr>
                            </thead>
                            <tbody>
                                {m.challenges.map(c => (
                                    <tr key={c.id} style={{ borderBottom: "1px solid rgba(239,68,68,0.05)" }}>
                                        <td style={{ padding: "8px 10px", color: "var(--text-3)" }}>{c.sortOrder}</td>
                                        <td style={{ padding: "8px 10px" }}>{c.title}</td>
                                        <td style={{ padding: "8px 10px", color: "var(--text-3)" }}>{c.contentType ?? c.type}</td>
                                        <td style={{ padding: "8px 10px", color: "var(--text-3)" }}>{c.category ?? "—"}</td>
                                        <td style={{ padding: "8px 10px", textAlign: "right" }}>{c.points}</td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                        </div>
                    </div>
                ))
            )}
        </div>
    );
}

function AccessTab({ pathId }: { pathId: string }) {
    const [tenants, setTenants] = useState<TenantAccess[]>([]);
    const [loading, setLoading] = useState(true);
    const [saving, setSaving] = useState(false);
    const [msg, setMsg] = useState<string | null>(null);
    const [dirty, setDirty] = useState<Record<string, boolean>>({});

    const load = useCallback(async () => {
        setLoading(true);
        try {
            const data = await apiFetch<TenantAccess[]>(`/api/superadmin/catalog/parcours/${pathId}/access`);
            setTenants(data);
            setDirty({});
        } finally {
            setLoading(false);
        }
    }, [pathId]);

    useEffect(() => { load(); }, [load]);

    const toggle = (tenantId: string) => {
        setDirty(prev => {
            const curVal = prev[tenantId];
            const t = tenants.find(x => x.tenantId === tenantId);
            const original = t?.hasAccess ?? false;
            // If not dirty yet: start tracking with opposite of original
            if (curVal === undefined) return { ...prev, [tenantId]: !original };
            // If reverting back to original, remove from dirty
            if (curVal === original) {
                const clone = { ...prev };
                delete clone[tenantId];
                return clone;
            }
            return { ...prev, [tenantId]: !curVal };
        });
    };

    const save = async () => {
        setSaving(true);
        setMsg(null);
        try {
            const toGrant: string[] = [];
            const toRevoke: string[] = [];
            for (const [tid, desired] of Object.entries(dirty)) {
                const orig = tenants.find(x => x.tenantId === tid)?.hasAccess ?? false;
                if (desired === orig) continue;
                if (desired) toGrant.push(tid);
                else toRevoke.push(tid);
            }
            if (toGrant.length > 0) {
                await apiFetch(`/api/superadmin/catalog/parcours/${pathId}/access`, {
                    method: "POST",
                    body: JSON.stringify({ tenantIds: toGrant }),
                });
            }
            for (const tid of toRevoke) {
                await apiFetch(`/api/superadmin/catalog/parcours/${pathId}/access/${tid}`, { method: "DELETE" });
            }
            setMsg(`✓ ${toGrant.length} accordé(s), ${toRevoke.length} révoqué(s)`);
            await load();
        } catch (e) {
            setMsg("✗ Erreur : " + (e instanceof Error ? e.message : "inconnue"));
        } finally {
            setSaving(false);
        }
    };

    const bulkGrantAll = () => setDirty(Object.fromEntries(tenants.filter(t => !t.hasAccess).map(t => [t.tenantId, true])));
    const bulkRevokeAll = () => setDirty(Object.fromEntries(tenants.filter(t => t.hasAccess).map(t => [t.tenantId, false])));

    const dirtyCount = Object.keys(dirty).length;

    if (loading) return <div style={{ ...card, textAlign: "center", padding: 40 }}>Chargement...</div>;

    return (
        <div style={card}>
            <div style={{ display: "flex", alignItems: "center", gap: 10, marginBottom: 14, flexWrap: "wrap" }}>
                <button style={btnGhost} onClick={bulkGrantAll}>Accorder à tous</button>
                <button style={btnGhost} onClick={bulkRevokeAll}>Révoquer de tous</button>
                <div style={{ flex: 1 }} />
                {msg && <span style={{ fontSize: 12, color: "var(--success)" }}>{msg}</span>}
                <button
                    style={{ ...btnPrimary, opacity: dirtyCount > 0 ? 1 : 0.4, cursor: dirtyCount > 0 ? "pointer" : "not-allowed" }}
                    disabled={dirtyCount === 0 || saving}
                    onClick={save}
                >{saving ? "Enregistrement..." : `Enregistrer (${dirtyCount})`}</button>
            </div>

            <div className="resp-scroll-x">
            <table style={{ width: "100%", borderCollapse: "collapse", fontSize: 13, minWidth: 520 }}>
                <thead>
                    <tr style={{ background: "rgba(239,68,68,0.05)" }}>
                        <th style={{ ...labelCss, padding: "10px", textAlign: "left" }}>Tenant</th>
                        <th style={{ ...labelCss, padding: "10px", textAlign: "center" }}>Accès</th>
                        <th style={{ ...labelCss, padding: "10px", textAlign: "left" }}>Accordé le</th>
                        <th style={{ ...labelCss, padding: "10px", textAlign: "left" }}>Par</th>
                    </tr>
                </thead>
                <tbody>
                    {tenants.map(t => {
                        const desired = dirty[t.tenantId] !== undefined ? dirty[t.tenantId] : t.hasAccess;
                        const changed = dirty[t.tenantId] !== undefined && dirty[t.tenantId] !== t.hasAccess;
                        return (
                            <tr key={t.tenantId} style={{ borderBottom: "1px solid rgba(239,68,68,0.05)", background: changed ? "rgba(239,68,68,0.04)" : "transparent" }}>
                                <td style={{ padding: "10px", fontWeight: changed ? 600 : 400 }}>{t.tenantName}</td>
                                <td style={{ padding: "10px", textAlign: "center" }}>
                                    <button
                                        onClick={() => toggle(t.tenantId)}
                                        style={{
                                            width: 42, height: 22, borderRadius: 11,
                                            background: desired ? "var(--success)" : "var(--surface-2)",
                                            border: "none", cursor: "pointer",
                                            position: "relative", transition: "background 200ms",
                                        }}
                                    >
                                        <span style={{
                                            position: "absolute", top: 2,
                                            left: desired ? 22 : 2,
                                            width: 18, height: 18, borderRadius: "50%",
                                            background: "rgba(255,255,255,0.95)",
                                            transition: "left 200ms",
                                        }} />
                                    </button>
                                </td>
                                <td style={{ padding: "10px", color: "var(--text-3)", fontSize: 11 }}>
                                    {t.grantedAt ? new Date(t.grantedAt).toLocaleDateString("fr-FR") : "—"}
                                </td>
                                <td style={{ padding: "10px", color: "var(--text-3)", fontSize: 11 }}>
                                    {t.grantedByEmail ?? "—"}
                                </td>
                            </tr>
                        );
                    })}
                </tbody>
            </table>
            </div>
        </div>
    );
}

function StatsTab({ pathId }: { pathId: string }) {
    const [stats, setStats] = useState<PathStats | null>(null);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        apiFetch<PathStats>(`/api/superadmin/catalog/parcours/${pathId}/stats`)
            .then(setStats).catch(() => setStats(null)).finally(() => setLoading(false));
    }, [pathId]);

    if (loading) return <div style={{ ...card, textAlign: "center", padding: 40 }}>Chargement...</div>;
    if (!stats) return <div style={{ ...card, textAlign: "center", padding: 40, color: "var(--text-3)" }}>Pas de statistiques disponibles.</div>;

    const kpis = [
        ["Tenants avec accès", stats.tenantsWithAccess],
        ["Utilisateurs concernés", stats.totalUsersWithAccess],
        ["Ont commencé", stats.usersStarted],
        ["Ont terminé", stats.usersCompleted],
        ["Complétion moyenne", `${stats.averageCompletionPercent}%`],
    ] as const;

    return (
        <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fit, minmax(180px, 1fr))", gap: 12 }}>
            {kpis.map(([label, val]) => (
                <div key={label} style={card}>
                    <div style={labelCss}>{label}</div>
                    <div style={{ fontSize: 28, fontWeight: 700, color: "var(--danger)", marginTop: 8 }}>{val}</div>
                </div>
            ))}
        </div>
    );
}
