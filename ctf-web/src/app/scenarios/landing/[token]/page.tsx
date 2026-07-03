"use client";

import { useEffect, useRef } from "react";
import { useParams } from "next/navigation";
import Link from "next/link";
import { useScenarioLanding } from "@/lib/hooks/useScenarios";
import { useGenerateCoaching } from "@/lib/hooks/useCoaching";
import { useIsMobile } from "@/hooks/useMediaQuery";
import {
    AlertTriangle, ShieldAlert, Lightbulb, BookOpen, Heart, Mail,
} from "lucide-react";

export default function ScenarioLandingPage() {
    const params = useParams<{ token: string }>();
    const isMobile = useIsMobile();
    const { data: landing, isLoading } = useScenarioLanding(params.token);
    const { generate, data: coaching, isLoading: coachingLoading, error: coachingError } = useGenerateCoaching();
    const triedRef = useRef(false);

    useEffect(() => {
        // Une seule tentative auto à l'affichage. Si l'employé recharge la page,
        // useGenerateCoaching renvoie l'instance existante (idempotence backend).
        // Le ref évite la cascade setState→render→effect que React 19 signale,
        // sans changer la sémantique « tenter une seule fois par mount ».
        if (!triedRef.current && landing?.emailId && landing.isAttackStep && landing.wasClicked) {
            triedRef.current = true;
            // Pour brancher le coaching IA P4, on génère sur l'emailId comme
            // attemptId discriminant. P4 lit depuis ChallengeCompletions et
            // renverra 404 — le rendu côté frontend fallback alors sur les
            // hints VSF (ce qui est le comportement V1 acceptable : le coaching
            // P4 sera étendu aux scenarios en V2 via un seedAttempt côté engine).
            generate(landing.emailId);
        }
    }, [landing?.emailId, landing?.isAttackStep, landing?.wasClicked, generate]);

    if (isLoading) {
        return (
            <div style={{ minHeight: "100vh", background: "#0A0A0B", display: "flex", alignItems: "center", justifyContent: "center", color: "#94A3B8" }}>
                Chargement…
            </div>
        );
    }
    if (!landing) {
        return (
            <div style={{ minHeight: "100vh", background: "#0A0A0B", display: "flex", alignItems: "center", justifyContent: "center", color: "#94A3B8" }}>
                Lien invalide ou expiré.
            </div>
        );
    }

    return (
        <div style={{ minHeight: "100vh", background: "linear-gradient(135deg,#0A0A0B 0%,#1E293B 100%)", padding: isMobile ? "28px 16px" : "48px 24px" }}>
            <div style={{ maxWidth: 720, margin: "0 auto" }}>
                {/* Hero */}
                <div style={{
                    background: "rgba(239,68,68,0.10)", border: "1px solid rgba(239,68,68,0.30)",
                    borderRadius: 16, padding: isMobile ? 20 : 32, marginBottom: isMobile ? 16 : 24,
                    display: "flex", alignItems: "flex-start", gap: isMobile ? 14 : 20,
                }}>
                    <div style={{ width: isMobile ? 44 : 56, height: isMobile ? 44 : 56, borderRadius: 12, background: "rgba(239,68,68,0.20)", display: "flex", alignItems: "center", justifyContent: "center", flexShrink: 0 }}>
                        <ShieldAlert size={isMobile ? 22 : 28} color="#FCA5A5" strokeWidth={1.75} />
                    </div>
                    <div style={{ minWidth: 0 }}>
                        <h1 style={{ fontSize: isMobile ? 19 : 24, fontWeight: 700, color: "#FCA5A5", margin: 0, overflowWrap: "break-word" }}>Tu viens de cliquer sur un lien piégé</h1>
                        <p style={{ fontSize: 14, color: "#FECACA", margin: "8px 0 0", lineHeight: 1.6 }}>
                            {"Bonne nouvelle : c'est une simulation Sentys, pas une vraie attaque."} <strong>{"Aucun mot de passe, RIB ou virement n'est en jeu."}</strong>{" "}{"Mais imagine 2 secondes que c'était vrai — c'est exactement à ça qu'on s'entraîne."}
                        </p>
                    </div>
                </div>

                {/* Scénario context */}
                {landing.scenarioName && (
                    <div style={{ background: "rgba(255,255,255,0.04)", border: "1px solid rgba(255,255,255,0.08)", borderRadius: 12, padding: isMobile ? 16 : 20, marginBottom: isMobile ? 16 : 24 }}>
                        <div style={{ fontSize: 11, color: "#94A3B8", textTransform: "uppercase", letterSpacing: "0.05em", fontWeight: 600, marginBottom: 4 }}>Scénario simulé</div>
                        <div style={{ fontSize: 16, color: "#F1F5F9", fontWeight: 500, overflowWrap: "break-word" }}>{landing.scenarioName}</div>
                        {landing.scenarioCategory && <div style={{ fontSize: 12, color: "#64748B", marginTop: 2 }}>Catégorie : {landing.scenarioCategory}</div>}
                    </div>
                )}

                {/* Hints VSF — toujours affichés */}
                {landing.hints.length > 0 && (
                    <div style={{ background: "rgba(255,255,255,0.04)", border: "1px solid rgba(255,255,255,0.08)", borderRadius: 12, padding: isMobile ? 16 : 24, marginBottom: isMobile ? 16 : 24 }}>
                        <h2 style={{ fontSize: 16, fontWeight: 600, color: "#F1F5F9", margin: "0 0 16px", display: "flex", alignItems: "center", gap: 8 }}>
                            <Lightbulb size={18} color="#F59E0B" /> Les indices que tu aurais pu repérer
                        </h2>
                        <ul style={{ margin: 0, paddingLeft: 20, color: "#CBD5E1", fontSize: 13.5, lineHeight: 1.7, overflowWrap: "break-word", wordBreak: "break-word" }}>
                            {landing.hints.map((h, i) => <li key={i} style={{ marginBottom: 6 }}>{h}</li>)}
                        </ul>
                    </div>
                )}

                {/* Coaching IA — si dispo */}
                {coaching && (
                    <div style={{ background: "rgba(59,130,246,0.08)", border: "1px solid rgba(59,130,246,0.25)", borderRadius: 12, padding: isMobile ? 16 : 24, marginBottom: isMobile ? 16 : 24 }}>
                        <h2 style={{ fontSize: 16, fontWeight: 600, color: "#93C5FD", margin: "0 0 16px", display: "flex", alignItems: "center", gap: 8 }}>
                            <Heart size={18} /> Coaching personnalisé
                        </h2>
                        <p style={{ fontSize: 13.5, color: "#DBEAFE", lineHeight: 1.7, whiteSpace: "pre-wrap", overflowWrap: "break-word", wordBreak: "break-word", margin: 0 }}>{coaching.content}</p>
                    </div>
                )}
                {coachingLoading && (
                    <div style={{ background: "rgba(255,255,255,0.04)", border: "1px solid rgba(255,255,255,0.08)", borderRadius: 12, padding: 16, marginBottom: 24, color: "#94A3B8", fontSize: 13 }}>
                        Génération du coaching personnalisé…
                    </div>
                )}
                {coachingError && !coachingLoading && (
                    <div style={{ background: "rgba(245,158,11,0.08)", border: "1px solid rgba(245,158,11,0.20)", borderRadius: 12, padding: 16, marginBottom: 24, color: "#FCD34D", fontSize: 13 }}>
                        Coaching IA indisponible pour ce scénario — les indices ci-dessus restent ta meilleure ressource pour ne plus tomber dans le piège.
                    </div>
                )}

                {/* Actions */}
                <div style={{ display: "flex", gap: 12, justifyContent: "center", flexWrap: "wrap", marginTop: isMobile ? 24 : 32 }}>
                    <Link href="/inbox" style={{
                        display: "inline-flex", alignItems: "center", justifyContent: "center", gap: 8,
                        padding: "12px 20px", minHeight: 44, background: "#3B82F6", color: "white",
                        borderRadius: 8, fontSize: 14, fontWeight: 500, textDecoration: "none",
                        width: isMobile ? "100%" : "auto",
                    }}><Mail size={16} /> {"Retour à l'Inbox"}</Link>
                    <Link href="/dashboard" style={{
                        display: "inline-flex", alignItems: "center", justifyContent: "center", gap: 8,
                        padding: "12px 20px", minHeight: 44, border: "1px solid rgba(255,255,255,0.10)", color: "#CBD5E1",
                        borderRadius: 8, fontSize: 14, fontWeight: 500, textDecoration: "none",
                        background: "rgba(255,255,255,0.04)",
                        width: isMobile ? "100%" : "auto",
                    }}><BookOpen size={16} /> Mon dashboard</Link>
                </div>

                <div style={{ marginTop: 32, padding: 16, background: "rgba(245,158,11,0.06)", border: "1px solid rgba(245,158,11,0.15)", borderRadius: 8, color: "#FCD34D", fontSize: 12, display: "flex", alignItems: "flex-start", gap: 10 }}>
                    <AlertTriangle size={16} style={{ flexShrink: 0, marginTop: 2 }} />
                    <span>{"Si tu reçois un email similaire "}<em>{"en vrai"}</em>{" et que tu cliques par réflexe, change immédiatement ton mot de passe et préviens ton support IT. Le bon réflexe : signaler avant d'agir."}</span>
                </div>
            </div>
        </div>
    );
}
