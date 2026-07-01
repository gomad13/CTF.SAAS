import Link from "next/link";

export default function CtaSection() {
    return (
        <section id="contact" style={{
            background: "radial-gradient(ellipse at center, rgba(59,130,246,0.06) 0%, transparent 70%)",
            padding: "clamp(56px, 10vw, 100px) var(--page-x)",
            textAlign: "center",
        }}>
            <div style={{ maxWidth: 600, margin: "0 auto" }}>
                <h2 style={{
                    fontSize: "clamp(26px, 4.5vw, 38px)",
                    fontWeight: 700,
                    color: "#F1F5F9",
                    lineHeight: 1.15,
                    marginBottom: 16,
                }}>
                    Prêt à renforcer la résilience cyber de vos équipes ?
                </h2>
                <p style={{
                    color: "#CBD5E1",
                    fontSize: 16,
                    lineHeight: 1.7,
                    marginBottom: 36,
                }}>
                    Créez votre compte et accédez immédiatement
                    au parcours de démonstration — sans engagement.
                </p>

                <Link href="/login" style={{
                    display: "inline-block",
                    padding: "16px 40px",
                    fontSize: 16,
                    background: "linear-gradient(135deg, #3B82F6, #2563EB)",
                    color: "#FFFFFF",
                    fontWeight: 700,
                    borderRadius: 8,
                    textDecoration: "none",
                    boxShadow: "0 0 20px rgba(59,130,246,0.4)",
                }}>
                    Démarrer la démonstration →
                </Link>

                <p style={{
                    fontSize: 12,
                    color: "#94A3B8",
                    fontFamily: "'JetBrains Mono', monospace",
                    marginTop: 20,
                }}>
                    ✓ Accès immédiat &nbsp; ✓ Sans carte bancaire &nbsp; ✓ Sans engagement
                </p>
            </div>
        </section>
    );
}
