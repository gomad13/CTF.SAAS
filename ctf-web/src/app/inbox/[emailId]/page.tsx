"use client";

import { useEffect, useMemo, useState } from "react";
import { useParams, useRouter } from "next/navigation";
import Link from "next/link";
import DOMPurify from "dompurify";
import { useInboxEmail, useReportPhishing } from "@/lib/hooks/useScenarios";
import { ArrowLeft, ShieldCheck, AlertTriangle } from "lucide-react";
import { useIsMobile } from "@/hooks/useMediaQuery";
import Reveal from "@/components/Reveal";

export default function EmailDetailPage() {
    const params = useParams<{ emailId: string }>();
    const router = useRouter();
    const isMobile = useIsMobile();
    const { data: email, isLoading, refetch } = useInboxEmail(params.emailId);
    const { report, isLoading: reporting } = useReportPhishing();
    const [reportMessage, setReportMessage] = useState<string | null>(null);
    const [reportSuccess, setReportSuccess] = useState(false);

    const safeHtml = useMemo(() => {
        if (!email?.bodyHtml) return "";
        // DOMPurify avec config par défaut côté client. Le backend renvoie déjà
        // du HTML rendu et nettoyé (nos templates), mais on ajoute cette défense
        // en profondeur côté front au cas où un override admin contiendrait du XSS.
        // [PENTEST] retrait de "style" (anti CSS-injection / UI redressing) + interdiction explicite des tags dangereux
        return DOMPurify.sanitize(email.bodyHtml, {
            ALLOWED_TAGS: ["p", "a", "br", "strong", "em", "b", "i", "u", "ul", "ol", "li", "img", "h1", "h2", "h3", "h4", "blockquote", "div", "span"],
            ALLOWED_ATTR: ["href", "src", "alt", "title", "rel", "target", "width", "height", "data-original-href"],
            FORBID_TAGS: ["script", "iframe", "object", "embed", "style", "form"],
            FORBID_ATTR: ["style", "onerror", "onload"],
        });
    }, [email?.bodyHtml]);

    useEffect(() => {
        // Quand on charge l'email, le backend marque automatiquement comme lu
        // (cf. InboxController.GetMine). On rafraîchit pour que la liste reflète.
        if (email) refetch();
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [email?.id]);

    if (isLoading) return <div style={{ padding: 24, color: "var(--text-2)" }}>Chargement…</div>;
    if (!email) return <div style={{ padding: 24, color: "var(--danger-t)" }}>Email introuvable.</div>;

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
        <div style={{ padding: isMobile ? "20px 16px" : "32px 24px", background: "var(--bg)", minHeight: "100%" }}>
            <Link href="/inbox" style={{ display: "inline-flex", alignItems: "center", gap: 6, minHeight: 44, color: "var(--text-2)", fontSize: 13, textDecoration: "none", marginBottom: 8 }}>
                <ArrowLeft size={14} /> Retour Inbox
            </Link>

            <Reveal>
            <div style={{ background: "var(--surface)", border: "1px solid var(--border)", borderRadius: 12, padding: isMobile ? 16 : 24 }}>
                <div style={{ display: "flex", alignItems: "flex-start", justifyContent: "space-between", gap: 12, flexWrap: "wrap", marginBottom: 16, paddingBottom: 16, borderBottom: "1px solid var(--border)" }}>
                    <div style={{ flex: 1, minWidth: 0 }}>
                        <h1 style={{ fontSize: 18, fontWeight: 600, color: "var(--text)", margin: "0 0 8px", overflowWrap: "break-word" }}>{email.subject}</h1>
                        <div style={{ fontSize: 13, color: "var(--text-2)", overflowWrap: "break-word", wordBreak: "break-word" }}>
                            <strong style={{ color: "var(--text)" }}>{email.fromName}</strong> &lt;{email.fromEmail}&gt;
                        </div>
                        <div style={{ fontSize: 12, color: "var(--text-3)", marginTop: 2 }}>
                            {new Date(email.sentAt).toLocaleString("fr-FR")}
                        </div>
                    </div>
                    {!email.isSystemNotification && !email.isReported && (
                        <button onClick={onReport} disabled={reporting} style={{
                            display: "inline-flex", alignItems: "center", justifyContent: "center", gap: 6,
                            padding: "10px 14px", minHeight: 44, border: "1px solid color-mix(in srgb, var(--danger) 40%, transparent)",
                            background: "var(--danger-subtle)", color: "var(--danger-t)",
                            borderRadius: 8, fontSize: 13, fontWeight: 500, cursor: reporting ? "wait" : "pointer",
                            transition: "all 0.2s",
                            width: isMobile ? "100%" : "auto",
                        }}
                            onMouseEnter={e => { e.currentTarget.style.background = "color-mix(in srgb, var(--danger) 18%, transparent)"; }}
                            onMouseLeave={e => { e.currentTarget.style.background = "var(--danger-subtle)"; }}
                        >
                            <AlertTriangle size={14} /> {reporting ? "Signalement…" : "Signaler comme phishing"}
                        </button>
                    )}
                    {email.isReported && (
                        <span style={{ display: "inline-flex", alignItems: "center", gap: 6, padding: "8px 12px", background: "var(--success-subtle)", color: "var(--success-t)", borderRadius: 99, fontSize: 12, fontWeight: 500 }}>
                            <ShieldCheck size={14} /> Email signalé
                        </span>
                    )}
                </div>

                {reportMessage && (
                    <div style={{
                        marginBottom: 16, padding: 12,
                        background: reportSuccess ? "var(--success-subtle)" : "var(--danger-subtle)",
                        border: reportSuccess ? "1px solid color-mix(in srgb, var(--success) 22%, transparent)" : "1px solid color-mix(in srgb, var(--danger) 22%, transparent)",
                        borderRadius: 8, color: reportSuccess ? "var(--success-t)" : "var(--danger-t)", fontSize: 13,
                    }}>{reportMessage}</div>
                )}

                <div
                    className="inbox-email-body"
                    style={{ fontSize: 14, color: "var(--text-2)", lineHeight: 1.6, overflowWrap: "break-word", wordBreak: "break-word" }}
                    dangerouslySetInnerHTML={{ __html: safeHtml }}
                />
            </div>
            </Reveal>
        </div>
    );
}
