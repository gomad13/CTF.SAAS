import Link from "next/link";

export default function Footer({ compact = false }: { compact?: boolean }) {
    return (
        <footer
            style={{
                marginTop: compact ? 16 : 32,
                padding: compact ? "14px 20px" : "20px 24px",
                borderTop: "1px solid rgba(255,255,255,0.06)",
                background: "transparent",
                color: "#94A3B8",
                fontSize: 12,
                lineHeight: 1.6,
                display: "flex",
                flexWrap: "wrap",
                gap: 12,
                justifyContent: "space-between",
                alignItems: "center",
            }}
        >
            <span>© 2026 Viper — Bêta privée · Plateforme hébergée en France 🇫🇷</span>
            <nav style={{ display: "flex", gap: 14, flexWrap: "wrap" }} aria-label="Liens légaux">
                <Link href="/cgu" style={linkStyle()}>CGU</Link>
                <Link href="/privacy" style={linkStyle()}>Confidentialité</Link>
                <Link href="/mentions-legales" style={linkStyle()}>Mentions légales</Link>
                <Link href="/feedback" style={linkStyle()}>Feedback</Link>
            </nav>
        </footer>
    );
}

function linkStyle(): React.CSSProperties {
    return {
        color: "#CBD5E1",
        textDecoration: "none",
        transition: "color 0.15s",
    };
}
