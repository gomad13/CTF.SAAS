"use client";

import { forwardRef, useId, useState, type CSSProperties } from "react";
import { Eye, EyeOff } from "lucide-react";

type PasswordInputProps = Omit<React.InputHTMLAttributes<HTMLInputElement>, "type"> & {
    label?: string;
    error?: string;
};

const defaultInputStyle: CSSProperties = {
    width: "100%",
    background: "var(--bg-input)",
    border: "1px solid var(--border)",
    borderRadius: 8,
    padding: "10px 14px",
    color: "var(--text-primary)",
    fontSize: 14,
    outline: "none",
    boxSizing: "border-box",
    transition: "border-color 0.2s, box-shadow 0.2s",
};

const labelStyle: CSSProperties = {
    display: "block",
    fontFamily: "'JetBrains Mono', monospace",
    fontSize: 11,
    fontWeight: 600,
    letterSpacing: "0.1em",
    textTransform: "uppercase",
    color: "var(--text-muted)",
    marginBottom: 6,
};

export const PasswordInput = forwardRef<HTMLInputElement, PasswordInputProps>(
    function PasswordInput(
        {
            label,
            error,
            id: idProp,
            className,
            style,
            onFocus: userOnFocus,
            onBlur: userOnBlur,
            ...rest
        },
        ref,
    ) {
        const [isVisible, setIsVisible] = useState(false);
        const reactId = useId();
        const inputId = idProp ?? reactId;

        // When `className` is provided we let the class own most properties.
        // When it's not, we apply our canonical inline styles.
        // In both cases we force `paddingRight: 44` so the eye icon never
        // overlaps the typed text. `error` paints a red border in fallback mode.
        const finalStyle: CSSProperties = className
            ? { ...style, paddingRight: 44 }
            : {
                ...defaultInputStyle,
                border: error ? "1px solid var(--error)" : defaultInputStyle.border,
                ...style,
                paddingRight: 44,
            };

        return (
            <div style={{ width: "100%" }}>
                {label && (
                    <label htmlFor={inputId} style={labelStyle}>
                        {label}
                    </label>
                )}
                <div style={{ position: "relative" }}>
                    <input
                        ref={ref}
                        id={inputId}
                        type={isVisible ? "text" : "password"}
                        className={className}
                        style={finalStyle}
                        onFocus={e => {
                            if (!className) {
                                e.currentTarget.style.borderColor = "var(--border-focus)";
                                e.currentTarget.style.boxShadow = "0 0 0 3px rgba(59,130,246,0.1)";
                            }
                            userOnFocus?.(e);
                        }}
                        onBlur={e => {
                            if (!className) {
                                e.currentTarget.style.borderColor = error
                                    ? "var(--error)"
                                    : "var(--border)";
                                e.currentTarget.style.boxShadow = "none";
                            }
                            userOnBlur?.(e);
                        }}
                        {...rest}
                    />
                    <button
                        type="button"
                        onClick={() => setIsVisible(v => !v)}
                        aria-label={
                            isVisible ? "Masquer le mot de passe" : "Afficher le mot de passe"
                        }
                        aria-pressed={isVisible}
                        tabIndex={-1}
                        style={{
                            position: "absolute",
                            right: 8,
                            top: "50%",
                            transform: "translateY(-50%)",
                            background: "transparent",
                            border: "none",
                            cursor: "pointer",
                            padding: 6,
                            minWidth: 36,
                            minHeight: 36,
                            display: "flex",
                            alignItems: "center",
                            justifyContent: "center",
                            color: "var(--text-muted)",
                            borderRadius: 6,
                            transition: "background-color 0.15s, color 0.15s",
                        }}
                        onMouseEnter={e => {
                            e.currentTarget.style.backgroundColor = "rgba(15,23,42,0.06)";
                            e.currentTarget.style.color = "var(--primary)";
                        }}
                        onMouseLeave={e => {
                            e.currentTarget.style.backgroundColor = "transparent";
                            e.currentTarget.style.color = "var(--text-muted)";
                        }}
                    >
                        {isVisible ? (
                            <EyeOff size={18} strokeWidth={1.75} aria-hidden="true" />
                        ) : (
                            <Eye size={18} strokeWidth={1.75} aria-hidden="true" />
                        )}
                    </button>
                </div>
                {error && (
                    <div style={{ marginTop: 6, fontSize: 13, color: "var(--error)" }}>
                        {error}
                    </div>
                )}
            </div>
        );
    },
);
