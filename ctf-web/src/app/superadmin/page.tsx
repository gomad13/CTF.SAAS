"use client";

import { useEffect, useRef, useState, useCallback, createContext, useContext } from "react";
import { useRouter } from "next/navigation";
import { apiFetch } from "@/lib/api";
import { useIsMobile } from "@/hooks/useMediaQuery";
import type { Me } from "@/lib/types";
import { useSaDialog } from "@/components/superadmin/SaDialog";
import CatalogSection from "@/components/superadmin/CatalogSection";
import CatalogMatrix from "@/components/superadmin/CatalogMatrix";
import DomainsSection from "@/components/superadmin/DomainsSection";

// Contexte pour partager les modals/toasts entre toutes les sections SuperAdmin.
type DialogApi = ReturnType<typeof useSaDialog>;
const SaDialogContext = createContext<DialogApi | null>(null);
function useDlg(): DialogApi {
    const v = useContext(SaDialogContext);
    if (!v) throw new Error("useDlg must be used within SuperAdminPage");
    return v;
}

type Section = "overview" | "tenants" | "users" | "memberships" | "content" | "catalog" | "matrix" | "domains" | "licenses" | "health" | "audit" | "announcements" | "exports" | "activity" | "statsglobal";

// eslint-disable-next-line @typescript-eslint/no-explicit-any
type Any = any;

declare global {
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    interface Window { Chart?: any }
}

const API_BASE = process.env.NEXT_PUBLIC_API_BASE_URL ?? "";

const card: React.CSSProperties = {
    background: "#0d0000",
    border: "1px solid rgba(239,68,68,0.15)",
    borderRadius: 8,
    padding: 20,
};

export default function SuperAdminPage() {
    const router = useRouter();
    const isMobile = useIsMobile();
    const [section, setSection] = useState<Section>("overview");
    const [me, setMe] = useState<Me | null>(null);
    const [loading, setLoading] = useState(true);
    const dlg = useSaDialog();
    useEffect(() => {
        (async () => {
            try {
                const data = await apiFetch<Me>("/api/auth/me");
                if (data.role !== "SuperAdmin") {
                    router.push("/dashboard");
                    return;
                }
                setMe(data);
                setLoading(false);
            } catch {
                router.push("/login");
            }
        })();
    }, [router]);

    if (loading) return (
        <div style={{
            minHeight: "100vh", display: "flex", alignItems: "center", justifyContent: "center",
            background: "#050505", color: "#94A3B8", fontFamily: "'JetBrains Mono', monospace",
        }}>
            Vérification des autorisations...
        </div>
    );

    return (
        <SaDialogContext.Provider value={dlg}>
        <div style={{ display: "flex", flexDirection: isMobile ? "column" : "row", minHeight: "100vh", background: "#050505", fontFamily: "Inter, sans-serif" }}>
            {/* Sidebar */}
            <aside style={{
                width: isMobile ? "100%" : 220,
                flexShrink: 0,
                background: "#080000",
                borderRight: isMobile ? "none" : "1px solid rgba(239,68,68,0.12)",
                borderBottom: isMobile ? "1px solid rgba(239,68,68,0.12)" : "none",
                display: "flex",
                flexDirection: "column",
                position: isMobile ? "static" : "sticky",
                top: 0,
                height: isMobile ? "auto" : "100vh",
            }}>
                <div style={{ padding: "20px 16px", borderBottom: "1px solid rgba(239,68,68,0.1)" }}>
                    <div style={{
                        display: "inline-flex", alignItems: "center", gap: 6,
                        padding: "4px 10px",
                        background: "rgba(239,68,68,0.12)",
                        border: "1px solid rgba(239,68,68,0.3)",
                        borderRadius: 5,
                    }}>
                        <span style={{ width: 6, height: 6, borderRadius: "50%", background: "#ef4444", boxShadow: "0 0 6px #ef4444" }} />
                        <span style={{ fontSize: 10, color: "#f87171", fontFamily: "'JetBrains Mono', monospace", letterSpacing: "0.15em" }}>SUPERADMIN</span>
                    </div>
                    <div style={{ marginTop: 10, fontSize: 11, color: "#94A3B8", fontFamily: "'JetBrains Mono', monospace" }}>
                        {me?.email}
                    </div>
                </div>

                <nav style={{
                    flex: 1, padding: "12px 8px",
                    overflowY: isMobile ? "hidden" : "auto",
                    overflowX: isMobile ? "auto" : "hidden",
                    display: isMobile ? "flex" : "block",
                    gap: isMobile ? 6 : 0,
                }}>
                    {([
                        ["overview",      "▸ Vue d'ensemble"],
                        ["tenants",       "▸ Entreprises"],
                        ["users",         "▸ Utilisateurs"],
                        ["memberships",   "▸ Multi-sociétés"],
                        ["content",       "▸ Contenus"],
                        ["catalog",       "▸ Catalogue parcours"],
                        ["matrix",        "▸ Matrice d'attribution"],
                        ["domains",       "▸ Domaines SSO"],
                        ["licenses",      "▸ Licences"],
                        ["health",        "▸ Santé système"],
                        ["audit",         "▸ Journal d'audit"],
                        ["announcements", "▸ Annonces"],
                        ["exports",       "▸ Exports"],
                        ["activity",      "▸ Activité globale"],
                        ["statsglobal",   "▸ Stats avancées"],
                    ] as [Section, string][]).map(([key, label]) => (
                        <button key={key}
                            onClick={() => setSection(key)}
                            style={{
                                width: isMobile ? "auto" : "100%",
                                flexShrink: isMobile ? 0 : undefined,
                                whiteSpace: isMobile ? "nowrap" : undefined,
                                textAlign: "left",
                                padding: "9px 12px",
                                borderRadius: 6,
                                border: "none",
                                cursor: "pointer",
                                fontSize: 13,
                                marginBottom: isMobile ? 0 : 2,
                                background: section === key ? "rgba(239,68,68,0.08)" : "transparent",
                                color: section === key ? "#f87171" : "#D1D5DB",
                                borderLeft: section === key ? "2px solid #ef4444" : "2px solid transparent",
                            }}
                        >{label}</button>
                    ))}
                </nav>

                <div style={{ padding: "12px 16px", borderTop: "1px solid rgba(239,68,68,0.08)" }}>
                    <button
                        onClick={() => router.push("/dashboard")}
                        style={{
                            width: "100%", padding: 8,
                            background: "transparent",
                            border: "1px solid #E2E8F0",
                            borderRadius: 6, color: "#94A3B8",
                            fontSize: 12, cursor: "pointer",
                        }}
                    >← Retour dashboard</button>
                </div>
            </aside>

            {/* Main */}
            <main style={{ flex: 1, minWidth: 0, overflowX: "hidden" }}>
                <div style={{
                    background: "#0a0000",
                    borderBottom: "1px solid rgba(239,68,68,0.2)",
                    padding: isMobile ? "8px var(--page-x)" : "0 32px",
                    minHeight: 56,
                    display: "flex", alignItems: "center", justifyContent: "space-between",
                    gap: 8, flexWrap: "wrap",
                    position: "sticky", top: 0, zIndex: 10,
                }}>
                    <span style={{ fontSize: 13, color: "#94A3B8" }}>
                        Console Plateforme Sentys — SuperAdmin
                    </span>
                    <RealtimeWidget />
                </div>

                {section === "overview"      && <OverviewSection />}
                {section === "tenants"       && <TenantsSection />}
                {section === "users"         && <UsersSection />}
                {section === "memberships"   && <MembershipsSection />}
                {section === "content"       && <ContentSection />}
                {section === "catalog"       && <CatalogSection />}
                {section === "matrix"        && <CatalogMatrix />}
                {section === "domains"       && <DomainsSection />}
                {section === "licenses"      && <LicensesSection />}
                {section === "health"        && <HealthSection />}
                {section === "audit"         && <AuditSection />}
                {section === "announcements" && <AnnouncementsSection />}
                {section === "exports"       && <ExportsSection />}
                {section === "activity"      && <ActivitySection />}
                {section === "statsglobal"   && <StatsGlobalSection />}
            </main>
            {dlg.view}
        </div>
        </SaDialogContext.Provider>
    );
}

// Humanise les erreurs du backend en messages métier clairs.
function humaniseBackendError(raw: string): { title: string; body: string } {
    const r = raw.toLowerCase();
    if (r.includes("cannot delete demo") || r.includes("cannot modify demo"))
        return {
            title: "Tenant Demo protégé",
            body: "Le tenant Demo est le tenant de démonstration de la plateforme et ne peut pas être supprimé ni modifié. Supprimer cette protection ferait casser les parcours de découverte exposés publiquement.",
        };
    if (r.includes("utilisateurs actifs") || r.includes("active users"))
        return {
            title: "Tenant non vide",
            body: `Ce tenant contient encore des utilisateurs actifs. Désactivez-les ou migrez-les vers un autre tenant avant de supprimer ce tenant. (${raw})`,
        };
    if (r.includes("votre propre compte") || r.includes("own account"))
        return {
            title: "Auto-désactivation impossible",
            body: "Vous ne pouvez pas désactiver votre propre compte SuperAdmin. Demandez à un autre SuperAdmin de procéder, ou suivez SUPERADMIN_PROCEDURE.md pour retirer l'accès en sécurité.",
        };
    if (r.includes("existe déjà") || r.includes("already exists"))
        return { title: "Conflit", body: raw };
    if (r.includes("introuvable") || r.includes("not found"))
        return { title: "Introuvable", body: raw };
    return { title: "Erreur", body: raw };
}

// Wrapper utilitaire : exécute une mutation et affiche automatiquement succès ou garde-fou.
async function runWithDlg<T>(
    dlg: DialogApi,
    successMsg: string,
    fn: () => Promise<T>,
): Promise<T | null> {
    try {
        const r = await fn();
        dlg.toast(successMsg, "success");
        return r;
    } catch (e) {
        const msg = e instanceof Error ? e.message : String(e);
        const h = humaniseBackendError(msg);
        await dlg.error(h.title, h.body);
        return null;
    }
}

// ── OVERVIEW ────────────────────────────────────────────────────────────────
function OverviewSection() {
    const [overview, setOverview] = useState<Any>(null);
    const [system, setSystem] = useState<Any>(null);
    useEffect(() => {
        Promise.all([
            apiFetch<Any>("/api/superadmin/overview"),
            apiFetch<Any>("/api/superadmin/system"),
        ]).then(([o, s]) => { setOverview(o); setSystem(s); }).catch(() => {});
    }, []);

    return (
        <div style={{ padding: 32 }}>
            <h1 style={{ fontSize: 22, fontWeight: 700, color: "#ffffff", marginBottom: 6 }}>Vue d&apos;ensemble</h1>
            <p style={{ color: "#94A3B8", fontSize: 13, marginBottom: 28 }}>
                Supervision globale de toutes les entreprises
            </p>
            <div style={{ display: "grid", gridTemplateColumns: "repeat(4, 1fr)", gap: 16 }}>
                <Kpi label="ENTREPRISES"   value={overview?.totalTenants ?? 0}     color="#f87171" />
                <Kpi label="UTILISATEURS"  value={overview?.totalUsers ?? 0}       color="#ffffff" />
                <Kpi label="FORMATIONS"    value={overview?.totalCompletions ?? 0} color="var(--pr-l)" />
                <Kpi label="ENVIRONNEMENT" value={system?.environment ?? "—"}       color="#4ade80" />
            </div>
        </div>
    );
}

// ── TENANTS CRUD ────────────────────────────────────────────────────────────
function TenantsSection() {
    const dlg = useDlg();
    const [overview, setOverview] = useState<Any>(null);
    const [showCreate, setShowCreate] = useState(false);
    const [name, setName] = useState("");
    const [error, setError] = useState<string | null>(null);

    const reload = useCallback(async () => {
        try { setOverview(await apiFetch<Any>("/api/superadmin/overview")); } catch {}
    }, []);

    useEffect(() => { reload(); }, [reload]);

    async function createTenant() {
        setError(null);
        const r = await runWithDlg(dlg, `Entreprise « ${name} » créée`, () =>
            apiFetch("/api/superadmin/tenants", { method: "POST", body: JSON.stringify({ name }) })
        );
        if (r !== null) { setShowCreate(false); setName(""); reload(); }
    }

    async function toggleTenant(tenantId: string, tName: string, current: boolean) {
        const label = current ? "suspendre" : "réactiver";
        const ok = await dlg.confirm(
            `${current ? "Suspendre" : "Réactiver"} « ${tName} »`,
            `L'entreprise et ses utilisateurs ne pourront ${current ? "plus" : "à nouveau"} se connecter. Confirmer ?`,
            current ? "Suspendre" : "Réactiver",
            current ? "warning" : "info",
        );
        if (!ok) return;
        await runWithDlg(dlg, `Entreprise ${label}e`, () =>
            apiFetch(`/api/superadmin/tenants/${tenantId}/toggle-active`, { method: "PATCH" }));
        reload();
    }

    async function deleteTenant(tenantId: string, tName: string) {
        const typed = await dlg.prompt(
            `Supprimer « ${tName} »`,
            `Cette action est irréversible. Pour confirmer, tapez exactement « ${tName} » ci-dessous.`,
            tName,
            tName,
        );
        if (typed !== tName) return;
        await runWithDlg(dlg, `Entreprise « ${tName} » supprimée`, () =>
            apiFetch(`/api/superadmin/tenants/${tenantId}`, { method: "DELETE" }));
        reload();
    }

    return (
        <div style={{ padding: 32 }}>
            <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 24 }}>
                <h1 style={{ fontSize: 22, fontWeight: 700, color: "#ffffff" }}>Entreprises</h1>
                <button onClick={() => setShowCreate(true)} style={{
                    background: "#DC2626", color: "#ffffff", border: "none",
                    padding: "9px 16px", borderRadius: 7, fontSize: 13, fontWeight: 600, cursor: "pointer",
                }}>+ Nouvelle entreprise</button>
            </div>

            {showCreate && (
                <div style={{ ...card, marginBottom: 16 }}>
                    <h3 style={{ fontSize: 14, color: "#ffffff", marginBottom: 12 }}>Créer une entreprise</h3>
                    <input value={name} onChange={e => setName(e.target.value)} placeholder="Nom" style={{
                        width: "100%", background: "#0d0000",
                        border: "1px solid rgba(255,255,255,0.1)", borderRadius: 6,
                        padding: "9px 12px", color: "#ffffff", fontSize: 13, marginBottom: 12,
                    }} />
                    {error && <div style={{ color: "#f87171", fontSize: 12, marginBottom: 12 }}>{error}</div>}
                    <div style={{ display: "flex", gap: 8 }}>
                        <button onClick={createTenant} style={{ background: "#DC2626", color: "#fff", border: "none", padding: "8px 14px", borderRadius: 6, fontSize: 12, cursor: "pointer" }}>Créer</button>
                        <button onClick={() => setShowCreate(false)} style={{ background: "transparent", color: "#94A3B8", border: "1px solid rgba(255,255,255,0.1)", padding: "8px 14px", borderRadius: 6, fontSize: 12, cursor: "pointer" }}>Annuler</button>
                    </div>
                </div>
            )}

            <div className="resp-scroll-x" style={{ background: "#0d0000", border: "1px solid rgba(239,68,68,0.12)", borderRadius: 8 }}>
              <div style={{ minWidth: 640 }}>
                <div style={{
                    display: "grid", gridTemplateColumns: "2fr 1fr 1fr 1fr 1fr 1.4fr",
                    padding: "10px 20px", background: "rgba(239,68,68,0.05)",
                    borderBottom: "1px solid rgba(239,68,68,0.1)", gap: 8,
                }}>
                    {["ENTREPRISE","USERS","ADMINS","FORMATIONS","STATUT","ACTIONS"].map(h => (
                        <span key={h} style={{ fontSize: 10, color: "#94A3B8", letterSpacing: "0.08em", fontFamily: "'JetBrains Mono', monospace" }}>{h}</span>
                    ))}
                </div>
                {(overview?.tenants ?? []).map((t: Any, i: number) => (
                    <div key={t.tenantId} style={{
                        display: "grid", gridTemplateColumns: "2fr 1fr 1fr 1fr 1fr 1.4fr",
                        padding: "14px 20px", alignItems: "center", gap: 8,
                        borderBottom: i < overview.tenants.length - 1 ? "1px solid rgba(255,255,255,0.03)" : "none",
                    }}>
                        <span style={{ color: "#ffffff", fontWeight: 600, fontSize: 14 }}>{t.tenantName}</span>
                        <span style={{ color: "#a3a3a3" }}>{t.totalUsers}</span>
                        <span style={{ color: "var(--pr-l)" }}>{t.adminCount}</span>
                        <span style={{ color: "#a3a3a3" }}>{t.totalCompletions}</span>
                        <StatusBadge active={t.isActive} />
                        <div style={{ display: "flex", gap: 6 }}>
                            <SmallBtn color="var(--pr-l)" onClick={() => toggleTenant(t.tenantId, t.tenantName, t.isActive)}>{t.isActive ? "Suspendre" : "Réactiver"}</SmallBtn>
                            <SmallBtn color="#f87171" onClick={() => deleteTenant(t.tenantId, t.tenantName)}>Supprimer</SmallBtn>
                        </div>
                    </div>
                ))}
              </div>
            </div>
        </div>
    );
}

// ── USERS GLOBAL ────────────────────────────────────────────────────────────
function UsersSection() {
    const dlg = useDlg();
    const [data, setData] = useState<Any>(null);
    const [page, setPage] = useState(1);
    const [search, setSearch] = useState("");
    const [role, setRole] = useState("");

    const reload = useCallback(async () => {
        const params = new URLSearchParams({ page: String(page), pageSize: "25" });
        if (search) params.set("search", search);
        if (role) params.set("role", role);
        try { setData(await apiFetch<Any>(`/api/superadmin/users?${params}`)); } catch {}
    }, [page, search, role]);

    useEffect(() => { reload(); }, [reload]);

    async function changeRole(userId: string, email: string, current: string) {
        const newRole = current === "admin" ? "user" : "admin";
        const ok = await dlg.confirm(
            `Changer le rôle de ${email}`,
            `Promouvoir « ${email} » au rôle ${newRole.toUpperCase()} ? Le changement prend effet à la prochaine connexion.`,
            `Définir comme ${newRole}`,
            "warning",
        );
        if (!ok) return;
        await runWithDlg(dlg, `Rôle mis à jour : ${newRole}`, () =>
            apiFetch(`/api/superadmin/users/${userId}/role`, { method: "PATCH", body: JSON.stringify({ role: newRole }) }));
        reload();
    }

    async function deleteUser(userId: string, email: string) {
        const ok = await dlg.confirm(
            `Supprimer ${email}`,
            `Cette action est irréversible : compte, progressions et assignations seront supprimés. Continuer ?`,
            "Supprimer définitivement",
            "danger",
        );
        if (!ok) return;
        await runWithDlg(dlg, "Utilisateur supprimé", () =>
            apiFetch(`/api/superadmin/users/${userId}`, { method: "DELETE" }));
        reload();
    }

    async function resetPassword(userId: string, email: string) {
        const ok = await dlg.confirm(
            `Réinitialiser le mot de passe de ${email}`,
            `Un mot de passe temporaire sera généré. Toutes les sessions actives seront invalidées. Le mot de passe sera affiché une seule fois — vous devrez le communiquer à l'utilisateur par un canal sécurisé.`,
            "Générer un nouveau mot de passe",
            "warning",
        );
        if (!ok) return;
        try {
            const r = await apiFetch<{ temporaryPassword: string }>(`/api/superadmin/users/${userId}/reset-password`, { method: "POST" });
            await dlg.alert(
                `Mot de passe temporaire pour ${email}`,
                `Copiez-le maintenant, il ne sera plus affiché :\n\n${r.temporaryPassword}\n\nL'utilisateur devra le changer à sa prochaine connexion.`,
                "success",
            );
            reload();
        } catch (e) {
            const h = humaniseBackendError(e instanceof Error ? e.message : String(e));
            await dlg.error(h.title, h.body);
        }
    }

    async function toggleActive(userId: string, email: string, current: boolean) {
        const ok = await dlg.confirm(
            `${current ? "Désactiver" : "Réactiver"} ${email}`,
            `L'utilisateur ${current ? "ne pourra plus se connecter" : "pourra à nouveau se connecter"}. Confirmer ?`,
            current ? "Désactiver" : "Réactiver",
            current ? "warning" : "info",
        );
        if (!ok) return;
        await runWithDlg(dlg, "Statut utilisateur mis à jour", () =>
            apiFetch(`/api/superadmin/users/${userId}/toggle-active`, { method: "PATCH" }));
        reload();
    }

    return (
        <div style={{ padding: 32 }}>
            <h1 style={{ fontSize: 22, fontWeight: 700, color: "#ffffff", marginBottom: 24 }}>Utilisateurs (global)</h1>

            <div style={{ display: "flex", gap: 12, marginBottom: 16 }}>
                <input value={search} onChange={e => { setSearch(e.target.value); setPage(1); }}
                    placeholder="Rechercher..." style={inputStyle()} />
                <select value={role} onChange={e => { setRole(e.target.value); setPage(1); }} style={inputStyle()}>
                    <option value="">Tous les rôles</option>
                    <option value="user">User</option>
                    <option value="admin">Admin</option>
                </select>
            </div>

            <div className="resp-scroll-x" style={{ background: "#0d0000", border: "1px solid rgba(239,68,68,0.12)", borderRadius: 8 }}>
              <div style={{ minWidth: 680 }}>
                <div style={{
                    display: "grid", gridTemplateColumns: "2fr 1fr 1fr 1fr 1.4fr",
                    padding: "10px 20px", background: "rgba(239,68,68,0.05)",
                    borderBottom: "1px solid rgba(239,68,68,0.1)", gap: 8,
                }}>
                    {["EMAIL","NOM","RÔLE","STATUT","ACTIONS"].map(h => (
                        <span key={h} style={{ fontSize: 10, color: "#94A3B8", letterSpacing: "0.08em", fontFamily: "'JetBrains Mono', monospace" }}>{h}</span>
                    ))}
                </div>
                {(data?.users ?? []).map((u: Any, i: number) => (
                    <div key={u.id} style={{
                        display: "grid", gridTemplateColumns: "2fr 1fr 1fr 1fr 1.4fr",
                        padding: "12px 20px", alignItems: "center", gap: 8,
                        borderBottom: i < data.users.length - 1 ? "1px solid rgba(255,255,255,0.03)" : "none",
                    }}>
                        <span style={{ color: "#ffffff", fontSize: 13 }}>{u.email}</span>
                        <span style={{ color: "#a3a3a3", fontSize: 13 }}>{u.firstName} {u.lastName}</span>
                        <span style={{ color: "var(--pr-l)", fontSize: 12, fontFamily: "'JetBrains Mono', monospace" }}>{u.role}</span>
                        <StatusBadge active={u.isActive} />
                        <div style={{ display: "flex", gap: 6 }}>
                            <SmallBtn color="var(--pr-l)" onClick={() => changeRole(u.id, u.email, u.role)}>Rôle</SmallBtn>
                            <SmallBtn color="#fbbf24" onClick={() => toggleActive(u.id, u.email, u.isActive)}>{u.isActive ? "Désact." : "Réact."}</SmallBtn>
                            <SmallBtn color="#94A3B8" onClick={() => resetPassword(u.id, u.email)}>Reset pwd</SmallBtn>
                            <SmallBtn color="#f87171" onClick={() => deleteUser(u.id, u.email)}>Supprimer</SmallBtn>
                        </div>
                    </div>
                ))}
              </div>
            </div>

            {data && (
                <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginTop: 16 }}>
                    <span style={{ fontSize: 12, color: "#94A3B8" }}>
                        {data.total} utilisateurs · Page {data.page}/{data.totalPages}
                    </span>
                    <div style={{ display: "flex", gap: 8 }}>
                        <SmallBtn color="#a3a3a3" onClick={() => setPage(p => Math.max(1, p - 1))}>← Précédent</SmallBtn>
                        <SmallBtn color="#a3a3a3" onClick={() => setPage(p => Math.min(data.totalPages, p + 1))}>Suivant →</SmallBtn>
                    </div>
                </div>
            )}
        </div>
    );
}

// ── MULTI-SOCIETES (appartenances user ↔ sociétés) ───────────────────────────
type MembershipRow = {
    tenantId: string; tenantName: string; tenantActive: boolean;
    role: string; isDefault: boolean; joinedAt: string;
};
type UserTenantsResp = { userId: string; email: string; tenants: MembershipRow[] };
type TenantLite = { id: string; name: string; isActive: boolean };

function MembershipsSection() {
    const dlg = useDlg();
    const [search, setSearch] = useState("");
    const [results, setResults] = useState<Any[]>([]);
    const [selected, setSelected] = useState<{ id: string; email: string; name: string } | null>(null);
    const [data, setData] = useState<UserTenantsResp | null>(null);
    const [allTenants, setAllTenants] = useState<TenantLite[]>([]);
    const [addTenantId, setAddTenantId] = useState("");
    const [addRole, setAddRole] = useState("user");

    useEffect(() => {
        (async () => {
            try {
                const t = await apiFetch<Any[]>("/api/superadmin/tenants");
                setAllTenants(t.map((x: Any) => ({ id: x.id, name: x.name, isActive: x.isActive })));
            } catch {}
        })();
    }, []);

    const doSearch = useCallback(async () => {
        if (!search.trim()) { setResults([]); return; }
        const params = new URLSearchParams({ page: "1", pageSize: "10", search: search.trim() });
        try { const r = await apiFetch<Any>(`/api/superadmin/users?${params}`); setResults(r.users ?? []); } catch {}
    }, [search]);

    const loadMemberships = useCallback(async (userId: string) => {
        try { setData(await apiFetch<UserTenantsResp>(`/api/superadmin/users/${userId}/tenants`)); } catch {}
    }, []);

    async function selectUser(u: Any) {
        setSelected({ id: u.id, email: u.email, name: `${u.firstName} ${u.lastName}` });
        setResults([]);
        setSearch(u.email);
        await loadMemberships(u.id);
    }

    async function addMembership() {
        if (!selected || !addTenantId) return;
        const tName = allTenants.find(t => t.id === addTenantId)?.name ?? "société";
        const done = await runWithDlg(dlg, `${selected.email} ajouté à ${tName}`, () =>
            apiFetch(`/api/superadmin/users/${selected.id}/tenants`, {
                method: "POST", body: JSON.stringify({ tenantId: addTenantId, role: addRole }),
            }));
        if (done !== null) { setAddTenantId(""); await loadMemberships(selected.id); }
    }

    async function changeRole(m: MembershipRow) {
        if (!selected) return;
        const order = ["user", "admin", "owner"];
        const next = order[(order.indexOf(m.role) + 1) % order.length];
        await runWithDlg(dlg, `Rôle mis à jour : ${next}`, () =>
            apiFetch(`/api/superadmin/users/${selected.id}/tenants/${m.tenantId}`, {
                method: "PATCH", body: JSON.stringify({ role: next }),
            }));
        await loadMemberships(selected.id);
    }

    async function removeMembership(m: MembershipRow) {
        if (!selected) return;
        const ok = await dlg.confirm(
            `Retirer de ${m.tenantName}`,
            `« ${selected.email} » ne sera plus membre de « ${m.tenantName} ». Ses autres sociétés sont conservées. Confirmer ?`,
            "Retirer", "warning");
        if (!ok) return;
        await runWithDlg(dlg, "Société retirée", () =>
            apiFetch(`/api/superadmin/users/${selected.id}/tenants/${m.tenantId}`, { method: "DELETE" }));
        await loadMemberships(selected.id);
    }

    const availableTenants = allTenants.filter(t => !(data?.tenants ?? []).some(m => m.tenantId === t.id));

    return (
        <div style={{ padding: 32 }}>
            <h1 style={{ fontSize: 22, fontWeight: 700, color: "#ffffff", marginBottom: 6 }}>Multi-sociétés</h1>
            <p style={{ fontSize: 13, color: "#94A3B8", marginBottom: 24, maxWidth: 640 }}>
                Affecter un compte à plusieurs sociétés, avec un rôle propre à chaque société.
                Réservé SuperAdmin. La société marquée « défaut » est la société active au login.
            </p>

            <div style={{ display: "flex", gap: 12, marginBottom: 16, position: "relative", maxWidth: 560 }}>
                <input value={search}
                    onChange={e => setSearch(e.target.value)}
                    onKeyDown={e => { if (e.key === "Enter") doSearch(); }}
                    placeholder="Rechercher un utilisateur (email, nom)..." style={{ ...inputStyle(), flex: 1 }} />
                <SmallBtn color="var(--pr-l)" onClick={doSearch}>Rechercher</SmallBtn>
                {results.length > 0 && (
                    <div style={{
                        position: "absolute", top: 44, left: 0, right: 0, zIndex: 5,
                        background: "#0d0000", border: "1px solid rgba(239,68,68,0.2)", borderRadius: 8, overflow: "hidden",
                    }}>
                        {results.map((u: Any) => (
                            <button key={u.id} onClick={() => selectUser(u)} style={{
                                display: "block", width: "100%", textAlign: "left", padding: "10px 14px",
                                background: "transparent", border: "none", borderBottom: "1px solid rgba(255,255,255,0.04)",
                                color: "#e5e5e5", fontSize: 13, cursor: "pointer",
                            }}>
                                <span style={{ color: "#fff" }}>{u.email}</span>
                                <span style={{ color: "#94A3B8" }}> — {u.firstName} {u.lastName} · {u.role}</span>
                            </button>
                        ))}
                    </div>
                )}
            </div>

            {!selected && (
                <div style={{ ...card, color: "#94A3B8", fontSize: 13 }}>
                    Sélectionnez un utilisateur pour voir et gérer ses sociétés.
                </div>
            )}

            {selected && data && (
                <div style={{ ...card }}>
                    <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 18, flexWrap: "wrap", gap: 8 }}>
                        <div>
                            <div style={{ color: "#fff", fontSize: 15, fontWeight: 600 }}>{selected.name}</div>
                            <div style={{ color: "#94A3B8", fontSize: 12, fontFamily: "'JetBrains Mono', monospace" }}>{selected.email}</div>
                        </div>
                        <span style={{ fontSize: 12, color: "#94A3B8" }}>{data.tenants.length} société(s)</span>
                    </div>

                    <div className="resp-scroll-x">
                      <div style={{ minWidth: 560 }}>
                        <div style={{ display: "grid", gridTemplateColumns: "2fr 1fr 1fr 1.6fr", padding: "10px 14px", background: "rgba(239,68,68,0.05)", borderRadius: 6, gap: 8 }}>
                            {["SOCIÉTÉ", "RÔLE", "DÉFAUT", "ACTIONS"].map(h => (
                                <span key={h} style={{ fontSize: 10, color: "#94A3B8", letterSpacing: "0.08em", fontFamily: "'JetBrains Mono', monospace" }}>{h}</span>
                            ))}
                        </div>
                        {data.tenants.map((m) => (
                            <div key={m.tenantId} style={{ display: "grid", gridTemplateColumns: "2fr 1fr 1fr 1.6fr", padding: "12px 14px", alignItems: "center", gap: 8, borderBottom: "1px solid rgba(255,255,255,0.03)" }}>
                                <span style={{ color: "#fff", fontSize: 13 }}>
                                    {m.tenantName}
                                    {!m.tenantActive && <span style={{ color: "#fbbf24", fontSize: 11 }}> (inactive)</span>}
                                </span>
                                <span style={{ color: "var(--pr-l)", fontSize: 12, fontFamily: "'JetBrains Mono', monospace" }}>{m.role}</span>
                                <span>{m.isDefault
                                    ? <span style={{ background: "rgba(16,185,129,0.12)", color: "#34d399", fontSize: 11, padding: "2px 8px", borderRadius: 999 }}>défaut</span>
                                    : <span style={{ color: "#64748B", fontSize: 12 }}>—</span>}</span>
                                <div style={{ display: "flex", gap: 6 }}>
                                    <SmallBtn color="var(--pr-l)" onClick={() => changeRole(m)}>Rôle</SmallBtn>
                                    <SmallBtn color="#f87171" onClick={() => removeMembership(m)}>Retirer</SmallBtn>
                                </div>
                            </div>
                        ))}
                      </div>
                    </div>

                    <div style={{ marginTop: 20, paddingTop: 18, borderTop: "1px solid rgba(239,68,68,0.1)" }}>
                        <div style={{ fontSize: 12, color: "#94A3B8", marginBottom: 10, letterSpacing: "0.05em" }}>AJOUTER UNE SOCIÉTÉ</div>
                        <div style={{ display: "flex", gap: 10, flexWrap: "wrap", alignItems: "center" }}>
                            <select value={addTenantId} onChange={e => setAddTenantId(e.target.value)} style={{ ...inputStyle(), minWidth: 220 }}>
                                <option value="">Choisir une société...</option>
                                {availableTenants.map(t => (
                                    <option key={t.id} value={t.id}>{t.name}{t.isActive ? "" : " (inactive)"}</option>
                                ))}
                            </select>
                            <select value={addRole} onChange={e => setAddRole(e.target.value)} style={inputStyle()}>
                                <option value="user">user</option>
                                <option value="admin">admin</option>
                                <option value="owner">owner</option>
                            </select>
                            <button onClick={addMembership} disabled={!addTenantId} style={{
                                background: addTenantId ? "#DC2626" : "rgba(220,38,38,0.4)", color: "#fff", border: "none",
                                padding: "9px 16px", borderRadius: 6, fontSize: 13, cursor: addTenantId ? "pointer" : "not-allowed",
                            }}>+ Ajouter</button>
                        </div>
                        {availableTenants.length === 0 && (
                            <div style={{ fontSize: 12, color: "#64748B", marginTop: 8 }}>L utilisateur est déjà membre de toutes les sociétés.</div>
                        )}
                    </div>
                </div>
            )}
        </div>
    );
}


// ── CONTENT ─────────────────────────────────────────────────────────────────
function ContentSection() {
    const [data, setData] = useState<Any>(null);
    useEffect(() => { apiFetch<Any>("/api/superadmin/content/overview").then(setData).catch(() => {}); }, []);
    return (
        <div style={{ padding: 32 }}>
            <h1 style={{ fontSize: 22, fontWeight: 700, color: "#ffffff", marginBottom: 24 }}>Contenus</h1>
            <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 16, marginBottom: 24 }}>
                <Kpi label="PARCOURS" value={data?.totalPaths ?? 0} color="var(--pr-l)" />
                <Kpi label="CHALLENGES" value={data?.totalChallenges ?? 0} color="#f87171" />
            </div>
            <h3 style={{ fontSize: 14, color: "#ffffff", marginBottom: 12 }}>Parcours</h3>
            <div style={{ ...card, padding: 0 }}>
                {(data?.paths ?? []).map((p: Any, i: number) => (
                    <div key={p.id} style={{
                        display: "grid", gridTemplateColumns: "3fr 1fr 1fr 1fr",
                        padding: "12px 20px", gap: 8,
                        borderBottom: i < data.paths.length - 1 ? "1px solid rgba(255,255,255,0.03)" : "none",
                    }}>
                        <span style={{ color: "#ffffff", fontSize: 13 }}>{p.title}</span>
                        <span style={{ color: "#a3a3a3", fontSize: 12 }}>{p.level ?? "—"}</span>
                        <span style={{ color: "var(--pr-l)", fontSize: 12 }}>{p.challengeCount} ch.</span>
                        <StatusBadge active={p.isActive} />
                    </div>
                ))}
            </div>
        </div>
    );
}

// ── LICENSES ────────────────────────────────────────────────────────────────
function LicensesSection() {
    const [licenses, setLicenses] = useState<Any[]>([]);
    useEffect(() => { apiFetch<Any[]>("/api/superadmin/licenses").then(setLicenses).catch(() => {}); }, []);

    function planColor(plan: string) {
        return plan === "enterprise" ? "#f59e0b" : plan === "pro" ? "var(--pr-l)" : plan === "starter" ? "var(--pr)" : "#D1D5DB";
    }
    function daysColor(days: number) {
        return days > 30 ? "#4ade80" : days > 7 ? "#fbbf24" : "#f87171";
    }

    return (
        <div style={{ padding: 32 }}>
            <h1 style={{ fontSize: 22, fontWeight: 700, color: "#ffffff", marginBottom: 24 }}>Licences</h1>
            <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fit, minmax(320px, 1fr))", gap: 16 }}>
                {licenses.map(lic => (
                    <div key={lic.id} style={card}>
                        <div style={{ display: "flex", justifyContent: "space-between", alignItems: "flex-start", marginBottom: 14 }}>
                            <div>
                                <div style={{ fontSize: 15, fontWeight: 600, color: "#ffffff" }}>{lic.tenantName}</div>
                                <div style={{ fontSize: 11, color: "#94A3B8", marginTop: 2, fontFamily: "'JetBrains Mono', monospace" }}>{lic.tenantId}</div>
                            </div>
                            <span style={{
                                fontSize: 10, padding: "3px 10px", borderRadius: 4,
                                background: `${planColor(lic.plan)}22`,
                                border: `1px solid ${planColor(lic.plan)}55`,
                                color: planColor(lic.plan),
                                fontFamily: "'JetBrains Mono', monospace",
                                textTransform: "uppercase",
                            }}>{lic.plan}</span>
                        </div>
                        <div style={{ fontSize: 12, color: "#a3a3a3", marginBottom: 6 }}>
                            {lic.currentUsers} / {lic.maxUsers} utilisateurs
                        </div>
                        <div style={{ height: 4, background: "#FFFFFF", borderRadius: 2, overflow: "hidden", marginBottom: 12 }}>
                            <div style={{ width: `${Math.min(100, lic.usagePercent)}%`, height: "100%", background: planColor(lic.plan) }} />
                        </div>
                        <div style={{ fontSize: 11, color: "#94A3B8" }}>
                            Expire le {new Date(lic.expiresAt).toLocaleDateString("fr-FR")}
                        </div>
                        <div style={{ fontSize: 13, fontWeight: 600, color: daysColor(lic.daysLeft), marginTop: 4 }}>
                            {lic.daysLeft > 0 ? `${lic.daysLeft} jours restants` : "EXPIRÉ"}
                        </div>
                    </div>
                ))}
                {licenses.length === 0 && (
                    <div style={{ ...card, color: "#94A3B8", fontSize: 13 }}>Aucune licence enregistrée.</div>
                )}
            </div>
        </div>
    );
}

// ── HEALTH ──────────────────────────────────────────────────────────────────
function HealthSection() {
    const [health, setHealth] = useState<Any>(null);
    const reload = useCallback(() => { apiFetch<Any>("/api/superadmin/health").then(setHealth).catch(() => {}); }, []);
    useEffect(() => {
        reload();
        const t = setInterval(reload, 30000);
        return () => clearInterval(t);
    }, [reload]);

    if (!health) return <div style={{ padding: 32, color: "#94A3B8" }}>Chargement…</div>;

    return (
        <div style={{ padding: 32 }}>
            <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 24 }}>
                <h1 style={{ fontSize: 22, fontWeight: 700, color: "#ffffff" }}>Santé système</h1>
                <span style={{
                    fontSize: 11, padding: "4px 12px", borderRadius: 5,
                    background: health.status === "healthy" ? "rgba(34,197,94,0.1)" : "rgba(239,68,68,0.1)",
                    border: `1px solid ${health.status === "healthy" ? "rgba(34,197,94,0.35)" : "rgba(239,68,68,0.35)"}`,
                    color: health.status === "healthy" ? "#4ade80" : "#f87171",
                    fontFamily: "'JetBrains Mono', monospace",
                    textTransform: "uppercase",
                }}>{health.status}</span>
            </div>

            <div style={{ display: "grid", gridTemplateColumns: "repeat(2, 1fr)", gap: 16 }}>
                <Card2 title="BASE DE DONNÉES">
                    <KV k="status" v={health.database.status} />
                    <KV k="latency" v={health.database.latency} />
                </Card2>
                <Card2 title="MÉMOIRE">
                    <KV k="used" v={health.memory.used} />
                    <KV k="peak" v={health.memory.peak} />
                </Card2>
                <Card2 title="SERVEUR">
                    <KV k="uptime" v={health.server.uptime} />
                    <KV k="environment" v={health.server.environment} />
                    <KV k=".NET" v={health.server.dotnet} />
                    <KV k="processId" v={String(health.server.processId)} />
                    <KV k="threads" v={String(health.server.threads)} />
                </Card2>
                <Card2 title="STATS LIVE">
                    <KV k="totalTenants" v={String(health.stats.totalTenants)} />
                    <KV k="totalUsers" v={String(health.stats.totalUsers)} />
                    <KV k="activeUsers" v={String(health.stats.activeUsers)} />
                    <KV k="totalFormations" v={String(health.stats.totalFormations)} />
                    <KV k="todayFormations" v={String(health.stats.todayFormations)} />
                </Card2>
            </div>

            <div style={{ marginTop: 16, fontSize: 11, color: "#94A3B8" }}>
                Refresh auto toutes les 30 secondes · Dernier check : {new Date(health.checkedAt).toLocaleTimeString("fr-FR")}
            </div>

            <AriaHealthCard />
        </div>
    );
}

// ── ARIA / Ollama diagnostic card ─────────────────────────────────────────────
type AriaStatus = {
    available: boolean;
    status: string;
    provider: string;
    model: string;
    ollamaReachable?: boolean;
    modelInstalled?: boolean;
    modelWarm?: boolean;
    modelLoaded?: string | null;
    installedModels?: string[];
    loadedModels?: string[];
    latencyMs?: number;
    lastError?: string | null;
    suggestions?: string[];
    message?: string;
};

function AriaHealthCard() {
    const [s, setS] = useState<AriaStatus | null>(null);
    const [loading, setLoading] = useState(false);
    const [testResult, setTestResult] = useState<{ success: boolean; latencyMs: number; response?: string; error?: string } | null>(null);

    const reload = useCallback(() => {
        setLoading(true);
        apiFetch<AriaStatus>("/api/chatbot/status")
            .then(setS).catch(() => setS({ available: false, status: "down", provider: "?", model: "?", message: "probe failed" }))
            .finally(() => setLoading(false));
    }, []);
    useEffect(() => {
        reload();
        const t = setInterval(reload, 30000);
        return () => clearInterval(t);
    }, [reload]);

    const runTest = async () => {
        setTestResult(null);
        try {
            const r = await apiFetch<{ success: boolean; latencyMs: number; response?: string; error?: string }>("/api/chatbot/generate-test", { method: "POST" });
            setTestResult(r);
        } catch (e) {
            setTestResult({ success: false, latencyMs: 0, error: e instanceof Error ? e.message : "error" });
        }
    };

    const color =
        s?.status === "ok" ? "#4ade80" :
        s?.status === "degraded" ? "#F59E0B" :
        "#f87171";

    return (
        <div style={{ ...card, marginTop: 20 }}>
            <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 14 }}>
                <h2 style={{ fontSize: 14, fontWeight: 600, color: "#f87171", fontFamily: "'JetBrains Mono', monospace", letterSpacing: "0.06em" }}>
                    ARIA / OLLAMA
                </h2>
                <span style={{ fontSize: 11, color, fontFamily: "'JetBrains Mono', monospace", textTransform: "uppercase" }}>
                    {s?.status ?? "…"}
                </span>
            </div>

            {!s ? (
                <div style={{ color: "#94A3B8", fontSize: 12 }}>Chargement…</div>
            ) : (
                <>
                    <div style={{ display: "grid", gridTemplateColumns: "repeat(2, 1fr)", gap: 8, fontSize: 12, color: "#94A3B8" }}>
                        <KV k="available" v={s.available ? "true" : "false"} />
                        <KV k="ollamaReachable" v={String(s.ollamaReachable ?? "?")} />
                        <KV k="modelInstalled" v={String(s.modelInstalled ?? "?")} />
                        <KV k="modelWarm" v={String(s.modelWarm ?? "?")} />
                        <KV k="model" v={s.model} />
                        <KV k="latencyMs" v={String(s.latencyMs ?? "—")} />
                    </div>

                    {s.installedModels && s.installedModels.length > 0 && (
                        <div style={{ marginTop: 12, fontSize: 11, color: "#94A3B8" }}>
                            <strong>Installés :</strong> {s.installedModels.join(", ")}
                        </div>
                    )}
                    {s.loadedModels && s.loadedModels.length > 0 && (
                        <div style={{ fontSize: 11, color: "#94A3B8" }}>
                            <strong>Chargés en RAM :</strong> {s.loadedModels.join(", ")}
                        </div>
                    )}

                    {s.suggestions && s.suggestions.length > 0 && (
                        <div style={{ marginTop: 10, padding: 10, borderRadius: 6, background: "rgba(245,158,11,0.08)", border: "1px solid rgba(245,158,11,0.2)" }}>
                            {s.suggestions.map((t, i) => (
                                <div key={i} style={{ fontSize: 11, color: "#fbbf24" }}>• {t}</div>
                            ))}
                        </div>
                    )}

                    {s.lastError && (
                        <div style={{ marginTop: 10, fontSize: 11, color: "#f87171" }}>
                            <strong>lastError :</strong> {s.lastError}
                        </div>
                    )}

                    <div style={{ marginTop: 14, display: "flex", gap: 8, flexWrap: "wrap" }}>
                        <button
                            onClick={reload} disabled={loading}
                            style={{ padding: "6px 12px", background: "transparent", color: "#94A3B8", border: "1px solid rgba(239,68,68,0.25)", borderRadius: 6, fontSize: 12, cursor: "pointer" }}
                        >↻ Refresh</button>
                        <button
                            onClick={runTest}
                            style={{ padding: "6px 12px", background: "#3B82F6", color: "#fff", border: "none", borderRadius: 6, fontSize: 12, cursor: "pointer" }}
                        >▶ Test de génération</button>
                    </div>

                    {testResult && (
                        <div style={{
                            marginTop: 12, padding: 10, borderRadius: 6,
                            background: testResult.success ? "rgba(16,185,129,0.08)" : "rgba(239,68,68,0.08)",
                            border: `1px solid ${testResult.success ? "rgba(16,185,129,0.3)" : "rgba(239,68,68,0.3)"}`
                        }}>
                            <div style={{ fontSize: 11, color: testResult.success ? "#34d399" : "#f87171", fontFamily: "'JetBrains Mono', monospace" }}>
                                {testResult.success ? "✓" : "✗"} latency={testResult.latencyMs}ms
                            </div>
                            {testResult.success && testResult.response && (
                                <div style={{ fontSize: 12, color: "#94A3B8", marginTop: 4 }}>
                                    <strong>réponse :</strong> <em>{testResult.response}</em>
                                </div>
                            )}
                            {!testResult.success && testResult.error && (
                                <div style={{ fontSize: 11, color: "#f87171", marginTop: 4, fontFamily: "'JetBrains Mono', monospace" }}>
                                    {testResult.error}
                                </div>
                            )}
                        </div>
                    )}
                </>
            )}
        </div>
    );
}

// ── AUDIT ───────────────────────────────────────────────────────────────────
function AuditSection() {
    const [data, setData] = useState<Any>(null);
    const [severity, setSeverity] = useState("");
    const [page, setPage] = useState(1);

    useEffect(() => {
        const params = new URLSearchParams({ page: String(page), pageSize: "50" });
        if (severity) params.set("severity", severity);
        apiFetch<Any>(`/api/superadmin/audit-logs?${params}`).then(setData).catch(() => {});
    }, [severity, page]);

    function sevColor(s: string) {
        return s === "critical" ? "#f87171" : s === "warning" ? "#fbbf24" : "#D1D5DB";
    }

    return (
        <div style={{ padding: 32 }}>
            <h1 style={{ fontSize: 22, fontWeight: 700, color: "#ffffff", marginBottom: 24 }}>Journal d&apos;audit</h1>

            <div style={{ display: "flex", gap: 8, marginBottom: 16 }}>
                {["", "info", "warning", "critical"].map(s => (
                    <button key={s || "all"} onClick={() => { setSeverity(s); setPage(1); }} style={{
                        background: severity === s ? "rgba(239,68,68,0.12)" : "transparent",
                        border: "1px solid #E2E8F0",
                        color: severity === s ? "#f87171" : "#D1D5DB",
                        padding: "6px 14px", borderRadius: 5, cursor: "pointer",
                        fontSize: 12, textTransform: "uppercase", fontFamily: "'JetBrains Mono', monospace",
                    }}>{s || "TOUS"}</button>
                ))}
            </div>

            <div className="resp-scroll-x" style={{ ...card, padding: 0 }}>
              <div style={{ minWidth: 720 }}>
                {(data?.logs ?? []).map((l: Any, i: number) => (
                    <div key={l.id} style={{
                        display: "grid", gridTemplateColumns: "1.2fr 1fr 2fr 1.2fr 0.8fr",
                        padding: "10px 16px", gap: 12, alignItems: "center",
                        borderBottom: i < data.logs.length - 1 ? "1px solid rgba(255,255,255,0.03)" : "none",
                        fontSize: 12,
                    }}>
                        <span style={{ color: "#94A3B8", fontFamily: "'JetBrains Mono', monospace" }}>
                            {new Date(l.createdAt).toLocaleString("fr-FR")}
                        </span>
                        <span style={{ color: "var(--pr-l)", fontFamily: "'JetBrains Mono', monospace", fontSize: 11 }}>{l.action}</span>
                        <span style={{ color: "#a3a3a3" }}>{l.description}</span>
                        <span style={{ color: "#94A3B8", fontSize: 11 }}>{l.performedBy}</span>
                        <span style={{ color: sevColor(l.severity), fontFamily: "'JetBrains Mono', monospace", fontSize: 10, textTransform: "uppercase" }}>
                            {l.severity}
                        </span>
                    </div>
                ))}
                {(!data?.logs || data.logs.length === 0) && (
                    <div style={{ padding: 24, textAlign: "center", color: "#94A3B8", fontSize: 13 }}>
                        Aucune entrée d&apos;audit.
                    </div>
                )}
              </div>
            </div>
        </div>
    );
}

// ── ANNOUNCEMENTS ───────────────────────────────────────────────────────────
function AnnouncementsSection() {
    const dlg = useDlg();
    const [list, setList] = useState<Any[]>([]);
    const [showCreate, setShowCreate] = useState(false);
    const [editingId, setEditingId] = useState<string | null>(null);
    const [form, setForm] = useState({ title: "", message: "", type: "info" });

    const reload = useCallback(() => {
        apiFetch<Any[]>("/api/superadmin/announcements").then(setList).catch(() => {});
    }, []);
    useEffect(() => { reload(); }, [reload]);

    async function submit() {
        const isEdit = !!editingId;
        const r = await runWithDlg(dlg, isEdit ? "Annonce mise à jour" : "Annonce publiée", () =>
            isEdit
                ? apiFetch(`/api/superadmin/announcements/${editingId}`, { method: "PUT", body: JSON.stringify(form) })
                : apiFetch("/api/superadmin/announcements", { method: "POST", body: JSON.stringify({ ...form, tenantId: null }) })
        );
        if (r !== null) { setShowCreate(false); setEditingId(null); setForm({ title: "", message: "", type: "info" }); reload(); }
    }

    function startEdit(a: Any) {
        setEditingId(a.id);
        setForm({ title: a.title, message: a.message, type: a.type });
        setShowCreate(true);
    }

    async function deleteAnn(id: string, title: string) {
        const ok = await dlg.confirm(
            `Supprimer l'annonce`,
            `L'annonce « ${title} » sera retirée pour tous les utilisateurs. Continuer ?`,
            "Supprimer",
            "danger",
        );
        if (!ok) return;
        await runWithDlg(dlg, "Annonce supprimée", () =>
            apiFetch(`/api/superadmin/announcements/${id}`, { method: "DELETE" }));
        reload();
    }

    return (
        <div style={{ padding: 32 }}>
            <div style={{ display: "flex", justifyContent: "space-between", marginBottom: 24 }}>
                <h1 style={{ fontSize: 22, fontWeight: 700, color: "#ffffff" }}>Annonces</h1>
                <button onClick={() => setShowCreate(true)} style={{
                    background: "#DC2626", color: "#ffffff", border: "none",
                    padding: "9px 16px", borderRadius: 7, fontSize: 13, fontWeight: 600, cursor: "pointer",
                }}>+ Nouvelle annonce</button>
            </div>

            {showCreate && (
                <div style={{ ...card, marginBottom: 16 }}>
                    <input value={form.title} onChange={e => setForm({...form, title: e.target.value})} placeholder="Titre" style={{ ...inputStyle(), marginBottom: 8, width: "100%" }} />
                    <textarea value={form.message} onChange={e => setForm({...form, message: e.target.value})} placeholder="Message" rows={3} style={{ ...inputStyle(), marginBottom: 8, width: "100%", fontFamily: "inherit" }} />
                    <select value={form.type} onChange={e => setForm({...form, type: e.target.value})} style={{ ...inputStyle(), marginBottom: 12, width: "100%" }}>
                        <option value="info">Info</option>
                        <option value="warning">Warning</option>
                        <option value="maintenance">Maintenance</option>
                        <option value="update">Update</option>
                    </select>
                    <div style={{ display: "flex", gap: 8 }}>
                        <button onClick={submit} style={{ background: "#DC2626", color: "#fff", border: "none", padding: "8px 14px", borderRadius: 6, fontSize: 12, cursor: "pointer" }}>{editingId ? "Mettre à jour" : "Publier"}</button>
                        <button onClick={() => { setShowCreate(false); setEditingId(null); setForm({ title: "", message: "", type: "info" }); }} style={{ background: "transparent", color: "#94A3B8", border: "1px solid rgba(255,255,255,0.12)", padding: "8px 14px", borderRadius: 6, fontSize: 12, cursor: "pointer" }}>Annuler</button>
                    </div>
                </div>
            )}

            <div style={{ display: "flex", flexDirection: "column", gap: 12 }}>
                {list.map(a => (
                    <div key={a.id} style={{ ...card, display: "flex", justifyContent: "space-between", alignItems: "flex-start" }}>
                        <div>
                            <div style={{ display: "flex", alignItems: "center", gap: 10, marginBottom: 6 }}>
                                <span style={{
                                    fontSize: 10, padding: "2px 8px", borderRadius: 4,
                                    background: "rgba(59,130,246,0.12)", border: "1px solid rgba(59,130,246,0.3)",
                                    color: "var(--pr-l)", fontFamily: "'JetBrains Mono', monospace", textTransform: "uppercase",
                                }}>{a.type}</span>
                                <span style={{ color: "#ffffff", fontSize: 14, fontWeight: 600 }}>{a.title}</span>
                            </div>
                            <p style={{ color: "#a3a3a3", fontSize: 13, marginBottom: 6 }}>{a.message}</p>
                            <span style={{ color: "#94A3B8", fontSize: 11 }}>
                                {a.tenantId ? `Tenant : ${a.tenantId}` : "Tous les tenants"} · {new Date(a.createdAt).toLocaleDateString("fr-FR")}
                            </span>
                        </div>
                        <div style={{ display: "flex", gap: 6 }}>
                            <SmallBtn color="var(--pr-l)" onClick={() => startEdit(a)}>Modifier</SmallBtn>
                            <SmallBtn color="#f87171" onClick={() => deleteAnn(a.id, a.title)}>Supprimer</SmallBtn>
                        </div>
                    </div>
                ))}
                {list.length === 0 && (
                    <div style={{ ...card, color: "#94A3B8", fontSize: 13 }}>Aucune annonce active.</div>
                )}
            </div>
        </div>
    );
}

// ── EXPORTS ─────────────────────────────────────────────────────────────────
function ExportsSection() {
    function exportCsv(endpoint: string) {
        window.open(`${API_BASE}${endpoint}`, "_blank");
    }
    return (
        <div style={{ padding: 32 }}>
            <h1 style={{ fontSize: 22, fontWeight: 700, color: "#ffffff", marginBottom: 24 }}>Exports CSV</h1>
            <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 16 }}>
                <div style={card}>
                    <h3 style={{ color: "#ffffff", fontSize: 14, marginBottom: 8 }}>Utilisateurs</h3>
                    <p style={{ color: "#94A3B8", fontSize: 12, marginBottom: 14 }}>Tous les utilisateurs de toutes les entreprises.</p>
                    <button onClick={() => exportCsv("/api/superadmin/export/users")} style={{
                        background: "#DC2626", color: "#fff", border: "none", padding: "9px 16px",
                        borderRadius: 6, fontSize: 12, cursor: "pointer", fontWeight: 600,
                    }}>Exporter CSV</button>
                </div>
                <div style={card}>
                    <h3 style={{ color: "#ffffff", fontSize: 14, marginBottom: 8 }}>Formations</h3>
                    <p style={{ color: "#94A3B8", fontSize: 12, marginBottom: 14 }}>Toutes les complétions de toutes les entreprises.</p>
                    <button onClick={() => exportCsv("/api/superadmin/export/completions")} style={{
                        background: "#DC2626", color: "#fff", border: "none", padding: "9px 16px",
                        borderRadius: 6, fontSize: 12, cursor: "pointer", fontWeight: 600,
                    }}>Exporter CSV</button>
                </div>
            </div>
            <p style={{ marginTop: 16, fontSize: 11, color: "#94A3B8" }}>
                Les fichiers s&apos;ouvrent directement dans le navigateur.
            </p>
        </div>
    );
}

// ── ACTIVITY ────────────────────────────────────────────────────────────────
function ActivitySection() {
    const [activity, setActivity] = useState<Any>(null);
    const [chartReady, setChartReady] = useState(false);
    const lineRef = useRef<HTMLCanvasElement>(null);
    const barRef = useRef<HTMLCanvasElement>(null);
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    const charts = useRef<any[]>([]);

    useEffect(() => { apiFetch<Any>("/api/superadmin/activity").then(setActivity).catch(() => {}); }, []);

    useEffect(() => {
        if (window.Chart) { setChartReady(true); return; }
        const s = document.createElement("script");
        s.src = "https://cdnjs.cloudflare.com/ajax/libs/Chart.js/4.4.1/chart.umd.min.js";
        s.onload = () => setChartReady(true);
        document.head.appendChild(s);
    }, []);

    useEffect(() => {
        if (!chartReady || !activity || !window.Chart) return;
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        const Chart = window.Chart as any;
        charts.current.forEach(c => c?.destroy?.());
        charts.current = [];

        const grid = "rgba(255,255,255,0.04)", tick = "#D1D5DB";

        if (lineRef.current) {
            charts.current.push(new Chart(lineRef.current, {
                type: "line",
                data: {
                    labels: activity.completionsByDay.map((d: Any) => new Date(d.date).toLocaleDateString("fr-FR", { day: "2-digit", month: "2-digit" })),
                    datasets: [{ label: "Formations", data: activity.completionsByDay.map((d: Any) => d.count), borderColor: "#ef4444", backgroundColor: "rgba(239,68,68,0.08)", fill: true, tension: 0.4, pointBackgroundColor: "#f87171" }],
                },
                options: { responsive: true, maintainAspectRatio: false, plugins: { legend: { display: false } }, scales: { x: { grid: { color: grid }, ticks: { color: tick } }, y: { grid: { color: grid }, ticks: { color: tick, stepSize: 1 }, beginAtZero: true } } },
            }));
        }
        if (barRef.current) {
            charts.current.push(new Chart(barRef.current, {
                type: "bar",
                data: {
                    labels: activity.newUsersByDay.map((d: Any) => new Date(d.date).toLocaleDateString("fr-FR", { day: "2-digit", month: "2-digit" })),
                    datasets: [{ label: "Nouveaux", data: activity.newUsersByDay.map((d: Any) => d.count), backgroundColor: "rgba(59,130,246,0.6)", borderColor: "var(--pr)", borderWidth: 1, borderRadius: 4 }],
                },
                options: { responsive: true, maintainAspectRatio: false, plugins: { legend: { display: false } }, scales: { x: { grid: { color: grid }, ticks: { color: tick } }, y: { grid: { color: grid }, ticks: { color: tick, stepSize: 1 }, beginAtZero: true } } },
            }));
        }
        return () => { charts.current.forEach(c => c?.destroy?.()); charts.current = []; };
    }, [chartReady, activity]);

    return (
        <div style={{ padding: 32 }}>
            <h1 style={{ fontSize: 22, fontWeight: 700, color: "#ffffff", marginBottom: 28 }}>
                Activité globale — 30 derniers jours
            </h1>
            <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 24 }}>
                <Card2 title="FORMATIONS COMPLÉTÉES">
                    <div style={{ height: 220 }}><canvas ref={lineRef} /></div>
                </Card2>
                <Card2 title="NOUVEAUX UTILISATEURS">
                    <div style={{ height: 220 }}><canvas ref={barRef} /></div>
                </Card2>
            </div>
        </div>
    );
}

// ── HELPERS ─────────────────────────────────────────────────────────────────
function Kpi({ label, value, color }: { label: string; value: string | number; color: string }) {
    return (
        <div style={card}>
            <div style={{ fontSize: 32, fontWeight: 700, fontFamily: "'JetBrains Mono', monospace", color }}>{value}</div>
            <div style={{ fontSize: 11, color: "#94A3B8", letterSpacing: "0.1em", marginTop: 6, textTransform: "uppercase" }}>{label}</div>
        </div>
    );
}

function StatusBadge({ active }: { active: boolean }) {
    return (
        <span style={{
            fontSize: 11, padding: "2px 8px", borderRadius: 4,
            background: active ? "rgba(34,197,94,0.1)" : "rgba(239,68,68,0.1)",
            border: `1px solid ${active ? "rgba(34,197,94,0.3)" : "rgba(239,68,68,0.3)"}`,
            color: active ? "#4ade80" : "#f87171",
            width: "fit-content",
            fontFamily: "'JetBrains Mono', monospace",
            textTransform: "uppercase",
        }}>{active ? "Actif" : "Suspendu"}</span>
    );
}

function SmallBtn({ children, onClick, color }: { children: React.ReactNode; onClick: () => void; color: string }) {
    return (
        <button onClick={onClick} style={{
            fontSize: 11, padding: "4px 10px",
            background: "transparent",
            border: `1px solid ${color}55`,
            borderRadius: 4, color, cursor: "pointer",
        }}>{children}</button>
    );
}

function Card2({ title, children }: { title: string; children: React.ReactNode }) {
    return (
        <div style={card}>
            <h3 style={{ fontSize: 11, color: "#94A3B8", marginBottom: 14, letterSpacing: "0.1em", fontFamily: "'JetBrains Mono', monospace" }}>{title}</h3>
            {children}
        </div>
    );
}

function KV({ k, v }: { k: string; v: string }) {
    return (
        <div style={{ display: "flex", justifyContent: "space-between", padding: "5px 0", fontSize: 12, fontFamily: "'JetBrains Mono', monospace" }}>
            <span style={{ color: "#94A3B8" }}>{k}</span>
            <span style={{ color: "#a3a3a3" }}>{v}</span>
        </div>
    );
}

function inputStyle(): React.CSSProperties {
    return {
        background: "#0d0000",
        border: "1px solid rgba(239,68,68,0.25)",
        borderRadius: 6,
        padding: "8px 12px",
        color: "#ffffff",
        fontSize: 13,
        outline: "none",
    };
}

// ── REALTIME WIDGET (topbar) ────────────────────────────────────────────────
function RealtimeWidget() {
    const [stats, setStats] = useState<Any>(null);
    useEffect(() => {
        const fetchIt = () => apiFetch<Any>("/api/superadmin/stats/realtime").then(setStats).catch(() => {});
        fetchIt();
        const t = setInterval(fetchIt, 30000);
        return () => clearInterval(t);
    }, []);

    if (!stats) return null;

    const pill: React.CSSProperties = {
        display: "inline-flex",
        alignItems: "center",
        gap: 4,
        padding: "3px 10px",
        borderRadius: 10,
        background: "rgba(255,255,255,0.04)",
        border: "1px solid #E2E8F0",
        fontSize: 11,
        fontFamily: "'JetBrains Mono', monospace",
    };

    return (
        <div style={{ display: "flex", gap: 8 }} role="group" aria-label="Indicateurs temps réel SuperAdmin">
            <span style={pill} title={`${stats.todayFormations} formations complétées aujourd'hui`} aria-label={`${stats.todayFormations} formations complétées aujourd'hui`}>
                <span aria-hidden="true" style={{ color: "#94A3B8" }}>📚</span>
                <span style={{ color: "#ffffff" }}>{stats.todayFormations}</span>
            </span>
            <span style={pill} title={`${stats.todayNewUsers} nouveaux utilisateurs aujourd'hui`} aria-label={`${stats.todayNewUsers} nouveaux utilisateurs aujourd'hui`}>
                <span aria-hidden="true" style={{ color: "#94A3B8" }}>👥</span>
                <span style={{ color: "#ffffff" }}>{stats.todayNewUsers}</span>
            </span>
            <span style={pill} title={`${stats.totalActiveSessions} sessions actives`} aria-label={`${stats.totalActiveSessions} sessions actives`}>
                <span aria-hidden="true" style={{ color: "#94A3B8" }}>🔑</span>
                <span style={{ color: "#ffffff" }}>{stats.totalActiveSessions}</span>
            </span>
            <span
                style={{
                    ...pill,
                    color: stats.pendingLicenses > 0 ? "#fbbf24" : "#D1D5DB",
                    background: stats.pendingLicenses > 0 ? "rgba(245,158,11,0.10)" : "rgba(255,255,255,0.04)",
                    border: stats.pendingLicenses > 0 ? "1px solid rgba(245,158,11,0.35)" : "1px solid rgba(255,255,255,0.08)",
                }}
                title={`${stats.pendingLicenses} licences en attente de validation`}
                aria-label={`${stats.pendingLicenses} licences en attente`}
            >
                <span aria-hidden="true">⚠</span>
                <span>{stats.pendingLicenses}</span>
            </span>
        </div>
    );
}

// ── STATS GLOBAL SECTION ────────────────────────────────────────────────────
function StatsGlobalSection() {
    const [stats, setStats] = useState<Any>(null);
    const [period, setPeriod] = useState<"today" | "week" | "month">("month");

    useEffect(() => { apiFetch<Any>("/api/superadmin/stats/global").then(setStats).catch(() => {}); }, []);

    if (!stats) return <div style={{ padding: 32, color: "#94A3B8" }}>Chargement…</div>;

    const p = stats.periods[period];

    return (
        <div style={{ padding: 32, display: "flex", flexDirection: "column", gap: 24 }}>
            <h1 style={{ fontSize: 22, fontWeight: 700, color: "#ffffff" }}>Statistiques avancées</h1>

            {/* Tabs */}
            <div style={{ display: "flex", gap: 6 }}>
                {(["today", "week", "month"] as const).map(k => {
                    const labels = { today: "Aujourd'hui", week: "7 jours", month: "30 jours" };
                    return (
                        <button key={k} onClick={() => setPeriod(k)} style={{
                            padding: "6px 14px",
                            borderRadius: 6,
                            background: period === k ? "rgba(239,68,68,0.12)" : "transparent",
                            border: "1px solid #E2E8F0",
                            color: period === k ? "#f87171" : "#D1D5DB",
                            fontSize: 12, cursor: "pointer",
                        }}>{labels[k]}</button>
                    );
                })}
            </div>

            {/* KPIs période */}
            <div style={{ display: "grid", gridTemplateColumns: "repeat(3, 1fr)", gap: 16 }}>
                <div style={card}>
                    <div style={{ fontSize: 28, fontWeight: 700, fontFamily: "'JetBrains Mono', monospace", color: "var(--pr-l)" }}>{p.completions}</div>
                    <div style={{ fontSize: 11, color: "#94A3B8", letterSpacing: "0.1em", marginTop: 4 }}>FORMATIONS</div>
                </div>
                <div style={card}>
                    <div style={{ fontSize: 28, fontWeight: 700, fontFamily: "'JetBrains Mono', monospace", color: "#4ade80" }}>{p.newUsers}</div>
                    <div style={{ fontSize: 11, color: "#94A3B8", letterSpacing: "0.1em", marginTop: 4 }}>NOUVEAUX USERS</div>
                </div>
                <div style={card}>
                    <div style={{ fontSize: 28, fontWeight: 700, fontFamily: "'JetBrains Mono', monospace", color: "#fbbf24" }}>{p.avgScore}%</div>
                    <div style={{ fontSize: 11, color: "#94A3B8", letterSpacing: "0.1em", marginTop: 4 }}>SCORE MOYEN</div>
                </div>
            </div>

            {/* Top tenants */}
            <div style={card}>
                <h3 style={{ fontSize: 14, color: "#ffffff", marginBottom: 14 }}>Top 5 entreprises actives (30j)</h3>
                {(stats.topTenants ?? []).map((t: Any, i: number) => (
                    <div key={i} style={{ display: "flex", justifyContent: "space-between", padding: "8px 0", borderBottom: "1px solid #E2E8F0", fontSize: 13 }}>
                        <span style={{ color: "#a3a3a3" }}>#{i + 1} {t.tenantName}</span>
                        <span style={{ color: "var(--pr-l)", fontFamily: "'JetBrains Mono', monospace" }}>
                            {t.completions} · {t.avgScore}%
                        </span>
                    </div>
                ))}
                {(stats.topTenants ?? []).length === 0 && <div style={{ color: "#94A3B8", fontSize: 12 }}>Aucune activité ce mois.</div>}
            </div>

            {/* Top challenges */}
            <div style={card}>
                <h3 style={{ fontSize: 14, color: "#ffffff", marginBottom: 14 }}>Top 10 challenges les plus joués</h3>
                {(stats.topChallenges ?? []).map((c: Any, i: number) => (
                    <div key={i} style={{ display: "grid", gridTemplateColumns: "1fr auto auto", gap: 12, padding: "8px 0", borderBottom: "1px solid #E2E8F0", fontSize: 13, alignItems: "center" }}>
                        <span style={{ color: "#a3a3a3", overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap" }}>{c.title}</span>
                        <span style={{ color: "var(--pr-l)", fontFamily: "'JetBrains Mono', monospace" }}>{c.completions}</span>
                        <span style={{ color: c.avgScore >= 80 ? "#4ade80" : c.avgScore >= 50 ? "#fbbf24" : "#f87171", fontFamily: "'JetBrains Mono', monospace" }}>{c.avgScore}%</span>
                    </div>
                ))}
            </div>

            {/* Distribution scores */}
            <div style={card}>
                <h3 style={{ fontSize: 14, color: "#ffffff", marginBottom: 14 }}>Distribution scores globale</h3>
                <div style={{ display: "flex", gap: 8 }}>
                    {(stats.scoreDistribution ?? []).map((s: Any, i: number) => {
                        const colors = ["#ef4444", "#f59e0b", "#fbbf24", "#10B981"];
                        return (
                            <div key={i} style={{ flex: 1, textAlign: "center" }}>
                                <div style={{ fontSize: 24, fontWeight: 700, fontFamily: "'JetBrains Mono', monospace", color: colors[i] }}>{s.count}</div>
                                <div style={{ fontSize: 11, color: "#94A3B8", marginTop: 4 }}>{s.range}%</div>
                            </div>
                        );
                    })}
                </div>
            </div>

            {/* Croissance */}
            <div style={card}>
                <h3 style={{ fontSize: 14, color: "#ffffff", marginBottom: 6 }}>Croissance & rétention</h3>
                <div style={{ fontSize: 13, color: "#a3a3a3", marginBottom: 4 }}>
                    Total users : <span style={{ color: "#ffffff", fontFamily: "'JetBrains Mono', monospace" }}>{stats.growth.totalUsers}</span>
                </div>
                <div style={{ fontSize: 13, color: "#a3a3a3", marginBottom: 4 }}>
                    Actifs (7j) : <span style={{ color: "#4ade80", fontFamily: "'JetBrains Mono', monospace" }}>{stats.growth.activeLastWeek}</span>
                </div>
                <div style={{ fontSize: 13, color: "#a3a3a3" }}>
                    Rétention : <span style={{ color: "var(--pr-l)", fontFamily: "'JetBrains Mono', monospace" }}>{stats.growth.retentionRate}%</span>
                </div>
            </div>

            {/* Licences expirantes */}
            {(stats.expiringLicenses ?? []).length > 0 && (
                <div style={{ ...card, borderColor: "rgba(245,158,11,0.35)" }}>
                    <h3 style={{ fontSize: 14, color: "#fbbf24", marginBottom: 14 }}>⚠ Licences expirant dans 30 jours</h3>
                    {stats.expiringLicenses.map((l: Any, i: number) => (
                        <div key={i} style={{ display: "flex", justifyContent: "space-between", padding: "6px 0", fontSize: 12 }}>
                            <span style={{ color: "#a3a3a3", fontFamily: "'JetBrains Mono', monospace" }}>{l.tenantId}</span>
                            <span style={{ color: l.daysLeft <= 7 ? "#f87171" : "#fbbf24", fontFamily: "'JetBrains Mono', monospace" }}>
                                {l.plan} · {l.daysLeft}j
                            </span>
                        </div>
                    ))}
                </div>
            )}
        </div>
    );
}
