"use client";

import { useEffect, useMemo, useState } from "react";
import { useParams, useRouter } from "next/navigation";
import Link from "next/link";
import DOMPurify from "dompurify";
import { useInboxEmail, useReportPhishing } from "@/lib/hooks/useScenarios";
import { ArrowLeft, ShieldCheck, AlertTriangle } from "lucide-react";

export default function EmailDetailPage() {
    const params = useParams<{ emailId: string }>();
    const router = useRouter();
    const { data: email, isLoading, refetch } = useInboxEmail(params.emailId);
    const { report, isLoading: reporting } = useReportPhishing();
    const [reportMessage, setReportMessage] = useState<string | null>(null);
    const [reportSuccess, setReportSuccess] = useState(false);

    const safeHtml = useMemo(() => {
        if (!email?.bodyHtml) return "";
        // DOMPurify avec config par défaut côté client. Le backend renvoie déjà
        // du HTML rendu et nettoyé (nos templates), mais on ajoute cette défense
        // en profondeur côté front au cas où un override admin contiendrait du XSS.
        return DOMPurify.sanitize(email.bodyHtml, {
            ALLOWED_TAGS: ["p", "a", "br", "strong", "em", "b", "i", "u", "ul", "ol", "li", "img", "h1", "h2", "h3", "h4", "blockquote", "div", "span"],
            ALLOWED_ATTR: ["href", "src", "alt", "title", "rel", "target", "width", "height", "style", "data-original-href"],
        });
    }, [email?.bodyHtml]);

    useEffect(() => {
        // Quand on charge l'email, le backend marque automatiquement comme lu
        // (cf. InboxController.GetMine). On rafraîchit pour que la liste reflète.
        if (email) refetch();
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [email?.id]);

    if (isLoading) return <div style={{ padding: 24, color: "#64748B" }}>Chargement…</div>;
    if (!email) return <div style={{ padding: 24, color: "#EF4444" }}>Email introuvable.</div>;

    async function onReport() {
        const r = await report(email!.id);
        setReportMessage(r.message);
        setReportSuccess(r.success);
        if (r.triggeredOutcome) {
            // Petit délai puis retour inbox pour voir le message système final
            setTimeout(() => router.push("/inbox"), 2000);
        }
    }

    return (
        <div style={{ padding: "32px 24px", background: "#F8FAFC", minHeight: "100%" }}>
            <Link href="/inbox" style={{ display: "inline-flex", alignItems: "center", gap: 6, color: "#64748B", fontSize: 13, textDecoration: "none", marginBottom: 12 }}>
                <ArrowLeft size={14} /> Retour Inbox
            </Link>

            <div style={{ background: "#FFFFFF", border: "1px solid #E2E8F0", borderRadius: 12, padding: 24 }}>
                <div style={{ display: "flex", alignItems: "flex-start", justifyContent: "space-between", gap: 12, marginBottom: 16, paddingBottom: 16, borderBottom: "1px solid #E2E8F0" }}>
                    <div style={{ flex: 1, minWidth: 0 }}>
                        <h1 style={{ fontSize: 18, fontWeight: 600, color: "#1E293B", margin: "0 0 8px" }}>{email.subject}</h1>
                        <div style={{ fontSize: 13, color: "#64748B" }}>
                            <strong style={{ color: "#334155" }}>{email.fromName}</strong> &lt;{email.fromEmail}&gt;
                        </div>
                        <div style={{ fontSize: 12, color: "#94A3B8", marginTop: 2 }}>
                            {new Date(email.sentAt).toLocaleString("fr-FR")}
                        </div>
                    </div>
                    {!email.isSystemNotification && !email.isReported && (
                        <button onClick={onReport} disabled={reporting} style={{
                            display: "inline-flex", alignItems: "center", gap: 6,
                            padding: "10px 14px", border: "1px solid #FCA5A5",
                            background: "#FEF2F2", color: "#B91C1C",
                            borderRadius: 8, fontSize: 13, fontWeight: 500, cursor: reporting ? "wait" : "pointer",
                            transition: "all 0.2s",
                        }}>
                            <AlertTriangle size={14} /> {reporting ? "Signalement…" : "Signaler comme phishing"}
                        </button>
                    )}
                    {email.isReported && (
                        <span style={{ display: "inline-flex", alignItems: "center", gap: 6, padding: "8px 12px", background: "rgba(16,185,129,0.10)", color: "#10B981", borderRadius: 99, fontSize: 12, fontWeight: 500 }}>
                            <ShieldCheck size={14} /> Email signalé
                        </span>
                    )}
                </div>

                {reportMessage && (
                    <div style={{
                        marginBottom: 16, padding: 12,
                        background: reportSuccess ? "rgba(16,185,129,0.08)" : "rgba(239,68,68,0.08)",
                        border: reportSuccess ? "1px solid rgba(16,185,129,0.20)" : "1px solid rgba(239,68,68,0.20)",
                        borderRadius: 8, color: reportSuccess ? "#065F46" : "#B91C1C", fontSize: 13,
                    }}>{reportMessage}</div>
                )}

                <div
                    style={{ fontSize: 14, color: "#334155", lineHeight: 1.6 }}
                    dangerouslySetInnerHTML={{ __html: safeHtml }}
                />
            </div>
        </div>
    );
}
