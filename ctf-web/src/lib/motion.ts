// Système d'animation réutilisable Sentys (framer-motion). Sobre et pro.
// Voir aussi : components/Reveal.tsx, components/Stagger.tsx, components/CountUp.tsx.
import type { Variants } from "framer-motion";

/** Apparition douce (fade + slide court). Entrée par défaut du projet. */
export const fadeInUp: Variants = {
    initial: { opacity: 0, y: 10 },
    animate: { opacity: 1, y: 0 },
    exit: { opacity: 0, y: 10 },
};

export const fadeIn: Variants = {
    initial: { opacity: 0 },
    animate: { opacity: 1 },
    exit: { opacity: 0 },
};

export const scaleIn: Variants = {
    initial: { opacity: 0, scale: 0.97 },
    animate: { opacity: 1, scale: 1 },
    exit: { opacity: 0, scale: 0.97 },
};

/** Transition standard (courte, ease-out). */
export const easeOut = { duration: 0.28, ease: [0.16, 1, 0.3, 1] as const };
export const easeQuick = { duration: 0.18, ease: "easeOut" as const };

/** Conteneur qui décale l'apparition de ses enfants (cascade). */
export const staggerContainer = (stagger = 0.05, delayChildren = 0): Variants => ({
    initial: {},
    animate: { transition: { staggerChildren: stagger, delayChildren } },
});

/** Enfant d'un staggerContainer (à utiliser avec `variants={staggerItem}`). */
export const staggerItem: Variants = {
    initial: { opacity: 0, y: 8 },
    animate: { opacity: 1, y: 0, transition: easeOut },
};
