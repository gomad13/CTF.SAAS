"use client";

import { use, useState, useEffect, useCallback, useRef } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiFetch } from "@/lib/api";
import type { ChallengeItem, SubmissionResult } from "@/lib/types";

type NextChallengeResp = { nextChallengeId: string | null; moduleId: string | null; isLastOfPath: boolean };
import CeoFraudChallenge from "@/components/challenges/CeoFraudChallenge";
import MailboxChallenge from "@/components/challenges/MailboxChallenge";
import PhishingAiChallenge from "@/components/challenges/PhishingAiChallenge";
import MultichoiceChallenge from "@/components/challenges/MultichoiceChallenge";
import PasswordQuizChallenge from "@/components/challenges/PasswordQuizChallenge";
import FreeTextChallenge from "@/components/challenges/FreeTextChallenge";
import ChallengeHeader from "@/components/challenges/ChallengeHeader";
import ChallengeIntro from "@/components/challenges/ChallengeIntro";
import ChallengeReminderBar from "@/components/challenges/ChallengeReminderBar";
import {
    DndContext,
    closestCenter,
    PointerSensor,
    TouchSensor,
    useSensor,
    useSensors,
    type DragEndEvent,
} from "@dnd-kit/core";
import {
    SortableContext,
    verticalListSortingStrategy,
    useSortable,
    arrayMove,
} from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";

// ── Types for structured challenge data ──────────────────────────────────────
type EmailData  = { context: string; from: string; subject: string; body: string };
type EmailEntry = { id: string; from: string; subject: string; preview: string };
type EmailSortData = { context: string; emails: EmailEntry[] };
type ChatMessage  = { from: "other" | "you"; text: string };
type ChatContact  = { name: string; title: string; avatar: string };
type ChatData     = { context: string; contact: ChatContact; messages: ChatMessage[] };
type TerminalData = { context: string; popup: { title: string; message: string; buttons: string[] } };

type Step = "mission" | "result";

// ── Page ─────────────────────────────────────────────────────────────────────
export default function ChallengePage({ params }: { params: Promise<{ id: string }> }) {
    const { id: challengeId } = use(params);
    const qc = useQueryClient();
    const router = useRouter();
    const searchParams = useSearchParams();
    const pathId = searchParams.get("path");
    const [step, setStep] = useState<Step>("mission");
    const [started, setStarted] = useState(false);
    const [lastResult, setLastResult] = useState<SubmissionResult | null>(null);
    const handleStart = useCallback(() => setStarted(true), []);

    const challengeQ = useQuery<ChallengeItem>({
        queryKey: ["challenge", challengeId],
        queryFn: () => apiFetch<ChallengeItem>(`/api/challenges/${challengeId}`),
    });

    const submitM = useMutation({
        mutationFn: (answer: string) =>
            apiFetch<SubmissionResult>("/api/submissions", {
                method: "POST",
                body: JSON.stringify({ challengeId, answer }),
            }),
        onSuccess: (data) => {
            setLastResult(data);
            setStep("result");
            qc.invalidateQueries({ queryKey: ["submissions", "recent"] });
            qc.invalidateQueries({ queryKey: ["assignments", "mine"] });
            if (pathId) qc.invalidateQueries({ queryKey: ["path", pathId, "progress"] });
        },
    });

    async function goToNextInPath() {
        if (!pathId) { window.history.back(); return; }
        try {
            const r = await apiFetch<NextChallengeResp>(
                `/api/paths/${pathId}/next-challenge?afterChallengeId=${challengeId}`
            );
            if (r.nextChallengeId && r.nextChallengeId !== challengeId) {
                router.push(`/dashboard/challenge/${r.nextChallengeId}?path=${pathId}`);
            } else {
                router.push(`/dashboard/parcours/${pathId}`);
            }
        } catch {
            router.push(`/dashboard/parcours/${pathId}`);
        }
    }

    function onInteractiveComplete() {
        qc.invalidateQueries({ queryKey: ["submissions", "recent"] });
        qc.invalidateQueries({ queryKey: ["assignments", "mine"] });
        if (pathId) qc.invalidateQueries({ queryKey: ["path", pathId, "progress"] });
        void goToNextInPath();
    }

    if (challengeQ.isLoading) return <LoadingSkeleton />;
    if (challengeQ.isError || !challengeQ.data) return <ErrorState msg={(challengeQ.error as Error)?.message} />;

    const c = challengeQ.data;

    // Consignes pédagogiques — fallback gracieux si absentes
    const introBody = c.instructionBody?.trim() ? c.instructionBody : null;
    const reminder  = c.instructionShortReminder?.trim() ? c.instructionShortReminder : null;
    // Intro affichée seulement si une consigne existe ET tant que l'apprenant n'a pas commencé
    const showIntro = step === "mission" && introBody !== null && !started;

    return (
        <div style={{ maxWidth: 900, margin: "0 auto", padding: "32px var(--page-x) 80px" }}>
            {/* Header sobre */}
            <button
                onClick={() => window.history.back()}
                style={{
                    display: "inline-flex",
                    alignItems: "center",
                    gap: 6,
                    color: "var(--text-on-dark-faint)",
                    fontSize: 13,
                    background: "none",
                    border: "none",
                    cursor: "pointer",
                    padding: 0,
                    marginBottom: 16,
                    transition: "color 0.2s",
                }}
                onMouseOver={e => { e.currentTarget.style.color = "var(--text-on-dark)"; }}
                onMouseOut={e => { e.currentTarget.style.color = "var(--text-on-dark-faint)"; }}
            >
                ← Retour au parcours
            </button>

            <ChallengeHeader challenge={c} />

            {showIntro ? (
                <ChallengeIntro
                    title={c.instructionTitle}
                    body={introBody!}
                    onStart={handleStart}
                />
            ) : (
                <>
                    {step === "mission" && (
                        <>
                            {reminder && <ChallengeReminderBar reminder={reminder} />}
                            <MissionStep
                                challenge={c}
                                isPending={submitM.isPending}
                                error={submitM.isError ? (submitM.error as Error)?.message : null}
                                onSubmit={(answer) => submitM.mutate(answer)}
                                onInteractiveComplete={onInteractiveComplete}
                            />
                        </>
                    )}
                    {step === "result"  && lastResult && (
                        <ResultStep
                            challenge={c}
                            result={lastResult}
                            onRetry={() => { setLastResult(null); setStep("mission"); }}
                            onNext={() => { void goToNextInPath(); }}
                        />
                    )}
                </>
            )}
        </div>
    );
}

// ── Step 1: Brief ─────────────────────────────────────────────────────────────
function BriefStep({ challenge: c, onStart }: { challenge: ChallengeItem; onStart: () => void }) {
    const typeInfo = TYPE_INFO[c.type] ?? { label: c.type, icon: "🎯", color: "from-neutral-800 to-neutral-900", accent: "text-fg-heading" };
    return (
        <div className="overflow-hidden rounded-2xl border border-border bg-surface">
            {/* Hero banner */}
            <div className={`bg-gradient-to-br ${typeInfo.color} px-6 py-10 text-center`}>
                <div className="text-5xl">{typeInfo.icon}</div>
                <div className={`mt-3 text-xs font-semibold uppercase tracking-widest ${typeInfo.accent}`}>{typeInfo.label}</div>
                <h1 className="mt-2 text-2xl font-bold text-white">{c.title}</h1>
            </div>

            {/* Meta */}
            <div className="flex items-center gap-4 border-b border-border px-6 py-4">
                {c.difficulty && <DifficultyBadge difficulty={c.difficulty} />}
                <span className="text-sm text-fg-heading">{c.points} points</span>
                <span className="ml-auto text-xs text-fg-heading uppercase tracking-wider">{typeInfo.label}</span>
            </div>

            {/* Description */}
            <div className="px-6 py-5">
                <p className="text-sm leading-relaxed text-fg-heading">
                    {BRIEF_TEXT[c.type] ?? "Une nouvelle mission t'attend. Analyse la situation et réponds avec précision."}
                </p>
            </div>

            <div className="px-6 pb-6">
                <button
                    onClick={onStart}
                    className="w-full rounded-xl bg-primary py-3 text-sm font-semibold text-white hover:bg-primary-hover transition-colors"
                >
                    Lancer la mission →
                </button>
            </div>
        </div>
    );
}

// ── Step 2: Mission ───────────────────────────────────────────────────────────
function MissionStep({ challenge: c, isPending, error, onSubmit, onInteractiveComplete }: {
    challenge: ChallengeItem;
    isPending: boolean;
    error: string | null;
    onSubmit: (answer: string) => void;
    onInteractiveComplete: () => void;
}) {
    const [answer, setAnswer] = useState("");

    // Try to parse JSON for structured types
    let parsed: unknown = null;
    if (["email", "email-sort", "chat", "terminal"].includes(c.type)) {
        try { parsed = JSON.parse(c.instructions); } catch { /* plain text fallback */ }
    }

    // Interactive challenges handle their own full flow internally
    if (c.type === "interactive") {
        return (
            <InteractiveMission
                challengeId={c.id}
                onComplete={onInteractiveComplete}
            />
        );
    }

    return (
        <div className="space-y-5">
            {/* Type-specific UI */}
            {c.type === "quiz" || c.type === "scenario" ? (
                <QuizMission instructions={c.instructions} isPending={isPending} onSubmit={onSubmit} />
            ) : c.type === "email" && parsed ? (
                <EmailMission data={parsed as EmailData} isPending={isPending} onSubmit={onSubmit} />
            ) : c.type === "email-sort" && parsed ? (
                <EmailSortMission data={parsed as EmailSortData} isPending={isPending} onSubmit={onSubmit} />
            ) : c.type === "chat" && parsed ? (
                <ChatMission data={parsed as ChatData} isPending={isPending} onSubmit={onSubmit} />
            ) : c.type === "terminal" && parsed ? (
                <TerminalMission data={parsed as TerminalData} isPending={isPending} onSubmit={onSubmit} />
            ) : (
                <FallbackMission instructions={c.instructions} isPending={isPending} onSubmit={onSubmit} answer={answer} setAnswer={setAnswer} />
            )}

            {error && (
                <div className="rounded-xl border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-600">{error}</div>
            )}
        </div>
    );
}

// ── Step 3: Result ────────────────────────────────────────────────────────────
function ResultStep({ challenge: c, result: r, onRetry, onNext }: {
    challenge: ChallengeItem;
    result: SubmissionResult;
    onRetry: () => void;
    onNext: () => void;
}) {
    const isEmailSort = c.type === "email-sort";
    const isPartial   = isEmailSort && !r.isCorrect && r.scoreAwarded > 0;

    return (
        <div className="space-y-5">
            {/* Result banner */}
            <div className={[
                "rounded-2xl border px-5 py-5",
                r.isCorrect
                    ? "border-emerald-500/40 bg-emerald-950/30"
                    : isPartial
                    ? "border-yellow-500/40 bg-yellow-950/20"
                    : "border-red-200 bg-red-50",
            ].join(" ")}>
                <div className="flex items-center gap-3">
                    <span className="text-3xl">
                        {r.isCorrect ? "✅" : isPartial ? "🟡" : "❌"}
                    </span>
                    <div>
                        <div className={`font-bold text-lg ${r.isCorrect ? "text-emerald-400" : isPartial ? "text-yellow-400" : "text-red-300"}`}>
                            {r.isCorrect
                                ? "Bonne réponse !"
                                : isPartial
                                ? "Partiellement correct"
                                : "Réponse incorrecte"}
                        </div>
                        <div className="text-sm text-fg-heading">
                            {r.scoreAwarded > 0 ? `+${r.scoreAwarded} points` : "0 point gagné"}
                            {" · "}Tentative #{r.attemptNo}
                        </div>
                    </div>
                </div>
            </div>

            {/* Correct answer */}
            {r.correctAnswer && (
                <div className="rounded-xl border border-border bg-surface px-4 py-4">
                    <div className="mb-1 text-xs font-semibold uppercase tracking-wider text-fg-heading">Bonne réponse</div>
                    <div className="text-sm font-mono text-emerald-400">{r.correctAnswer}</div>
                </div>
            )}

            {/* Explanation */}
            {r.explanation && (
                <div className="rounded-xl border border-primary/20 bg-primary/5 px-4 py-4">
                    <div className="mb-2 flex items-center gap-2">
                        <span className="text-base">💡</span>
                        <span className="text-xs font-semibold uppercase tracking-wider text-primary/80">Explication</span>
                    </div>
                    <p className="text-sm leading-relaxed text-fg-heading">{r.explanation}</p>
                </div>
            )}

            {/* Actions */}
            <div className="flex gap-3">
                {!r.isCorrect && (
                    <button
                        onClick={onRetry}
                        className="flex-1 rounded-xl border border-border bg-surface-2 py-2.5 text-sm font-semibold text-white hover:bg-surface-2 transition-colors"
                    >
                        Réessayer
                    </button>
                )}
                <button
                    onClick={onNext}
                    className="flex-1 rounded-xl bg-primary py-2.5 text-sm font-semibold text-white hover:bg-primary-hover transition-colors"
                >
                    {r.isCorrect ? "Challenge suivant →" : "Retour au parcours"}
                </button>
            </div>
        </div>
    );
}

// ── Interactive Mission wrapper ───────────────────────────────────────────────
// Fetches content from the interactive endpoint and routes to the right component.
// The component handles its own submission and result display internally.

// eslint-disable-next-line @typescript-eslint/no-explicit-any
type InteractiveContent = { id: string; contentType: string; title: string; instructions: string; points: number; variantIndex?: number | null; content: Record<string, any> };

function InteractiveMission({ challengeId, onComplete }: { challengeId: string; onComplete: () => void }) {
    const [data, setData]   = useState<InteractiveContent | null>(null);
    const [error, setError] = useState<string | null>(null);
    const startedAtRef = useRef<number>(Date.now());

    useEffect(() => {
        startedAtRef.current = Date.now();
        apiFetch<InteractiveContent>(`/api/challenges/interactive/${challengeId}/content`)
            .then(setData)
            .catch(e => setError(e instanceof Error ? e.message : "Erreur chargement"));
    }, [challengeId]);

    // Bonus rapidite : enregistre le temps de resolution (best effort, non bloquant)
    // puis remonte la completion au parent.
    const handleComplete = useCallback(() => {
        const durationSeconds = Math.max(1, Math.round((Date.now() - startedAtRef.current) / 1000));
        apiFetch("/api/competition/duration", {
            method: "POST",
            body: JSON.stringify({ challengeId, durationSeconds }),
        }).catch(() => { /* non bloquant : la competition n'est pas toujours active */ });
        onComplete();
    }, [challengeId, onComplete]);

    if (error) return <p className="text-sm text-red-400 py-6 text-center">{error}</p>;
    if (!data)  return (
        <div className="flex items-center justify-center gap-3 py-12 text-fg-heading">
            <span className="h-5 w-5 rounded-full border-2 border-border border-t-[#3B82F6] animate-spin" />
            <span className="text-sm">Chargement de la mission…</span>
        </div>
    );

    const variantIndex = data.variantIndex ?? null;
    if (data.contentType === "ceo_fraud")
        return <CeoFraudChallenge challengeId={data.id} variantIndex={variantIndex} content={data.content as Parameters<typeof CeoFraudChallenge>[0]["content"]} onComplete={handleComplete} />;
    if (data.contentType === "mailbox")
        return <MailboxChallenge challengeId={data.id} variantIndex={variantIndex} content={data.content as Parameters<typeof MailboxChallenge>[0]["content"]} onComplete={handleComplete} />;
    if (data.contentType === "phishing_ai")
        return <PhishingAiChallenge challengeId={data.id} content={data.content as Parameters<typeof PhishingAiChallenge>[0]["content"]} onComplete={handleComplete} />;
    if (data.contentType === "multichoice")
        return <MultichoiceChallenge challengeId={data.id} variantIndex={variantIndex} content={data.content as Parameters<typeof MultichoiceChallenge>[0]["content"]} onComplete={handleComplete} />;
    if (data.contentType === "password_quiz")
        return <PasswordQuizChallenge challengeId={data.id} variantIndex={variantIndex} content={data.content as Parameters<typeof PasswordQuizChallenge>[0]["content"]} onComplete={handleComplete} />;
    if (data.contentType === "free_text")
        return <FreeTextChallenge challengeId={data.id} content={data.content as Parameters<typeof FreeTextChallenge>[0]["content"]} onComplete={handleComplete} />;

    return <p className="text-sm text-fg-heading py-6 text-center">Type interactif inconnu : {data.contentType}</p>;
}

// ── Quiz / Scenario Mission ───────────────────────────────────────────────────
function QuizMission({ instructions, isPending, onSubmit }: {
    instructions: string; isPending: boolean; onSubmit: (a: string) => void;
}) {
    const [selected, setSelected] = useState<string | null>(null);

    // Split narrative text from options
    const lines    = instructions.split("\n");
    const optReg   = /^([A-D])\)\s+(.+)$/;
    const options  = lines.filter(l => optReg.test(l.trim()));
    const narrative = lines.filter(l => !optReg.test(l.trim())).join("\n").trim();

    const hasOptions = options.length > 0;

    return (
        <div className="space-y-4">
            {/* Narrative */}
            <div className="rounded-2xl border border-border bg-surface px-5 py-5">
                <div className="whitespace-pre-wrap text-sm leading-relaxed text-fg-heading">{narrative}</div>
            </div>

            {/* Clickable options */}
            {hasOptions ? (
                <div className="space-y-2">
                    {options.map(opt => {
                        const m = opt.trim().match(optReg)!;
                        const letter = m[1];
                        const text   = m[2];
                        const isSelected = selected === letter;
                        return (
                            <button
                                key={letter}
                                onClick={() => setSelected(letter)}
                                className={[
                                    "w-full rounded-xl border px-4 py-3 text-left text-sm transition-all",
                                    isSelected
                                        ? "border-primary bg-primary text-white"
                                        : "border-border bg-surface text-fg-heading hover:border-border hover:bg-surface-2",
                                ].join(" ")}
                            >
                                <span className={`mr-3 font-bold ${isSelected ? "text-white" : "text-fg-heading"}`}>{letter}</span>
                                {text}
                            </button>
                        );
                    })}
                </div>
            ) : (
                <textarea
                    rows={3}
                    placeholder="Votre réponse…"
                    value={selected ?? ""}
                    onChange={e => setSelected(e.target.value)}
                    className="w-full resize-none rounded-xl border border-border bg-surface/40 px-3 py-2 text-sm text-white placeholder-black focus:border-primary focus:outline-none"
                />
            )}

            <button
                onClick={() => selected && onSubmit(selected)}
                disabled={isPending || !selected}
                className="w-full rounded-xl bg-primary py-2.5 text-sm font-semibold text-white hover:bg-primary-hover disabled:opacity-50 transition-colors"
            >
                {isPending ? "Vérification…" : "Valider ma réponse"}
            </button>
        </div>
    );
}

// ── Email Mission ─────────────────────────────────────────────────────────────
function EmailMission({ data, isPending, onSubmit }: { data: EmailData; isPending: boolean; onSubmit: (a: string) => void }) {
    const [answer, setAnswer] = useState("");
    return (
        <div className="space-y-4">
            <p className="text-sm text-fg-heading">{data.context}</p>

            {/* Simulated email client */}
            <div className="rounded-2xl border border-border bg-surface overflow-hidden">
                <div className="border-b border-border bg-surface/60 px-4 py-3">
                    <div className="flex items-center gap-2 text-xs text-fg-heading">
                        <span className="font-medium text-fg-heading">De :</span>
                        <span className="text-red-400">{data.from}</span>
                    </div>
                    <div className="mt-1 flex items-center gap-2 text-xs text-fg-heading">
                        <span className="font-medium text-fg-heading">Objet :</span>
                        <span className="text-fg-heading">{data.subject}</span>
                    </div>
                </div>
                <div className="px-4 py-4">
                    <div className="whitespace-pre-wrap text-sm leading-relaxed text-fg-heading">{data.body}</div>
                </div>
            </div>

            <div>
                <label className="mb-1.5 block text-xs font-medium text-fg-heading">
                    Identifie les indices suspects et décris la bonne réaction
                </label>
                <textarea
                    rows={3}
                    value={answer}
                    onChange={e => setAnswer(e.target.value)}
                    placeholder="Ex: domaine suspect, urgence artificielle, lien externe…"
                    className="w-full resize-none rounded-xl border border-border bg-surface/40 px-3 py-2 text-sm text-white placeholder-black focus:border-primary focus:outline-none"
                />
            </div>

            <button
                onClick={() => onSubmit(answer.trim())}
                disabled={isPending || !answer.trim()}
                className="w-full rounded-xl bg-primary py-2.5 text-sm font-semibold text-white hover:bg-primary-hover disabled:opacity-50 transition-colors"
            >
                {isPending ? "Vérification…" : "Valider ma réponse"}
            </button>
        </div>
    );
}

// ── Email-Sort Mission (drag and drop) ────────────────────────────────────────
function EmailSortMission({ data, isPending, onSubmit }: { data: EmailSortData; isPending: boolean; onSubmit: (a: string) => void }) {
    const [items, setItems] = useState<EmailEntry[]>(
        () => [...data.emails].sort(() => Math.random() - 0.5)
    );

    const sensors = useSensors(
        useSensor(PointerSensor, { activationConstraint: { distance: 5 } }),
        useSensor(TouchSensor,   { activationConstraint: { delay: 200, tolerance: 5 } })
    );

    const handleDragEnd = useCallback((event: DragEndEvent) => {
        const { active, over } = event;
        if (over && active.id !== over.id) {
            setItems(prev => {
                const oldIdx = prev.findIndex(e => e.id === active.id);
                const newIdx = prev.findIndex(e => e.id === over.id);
                return arrayMove(prev, oldIdx, newIdx);
            });
        }
    }, []);

    return (
        <div className="space-y-4">
            <p className="text-sm text-fg-heading">{data.context}</p>
            <div className="flex items-center gap-2 text-xs text-fg-heading rounded-lg border border-border bg-surface px-3 py-2">
                <span>⬆️</span>
                <span>Glisse les emails — le plus suspect en haut, le moins suspect en bas</span>
            </div>

            <DndContext sensors={sensors} collisionDetection={closestCenter} onDragEnd={handleDragEnd}>
                <SortableContext items={items.map(e => e.id)} strategy={verticalListSortingStrategy}>
                    <div className="space-y-2">
                        {items.map((email, i) => (
                            <SortableEmailRow key={email.id} email={email} rank={i + 1} />
                        ))}
                    </div>
                </SortableContext>
            </DndContext>

            <button
                onClick={() => onSubmit(items.map(e => e.id).join(","))}
                disabled={isPending}
                className="w-full rounded-xl bg-primary py-2.5 text-sm font-semibold text-white hover:bg-primary-hover disabled:opacity-50 transition-colors"
            >
                {isPending ? "Vérification…" : "Valider l'ordre"}
            </button>
        </div>
    );
}

function SortableEmailRow({ email, rank }: { email: EmailEntry; rank: number }) {
    const { attributes, listeners, setNodeRef, transform, transition, isDragging } =
        useSortable({ id: email.id });

    const style: React.CSSProperties = {
        transform: CSS.Transform.toString(transform),
        transition,
        opacity: isDragging ? 0.5 : 1,
    };

    return (
        <div
            ref={setNodeRef}
            style={style}
            {...attributes}
            {...listeners}
            className="flex items-start gap-3 rounded-xl border border-border bg-surface px-4 py-3 cursor-grab active:cursor-grabbing select-none"
        >
            <div className="mt-0.5 flex h-6 w-6 flex-shrink-0 items-center justify-center rounded-full bg-surface-2 text-xs font-bold text-fg-heading">
                {rank}
            </div>
            <div className="min-w-0 flex-1">
                <div className="truncate text-xs text-fg-heading">{email.from}</div>
                <div className="mt-0.5 truncate text-sm font-medium text-fg-heading">{email.subject}</div>
                <div className="mt-0.5 truncate text-xs text-fg-heading">{email.preview}</div>
            </div>
            <div className="mt-1 flex-shrink-0 text-fg-heading">⣿</div>
        </div>
    );
}

// ── Chat Mission ──────────────────────────────────────────────────────────────
function ChatMission({ data, isPending, onSubmit }: { data: ChatData; isPending: boolean; onSubmit: (a: string) => void }) {
    const [answer, setAnswer] = useState("");
    const [visible, setVisible] = useState(0);

    useEffect(() => {
        if (visible >= data.messages.length) return;
        const t = setTimeout(() => setVisible(v => v + 1), 700);
        return () => clearTimeout(t);
    }, [visible, data.messages.length]);

    return (
        <div className="space-y-4">
            <p className="text-sm text-fg-heading">{data.context}</p>

            {/* Teams-style chat */}
            <div className="rounded-2xl border border-border bg-surface/80 overflow-hidden">
                {/* Header */}
                <div className="flex items-center gap-3 border-b border-border bg-surface px-4 py-3">
                    <div className="flex h-8 w-8 items-center justify-center rounded-full bg-primary/80 text-sm font-bold text-white">
                        {data.contact.avatar}
                    </div>
                    <div>
                        <div className="text-sm font-semibold text-fg-heading">{data.contact.name}</div>
                        <div className="text-xs text-fg-heading">{data.contact.title}</div>
                    </div>
                    <div className="ml-auto flex items-center gap-1.5">
                        <div className="h-2 w-2 rounded-full bg-emerald-500" />
                        <span className="text-xs text-fg-heading">en ligne</span>
                    </div>
                </div>

                {/* Messages */}
                <div className="min-h-[120px] space-y-3 px-4 py-4">
                    {data.messages.slice(0, visible).map((msg, i) => (
                        <div key={i} className={`flex ${msg.from === "you" ? "justify-end" : "justify-start"}`}>
                            {msg.from === "other" && (
                                <div className="mr-2 flex h-6 w-6 flex-shrink-0 items-center justify-center rounded-full bg-primary/50 text-xs font-bold text-white">
                                    {data.contact.avatar}
                                </div>
                            )}
                            <div className={[
                                "max-w-xs rounded-2xl px-3 py-2 text-sm",
                                msg.from === "other"
                                    ? "rounded-tl-none bg-surface-2 text-fg-heading"
                                    : "rounded-tr-none bg-primary/80 text-white",
                            ].join(" ")}>
                                {msg.text}
                            </div>
                        </div>
                    ))}
                    {visible < data.messages.length && (
                        <div className="flex items-center gap-1.5 px-2">
                            <div className="h-1.5 w-1.5 animate-bounce rounded-full bg-neutral-500" style={{ animationDelay: "0ms" }} />
                            <div className="h-1.5 w-1.5 animate-bounce rounded-full bg-neutral-500" style={{ animationDelay: "150ms" }} />
                            <div className="h-1.5 w-1.5 animate-bounce rounded-full bg-neutral-500" style={{ animationDelay: "300ms" }} />
                        </div>
                    )}
                </div>
            </div>

            <div>
                <label className="mb-1.5 block text-xs font-medium text-fg-heading">
                    Identifie les signaux suspects et décris ta réaction
                </label>
                <textarea
                    rows={3}
                    value={answer}
                    onChange={e => setAnswer(e.target.value)}
                    placeholder="Ex: vérifier l'identité via les RH, ne pas transmettre d'accès…"
                    className="w-full resize-none rounded-xl border border-border bg-surface/40 px-3 py-2 text-sm text-white placeholder-black focus:border-primary focus:outline-none"
                />
            </div>

            <button
                onClick={() => onSubmit(answer.trim())}
                disabled={isPending || !answer.trim()}
                className="w-full rounded-xl bg-primary py-2.5 text-sm font-semibold text-white hover:bg-primary-hover disabled:opacity-50 transition-colors"
            >
                {isPending ? "Vérification…" : "Valider ma réponse"}
            </button>
        </div>
    );
}

// ── Terminal / Scareware Mission ──────────────────────────────────────────────
function TerminalMission({ data, isPending, onSubmit }: { data: TerminalData; isPending: boolean; onSubmit: (a: string) => void }) {
    const [showPopup, setShowPopup] = useState(true);
    const [answer, setAnswer] = useState("");

    return (
        <div className="space-y-4">
            <p className="text-sm text-fg-heading">{data.context}</p>

            {/* Desktop simulation */}
            <div className="relative rounded-2xl border border-border bg-surface/80 overflow-hidden" style={{ minHeight: "260px" }}>
                {/* Fake desktop */}
                <div className="h-full px-6 py-8 text-center text-fg-heading text-xs">
                    [Bureau — dossier\_projet\_Q4.xlsx en cours d&apos;édition]
                </div>

                {/* Popup overlay */}
                {showPopup && (
                    <div className="absolute inset-0 flex items-center justify-center bg-sidebar/60 backdrop-blur-sm">
                        <div className="w-full max-w-sm rounded-xl border border-red-500/60 bg-surface shadow-2xl shadow-red-900/40 overflow-hidden">
                            {/* Window title bar */}
                            <div className="flex items-center gap-2 bg-red-900/80 px-3 py-2">
                                <div className="h-3 w-3 rounded-full bg-red-500" />
                                <div className="h-3 w-3 rounded-full bg-yellow-500" />
                                <div className="h-3 w-3 rounded-full bg-green-500" />
                                <span className="ml-2 text-xs font-semibold text-red-100">Alerte Sécurité</span>
                            </div>
                            <div className="px-4 py-4">
                                <div className="mb-2 text-base font-bold text-red-400">{data.popup.title}</div>
                                <div className="whitespace-pre-wrap text-xs leading-relaxed text-fg-heading">{data.popup.message}</div>
                                <div className="mt-4 flex flex-col gap-2">
                                    {data.popup.buttons.map((btn, i) => (
                                        <button
                                            key={i}
                                            onClick={() => i === data.popup.buttons.length - 1 && setShowPopup(false)}
                                            className={[
                                                "w-full rounded-lg py-1.5 text-xs font-semibold transition-colors",
                                                i === 0
                                                    ? "bg-red-600 text-white hover:bg-red-700"
                                                    : i === data.popup.buttons.length - 1
                                                    ? "border border-border bg-surface-2 text-fg-heading hover:bg-surface-2"
                                                    : "bg-orange-600 text-white hover:bg-orange-700",
                                            ].join(" ")}
                                        >
                                            {btn}
                                        </button>
                                    ))}
                                </div>
                            </div>
                        </div>
                    </div>
                )}
            </div>

            {!showPopup && (
                <div className="rounded-lg border border-emerald-900/60 bg-emerald-950/20 px-3 py-2 text-xs text-emerald-300">
                    Tu as fermé la fenêtre. Décris maintenant pourquoi c&apos;était la bonne réaction.
                </div>
            )}

            <div>
                <label className="mb-1.5 block text-xs font-medium text-fg-heading">
                    Que fais-tu face à cette alerte et pourquoi ?
                </label>
                <textarea
                    rows={3}
                    value={answer}
                    onChange={e => setAnswer(e.target.value)}
                    placeholder="Ex: fermer la fenêtre, c'est un scareware…"
                    className="w-full resize-none rounded-xl border border-border bg-surface/40 px-3 py-2 text-sm text-white placeholder-black focus:border-primary focus:outline-none"
                />
            </div>

            <button
                onClick={() => onSubmit(answer.trim())}
                disabled={isPending || !answer.trim()}
                className="w-full rounded-xl bg-primary py-2.5 text-sm font-semibold text-white hover:bg-primary-hover disabled:opacity-50 transition-colors"
            >
                {isPending ? "Vérification…" : "Valider ma réponse"}
            </button>
        </div>
    );
}

// ── Fallback ──────────────────────────────────────────────────────────────────
function FallbackMission({ instructions, isPending, onSubmit, answer, setAnswer }: {
    instructions: string; isPending: boolean; onSubmit: (a: string) => void;
    answer: string; setAnswer: (v: string) => void;
}) {
    return (
        <div className="space-y-4">
            <div className="rounded-2xl border border-border bg-surface px-5 py-5">
                <div className="whitespace-pre-wrap text-sm leading-relaxed text-fg-heading">{instructions}</div>
            </div>
            <textarea
                rows={4}
                value={answer}
                onChange={e => setAnswer(e.target.value)}
                placeholder="Rédigez votre réponse…"
                className="w-full resize-none rounded-xl border border-border bg-surface/40 px-3 py-2 text-sm text-white placeholder-black focus:border-primary focus:outline-none"
            />
            <button
                onClick={() => onSubmit(answer.trim())}
                disabled={isPending || !answer.trim()}
                className="w-full rounded-xl bg-primary py-2.5 text-sm font-semibold text-white hover:bg-primary-hover disabled:opacity-50 transition-colors"
            >
                {isPending ? "Vérification…" : "Valider ma réponse"}
            </button>
        </div>
    );
}

// ── Shared UI ─────────────────────────────────────────────────────────────────
function TypeBadge({ type }: { type: string }) {
    const info = TYPE_INFO[type];
    return (
        <span className="rounded-md border border-border bg-surface-2 px-2 py-0.5 text-xs text-fg-heading">
            {info?.icon} {info?.label ?? type}
        </span>
    );
}

function DifficultyBadge({ difficulty }: { difficulty: number }) {
    const map: Record<number, { label: string; color: string }> = {
        1: { label: "Facile",    color: "text-emerald-400 bg-emerald-400/10 border-emerald-400/20" },
        2: { label: "Moyen",     color: "text-yellow-400 bg-yellow-400/10 border-yellow-400/20" },
        3: { label: "Difficile", color: "text-orange-400 bg-orange-400/10 border-orange-400/20" },
        4: { label: "Expert",    color: "text-red-400 bg-red-400/10 border-red-400/20" },
        5: { label: "Maître",    color: "text-purple-400 bg-purple-400/10 border-purple-400/20" },
    };
    const d = map[difficulty] ?? { label: String(difficulty), color: "text-fg-heading border-border" };
    return (
        <span className={`rounded-md border px-2 py-0.5 text-xs ${d.color}`}>{d.label}</span>
    );
}

function LoadingSkeleton() {
    return (
        <div className="mx-auto max-w-3xl space-y-4 px-4 py-8">
            <div className="h-8 w-48 animate-pulse rounded-lg bg-surface-2" />
            <div className="h-48 animate-pulse rounded-2xl border border-border bg-surface" />
        </div>
    );
}

function ErrorState({ msg }: { msg?: string }) {
    return (
        <div className="mx-auto max-w-3xl px-4 py-8">
            <div className="rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-600">
                {msg || "Challenge introuvable."}
            </div>
        </div>
    );
}

// ── Constants ─────────────────────────────────────────────────────────────────
const TYPE_INFO: Record<string, { label: string; icon: string; color: string; accent: string }> = {
    quiz:        { label: "Quiz",          icon: "🧠", color: "from-blue-900/60 to-neutral-900",   accent: "text-blue-300" },
    scenario:    { label: "Scénario",      icon: "🎭", color: "from-purple-900/60 to-neutral-900", accent: "text-purple-300" },
    email:       { label: "Email",         icon: "📧", color: "from-yellow-900/60 to-neutral-900", accent: "text-yellow-300" },
    "email-sort":{ label: "Tri d'emails",  icon: "📬", color: "from-orange-900/60 to-neutral-900", accent: "text-orange-300" },
    chat:        { label: "Chat",          icon: "💬", color: "from-teal-900/60 to-neutral-900",   accent: "text-teal-300" },
    terminal:    { label: "Fausse alerte", icon: "🚨", color: "from-red-900/60 to-neutral-900",    accent: "text-red-300" },
    flag:        { label: "Flag",          icon: "🚩", color: "from-neutral-800 to-neutral-900",   accent: "text-fg-heading" },
    code:        { label: "Code",          icon: "💻", color: "from-neutral-800 to-neutral-900",   accent: "text-fg-heading" },
};

const BRIEF_TEXT: Record<string, string> = {
    quiz:         "Une question à choix multiples t'attend. Lis attentivement le contexte, réfléchis à chaque option avant de répondre.",
    scenario:     "Tu es confronté à une situation réelle. Analyse la scène, identifie les risques et choisis la meilleure réaction.",
    email:        "Un email suspect vient d'arriver dans ta boîte. Ton objectif : identifier les signaux d'hameçonnage et décrire la bonne réaction.",
    "email-sort": "Ta messagerie affiche plusieurs emails après ton absence. Classe-les du plus suspect au moins suspect par glisser-déposer.",
    chat:         "Un inconnu te contacte par messagerie instantanée. Identifie les techniques d'ingénierie sociale et décris ta réaction.",
    terminal:     "Une fenêtre inattendue apparaît sur ton écran. Reconnaître un scareware et réagir correctement peut éviter un incident grave.",
};
