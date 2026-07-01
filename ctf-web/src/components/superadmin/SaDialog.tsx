"use client";

import { useEffect, useState, useCallback, useRef } from "react";

/**
 * SaDialog — modal + toast cohérents avec le thème SuperAdmin.
 * Remplace les alert() / confirm() / prompt() natifs.
 *
 * Usage :
 *   const dlg = useSaDialog();
 *   await dlg.alert("Titre", "Message") ;                    // info
 *   await dlg.error("Titre", "Message") ;                    // erreur garde-fou
 *   const ok = await dlg.confirm("Titre", "Question", "Confirmer");
 *   const typed = await dlg.prompt("Titre", "Question", "Tapez ici");
 *   dlg.toast("Action effectuée");
 */

type DialogVariant = "info" | "danger" | "warning" | "success";

type PendingDialog =
    | { kind: "alert";   variant: DialogVariant; title: string; message: string;  resolve: () => void }
    | { kind: "confirm"; variant: DialogVariant; title: string; message: string; confirmLabel: string; resolve: (ok: boolean) => void }
    | { kind: "prompt";  variant: DialogVariant; title: string; message: string; placeholder: string; expected?: string; resolve: (value: string | null) => void };

type PendingToast = { id: number; message: string; variant: DialogVariant };

export function useSaDialog() {
    const [pending, setPending] = useState<PendingDialog | null>(null);
    const [toasts, setToasts]   = useState<PendingToast[]>([]);
    const seqRef = useRef(0);

    const show = useCallback((p: PendingDialog) => {
        setPending(p);
    }, []);

    const alert = useCallback((title: string, message: string, variant: DialogVariant = "info") =>
        new Promise<void>(resolve => show({ kind: "alert", variant, title, message, resolve })), [show]);

    const error = useCallback((title: string, message: string) => alert(title, message, "danger"), [alert]);

    const confirm = useCallback((title: string, message: string, confirmLabel: string = "Confirmer", variant: DialogVariant = "warning") =>
        new Promise<boolean>(resolve => show({ kind: "confirm", variant, title, message, confirmLabel, resolve })), [show]);

    const prompt = useCallback((title: string, message: string, placeholder: string = "", expected?: string) =>
        new Promise<string | null>(resolve => show({ kind: "prompt", variant: "danger", title, message, placeholder, expected, resolve })), [show]);

    const toast = useCallback((message: string, variant: DialogVariant = "success") => {
        const id = ++seqRef.current;
        setToasts(prev => [...prev, { id, message, variant }]);
        setTimeout(() => setToasts(prev => prev.filter(t => t.id !== id)), 3500);
    }, []);

    const view = <SaDialogHost pending={pending} setPending={setPending} toasts={toasts} />;

    return { alert, error, confirm, prompt, toast, view };
}

function SaDialogHost({
    pending, setPending, toasts,
}: {
    pending: PendingDialog | null;
    setPending: (p: PendingDialog | null) => void;
    toasts: PendingToast[];
}) {
    const [promptValue, setPromptValue] = useState("");
    useEffect(() => { if (pending?.kind === "prompt") setPromptValue(""); }, [pending]);

    useEffect(() => {
        if (!pending) return;
        function onKey(e: KeyboardEvent) {
            if (e.key === "Escape") cancel();
        }
        window.addEventListener("keydown", onKey);
        return () => window.removeEventListener("keydown", onKey);
    });

    function close() { setPending(null); }
    function cancel() {
        if (!pending) return;
        if (pending.kind === "alert")   pending.resolve();
        if (pending.kind === "confirm") pending.resolve(false);
        if (pending.kind === "prompt")  pending.resolve(null);
        close();
    }
    function ok() {
        if (!pending) return;
        if (pending.kind === "alert")   pending.resolve();
        if (pending.kind === "confirm") pending.resolve(true);
        if (pending.kind === "prompt") {
            if (pending.expected && promptValue !== pending.expected) return; // bloqué tant qu'on n'a pas tapé la valeur exacte
            pending.resolve(promptValue);
        }
        close();
    }

    const color = variantColor(pending?.variant ?? "info");

    return (
        <>
            {pending && (
                <div
                    role="dialog"
                    aria-modal="true"
                    onClick={cancel}
                    style={{
                        position: "fixed", inset: 0, zIndex: 9999,
                        background: "rgba(5,0,0,0.72)",
                        backdropFilter: "blur(6px)",
                        display: "flex", alignItems: "center", justifyContent: "center",
                        padding: 16,
                    }}
                >
                    <div
                        onClick={e => e.stopPropagation()}
                        style={{
                            width: "100%", maxWidth: 480,
                            background: "#0d0000",
                            border: `1px solid ${color.border}`,
                            borderRadius: 10,
                            boxShadow: `0 20px 60px rgba(0,0,0,0.6), 0 0 0 1px ${color.border}`,
                            padding: 24,
                        }}
                    >
                        <div style={{ display: "flex", alignItems: "flex-start", gap: 12, marginBottom: 14 }}>
                            <div style={{
                                width: 36, height: 36, borderRadius: 8,
                                background: `${color.bg}`,
                                border: `1px solid ${color.border}`,
                                display: "flex", alignItems: "center", justifyContent: "center",
                                color: color.fg, fontSize: 18, fontWeight: 700, flexShrink: 0,
                            }}>{color.icon}</div>
                            <div style={{ flex: 1, minWidth: 0 }}>
                                <h3 style={{
                                    fontSize: 15, fontWeight: 700, color: "#FFFFFF",
                                    margin: 0, marginBottom: 6, lineHeight: 1.35,
                                }}>{pending.title}</h3>
                                <p style={{
                                    fontSize: 13, color: "#94A3B8",
                                    margin: 0, lineHeight: 1.55,
                                }}>{pending.message}</p>
                            </div>
                        </div>

                        {pending.kind === "prompt" && (
                            <div style={{ marginTop: 6, marginBottom: 4 }}>
                                <input
                                    autoFocus
                                    value={promptValue}
                                    onChange={e => setPromptValue(e.target.value)}
                                    onKeyDown={e => { if (e.key === "Enter") ok(); }}
                                    placeholder={pending.placeholder}
                                    style={{
                                        width: "100%",
                                        background: "#1a0000",
                                        border: "1px solid rgba(239,68,68,0.35)",
                                        borderRadius: 6,
                                        padding: "9px 12px",
                                        color: "#FFFFFF",
                                        fontSize: 13,
                                        fontFamily: "'JetBrains Mono', monospace",
                                        outline: "none",
                                    }}
                                />
                                {pending.expected && promptValue && promptValue !== pending.expected && (
                                    <div style={{ marginTop: 6, fontSize: 11, color: "#FCA5A5" }}>
                                        Ne correspond pas à l&apos;intitulé exact.
                                    </div>
                                )}
                            </div>
                        )}

                        <div style={{ display: "flex", justifyContent: "flex-end", gap: 8, marginTop: 18 }}>
                            {pending.kind !== "alert" && (
                                <button onClick={cancel} style={{
                                    background: "transparent",
                                    border: "1px solid rgba(255,255,255,0.12)",
                                    color: "#94A3B8",
                                    padding: "8px 16px",
                                    borderRadius: 6, fontSize: 12, cursor: "pointer",
                                }}>Annuler</button>
                            )}
                            <button
                                onClick={ok}
                                disabled={pending.kind === "prompt" && !!pending.expected && promptValue !== pending.expected}
                                style={{
                                    background: color.fg,
                                    border: "none",
                                    color: "#FFFFFF",
                                    padding: "8px 16px",
                                    borderRadius: 6, fontSize: 12, fontWeight: 600,
                                    cursor: "pointer",
                                    opacity: pending.kind === "prompt" && !!pending.expected && promptValue !== pending.expected ? 0.5 : 1,
                                }}
                            >
                                {pending.kind === "alert" ? "OK" :
                                 pending.kind === "confirm" ? pending.confirmLabel :
                                 "Confirmer"}
                            </button>
                        </div>
                    </div>
                </div>
            )}

            {/* Toasts */}
            {toasts.length > 0 && (
                <div style={{
                    position: "fixed", bottom: 24, right: 24, zIndex: 9998,
                    display: "flex", flexDirection: "column", gap: 8, pointerEvents: "none",
                }}>
                    {toasts.map(t => {
                        const c = variantColor(t.variant);
                        return (
                            <div key={t.id} style={{
                                pointerEvents: "auto",
                                display: "flex", alignItems: "center", gap: 10,
                                padding: "10px 14px",
                                background: "#0d0000",
                                border: `1px solid ${c.border}`,
                                borderLeft: `3px solid ${c.fg}`,
                                borderRadius: 6,
                                color: "#FFFFFF",
                                fontSize: 13,
                                minWidth: 240, maxWidth: 360,
                                boxShadow: "0 10px 30px rgba(0,0,0,0.5)",
                            }}>
                                <span style={{ color: c.fg, fontSize: 14, fontWeight: 700 }}>{c.icon}</span>
                                <span>{t.message}</span>
                            </div>
                        );
                    })}
                </div>
            )}
        </>
    );
}

function variantColor(v: DialogVariant) {
    switch (v) {
        case "danger":  return { fg: "#EF4444", bg: "rgba(239,68,68,0.12)",   border: "rgba(239,68,68,0.45)", icon: "⚠" };
        case "warning": return { fg: "#F59E0B", bg: "rgba(245,158,11,0.12)",  border: "rgba(245,158,11,0.45)", icon: "!" };
        case "success": return { fg: "#10B981", bg: "rgba(16,185,129,0.12)",  border: "rgba(16,185,129,0.45)", icon: "✓" };
        case "info":
        default:        return { fg: "#3B82F6", bg: "rgba(59,130,246,0.12)",  border: "rgba(59,130,246,0.45)", icon: "i" };
    }
}
