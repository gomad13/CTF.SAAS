"use client";

type Props = {
    id: string;
    label: React.ReactNode;
    documentSlug?: string;
    checked: boolean;
    required?: boolean;
    error?: string | null;
    onChange: (checked: boolean) => void;
};

/**
 * Checkbox de consentement avec lien optionnel "Lire le document complet".
 * Pas de pré-cochage par défaut (recommandation CNIL).
 */
export default function ConsentCheckbox({ id, label, documentSlug, checked, required, error, onChange }: Props) {
    return (
        <div style={{ marginBottom: 12 }}>
            <label
                htmlFor={id}
                style={{
                    display: "flex", alignItems: "flex-start", gap: 10, cursor: "pointer",
                    padding: 8, minHeight: 44, borderRadius: 6,
                    transition: "background 0.2s",
                }}
            >
                <input
                    id={id}
                    type="checkbox"
                    checked={checked}
                    onChange={e => onChange(e.target.checked)}
                    style={{
                        marginTop: 2, width: 20, height: 20,
                        accentColor: "var(--accent)", flexShrink: 0,
                    }}
                    aria-required={required ? "true" : undefined}
                    aria-invalid={error ? "true" : undefined}
                />
                <span style={{ fontSize: 13, lineHeight: 1.5, color: "var(--text)" }}>
                    {label}
                    {required && <span style={{ color: "var(--danger)", marginLeft: 4 }} aria-hidden="true">*</span>}
                    {documentSlug && (
                        <>
                            {" "}
                            <a
                                href={`/legal/${documentSlug}`}
                                target="_blank"
                                rel="noopener noreferrer"
                                onClick={e => e.stopPropagation()}
                                className="transition-colors duration-200 hover:text-[var(--accent-hover)]"
                                style={{ color: "var(--accent)", fontWeight: 500, textDecoration: "underline" }}
                            >
                                Lire le document complet
                            </a>
                        </>
                    )}
                </span>
            </label>
            {error && (
                <div role="alert" style={{ fontSize: 11, color: "var(--danger)", marginTop: 2, marginLeft: 38 }}>
                    {error}
                </div>
            )}
        </div>
    );
}
