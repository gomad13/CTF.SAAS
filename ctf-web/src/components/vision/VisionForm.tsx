"use client";

import { useState } from "react";
import type { CSSProperties, ReactNode, InputHTMLAttributes, TextareaHTMLAttributes, SelectHTMLAttributes, ButtonHTMLAttributes } from "react";
import VisionCard from "./VisionCard";

/**
 * Primitives de formulaire Vision UI (thème violet, tokens --v-*).
 * Réutilisées par les Paramètres entreprise + InvitesManager. Zéro couleur en dur.
 * À utiliser dans un sous-arbre `.vision-dashboard`.
 */

const RING = "0 0 0 3px color-mix(in srgb, var(--v-accent) 32%, transparent)";
const fieldBase: CSSProperties = {
    width: "100%", minWidth: 0, borderRadius: 10, background: "var(--v-surface-2)",
    border: "1px solid var(--v-border)", color: "var(--v-text)", fontSize: 14,
    padding: "10px 14px", outline: "none",
    transition: "border-color .15s ease, box-shadow .15s ease",
};
function focusStyle(f: boolean): CSSProperties {
    return { borderColor: f ? "var(--v-accent)" : "var(--v-border)", boxShadow: f ? RING : "none" };
}

export function VisionSection({ icon, title, desc, children }: { icon: ReactNode; title: string; desc?: string; children: ReactNode }) {
    return (
        <VisionCard>
            <div style={{ display: "flex", alignItems: "center", gap: 12 }}>
                <span aria-hidden style={{ display: "inline-flex", width: 34, height: 34, borderRadius: 10, alignItems: "center", justifyContent: "center", background: "color-mix(in srgb, var(--v-accent) 16%, transparent)", color: "var(--v-accent)", flexShrink: 0 }}>{icon}</span>
                <div style={{ minWidth: 0 }}>
                    <h2 style={{ fontSize: 16, fontWeight: 700, color: "var(--v-text)", letterSpacing: "-0.01em" }}>{title}</h2>
                    {desc && <p style={{ fontSize: 12.5, color: "var(--v-text-2)", marginTop: 2, lineHeight: 1.5 }}>{desc}</p>}
                </div>
            </div>
            <div style={{ marginTop: 18, display: "flex", flexDirection: "column", gap: 16 }}>{children}</div>
        </VisionCard>
    );
}

export function VisionField({ label, hint, children }: { label: string; hint?: ReactNode; children: ReactNode }) {
    return (
        <label style={{ display: "flex", flexDirection: "column", gap: 6 }}>
            <span style={{ fontSize: 12.5, fontWeight: 600, color: "var(--v-text)" }}>{label}</span>
            {children}
            {hint && <span style={{ fontSize: 12, color: "var(--v-text-2)", lineHeight: 1.5 }}>{hint}</span>}
        </label>
    );
}

export function VisionInput({ style, onFocus, onBlur, ...rest }: InputHTMLAttributes<HTMLInputElement>) {
    const [f, setF] = useState(false);
    return <input {...rest}
        onFocus={e => { setF(true); onFocus?.(e); }} onBlur={e => { setF(false); onBlur?.(e); }}
        style={{ ...fieldBase, ...focusStyle(f), ...style }} />;
}

export function VisionTextarea({ style, onFocus, onBlur, ...rest }: TextareaHTMLAttributes<HTMLTextAreaElement>) {
    const [f, setF] = useState(false);
    return <textarea {...rest}
        onFocus={e => { setF(true); onFocus?.(e); }} onBlur={e => { setF(false); onBlur?.(e); }}
        style={{ ...fieldBase, resize: "vertical", ...focusStyle(f), ...style }} />;
}

export function VisionSelect({ style, onFocus, onBlur, children, ...rest }: SelectHTMLAttributes<HTMLSelectElement>) {
    const [f, setF] = useState(false);
    return <select {...rest}
        onFocus={e => { setF(true); onFocus?.(e); }} onBlur={e => { setF(false); onBlur?.(e); }}
        style={{ ...fieldBase, cursor: "pointer", ...focusStyle(f), ...style }}>{children}</select>;
}

export function VisionButton({ variant = "primary", style, disabled, children, ...rest }: { variant?: "primary" | "secondary" | "ghost" } & ButtonHTMLAttributes<HTMLButtonElement>) {
    const [h, setH] = useState(false);
    const primary = variant === "primary";
    const secondary = variant === "secondary";
    return (
        <button {...rest} disabled={disabled} onMouseEnter={() => setH(true)} onMouseLeave={() => setH(false)}
            style={{
                display: "inline-flex", alignItems: "center", justifyContent: "center", gap: 8,
                fontSize: 13.5, fontWeight: 600, padding: "9px 16px", borderRadius: 10,
                cursor: disabled ? "default" : "pointer", opacity: disabled ? 0.5 : 1,
                transition: "filter .15s ease, background-color .15s ease, transform .1s ease, box-shadow .15s ease",
                transform: h && !disabled ? "translateY(-1px)" : "none",
                border: primary ? "none" : "1px solid var(--v-border)",
                background: primary ? "var(--v-grad)" : secondary ? (h ? "var(--v-surface-2)" : "color-mix(in srgb, var(--v-surface-2) 60%, transparent)") : (h ? "var(--v-surface-2)" : "transparent"),
                color: primary ? "var(--v-text)" : "var(--v-text)",
                boxShadow: primary ? "0 6px 18px color-mix(in srgb, var(--v-accent) 42%, transparent)" : "none",
                filter: primary && h && !disabled ? "brightness(1.08)" : "none",
                ...style,
            }}>{children}</button>
    );
}

export function VisionToggle({ on, onChange, disabled = false }: { on: boolean; onChange: (v: boolean) => void; disabled?: boolean }) {
    return (
        <button type="button" role="switch" aria-checked={on} disabled={disabled} onClick={() => onChange(!on)}
            style={{ position: "relative", height: 24, width: 44, flexShrink: 0, borderRadius: 999, border: "none", cursor: disabled ? "default" : "pointer", opacity: disabled ? 0.5 : 1, background: on ? "var(--v-grad)" : "var(--v-surface-2)", transition: "background .2s ease" }}>
            <span aria-hidden style={{ position: "absolute", top: 2, left: on ? 22 : 2, height: 20, width: 20, borderRadius: 999, background: "var(--v-text)", transition: "left .2s ease", boxShadow: "0 1px 3px rgba(0,0,0,.3)" }} />
        </button>
    );
}
