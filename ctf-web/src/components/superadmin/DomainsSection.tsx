"use client";

import { useCallback, useEffect, useState } from "react";
import { apiFetch } from "@/lib/api";

type DomainRow = {
    id: string;
    domain: string;
    tenantId: string;
    tenantName: string;
    isAutoProvisioningEnabled: boolean;
    createdAt: string;
    createdBy: string | null;
};

type TenantLite = { id: string; name: string; isActive: boolean };

const card: React.CSSProperties = {
    background: "var(--surface)",
    border: "1px solid rgba(239,68,68,0.15)",
    borderRadius: 8,
    padding: 20,
};

const btnPrimary: React.CSSProperties = {
    padding: "8px 14px", background: "var(--danger)", color: "rgba(255,255,255,0.95)", border: "none",
    borderRadius: 6, fontSize: 13, fontWeight: 500, cursor: "pointer"
};

const btnGhost: React.CSSProperties = {
    padding: "6px 12px", background: "transparent", color: "var(--text-2)",
    border: "1px solid rgba(239,68,68,0.25)", borderRadius: 6, fontSize: 12, cursor: "pointer"
};

const labelCss: React.CSSProperties = {
    fontSize: 11, color: "var(--text-3)", fontFamily: "'JetBrains Mono', monospace",
    letterSpacing: "0.08em", textTransform: "uppercase"
};

export default function DomainsSection() {
    const [rows, setRows] = useState<DomainRow[]>([]);
    const [tenants, setTenants] = useState<TenantLite[]>([]);
    const [loading, setLoading] = useState(true);
    const [showCreate, setShowCreate] = useState(false);
    const [msg, setMsg] = useState<string | null>(null);

    const load = useCallback(async () => {
        setLoading(true);
        try {
            const [list, tn] = await Promise.all([
                apiFetch<DomainRow[]>("/api/superadmin/domains"),
                apiFetch<TenantLite[]>("/api/superadmin/tenants"),
            ]);
            setRows(list);
            setTenants(tn.filter(t => t.isActive && t.id !== "00000000-0000-0000-0000-000000000000"));
        } finally { setLoading(false); }
    }, []);

    useEffect(() => { load(); }, [load]);

    const toggle = async (row: DomainRow) => {
        await apiFetch(`/api/superadmin/domains/${row.id}`, {
            method: "PATCH",
            body: JSON.stringify({ isAutoProvisioningEnabled: !row.isAutoProvisioningEnabled }),
        });
        setMsg(`✓ ${row.domain} : auto-provisioning ${!row.isAutoProvisioningEnabled ? "activé" : "désactivé"}`);
        await load();
    };

    const del = async (row: DomainRow) => {
        if (!confirm(`Supprimer le mapping '${row.domain}' → ${row.tenantName} ?\n\nLes users existants ne sont pas affectés. Les futurs SSO tomberont sur Demo.`)) return;
        await apiFetch(`/api/superadmin/domains/${row.id}`, { method: "DELETE" });
        setMsg(`✓ ${row.domain} supprimé`);
        await load();
    };

    return (
        <div style={{ padding: "var(--page-x)", color: "var(--text-2)" }}>
            <div style={{ display: "flex", justifyContent: "space-between", alignItems: "flex-end", marginBottom: 20, gap: 12, flexWrap: "wrap" }}>
                <div>
                    <h2 style={{ color: "var(--danger)", fontSize: 22, margin: 0, marginBottom: 6, fontFamily: "'JetBrains Mono', monospace" }}>
                        ▸ Domaines SSO
                    </h2>
                    <div style={{ fontSize: 13, color: "var(--text-3)" }}>
                        Whitelist des domaines email → tenant. Un SSO reconnu sur un domaine whitelisté crée/login le user sur le tenant correspondant. Sinon fallback sur Demo.
                    </div>
                </div>
                <button style={btnPrimary} onClick={() => setShowCreate(true)}>+ Ajouter un domaine</button>
            </div>

            {msg && <div style={{ ...card, marginBottom: 12, background: "var(--success-subtle)", borderColor: "rgba(16,185,129,0.25)", fontSize: 12, color: "var(--success)" }}>{msg}</div>}

            {loading ? (
                <div style={{ ...card, textAlign: "center", padding: 40 }}>Chargement…</div>
            ) : rows.length === 0 ? (
                <div style={{ ...card, textAlign: "center", padding: 40, color: "var(--text-3)" }}>
                    Aucun domaine whitelisté. Cliquez sur « + Ajouter un domaine » pour commencer.
                </div>
            ) : (
                <div style={{ ...card, overflowX: "auto", padding: 0 }}>
                    <table style={{ width: "100%", borderCollapse: "collapse", fontSize: 13, minWidth: 620 }}>
                        <thead>
                            <tr style={{ background: "rgba(239,68,68,0.05)" }}>
                                <th style={{ ...labelCss, padding: 10, textAlign: "left" }}>Domaine</th>
                                <th style={{ ...labelCss, padding: 10, textAlign: "left" }}>Tenant</th>
                                <th style={{ ...labelCss, padding: 10, textAlign: "center" }}>Auto-provisioning</th>
                                <th style={{ ...labelCss, padding: 10, textAlign: "left" }}>Ajouté le</th>
                                <th style={{ ...labelCss, padding: 10, textAlign: "right" }}>Actions</th>
                            </tr>
                        </thead>
                        <tbody>
                            {rows.map(r => (
                                <tr key={r.id} style={{ borderBottom: "1px solid rgba(239,68,68,0.05)" }}>
                                    <td style={{ padding: 10, fontFamily: "'JetBrains Mono', monospace", color: "var(--danger)" }}>@{r.domain}</td>
                                    <td style={{ padding: 10 }}>{r.tenantName}</td>
                                    <td style={{ padding: 10, textAlign: "center" }}>
                                        <button onClick={() => toggle(r)}
                                            style={{
                                                width: 42, height: 22, borderRadius: 11,
                                                background: r.isAutoProvisioningEnabled ? "var(--success)" : "var(--surface-2)",
                                                border: "none", cursor: "pointer", position: "relative", transition: "background 200ms",
                                            }}>
                                            <span style={{
                                                position: "absolute", top: 2, left: r.isAutoProvisioningEnabled ? 22 : 2,
                                                width: 18, height: 18, borderRadius: "50%", background: "rgba(255,255,255,0.95)", transition: "left 200ms",
                                            }} />
                                        </button>
                                    </td>
                                    <td style={{ padding: 10, color: "var(--text-3)", fontSize: 11 }}>
                                        {new Date(r.createdAt).toLocaleDateString("fr-FR")}
                                    </td>
                                    <td style={{ padding: 10, textAlign: "right" }}>
                                        <button onClick={() => del(r)} style={{
                                            padding: "4px 10px", background: "transparent", color: "var(--danger)",
                                            border: "1px solid rgba(239,68,68,0.25)", borderRadius: 6, fontSize: 11, cursor: "pointer"
                                        }}>Supprimer</button>
                                    </td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                </div>
            )}

            {showCreate && (
                <CreateDomainModal tenants={tenants} onClose={() => setShowCreate(false)} onSuccess={(m) => { setMsg(m); setShowCreate(false); load(); }} />
            )}
        </div>
    );
}

function CreateDomainModal({ tenants, onClose, onSuccess }: {
    tenants: TenantLite[]; onClose: () => void; onSuccess: (msg: string) => void;
}) {
    const [tenantId, setTenantId] = useState("");
    const [domain, setDomain] = useState("");
    const [autoProv, setAutoProv] = useState(true);
    const [submitting, setSubmitting] = useState(false);
    const [err, setErr] = useState<string | null>(null);

    const submit = async (e: React.FormEvent) => {
        e.preventDefault();
        setErr(null); setSubmitting(true);
        try {
            await apiFetch("/api/superadmin/domains", {
                method: "POST",
                body: JSON.stringify({ tenantId, domain: domain.trim().toLowerCase(), isAutoProvisioningEnabled: autoProv }),
            });
            onSuccess(`✓ Domaine ${domain} whitelisté`);
        } catch (e) {
            setErr(e instanceof Error ? e.message : "Erreur");
        } finally { setSubmitting(false); }
    };

    return (
        <div onClick={onClose} style={{
            position: "fixed", inset: 0, background: "rgba(0,0,0,0.7)", zIndex: 100,
            display: "flex", alignItems: "center", justifyContent: "center", padding: 16
        }}>
            <form onClick={e => e.stopPropagation()} onSubmit={submit} style={{
                background: "var(--surface)", border: "1px solid rgba(239,68,68,0.25)", borderRadius: 10,
                padding: 24, width: 480, maxWidth: "100%"
            }}>
                <h2 style={{ color: "var(--danger)", fontSize: 18, margin: 0, marginBottom: 16, fontFamily: "'JetBrains Mono', monospace" }}>
                    ▸ Nouveau domaine SSO
                </h2>

                <div style={{ marginBottom: 14 }}>
                    <label style={{ ...labelCss, display: "block", marginBottom: 6 }}>Tenant</label>
                    <select required value={tenantId} onChange={e => setTenantId(e.target.value)}
                        style={{ width: "100%", padding: "8px 12px", background: "var(--surface)", color: "#fff", border: "1px solid rgba(239,68,68,0.25)", borderRadius: 6, fontSize: 13 }}>
                        <option value="">-- sélectionner --</option>
                        {tenants.map(t => <option key={t.id} value={t.id}>{t.name}</option>)}
                    </select>
                </div>

                <div style={{ marginBottom: 14 }}>
                    <label style={{ ...labelCss, display: "block", marginBottom: 6 }}>Domaine email</label>
                    <input required type="text" value={domain} onChange={e => setDomain(e.target.value)}
                        placeholder="cybermed.fr"
                        style={{ width: "100%", padding: "8px 12px", background: "var(--surface)", color: "#fff", border: "1px solid rgba(239,68,68,0.25)", borderRadius: 6, fontSize: 13, fontFamily: "'JetBrains Mono', monospace" }} />
                    <div style={{ fontSize: 11, color: "var(--text-3)", marginTop: 4 }}>
                        Sans le `@`. Un domaine = un seul tenant (unicité globale).
                    </div>
                </div>

                <div style={{ marginBottom: 16, display: "flex", alignItems: "center", gap: 8 }}>
                    <input id="autoProv" type="checkbox" checked={autoProv} onChange={e => setAutoProv(e.target.checked)} />
                    <label htmlFor="autoProv" style={{ fontSize: 13, color: "var(--text-2)" }}>
                        Auto-provisioning activé (les nouveaux SSO créent les comptes)
                    </label>
                </div>

                {err && <div style={{ fontSize: 12, color: "var(--danger)", marginBottom: 12 }}>✗ {err}</div>}

                <div style={{ display: "flex", gap: 8, justifyContent: "flex-end" }}>
                    <button type="button" onClick={onClose} style={btnGhost}>Annuler</button>
                    <button type="submit" disabled={submitting} style={btnPrimary}>
                        {submitting ? "..." : "Créer"}
                    </button>
                </div>
            </form>
        </div>
    );
}
