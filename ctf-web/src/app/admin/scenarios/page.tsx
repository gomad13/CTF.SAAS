"use client";

import Link from "next/link";
import { useScenarioCatalog, useScenarioInstances } from "@/lib/hooks/useScenarios";
import {
    Theater, Mail, Clock, ShieldAlert, ChevronRight, Activity,
} from "lucide-react";
import type { ScenarioCatalogItem } from "@/lib/types/scenarios";

const CATEGORY_LABELS: Record<string, string> = {
    ceo_fraud: "Fraude au président",
    hr_phishing: "Phishing RH",
    it_phishing: "Phishing IT",
    supplier_fraud: "Fraude fournisseur",
    delivery_phishing: "Phishing livraison",
};

const DIFFICULTY_TONES: Record<string, { bg: string; fg: string; label: string }> = {
    easy: { bg: "rgba(16,185,129,0.10)", fg: "#10B981", label: "Facile" },
    medium: { bg: "rgba(245,158,11,0.10)", fg: "#F59E0B", label: "Intermédiaire" },
    hard: { bg: "rgba(239,68,68,0.10)", fg: "#EF4444", label: "Avancé" },
};

export default function AdminScenariosPage() {
    const { data: catalog, isLoading } = useScenarioCatalog();
    const { data: instances } = useScenarioInstances();
    const runningCount = instances?.filter(i => i.status === "running" || i.status === "scheduled").length ?? 0;

    return (
        <div style={{ padding: "var(--page-x)", background: "#F8FAFC", minHeight: "100%" }}>
            {/* Header */}
            <div style={{ marginBottom: 24, display: "flex", alignItems: "flex-end", justifyContent: "space-between", flexWrap: "wrap", gap: 12 }}>
                <div>
                    <h1 style={{ fontSize: 24, fontWeight: 700, color: "#1E293B", margin: 0 }}>Scénarios narratifs</h1>
                    <p style={{ color: "#64748B", fontSize: 14, marginTop: 4 }}>Lancer une simulation de phishing multi-jours, observer les réactions, débriefer en équipe.</p>
                </div>
                <Link href="/admin/scenarios/instances" style={{
                    display: "inline-flex", alignItems: "center", gap: 8,
                    padding: "10px 16px", background: "#FFFFFF", border: "1px solid #E2E8F0",
                    borderRadius: 8, color: "#334155", fontSize: 14, fontWeight: 500, textDecoration: "none",
                    transition: "all 0.2s",
                }}>
                    <Activity size={16} />
                    Instances en cours
                    {runningCount > 0 && (
                        <span style={{
                            background: "#3B82F6", color: "white",
                            fontSize: 11, fontWeight: 600, padding: "2px 8px",
                            borderRadius: 99,
                        }}>{runningCount}</span>
                    )}
                </Link>
            </div>

            {isLoading && <div style={{ color: "#64748B" }}>Chargement du catalogue…</div>}

            {!isLoading && catalog && catalog.length === 0 && (
                <div style={{
                    padding: 24, background: "#FFFFFF", border: "1px solid #E2E8F0",
                    borderRadius: 12, color: "#64748B",
                }}>
                    Aucun scénario chargé. Vérifiez que les fichiers VSF sont bien dans <code>Resources/Scenarios/</code>.
                </div>
            )}

            <div style={{
                display: "grid",
                gridTemplateColumns: "repeat(auto-fill, minmax(min(280px, 100%), 1fr))",
                gap: 16,
            }}>
                {catalog?.map(item => <CatalogCard key={item.id} item={item} />)}
            </div>
        </div>
    );
}

function CatalogCard({ item }: { item: ScenarioCatalogItem }) {
    const diffTone = DIFFICULTY_TONES[item.difficulty] ?? DIFFICULTY_TONES.easy;
    const categoryLabel = CATEGORY_LABELS[item.category] ?? item.category;

    return (
        <Link href={`/admin/scenarios/launch/${item.id}`} style={{
            background: "#FFFFFF",
            border: "1px solid #E2E8F0",
            borderRadius: 12,
            padding: 24,
            display: "flex",
            flexDirection: "column",
            gap: 14,
            textDecoration: "none",
            color: "inherit",
            boxShadow: "0 1px 2px rgba(0,0,0,0.04)",
            transition: "transform 0.2s, box-shadow 0.2s, border-color 0.2s",
        }}
            onMouseEnter={e => { (e.currentTarget as HTMLElement).style.borderColor = "#3B82F6"; (e.currentTarget as HTMLElement).style.transform = "translateY(-2px)"; (e.currentTarget as HTMLElement).style.boxShadow = "0 4px 12px rgba(0,0,0,0.06)"; }}
            onMouseLeave={e => { (e.currentTarget as HTMLElement).style.borderColor = "#E2E8F0"; (e.currentTarget as HTMLElement).style.transform = "translateY(0)"; (e.currentTarget as HTMLElement).style.boxShadow = "0 1px 2px rgba(0,0,0,0.04)"; }}
        >
            <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between" }}>
                <div style={{ width: 40, height: 40, borderRadius: 8, background: "rgba(59,130,246,0.10)", display: "flex", alignItems: "center", justifyContent: "center" }}>
                    <Theater size={20} color="#3B82F6" strokeWidth={1.75} />
                </div>
                <span style={{
                    fontSize: 12, fontWeight: 500,
                    padding: "4px 10px", borderRadius: 99,
                    background: diffTone.bg, color: diffTone.fg,
                }}>{diffTone.label}</span>
            </div>

            <div>
                <div style={{ fontSize: 11, color: "#64748B", textTransform: "uppercase", letterSpacing: "0.05em", fontWeight: 600 }}>{categoryLabel}</div>
                <h3 style={{ fontSize: 16, fontWeight: 600, color: "#1E293B", margin: "4px 0 0" }}>{item.name}</h3>
            </div>

            <p style={{ fontSize: 13, color: "#475569", lineHeight: 1.5, margin: 0 }}>{item.description}</p>

            <div style={{ display: "flex", gap: 16, fontSize: 12, color: "#64748B" }}>
                <span style={{ display: "inline-flex", alignItems: "center", gap: 4 }}><Clock size={13} strokeWidth={1.75} />{item.durationDays} j</span>
                <span style={{ display: "inline-flex", alignItems: "center", gap: 4 }}><Mail size={13} strokeWidth={1.75} />{item.emailCount} emails</span>
                <span style={{ display: "inline-flex", alignItems: "center", gap: 4 }}><ShieldAlert size={13} strokeWidth={1.75} />{item.attackStepCount} attaque(s)</span>
            </div>

            <div style={{ display: "flex", alignItems: "center", justifyContent: "flex-end", color: "#3B82F6", fontSize: 13, fontWeight: 500 }}>
                Lancer <ChevronRight size={14} />
            </div>
        </Link>
    );
}
