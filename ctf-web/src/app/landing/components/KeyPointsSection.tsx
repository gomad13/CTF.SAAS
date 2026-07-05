export default function KeyPointsSection() {
    return (
        <section style={{
            background: "linear-gradient(180deg, transparent, color-mix(in srgb, var(--accent) 4%, transparent), transparent)",
            padding: "clamp(48px, 9vw, 80px) var(--page-x)",
            borderTop: "1px solid color-mix(in srgb, var(--accent) 8%, transparent)",
            borderBottom: "1px solid color-mix(in srgb, var(--accent) 8%, transparent)",
        }}>
            <div style={{
                maxWidth: 1100,
                margin: "0 auto",
                display: "grid",
                gridTemplateColumns: "repeat(auto-fit, minmax(280px, 1fr))",
                gap: 32,
            }}>
                {/* Point 1 */}
                <KeyPoint
                    icon={<IconShield />}
                    value="100%"
                    label="DE VOS SCÉNARIOS BASÉS SUR DES ATTAQUES RÉELLES"
                    sub="Phishing, ingénierie sociale, fuite de données"
                />

                {/* Point 2 */}
                <KeyPoint
                    icon={<IconChart />}
                    value="3x"
                    label="PLUS EFFICACE QUE LA FORMATION THÉORIQUE"
                    sub="L'entraînement pratique ancre durablement les réflexes"
                />

                {/* Point 3 */}
                <KeyPoint
                    icon={<IconTeam />}
                    value="Multi"
                    label="NIVEAUX — DU DÉBUTANT À L'EXPERT"
                    sub="Parcours adaptés à chaque profil de collaborateur"
                />
            </div>
        </section>
    );
}

function KeyPoint({ icon, value, label, sub }: {
    icon: React.ReactNode;
    value: string;
    label: string;
    sub: string;
}) {
    return (
        <div style={{ textAlign: "center", padding: "16px 8px" }}>
            <div style={{ display: "flex", justifyContent: "center", marginBottom: 16 }}>
                {icon}
            </div>
            <div style={{
                fontFamily: "'JetBrains Mono', monospace",
                fontWeight: 800,
                fontSize: 42,
                color: "var(--pr)",
                textShadow: "0 0 20px color-mix(in srgb, var(--accent) 40%, transparent)",
                lineHeight: 1,
                marginBottom: 10,
            }}>
                {value}
            </div>
            <div style={{
                fontSize: 11,
                letterSpacing: "0.1em",
                color: "var(--text-3)",
                fontFamily: "'JetBrains Mono', monospace",
                marginBottom: 8,
            }}>
                {label}
            </div>
            <div style={{
                fontSize: 13,
                color: "var(--text-2)",
                lineHeight: 1.5,
            }}>
                {sub}
            </div>
        </div>
    );
}

function IconShield() {
    return (
        <svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="var(--pr)" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
            <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z" />
            <path d="M9 12l2 2 4-4" />
        </svg>
    );
}

function IconChart() {
    return (
        <svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="var(--pr)" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
            <path d="M3 3v18h18" />
            <path d="M18 17V9" />
            <path d="M13 17V5" />
            <path d="M8 17v-3" />
        </svg>
    );
}

function IconTeam() {
    return (
        <svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="var(--pr)" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
            <path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2" />
            <circle cx="9" cy="7" r="4" />
            <path d="M22 21v-2a4 4 0 0 0-3-3.87" />
            <path d="M16 3.13a4 4 0 0 1 0 7.75" />
        </svg>
    );
}
