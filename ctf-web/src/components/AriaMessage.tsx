"use client";

import { parseAriaMessage } from "@/lib/chatParser";

export default function AriaMessage({ content }: { content: string }) {
    const segments = parseAriaMessage(content);

    return (
        <div style={{
            display: "flex",
            flexDirection: "column",
            gap: 10,
            fontSize: 13,
            lineHeight: 1.65,
        }}>
            {segments.map((seg, i) => {
                if (seg.type === "conseil") {
                    return (
                        <div key={i} style={{
                            display: "flex",
                            gap: 10,
                            alignItems: "flex-start",
                            background: "rgba(34,197,94,0.08)",
                            border: "1px solid rgba(34,197,94,0.25)",
                            borderLeft: "3px solid #10B981",
                            borderRadius: "0 6px 6px 0",
                            padding: "9px 12px",
                        }}>
                            <span style={{ flexShrink: 0, fontSize: 13, marginTop: 1 }}>✅</span>
                            <span style={{ color: "#86efac", fontWeight: 400 }}>{seg.content}</span>
                        </div>
                    );
                }

                if (seg.type === "danger") {
                    return (
                        <div key={i} style={{
                            display: "flex",
                            gap: 10,
                            alignItems: "flex-start",
                            background: "rgba(239,68,68,0.08)",
                            border: "1px solid rgba(239,68,68,0.25)",
                            borderLeft: "3px solid #ef4444",
                            borderRadius: "0 6px 6px 0",
                            padding: "9px 12px",
                        }}>
                            <span style={{ flexShrink: 0, fontSize: 13, marginTop: 1 }}>⚠️</span>
                            <span style={{ color: "#fca5a5", fontWeight: 400 }}>{seg.content}</span>
                        </div>
                    );
                }

                return (
                    <p key={i} style={{ color: "#d4d4d4", margin: 0, lineHeight: 1.65 }}>
                        {seg.content}
                    </p>
                );
            })}
        </div>
    );
}
