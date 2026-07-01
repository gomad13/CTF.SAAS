export default function Footer() {
    return (
        <footer style={{
            background: "#0F172A",
            borderTop: "1px solid rgba(59,130,246,0.08)",
            padding: "32px var(--page-x)",
        }}>
            <div style={{
                maxWidth: 1200,
                margin: "0 auto",
                display: "flex",
                flexWrap: "wrap",
                justifyContent: "space-between",
                alignItems: "center",
                gap: 16,
            }}>
                {/* Gauche */}
                <span style={{
                    fontSize: 12,
                    color: "#94A3B8",
                    fontFamily: "'JetBrains Mono', monospace",
                }}>
                    &copy; 2026 Sentys — Tous droits réservés
                </span>

                {/* Centre — liens */}
                <nav style={{ display: "flex", gap: 24 }}>
                    <a href="#" className="footer-nav-link" style={{ fontSize: 12 }}>
                        Mentions légales
                    </a>
                    <a href="#" className="footer-nav-link" style={{ fontSize: 12 }}>
                        Politique de confidentialité
                    </a>
                </nav>

                {/* Droite */}
                <span style={{
                    fontSize: 12,
                    color: "#94A3B8",
                    fontFamily: "'JetBrains Mono', monospace",
                }}>
                    Fait avec ◈ pour la cybersécurité
                </span>
            </div>
        </footer>
    );
}
