"use client";

import { useState, useMemo, useRef } from "react";
import { useParams, useRouter } from "next/navigation";
import Link from "next/link";
import { useIsMobile } from "@/hooks/useMediaQuery";
import {
    useScenarioCatalogDetail,
    useEmployeesWithConsent,
    useLaunchScenario,
} from "@/lib/hooks/useScenarios";
import {
    ArrowLeft, ChevronRight, ChevronLeft, Check, Theater, Mail, Clock,
    Users, Eye, Rocket, CheckCircle2, XCircle, X,
} from "lucide-react";
import type { EmployeeWithConsent } from "@/lib/types/scenarios";

const STEP_LABELS = ["Cible et expéditeur", "Personnalisation", "Confirmation"] as const;
const MAX_CHIPS_VISIBLE = 10;

export default function LaunchScenarioPage() {
    const params = useParams<{ templateId: string }>();
    const router = useRouter();
    const templateId = params.templateId;

    const { data: tpl, isLoading: tplLoading } = useScenarioCatalogDetail(templateId);
    const { data: employees, isLoading: employeesLoading } = useEmployeesWithConsent();
    const isMobile = useIsMobile();

    const [stepIndex, setStepIndex] = useState(0);
    const [targetUserIds, setTargetUserIds] = useState<string[]>([]);
    const [senderUserId, setSenderUserId] = useState<string | null>(null);
    const [mode, setMode] = useState<"normal" | "demo">("normal");
    const [overrides, setOverrides] = useState<Record<string, { subject?: string; bodyTemplate?: string }>>({});
    const [error, setError] = useState<string | null>(null);
    const targetsListRef = useRef<HTMLDivElement | null>(null);

    const { launch, isLoading: launching } = useLaunchScenario();

    const employeesById = useMemo(() => {
        const map = new Map<string, EmployeeWithConsent>();
        for (const u of employees ?? []) map.set(u.id, u);
        return map;
    }, [employees]);

    const targetUsers = useMemo(
        () => targetUserIds.map(id => employeesById.get(id)).filter((u): u is EmployeeWithConsent => !!u),
        [targetUserIds, employeesById]);
    const senderUser = useMemo(
        () => (senderUserId ? employeesById.get(senderUserId) ?? null : null),
        [senderUserId, employeesById]);

    const canNext =
        (stepIndex === 0 && targetUserIds.length > 0 && senderUserId &&
            !targetUserIds.includes(senderUserId)) ||
        (stepIndex === 1) ||
        (stepIndex === 2);

    function toggleTarget(id: string) {
        setTargetUserIds(prev =>
            prev.includes(id) ? prev.filter(x => x !== id) : [...prev, id]);
    }
    function removeTarget(id: string) {
        setTargetUserIds(prev => prev.filter(x => x !== id));
    }
    function scrollToTargetsList() {
        targetsListRef.current?.scrollIntoView({ behavior: "smooth", block: "center" });
    }

    async function onLaunch() {
        if (targetUserIds.length === 0 || !senderUserId || !tpl) return;
        setError(null);
        // V1 multi-target : on lance une instance par destinataire en série.
        // En cas d'échec, on s'arrête pour que l'admin puisse corriger sans
        // découvrir une cinquantaine d'instances cassées dans la table.
        const stepOverrides = Object.entries(overrides).map(([stepId, ov]) => ({
            stepId, subject: ov.subject, bodyTemplate: ov.bodyTemplate,
        }));
        const failures: string[] = [];
        for (const targetId of targetUserIds) {
            const r = await launch({
                templateId: tpl.id,
                targetUserId: targetId,
                senderUserId,
                mode,
                stepOverrides,
            });
            if (!r) {
                const u = employeesById.get(targetId);
                failures.push(u ? `${u.firstName} ${u.lastName}` : targetId);
                break;
            }
        }
        if (failures.length === 0) {
            router.push("/admin/scenarios/instances");
        } else {
            setError(`Échec du lancement pour ${failures.join(", ")}. Vérifie le consentement de l'expéditeur et la disponibilité des cibles.`);
        }
    }

    if (tplLoading) return <div style={{ padding: 24, color: "#64748B" }}>Chargement…</div>;
    if (!tpl) return <div style={{ padding: 24, color: "#EF4444" }}>Scénario introuvable.</div>;

    return (
        <div style={{ padding: isMobile ? "20px var(--page-x)" : "32px 24px", background: "#F8FAFC", minHeight: "100%" }}>
            {/* Header */}
            <Link href="/admin/scenarios" style={{ display: "inline-flex", alignItems: "center", gap: 6, color: "#64748B", fontSize: 13, textDecoration: "none", marginBottom: 12 }}>
                <ArrowLeft size={14} /> Retour catalogue
            </Link>
            <div style={{ background: "#FFFFFF", border: "1px solid #E2E8F0", borderRadius: 12, padding: isMobile ? 16 : 24, marginBottom: isMobile ? 16 : 24, display: "flex", alignItems: isMobile ? "flex-start" : "center", gap: 16, flexWrap: "wrap" }}>
                <div style={{ width: 48, height: 48, borderRadius: 8, background: "rgba(59,130,246,0.10)", display: "flex", alignItems: "center", justifyContent: "center", flexShrink: 0 }}>
                    <Theater size={24} color="#3B82F6" strokeWidth={1.75} />
                </div>
                <div style={{ flex: 1, minWidth: 0 }}>
                    <h1 style={{ fontSize: isMobile ? 17 : 20, fontWeight: 700, color: "#1E293B", margin: 0 }}>{tpl.name}</h1>
                    <p style={{ fontSize: 13, color: "#64748B", margin: "4px 0 0" }}>{tpl.description}</p>
                </div>
                <div style={{ display: "flex", gap: 16, fontSize: 13, color: "#64748B" }}>
                    <span style={{ display: "inline-flex", alignItems: "center", gap: 4 }}><Clock size={14} />{tpl.durationDays} j</span>
                    <span style={{ display: "inline-flex", alignItems: "center", gap: 4 }}><Mail size={14} />{tpl.emailCount} emails</span>
                </div>
            </div>

            {/* Stepper : compact "Étape n/N" + barre de progression sur mobile */}
            {isMobile ? (
                <div style={{ marginBottom: 16 }}>
                    <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", marginBottom: 8 }}>
                        <span style={{ fontSize: 13, fontWeight: 600, color: "#1E293B" }}>
                            Étape {stepIndex + 1}/{STEP_LABELS.length} — {STEP_LABELS[stepIndex]}
                        </span>
                    </div>
                    <div style={{ height: 6, background: "#E2E8F0", borderRadius: 999, overflow: "hidden" }}>
                        <div style={{
                            height: "100%", width: `${((stepIndex + 1) / STEP_LABELS.length) * 100}%`,
                            background: "#3B82F6", borderRadius: 999, transition: "width 0.2s",
                        }} />
                    </div>
                </div>
            ) : (
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
            )}

            {/* Content */}
            <div style={{ background: "#FFFFFF", border: "1px solid #E2E8F0", borderRadius: 12, padding: isMobile ? 16 : 24, minHeight: 320 }}>
                {stepIndex === 0 && (
                    <StepTargetAndSender
                        employees={employees ?? []}
                        loading={employeesLoading}
                        targetUserIds={targetUserIds}
                        senderUserId={senderUserId}
                        onToggleTarget={toggleTarget}
                        onRemoveTarget={removeTarget}
                        onSelectSender={setSenderUserId}
                        onScrollToTargets={scrollToTargetsList}
                        targetsListRef={targetsListRef}
                        isMobile={isMobile}
                    />
                )}
                {stepIndex === 1 && (
                    <StepCustomize
                        tpl={tpl}
                        overrides={overrides}
                        onChange={setOverrides}
                        mode={mode}
                        onModeChange={setMode}
                        isMobile={isMobile}
                    />
                )}
                {stepIndex === 2 && (
                    <StepReview
                        tpl={tpl}
                        targets={targetUsers}
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

            {/* Footer buttons : empilés et pleine largeur sur mobile */}
            <div style={{
                marginTop: 16, display: "flex", gap: 8,
                flexDirection: isMobile ? "column-reverse" : "row",
                justifyContent: "space-between",
            }}>
                <button
                    onClick={() => setStepIndex(i => Math.max(0, i - 1))}
                    disabled={stepIndex === 0}
                    style={{
                        display: "inline-flex", alignItems: "center", justifyContent: "center", gap: 6,
                        padding: "10px 16px", border: "1px solid #E2E8F0", borderRadius: 8,
                        background: "#FFFFFF", color: stepIndex === 0 ? "#CBD5E1" : "#334155",
                        fontSize: 14, cursor: stepIndex === 0 ? "not-allowed" : "pointer",
                        transition: "all 0.2s", width: isMobile ? "100%" : "auto",
                    }}
                ><ChevronLeft size={16} /> Précédent</button>

                {stepIndex < STEP_LABELS.length - 1 ? (
                    <button
                        onClick={() => setStepIndex(i => Math.min(STEP_LABELS.length - 1, i + 1))}
                        disabled={!canNext}
                        style={{
                            display: "inline-flex", alignItems: "center", justifyContent: "center", gap: 6,
                            padding: "10px 16px", border: "none", borderRadius: 8,
                            background: canNext ? "#3B82F6" : "#CBD5E1", color: "white",
                            fontSize: 14, cursor: canNext ? "pointer" : "not-allowed",
                            transition: "all 0.2s", fontWeight: 500, width: isMobile ? "100%" : "auto",
                        }}
                    >Suivant <ChevronRight size={16} /></button>
                ) : (
                    <button
                        onClick={onLaunch}
                        disabled={launching || targetUserIds.length === 0 || !senderUserId}
                        style={{
                            display: "inline-flex", alignItems: "center", justifyContent: "center", gap: 6,
                            padding: "10px 20px", border: "none", borderRadius: 8,
                            background: "#10B981", color: "white",
                            fontSize: 14, cursor: launching ? "wait" : "pointer",
                            transition: "all 0.2s", fontWeight: 600, width: isMobile ? "100%" : "auto",
                        }}
                    ><Rocket size={16} /> {launching ? "Lancement…" : `Lancer (${targetUserIds.length})`}</button>
                )}
            </div>
        </div>
    );
}

// ── Étape 1 : cible et expéditeur (multi-target + sender single-select) ────

function StepTargetAndSender({
    employees, loading,
    targetUserIds, senderUserId,
    onToggleTarget, onRemoveTarget, onSelectSender,
    onScrollToTargets, targetsListRef, isMobile,
}: {
    employees: EmployeeWithConsent[]; loading: boolean;
    targetUserIds: string[]; senderUserId: string | null;
    onToggleTarget: (id: string) => void;
    onRemoveTarget: (id: string) => void;
    onSelectSender: (id: string) => void;
    onScrollToTargets: () => void;
    targetsListRef: React.MutableRefObject<HTMLDivElement | null>;
    isMobile: boolean;
}) {
    const [filterTarget, setFilterTarget] = useState("");
    const [filterSender, setFilterSender] = useState("");

    const targetCandidates = useMemo(() => {
        const q = filterTarget.toLowerCase();
        return employees
            .filter(u => u.id !== senderUserId)
            .filter(u => !q || u.email.toLowerCase().includes(q)
                || `${u.firstName} ${u.lastName}`.toLowerCase().includes(q));
    }, [employees, filterTarget, senderUserId]);

    const senderCandidates = useMemo(() => {
        const q = filterSender.toLowerCase();
        return employees
            .filter(u => !targetUserIds.includes(u.id))
            .filter(u => !q || u.email.toLowerCase().includes(q)
                || `${u.firstName} ${u.lastName}`.toLowerCase().includes(q));
    }, [employees, filterSender, targetUserIds]);

    const selectedTargets = useMemo(
        () => targetUserIds.map(id => employees.find(e => e.id === id))
            .filter((u): u is EmployeeWithConsent => !!u),
        [targetUserIds, employees]);

    const visibleChips = selectedTargets.slice(0, MAX_CHIPS_VISIBLE);
    const overflowCount = Math.max(0, selectedTargets.length - MAX_CHIPS_VISIBLE);

    return (
        <div>
            <h2 style={{ fontSize: 16, fontWeight: 600, color: "#1E293B", margin: "0 0 6px" }}>Cible et expéditeur</h2>
            <p style={{ fontSize: 13, color: "#64748B", margin: "0 0 16px" }}>{"Choisis les destinataires du scénario et l'employé qui prêtera son identité comme expéditeur fictif."}</p>

            {/* Récap des destinataires sélectionnés */}
            <RecipientsRecap
                selected={selectedTargets}
                visibleChips={visibleChips}
                overflowCount={overflowCount}
                onRemove={onRemoveTarget}
                onScrollToList={onScrollToTargets}
            />

            <div style={{ display: "grid", gridTemplateColumns: isMobile ? "1fr" : "1fr 1fr", gap: 16, marginTop: 16 }}>
                {/* Colonne destinataires (multi-select) */}
                <div ref={targetsListRef}>
                    <h3 style={{ fontSize: 14, fontWeight: 600, color: "#1E293B", margin: "0 0 8px", display: "inline-flex", alignItems: "center", gap: 6 }}>
                        <Users size={15} /> Destinataires
                    </h3>
                    <p style={{ fontSize: 12, color: "#64748B", margin: "0 0 8px" }}>{"Sélectionne un ou plusieurs employés (multi-sélection). Le consentement n'est pas requis pour être destinataire."}</p>
                    <input
                        type="search" placeholder="Rechercher (nom, email)…" value={filterTarget} onChange={e => setFilterTarget(e.target.value)}
                        style={{ width: "100%", padding: "10px 14px", border: "1px solid #E2E8F0", borderRadius: 8, marginBottom: 8, fontSize: 14 }}
                    />
                    <div style={{ maxHeight: 360, overflowY: "auto", border: "1px solid #E2E8F0", borderRadius: 8 }}>
                        {loading && <div style={{ padding: 16, color: "#64748B" }}>Chargement…</div>}
                        {!loading && targetCandidates.length === 0 && <div style={{ padding: 16, color: "#64748B" }}>Aucun résultat.</div>}
                        {targetCandidates.map(u => {
                            const checked = targetUserIds.includes(u.id);
                            return (
                                <label key={u.id} style={{
                                    display: "flex", alignItems: "center", gap: 10,
                                    padding: 10, borderBottom: "1px solid #F1F5F9", cursor: "pointer",
                                    background: checked ? "rgba(3,121,113,0.08)" : "transparent",
                                    transition: "background 0.2s",
                                }}>
                                    <input type="checkbox" checked={checked} onChange={() => onToggleTarget(u.id)} />
                                    <ConsentBadge consents={u.consentsToBeFictionalSender} side="recipient" />
                                    <div style={{ flex: 1, minWidth: 0 }}>
                                        <div style={{ fontSize: 13, color: "#1E293B", whiteSpace: "nowrap", overflow: "hidden", textOverflow: "ellipsis" }}>{u.firstName} {u.lastName}</div>
                                        <div style={{ fontSize: 11, color: "#64748B", whiteSpace: "nowrap", overflow: "hidden", textOverflow: "ellipsis" }}>{u.email}</div>
                                    </div>
                                </label>
                            );
                        })}
                    </div>
                </div>

                {/* Colonne expéditeur (single-select, non-consentants désactivés) */}
                <div>
                    <h3 style={{ fontSize: 14, fontWeight: 600, color: "#1E293B", margin: "0 0 8px", display: "inline-flex", alignItems: "center", gap: 6 }}>
                        <Mail size={15} /> Expéditeur fictif
                    </h3>
                    <p style={{ fontSize: 12, color: "#64748B", margin: "0 0 8px" }}>{"Seuls les employés ayant donné leur consentement peuvent être sélectionnés. Les autres restent visibles mais sont désactivés."}</p>
                    <input
                        type="search" placeholder="Rechercher (nom, email)…" value={filterSender} onChange={e => setFilterSender(e.target.value)}
                        style={{ width: "100%", padding: "10px 14px", border: "1px solid #E2E8F0", borderRadius: 8, marginBottom: 8, fontSize: 14 }}
                    />
                    <div style={{ maxHeight: 360, overflowY: "auto", border: "1px solid #E2E8F0", borderRadius: 8 }}>
                        {loading && <div style={{ padding: 16, color: "#64748B" }}>Chargement…</div>}
                        {!loading && senderCandidates.length === 0 && <div style={{ padding: 16, color: "#64748B" }}>Aucun résultat.</div>}
                        {senderCandidates.map(u => {
                            const consents = u.consentsToBeFictionalSender;
                            const selected = senderUserId === u.id;
                            const tooltip = consents
                                ? undefined
                                : "Cet employé n'a pas consenti à être expéditeur fictif. Active l'option dans sa fiche pour pouvoir le sélectionner.";
                            return (
                                <label key={u.id} title={tooltip} style={{
                                    display: "flex", alignItems: "center", gap: 10,
                                    padding: 10, borderBottom: "1px solid #F1F5F9",
                                    cursor: consents ? "pointer" : "not-allowed",
                                    opacity: consents ? 1 : 0.5,
                                    background: selected ? "rgba(3,121,113,0.08)" : "transparent",
                                    transition: "background 0.2s",
                                }}>
                                    <input
                                        type="radio" name="sender"
                                        checked={selected}
                                        disabled={!consents}
                                        onChange={() => consents && onSelectSender(u.id)}
                                    />
                                    <ConsentBadge consents={consents} side="sender" />
                                    <div style={{ flex: 1, minWidth: 0 }}>
                                        <div style={{ fontSize: 13, color: "#1E293B", whiteSpace: "nowrap", overflow: "hidden", textOverflow: "ellipsis" }}>{u.firstName} {u.lastName}</div>
                                        <div style={{ fontSize: 11, color: "#64748B", whiteSpace: "nowrap", overflow: "hidden", textOverflow: "ellipsis" }}>{u.email}</div>
                                    </div>
                                </label>
                            );
                        })}
                    </div>
                </div>
            </div>
        </div>
    );
}

function ConsentBadge({ consents, side }: { consents: boolean; side: "sender" | "recipient" }) {
    const label = consents ? "Consent: Yes" : "Consent: No";
    const recipientTooltip = !consents
        ? "Cet employé n'a pas consenti à être expéditeur fictif (il peut quand même être destinataire d'un scénario)."
        : undefined;
    if (consents) {
        return <CheckCircle2 size={16} color="#10b981" strokeWidth={1.75} aria-label={label} />;
    }
    return (
        <span title={side === "recipient" ? recipientTooltip : undefined} style={{ display: "inline-flex" }}>
            <XCircle size={16} color="#ef4444" strokeWidth={1.75} aria-label={label} />
        </span>
    );
}

function RecipientsRecap({
    selected, visibleChips, overflowCount, onRemove, onScrollToList,
}: {
    selected: EmployeeWithConsent[];
    visibleChips: EmployeeWithConsent[];
    overflowCount: number;
    onRemove: (id: string) => void;
    onScrollToList: () => void;
}) {
    if (selected.length === 0) {
        return (
            <div style={{
                padding: "12px 16px",
                background: "#F1F5F9",
                border: "1px solid #E2E8F0",
                borderRadius: 8,
                color: "#64748B",
                fontSize: 13,
            }}>
                Aucun destinataire sélectionné
            </div>
        );
    }
    return (
        <div style={{
            background: "var(--surface-2)",
            borderLeft: "4px solid var(--accent)",
            borderRadius: 8,
            padding: 16,
            color: "#FFFFFF",
        }}>
            <div style={{ fontSize: 13, fontWeight: 700, marginBottom: 10 }}>
                Destinataires sélectionnés ({selected.length})
            </div>
            <div style={{ display: "flex", flexWrap: "wrap", gap: 8 }}>
                {visibleChips.map(u => (
                    <span key={u.id} style={{
                        display: "inline-flex", alignItems: "center", gap: 6,
                        padding: "4px 12px",
                        background: "var(--accent-hover)",
                        color: "#FFFFFF",
                        borderRadius: 999,
                        fontSize: 12,
                        fontWeight: 500,
                        transition: "background 0.2s",
                    }}
                        onMouseEnter={e => (e.currentTarget as HTMLElement).style.background = "var(--accent)"}
                        onMouseLeave={e => (e.currentTarget as HTMLElement).style.background = "var(--accent-hover)"}
                    >
                        {u.firstName} {u.lastName}
                        <button
                            type="button"
                            aria-label={`Retirer ${u.firstName} ${u.lastName}`}
                            onClick={() => onRemove(u.id)}
                            style={{
                                display: "inline-flex", alignItems: "center", justifyContent: "center",
                                background: "transparent", border: "none", padding: 0,
                                color: "#FFFFFF", cursor: "pointer",
                            }}
                        >
                            <X size={14} strokeWidth={2.25} />
                        </button>
                    </span>
                ))}
                {overflowCount > 0 && (
                    <button
                        type="button"
                        onClick={onScrollToList}
                        style={{
                            padding: "4px 12px",
                            background: "rgba(255,255,255,0.10)",
                            color: "#FFFFFF",
                            border: "1px solid rgba(255,255,255,0.20)",
                            borderRadius: 999,
                            fontSize: 12,
                            fontWeight: 500,
                            cursor: "pointer",
                            transition: "background 0.2s",
                        }}
                        onMouseEnter={e => (e.currentTarget as HTMLElement).style.background = "rgba(255,255,255,0.20)"}
                        onMouseLeave={e => (e.currentTarget as HTMLElement).style.background = "rgba(255,255,255,0.10)"}
                    >
                        +{overflowCount} autres
                    </button>
                )}
            </div>
        </div>
    );
}

// ── Étape 2 : personnalisation + mode ───────────────────────────────────────

function StepCustomize({ tpl, overrides, onChange, mode, onModeChange, isMobile }: {
    tpl: { timeline?: { stepId: string; subject: string; bodyTemplate: string; isAttackStep: boolean; stepOrder: number }[] };
    overrides: Record<string, { subject?: string; bodyTemplate?: string }>;
    onChange: (next: Record<string, { subject?: string; bodyTemplate?: string }>) => void;
    mode: "normal" | "demo";
    onModeChange: (m: "normal" | "demo") => void;
    isMobile: boolean;
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

            <div style={{ display: "grid", gridTemplateColumns: isMobile ? "1fr" : "200px 1fr", gap: 16 }}>
                <div style={{
                    display: "flex",
                    flexDirection: isMobile ? "row" : "column",
                    gap: isMobile ? 8 : 4,
                    overflowX: isMobile ? "auto" : "visible",
                    paddingBottom: isMobile ? 4 : 0,
                }}>
                    {steps.map(s => (
                        <button key={`${s.stepOrder}-${s.stepId}`} onClick={() => setActiveStep(s.stepId)} style={{
                            textAlign: "left", padding: "10px 12px", borderRadius: 8,
                            border: activeStep === s.stepId ? "2px solid #3B82F6" : "1px solid #E2E8F0",
                            background: activeStep === s.stepId ? "#F1F5F9" : "#FFFFFF",
                            cursor: "pointer", fontSize: 13,
                            color: "#1E293B", flexShrink: isMobile ? 0 : undefined,
                            whiteSpace: isMobile ? "nowrap" : undefined,
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
                                    value={ov?.subject ?? current.subject ?? ""}
                                    onChange={e => setOverride(current.stepId, "subject", e.target.value)}
                                    style={{ width: "100%", padding: "8px 12px", border: "1px solid #E2E8F0", borderRadius: 6, fontSize: 13 }}
                                />
                            </label>
                            <label>
                                <span style={{ fontSize: 12, fontWeight: 600, color: "#64748B", display: "block", marginBottom: 4 }}>CORPS HTML</span>
                                <textarea
                                    rows={12}
                                    value={ov?.bodyTemplate ?? current.bodyTemplate ?? ""}
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

// ── Étape 3 : récapitulatif ─────────────────────────────────────────────────

function StepReview({ tpl, targets, sender, mode, overrides }: {
    tpl: { name: string; durationDays: number; emailCount: number };
    targets: EmployeeWithConsent[];
    sender: EmployeeWithConsent | null;
    mode: string;
    overrides: Record<string, { subject?: string; bodyTemplate?: string }>;
}) {
    const overrideCount = Object.keys(overrides).length;
    const targetsLabel = targets.length === 0
        ? "—"
        : targets.length === 1
            ? `${targets[0].firstName} ${targets[0].lastName} — ${targets[0].email}`
            : `${targets.length} destinataires : ${targets.slice(0, 3).map(t => `${t.firstName} ${t.lastName}`).join(", ")}${targets.length > 3 ? `, +${targets.length - 3} autres` : ""}`;

    return (
        <div>
            <h2 style={{ fontSize: 16, fontWeight: 600, color: "#1E293B", margin: "0 0 6px" }}>Récapitulatif</h2>
            <p style={{ fontSize: 13, color: "#64748B", margin: "0 0 16px" }}>{`Vérifie avant de lancer. ${targets.length > 1 ? `${targets.length} instances seront créées (une par destinataire).` : "Le scénario démarre immédiatement après confirmation."}`}</p>

            <div style={{ display: "grid", gap: 12 }}>
                <ReviewRow label="Scénario" value={`${tpl.name} (${tpl.emailCount} emails sur ${tpl.durationDays} j)`} />
                <ReviewRow label="Destinataires" value={targetsLabel} icon={<Users size={16} />} />
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
