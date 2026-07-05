const FEATURES = [
    {
        icon: <IconPhishing />,
        title: "Scénarios de phishing réalistes",
        description: "Vos collaborateurs analysent de vrais emails frauduleux et apprennent à identifier les signaux d'alerte.",
    },
    {
        icon: <IconAnalytics />,
        title: "Analyse comportementale",
        description: "Identifiez les lacunes de vos équipes et ciblez les formations prioritaires.",
    },
    {
        icon: <IconOrg />,
        title: "Gestion multi-équipes",
        description: "Assignez des parcours par département, suivez la progression de chaque collaborateur.",
    },
    {
        icon: <IconDashboard />,
        title: "Tableau de bord manager",
        description: "Vue consolidée des performances, exports PDF, rapports de conformité.",
    },
    {
        icon: <IconShieldLaw />,
        title: "Modules RGPD & réglementation",
        description: "Sensibilisez vos équipes aux obligations légales et aux bonnes pratiques de protection des données.",
    },
    {
        icon: <IconProgress />,
        title: "Suivi de progression individuel",
        description: "Chaque collaborateur avance à son rythme avec des corrections immédiates après chaque exercice.",
    },
] as const;

export default function FeaturesSection() {
    return (
        <section id="features" style={{ padding: "clamp(56px, 10vw, 100px) var(--page-x)" }}>
            <div style={{ maxWidth: 1100, margin: "0 auto" }}>
                {/* Header */}
                <div style={{ textAlign: "center", marginBottom: 56 }}>
                    <span style={{
                        display: "inline-block",
                        fontSize: 11,
                        fontWeight: 600,
                        textTransform: "uppercase",
                        letterSpacing: "0.14em",
                        color: "var(--pr)",
                        fontFamily: "'JetBrains Mono', monospace",
                        background: "color-mix(in srgb, var(--accent) 7%, transparent)",
                        border: "1px solid color-mix(in srgb, var(--accent) 22%, transparent)",
                        borderRadius: 20,
                        padding: "4px 14px",
                        marginBottom: 16,
                    }}>
                        FONCTIONNALITÉS
                    </span>
                    <h2 style={{
                        fontSize: "clamp(26px, 4vw, 36px)",
                        fontWeight: 700,
                        color: "var(--text)",
                        marginTop: 16,
                    }}>
                        Une plateforme complète pour votre organisation
                    </h2>
                </div>

                {/* Cards */}
                <div style={{
                    display: "grid",
                    gridTemplateColumns: "repeat(auto-fit, minmax(min(100%, 280px), 1fr))",
                    gap: 20,
                }}>
                    {FEATURES.map((f) => (
                        <FeatureCard key={f.title} icon={f.icon} title={f.title} description={f.description} />
                    ))}
                </div>
            </div>
        </section>
    );
}

function FeatureCard({ icon, title, description }: { icon: React.ReactNode; title: string; description: string }) {
    return (
        <div style={{
            background: "var(--surface)",
            border: "1px solid var(--border)",
            borderRadius: 12,
            padding: "28px 24px",
            position: "relative",
            overflow: "hidden",
            boxShadow: "0 1px 3px rgba(0,0,0,0.04)",
        }}>
            {/* Coin décoratif */}
            <div aria-hidden style={{
                position: "absolute",
                top: 0,
                right: 0,
                width: 36,
                height: 36,
                background: "linear-gradient(225deg, color-mix(in srgb, var(--accent) 20%, transparent) 0%, transparent 60%)",
                clipPath: "polygon(100% 0, 0 0, 100% 100%)",
            }} />

            <div style={{
                width: 40,
                height: 40,
                display: "flex",
                alignItems: "center",
                justifyContent: "center",
                borderRadius: 8,
                background: "color-mix(in srgb, var(--accent) 8%, transparent)",
                border: "1px solid color-mix(in srgb, var(--accent) 18%, transparent)",
                color: "var(--pr)",
            }}>
                {icon}
            </div>

            <h3 style={{
                fontSize: 16,
                fontWeight: 600,
                color: "var(--text)",
                marginTop: 14,
            }}>
                {title}
            </h3>
            <p style={{
                fontSize: 14,
                color: "var(--text-2)",
                lineHeight: 1.65,
                marginTop: 8,
            }}>
                {description}
            </p>
        </div>
    );
}

function IconPhishing() {
    return (
        <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
            <rect x="2" y="4" width="20" height="16" rx="2" />
            <path d="M22 7l-8.97 5.7a1.94 1.94 0 0 1-2.06 0L2 7" />
        </svg>
    );
}

function IconAnalytics() {
    return (
        <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
            <path d="M2 12s3-7 10-7 10 7 10 7-3 7-10 7-10-7-10-7Z" />
            <circle cx="12" cy="12" r="3" />
        </svg>
    );
}

function IconOrg() {
    return (
        <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
            <path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2" />
            <circle cx="9" cy="7" r="4" />
            <path d="M22 21v-2a4 4 0 0 0-3-3.87" />
            <path d="M16 3.13a4 4 0 0 1 0 7.75" />
        </svg>
    );
}

function IconDashboard() {
    return (
        <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
            <rect x="2" y="3" width="7" height="9" rx="1" />
            <rect x="15" y="3" width="7" height="5" rx="1" />
            <rect x="15" y="12" width="7" height="9" rx="1" />
            <rect x="2" y="16" width="7" height="5" rx="1" />
        </svg>
    );
}

function IconShieldLaw() {
    return (
        <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
            <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z" />
            <path d="M8 11h8" />
            <path d="M8 15h5" />
        </svg>
    );
}

function IconProgress() {
    return (
        <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
            <path d="M22 12h-4l-3 9L9 3l-3 9H2" />
        </svg>
    );
}
