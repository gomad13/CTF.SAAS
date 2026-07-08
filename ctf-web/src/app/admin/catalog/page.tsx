"use client";

import { useMemo, useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { motion, AnimatePresence } from "framer-motion";
import { BookOpen, Heart, Shield, Calculator, Banknote, Lock, Play, Square, Info, X, Clock, BarChart2, Package, Target, Sparkles, ListChecks, Quote } from "lucide-react";
import { apiFetch } from "@/lib/api";
import Reveal from "@/components/Reveal";
import { Stagger, StaggerItem } from "@/components/Stagger";
import CountUp from "@/components/CountUp";
import VisionCard from "@/components/vision/VisionCard";
import { VisionInput, VisionSelect, VisionButton } from "@/components/vision/VisionForm";

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
type FicheModule = { title: string; challengeCount: number };
type FicheExample = { title: string; body: string; category: string | null };
type CatalogFiche = {
    id: string; title: string; description: string | null; level: string | null; sector: string | null;
    estimatedMinutes: number | null; tags: string | null; moduleCount: number; challengeCount: number;
    modules: FicheModule[]; themes: string[]; example: FicheExample | null;
};

const sectorIcon = (s: string | null, size = 18): React.ReactNode => {
    switch (s) {
        case "sante": return <Heart size={size} strokeWidth={1.75} />;
        case "cyber-general": return <Shield size={size} strokeWidth={1.75} />;
        case "comptabilite": return <Calculator size={size} strokeWidth={1.75} />;
        case "finance": return <Banknote size={size} strokeWidth={1.75} />;
        default: return <BookOpen size={size} strokeWidth={1.75} />;
    }
};
// Couleur secteur en tokens Vision (finance = --v-success teal, jamais le vert cyber de charte).
const sectorColor = (s: string | null): string => {
    switch (s) {
        case "sante": return "var(--v-danger)";
        case "cyber-general": return "var(--v-accent)";
        case "comptabilite": return "var(--v-warning)";
        case "finance": return "var(--v-success)";
        default: return "var(--v-text-2)";
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
const statusMeta = (s: CatalogItem["status"]): { label: string; color: string } => {
    switch (s) {
        case "not_granted": return { label: "Non accordé", color: "var(--v-text-2)" };
        case "granted_inactive": return { label: "Accordé — à activer", color: "var(--v-accent)" };
        case "activated_global": return { label: "Activé (entreprise)", color: "var(--v-success)" };
        case "activated_teams_only": return { label: "Activé (équipes)", color: "var(--v-success)" };
    }
};
function Pill({ color, children }: { color: string; children: React.ReactNode }) {
    return <span style={{ display: "inline-flex", alignItems: "center", gap: 4, borderRadius: 999, padding: "3px 10px", fontSize: 11, fontWeight: 600, whiteSpace: "nowrap", background: "color-mix(in srgb, " + color + " 15%, transparent)", color, border: "1px solid color-mix(in srgb, " + color + " 30%, transparent)" }}>{children}</span>;
}

export default function AdminCatalogPage() {
    const qc = useQueryClient();
    const [filterSector, setFilterSector] = useState("");
    const [filterLevel, setFilterLevel] = useState("");
    const [search, setSearch] = useState("");
    const [activateFor, setActivateFor] = useState<CatalogItem | null>(null);
    const [ficheFor, setFicheFor] = useState<CatalogItem | null>(null);

    const cat = useQuery<CatalogItem[]>({ queryKey: ["admin-catalog"], queryFn: () => apiFetch<CatalogItem[]>("/api/admin/catalog") });

    const filtered = useMemo(() => {
        const list = cat.data ?? [];
        return list.filter(p => {
            if (filterSector && p.sector !== filterSector) return false;
            if (filterLevel && p.level !== filterLevel) return false;
            if (search && !p.title.toLowerCase().includes(search.toLowerCase()) && !(p.tags ?? "").toLowerCase().includes(search.toLowerCase())) return false;
            return true;
        });
    }, [cat.data, filterSector, filterLevel, search]);

    const activateMut = useMutation({
        mutationFn: ({ id, mode }: { id: string; mode: "global" | "teams_only" }) =>
            apiFetch(`/api/admin/catalog/${id}/activate`, { method: "POST", body: JSON.stringify({ mode }) }),
        onSuccess: () => { qc.invalidateQueries({ queryKey: ["admin-catalog"] }); qc.invalidateQueries({ queryKey: ["admin-paths"] }); qc.invalidateQueries({ queryKey: ["admin-paths-list"] }); setActivateFor(null); },
    });
    const deactivateMut = useMutation({
        mutationFn: (id: string) => apiFetch(`/api/admin/catalog/${id}/deactivate`, { method: "POST" }),
        onSuccess: () => { qc.invalidateQueries({ queryKey: ["admin-catalog"] }); qc.invalidateQueries({ queryKey: ["admin-paths"] }); qc.invalidateQueries({ queryKey: ["admin-paths-list"] }); },
    });

    const selectStyle: React.CSSProperties = { width: "auto", padding: "9px 12px", fontSize: 13 };

    return (
        <div className="vision-dashboard" style={{ minHeight: "100%" }}>
            <div style={{ maxWidth: 1160, margin: "0 auto", padding: "24px 20px 80px" }}>
                <Reveal>
                    <div style={{ marginBottom: 16 }}>
                        <h1 style={{ fontSize: 26, fontWeight: 700, color: "var(--v-text)", letterSpacing: "-0.02em" }}>Catalogue des parcours</h1>
                        <p style={{ marginTop: 6, fontSize: 13.5, color: "var(--v-text-2)", lineHeight: 1.55 }}>Activez les parcours pour votre entreprise ou vos équipes. Les parcours non accordés se débloquent via votre commercial.</p>
                    </div>
                </Reveal>

                <Reveal delay={0.05}>
                    <div style={{ display: "flex", gap: 10, marginBottom: 20, flexWrap: "wrap", alignItems: "center" }}>
                        <div style={{ flex: "1 1 240px", minWidth: 0 }}>
                            <VisionInput placeholder="Rechercher un parcours…" value={search} onChange={e => setSearch(e.target.value)} />
                        </div>
                        <VisionSelect value={filterSector} onChange={e => setFilterSector(e.target.value)} style={selectStyle}>
                            <option value="">Tous secteurs</option>
                            <option value="sante">Santé</option>
                            <option value="cyber-general">Cyber</option>
                            <option value="comptabilite">Comptabilité</option>
                            <option value="finance">Finance</option>
                        </VisionSelect>
                        <VisionSelect value={filterLevel} onChange={e => setFilterLevel(e.target.value)} style={selectStyle}>
                            <option value="">Tous niveaux</option>
                            <option value="beginner">Débutant</option>
                            <option value="intermediate">Intermédiaire</option>
                            <option value="advanced">Avancé</option>
                        </VisionSelect>
                        <span style={{ fontSize: 12, color: "var(--v-text-2)", marginLeft: "auto" }}><CountUp value={filtered.length} /> parcours</span>
                    </div>
                </Reveal>

                {cat.isLoading ? (
                    <div style={{ textAlign: "center", padding: 48, color: "var(--v-text-2)" }}>Chargement…</div>
                ) : filtered.length === 0 ? (
                    <VisionCard><div style={{ textAlign: "center", padding: 24, color: "var(--v-text-2)" }}>Aucun parcours disponible. Contactez votre gestionnaire de compte pour accéder au catalogue.</div></VisionCard>
                ) : (
                    <Stagger className="grid gap-4 [grid-template-columns:repeat(auto-fill,minmax(min(300px,100%),1fr))]" gap={0.05}>
                        {filtered.map(p => {
                            const st = statusMeta(p.status);
                            const col = sectorColor(p.sector);
                            const greyed = p.status === "not_granted";
                            return (
                                <StaggerItem key={p.id}>
                                    <div className="v-catcard" onClick={() => setFicheFor(p)} role="button" tabIndex={0}
                                        onKeyDown={e => { if (e.key === "Enter") setFicheFor(p); }}
                                        style={{ cursor: "pointer", height: "100%", background: "color-mix(in srgb, var(--v-surface) 82%, transparent)", backdropFilter: "blur(14px)", WebkitBackdropFilter: "blur(14px)", border: "1px solid var(--v-border)", borderRadius: 18, padding: 22, opacity: greyed ? 0.7 : 1, display: "flex", flexDirection: "column" }}>
                                        <div style={{ display: "flex", alignItems: "flex-start", justifyContent: "space-between", marginBottom: 12 }}>
                                            <span style={{ width: 42, height: 42, borderRadius: 12, background: "color-mix(in srgb, " + col + " 16%, transparent)", color: col, display: "flex", alignItems: "center", justifyContent: "center" }}>{sectorIcon(p.sector)}</span>
                                            <Pill color={st.color}>{st.label}</Pill>
                                        </div>
                                        <div style={{ fontWeight: 700, color: "var(--v-text)", fontSize: 15, lineHeight: 1.3, marginBottom: 6 }}>{p.title}</div>
                                        <div style={{ fontSize: 12.5, color: "var(--v-text-2)", marginBottom: 12, lineHeight: 1.5, flex: 1 }}>
                                            {p.description ? (p.description.length > 120 ? p.description.slice(0, 120) + "…" : p.description) : "Description à venir."}
                                        </div>
                                        <div style={{ display: "flex", gap: 12, fontSize: 11.5, color: "var(--v-text-2)", flexWrap: "wrap", marginBottom: 14 }}>
                                            <span style={{ display: "inline-flex", alignItems: "center", gap: 3 }}>{sectorIcon(p.sector, 12)} {sectorLabel(p.sector)}</span>
                                            <span style={{ display: "inline-flex", alignItems: "center", gap: 3 }}><BarChart2 size={12} /> {levelLabel(p.level)}</span>
                                            <span style={{ display: "inline-flex", alignItems: "center", gap: 3 }}><Clock size={12} /> {p.estimatedMinutes ?? "—"} min</span>
                                            <span style={{ display: "inline-flex", alignItems: "center", gap: 3 }}><Package size={12} /> {p.challengesCount}</span>
                                        </div>
                                        <div style={{ display: "flex", gap: 6, flexWrap: "wrap" }} onClick={e => e.stopPropagation()}>
                                            <VisionButton variant="secondary" onClick={() => setFicheFor(p)} style={{ padding: "7px 12px", fontSize: 12.5 }}><Info size={13} /> Voir la fiche</VisionButton>
                                            {p.status === "not_granted" && (
                                                <VisionButton variant="ghost" disabled title="Contactez votre commercial pour débloquer" style={{ padding: "7px 12px", fontSize: 12.5, color: "var(--v-text-2)" }}><Lock size={13} /> Contacter commercial</VisionButton>
                                            )}
                                            {p.status === "granted_inactive" && (
                                                <VisionButton onClick={() => setActivateFor(p)} style={{ padding: "7px 12px", fontSize: 12.5 }}><Play size={13} /> Activer</VisionButton>
                                            )}
                                            {(p.status === "activated_global" || p.status === "activated_teams_only") && (
                                                <VisionButton variant="ghost" onClick={() => { if (confirm("Désactiver ce parcours ? La progression des utilisateurs sera conservée.")) deactivateMut.mutate(p.id); }} style={{ padding: "7px 12px", fontSize: 12.5, color: "var(--v-danger)", border: "1px solid color-mix(in srgb, var(--v-danger) 40%, transparent)" }}><Square size={13} /> Désactiver</VisionButton>
                                            )}
                                        </div>
                                    </div>
                                </StaggerItem>
                            );
                        })}
                    </Stagger>
                )}

                {/* Modal activation (flux EXISTANT, inchangé) */}
                <AnimatePresence>
                    {activateFor && (
                        <ModalShell onClose={() => setActivateFor(null)}>
                            <h2 style={{ fontSize: 16, fontWeight: 700, color: "var(--v-text)", marginBottom: 6 }}>Activer : {activateFor.title}</h2>
                            <p style={{ color: "var(--v-text-2)", fontSize: 13, marginBottom: 16 }}>Choisissez comment ce parcours sera disponible :</p>
                            <div style={{ display: "flex", flexDirection: "column", gap: 10 }}>
                                <VisionButton onClick={() => activateMut.mutate({ id: activateFor.id, mode: "global" })} disabled={activateMut.isPending} style={{ flexDirection: "column", alignItems: "flex-start", padding: 14, textAlign: "left" }}>
                                    <span style={{ fontWeight: 700 }}>Toute l&apos;entreprise</span>
                                    <span style={{ fontSize: 12, opacity: 0.9, fontWeight: 400 }}>Tous les utilisateurs, actuels et futurs, verront le parcours.</span>
                                </VisionButton>
                                <VisionButton variant="secondary" onClick={() => activateMut.mutate({ id: activateFor.id, mode: "teams_only" })} disabled={activateMut.isPending} style={{ flexDirection: "column", alignItems: "flex-start", padding: 14, textAlign: "left" }}>
                                    <span style={{ fontWeight: 700 }}>Uniquement équipes</span>
                                    <span style={{ fontSize: 12, color: "var(--v-text-2)", fontWeight: 400 }}>Vous l&apos;assignerez explicitement à certaines équipes.</span>
                                </VisionButton>
                            </div>
                        </ModalShell>
                    )}
                </AnimatePresence>

                {/* Fiche riche immersive (vitrine STATIQUE) */}
                <AnimatePresence>
                    {ficheFor && (
                        <FicheModal item={ficheFor} onClose={() => setFicheFor(null)}
                            onActivate={() => { setActivateFor(ficheFor); setFicheFor(null); }}
                            onDeactivate={() => { if (confirm("Désactiver ce parcours ? La progression des utilisateurs sera conservée.")) { deactivateMut.mutate(ficheFor.id); setFicheFor(null); } }} />
                    )}
                </AnimatePresence>
            </div>
        </div>
    );
}

function ModalShell({ children, onClose, wide = false }: { children: React.ReactNode; onClose: () => void; wide?: boolean }) {
    return (
        <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }} exit={{ opacity: 0 }} transition={{ duration: 0.18 }}
            onClick={onClose} style={{ position: "fixed", inset: 0, background: "rgba(0,0,0,0.6)", backdropFilter: "blur(2px)", display: "flex", alignItems: "center", justifyContent: "center", zIndex: 100, padding: 16 }}>
            <motion.div initial={{ opacity: 0, scale: 0.96, y: 8 }} animate={{ opacity: 1, scale: 1, y: 0 }} exit={{ opacity: 0, scale: 0.96 }} transition={{ duration: 0.22, ease: "easeOut" }}
                onClick={e => e.stopPropagation()} className="vision-dashboard"
                style={{ background: "var(--v-surface)", border: "1px solid var(--v-border)", borderRadius: 20, maxWidth: wide ? 620 : 480, width: "100%", maxHeight: "88vh", overflow: "auto", boxShadow: "0 24px 70px rgba(0,0,0,0.5)" }}>
                {wide ? children : <div style={{ padding: 24 }}>{children}</div>}
            </motion.div>
        </motion.div>
    );
}

function Section({ icon, title, children }: { icon: React.ReactNode; title: string; children: React.ReactNode }) {
    return (
        <div style={{ marginTop: 20 }}>
            <div style={{ display: "flex", alignItems: "center", gap: 8, marginBottom: 10 }}>
                <span style={{ color: "var(--v-accent)", display: "inline-flex" }}>{icon}</span>
                <h3 style={{ fontSize: 14, fontWeight: 700, color: "var(--v-text)" }}>{title}</h3>
            </div>
            {children}
        </div>
    );
}

function FicheModal({ item, onClose, onActivate, onDeactivate }: { item: CatalogItem; onClose: () => void; onActivate: () => void; onDeactivate: () => void }) {
    const q = useQuery<CatalogFiche>({ queryKey: ["catalog-fiche", item.id], queryFn: () => apiFetch<CatalogFiche>(`/api/admin/catalog/${item.id}/fiche`) });
    const d = q.data;
    const col = sectorColor(item.sector);
    const tags = (item.tags ?? "").split(",").map(t => t.trim()).filter(Boolean);

    return (
        <ModalShell onClose={onClose} wide>
            {/* En-tête immersif */}
            <div style={{ position: "relative", padding: "28px 24px", background: `linear-gradient(135deg, color-mix(in srgb, ${col} 55%, var(--v-accent)), var(--v-accent-2))`, borderRadius: "20px 20px 0 0" }}>
                <button onClick={onClose} title="Fermer" style={{ position: "absolute", top: 14, right: 14, display: "inline-flex", background: "color-mix(in srgb, var(--v-bg) 30%, transparent)", border: "none", borderRadius: 8, padding: 6, color: "var(--v-text)", cursor: "pointer" }}><X size={16} /></button>
                <span style={{ display: "inline-flex", width: 52, height: 52, borderRadius: 14, alignItems: "center", justifyContent: "center", background: "color-mix(in srgb, var(--v-bg) 22%, transparent)", color: "var(--v-text)", marginBottom: 12 }}>{sectorIcon(item.sector, 26)}</span>
                <h2 style={{ fontSize: 22, fontWeight: 800, color: "var(--v-text)", letterSpacing: "-0.02em", lineHeight: 1.15 }}>{item.title}</h2>
                <div style={{ marginTop: 12, display: "flex", flexWrap: "wrap", gap: 8 }}>
                    <HeaderChip icon={<Clock size={12} />} label={`${item.estimatedMinutes ?? "—"} min`} />
                    <HeaderChip icon={<BarChart2 size={12} />} label={levelLabel(item.level)} />
                    <HeaderChip icon={sectorIcon(item.sector, 12)} label={sectorLabel(item.sector)} />
                    <HeaderChip icon={<Package size={12} />} label={`${item.challengesCount} exercices`} />
                </div>
            </div>

            <div style={{ padding: 24 }}>
                {q.isLoading ? (
                    <div style={{ height: 220, borderRadius: 14, background: "var(--v-surface-2)", opacity: 0.6 }} />
                ) : !d ? (
                    <div style={{ padding: 24, textAlign: "center", color: "var(--v-text-2)" }}>Fiche indisponible.</div>
                ) : (
                    <>
                        {/* Pour qui / pourquoi */}
                        <Section icon={<Target size={16} />} title="Pour qui, et pourquoi">
                            <p style={{ fontSize: 13.5, color: "var(--v-text-2)", lineHeight: 1.6 }}>
                                {d.description || "Description à venir."}
                            </p>
                            <div style={{ marginTop: 8, fontSize: 12.5, color: "var(--v-text-2)" }}>
                                Public : <b style={{ color: "var(--v-text)" }}>{sectorLabel(d.sector)}</b> · Niveau <b style={{ color: "var(--v-text)" }}>{levelLabel(d.level)}</b>
                            </div>
                            {tags.length > 0 && (
                                <div style={{ marginTop: 10, display: "flex", flexWrap: "wrap", gap: 6 }}>
                                    {tags.map(t => <span key={t} style={{ fontSize: 11, padding: "3px 9px", borderRadius: 999, background: "var(--v-surface-2)", color: "var(--v-text-2)", border: "1px solid var(--v-border)" }}>{t}</span>)}
                                </div>
                            )}
                        </Section>

                        {/* Ce que vous allez apprendre (thèmes réels) */}
                        <Section icon={<Sparkles size={16} />} title="Ce que vous allez apprendre">
                            {d.themes.length > 0 ? (
                                <div style={{ display: "flex", flexWrap: "wrap", gap: 8 }}>
                                    {d.themes.map(t => <Pill key={t} color="var(--v-accent)">{t}</Pill>)}
                                </div>
                            ) : <p style={{ fontSize: 13, color: "var(--v-text-2)" }}>Thèmes à venir.</p>}
                        </Section>

                        {/* Au programme (modules réels) */}
                        <Section icon={<ListChecks size={16} />} title="Au programme">
                            {d.modules.length > 0 ? (
                                <div style={{ display: "flex", flexDirection: "column", gap: 8 }}>
                                    {d.modules.map((m, i) => (
                                        <div key={i} style={{ display: "flex", alignItems: "center", gap: 12, borderRadius: 12, border: "1px solid var(--v-border)", background: "var(--v-surface-2)", padding: "10px 14px" }}>
                                            <span style={{ flexShrink: 0, width: 24, height: 24, borderRadius: 999, display: "inline-flex", alignItems: "center", justifyContent: "center", fontSize: 12, fontWeight: 700, background: "color-mix(in srgb, var(--v-accent) 16%, transparent)", color: "var(--v-accent)" }}>{i + 1}</span>
                                            <span style={{ flex: 1, minWidth: 0, fontSize: 13.5, color: "var(--v-text)" }}>{m.title}</span>
                                            <span style={{ flexShrink: 0, fontSize: 11.5, color: "var(--v-text-2)" }}>{m.challengeCount} exercice{m.challengeCount > 1 ? "s" : ""}</span>
                                        </div>
                                    ))}
                                </div>
                            ) : <p style={{ fontSize: 13, color: "var(--v-text-2)" }}>Programme à venir.</p>}
                        </Section>

                        {/* Exemple représentatif (vrai contenu, aperçu) */}
                        <Section icon={<Quote size={16} />} title="Aperçu — un exemple">
                            {d.example ? (
                                <div style={{ borderRadius: 14, border: "1px solid var(--v-border)", background: "var(--v-surface-2)", padding: 16 }}>
                                    <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", gap: 8, marginBottom: 6 }}>
                                        <span style={{ fontSize: 13.5, fontWeight: 700, color: "var(--v-text)" }}>{d.example.title}</span>
                                        {d.example.category && <Pill color="var(--v-cyan)">{d.example.category}</Pill>}
                                    </div>
                                    <p style={{ fontSize: 13, color: "var(--v-text-2)", lineHeight: 1.6, fontStyle: "italic" }}>« {d.example.body} »</p>
                                    <p style={{ marginTop: 8, fontSize: 11, color: "var(--v-text-3)" }}>Aperçu d&apos;une mise en situation du parcours.</p>
                                </div>
                            ) : <p style={{ fontSize: 13, color: "var(--v-text-2)" }}>Aperçu de contenu à venir pour ce parcours.</p>}
                        </Section>
                    </>
                )}

                {/* CTA — flux EXISTANT */}
                <div style={{ marginTop: 22, display: "flex", gap: 8, flexWrap: "wrap", justifyContent: "flex-end" }}>
                    <VisionButton variant="secondary" onClick={onClose}>Fermer</VisionButton>
                    {item.status === "not_granted" && <VisionButton variant="ghost" disabled style={{ color: "var(--v-text-2)" }}><Lock size={14} /> Contacter commercial</VisionButton>}
                    {item.status === "granted_inactive" && <VisionButton onClick={onActivate}><Play size={14} /> Activer ce parcours</VisionButton>}
                    {(item.status === "activated_global" || item.status === "activated_teams_only") && <VisionButton variant="ghost" onClick={onDeactivate} style={{ color: "var(--v-danger)", border: "1px solid color-mix(in srgb, var(--v-danger) 40%, transparent)" }}><Square size={14} /> Désactiver</VisionButton>}
                </div>
            </div>
        </ModalShell>
    );
}

function HeaderChip({ icon, label }: { icon: React.ReactNode; label: string }) {
    return <span style={{ display: "inline-flex", alignItems: "center", gap: 5, borderRadius: 999, padding: "4px 10px", fontSize: 12, fontWeight: 600, background: "color-mix(in srgb, var(--v-bg) 22%, transparent)", color: "var(--v-text)" }}>{icon} {label}</span>;
}
