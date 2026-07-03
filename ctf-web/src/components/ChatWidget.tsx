"use client";

import { useState, useEffect, useRef, useCallback } from "react";
import { usePathname } from "next/navigation";
import { apiFetch } from "@/lib/api";
import AriaMessage from "@/components/AriaMessage";

const API_BASE = process.env.NEXT_PUBLIC_API_BASE_URL ?? "";

interface ChatMessage {
    id: string;
    role: "user" | "assistant";
    content: string;
    error?: boolean;
}

const SUGGESTIONS = [
    "Comment fonctionne le phishing ?",
    "J'ai du mal avec ce challenge, peux-tu m'aider ?",
    "Comment réinitialiser mon mot de passe ?",
    "Qu'est-ce que le RGPD ?",
];

type Status = {
    available: boolean;
    provider: string;
    model: string;
    message: string;
    ollamaReachable?: boolean;
    modelInstalled?: boolean;
    modelWarm?: boolean;
    suggestions?: string[];
};

export default function ChatWidget({
    challengeTitle,
    challengeDifficulty,
    userPoints,
    userProgressPercent,
}: {
    challengeTitle?: string;
    challengeDifficulty?: string;
    userPoints?: number;
    userProgressPercent?: number;
}) {
    const [isOpen, setIsOpen] = useState(false);
    const [input, setInput] = useState("");
    const [messages, setMessages] = useState<ChatMessage[]>([]);
    const [loading, setLoading] = useState(false);
    const [status, setStatus] = useState<Status | null>(null);
    const [remaining, setRemaining] = useState(50);
    const messagesEndRef = useRef<HTMLDivElement>(null);
    const inputRef = useRef<HTMLInputElement>(null);
    const pathname = usePathname();

    const checkStatus = useCallback(async () => {
        try {
            const data = await apiFetch<Status>("/api/chatbot/status");
            setStatus(data);
        } catch {
            setStatus({ available: false, provider: "ollama", model: "?", message: "Erreur" });
        }
    }, []);

    useEffect(() => { checkStatus(); }, [checkStatus]);

    useEffect(() => {
        if (isOpen) {
            setTimeout(() => inputRef.current?.focus(), 100);
            messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
        }
    }, [isOpen, messages]);

    const isAvailable = status?.available ?? false;

    async function sendMessage() {
        if (!input.trim() || loading || !isAvailable) return;

        const text = input.trim();
        setInput("");

        const userMsg: ChatMessage = { id: crypto.randomUUID(), role: "user", content: text };
        const assistantId = crypto.randomUUID();
        // On crée la bulle assistant vide immédiatement pour afficher le "caret" dès le 1er token.
        setMessages(prev => [
            ...prev, userMsg,
            { id: assistantId, role: "assistant", content: "" },
        ]);
        setLoading(true);

        const history = messages.slice(-10).map(m => ({ role: m.role, content: m.content }));
        const body = JSON.stringify({
            message: text,
            history,
            context: { currentPage: pathname, challengeTitle, challengeDifficulty, userPoints, userProgressPercent },
        });

        try {
            const res = await fetch(`${API_BASE}/api/chatbot/stream`, {
                method: "POST",
                credentials: "include",
                headers: { "Content-Type": "application/json", "X-Requested-With": "XMLHttpRequest" },
                body,
            });

            if (!res.ok || !res.body) {
                const msg = res.status === 429
                    ? "Limite horaire atteinte. Réessayez plus tard."
                    : res.status === 503 || res.status === 504
                        ? "Le service IA est temporairement indisponible. Réessayez dans quelques instants."
                        : "Erreur de connexion au service IA.";
                setMessages(prev => prev.map(m => m.id === assistantId ? { ...m, content: msg, error: true } : m));
                return;
            }

            const reader = res.body.getReader();
            const decoder = new TextDecoder();
            let buffer = "";
            let accumulated = "";
            let pendingEventName: string | null = null;

            function flushMessage(data: string) {
                if (!pendingEventName) return;
                const eventName = pendingEventName;
                pendingEventName = null;
                let parsed: { content?: string; message?: string; remaining?: number; code?: string } = {};
                try { parsed = JSON.parse(data); } catch { return; }

                if (eventName === "token" && parsed.content) {
                    accumulated += parsed.content;
                    setMessages(prev => prev.map(m => m.id === assistantId ? { ...m, content: accumulated } : m));
                } else if (eventName === "final" && parsed.content) {
                    accumulated = parsed.content;
                    setMessages(prev => prev.map(m => m.id === assistantId ? { ...m, content: accumulated } : m));
                } else if (eventName === "done") {
                    if (typeof parsed.remaining === "number") setRemaining(parsed.remaining);
                } else if (eventName === "error") {
                    const errMsg = parsed.message || "Erreur du service IA.";
                    setMessages(prev => prev.map(m => m.id === assistantId ? { ...m, content: errMsg, error: true } : m));
                }
            }

            while (true) {
                const { done, value } = await reader.read();
                if (done) break;
                buffer += decoder.decode(value, { stream: true });
                // SSE : les events sont séparés par \n\n.
                let sepIdx: number;
                while ((sepIdx = buffer.indexOf("\n\n")) >= 0) {
                    const raw = buffer.slice(0, sepIdx);
                    buffer = buffer.slice(sepIdx + 2);
                    pendingEventName = null;
                    let dataLine = "";
                    for (const line of raw.split("\n")) {
                        if (line.startsWith("event:")) pendingEventName = line.slice(6).trim();
                        else if (line.startsWith("data:")) dataLine += line.slice(5).trimStart();
                    }
                    if (pendingEventName && dataLine) flushMessage(dataLine);
                }
            }
        } catch (e) {
            setMessages(prev => prev.map(m =>
                m.id === assistantId
                    ? { ...m, content: "Erreur de connexion. Vérifiez votre connexion réseau.", error: true }
                    : m
            ));
        } finally {
            setLoading(false);
        }
    }

    function handleKeyDown(e: React.KeyboardEvent) {
        if (e.key === "Enter" && !e.shiftKey) { e.preventDefault(); sendMessage(); }
        if (e.key === "Escape") setIsOpen(false);
    }

    return (
        <>
            {/* Bouton flottant */}
            <button
                onClick={() => setIsOpen(o => !o)}
                style={{
                    position: "fixed",
                    bottom: 28,
                    right: 32,
                    width: 56,
                    height: 56,
                    borderRadius: "50%",
                    background: isAvailable ? "linear-gradient(135deg, var(--accent), var(--accent-hover))" : "#374151",
                    border: "none",
                    cursor: "pointer",
                    zIndex: 1000,
                    display: "flex",
                    alignItems: "center",
                    justifyContent: "center",
                    boxShadow: isAvailable ? "0 4px 20px rgba(59,130,246,0.4)" : "0 4px 12px rgba(0,0,0,0.3)",
                }}
                title={isAvailable ? "Sentys Bot — Assistante IA" : "IA indisponible"}
            >
                {isOpen ? (
                    <span style={{ color: "#fff", fontSize: 22 }}>×</span>
                ) : (
                    <svg width="24" height="24" viewBox="0 0 24 24" fill="none">
                        <path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm0 18c-4.41 0-8-3.59-8-8s3.59-8 8-8 8 3.59 8 8-3.59 8-8 8zm-1-4h2v2h-2zm1-10c-2.21 0-4 1.79-4 4h2c0-1.1.9-2 2-2s2 .9 2 2c0 2-3 1.75-3 5h2c0-2.25 3-2.5 3-5 0-2.21-1.79-4-4-4z" fill="white" />
                    </svg>
                )}
                <span style={{
                    position: "absolute", top: 2, right: 2,
                    width: 12, height: 12, borderRadius: "50%",
                    background: isAvailable ? "#10B981" : "#ef4444",
                    border: "2px solid #0A0A0B",
                }} />
            </button>

            {/* Fenêtre chat */}
            {isOpen && (
                <div style={{
                    position: "fixed",
                    bottom: 96,
                    right: 16,
                    width: "min(360px, calc(100vw - 32px))",
                    height: "min(560px, calc(100vh - 120px))",
                    background: "var(--bg-surface)",
                    border: "1px solid rgba(59,130,246,0.25)",
                    borderRadius: 16,
                    display: "flex",
                    flexDirection: "column",
                    zIndex: 1000,
                    boxShadow: "0 20px 60px rgba(0,0,0,0.5), 0 0 0 1px rgba(59,130,246,0.1)",
                    overflow: "hidden",
                }}>
                    {/* Header */}
                    <div style={{
                        padding: "16px 20px",
                        borderBottom: "1px solid #E2E8F0",
                        background: "var(--bg-base)",
                        display: "flex",
                        alignItems: "center",
                        justifyContent: "space-between",
                    }}>
                        <div style={{ display: "flex", alignItems: "center", gap: 10 }}>
                            <div style={{
                                width: 36, height: 36, borderRadius: "50%",
                                background: "linear-gradient(135deg, var(--accent), var(--accent-hover))",
                                display: "flex", alignItems: "center", justifyContent: "center",
                                fontSize: 16,
                            }}>🤖</div>
                            <div>
                                <div style={{ fontWeight: 600, color: "var(--text-primary)", fontSize: 14 }}>Sentys Bot</div>
                                <div style={{
                                    fontSize: 11,
                                    color: isAvailable ? "#4ade80" : "#f87171",
                                    display: "flex",
                                    alignItems: "center",
                                    gap: 4,
                                }}>
                                    <span style={{
                                        width: 6, height: 6, borderRadius: "50%",
                                        background: isAvailable ? "#10B981" : "#ef4444",
                                        display: "inline-block",
                                    }} />
                                    {isAvailable
                                        ? (status?.modelWarm === false
                                            ? `Prête · ${status?.model ?? "IA"} (1er appel lent)`
                                            : `En ligne · ${status?.model ?? "IA"}`)
                                        : "Hors ligne"}
                                </div>
                            </div>
                        </div>
                        {messages.length > 0 && (
                            <button
                                onClick={() => setMessages([])}
                                style={{
                                    background: "transparent",
                                    border: "1px solid rgba(255,255,255,0.1)",
                                    borderRadius: 6,
                                    color: "var(--text-muted)",
                                    cursor: "pointer",
                                    padding: "4px 10px",
                                    fontSize: 11,
                                }}
                            >Effacer</button>
                        )}
                    </div>

                    {/* Messages */}
                    <div style={{
                        flex: 1,
                        overflowY: "auto",
                        padding: 16,
                        display: "flex",
                        flexDirection: "column",
                        gap: 12,
                    }}>
                        {messages.length === 0 && (
                            <div>
                                <div style={{
                                    background: "rgba(59,130,246,0.08)",
                                    border: "1px solid rgba(59,130,246,0.2)",
                                    borderRadius: 12,
                                    padding: 14,
                                    marginBottom: 16,
                                }}>
                                    <p style={{ color: "var(--text-secondary)", fontSize: 13, lineHeight: 1.6, margin: 0 }}>
                                        👋 Bonjour ! Je suis <strong style={{ color: "var(--pr-l)" }}>Sentys Bot</strong>, votre assistant IA.
                                        <br /><br />
                                        Je peux vous aider avec la plateforme ou vous guider sur les challenges.
                                        {challengeTitle && (
                                            <>
                                                <br /><br />
                                                <span style={{ color: "#fbbf24" }}>
                                                    📌 Challenge actuel : <strong>{challengeTitle}</strong>
                                                </span>
                                            </>
                                        )}
                                    </p>
                                </div>

                                {!isAvailable ? (
                                    <div style={{
                                        background: "rgba(239,68,68,0.08)",
                                        border: "1px solid rgba(239,68,68,0.2)",
                                        borderRadius: 8,
                                        padding: 12,
                                        fontSize: 12,
                                        color: "#f87171",
                                    }}>
                                        ⚠️ L&apos;IA locale n&apos;est pas disponible.<br />
                                        Assurez-vous qu&apos;Ollama est lancé :
                                        <code style={{
                                            display: "block",
                                            marginTop: 6,
                                            background: "var(--bg-base)",
                                            padding: "4px 8px",
                                            borderRadius: 4,
                                            color: "#4ade80",
                                        }}>ollama serve</code>
                                    </div>
                                ) : (
                                    <div>
                                        <p style={{ fontSize: 11, color: "var(--text-muted)", marginBottom: 8 }}>Suggestions :</p>
                                        {SUGGESTIONS.map((s, i) => (
                                            <button key={i}
                                                onClick={() => { setInput(s); inputRef.current?.focus(); }}
                                                style={{
                                                    display: "block",
                                                    width: "100%",
                                                    textAlign: "left",
                                                    background: "var(--bg-card)",
                                                    border: "1px solid #E2E8F0",
                                                    borderRadius: 8,
                                                    padding: "8px 12px",
                                                    color: "var(--text-secondary)",
                                                    fontSize: 12,
                                                    cursor: "pointer",
                                                    marginBottom: 6,
                                                }}
                                            >{s}</button>
                                        ))}
                                    </div>
                                )}
                            </div>
                        )}

                        {messages.map(msg => (
                            <div key={msg.id} style={{
                                display: "flex",
                                flexDirection: msg.role === "user" ? "row-reverse" : "row",
                                alignItems: "flex-start",
                                gap: 8,
                            }}>
                                {msg.role === "assistant" && (
                                    <div style={{
                                        width: 28, height: 28, borderRadius: "50%",
                                        background: "linear-gradient(135deg, var(--accent), var(--accent-hover))",
                                        display: "flex", alignItems: "center", justifyContent: "center",
                                        fontSize: 12, flexShrink: 0,
                                    }}>🤖</div>
                                )}
                                <div style={{
                                    maxWidth: "80%",
                                    padding: "10px 14px",
                                    borderRadius: msg.role === "user" ? "14px 14px 4px 14px" : "14px 14px 14px 4px",
                                    background: msg.error
                                        ? "rgba(239,68,68,0.1)"
                                        : msg.role === "user"
                                            ? "linear-gradient(135deg, var(--accent), var(--accent-hover))"
                                            : "var(--bg-card)",
                                    border: msg.error
                                        ? "1px solid rgba(239,68,68,0.3)"
                                        : msg.role === "user"
                                            ? "none"
                                            : "1px solid rgba(255,255,255,0.06)",
                                    color: msg.error ? "#f87171" : "#ffffff",
                                    fontSize: 13,
                                    lineHeight: 1.6,
                                    whiteSpace: "pre-wrap",
                                }}>
                                    {msg.role === "assistant" && !msg.error ? (
                                        <AriaMessage content={msg.content} />
                                    ) : (
                                        msg.content
                                    )}
                                </div>
                            </div>
                        ))}

                        {loading && (
                            <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
                                <div style={{
                                    width: 28, height: 28, borderRadius: "50%",
                                    background: "linear-gradient(135deg, var(--accent), var(--accent-hover))",
                                    display: "flex", alignItems: "center", justifyContent: "center",
                                    fontSize: 12,
                                }}>🤖</div>
                                <div style={{
                                    background: "var(--bg-card)",
                                    border: "1px solid #E2E8F0",
                                    borderRadius: "14px 14px 14px 4px",
                                    padding: "12px 16px",
                                    display: "flex", gap: 4, alignItems: "center",
                                }}>
                                    {[0, 1, 2].map(i => (
                                        <span key={i} style={{
                                            width: 6, height: 6, borderRadius: "50%",
                                            background: "var(--pr)",
                                            animation: `chatBounce 1s ${i * 0.2}s infinite`,
                                        }} />
                                    ))}
                                </div>
                            </div>
                        )}

                        <div ref={messagesEndRef} />
                    </div>

                    {/* Footer */}
                    <div style={{ padding: "12px 16px", borderTop: "1px solid #E2E8F0", background: "var(--bg-base)" }}>
                        <div style={{ display: "flex", justifyContent: "flex-end", marginBottom: 8 }}>
                            <span style={{ fontSize: 10, color: remaining < 10 ? "#f87171" : "#6b7280" }}>
                                {remaining} messages restants / heure
                            </span>
                        </div>
                        <div style={{ display: "flex", gap: 8, alignItems: "flex-end" }}>
                            <input
                                ref={inputRef}
                                value={input}
                                onChange={e => setInput(e.target.value)}
                                onKeyDown={handleKeyDown}
                                placeholder={isAvailable ? "Posez votre question..." : "IA indisponible"}
                                disabled={!isAvailable || loading}
                                maxLength={2000}
                                style={{
                                    flex: 1,
                                    background: "var(--bg-card)",
                                    border: "1px solid #E2E8F0",
                                    borderRadius: 10,
                                    padding: "10px 14px",
                                    color: "var(--text-primary)",
                                    fontSize: 13,
                                    outline: "none",
                                }}
                                onFocus={e => { e.currentTarget.style.borderColor = "rgba(59,130,246,0.5)"; }}
                                onBlur={e => { e.currentTarget.style.borderColor = "var(--border)"; }}
                            />
                            <button
                                onClick={sendMessage}
                                disabled={!input.trim() || !isAvailable || loading}
                                style={{
                                    width: 40,
                                    height: 40,
                                    borderRadius: 10,
                                    background: !input.trim() || !isAvailable
                                        ? "var(--bg-card)"
                                        : "linear-gradient(135deg, var(--accent), var(--accent-hover))",
                                    border: "none",
                                    cursor: !input.trim() || !isAvailable ? "not-allowed" : "pointer",
                                    display: "flex",
                                    alignItems: "center",
                                    justifyContent: "center",
                                    flexShrink: 0,
                                }}
                            >
                                <svg width="18" height="18" viewBox="0 0 24 24" fill="none">
                                    <path d="M22 2L11 13M22 2L15 22l-4-9-9-4 7-7z"
                                        stroke="white" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" />
                                </svg>
                            </button>
                        </div>
                    </div>
                </div>
            )}

            <style>{`
                @keyframes chatBounce {
                    0%,80%,100% { transform: translateY(0); }
                    40% { transform: translateY(-6px); }
                }
            `}</style>
        </>
    );
}
