"use client";
import type { ReactNode } from "react";
import { motion, useReducedMotion } from "framer-motion";

export type Column<T> = { key: string; header: string; align?: "left" | "right" | "center"; render?: (row: T) => ReactNode };

/** Tableau premium : header cohérent, hover, apparition en cascade, empty state, responsive. */
export default function DataTable<T extends Record<string, unknown>>({ columns, rows, empty = "Aucune donnée." }: {
    columns: Column<T>[]; rows: T[]; empty?: string;
}) {
    const reduce = useReducedMotion();
    if (!rows.length) return <div className="empty"><div className="empty-title">Rien à afficher</div><div className="empty-text">{empty}</div></div>;
    return (
        <div className="overflow-x-auto rounded-xl border border-border">
            <table className="w-full text-sm" style={{ minWidth: 520 }}>
                <thead style={{ background: "var(--surface-2)" }}>
                    <tr>{columns.map(c => (
                        <th key={c.key} className="px-4 py-3 text-xs font-semibold uppercase tracking-wider" style={{ color: "var(--text-3)", textAlign: c.align ?? "left" }}>{c.header}</th>
                    ))}</tr>
                </thead>
                <tbody className="divide-y" style={{ borderColor: "var(--border)" }}>
                    {rows.map((row, i) => (
                        <motion.tr key={i}
                            initial={reduce ? false : { opacity: 0, y: 6 }}
                            whileInView={reduce ? undefined : { opacity: 1, y: 0 }}
                            viewport={{ once: true, margin: "-20px" }}
                            transition={{ duration: 0.25, delay: Math.min(i * 0.03, 0.4) }}
                            className="transition-colors" style={{ color: "var(--text)" }}
                            onMouseEnter={e => (e.currentTarget.style.background = "var(--surface-2)")}
                            onMouseLeave={e => (e.currentTarget.style.background = "transparent")}>
                            {columns.map(c => (
                                <td key={c.key} className="px-4 py-3" style={{ textAlign: c.align ?? "left" }}>
                                    {c.render ? c.render(row) : String(row[c.key] ?? "")}
                                </td>
                            ))}
                        </motion.tr>
                    ))}
                </tbody>
            </table>
        </div>
    );
}
