import Link from "next/link";

export default function CtaSection() {
    return (
        <section id="contact" style={{
            background: "radial-gradient(ellipse at center, color-mix(in srgb, var(--accent) 6%, transparent) 0%, transparent 70%)",
            padding: "clamp(56px, 10vw, 100px) var(--page-x)",
            textAlign: "center",
        }}>
            <div style={{ maxWidth: 600, margin: "0 auto" }}>
                <h2 style={{
                    fontSize: "clamp(26px, 4.5vw, 38px)",
                    fontWeight: 700,
                    color: "var(--text)",
                    lineHeight: 1.15,
                    marginBottom: 16,
                }}>
                    Prêt à renforcer la résilience cyber de vos équipes ?
                </h2>
                <p style={{
                    color: "var(--text-2)",
                    fontSize: 16,
                    lineHeight: 1.7,
                    marginBottom: 36,
                }}>
                    Créez votre compte et accédez immédiatement
                    au parcours de démonstration — sans engagement.
                </p>

                <Link href="/login" className="transition-transform duration-200 hover:-translate-y-0.5" style={{
                    display: "inline-block",
                    padding: "16px 40px",
                    fontSize: 16,
                    background: "linear-gradient(135deg, var(--accent), var(--accent-hover))",
                    color: "var(--on-accent)",
                    fontWeight: 700,
                    borderRadius: 8,
                    textDecoration: "none",
                    boxShadow: "0 0 20px color-mix(in srgb, var(--accent) 40%, transparent)",
                }}>
                    Démarrer la démonstration →
                </Link>

                <p style={{
                    fontSize: 12,
                    color: "var(--text-3)",
                    fontFamily: "'JetBrains Mono', monospace",
                    marginTop: 20,
                }}>
                    ✓ Accès immédiat &nbsp; ✓ Sans carte bancaire &nbsp; ✓ Sans engagement
                </p>
            </div>
        </section>
    );
}
