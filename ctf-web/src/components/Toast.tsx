"use client";

import { useState, useEffect } from "react";

type TType = "ok" | "er" | "wa" | "info";
interface T { id: string; msg: string; type: TType }

const listeners: ((t: T) => void)[] = [];

export const toast = {
    ok: (msg: string) => fire(msg, "ok"),
    er: (msg: string) => fire(msg, "er"),
    wa: (msg: string) => fire(msg, "wa"),
    info: (msg: string) => fire(msg, "info"),
    success: (msg: string) => fire(msg, "ok"),
    error: (msg: string) => fire(msg, "er"),
    warning: (msg: string) => fire(msg, "wa"),
};

function fire(msg: string, type: TType) {
    const t = { id: crypto.randomUUID(), msg, type };
    listeners.forEach(l => l(t));
}

export default function ToastContainer() {
    const [ts, setTs] = useState<T[]>([]);

    useEffect(() => {
        const add = (t: T) => {
            setTs(p => [...p, t]);
            setTimeout(() => setTs(p => p.filter(x => x.id !== t.id)), 3500);
        };
        listeners.push(add);
        return () => {
            const i = listeners.indexOf(add);
            if (i > -1) listeners.splice(i, 1);
        };
    }, []);

    const icons: Record<TType, string> = { ok: "✓", er: "✕", wa: "⚠", info: "ℹ" };

    return (
        <div className="toast-wrap">
            {ts.map(t => (
                <div key={t.id} className={`toast toast-${t.type} anim-up`}>
                    <span style={{ fontSize: 15, flexShrink: 0 }}>{icons[t.type]}</span>
                    <span style={{ flex: 1 }}>{t.msg}</span>
                    <button
                        onClick={() => setTs(p => p.filter(x => x.id !== t.id))}
                        style={{
                            background: "none", border: "none", color: "var(--tx-3)",
                            cursor: "pointer", fontSize: 18, lineHeight: 1, padding: "0 2px", flexShrink: 0,
                        }}
                    >×</button>
                </div>
            ))}
        </div>
    );
}
