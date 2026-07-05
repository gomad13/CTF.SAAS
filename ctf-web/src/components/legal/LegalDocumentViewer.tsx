"use client";

import { useMemo } from "react";
import { useQuery } from "@tanstack/react-query";
import DOMPurify from "dompurify";
import type { LegalDocument } from "@/lib/types/legal";

const API_BASE = process.env.NEXT_PUBLIC_API_BASE_URL ?? "";

const ALLOWED_TAGS = [
    "p", "h1", "h2", "h3", "h4", "h5", "h6",
    "ul", "ol", "li", "strong", "em", "a", "table", "thead", "tbody",
    "tr", "td", "th", "blockquote", "br", "hr", "span", "div", "code", "pre",
];
const ALLOWED_ATTR = ["href", "title", "target", "rel"];

async function fetchDocument(slug: string): Promise<LegalDocument> {
    const r = await fetch(`${API_BASE}/api/legal/documents/${encodeURIComponent(slug)}`, { credentials: "include" });
    if (r.status === 404) throw new Error("Document introuvable.");
    if (!r.ok) throw new Error(`HTTP ${r.status}`);
    return r.json();
}

/**
 * Rendu lisible d'un document légal récupéré via /api/legal/documents/{slug}.
 * Sanitization stricte du HTML via DOMPurify (whitelist sémantique).
 */
export default function LegalDocumentViewer({ slug }: { slug: string }) {
    const { data: doc, isLoading, error } = useQuery<LegalDocument>({
        queryKey: ["legal", "document", slug],
        queryFn: () => fetchDocument(slug),
        staleTime: 5 * 60 * 1000,
    });

    const safeHtml = useMemo(() => {
        if (!doc) return "";
        return DOMPurify.sanitize(doc.contentHtml, {
            ALLOWED_TAGS,
            ALLOWED_ATTR,
            FORBID_TAGS: ["script", "iframe", "object", "embed", "style"],
        });
    }, [doc]);

    if (isLoading) return <div style={{ padding: 24, color: "var(--text-3)" }}>Chargement…</div>;
    if (error) return <div style={{ padding: 24, color: "var(--danger)" }}>{error instanceof Error ? error.message : "Erreur."}</div>;
    if (!doc) return null;

    return (
        <article style={{ maxWidth: 800, margin: "0 auto", padding: "clamp(20px, 6vw, 32px) clamp(16px, 5vw, 24px)", color: "var(--text)", overflowWrap: "break-word", wordBreak: "break-word" }}>
            <header style={{ borderBottom: "1px solid var(--border)", paddingBottom: 16, marginBottom: 24 }}>
                <h1 style={{ fontSize: 28, fontWeight: 700, margin: "0 0 8px", color: "var(--text)" }}>{doc.title}</h1>
                <div style={{ fontSize: 13, color: "var(--text-3)" }}>
                    Version <strong>{doc.version}</strong> — Publiée le{" "}
                    <time dateTime={doc.publishedAt}>{new Date(doc.publishedAt).toLocaleDateString("fr-FR")}</time>
                </div>
            </header>
            <div
                className="legal-content"
                style={{ fontSize: 16, lineHeight: 1.7 }}
                dangerouslySetInnerHTML={{ __html: safeHtml }}
            />
            <footer style={{ marginTop: 40, paddingTop: 16, borderTop: "1px solid var(--border)", fontSize: 13, color: "var(--text-3)" }}>
                Vous avez des questions ? Contactez notre DPO :{" "}
                <a href="mailto:dpo@sentys.fr" className="transition-colors duration-200 hover:text-[var(--accent-hover)]" style={{ color: "var(--accent)", fontWeight: 500 }}>dpo@sentys.fr</a>.
            </footer>
            <style>{`
                .legal-content h2 { font-size: 20px; font-weight: 700; margin: 28px 0 12px; color: var(--text); }
                .legal-content h3 { font-size: 16px; font-weight: 600; margin: 20px 0 8px; color: var(--text-2); }
                .legal-content p, .legal-content li { color: var(--text-2); overflow-wrap: break-word; word-break: break-word; }
                .legal-content ul, .legal-content ol { padding-left: 24px; margin: 12px 0; }
                .legal-content li { margin: 6px 0; }
                .legal-content a { overflow-wrap: break-word; word-break: break-word; }
                .legal-content table { display: block; max-width: 100%; overflow-x: auto; -webkit-overflow-scrolling: touch; border-collapse: collapse; margin: 16px 0; font-size: 14px; }
                .legal-content th, .legal-content td { border: 1px solid var(--border); padding: 10px 12px; text-align: left; vertical-align: top; }
                .legal-content thead { background: var(--surface-2); }
                .legal-content a { color: var(--accent); text-decoration: underline; }
                .legal-content a:hover { color: var(--accent-hover); }
                .legal-content strong { color: var(--text); }
            `}</style>
        </article>
    );
}
