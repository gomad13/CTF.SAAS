import type { Metadata } from "next";

export const metadata: Metadata = {
    title: "Conditions générales d'utilisation — Sentys",
    description: "CGU bêta privée Sentys",
};

const LAST_UPDATE = "27 avril 2026";

export default function CguPage() {
    return (
        <article>
            <h1 style={{ fontSize: 28, fontWeight: 700, marginBottom: 8 }}>Conditions générales d&apos;utilisation</h1>
            <p style={{ color: "var(--text-muted, #94A3B8)", marginBottom: 32, fontSize: 13 }}>
                Dernière mise à jour : {LAST_UPDATE} — version bêta privée
            </p>

            <h2>1. Objet</h2>
            <p>
                Les présentes conditions régissent l&apos;accès et l&apos;usage de la plateforme Sentys (« la Plateforme »),
                un service SaaS de formation et sensibilisation à la cybersécurité destiné aux organisations professionnelles.
                La Plateforme est actuellement proposée en <strong>bêta privée</strong>, à un nombre restreint d&apos;organisations
                partenaires, à titre <strong>gratuit</strong> et sur invitation uniquement.
            </p>

            <h2>2. Phase bêta — absence de garantie</h2>
            <p>
                La Plateforme est en phase de test. Elle peut comporter des bugs, des indisponibilités ou des évolutions
                non rétro-compatibles. Aucun engagement de niveau de service (SLA) n&apos;est fourni durant cette phase.
                Les contenus, fonctionnalités et données peuvent évoluer ou être réinitialisés à tout moment.
            </p>

            <h2>3. Création de compte</h2>
            <p>
                L&apos;accès à la Plateforme nécessite la création d&apos;un compte par un administrateur de l&apos;organisation
                cliente, ou via un code d&apos;invitation fourni par l&apos;équipe Sentys.
                L&apos;utilisateur s&apos;engage à fournir des informations exactes et à conserver la confidentialité de ses
                identifiants. Tout usage du compte est réputé fait par son titulaire.
            </p>

            <h2>4. Usage autorisé</h2>
            <p>
                L&apos;utilisateur s&apos;engage à utiliser la Plateforme conformément à sa destination — formation et sensibilisation
                cyber — et à ne pas :
            </p>
            <ul>
                <li>tenter de contourner les mécanismes d&apos;authentification ou d&apos;autorisation ;</li>
                <li>injecter du contenu illégal, diffamatoire ou contrefait ;</li>
                <li>opérer du reverse-engineering ou tenter d&apos;extraire les données d&apos;autres organisations ;</li>
                <li>utiliser la Plateforme pour conduire des attaques réelles ou non-autorisées vis-à-vis de tiers.</li>
            </ul>

            <h2>5. Propriété intellectuelle</h2>
            <p>
                Les contenus de formation (parcours, modules, scénarios, simulations) restent la propriété exclusive de Sentys.
                L&apos;utilisateur dispose d&apos;un droit d&apos;accès non-transférable, limité à la durée de la phase bêta et au
                périmètre de son organisation.
            </p>

            <h2>6. Données personnelles</h2>
            <p>
                Le traitement des données personnelles est décrit dans la <a href="/privacy">Politique de confidentialité</a>,
                laquelle fait partie intégrante des présentes CGU.
            </p>

            <h2>7. Limitation de responsabilité</h2>
            <p>
                Dans toute la mesure permise par le droit applicable, Sentys ne saurait être tenu responsable des dommages
                indirects, immatériels ou consécutifs (perte de données, perte d&apos;exploitation, manque à gagner)
                liés à l&apos;usage de la Plateforme en phase bêta. La responsabilité globale de Sentys, toutes causes confondues,
                est limitée au montant effectivement versé par l&apos;utilisateur — soit zéro pour la bêta privée gratuite.
            </p>

            <h2>8. Suspension &amp; résiliation</h2>
            <p>
                Sentys peut suspendre ou clôturer un compte en cas de non-respect des présentes CGU, sans préavis.
                L&apos;utilisateur peut demander la clôture de son compte à tout moment via{" "}
                <a href="mailto:contact@sentys.local?subject=Suppression%20de%20compte">contact@sentys.local</a>.
            </p>

            <h2>9. Loi applicable &amp; juridiction</h2>
            <p>
                Les présentes CGU sont régies par le droit français. Tout litige relatif à leur interprétation ou exécution
                relève de la compétence exclusive des tribunaux français, après tentative de résolution amiable.
            </p>

            <h2>10. Évolution des CGU</h2>
            <p>
                Les CGU peuvent être modifiées. La version en vigueur est celle accessible sur la Plateforme à la date d&apos;usage.
                Les modifications substantielles seront notifiées aux utilisateurs.
            </p>

            <p style={{ fontSize: 13, color: "var(--text-muted, #94A3B8)", marginTop: 32, fontStyle: "italic" }}>
                Document à valeur indicative — bêta privée. Une version V1 publique sera revue par un conseil juridique
                avant le passage en commercialisation.
            </p>
        </article>
    );
}
