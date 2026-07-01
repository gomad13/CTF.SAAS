import type { Metadata } from "next";

export const metadata: Metadata = {
    title: "Mentions légales — Sentys",
    description: "Mentions légales du site Sentys",
};

export default function MentionsPage() {
    return (
        <article>
            <h1 style={{ fontSize: 28, fontWeight: 700, marginBottom: 32 }}>Mentions légales</h1>

            <h2>Éditeur</h2>
            <p>
                <strong>Projet Sentys</strong> — porté à titre individuel par <strong>Hamid Madoumier</strong>.<br />
                Statut : projet entrepreneurial en phase bêta privée.<br />
                Email : <a href="mailto:contact@sentys.local">contact@sentys.local</a>.
            </p>

            <h2>Directeur de la publication</h2>
            <p>Hamid Madoumier.</p>

            <h2>Hébergement</h2>
            <p>
                Pendant la phase bêta : environnement de pré-production hébergé en France.<br />
                Cible production : <strong>Scaleway SAS</strong>, 8 rue de la Ville l&apos;Évêque, 75008 Paris,
                France — <a href="https://www.scaleway.com" rel="noopener" target="_blank">scaleway.com</a>.
            </p>

            <h2>Propriété intellectuelle</h2>
            <p>
                L&apos;ensemble des contenus présents sur la Plateforme (textes, scénarios pédagogiques, illustrations,
                interfaces) est la propriété exclusive de Sentys, sauf mention contraire. Toute reproduction sans
                autorisation écrite est interdite.
            </p>

            <h2>Crédits</h2>
            <p>
                Icônes : <a href="https://lucide.dev" rel="noopener" target="_blank">Lucide</a> (licence ISC).<br />
                Police d&apos;écriture : <a href="https://fonts.google.com/specimen/Inter" rel="noopener" target="_blank">Inter</a> (licence SIL Open Font).
            </p>

            <h2>Contact</h2>
            <p>
                Pour toute question relative à la Plateforme :{" "}
                <a href="mailto:contact@sentys.local">contact@sentys.local</a>.
            </p>
        </article>
    );
}
