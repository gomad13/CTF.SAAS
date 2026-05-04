"use client";

import Link from "next/link";
import { useInbox } from "@/lib/hooks/useScenarios";
import { Mail, ShieldCheck, AlertTriangle, Inbox as InboxIcon } from "lucide-react";
import type { InboxEmailListItem } from "@/lib/types/scenarios";

export default function InboxPage() {
    const { data, isLoading } = useInbox(true);

    const unreadCount = data?.filter(e => !e.isRead).length ?? 0;

    return (
        <div style={{ padding: "32px 24px", background: "#F8FAFC", minHeight: "100%" }}>
            <div style={{ marginBottom: 24, display: "flex", alignItems: "center", justifyContent: "space-between" }}>
                <div>
                    <h1 style={{ fontSize: 24, fontWeight: 700, color: "#1E293B", margin: 0, display: "flex", alignItems: "center", gap: 8 }}>
                        <InboxIcon size={26} strokeWidth={1.75} /> Inbox
                    </h1>
                    <p style={{ color: "#64748B", fontSize: 14, marginTop: 4 }}>Tes emails de simulation. Si tu repères un message suspect, signale-le immédiatement.</p>
                </div>
                {unreadCount > 0 && (
                    <span style={{ background: "#3B82F6", color: "white", padding: "4px 12px", borderRadius: 99, fontSize: 12, fontWeight: 600 }}>{unreadCount} non lu{unreadCount > 1 ? "s" : ""}</span>
                )}
            </div>

            <div style={{ background: "#FFFFFF", border: "1px solid #E2E8F0", borderRadius: 12, overflow: "hidden" }}>
                {isLoading && <div style={{ padding: 24, color: "#64748B" }}>Chargement…</div>}
                {!isLoading && (data ?? []).length === 0 && (
                    <div style={{ padding: 32, textAlign: "center", color: "#64748B" }}>
                        <InboxIcon size={32} style={{ margin: "0 auto 8px", opacity: 0.4 }} />
                        <div style={{ fontSize: 14 }}>Inbox vide.</div>
                    </div>
                )}
                {(data ?? []).map(e => <Row key={e.id} email={e} />)}
            </div>
        </div>
    );
}

function Row({ email }: { email: InboxEmailListItem }) {
    const isUnread = !email.isRead && !email.isReported;
    return (
        <Link href={`/inbox/${email.id}`} style={{
            display: "flex", alignItems: "center", gap: 12,
            padding: 14, borderTop: "1px solid #E2E8F0",
            background: isUnread ? "#F8FAFC" : "#FFFFFF",
            textDecoration: "none", color: "inherit",
            transition: "background 0.2s",
        }}
            onMouseEnter={e => (e.currentTarget as HTMLElement).style.background = "#F1F5F9"}
            onMouseLeave={e => (e.currentTarget as HTMLElement).style.background = isUnread ? "#F8FAFC" : "#FFFFFF"}
        >
            <div style={{ width: 36, height: 36, borderRadius: 8, background: email.isSystemNotification ? "rgba(59,130,246,0.10)" : "rgba(100,116,139,0.10)", display: "flex", alignItems: "center", justifyContent: "center", flexShrink: 0 }}>
                <Mail size={16} color={email.isSystemNotification ? "#3B82F6" : "#64748B"} strokeWidth={1.75} />
            </div>
            <div style={{ flex: 1, minWidth: 0 }}>
                <div style={{ display: "flex", justifyContent: "space-between", alignItems: "baseline", gap: 12 }}>
                    <span style={{ fontSize: 13, fontWeight: isUnread ? 600 : 400, color: "#1E293B", whiteSpace: "nowrap", overflow: "hidden", textOverflow: "ellipsis" }}>{email.fromName} &lt;{email.fromEmail}&gt;</span>
                    <span style={{ fontSize: 11, color: "#94A3B8", flexShrink: 0 }}>{new Date(email.sentAt).toLocaleString("fr-FR", { day: "2-digit", month: "short", hour: "2-digit", minute: "2-digit" })}</span>
                </div>
                <div style={{ fontSize: 13.5, color: "#334155", marginTop: 2, fontWeight: isUnread ? 500 : 400, whiteSpace: "nowrap", overflow: "hidden", textOverflow: "ellipsis" }}>{email.subject}</div>
            </div>
            <div style={{ display: "flex", gap: 6, flexShrink: 0 }}>
                {email.isReported && <span style={{ display: "inline-flex", alignItems: "center", gap: 3, fontSize: 11, padding: "2px 8px", borderRadius: 99, background: "rgba(16,185,129,0.10)", color: "#10B981" }}><ShieldCheck size={11} /> Signalé</span>}
                {email.isSystemNotification && <span style={{ fontSize: 11, padding: "2px 8px", borderRadius: 99, background: "rgba(59,130,246,0.10)", color: "#3B82F6" }}>Système</span>}
                {isUnread && <span style={{ width: 8, height: 8, borderRadius: "50%", background: "#3B82F6" }} />}
            </div>
        </Link>
    );
}

// keep AlertTriangle imported for future surfacing of attack hints inline
void AlertTriangle;
