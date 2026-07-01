const PROFILES = [
    {
        title: "Responsables RH & Formation",
        description: "Intégrez la sensibilisation cyber dans vos programmes d'onboarding et de formation continue sans expertise technique.",
    },
    {
        title: "DSI & RSSI",
        description: "Réduisez la surface d'attaque humaine de votre organisation avec des métriques claires et des parcours ciblés.",
    },
    {
        title: "Directions Générales",
        description: "Répondez aux exigences de conformité (NIS2, RGPD) et démontrez l'engagement de votre organisation en matière de cybersécurité.",
    },
] as const;

export default function AudienceSection() {
    return (
        <section id="audience" style={{
            background: "rgba(59,130,246,0.03)",
            padding: "clamp(56px, 10vw, 100px) var(--page-x)",
        }}>
            <div style={{ maxWidth: 1100, margin: "0 auto" }}>
                <div style={{ textAlign: "center", marginBottom: 56 }}>
                    <h2 style={{
                        fontSize: "clamp(26px, 4vw, 36px)",
                        fontWeight: 700,
                        color: "#F1F5F9",
                        maxWidth: 700,
                        margin: "0 auto",
                    }}>
                        Conçu pour les entreprises qui prennent la cybersécurité au sérieux
                    </h2>
                </div>

                <div style={{
                    display: "grid",
                    gridTemplateColumns: "repeat(auto-fit, minmax(280px, 1fr))",
                    gap: 24,
                }}>
                    {PROFILES.map((p) => (
                        <div key={p.title} style={{
                            background: "var(--bg-card)",
                            borderLeft: "3px solid #3B82F6",
                            borderRadius: "0 10px 10px 0",
                            padding: 24,
                        }}>
                            <h3 style={{
                                fontSize: 17,
                                fontWeight: 600,
                                color: "#1E293B",
                                marginBottom: 10,
                            }}>
                                {p.title}
                            </h3>
                            <p style={{
                                fontSize: 14,
                                color: "#334155",
                                lineHeight: 1.65,
                            }}>
                                {p.description}
                            </p>
                        </div>
                    ))}
                </div>
            </div>
        </section>
    );
}
