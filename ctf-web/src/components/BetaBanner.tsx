"use client";

import { useEffect, useState } from "react";

const STORAGE_KEY = "viper.betaBannerDismissed";

export default function BetaBanner() {
    const [dismissed, setDismissed] = useState<boolean | null>(null);

    useEffect(() => {
        try {
            setDismissed(localStorage.getItem(STORAGE_KEY) === "1");
        } catch {
            setDismissed(false);
        }
    }, []);

    if (dismissed === null || dismissed) return null;

    const close = () => {
        try { localStorage.setItem(STORAGE_KEY, "1"); } catch { /* noop */ }
        setDismissed(true);
    };

    return (
        <div
            role="status"
            aria-live="polite"
            style={{
                width: "100%",
                background: "linear-gradient(90deg, #1E3A8A 0%, #2563EB 100%)",
                color: "#FFFFFF",
                fontSize: 13,
                padding: "8px 16px",
                display: "flex",
                alignItems: "center",
                justifyContent: "center",
                gap: 12,
                position: "relative",
                zIndex: 50,
            }}
        >
            <span aria-hidden="true">🧪</span>
            <span>
                Version <strong>bêta privée</strong> — vos retours nous aident à améliorer la plateforme.{" "}
                <a
                    href="/feedback"
                    style={{ color: "#FFFFFF", textDecoration: "underline", fontWeight: 600 }}
                >
                    Envoyer un feedback
                </a>
            </span>
            <button
                type="button"
                onClick={close}
                aria-label="Fermer le bandeau bêta"
                style={{
                    position: "absolute",
                    right: 12,
                    top: "50%",
                    transform: "translateY(-50%)",
                    background: "transparent",
                    border: "none",
                    color: "#FFFFFF",
                    cursor: "pointer",
                    fontSize: 16,
                    lineHeight: 1,
                    padding: 4,
                }}
            >
                ×
            </button>
        </div>
    );
}
