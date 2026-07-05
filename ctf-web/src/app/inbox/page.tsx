"use client";

import Link from "next/link";
import { useInbox } from "@/lib/hooks/useScenarios";
import { Mail, ShieldCheck, AlertTriangle, Inbox as InboxIcon } from "lucide-react";
import { useIsMobile } from "@/hooks/useMediaQuery";
import type { InboxEmailListItem } from "@/lib/types/scenarios";
import Reveal from "@/components/Reveal";
import { Stagger, StaggerItem } from "@/components/Stagger";
import CountUp from "@/components/CountUp";

export default function InboxPage() {
    const { data, isLoading } = useInbox(true);
    const isMobile = useIsMobile();

    const unreadCount = data?.filter(e => !e.isRead).length ?? 0;

    return (
        <div style={{ padding: isMobile ? "20px 16px" : "32px 24px", background: "var(--bg)", minHeight: "100%" }}>
            <Reveal>
                <div style={{ marginBottom: isMobile ? 16 : 24, display: "flex", alignItems: "center", justifyContent: "space-between", gap: 12, flexWrap: "wrap" }}>
                    <div style={{ minWidth: 0 }}>
                        <h1 style={{ fontSize: isMobile ? 20 : 24, fontWeight: 700, color: "var(--text)", margin: 0, display: "flex", alignItems: "center", gap: 8 }}>
                            <InboxIcon size={isMobile ? 22 : 26} strokeWidth={1.75} /> Inbox
                        </h1>
                        <p style={{ color: "var(--text-2)", fontSize: isMobile ? 13 : 14, marginTop: 4 }}>Tes emails de simulation. Si tu repères un message suspect, signale-le immédiatement.</p>
                    </div>
                    {unreadCount > 0 && (
                        <span style={{ background: "var(--accent)", color: "var(--on-accent)", padding: "4px 12px", borderRadius: 99, fontSize: 12, fontWeight: 600 }}><CountUp value={unreadCount} /> non lu{unreadCount > 1 ? "s" : ""}</span>
                    )}
                </div>
            </Reveal>

            <div style={{ background: "var(--surface)", border: "1px solid var(--border)", borderRadius: 12, overflow: "hidden" }}>
                {isLoading && <div style={{ padding: 24, color: "var(--text-2)" }}>Chargement…</div>}
                {!isLoading && (data ?? []).length === 0 && (
                    <div style={{ padding: 32, textAlign: "center", color: "var(--text-2)" }}>
                        <InboxIcon size={32} style={{ margin: "0 auto 8px", opacity: 0.4 }} />
                        <div style={{ fontSize: 14 }}>Inbox vide.</div>
                    </div>
                )}
                <Stagger gap={0.04}>
                    {(data ?? []).map(e => <StaggerItem key={e.id}><Row email={e} /></StaggerItem>)}
                </Stagger>
            </div>
        </div>
    );
}

function Row({ email }: { email: InboxEmailListItem }) {
    const isUnread = !email.isRead && !email.isReported;
    return (
        <Link href={`/inbox/${email.id}`} style={{
            display: "flex", alignItems: "center", gap: 12,
            padding: 14, borderTop: "1px solid var(--border)",
            background: isUnread ? "var(--surface-2)" : "var(--surface)",
            textDecoration: "none", color: "inherit",
            transition: "background 0.2s",
        }}
            onMouseEnter={e => (e.currentTarget as HTMLElement).style.background = "var(--surface-2)"}
            onMouseLeave={e => (e.currentTarget as HTMLElement).style.background = isUnread ? "var(--surface-2)" : "var(--surface)"}
        >
            <div style={{ width: 36, height: 36, borderRadius: 8, background: email.isSystemNotification ? "var(--info-subtle)" : "var(--surface-2)", display: "flex", alignItems: "center", justifyContent: "center", flexShrink: 0 }}>
                <Mail size={16} color={email.isSystemNotification ? "var(--info)" : "var(--text-2)"} strokeWidth={1.75} />
            </div>
            <div style={{ flex: 1, minWidth: 0 }}>
                <div style={{ display: "flex", justifyContent: "space-between", alignItems: "baseline", gap: 12 }}>
                    <span style={{ fontSize: 13, fontWeight: isUnread ? 600 : 400, color: "var(--text)", whiteSpace: "nowrap", overflow: "hidden", textOverflow: "ellipsis" }}>{email.fromName} &lt;{email.fromEmail}&gt;</span>
                    <span style={{ fontSize: 11, color: "var(--text-3)", flexShrink: 0 }}>{new Date(email.sentAt).toLocaleString("fr-FR", { day: "2-digit", month: "short", hour: "2-digit", minute: "2-digit" })}</span>
                </div>
                <div style={{ fontSize: 13.5, color: "var(--text-2)", marginTop: 2, fontWeight: isUnread ? 500 : 400, whiteSpace: "nowrap", overflow: "hidden", textOverflow: "ellipsis" }}>{email.subject}</div>
            </div>
            <div style={{ display: "flex", gap: 6, flexShrink: 0 }}>
                {email.isReported && <span style={{ display: "inline-flex", alignItems: "center", gap: 3, fontSize: 11, padding: "2px 8px", borderRadius: 99, background: "var(--success-subtle)", color: "var(--success-t)" }}><ShieldCheck size={11} /> Signalé</span>}
                {email.isSystemNotification && <span style={{ fontSize: 11, padding: "2px 8px", borderRadius: 99, background: "var(--info-subtle)", color: "var(--info)" }}>Système</span>}
                {isUnread && <span style={{ width: 8, height: 8, borderRadius: "50%", background: "var(--accent)" }} />}
            </div>
        </Link>
    );
}

// keep AlertTriangle imported for future surfacing of attack hints inline
void AlertTriangle;
