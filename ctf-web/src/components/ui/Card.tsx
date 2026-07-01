import { ReactNode, CSSProperties } from "react";

export default function Card({ children, padding = 24, hover = false, style = {} }: {
    children: ReactNode;
    padding?: number;
    hover?: boolean;
    style?: CSSProperties;
}) {
    return (
        <div
            style={{
                background: "var(--c-bg-2)",
                border: "1px solid var(--c-border)",
                borderRadius: "var(--r-lg)",
                padding,
                boxShadow: "var(--sh-card)",
                transition: hover ? "border-color var(--t-base), box-shadow var(--t-base), transform var(--t-base)" : "none",
                ...style,
            }}
            onMouseEnter={hover ? e => {
                e.currentTarget.style.borderColor = "var(--c-border-h)";
                e.currentTarget.style.boxShadow = "var(--sh-md)";
                e.currentTarget.style.transform = "translateY(-1px)";
            } : undefined}
            onMouseLeave={hover ? e => {
                e.currentTarget.style.borderColor = "var(--c-border)";
                e.currentTarget.style.boxShadow = "var(--sh-card)";
                e.currentTarget.style.transform = "translateY(0)";
            } : undefined}
        >
            {children}
        </div>
    );
}
