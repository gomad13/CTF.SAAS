"use client";

export type BadgeVariant = "default" | "primary" | "success" | "error" | "warning" | "ghost";

const styles: Record<BadgeVariant, { bg: string; color: string; border: string }> = {
    default: { bg: "var(--c-bg-4)", color: "var(--c-tx-2)", border: "var(--c-border)" },
    primary: { bg: "var(--c-primary-bg)", color: "var(--c-primary-l)", border: "var(--c-primary-b)" },
    success: { bg: "var(--c-success-bg)", color: "var(--c-success-t)", border: "var(--c-success-b)" },
    error:   { bg: "var(--c-error-bg)", color: "var(--c-error-t)", border: "var(--c-error-b)" },
    warning: { bg: "var(--c-warning-bg)", color: "var(--c-warning-t)", border: "var(--c-warning-b)" },
    ghost:   { bg: "transparent", color: "var(--c-tx-3)", border: "var(--c-border)" },
};

export default function Badge({ children, variant = "default", dot = false }: {
    children: React.ReactNode;
    variant?: BadgeVariant;
    dot?: boolean;
}) {
    const s = styles[variant];
    return (
        <span style={{
            display: "inline-flex",
            alignItems: "center",
            gap: 4,
            padding: "2px 8px",
            borderRadius: "var(--r-full)",
            fontSize: "11.5px",
            fontWeight: 500,
            letterSpacing: "0.01em",
            background: s.bg,
            color: s.color,
            border: `1px solid ${s.border}`,
            whiteSpace: "nowrap",
        }}>
            {dot && (
                <span style={{
                    width: 5,
                    height: 5,
                    borderRadius: "50%",
                    background: s.color,
                    flexShrink: 0,
                }} />
            )}
            {children}
        </span>
    );
}
