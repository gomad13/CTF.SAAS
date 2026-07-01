"use client";

import Image from "next/image";
import { notFound, useParams } from "next/navigation";
import { useMemo, useState } from "react";
import { useIsMobile } from "@/hooks/useMediaQuery";

type ActionId =
    | "wire_now"
    | "reply_mail"
    | "ignore"
    | "check_domain"
    | "ask_proof"
    | "call_internal"
    | "report_security";

type ActionDef = {
    id: ActionId;
    label: string;
    short: string;
    points: number;
    type: string;
    consequenceTitle: string;
    consequenceText: string;
    impactLines: string[];
};

type Scenario = {
    id: string; // moduleId
    title: string;
    urgencyLabel: string;
    contextTitle: string;
    fromLabel: string;
    subjectLabel: string;
    quoteHtml: React.ReactNode;
    rightHintTitle: string;
    rightHintHtml: React.ReactNode;
    imageSrc: string;
    actions: ActionDef[];
};

const SCENARIOS: Record<string, Scenario> = {
    "arnaque-au-president": {
        id: "arnaque-au-president",
        title: "Exemple immersif : Arnaque au Pr�sident",
        urgencyLabel: "URGENCE CRITIQUE",
        contextTitle: "Le Contexte (18h47)",
        fromLabel: "PDG",
        subjectLabel: "Confidentiel / Urgent",
        quoteHtml: (
            <>
                �Je suis en d�placement avec un partenaire strat�gique.
                <br />
                J�ai besoin d�un virement confidentiel de <b>48 500�</b>.
                <br />
                Ne contacte personne. C�est urgent.�
            </>
        ),
        rightHintTitle: "Indice visuel",
        rightHintHtml: (
            <>
                L�attaquant utilise souvent : <b>urgence</b>, <b>confidentialit�</b>, et une{" "}
                <b>autorit�</b> (�PDG�, �direction�) pour court-circuiter la proc�dure.
            </>
        ),
        imageSrc: "/modules/arnaque-au-president.jpg",
        actions: [
            {
                id: "wire_now",
                label: "Faire le virement imm�diatement",
                short: "Virement direct",
                points: -40,
                type: "Incident Majeur",
                consequenceTitle: "Vous ex�cutez la demande sans v�rification.",
                consequenceText:
                    "Le paiement part. Quelques minutes plus tard, vous r�alisez que la demande �tait frauduleuse.",
                impactLines: ["? Perte : 48 500�", "? Impact : Fraude comptable", "?? Risque r�putation : �lev�"],
            },
            {
                id: "reply_mail",
                label: 'R�pondre au mail ("Ok, je m�en occupe")',
                short: "R�pondre au mail",
                points: -15,
                type: "Mauvais Canal",
                consequenceTitle: "Vous r�pondez � l�attaquant.",
                consequenceText:
                    "Vous confirmez votre disponibilit� et donnez de la mati�re pour la manipulation (urgence, pression).",
                impactLines: ["?? Risque : Relance imm�diate", "?? Impact : Pression accrue", "?? Risque r�putation : Moyen"],
            },
            {
                id: "ignore",
                label: "Ignorer sans signaler",
                short: "Ignorer sans signaler",
                points: -10,
                type: "Manque Proactivit�",
                consequenceTitle: "Vous n�agissez pas et ne signalez pas.",
                consequenceText: "Le risque reste actif : quelqu�un d�autre peut tomber dans le pi�ge.",
                impactLines: ["?? Risque : Attaque toujours active", "?? Impact : D�pend d�un autre coll�gue", "?? Risque r�putation : Moyen"],
            },
            {
                id: "check_domain",
                label: "V�rifier l�adresse email / le domaine",
                short: "V�rifier le domaine",
                points: +10,
                type: "Bon R�flexe",
                consequenceTitle: "Vous v�rifiez l�identit� du canal.",
                consequenceText: "Vous rep�rez des signaux faibles (domaine proche, alias, affichage trompeur).",
                impactLines: ["? Risque r�duit : D�tection possible", "?? Impact : � compl�ter par une validation forte", "?? Risque r�putation : Moyen"],
            },
            {
                id: "ask_proof",
                label: "Demander justificatif (facture / RIB / ordre �crit)",
                short: "Demander justificatif",
                points: +5,
                type: "R�flexe Moyen",
                consequenceTitle: "Vous demandez des �l�ments de preuve.",
                consequenceText:
                    "C�est mieux que rien, mais un attaquant peut fournir de faux documents. Il faut une validation hors canal.",
                impactLines: ["? Risque r�duit : Un peu", "?? Impact : Faux justificatifs possibles", "?? Risque r�putation : Moyen"],
            },
            {
                id: "call_internal",
                label: "Appeler le PDG via num�ro interne / standard",
                short: "Appeler (num�ro interne)",
                points: +30,
                type: "Validation Forte",
                consequenceTitle: "Vous appelez via un canal interne fiable.",
                consequenceText: "Le PDG confirme qu�il n�a rien demand�. Vous coupez la manipulation imm�diatement.",
                impactLines: ["? Risque �vit� : 48 500�", "? Impact �vit� : Fraude comptable", "?? Risque r�putation : �lev� (attaque cibl�e)"],
            },
            {
                id: "report_security",
                label: "Signaler au manager / s�curit� / RSSI",
                short: "Signaler",
                points: +20,
                type: "Proc�dure",
                consequenceTitle: "Vous d�clenchez la proc�dure interne.",
                consequenceText:
                    "L�alerte est trac�e, l�email est analys�, et un message de pr�vention peut �tre envoy� � l�organisation.",
                impactLines: ["? Risque r�duit : Organisation prot�g�e", "? Impact : Sensibilisation imm�diate", "?? Risque r�putation : Moyen"],
            },
        ],
    },
};

function clamp(n: number, min: number, max: number) {
    return Math.max(min, Math.min(max, n));
}

export default function ModulePage() {
    const params = useParams<{ pathId: string; modulesId: string }>();

    // Route params
    const pathId = params?.pathId ?? "";
    const moduleId = params?.modulesId ?? "";

    // On supporte "general" pour l'instant (structure prod pr�te)
    // Tu peux �largir plus tard.
    if (!pathId) return notFound();

    const scenario = SCENARIOS[moduleId];
    if (!scenario) return notFound();

    const [step, setStep] = useState<"context" | "result">("context");
    const [selected, setSelected] = useState<ActionDef | null>(null);
    const [score, setScore] = useState<number>(100);
    const isMobile = useIsMobile();

    const scoreLabel = useMemo(() => {
        if (score >= 90) return "R�flexes exemplaires";
        if (score >= 70) return "Bon niveau";
        if (score >= 50) return "Vigilance � am�liorer";
        return "Risque �lev�";
    }, [score]);

    const onChoose = (a: ActionDef) => {
        const newScore = clamp(score + a.points, 0, 100);
        setScore(newScore);
        setSelected(a);
        setStep("result");
    };

    const onReplay = () => {
        setScore(100);
        setSelected(null);
        setStep("context");
    };

    return (
        <div style={{ ...styles.page, padding: isMobile ? "20px var(--page-x) 40px" : styles.page.padding }}>
            {/* Header */}
            <div style={styles.header}>
                <div style={styles.headerBar} />
                <div style={{ minWidth: 0 }}>
                    <h1 style={{ ...styles.h1, fontSize: isMobile ? 28 : styles.h1.fontSize }}>{scenario.title}</h1>
                    <div style={styles.subRow}>
                        <span style={styles.badge}>{scenario.urgencyLabel}</span>
                        <span style={styles.subTitle}>{scenario.contextTitle}</span>
                    </div>
                </div>
            </div>

            {/* Layout 2 colonnes */}
            <div style={{ ...styles.grid, gridTemplateColumns: isMobile ? "1fr" : "1.2fr 1fr" }}>
                {/* LEFT */}
                <div style={styles.leftCard}>
                    {step === "context" && (
                        <>
                            <div style={styles.contextCard}>
                                <div style={styles.contextMeta}>
                                    <div style={styles.metaLine}>
                                        <b>De :</b> {scenario.fromLabel}
                                    </div>
                                    <div style={styles.metaLine}>
                                        <b>Objet :</b> {scenario.subjectLabel}
                                    </div>
                                </div>

                                <div style={styles.quote}>{scenario.quoteHtml}</div>
                            </div>

                            <h2 style={styles.h2}>Choix d�action</h2>
                            <div style={styles.help}>
                                L�utilisateur doit d�cider : Virement ? R�ponse ? V�rification ? Appel ?
                            </div>

                            <div style={styles.actions}>
                                {scenario.actions.map((a) => (
                                    <button key={a.id} onClick={() => onChoose(a)} style={styles.actionBtn}>
                                        <div style={styles.actionLabel}>{a.label}</div>
                                        <div style={styles.actionMeta}>
                                            <span style={styles.actionType}>{a.type}</span>
                                            <span
                                                style={{
                                                    ...styles.points,
                                                    ...(a.points > 0 ? styles.pointsPos : a.points < 0 ? styles.pointsNeg : {}),
                                                }}
                                            >
                                                {a.points > 0 ? `+${a.points}` : a.points} pts
                                            </span>
                                        </div>
                                    </button>
                                ))}
                            </div>
                        </>
                    )}

                    {step === "result" && selected && (
                        <>
                            <div style={{ ...styles.resultTopRow, flexDirection: isMobile ? "column" : "row" }}>
                                <div>
                                    <h2 style={{ ...styles.h2, fontSize: isMobile ? 22 : styles.h2.fontSize }}>Syst�me de Scoring & Cons�quences</h2>
                                    <div style={styles.help}>
                                        Votre score �volue selon l�action choisie. Objectif : r�duire le risque et appliquer les bons r�flexes.
                                    </div>
                                </div>

                                <div style={{ ...styles.scoreBox, alignSelf: isMobile ? "stretch" : undefined, textAlign: isMobile ? "left" : "right" }}>
                                    <div style={styles.scoreTitle}>Score</div>
                                    <div style={styles.scoreValue}>{score}/100</div>
                                    <div style={styles.scoreLabel}>{scoreLabel}</div>
                                </div>
                            </div>

                            <div style={{ ...styles.twoCols, gridTemplateColumns: isMobile ? "1fr" : "1fr 1fr" }}>
                                {/* Table points */}
                                <div style={styles.tableCard}>
                                    <div style={styles.tableTitle}>Logique de Points</div>
                                    <div style={{ ...styles.tableHeader, gridTemplateColumns: isMobile ? "1fr 60px 90px" : "1fr 90px 140px" }}>
                                        <div>Action</div>
                                        <div style={{ textAlign: "right" }}>Score</div>
                                        <div style={{ textAlign: "right" }}>Type</div>
                                    </div>

                                    {scenario.actions.map((a) => (
                                        <div key={a.id} style={{ ...styles.tableRow, gridTemplateColumns: isMobile ? "1fr 60px 90px" : "1fr 90px 140px" }}>
                                            <div style={styles.tableRowLeft}>{a.short}</div>
                                            <div
                                                style={{
                                                    ...styles.tableRowRight,
                                                    ...(a.points > 0 ? styles.pointsPos : a.points < 0 ? styles.pointsNeg : {}),
                                                }}
                                            >
                                                {a.points > 0 ? `+${a.points}` : a.points} pts
                                            </div>
                                            <div style={styles.tableRowType}>{a.type}</div>
                                        </div>
                                    ))}
                                </div>

                                {/* Consequence card */}
                                <div style={styles.consequenceCard}>
                                    <div style={styles.consequenceTitle}>
                                        Si l�utilisateur choisit : <span style={styles.consequenceHighlight}>{selected.short}</span>
                                    </div>
                                    <div style={styles.consequenceText}>
                                        <b>{selected.consequenceTitle}</b>
                                        <div style={{ marginTop: 10 }}>{selected.consequenceText}</div>
                                    </div>

                                    <div style={styles.impactBox}>
                                        {selected.impactLines.map((l, idx) => (
                                            <div key={idx} style={styles.impactLine}>
                                                {l}
                                            </div>
                                        ))}
                                    </div>

                                    <div style={styles.ctaRow}>
                                        <button style={styles.secondaryBtn} onClick={() => setStep("context")}>
                                            Revenir au contexte
                                        </button>
                                        <button style={styles.primaryBtn} onClick={onReplay}>
                                            Rejouer
                                        </button>
                                    </div>
                                </div>
                            </div>
                        </>
                    )}
                </div>

                {/* RIGHT */}
                <div style={styles.rightCard}>
                    <div style={styles.imageWrap}>
                        <Image
                            src={scenario.imageSrc}
                            alt={`${scenario.title} - illustration`}
                            fill
                            style={{ objectFit: "cover" }}
                            priority
                        />
                    </div>

                    <div style={styles.rightInfo}>
                        <div style={styles.rightInfoTitle}>{scenario.rightHintTitle}</div>
                        <div style={styles.rightInfoText}>{scenario.rightHintHtml}</div>
                    </div>
                </div>
            </div>
        </div>
    );
}

const styles: Record<string, React.CSSProperties> = {
    page: {
        minHeight: "100vh",
        padding: "28px 28px 40px",
        background: "linear-gradient(180deg, #0b1220 0%, #07101c 100%)",
        color: "#eaf0ff",
        fontFamily: "system-ui, -apple-system, Segoe UI, Roboto, Arial, sans-serif",
    },
    header: { display: "flex", gap: 16, alignItems: "flex-start", marginBottom: 18 },
    headerBar: { width: 6, height: 54, background: "#2dd4ff", borderRadius: 999 },
    h1: { fontSize: 42, lineHeight: 1.05, margin: 0, fontWeight: 800 },
    subRow: { display: "flex", gap: 14, alignItems: "center", marginTop: 10 },
    badge: {
        fontSize: 12,
        padding: "6px 10px",
        borderRadius: 999,
        background: "rgba(255, 80, 80, 0.14)",
        border: "1px solid rgba(255, 80, 80, 0.35)",
        color: "#ff7373",
        fontWeight: 700,
        letterSpacing: 0.5,
    },
    subTitle: { fontSize: 20, color: "#2dd4ff", fontWeight: 800 },

    grid: { display: "grid", gridTemplateColumns: "1.2fr 1fr", gap: 18, alignItems: "start" },

    leftCard: {
        background: "rgba(255,255,255,0.03)",
        border: "1px solid #E2E8F0",
        borderRadius: 18,
        padding: 18,
        boxShadow: "0 20px 60px rgba(0,0,0,0.35)",
    },

    rightCard: {
        background: "rgba(255,255,255,0.03)",
        border: "1px solid #E2E8F0",
        borderRadius: 18,
        padding: 14,
        boxShadow: "0 20px 60px rgba(0,0,0,0.35)",
    },

    contextCard: {
        background: "rgba(255,255,255,0.06)",
        border: "1px solid rgba(255, 80, 80, 0.45)",
        borderRadius: 14,
        padding: 16,
    },
    contextMeta: { display: "grid", gap: 8 },
    metaLine: { color: "#d9e3ff" },
    quote: {
        marginTop: 14,
        padding: "14px 14px",
        borderRadius: 12,
        background: "rgba(255,255,255,0.06)",
        color: "#f4f7ff",
        lineHeight: 1.5,
        fontStyle: "italic",
    },

    h2: { margin: "18px 0 8px", fontSize: 28, fontWeight: 900, color: "#2dd4ff" },
    help: { color: "rgba(234,240,255,0.75)", marginBottom: 12 },

    actions: { display: "grid", gap: 10, marginTop: 10 },
    actionBtn: {
        textAlign: "left",
        background: "rgba(255,255,255,0.04)",
        border: "1px solid rgba(255,255,255,0.10)",
        borderRadius: 14,
        padding: 14,
        cursor: "pointer",
    },
    actionLabel: { fontSize: 16, fontWeight: 800, marginBottom: 8 },
    actionMeta: {
        display: "flex",
        justifyContent: "space-between",
        alignItems: "center",
        gap: 10,
        color: "rgba(234,240,255,0.75)",
        fontSize: 13,
    },
    actionType: { opacity: 0.95 },
    points: { fontWeight: 900 },
    pointsPos: { color: "#4ade80" },
    pointsNeg: { color: "#ff7373" },

    imageWrap: {
        position: "relative",
        width: "100%",
        height: 360,
        borderRadius: 14,
        overflow: "hidden",
        border: "1px solid rgba(255,255,255,0.10)",
    },
    rightInfo: {
        marginTop: 12,
        borderRadius: 14,
        padding: 12,
        background: "rgba(255,255,255,0.04)",
        border: "1px solid #E2E8F0",
    },
    rightInfoTitle: { fontWeight: 900, marginBottom: 6 },
    rightInfoText: { color: "rgba(234,240,255,0.78)", lineHeight: 1.4 },

    resultTopRow: { display: "flex", justifyContent: "space-between", gap: 12, alignItems: "flex-start" },
    scoreBox: {
        minWidth: 170,
        borderRadius: 14,
        padding: 12,
        background: "rgba(45, 212, 255, 0.10)",
        border: "1px solid rgba(45, 212, 255, 0.30)",
        textAlign: "right",
    },
    scoreTitle: { fontWeight: 900, opacity: 0.9 },
    scoreValue: { fontSize: 28, fontWeight: 1000, marginTop: 4 },
    scoreLabel: { marginTop: 2, opacity: 0.9 },

    twoCols: { display: "grid", gridTemplateColumns: "1fr 1fr", gap: 14, marginTop: 12 },

    tableCard: {
        borderRadius: 14,
        background: "rgba(255,255,255,0.04)",
        border: "1px solid rgba(255,255,255,0.10)",
        padding: 12,
    },
    tableTitle: { fontWeight: 1000, marginBottom: 10 },
    tableHeader: {
        display: "grid",
        gridTemplateColumns: "1fr 90px 140px",
        gap: 10,
        fontSize: 12,
        opacity: 0.8,
        paddingBottom: 8,
        borderBottom: "1px solid rgba(255,255,255,0.10)",
    },
    tableRow: {
        display: "grid",
        gridTemplateColumns: "1fr 90px 140px",
        gap: 10,
        padding: "10px 0",
        borderBottom: "1px solid #E2E8F0",
        alignItems: "center",
    },
    tableRowLeft: { fontWeight: 800 },
    tableRowRight: { textAlign: "right", fontWeight: 1000 },
    tableRowType: { textAlign: "right", opacity: 0.85 },

    consequenceCard: {
        borderRadius: 14,
        background: "rgba(255,255,255,0.04)",
        border: "1px solid rgba(45, 212, 255, 0.25)",
        padding: 12,
    },
    consequenceTitle: { fontWeight: 1000, marginBottom: 10 },
    consequenceHighlight: { color: "#2dd4ff" },
    consequenceText: { color: "rgba(234,240,255,0.85)", lineHeight: 1.45 },

    impactBox: {
        marginTop: 12,
        borderRadius: 12,
        padding: 12,
        background: "rgba(0,0,0,0.35)",
        border: "1px solid rgba(255,255,255,0.10)",
        fontFamily: "ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace",
    },
    impactLine: { marginBottom: 6 },

    ctaRow: { display: "flex", justifyContent: "flex-end", gap: 10, marginTop: 12 },
    primaryBtn: {
        padding: "10px 12px",
        borderRadius: 12,
        border: "1px solid rgba(45, 212, 255, 0.35)",
        background: "rgba(45, 212, 255, 0.14)",
        color: "#eaf0ff",
        fontWeight: 900,
        cursor: "pointer",
    },
    secondaryBtn: {
        padding: "10px 12px",
        borderRadius: 12,
        border: "1px solid rgba(255,255,255,0.14)",
        background: "rgba(255,255,255,0.05)",
        color: "#eaf0ff",
        fontWeight: 900,
        cursor: "pointer",
    },
};