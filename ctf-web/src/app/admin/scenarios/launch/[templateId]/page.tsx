"use client";

import { useState, useMemo } from "react";
import { useParams, useRouter } from "next/navigation";
import Link from "next/link";
import {
    useScenarioCatalogDetail,
    useEligibleSenders,
    useLaunchScenario,
} from "@/lib/hooks/useScenarios";
import { apiFetch } from "@/lib/api";
import { useQuery } from "@tanstack/react-query";
import {
    ArrowLeft, ChevronRight, ChevronLeft, Check, Theater, Mail, Clock,
    Users, Eye, Rocket,
} from "lucide-react";

type DirectoryUser = { id: string; firstName: string; lastName: string; email: string };

const STEP_LABELS = ["Cible", "Expéditeur", "Personnalisation", "Confirmation"] as const;

export default function LaunchScenarioPage() {
    const params = useParams<{ templateId: string }>();
    const router = useRouter();
    const templateId = params.templateId;

    const { data: tpl, isLoading: tplLoading } = useScenarioCatalogDetail(templateId);
    const { data: senders, isLoading: sendersLoading } = useEligibleSenders();

    // Annuaire complet (cibles possibles) : on réutilise l'endpoint /api/users.
    const { data: allUsers, isLoading: usersLoading } = useQuery<DirectoryUser[]>({
        queryKey: ["users", "all"],
        queryFn: () => apiFetch<DirectoryUser[]>("/api/users"),
        staleTime: 60_000,
    });

    const [stepIndex, setStepIndex] = useState(0);
    const [targetUserId, setTargetUserId] = useState<string | null>(null);
    const [senderUserId, setSenderUserId] = useState<string | null>(null);
    const [mode, setMode] = useState<"normal" | "demo">("normal");
    const [overrides, setOverrides] = useState<Record<string, { subject?: string; bodyTemplate?: string }>>({});
    const [error, setError] = useState<string | null>(null);

    const { launch, isLoading: launching } = useLaunchScenario();

    const targetUser = useMemo(
        () => allUsers?.find(u => u.id === targetUserId) ?? null,
        [allUsers, targetUserId]);
    const senderUser = useMemo(
        () => senders?.find(u => u.userId === senderUserId) ?? null,
        [senders, senderUserId]);

    const canNext =
        (stepIndex === 0 && targetUserId) ||
        (stepIndex === 1 && senderUserId && senderUserId !== targetUserId) ||
        (stepIndex === 2) ||
        (stepIndex === 3);

    async function onLaunch() {
        if (!targetUserId || !senderUserId || !tpl) return;
        setError(null);
        const r = await launch({
            templateId: tpl.id,
            targetUserId,
            senderUserId,
            mode,
            stepOverrides: Object.entries(overrides).map(([stepId, ov]) => ({
                stepId, subject: ov.subject, bodyTemplate: ov.bodyTemplate,
            })),
        });
        if (r) {
            router.push("/admin/scenarios/instances");
        } else {
            setError("Échec du lancement. Vérifie le consentement de l'expéditeur et la disponibilité de la cible.");
        }
    }

    if (tplLoading) return <div style={{ padding: 24, color: "#64748B" }}>Chargement…</div>;
    if (!tpl) return <div style={{ padding: 24, color: "#EF4444" }}>Scénario introuvable.</div>;

    return (
        <div style={{ padding: "32px 24px", background: "#F8FAFC", minHeight: "100%" }}>
            {/* Header */}
            <Link href="/admin/scenarios" style={{ display: "inline-flex", alignItems: "center", gap: 6, color: "#64748B", fontSize: 13, textDecoration: "none", marginBottom: 12 }}>
                <ArrowLeft size={14} /> Retour catalogue
            </Link>
            <div style={{ background: "#FFFFFF", border: "1px solid #E2E8F0", borderRadius: 12, padding: 24, marginBottom: 24, display: "flex", alignItems: "center", gap: 16 }}>
                <div style={{ width: 48, height: 48, borderRadius: 8, background: "rgba(59,130,246,0.10)", display: "flex", alignItems: "center", justifyContent: "center" }}>
                    <Theater size={24} color="#3B82F6" strokeWidth={1.75} />
                </div>
                <div style={{ flex: 1 }}>
                    <h1 style={{ fontSize: 20, fontWeight: 700, color: "#1E293B", margin: 0 }}>{tpl.name}</h1>
                    <p style={{ fontSize: 13, color: "#64748B", margin: "4px 0 0" }}>{tpl.description}</p>
                </div>
                <div style={{ display: "flex", gap: 16, fontSize: 13, color: "#64748B" }}>
                    <span style={{ display: "inline-flex", alignItems: "center", gap: 4 }}><Clock size={14} />{tpl.durationDays} j</span>
                    <span style={{ display: "inline-flex", alignItems: "center", gap: 4 }}><Mail size={14} />{tpl.emailCount} emails</span>
                </div>
            </div>

            {/* Stepper */}
            <div style={{ display: "flex", gap: 8, marginBottom: 24 }}>
                {STEP_LABELS.map((label, i) => (
                    <div key={label} style={{
                        flex: 1, display: "flex", alignItems: "center", gap: 8,
                        padding: "10px 14px", borderRadius: 8,
                        background: i === stepIndex ? "#FFFFFF" : "transparent",
                        border: i === stepIndex ? "1px solid #3B82F6" : "1px solid transparent",
                    }}>
                        <div style={{
                            width: 24, height: 24, borderRadius: "50%",
                            background: i < stepIndex ? "#10B981" : i === stepIndex ? "#3B82F6" : "#E2E8F0",
                            color: i <= stepIndex ? "white" : "#64748B",
                            display: "flex", alignItems: "center", justifyContent: "center",
                            fontSize: 12, fontWeight: 600,
                        }}>{i < stepIndex ? <Check size={13} /> : i + 1}</div>
                        <span style={{ fontSize: 13, fontWeight: i === stepIndex ? 600 : 400, color: i === stepIndex ? "#1E293B" : "#64748B" }}>{label}</span>
                    </div>
                ))}
            </div>

            {/* Content */}
            <div style={{ background: "#FFFFFF", border: "1px solid #E2E8F0", borderRadius: 12, padding: 24, minHeight: 320 }}>
                {stepIndex === 0 && (
                    <StepTarget
                        users={allUsers ?? []}
                        loading={usersLoading}
                        selectedId={targetUserId}
                        onSelect={setTargetUserId}
                    />
                )}
                {stepIndex === 1 && (
                    <StepSender
                        senders={senders ?? []}
                        loading={sendersLoading}
                        targetUserId={targetUserId}
                        selectedId={senderUserId}
                        onSelect={setSenderUserId}
                    />
                )}
                {stepIndex === 2 && (
                    <StepCustomize
                        tpl={tpl}
                        overrides={overrides}
                        onChange={setOverrides}
                        mode={mode}
                        onModeChange={setMode}
                    />
                )}
                {stepIndex === 3 && (
                    <StepReview
                        tpl={tpl}
                        target={targetUser}
                        sender={senderUser}
                        mode={mode}
                        overrides={overrides}
                    />
                )}
            </div>

            {error && (
                <div style={{ marginTop: 12, padding: 12, background: "rgba(239,68,68,0.08)", border: "1px solid rgba(239,68,68,0.20)", borderRadius: 8, color: "#B91C1C", fontSize: 13 }}>
                    {error}
                </div>
            )}

            {/* Footer buttons */}
            <div style={{ marginTop: 16, display: "flex", justifyContent: "space-between" }}>
                <button
                    onClick={() => setStepIndex(i => Math.max(0, i - 1))}
                    disabled={stepIndex === 0}
                    style={{
                        display: "inline-flex", alignItems: "center", gap: 6,
                        padding: "10px 16px", border: "1px solid #E2E8F0", borderRadius: 8,
                        background: "#FFFFFF", color: stepIndex === 0 ? "#CBD5E1" : "#334155",
                        fontSize: 14, cursor: stepIndex === 0 ? "not-allowed" : "pointer",
                        transition: "all 0.2s",
                    }}
                ><ChevronLeft size={16} /> Précédent</button>

                {stepIndex < STEP_LABELS.length - 1 ? (
                    <button
                        onClick={() => setStepIndex(i => Math.min(STEP_LABELS.length - 1, i + 1))}
                        disabled={!canNext}
                        style={{
                            display: "inline-flex", alignItems: "center", gap: 6,
                            padding: "10px 16px", border: "none", borderRadius: 8,
                            background: canNext ? "#3B82F6" : "#CBD5E1", color: "white",
                            fontSize: 14, cursor: canNext ? "pointer" : "not-allowed",
                            transition: "all 0.2s", fontWeight: 500,
                        }}
                    >Suivant <ChevronRight size={16} /></button>
                ) : (
                    <button
                        onClick={onLaunch}
                        disabled={launching || !targetUserId || !senderUserId}
                        style={{
                            display: "inline-flex", alignItems: "center", gap: 6,
                            padding: "10px 20px", border: "none", borderRadius: 8,
                            background: "#10B981", color: "white",
                            fontSize: 14, cursor: launching ? "wait" : "pointer",
                            transition: "all 0.2s", fontWeight: 600,
                        }}
                    ><Rocket size={16} /> {launching ? "Lancement…" : "Lancer le scénario"}</button>
                )}
            </div>
        </div>
    );
}

// ── Étape 1 : cible ─────────────────────────────────────────────────────────

function StepTarget({ users, loading, selectedId, onSelect }: {
    users: DirectoryUser[]; loading: boolean;
    selectedId: string | null; onSelect: (id: string) => void;
}) {
    const [filter, setFilter] = useState("");
    const filtered = users.filter(u => {
        const q = filter.toLowerCase();
        return !q || u.email.toLowerCase().includes(q) ||
            `${u.firstName} ${u.lastName}`.toLowerCase().includes(q);
    });

    return (
        <div>
            <h2 style={{ fontSize: 16, fontWeight: 600, color: "#1E293B", margin: "0 0 6px" }}>Choix de la cible</h2>
            <p style={{ fontSize: 13, color: "#64748B", margin: "0 0 16px" }}>{"L'employé qui recevra les emails du scénario."}</p>
            <input
                type="search" placeholder="Rechercher (nom, email)…" value={filter} onChange={e => setFilter(e.target.value)}
                style={{ width: "100%", padding: "10px 14px", border: "1px solid #E2E8F0", borderRadius: 8, marginBottom: 12, fontSize: 14 }}
            />
            <div style={{ maxHeight: 320, overflowY: "auto", border: "1px solid #E2E8F0", borderRadius: 8 }}>
                {loading && <div style={{ padding: 16, color: "#64748B" }}>Chargement…</div>}
                {filtered.map(u => (
                    <label key={u.id} style={{
                        display: "flex", alignItems: "center", gap: 12,
                        padding: 12, borderBottom: "1px solid #F1F5F9", cursor: "pointer",
                        background: selectedId === u.id ? "#F1F5F9" : "transparent",
                        transition: "background 0.2s",
                    }}>
                        <input type="radio" checked={selectedId === u.id} onChange={() => onSelect(u.id)} />
                        <div style={{ flex: 1 }}>
                            <div style={{ fontSize: 14, color: "#1E293B" }}>{u.firstName} {u.lastName}</div>
                            <div style={{ fontSize: 12, color: "#64748B" }}>{u.email}</div>
                        </div>
                    </label>
                ))}
                {!loading && filtered.length === 0 && <div style={{ padding: 16, color: "#64748B" }}>Aucun résultat.</div>}
            </div>
        </div>
    );
}

// ── Étape 2 : expéditeur fictif consentant ──────────────────────────────────

function StepSender({ senders, loading, targetUserId, selectedId, onSelect }: {
    senders: { userId: string; firstName: string; lastName: string; email: string }[];
    loading: boolean; targetUserId: string | null;
    selectedId: string | null; onSelect: (id: string) => void;
}) {
    const eligible = senders.filter(s => s.userId !== targetUserId);
    return (
        <div>
            <h2 style={{ fontSize: 16, fontWeight: 600, color: "#1E293B", margin: "0 0 6px" }}>{"Choix de l'expéditeur fictif"}</h2>
            <p style={{ fontSize: 13, color: "#64748B", margin: "0 0 16px" }}>{"Seuls les employés qui ont donné leur "}<strong>{"consentement explicite"}</strong>{" dans leur profil apparaissent ici. Leur prénom / nom servira d'expéditeur affiché dans l'email simulé. Aucun email réel n'est envoyé."}</p>
            {loading && <div style={{ color: "#64748B" }}>Chargement…</div>}
            {!loading && eligible.length === 0 && (
                <div style={{ padding: 16, background: "rgba(245,158,11,0.08)", border: "1px solid rgba(245,158,11,0.20)", borderRadius: 8, color: "#92400E", fontSize: 13 }}>
                    {"Aucun employé n'a encore donné son consentement. Les employés peuvent activer cette option depuis leur page Paramètres > Consentement."}
                </div>
            )}
            <div style={{ maxHeight: 320, overflowY: "auto", border: "1px solid #E2E8F0", borderRadius: 8 }}>
                {eligible.map(s => (
                    <label key={s.userId} style={{
                        display: "flex", alignItems: "center", gap: 12,
                        padding: 12, borderBottom: "1px solid #F1F5F9", cursor: "pointer",
                        background: selectedId === s.userId ? "#F1F5F9" : "transparent",
                    }}>
                        <input type="radio" checked={selectedId === s.userId} onChange={() => onSelect(s.userId)} />
                        <div style={{ flex: 1 }}>
                            <div style={{ fontSize: 14, color: "#1E293B" }}>{s.firstName} {s.lastName}</div>
                            <div style={{ fontSize: 12, color: "#64748B" }}>{s.email}</div>
                        </div>
                    </label>
                ))}
            </div>
        </div>
    );
}

// ── Étape 3 : personnalisation + mode ───────────────────────────────────────

function StepCustomize({ tpl, overrides, onChange, mode, onModeChange }: {
    tpl: { timeline?: { stepId: string; subject: string; bodyTemplate: string; isAttackStep: boolean; stepOrder: number }[] };
    overrides: Record<string, { subject?: string; bodyTemplate?: string }>;
    onChange: (next: Record<string, { subject?: string; bodyTemplate?: string }>) => void;
    mode: "normal" | "demo";
    onModeChange: (m: "normal" | "demo") => void;
}) {
    const steps = (tpl.timeline ?? []).slice().sort((a, b) => a.stepOrder - b.stepOrder);
    const [activeStep, setActiveStep] = useState<string | null>(steps[0]?.stepId ?? null);

    function setOverride(stepId: string, field: "subject" | "bodyTemplate", value: string) {
        onChange({
            ...overrides,
            [stepId]: { ...overrides[stepId], [field]: value },
        });
    }

    const current = steps.find(s => s.stepId === activeStep);
    const ov = current ? overrides[current.stepId] : undefined;

    return (
        <div>
            <h2 style={{ fontSize: 16, fontWeight: 600, color: "#1E293B", margin: "0 0 6px" }}>Personnalisation & mode</h2>
            <p style={{ fontSize: 13, color: "#64748B", margin: "0 0 16px" }}>Tu peux laisser le scénario tel quel ou retoucher sujet / corps de chaque étape. Les variables Scriban (<code>{"{{recipient.firstName}}"}</code> etc.) sont rendues automatiquement.</p>

            <div style={{ marginBottom: 16, padding: 16, background: "#F8FAFC", borderRadius: 8, border: "1px solid #E2E8F0" }}>
                <div style={{ fontSize: 13, fontWeight: 600, color: "#1E293B", marginBottom: 8 }}>{"Mode d'exécution"}</div>
                <div style={{ display: "flex", gap: 8 }}>
                    <button onClick={() => onModeChange("normal")} style={{ flex: 1, padding: 10, border: mode === "normal" ? "2px solid #3B82F6" : "1px solid #E2E8F0", borderRadius: 8, background: "#FFFFFF", cursor: "pointer", fontSize: 13, fontWeight: mode === "normal" ? 600 : 400, color: "#1E293B" }}>
                        <div>Normal</div>
                        <div style={{ fontSize: 11, color: "#64748B" }}>1 jour = 1 jour réel</div>
                    </button>
                    <button onClick={() => onModeChange("demo")} style={{ flex: 1, padding: 10, border: mode === "demo" ? "2px solid #3B82F6" : "1px solid #E2E8F0", borderRadius: 8, background: "#FFFFFF", cursor: "pointer", fontSize: 13, fontWeight: mode === "demo" ? 600 : 400, color: "#1E293B" }}>
                        <div>Démo (1 j = 1 min)</div>
                        <div style={{ fontSize: 11, color: "#64748B" }}>Compression 1440x</div>
                    </button>
                </div>
            </div>

            <div style={{ display: "grid", gridTemplateColumns: "200px 1fr", gap: 16 }}>
                <div style={{ display: "flex", flexDirection: "column", gap: 4 }}>
                    {steps.map(s => (
                        <button key={s.stepId} onClick={() => setActiveStep(s.stepId)} style={{
                            textAlign: "left", padding: "10px 12px", borderRadius: 8,
                            border: activeStep === s.stepId ? "2px solid #3B82F6" : "1px solid #E2E8F0",
                            background: activeStep === s.stepId ? "#F1F5F9" : "#FFFFFF",
                            cursor: "pointer", fontSize: 13,
                            color: "#1E293B",
                        }}>
                            <div style={{ fontWeight: 600 }}>Étape {s.stepOrder}</div>
                            <div style={{ fontSize: 11, color: s.isAttackStep ? "#EF4444" : "#64748B" }}>{s.isAttackStep ? "⚠ Attaque" : "Anodine"}</div>
                        </button>
                    ))}
                </div>
                <div>
                    {current && (
                        <>
                            <label style={{ display: "block", marginBottom: 12 }}>
                                <span style={{ fontSize: 12, fontWeight: 600, color: "#64748B", display: "block", marginBottom: 4 }}>SUJET</span>
                                <input
                                    type="text"
                                    defaultValue={current.subject}
                                    value={ov?.subject ?? current.subject}
                                    onChange={e => setOverride(current.stepId, "subject", e.target.value)}
                                    style={{ width: "100%", padding: "8px 12px", border: "1px solid #E2E8F0", borderRadius: 6, fontSize: 13 }}
                                />
                            </label>
                            <label>
                                <span style={{ fontSize: 12, fontWeight: 600, color: "#64748B", display: "block", marginBottom: 4 }}>CORPS HTML</span>
                                <textarea
                                    rows={12}
                                    value={ov?.bodyTemplate ?? current.bodyTemplate}
                                    onChange={e => setOverride(current.stepId, "bodyTemplate", e.target.value)}
                                    style={{ width: "100%", padding: 12, border: "1px solid #E2E8F0", borderRadius: 6, fontSize: 13, fontFamily: "monospace", resize: "vertical" }}
                                />
                            </label>
                        </>
                    )}
                </div>
            </div>
        </div>
    );
}

// ── Étape 4 : récapitulatif ─────────────────────────────────────────────────

function StepReview({ tpl, target, sender, mode, overrides }: {
    tpl: { name: string; durationDays: number; emailCount: number };
    target: DirectoryUser | null;
    sender: { firstName: string; lastName: string; email: string } | null;
    mode: string;
    overrides: Record<string, { subject?: string; bodyTemplate?: string }>;
}) {
    const overrideCount = Object.keys(overrides).length;
    return (
        <div>
            <h2 style={{ fontSize: 16, fontWeight: 600, color: "#1E293B", margin: "0 0 6px" }}>Récapitulatif</h2>
            <p style={{ fontSize: 13, color: "#64748B", margin: "0 0 16px" }}>Vérifie avant de lancer. Le scénario démarre immédiatement après confirmation.</p>

            <div style={{ display: "grid", gap: 12 }}>
                <ReviewRow label="Scénario" value={`${tpl.name} (${tpl.emailCount} emails sur ${tpl.durationDays} j)`} />
                <ReviewRow label="Cible" value={target ? `${target.firstName} ${target.lastName} — ${target.email}` : "—"} icon={<Users size={16} />} />
                <ReviewRow label="Expéditeur fictif" value={sender ? `${sender.firstName} ${sender.lastName} (consentement actif)` : "—"} />
                <ReviewRow label="Mode" value={mode === "demo" ? "Démo (1 jour = 1 minute)" : "Normal"} icon={<Eye size={16} />} />
                <ReviewRow label="Personnalisations" value={overrideCount > 0 ? `${overrideCount} étape(s) modifiée(s)` : "Aucune (template d'origine)"} />
            </div>
        </div>
    );
}

function ReviewRow({ label, value, icon }: { label: string; value: string; icon?: React.ReactNode }) {
    return (
        <div style={{ display: "flex", justifyContent: "space-between", padding: "12px 16px", background: "#F8FAFC", borderRadius: 8, border: "1px solid #E2E8F0" }}>
            <span style={{ fontSize: 12, fontWeight: 600, color: "#64748B", display: "inline-flex", alignItems: "center", gap: 6 }}>{icon}{label}</span>
            <span style={{ fontSize: 13, color: "#1E293B", fontWeight: 500 }}>{value}</span>
        </div>
    );
}
