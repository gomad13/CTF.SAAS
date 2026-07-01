type Props = {
    icon: string;
    value: string | number;
    label: string;
    sub?: string;
    accent?: boolean;
};

export default function StatsCard({ icon, value, label, sub }: Props) {
    return (
        <div style={{
            background: "var(--bg-card)",
            border: "1px solid var(--border)",
            borderRadius: 10,
            padding: 20,
        }}>
            <div style={{ fontSize: 20, marginBottom: 8 }}>{icon}</div>
            <div style={{
                fontSize: 28,
                fontWeight: 700,
                fontFamily: "'JetBrains Mono', monospace",
                color: "var(--primary)",
            }}>
                {value}
            </div>
            <div style={{
                fontSize: 11,
                letterSpacing: "0.1em",
                textTransform: "uppercase",
                color: "var(--text-muted)",
                marginTop: 4,
            }}>
                {label}
            </div>
            {sub && <div style={{ fontSize: 11, color: "var(--text-muted)", marginTop: 4 }}>{sub}</div>}
        </div>
    );
}
