"use client";

import { useMemo, useState } from "react";
import { motion, AnimatePresence } from "framer-motion";
import { apiFetch } from "@/lib/api";
import { Check, X, RotateCw, Trophy } from "lucide-react";

type MatchLeft = { id: string; text: string };
type MatchRight = { id: string; text: string };
type FlipCard = { id: string; front: string; back: string };
type Content = {
    subtype: "match" | "flip";
    instructions?: string;
    left?: MatchLeft[];
    right?: MatchRight[];
    cards?: FlipCard[];
};
type MatchResult = { leftId: string; correctRightId: string; givenRightId: string | null; isCorrect: boolean };

function shuffle<T>(a: T[]): T[] {
    const r = [...a];
    for (let i = r.length - 1; i > 0; i--) { const j = Math.floor(Math.random() * (i + 1)); [r[i], r[j]] = [r[j], r[i]]; }
    return r;
}

export default function FlashCardsChallenge({
    challengeId, content, onComplete,
}: { challengeId: string; content: Content; onComplete: () => void }) {
    if (content.subtype === "match") return <MatchGame challengeId={challengeId} content={content} onComplete={onComplete} />;
    return <FlipDeck challengeId={challengeId} content={content} onComplete={onComplete} />;
}

// ── Sous-type A : association (relier risque ↔ définition) ────────────────────
function MatchGame({ challengeId, content, onComplete }: { challengeId: string; content: Content; onComplete: () => void }) {
    const left = content.left ?? [];
    const rights = useMemo(() => shuffle(content.right ?? []), [content.right]);
    const [sel, setSel] = useState<string | null>(null);
    const [pairs, setPairs] = useState<Record<string, string>>({});
    const [res, setRes] = useState<MatchResult[] | null>(null);
    const [score, setScore] = useState<number | null>(null);
    const [loading, setLoading] = useState(false);

    const usedRights = new Set(Object.values(pairs));
    const allPaired = Object.keys(pairs).length === left.length && left.length > 0;

    function pickRight(rid: string) {
        if (res || !sel) return;
        setPairs(p => {
            const next = { ...p };
            for (const k of Object.keys(next)) if (next[k] === rid) delete next[k]; // 1 right = 1 left
            next[sel] = rid;
            return next;
        });
        setSel(null);
    }

    async function submit() {
        setLoading(true);
        try {
            const r = await apiFetch<{ scorePercent: number; results: MatchResult[] }>(
                `/api/challenges/interactive/${challengeId}/submit-flash-cards`,
                { method: "POST", body: JSON.stringify({ pairs }) });
            setRes(r.results); setScore(r.scorePercent);
            setTimeout(onComplete, 400);
        } catch { /* noop */ } finally { setLoading(false); }
    }

    const rightById = (id: string) => rights.find(r => r.id === id)?.text ?? "";
    const resFor = (lid: string) => res?.find(x => x.leftId === lid);

    return (
        <div className="flex flex-col gap-4">
            <p className="text-sm text-fg-muted">{content.instructions ?? "Reliez chaque élément de gauche à sa correspondance à droite."}</p>
            {score !== null && (
                <div className="flex items-center gap-2 rounded-xl border px-4 py-3" style={{ background: "var(--accent-subtle)", borderColor: "var(--accent-border)", color: "var(--pr-t)" }}>
                    <Trophy size={18} /> <span className="font-semibold">Score : {score}%</span>
                </div>
            )}
            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                {/* Colonne gauche */}
                <div className="flex flex-col gap-2">
                    {left.map(l => {
                        const r = resFor(l.id);
                        const paired = pairs[l.id];
                        const border = r ? (r.isCorrect ? "var(--success)" : "var(--danger)") : sel === l.id ? "var(--accent)" : "var(--border)";
                        return (
                            <button key={l.id} type="button" disabled={!!res} onClick={() => setSel(sel === l.id ? null : l.id)}
                                className="rounded-xl border px-4 py-3 text-left transition-colors" style={{ minHeight: 44, background: "var(--surface)", borderColor: border, color: "var(--text)" }}>
                                <div className="text-sm font-medium">{l.text}</div>
                                {paired && <div className="mt-1 flex items-center gap-1 text-xs" style={{ color: r ? (r.isCorrect ? "var(--success-t)" : "var(--danger-t)") : "var(--text-2)" }}>
                                    {r && (r.isCorrect ? <Check size={13} /> : <X size={13} />)} → {rightById(paired)}
                                </div>}
                                {r && !r.isCorrect && <div className="mt-0.5 text-xs" style={{ color: "var(--success-t)" }}>Bonne réponse : {rightById(r.correctRightId)}</div>}
                            </button>
                        );
                    })}
                </div>
                {/* Colonne droite */}
                <div className="flex flex-col gap-2">
                    {rights.map(r => {
                        const used = usedRights.has(r.id);
                        return (
                            <button key={r.id} type="button" disabled={!!res || (!sel && !used)} onClick={() => pickRight(r.id)}
                                className="rounded-xl border px-4 py-3 text-left text-sm transition-colors disabled:opacity-60" style={{ minHeight: 44, background: used ? "var(--accent-subtle)" : "var(--surface-2)", borderColor: used ? "var(--accent-border)" : "var(--border)", color: "var(--text)" }}>
                                {r.text}
                            </button>
                        );
                    })}
                </div>
            </div>
            {!res && (
                <button type="button" onClick={submit} disabled={!allPaired || loading}
                    className="rounded-xl py-3 text-sm font-semibold text-white transition-colors disabled:opacity-50" style={{ background: allPaired ? "var(--accent)" : "var(--accent-subtle)", minHeight: 44 }}>
                    {loading ? "Validation…" : allPaired ? "Valider les associations" : `Reliez toutes les cartes (${Object.keys(pairs).length}/${left.length})`}
                </button>
            )}
        </div>
    );
}

// ── Sous-type B : cartes de révision (flip recto/verso) ───────────────────────
function FlipDeck({ challengeId, content, onComplete }: { challengeId: string; content: Content; onComplete: () => void }) {
    const cards = content.cards ?? [];
    const [idx, setIdx] = useState(0);
    const [flipped, setFlipped] = useState(false);
    const [known, setKnown] = useState(0);
    const [done, setDone] = useState(false);
    const [score, setScore] = useState<number | null>(null);

    async function finish(kTotal: number) {
        setDone(true);
        try {
            const r = await apiFetch<{ scorePercent: number }>(
                `/api/challenges/interactive/${challengeId}/submit-flash-cards`,
                { method: "POST", body: JSON.stringify({ knownCount: kTotal, total: cards.length }) });
            setScore(r.scorePercent);
        } catch { /* noop */ }
        setTimeout(onComplete, 400);
    }
    function rate(isKnown: boolean) {
        const k = known + (isKnown ? 1 : 0);
        setKnown(k);
        if (idx + 1 >= cards.length) finish(k);
        else { setIdx(idx + 1); setFlipped(false); }
    }

    if (done) return (
        <div className="flex flex-col items-center gap-3 py-8 text-center">
            <Trophy size={32} style={{ color: "var(--accent)" }} />
            <div className="text-lg font-semibold" style={{ color: "var(--text)" }}>Révision terminée</div>
            <div className="text-sm text-fg-muted">Cartes sues : {known}/{cards.length}{score !== null ? ` · Score ${score}%` : ""}</div>
        </div>
    );

    const card = cards[idx];
    return (
        <div className="flex flex-col items-center gap-4">
            <p className="text-sm text-fg-muted self-start">{content.instructions ?? "Retournez chaque carte, puis indiquez si vous la maîtrisiez."}</p>
            <div className="text-xs text-fg-muted">Carte {idx + 1} / {cards.length}</div>
            <div className="w-full" style={{ maxWidth: 480, perspective: 1200 }}>
                <motion.div onClick={() => setFlipped(f => !f)} animate={{ rotateY: flipped ? 180 : 0 }} transition={{ duration: 0.5 }}
                    className="relative w-full cursor-pointer select-none" style={{ transformStyle: "preserve-3d", minHeight: 200 }}>
                    <div className="absolute inset-0 flex flex-col items-center justify-center rounded-2xl border p-6 text-center" style={{ backfaceVisibility: "hidden", background: "var(--surface)", borderColor: "var(--border)" }}>
                        <div className="text-xs uppercase tracking-wider text-fg-muted">Question</div>
                        <div className="mt-2 text-base font-medium" style={{ color: "var(--text)" }}>{card.front}</div>
                        <div className="mt-4 flex items-center gap-1 text-xs" style={{ color: "var(--accent)" }}><RotateCw size={13} /> Cliquez pour retourner</div>
                    </div>
                    <div className="absolute inset-0 flex flex-col items-center justify-center rounded-2xl border p-6 text-center" style={{ backfaceVisibility: "hidden", transform: "rotateY(180deg)", background: "var(--accent-subtle)", borderColor: "var(--accent-border)" }}>
                        <div className="text-xs uppercase tracking-wider" style={{ color: "var(--pr-t)" }}>Réponse</div>
                        <div className="mt-2 text-sm leading-relaxed" style={{ color: "var(--text)" }}>{card.back}</div>
                    </div>
                </motion.div>
            </div>
            <AnimatePresence>
                {flipped && (
                    <motion.div initial={{ opacity: 0, y: 6 }} animate={{ opacity: 1, y: 0 }} className="flex w-full gap-3" style={{ maxWidth: 480 }}>
                        <button type="button" onClick={() => rate(false)} className="flex-1 rounded-xl border py-3 text-sm font-semibold transition-colors" style={{ minHeight: 44, background: "var(--surface-2)", borderColor: "var(--border)", color: "var(--danger-t)" }}>À revoir</button>
                        <button type="button" onClick={() => rate(true)} className="flex-1 rounded-xl py-3 text-sm font-semibold text-white transition-colors" style={{ minHeight: 44, background: "var(--accent)" }}>Je savais</button>
                    </motion.div>
                )}
            </AnimatePresence>
        </div>
    );
}
