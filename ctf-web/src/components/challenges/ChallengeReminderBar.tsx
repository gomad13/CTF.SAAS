"use client";

import { useState, useCallback } from "react";
import { Lightbulb, X } from "lucide-react";
import { useIsMobile } from "@/hooks/useMediaQuery";

/**
 * Bandeau de rappel discret affiché en permanence pendant l'exercice.
 * Reprend le `InstructionShortReminder`. Fermable (état local, non persisté :
 * le bandeau réapparaît au rafraîchissement de la page).
 *
 * Fallback gracieux : le parent ne rend ce composant que si `reminder` est non vide.
 */
export default function ChallengeReminderBar({ reminder }: { reminder: string }) {
    const [closed, setClosed] = useState(false);
    const isMobile = useIsMobile();
    const handleClose = useCallback(() => setClosed(true), []);

    if (closed) return null;

    return (
        <div
            style={{
                display: "flex",
                alignItems: "center",
                gap: 10,
                background: "rgba(0,191,179,0.12)",
                border: "1px solid rgba(59,130,246,0.30)",
                borderRadius: 10,
                padding: isMobile ? "8px 12px" : "9px 14px",
                marginBottom: isMobile ? 14 : 20,
            }}
        >
            <Lightbulb size={16} strokeWidth={2} color="var(--pr)" style={{ flexShrink: 0 }} />
            <span
                style={{
                    flex: 1,
                    minWidth: 0,
                    fontSize: isMobile ? 12.5 : 14,
                    lineHeight: 1.4,
                    color: "var(--text-on-dark-muted)",
                    overflowWrap: "break-word",
                }}
            >
                {reminder}
            </span>
            <button
                onClick={handleClose}
                aria-label="Masquer le rappel"
                style={{
                    display: "inline-flex",
                    alignItems: "center",
                    justifyContent: "center",
                    background: "none",
                    border: "none",
                    cursor: "pointer",
                    padding: 2,
                    color: "var(--text-on-dark-faint)",
                    flexShrink: 0,
                    transition: "color 0.2s",
                }}
                onMouseOver={(e) => { e.currentTarget.style.color = "var(--text-on-dark)"; }}
                onMouseOut={(e) => { e.currentTarget.style.color = "var(--text-on-dark-faint)"; }}
            >
                <X size={16} strokeWidth={2} />
            </button>
        </div>
    );
}
