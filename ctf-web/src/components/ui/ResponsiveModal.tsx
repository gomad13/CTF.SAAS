"use client";

import React, { useEffect } from "react";
import { X } from "lucide-react";

/**
 * Modale responsive : centrée max-width sur desktop, plein écran sur mobile (<640px).
 * S'appuie sur les classes .modal-overlay / .modal-box de globals.css (qui basculent
 * déjà en full-screen sous 640px) + un bouton fermer accessible au pouce.
 */
export function ResponsiveModal({
    open,
    onClose,
    title,
    children,
    footer,
    maxWidth = 600,
}: {
    open: boolean;
    onClose: () => void;
    title?: React.ReactNode;
    children: React.ReactNode;
    footer?: React.ReactNode;
    maxWidth?: number;
}) {
    useEffect(() => {
        if (!open) return;
        const onKey = (e: KeyboardEvent) => {
            if (e.key === "Escape") onClose();
        };
        window.addEventListener("keydown", onKey);
        return () => window.removeEventListener("keydown", onKey);
    }, [open, onClose]);

    if (!open) return null;

    return (
        <div className="modal-overlay" onClick={onClose} role="dialog" aria-modal="true">
            <div
                className="modal-box resp-modal"
                style={{ maxWidth, display: "flex", flexDirection: "column", gap: 0, padding: 0 }}
                onClick={(e) => e.stopPropagation()}
            >
                <div
                    style={{
                        display: "flex",
                        alignItems: "center",
                        justifyContent: "space-between",
                        gap: 12,
                        padding: "18px 20px",
                        borderBottom: "1px solid #E2E8F0",
                        position: "sticky",
                        top: 0,
                        background: "#fff",
                        zIndex: 1,
                    }}
                >
                    <div style={{ fontSize: 16, fontWeight: 700, color: "#1E293B", minWidth: 0 }}>
                        {title}
                    </div>
                    <button
                        onClick={onClose}
                        aria-label="Fermer"
                        style={{
                            display: "flex",
                            alignItems: "center",
                            justifyContent: "center",
                            width: 40,
                            height: 40,
                            minWidth: 40,
                            border: "none",
                            background: "transparent",
                            color: "#64748B",
                            borderRadius: 8,
                            cursor: "pointer",
                            flexShrink: 0,
                        }}
                    >
                        <X size={20} />
                    </button>
                </div>

                <div style={{ padding: "20px", overflowY: "auto", flex: 1 }}>{children}</div>

                {footer && (
                    <div
                        style={{
                            display: "flex",
                            justifyContent: "flex-end",
                            gap: 8,
                            padding: "14px 20px",
                            borderTop: "1px solid #E2E8F0",
                            flexWrap: "wrap",
                            position: "sticky",
                            bottom: 0,
                            background: "#fff",
                        }}
                    >
                        {footer}
                    </div>
                )}
            </div>
        </div>
    );
}

export default ResponsiveModal;
