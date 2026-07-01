"use client";

import { useMemo } from "react";
import ConsentCheckbox from "./ConsentCheckbox";
import type { LegalDocumentSummary } from "@/lib/types/legal";

export type RegistrationConsentsState = {
    politiqueConfidentialite: boolean;
    cgu: boolean;
    dpa: boolean;
    majeurUsageProfessionnel: boolean;
    newsletter: boolean;
    commercial: boolean;
};

export const initialConsentsState: RegistrationConsentsState = {
    politiqueConfidentialite: false,
    cgu: false,
    dpa: false,
    majeurUsageProfessionnel: false,
    newsletter: false,
    commercial: false,
};

type Props = {
    documents: LegalDocumentSummary[];
    isAdmin: boolean;
    state: RegistrationConsentsState;
    onChange: (next: RegistrationConsentsState) => void;
    showErrors?: boolean;
};

/**
 * Bloc consentements pour la page d'inscription. 4 obligatoires (3 si user
 * simple, le DPA n'apparaît que pour un compte administrateur/décideur),
 * 2 optionnels (newsletter, commercial). Les versions des documents sont
 * récupérées dynamiquement via `documents` (hook useLegalDocuments).
 */
export default function ConsentSection({ documents, isAdmin, state, onChange, showErrors }: Props) {
    const docBySlug = useMemo(() => {
        const m = new Map<string, LegalDocumentSummary>();
        for (const d of documents) m.set(d.slug, d);
        return m;
    }, [documents]);

    const set = <K extends keyof RegistrationConsentsState>(key: K, value: boolean) =>
        onChange({ ...state, [key]: value });

    const errPolitique = showErrors && !state.politiqueConfidentialite ? "Ce consentement est obligatoire." : null;
    const errCgu = showErrors && !state.cgu ? "Ce consentement est obligatoire." : null;
    const errDpa = showErrors && isAdmin && !state.dpa ? "Ce consentement est obligatoire pour les administrateurs." : null;
    const errMajeur = showErrors && !state.majeurUsageProfessionnel ? "Ce consentement est obligatoire." : null;

    const polTitle = docBySlug.get("politique-confidentialite")?.title ?? "Politique de Confidentialité";
    const cguTitle = docBySlug.get("cgu")?.title ?? "Conditions Générales d'Utilisation";
    const dpaTitle = docBySlug.get("dpa")?.title ?? "Accord de Traitement des Données";

    return (
        <fieldset style={{
            border: "1px solid var(--bd, #E2E8F0)", borderRadius: 10, padding: 16,
            margin: "16px 0", background: "var(--bg-card, #ffffff05)",
        }}>
            <legend style={{ padding: "0 8px", fontSize: 12, fontWeight: 600, color: "var(--tx-2, #64748B)", textTransform: "uppercase", letterSpacing: "0.06em" }}>
                Consentements
            </legend>
            <p style={{ fontSize: 12, color: "var(--tx-3, #64748B)", margin: "0 0 12px" }}>
                Pour finaliser ton inscription, merci d&apos;accepter les documents suivants. Les cases marquées d&apos;un astérisque sont obligatoires.
            </p>

            <ConsentCheckbox
                id="consent-politique"
                label={<>J&apos;ai lu et j&apos;accepte la <strong>{polTitle}</strong></>}
                documentSlug="politique-confidentialite"
                checked={state.politiqueConfidentialite}
                onChange={v => set("politiqueConfidentialite", v)}
                required
                error={errPolitique}
            />
            <ConsentCheckbox
                id="consent-cgu"
                label={<>J&apos;ai lu et j&apos;accepte les <strong>{cguTitle}</strong></>}
                documentSlug="cgu"
                checked={state.cgu}
                onChange={v => set("cgu", v)}
                required
                error={errCgu}
            />
            {isAdmin && (
                <ConsentCheckbox
                    id="consent-dpa"
                    label={<>J&apos;ai lu et j&apos;accepte l&apos;<strong>{dpaTitle}</strong></>}
                    documentSlug="dpa"
                    checked={state.dpa}
                    onChange={v => set("dpa", v)}
                    required
                    error={errDpa}
                />
            )}
            <ConsentCheckbox
                id="consent-majeur"
                label="Je certifie être majeur(e) (18 ans révolus) et agir dans le cadre de mon activité professionnelle"
                checked={state.majeurUsageProfessionnel}
                onChange={v => set("majeurUsageProfessionnel", v)}
                required
                error={errMajeur}
            />

            <div style={{ height: 1, background: "var(--bd, #E2E8F0)", margin: "12px 0" }} />
            <div style={{ fontSize: 11, color: "var(--tx-3, #64748B)", textTransform: "uppercase", letterSpacing: "0.06em", marginBottom: 8 }}>
                Optionnel
            </div>
            <ConsentCheckbox
                id="consent-newsletter"
                label="J'accepte de recevoir la newsletter Sentys (actualités produit, sécurité)"
                checked={state.newsletter}
                onChange={v => set("newsletter", v)}
            />
            <ConsentCheckbox
                id="consent-commercial"
                label="J'accepte d'être contacté(e) à des fins commerciales (offres, démos)"
                checked={state.commercial}
                onChange={v => set("commercial", v)}
            />
        </fieldset>
    );
}

/**
 * Calcule si tous les consentements obligatoires sont cochés.
 */
export function areMandatoryConsentsAccepted(state: RegistrationConsentsState, isAdmin: boolean): boolean {
    if (!state.politiqueConfidentialite) return false;
    if (!state.cgu) return false;
    if (isAdmin && !state.dpa) return false;
    if (!state.majeurUsageProfessionnel) return false;
    return true;
}
