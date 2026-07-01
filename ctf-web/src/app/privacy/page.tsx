import type { Metadata } from "next";

export const metadata: Metadata = {
    title: "Politique de confidentialité — Sentys",
    description: "Politique de confidentialité et traitement RGPD des données Sentys",
};

const LAST_UPDATE = "27 avril 2026";

export default function PrivacyPage() {
    return (
        <article>
            <h1 style={{ fontSize: 28, fontWeight: 700, marginBottom: 8 }}>Politique de confidentialité</h1>
            <p style={{ color: "var(--text-muted, #94A3B8)", marginBottom: 32, fontSize: 13 }}>
                Dernière mise à jour : {LAST_UPDATE} — conforme RGPD
            </p>

            <h2>1. Responsable du traitement</h2>
            <p>
                Le responsable du traitement des données personnelles est <strong>Sentys</strong>, projet entrepreneurial
                porté par <strong>Hamid Madoumier</strong>. Pour toute question relative à vos données :{" "}
                <a href="mailto:contact@sentys.local">contact@sentys.local</a>.
            </p>

            <h2>2. Données collectées</h2>
            <p>Dans le cadre de l&apos;usage de la Plateforme, nous collectons :</p>
            <ul>
                <li><strong>Identité &amp; contact</strong> : prénom, nom, adresse email professionnelle.</li>
                <li><strong>Tenant d&apos;appartenance</strong> : organisation cliente associée à votre compte.</li>
                <li><strong>Authentification</strong> : mot de passe (stocké sous forme de hash bcrypt), jetons de session.</li>
                <li><strong>Activité de formation</strong> : parcours assignés, soumissions aux challenges, scores, progression, dates.</li>
                <li><strong>Données techniques</strong> : adresse IP, user-agent, journaux d&apos;audit anonymisés.</li>
            </ul>
            <p>
                Aucune donnée sensible au sens de l&apos;article 9 du RGPD (santé, opinions, orientation, etc.) n&apos;est collectée.
            </p>

            <h2>3. Finalités du traitement</h2>
            <ul>
                <li>Fournir l&apos;accès au service de formation cyber.</li>
                <li>Suivre la progression individuelle et agrégée par équipe / tenant.</li>
                <li>Sécuriser l&apos;accès (détection des tentatives anormales, audit log).</li>
                <li>Améliorer la qualité du produit en phase bêta (analyse anonymisée des erreurs).</li>
            </ul>

            <h2>4. Base légale</h2>
            <p>
                Les traitements reposent sur l&apos;exécution du service souscrit par l&apos;organisation cliente
                (article 6.1.b RGPD), ainsi que sur l&apos;intérêt légitime de Sentys à sécuriser et améliorer la Plateforme
                (article 6.1.f).
            </p>

            <h2>5. Hébergement &amp; localisation</h2>
            <p>
                Pendant la phase bêta privée, la Plateforme est hébergée en environnement de pré-production en France.
                À l&apos;issue de la bêta, l&apos;infrastructure cible est <strong>Scaleway (Paris)</strong>.
                Aucune donnée n&apos;est transférée hors de l&apos;Espace économique européen.
            </p>

            <h2>6. Durée de conservation</h2>
            <ul>
                <li><strong>Compte actif</strong> : pendant toute la durée de la relation.</li>
                <li><strong>Compte clôturé</strong> : suppression sous 30 jours à compter de la demande.</li>
                <li><strong>Logs d&apos;authentification</strong> : 12 mois (obligation légale article L34-1 CPCE).</li>
                <li><strong>Données de formation anonymisées</strong> : conservées pour analyse statistique.</li>
            </ul>

            <h2>7. Destinataires</h2>
            <p>
                Vos données ne sont accessibles qu&apos;à :
            </p>
            <ul>
                <li>Vous-même via votre compte.</li>
                <li>Les administrateurs de votre organisation (rôle <code>admin</code>).</li>
                <li>L&apos;équipe Sentys (rôle <code>SuperAdmin</code>) pour le support et la maintenance technique.</li>
            </ul>
            <p>
                Aucune donnée n&apos;est revendue, partagée à des fins publicitaires, ni transmise à des tiers en dehors
                des sous-traitants techniques (hébergeur).
            </p>

            <h2>8. Cookies</h2>
            <p>
                La Plateforme utilise des cookies <strong>strictement nécessaires</strong> à son fonctionnement
                (authentification, session). Ces cookies sont posés par le serveur en HttpOnly et SameSite=Lax,
                sans tracking publicitaire.
                Aucun cookie tiers de mesure d&apos;audience n&apos;est utilisé en bêta privée.
            </p>

            <h2>9. Vos droits</h2>
            <p>Conformément au RGPD, vous disposez des droits suivants :</p>
            <ul>
                <li><strong>Accès</strong> : obtenir une copie de vos données (exportable depuis Paramètres → Profil).</li>
                <li><strong>Rectification</strong> : modifier vos informations personnelles.</li>
                <li><strong>Suppression</strong> (« droit à l&apos;oubli ») : demande à{" "}
                    <a href="mailto:contact@sentys.local?subject=Suppression%20de%20compte">contact@sentys.local</a>.
                </li>
                <li><strong>Portabilité</strong> : récupérer vos données dans un format structuré.</li>
                <li><strong>Opposition / limitation</strong> : pour les traitements fondés sur l&apos;intérêt légitime.</li>
                <li><strong>Réclamation auprès de la CNIL</strong> : <a href="https://www.cnil.fr" rel="noopener" target="_blank">cnil.fr</a>.</li>
            </ul>

            <h2>10. Sécurité</h2>
            <p>
                La Plateforme applique les mesures techniques suivantes : chiffrement TLS, hash bcrypt des mots de passe,
                JWT en cookie HttpOnly, isolation multi-tenant en base (Row-Level Security PostgreSQL),
                journalisation des accès administrateurs, headers HTTP de sécurité (CSP, HSTS, X-Frame-Options).
            </p>

            <p style={{ fontSize: 13, color: "var(--text-muted, #94A3B8)", marginTop: 32, fontStyle: "italic" }}>
                Document à valeur indicative — bêta privée. Une version V1 publique sera revue par un conseil juridique
                spécialisé RGPD avant la commercialisation.
            </p>
        </article>
    );
}
