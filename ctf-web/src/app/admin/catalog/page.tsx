"use client";

import { useMemo, useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { BookOpen, Heart, Shield, Calculator, Banknote, Lock, Play, Square, Info } from "lucide-react";
import { apiFetch } from "@/lib/api";
import Reveal from "@/components/Reveal";
import { Stagger, StaggerItem } from "@/components/Stagger";
import CountUp from "@/components/CountUp";

type CatalogItem = {
    id: string;
    title: string;
    description: string | null;
    level: string | null;
    sector: string | null;
    estimatedMinutes: number | null;
    tags: string | null;
    challengesCount: number;
    status: "not_granted" | "granted_inactive" | "activated_global" | "activated_teams_only";
    teamsUsingCount: number;
};

const sectorIcon = (s: string | null): React.ReactNode => {
    switch (s) {
        case "sante": return <Heart size={18} strokeWidth={1.75} />;
        case "cyber-general": return <Shield size={18} strokeWidth={1.75} />;
        case "comptabilite": return <Calculator size={18} strokeWidth={1.75} />;
        case "finance": return <Banknote size={18} strokeWidth={1.75} />;
        default: return <BookOpen size={18} strokeWidth={1.75} />;
    }
};
const sectorColor = (s: string | null): string => {
    switch (s) {
        case "sante": return "var(--danger)";
        case "cyber-general": return "var(--accent)";
        case "comptabilite": return "var(--warning)";
        case "finance": return "var(--success)";
        default: return "var(--text-2)";
    }
};
const sectorLabel = (s: string | null): string => {
    const m: Record<string, string> = { sante: "Santé", "cyber-general": "Cyber", comptabilite: "Comptabilité", finance: "Finance" };
    return s ? (m[s] ?? s) : "—";
};
const levelLabel = (l: string | null): string => {
    const m: Record<string, string> = { beginner: "Débutant", intermediate: "Intermédiaire", advanced: "Avancé" };
    return l ? (m[l] ?? l) : "—";
};

const statusMeta = (s: CatalogItem["status"]): { label: string; color: string; bg: string } => {
    switch (s) {
        case "not_granted": return { label: "🔒 Non accordé", color: "var(--text-2)", bg: "var(--surface-2)" };
        case "granted_inactive": return { label: "✅ Accordé — à activer", color: "var(--accent)", bg: "var(--accent-subtle)" };
        case "activated_global": return { label: "🚀 Activé (entreprise)", color: "var(--success-t)", bg: "var(--success-subtle)" };
        case "activated_teams_only": return { label: "🚀 Activé (équipes)", color: "var(--success-t)", bg: "var(--success-subtle)" };
    }
};

export default function AdminCatalogPage() {
    const qc = useQueryClient();
    const [filterSector, setFilterSector] = useState<string>("");
    const [filterLevel, setFilterLevel] = useState<string>("");
    const [search, setSearch] = useState<string>("");
    const [activateFor, setActivateFor] = useState<CatalogItem | null>(null);
    const [previewFor, setPreviewFor] = useState<CatalogItem | null>(null);

    const cat = useQuery<CatalogItem[]>({
        queryKey: ["admin-catalog"],
        queryFn: () => apiFetch<CatalogItem[]>("/api/admin/catalog"),
    });

    const filtered = useMemo(() => {
        const list = cat.data ?? [];
        return list.filter(p => {
            if (filterSector && p.sector !== filterSector) return false;
            if (filterLevel && p.level !== filterLevel) return false;
            if (search && !p.title.toLowerCase().includes(search.toLowerCase()) &&
                !(p.tags ?? "").toLowerCase().includes(search.toLowerCase())) return false;
            return true;
        });
    }, [cat.data, filterSector, filterLevel, search]);

    const activateMut = useMutation({
        mutationFn: ({ id, mode }: { id: string; mode: "global" | "teams_only" }) =>
            apiFetch(`/api/admin/catalog/${id}/activate`, { method: "POST", body: JSON.stringify({ mode }) }),
        onSuccess: () => {
            qc.invalidateQueries({ queryKey: ["admin-catalog"] });
            qc.invalidateQueries({ queryKey: ["admin-paths"] });
            qc.invalidateQueries({ queryKey: ["admin-paths-list"] });
            setActivateFor(null);
        },
    });

    const deactivateMut = useMutation({
        mutationFn: (id: string) => apiFetch(`/api/admin/catalog/${id}/deactivate`, { method: "POST" }),
        onSuccess: () => {
            qc.invalidateQueries({ queryKey: ["admin-catalog"] });
            qc.invalidateQueries({ queryKey: ["admin-paths"] });
            qc.invalidateQueries({ queryKey: ["admin-paths-list"] });
        },
    });

    return (
        <div style={{ padding: "var(--page-x)", background: "var(--bg)", minHeight: "100%" }}>
            <Reveal>
            <div style={{ marginBottom: 16 }}>
                <h1 style={{ fontSize: 22, fontWeight: 700, color: "var(--text)", margin: 0 }}>Catalogue des parcours</h1>
                <p style={{ fontSize: 13, color: "var(--text-2)", margin: "4px 0 0" }}>
                    Activez les parcours pour votre entreprise ou équipes. Les parcours 🔒 non accordés peuvent être débloqués via votre commercial.
                </p>
            </div>
            </Reveal>

            {/* Filtres */}
            <Reveal delay={0.05}>
            <div style={{ display: "flex", gap: 10, marginBottom: 16, flexWrap: "wrap", alignItems: "center" }}>
                <input
                    placeholder="Rechercher..."
                    value={search} onChange={e => setSearch(e.target.value)}
                    style={{ padding: "8px 12px", borderRadius: 8, border: "1px solid var(--border)", fontSize: 13, minWidth: 220 }}
                />
                <select value={filterSector} onChange={e => setFilterSector(e.target.value)}
                    style={{ padding: "8px 12px", borderRadius: 8, border: "1px solid var(--border)", fontSize: 13 }}>
                    <option value="">Tous secteurs</option>
                    <option value="sante">Santé</option>
                    <option value="cyber-general">Cyber</option>
                    <option value="comptabilite">Comptabilité</option>
                    <option value="finance">Finance</option>
                </select>
                <select value={filterLevel} onChange={e => setFilterLevel(e.target.value)}
                    style={{ padding: "8px 12px", borderRadius: 8, border: "1px solid var(--border)", fontSize: 13 }}>
                    <option value="">Tous niveaux</option>
                    <option value="beginner">Débutant</option>
                    <option value="intermediate">Intermédiaire</option>
                    <option value="advanced">Avancé</option>
                </select>
                <span style={{ fontSize: 12, color: "var(--text-2)", marginLeft: "auto" }}>
                    <CountUp value={filtered.length} /> parcours
                </span>
            </div>
            </Reveal>

            {cat.isLoading ? (
                <div style={{ textAlign: "center", padding: 40, color: "var(--text-2)" }}>Chargement...</div>
            ) : filtered.length === 0 ? (
                <div style={{ background: "var(--surface)", border: "1px solid var(--border)", borderRadius: 12, padding: 40, textAlign: "center", color: "var(--text-2)" }}>
                    Aucun parcours disponible. Contactez votre gestionnaire de compte pour accéder au catalogue.
                </div>
            ) : (
                <Stagger className="grid gap-4 [grid-template-columns:repeat(auto-fill,minmax(min(280px,100%),1fr))]">
                    {filtered.map(p => {
                        const st = statusMeta(p.status);
                        const greyed = p.status === "not_granted";
                        return (
                            <StaggerItem key={p.id}>
                            <div style={{
                                background: "var(--surface)",
                                border: "1px solid var(--border)",
                                borderRadius: 12,
                                padding: 24,
                                opacity: greyed ? 0.65 : 1,
                                transition: "all 200ms",
                            }}>
                                <div style={{ display: "flex", alignItems: "flex-start", justifyContent: "space-between", marginBottom: 12 }}>
                                    <div style={{
                                        width: 40, height: 40, borderRadius: 10,
                                        background: `color-mix(in srgb, ${sectorColor(p.sector)} 12%, transparent)`,
                                        color: sectorColor(p.sector),
                                        display: "flex", alignItems: "center", justifyContent: "center"
                                    }}>{sectorIcon(p.sector)}</div>
                                    <span style={{
                                        padding: "3px 10px", borderRadius: 999, fontSize: 11, fontWeight: 600,
                                        background: st.bg, color: st.color, whiteSpace: "nowrap"
                                    }}>{st.label}</span>
                                </div>

                                <div style={{ fontWeight: 600, color: "var(--text)", fontSize: 15, lineHeight: 1.3, marginBottom: 6 }}>{p.title}</div>
                                {p.description && <div style={{ fontSize: 12, color: "var(--text-2)", marginBottom: 12, lineHeight: 1.45 }}>
                                    {p.description.length > 110 ? p.description.slice(0, 110) + "..." : p.description}
                                </div>}

                                <div style={{ display: "flex", gap: 12, fontSize: 11, color: "var(--text-2)", flexWrap: "wrap", marginBottom: 12 }}>
                                    <span>🎯 {sectorLabel(p.sector)}</span>
                                    <span>📊 {levelLabel(p.level)}</span>
                                    <span>⏱ {p.estimatedMinutes ?? "—"} min</span>
                                    <span>📦 {p.challengesCount} challenges</span>
                                    {p.teamsUsingCount > 0 && <span>👥 {p.teamsUsingCount} équipe(s)</span>}
                                </div>

                                <div style={{ display: "flex", gap: 6, flexWrap: "wrap" }}>
                                    <button
                                        onClick={() => setPreviewFor(p)}
                                        style={{
                                            padding: "6px 12px", borderRadius: 6,
                                            background: "transparent", border: "1px solid var(--border)",
                                            color: "var(--text-2)", fontSize: 12, cursor: "pointer",
                                            display: "inline-flex", alignItems: "center", gap: 4,
                                            transition: "all 200ms",
                                        }}
                                    ><Info size={12} /> Aperçu</button>

                                    {p.status === "not_granted" && (
                                        <button
                                            disabled
                                            title="Contactez votre commercial pour débloquer"
                                            style={{
                                                padding: "6px 12px", borderRadius: 6,
                                                background: "var(--surface-2)", border: "1px solid var(--border)",
                                                color: "var(--text-2)", fontSize: 12, cursor: "not-allowed",
                                                display: "inline-flex", alignItems: "center", gap: 4,
                                            }}
                                        ><Lock size={12} /> Contacter commercial</button>
                                    )}

                                    {p.status === "granted_inactive" && (
                                        <button
                                            onClick={() => setActivateFor(p)}
                                            style={{
                                                padding: "6px 12px", borderRadius: 6,
                                                background: "var(--accent)", border: "none",
                                                color: "var(--on-accent)", fontSize: 12, cursor: "pointer",
                                                display: "inline-flex", alignItems: "center", gap: 4,
                                                transition: "background 200ms",
                                            }}
                                        ><Play size={12} /> Activer</button>
                                    )}

                                    {(p.status === "activated_global" || p.status === "activated_teams_only") && (
                                        <button
                                            onClick={() => { if (confirm("Désactiver ce parcours ? La progression des users sera conservée.")) deactivateMut.mutate(p.id); }}
                                            style={{
                                                padding: "6px 12px", borderRadius: 6,
                                                background: "transparent", border: "1px solid var(--danger)",
                                                color: "var(--danger)", fontSize: 12, cursor: "pointer",
                                                display: "inline-flex", alignItems: "center", gap: 4,
                                                transition: "all 200ms",
                                            }}
                                        ><Square size={12} /> Désactiver</button>
                                    )}
                                </div>
                            </div>
                            </StaggerItem>
                        );
                    })}
                </Stagger>
            )}

            {/* Modal activation */}
            {activateFor && (
                <Modal onClose={() => setActivateFor(null)} title={`Activer : ${activateFor.title}`}>
                    <p style={{ color: "var(--text-2)", fontSize: 13, marginBottom: 16 }}>Choisissez comment ce parcours sera disponible :</p>
                    <div style={{ display: "flex", flexDirection: "column", gap: 10 }}>
                        <button
                            onClick={() => activateMut.mutate({ id: activateFor.id, mode: "global" })}
                            disabled={activateMut.isPending}
                            style={{
                                padding: 14, borderRadius: 10,
                                background: "var(--accent)", border: "none",
                                color: "var(--on-accent)", fontSize: 13, textAlign: "left", cursor: "pointer",
                                transition: "background 200ms"
                            }}
                        >
                            <div style={{ fontWeight: 600, marginBottom: 4 }}>Toute l'entreprise</div>
                            <div style={{ fontSize: 12, opacity: 0.9 }}>Tous les utilisateurs actuels et futurs verront le parcours dans « Mes parcours ».</div>
                        </button>
                        <button
                            onClick={() => activateMut.mutate({ id: activateFor.id, mode: "teams_only" })}
                            disabled={activateMut.isPending}
                            style={{
                                padding: 14, borderRadius: 10,
                                background: "transparent", border: "1px solid var(--border)",
                                color: "var(--text)", fontSize: 13, textAlign: "left", cursor: "pointer",
                                transition: "all 200ms"
                            }}
                        >
                            <div style={{ fontWeight: 600, marginBottom: 4 }}>Uniquement équipes</div>
                            <div style={{ fontSize: 12, color: "var(--text-2)" }}>Vous l'assignerez explicitement à certaines équipes depuis la page Équipes.</div>
                        </button>
                    </div>
                </Modal>
            )}

            {/* Modal preview */}
            {previewFor && (
                <Modal onClose={() => setPreviewFor(null)} title={previewFor.title}>
                    <div style={{ fontSize: 13, color: "var(--text-2)", lineHeight: 1.5 }}>
                        {previewFor.description && <p style={{ marginBottom: 12 }}>{previewFor.description}</p>}
                        <div style={{ display: "grid", gridTemplateColumns: "repeat(2, 1fr)", gap: 8, fontSize: 12, color: "var(--text-2)" }}>
                            <div><strong>Secteur:</strong> {sectorLabel(previewFor.sector)}</div>
                            <div><strong>Niveau:</strong> {levelLabel(previewFor.level)}</div>
                            <div><strong>Durée:</strong> {previewFor.estimatedMinutes ?? "—"} min</div>
                            <div><strong>Challenges:</strong> {previewFor.challengesCount}</div>
                        </div>
                        {previewFor.tags && <div style={{ marginTop: 12, display: "flex", gap: 6, flexWrap: "wrap" }}>
                            {previewFor.tags.split(",").map(t => t.trim()).filter(Boolean).map(t => (
                                <span key={t} style={{ fontSize: 11, padding: "2px 8px", borderRadius: 999, background: "var(--surface-2)", color: "var(--text-2)", border: "1px solid var(--border)" }}>{t}</span>
                            ))}
                        </div>}
                    </div>
                </Modal>
            )}
        </div>
    );
}

function Modal({ children, onClose, title }: { children: React.ReactNode; onClose: () => void; title: string }) {
    return (
        <div onClick={onClose} style={{
            position: "fixed", inset: 0, background: "rgba(0,0,0,0.5)",
            display: "flex", alignItems: "center", justifyContent: "center", zIndex: 100, padding: 16,
        }}>
            <div onClick={e => e.stopPropagation()} style={{
                background: "var(--surface)", borderRadius: 12, padding: 24,
                maxWidth: 480, width: "90%", maxHeight: "80vh", overflow: "auto",
                boxShadow: "0 10px 40px rgba(0,0,0,0.15)"
            }}>
                <div style={{ fontWeight: 600, color: "var(--text)", fontSize: 16, marginBottom: 14 }}>{title}</div>
                {children}
                <div style={{ display: "flex", justifyContent: "flex-end", marginTop: 16 }}>
                    <button onClick={onClose} style={{
                        padding: "8px 14px", background: "transparent", border: "1px solid var(--border)",
                        borderRadius: 8, color: "var(--text-2)", fontSize: 13, cursor: "pointer"
                    }}>Fermer</button>
                </div>
            </div>
        </div>
    );
}
