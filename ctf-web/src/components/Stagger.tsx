"use client";

import { motion, useReducedMotion } from "framer-motion";
import type { ReactNode } from "react";
import { staggerContainer, staggerItem } from "@/lib/motion";

/**
 * Apparition en cascade d'une liste (tableaux, cartes, KPI).
 * <Stagger><StaggerItem>…</StaggerItem><StaggerItem>…</StaggerItem></Stagger>
 * Respecte prefers-reduced-motion (rendu statique si réduit).
 */
export function Stagger({ children, className, gap = 0.05 }: { children: ReactNode; className?: string; gap?: number }) {
    const reduce = useReducedMotion();
    if (reduce) return <div className={className}>{children}</div>;
    return (
        <motion.div className={className} variants={staggerContainer(gap)} initial="initial" whileInView="animate" viewport={{ once: true, margin: "-40px" }}>
            {children}
        </motion.div>
    );
}

export function StaggerItem({ children, className }: { children: ReactNode; className?: string }) {
    const reduce = useReducedMotion();
    if (reduce) return <div className={className}>{children}</div>;
    return (
        <motion.div className={className} variants={staggerItem}>
            {children}
        </motion.div>
    );
}
