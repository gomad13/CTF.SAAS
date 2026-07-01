type Props = {
    percent: number;
    className?: string;
    showLabel?: boolean;
};

export default function ProgressBar({ percent, className = "", showLabel = false }: Props) {
    const clamped = Math.max(0, Math.min(100, percent));

    return (
        <div className={className} style={{ display: "flex", flexDirection: "column", gap: 4 }}>
            {showLabel && (
                <div style={{ display: "flex", justifyContent: "space-between", fontSize: 11, color: "var(--text-muted)" }}>
                    <span>Progression</span>
                    <span style={{ fontFamily: "'JetBrains Mono', monospace", color: "var(--text-primary)" }}>{clamped}%</span>
                </div>
            )}
            <div style={{ height: 3, background: "rgba(59,130,246,0.1)", borderRadius: 2, overflow: "hidden" }}>
                <div style={{
                    height: "100%",
                    width: `${clamped}%`,
                    background: "linear-gradient(90deg, var(--primary-dark), var(--primary))",
                    boxShadow: "0 0 6px rgba(59,130,246,0.4)",
                    borderRadius: 2,
                    transition: "width 0.5s",
                }} />
            </div>
        </div>
    );
}
