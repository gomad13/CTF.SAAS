"use client";

import { useState, useMemo } from "react";
import Link from "next/link";
import { motion, useReducedMotion } from "framer-motion";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Plus, Trash2, Users, Search, Pencil, UserPlus } from "lucide-react";
import { apiFetch } from "@/lib/api";
import { useIsMobile } from "@/hooks/useMediaQuery";
import { renderTeamIcon, TEAM_ICON_NAMES } from "@/components/teams/teamIcons";
import TeamEditModal from "@/components/teams/TeamEditModal";
import Reveal from "@/components/Reveal";
import { Stagger, StaggerItem } from "@/components/Stagger";
import CountUp from "@/components/CountUp";
import VisionCard from "@/components/vision/VisionCard";
import { VisionInput, VisionButton, VisionSelect } from "@/components/vision/VisionForm";
import { staggerContainer, staggerItem } from "@/lib/motion";

type Team = {
    id: string;
    name: string;
    description: string | null;
    color: string | null;
    icon: string | null;
    managerId: string | null;
    memberCount: number;
    parcoursCount: number;
    compliancePercent: number;
    createdAt: string;
    maxMembers: number | null;
    isOpen: boolean;
};

type UnassignedUser = {
    userId: string;
    email: string;
    firstName: string;
    lastName: string;
    isActive: boolean;
    createdAt: string;
};

// Code couleur compliance (tokens Vision) : bon ≥80 / moyen ≥50 / faible.
function complianceColor(pct: number) {
    if (pct >= 80) return "var(--v-success)";
    if (pct >= 50) return "var(--v-warning)";
    return "var(--v-danger)";
}

const thStyle: React.CSSProperties = { padding: "12px 24px", textAlign: "left", fontSize: 11, fontWeight: 700, textTransform: "uppercase", letterSpacing: "0.05em", color: "var(--v-text-2)" };
const tdStyle: React.CSSProperties = { padding: "16px 24px", fontSize: 14, color: "var(--v-text)", verticalAlign: "middle" };

export default function TeamsPage() {
    const qc = useQueryClient();
    const isMobile = useIsMobile();
    const reduce = useReducedMotion();
    const statusQ = useQuery<{ isEnabled: boolean }>({
        queryKey: ["teams", "status"],
        queryFn: () => apiFetch("/api/teams/status"),
    });
    const listQ = useQuery<Team[]>({
        queryKey: ["teams", "list"],
        queryFn: () => apiFetch<Team[]>("/api/admin/teams"),
        enabled: statusQ.data?.isEnabled === true,
    });

    const [showCreate, setShowCreate] = useState(false);
    const [search, setSearch] = useState("");
    const [name, setName] = useState("");
    const [description, setDescription] = useState("");
    const [color, setColor] = useState("#7551FF"); // valeur par défaut du color-picker (donnée équipe, pas theming)
    const [icon, setIcon] = useState("Users");
    const [maxMembers, setMaxMembers] = useState("");
    const [formError, setFormError] = useState<string | null>(null);
    const [editTeam, setEditTeam] = useState<Team | null>(null);

    const createM = useMutation({
        mutationFn: () => apiFetch<Team>("/api/admin/teams", {
            method: "POST",
            body: JSON.stringify({
                name, description, color, icon,
                maxMembers: maxMembers.trim() === "" ? null : Number(maxMembers),
            }),
        }),
        onSuccess: () => {
            qc.invalidateQueries({ queryKey: ["teams", "list"] });
            setShowCreate(false); setName(""); setDescription(""); setColor("#7551FF"); setIcon("Users"); setMaxMembers("");
            setFormError(null);
        },
        onError: (e: Error) => setFormError(e.message || "Erreur"),
    });

    const deleteM = useMutation({
        mutationFn: (id: string) => apiFetch(`/api/admin/teams/${id}`, { method: "DELETE" }),
        onSuccess: () => qc.invalidateQueries({ queryKey: ["teams", "list"] }),
    });

    const filtered = useMemo(() => {
        const q = search.trim().toLowerCase();
        if (!q) return listQ.data ?? [];
        return (listQ.data ?? []).filter(t =>
            t.name.toLowerCase().includes(q) || (t.description ?? "").toLowerCase().includes(q));
    }, [listQ.data, search]);

    if (!statusQ.data?.isEnabled) {
        return (
            <div className="vision-dashboard" style={{ minHeight: "100%" }}>
                <div style={{ maxWidth: 720, margin: "0 auto", padding: "48px 20px", textAlign: "center" }}>
                    <div style={{ margin: "0 auto", display: "flex", height: 56, width: 56, alignItems: "center", justifyContent: "center", borderRadius: 999, background: "var(--v-surface-2)", color: "var(--v-text-2)" }}><Users size={22} /></div>
                    <h1 style={{ marginTop: 16, fontSize: 20, fontWeight: 700, color: "var(--v-text)" }}>Mode Équipes désactivé</h1>
                    <p style={{ marginTop: 8, fontSize: 14, color: "var(--v-text-2)" }}>Activez-le depuis Administration → Paramètres.</p>
                </div>
            </div>
        );
    }

    const rows = filtered;

    return (
        <div className="vision-dashboard" style={{ minHeight: "100%" }}>
            <div style={{ maxWidth: 1120, margin: "0 auto", padding: "24px 20px 80px", display: "flex", flexDirection: "column", gap: 20 }}>
                <Reveal>
                    <div style={{ display: "flex", flexWrap: "wrap", alignItems: "flex-end", justifyContent: "space-between", gap: 16 }}>
                        <div>
                            <h1 style={{ fontSize: 26, fontWeight: 700, color: "var(--v-text)", letterSpacing: "-0.02em" }}>Gestion des équipes</h1>
                            <p style={{ marginTop: 6, fontSize: 13.5, color: "var(--v-text-2)", lineHeight: 1.55, maxWidth: 560 }}>
                                Segmentez vos collaborateurs par département et assignez-leur des parcours spécifiques.
                            </p>
                        </div>
                        <VisionButton type="button" onClick={() => setShowCreate(v => !v)} className="w-full sm:w-auto">
                            <Plus size={15} /> Nouvelle équipe
                        </VisionButton>
                    </div>
                </Reveal>

                {showCreate && (
                    <Reveal>
                        <VisionCard>
                            <h2 style={{ fontSize: 13, fontWeight: 700, textTransform: "uppercase", letterSpacing: "0.05em", color: "var(--v-text-2)" }}>Créer une équipe</h2>
                            <div className="grid grid-cols-1 gap-3 md:grid-cols-2" style={{ marginTop: 16 }}>
                                <VisionInput placeholder="Nom (ex. « Comptabilité »)" value={name} onChange={e => setName(e.target.value)} />
                                <VisionInput placeholder="Description (optionnelle)" value={description} onChange={e => setDescription(e.target.value)} />
                            </div>
                            <div style={{ marginTop: 14, display: "flex", flexWrap: "wrap", alignItems: "center", gap: 18 }}>
                                <label style={{ display: "flex", alignItems: "center", gap: 8, fontSize: 13, color: "var(--v-text)" }}>
                                    Couleur
                                    <input type="color" value={color} onChange={e => setColor(e.target.value)}
                                        style={{ height: 34, width: 48, borderRadius: 8, border: "1px solid var(--v-border)", background: "var(--v-surface-2)", cursor: "pointer" }} />
                                </label>
                                <label style={{ display: "flex", alignItems: "center", gap: 8, fontSize: 13, color: "var(--v-text)" }}>
                                    Nombre max
                                    <VisionInput type="number" min={1} value={maxMembers} onChange={e => setMaxMembers(e.target.value)} placeholder="∞" style={{ width: 84 }} />
                                </label>
                                <div style={{ display: "flex", flexWrap: "wrap", alignItems: "center", gap: 6 }}>
                                    <span style={{ marginRight: 4, fontSize: 13, color: "var(--v-text)" }}>Icône</span>
                                    {TEAM_ICON_NAMES.map(k => {
                                        const active = icon === k;
                                        return (
                                            <button key={k} type="button" onClick={() => setIcon(k)} title={k} className="v-act"
                                                style={{ display: "flex", height: 34, width: 34, alignItems: "center", justifyContent: "center", borderRadius: 8, cursor: "pointer",
                                                    border: "1px solid " + (active ? "var(--v-accent)" : "var(--v-border)"),
                                                    background: active ? "color-mix(in srgb, var(--v-accent) 16%, transparent)" : "var(--v-surface-2)",
                                                    color: active ? "var(--v-accent)" : "var(--v-text-2)" }}>
                                                {renderTeamIcon(k, 16)}
                                            </button>
                                        );
                                    })}
                                </div>
                            </div>
                            {formError && <div style={{ marginTop: 12, fontSize: 12.5, color: "var(--v-danger)" }}>{formError}</div>}
                            <div style={{ marginTop: 16, display: "flex", gap: 8 }}>
                                <VisionButton type="button" disabled={!name.trim() || createM.isPending} onClick={() => createM.mutate()}>
                                    {createM.isPending ? "Création…" : "Créer"}
                                </VisionButton>
                                <VisionButton variant="secondary" type="button" onClick={() => { setShowCreate(false); setFormError(null); }}>Annuler</VisionButton>
                            </div>
                        </VisionCard>
                    </Reveal>
                )}

                <div style={{ position: "relative" }}>
                    <Search size={15} style={{ position: "absolute", left: 12, top: "50%", transform: "translateY(-50%)", color: "var(--v-text-2)", pointerEvents: "none" }} />
                    <VisionInput placeholder="Rechercher une équipe…" value={search} onChange={e => setSearch(e.target.value)} style={{ paddingLeft: 36 }} />
                </div>

                <Reveal>
                    <div style={{ overflow: "hidden", borderRadius: 20, border: "1px solid var(--v-border)", background: "color-mix(in srgb, var(--v-surface) 82%, transparent)", backdropFilter: "blur(14px)", WebkitBackdropFilter: "blur(14px)", boxShadow: "0 8px 30px rgba(0,0,0,0.25)" }}>
                        {isMobile ? (
                            <Stagger className="flex flex-col" gap={0.05}>
                                {rows.map(t => (
                                    <StaggerItem key={t.id}>
                                        <div className="v-row" style={{ display: "flex", alignItems: "flex-start", gap: 12, padding: 16, borderTop: "1px solid var(--v-border)" }}>
                                        <TeamBadge t={t} />
                                        <div style={{ minWidth: 0, flex: 1 }}>
                                            <div style={{ fontWeight: 600, color: "var(--v-text)" }}>{t.name}</div>
                                            {t.description && <div style={{ overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap", fontSize: 12, color: "var(--v-text-2)" }}>{t.description}</div>}
                                            <div style={{ marginTop: 4, display: "flex", flexWrap: "wrap", gap: "4px 16px", fontSize: 12, color: "var(--v-text-2)" }}>
                                                <span><CountUp value={t.memberCount} />{t.maxMembers != null ? ` / ${t.maxMembers}` : ""} membre{t.memberCount > 1 ? "s" : ""}</span>
                                                <span><CountUp value={t.parcoursCount} /> parcours</span>
                                                <span style={{ fontWeight: 700, color: complianceColor(t.compliancePercent) }}><CountUp value={t.compliancePercent} suffix="%" /> compliance</span>
                                            </div>
                                            <div style={{ marginTop: 10 }}><RowActions t={t} onEdit={() => setEditTeam(t)} onDelete={() => { if (confirm(`Supprimer « ${t.name} » ? Les membres seront détachés (pas supprimés).`)) deleteM.mutate(t.id); }} /></div>
                                        </div>
                                        </div>
                                    </StaggerItem>
                                ))}
                                {rows.length === 0 && <EmptyRow search={search} />}
                            </Stagger>
                        ) : (
                            <table style={{ width: "100%", borderCollapse: "collapse", fontSize: 14 }}>
                                <thead style={{ background: "var(--v-surface-2)" }}>
                                    <tr>
                                        <th style={thStyle}>Équipe</th>
                                        <th style={thStyle}>Membres</th>
                                        <th style={thStyle}>Parcours assignés</th>
                                        <th style={thStyle}>Compliance</th>
                                        <th style={{ ...thStyle, textAlign: "right" }}>Actions</th>
                                    </tr>
                                </thead>
                                <motion.tbody variants={staggerContainer(0.05)} initial={reduce ? false : "initial"} animate="animate">
                                    {rows.map(t => (
                                        <motion.tr key={t.id} variants={staggerItem} className="v-row" style={{ borderTop: "1px solid var(--v-border)" }}>
                                            <td style={tdStyle}>
                                                <div style={{ display: "flex", alignItems: "center", gap: 12 }}>
                                                    <TeamBadge t={t} />
                                                    <div>
                                                        <div style={{ fontWeight: 600, color: "var(--v-text)" }}>{t.name}</div>
                                                        {t.description && <div style={{ fontSize: 12, color: "var(--v-text-2)" }}>{t.description}</div>}
                                                    </div>
                                                </div>
                                            </td>
                                            <td style={tdStyle}><CountUp value={t.memberCount} />{t.maxMembers != null ? <span style={{ color: "var(--v-text-2)" }}> / {t.maxMembers}</span> : null}</td>
                                            <td style={tdStyle}><CountUp value={t.parcoursCount} /></td>
                                            <td style={tdStyle}><span style={{ fontWeight: 700, color: complianceColor(t.compliancePercent) }}><CountUp value={t.compliancePercent} suffix="%" /></span></td>
                                            <td style={{ ...tdStyle, textAlign: "right" }}>
                                                <div style={{ display: "inline-flex", alignItems: "center", gap: 8 }}>
                                                    <RowActions t={t} onEdit={() => setEditTeam(t)} onDelete={() => { if (confirm(`Supprimer « ${t.name} » ? Les membres seront détachés (pas supprimés).`)) deleteM.mutate(t.id); }} />
                                                </div>
                                            </td>
                                        </motion.tr>
                                    ))}
                                    {rows.length === 0 && (
                                        <tr><td colSpan={5} style={{ ...tdStyle, textAlign: "center", padding: "48px 24px", color: "var(--v-text-2)" }}>{search ? "Aucune équipe ne correspond." : "Aucune équipe pour l'instant. Cliquez sur « Nouvelle équipe »."}</td></tr>
                                    )}
                                </motion.tbody>
                            </table>
                        )}
                    </div>
                </Reveal>

                <UnassignedMembersSection teams={listQ.data ?? []} />

                {editTeam && (
                    <TeamEditModal
                        team={editTeam}
                        open={!!editTeam}
                        onClose={() => setEditTeam(null)}
                        onSaved={() => {
                            setEditTeam(null);
                            qc.invalidateQueries({ queryKey: ["teams", "list"] });
                            qc.invalidateQueries({ queryKey: ["admin-teams"] });
                        }}
                    />
                )}
            </div>
        </div>
    );
}

// Pastille d'équipe — couleur d'identité (vraie donnée t.color), conservée.
function TeamBadge({ t }: { t: Team }) {
    return (
        <span style={{ display: "flex", height: 36, width: 36, flexShrink: 0, alignItems: "center", justifyContent: "center", borderRadius: 10, color: "var(--v-text)", background: t.color ?? "var(--v-text-2)" }}>
            {renderTeamIcon(t.icon, 16)}
        </span>
    );
}

function RowActions({ t, onEdit, onDelete }: { t: Team; onEdit: () => void; onDelete: () => void }) {
    return (
        <>
            <Link href={`/admin/teams/${t.id}`} className="v-act"
                style={{ borderRadius: 8, border: "1px solid var(--v-border)", padding: "6px 12px", fontSize: 12.5, fontWeight: 600, color: "var(--v-text)", textDecoration: "none" }}>
                Voir
            </Link>
            <button type="button" onClick={onEdit} title="Modifier (nom, capacité, couleur, icône)" className="v-act"
                style={{ display: "inline-flex", borderRadius: 8, border: "1px solid var(--v-border)", padding: 6, color: "var(--v-text-2)", background: "transparent", cursor: "pointer" }}>
                <Pencil size={14} />
            </button>
            <button type="button" onClick={onDelete} title="Supprimer" className="v-act-danger"
                style={{ display: "inline-flex", borderRadius: 8, border: "1px solid color-mix(in srgb, var(--v-danger) 40%, transparent)", padding: 6, color: "var(--v-danger)", background: "transparent", cursor: "pointer" }}>
                <Trash2 size={14} />
            </button>
        </>
    );
}

function EmptyRow({ search }: { search: string }) {
    return (
        <div style={{ padding: "48px 16px", textAlign: "center", fontSize: 14, color: "var(--v-text-2)" }}>
            {search ? "Aucune équipe ne correspond." : "Aucune équipe pour l'instant. Cliquez sur « Nouvelle équipe »."}
        </div>
    );
}

// ── M4 : Membres sans équipe (affectation à l'arrivée) ──
function UnassignedMembersSection({ teams }: { teams: Team[] }) {
    const qc = useQueryClient();
    const unassignedQ = useQuery<UnassignedUser[]>({
        queryKey: ["teams", "unassigned"],
        queryFn: () => apiFetch<UnassignedUser[]>("/api/admin/teams/unassigned"),
    });
    const [error, setError] = useState<string | null>(null);

    const assignM = useMutation({
        mutationFn: ({ userId, teamId }: { userId: string; teamId: string }) =>
            apiFetch(`/api/admin/teams/${teamId}/members`, {
                method: "POST", body: JSON.stringify({ userIds: [userId] }),
            }),
        onSuccess: () => {
            setError(null);
            qc.invalidateQueries({ queryKey: ["teams", "unassigned"] });
            qc.invalidateQueries({ queryKey: ["teams", "list"] });
            qc.invalidateQueries({ queryKey: ["admin-teams"] });
        },
        onError: (e: Error) => setError(e.message || "Affectation impossible."),
    });

    const list = unassignedQ.data ?? [];

    return (
        <Reveal>
            <div style={{ overflow: "hidden", borderRadius: 20, border: "1px solid var(--v-border)", background: "color-mix(in srgb, var(--v-surface) 82%, transparent)", backdropFilter: "blur(14px)", WebkitBackdropFilter: "blur(14px)", boxShadow: "0 8px 30px rgba(0,0,0,0.25)" }}>
                <div style={{ display: "flex", alignItems: "center", gap: 8, borderBottom: "1px solid var(--v-border)", background: "var(--v-surface-2)", padding: "12px 24px", fontSize: 12, fontWeight: 700, textTransform: "uppercase", letterSpacing: "0.05em", color: "var(--v-text-2)" }}>
                    <UserPlus size={14} /> Membres sans équipe — à affecter ({list.length})
                </div>
                {error && <div style={{ borderBottom: "1px solid color-mix(in srgb, var(--v-danger) 40%, transparent)", background: "color-mix(in srgb, var(--v-danger) 10%, transparent)", padding: "8px 24px", fontSize: 12.5, color: "var(--v-danger)" }}>{error}</div>}
                <ul style={{ margin: 0, padding: 0, listStyle: "none" }}>
                    {list.map(u => (
                        <li key={u.userId} className="v-row" style={{ display: "flex", flexWrap: "wrap", alignItems: "center", justifyContent: "space-between", gap: 12, padding: "12px 24px", borderTop: "1px solid var(--v-border)" }}>
                            <div style={{ minWidth: 0 }}>
                                <div style={{ fontSize: 14, fontWeight: 600, color: "var(--v-text)" }}>{u.firstName} {u.lastName}</div>
                                <div style={{ fontSize: 12, color: "var(--v-text-2)" }}>{u.email}</div>
                            </div>
                            <VisionSelect value="" disabled={assignM.isPending || teams.length === 0}
                                onChange={e => { const t = e.target.value; if (t) assignM.mutate({ userId: u.userId, teamId: t }); }}
                                title="Affecter à une équipe" style={{ width: "auto", maxWidth: 220, fontSize: 12.5, padding: "8px 12px" }}>
                                <option value="">Affecter à…</option>
                                {teams.map(t => (
                                    <option key={t.id} value={t.id} disabled={t.maxMembers != null && t.memberCount >= t.maxMembers}>
                                        {t.name}{t.maxMembers != null ? ` (${t.memberCount}/${t.maxMembers})` : ""}
                                        {t.maxMembers != null && t.memberCount >= t.maxMembers ? " — pleine" : ""}
                                    </option>
                                ))}
                            </VisionSelect>
                        </li>
                    ))}
                    {list.length === 0 && (
                        <li style={{ padding: "32px 24px", textAlign: "center", fontSize: 14, color: "var(--v-text-2)" }}>
                            Tous les collaborateurs sont affectés à une équipe. 🎉
                        </li>
                    )}
                </ul>
            </div>
        </Reveal>
    );
}
