"use client";

import { useMemo, useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { Layers, Plus, Award, CircleSlash, BookOpen } from "lucide-react";
import { apiFetch } from "@/lib/api";
import { DEMO_FLASHCARDS, type Flashcard } from "@/lib/flashcards-demo";
import Reveal from "@/components/Reveal";
import VisionCard from "@/components/vision/VisionCard";
import { VisionInput, VisionSelect, VisionButton } from "@/components/vision/VisionForm";
import { toast } from "@/components/Toast";

type EditableModule = { id: string; title: string; sortOrder: number; challengeCount: number };
type EditablePath = { id: string; title: string; modules: EditableModule[] };

// Jeux de cartes disponibles (contenu réel existant : set QCM démo). Extensible.
const CARD_SETS: { id: string; label: string; cards: Flashcard[] }[] = [
    { id: "cyber-fondamentaux", label: "Cybersécurité — fondamentaux (QCM)", cards: DEMO_FLASHCARDS },
];

export default function AdminFlashcardsPage() {
    const qc = useQueryClient();
    const pathsQ = useQuery<EditablePath[]>({ queryKey: ["admin", "editable-paths"], queryFn: () => apiFetch<EditablePath[]>("/api/admin/paths/editable") });

    const [pathId, setPathId] = useState("");
    const [moduleId, setModuleId] = useState("");
    const [title, setTitle] = useState("Épreuve flashcards");
    const [note, setNote] = useState(true);
    const [cardSetId, setCardSetId] = useState(CARD_SETS[0].id);

    const paths = pathsQ.data ?? [];
    const selectedPath = paths.find(p => p.id === pathId);
    const modules = selectedPath?.modules ?? [];
    const cardSet = CARD_SETS.find(s => s.id === cardSetId) ?? CARD_SETS[0];

    const createM = useMutation({
        mutationFn: () => apiFetch(`/api/admin/paths/modules/${moduleId}/flashcards-epreuve`, {
            method: "POST",
            body: JSON.stringify({
                title: title.trim(),
                note,
                cards: cardSet.cards.map(c => ({ id: c.id, category: c.category, front: c.front, back: c.back, choices: c.choices, correctIndex: c.correctIndex })),
            }),
        }),
        onSuccess: () => { toast.ok("Module flashcards ajouté au parcours"); qc.invalidateQueries({ queryKey: ["admin", "editable-paths"] }); },
        onError: (e: Error) => toast.er(e.message || "Erreur"),
    });

    const canSubmit = useMemo(() => !!pathId && !!moduleId && !!title.trim() && cardSet.cards.length > 0, [pathId, moduleId, title, cardSet]);
    const fieldLabel: React.CSSProperties = { fontSize: 12.5, fontWeight: 600, color: "var(--v-text)", marginBottom: 6, display: "block" };

    return (
        <div className="vision-dashboard" style={{ minHeight: "100%" }}>
            <div style={{ maxWidth: 760, margin: "0 auto", padding: "24px 20px 80px", display: "flex", flexDirection: "column", gap: 20 }}>
                <Reveal>
                    <div style={{ display: "flex", alignItems: "center", gap: 10 }}>
                        <span style={{ display: "inline-flex", width: 38, height: 38, borderRadius: 11, alignItems: "center", justifyContent: "center", background: "color-mix(in srgb, var(--v-accent) 16%, transparent)", color: "var(--v-accent)" }}><Layers size={20} /></span>
                        <div>
                            <h1 style={{ fontSize: 24, fontWeight: 700, color: "var(--v-text)", letterSpacing: "-0.02em" }}>Module Flashcards (Épreuve)</h1>
                            <p style={{ marginTop: 4, fontSize: 13, color: "var(--v-text-2)" }}>Ajoutez une épreuve flashcards à un parcours de votre entreprise. Mode Épreuve uniquement.</p>
                        </div>
                    </div>
                </Reveal>

                {pathsQ.isLoading ? (
                    <div style={{ textAlign: "center", padding: 48, color: "var(--v-text-2)" }}>Chargement…</div>
                ) : paths.length === 0 ? (
                    <Reveal><VisionCard><div style={{ textAlign: "center", padding: 24, color: "var(--v-text-2)" }}>Aucun parcours propre à votre entreprise pour l&apos;instant. Les parcours du catalogue partagé ne sont pas éditables.</div></VisionCard></Reveal>
                ) : (
                    <Reveal>
                        <VisionCard>
                            <div style={{ display: "flex", flexDirection: "column", gap: 16 }}>
                                <div>
                                    <label style={fieldLabel}>Parcours</label>
                                    <VisionSelect value={pathId} onChange={e => { setPathId(e.target.value); setModuleId(""); }}>
                                        <option value="">Choisir un parcours…</option>
                                        {paths.map(p => <option key={p.id} value={p.id}>{p.title}</option>)}
                                    </VisionSelect>
                                </div>

                                <div>
                                    <label style={fieldLabel}>Module (étape du parcours)</label>
                                    <VisionSelect value={moduleId} onChange={e => setModuleId(e.target.value)} disabled={!selectedPath}>
                                        <option value="">{selectedPath ? "Choisir un module…" : "Sélectionnez d'abord un parcours"}</option>
                                        {modules.map(m => <option key={m.id} value={m.id}>{m.title} · {m.challengeCount} exercice{m.challengeCount > 1 ? "s" : ""}</option>)}
                                    </VisionSelect>
                                    {selectedPath && modules.length === 0 && <p style={{ marginTop: 6, fontSize: 12, color: "var(--v-text-2)" }}>Ce parcours n&apos;a pas encore de module.</p>}
                                </div>

                                <div>
                                    <label style={fieldLabel}>Titre du module flashcards</label>
                                    <VisionInput value={title} onChange={e => setTitle(e.target.value)} placeholder="Épreuve flashcards" maxLength={200} />
                                </div>

                                <div>
                                    <label style={fieldLabel}>Jeu de cartes</label>
                                    <VisionSelect value={cardSetId} onChange={e => setCardSetId(e.target.value)}>
                                        {CARD_SETS.map(s => <option key={s.id} value={s.id}>{s.label} — {s.cards.length} cartes</option>)}
                                    </VisionSelect>
                                    <div style={{ marginTop: 8, display: "flex", alignItems: "center", gap: 6, fontSize: 12, color: "var(--v-text-2)" }}>
                                        <BookOpen size={13} /> {cardSet.cards.length} cartes QCM · exemple : « {cardSet.cards[0]?.front.slice(0, 60)}… »
                                    </div>
                                </div>

                                <div>
                                    <label style={fieldLabel}>Barème</label>
                                    <div style={{ display: "flex", gap: 10, flexWrap: "wrap" }}>
                                        <NoteChoice active={note} onClick={() => setNote(true)} icon={<Award size={15} />} title="Noté" desc="Le résultat alimente le score (points)." />
                                        <NoteChoice active={!note} onClick={() => setNote(false)} icon={<CircleSlash size={15} />} title="Non noté" desc="Complète l'étape sans impacter le score." />
                                    </div>
                                </div>

                                <div style={{ marginTop: 4 }}>
                                    <VisionButton type="button" onClick={() => createM.mutate()} disabled={!canSubmit || createM.isPending}>
                                        <Plus size={15} /> {createM.isPending ? "Ajout…" : "Ajouter au parcours"}
                                    </VisionButton>
                                </div>
                            </div>
                        </VisionCard>
                    </Reveal>
                )}
            </div>
        </div>
    );
}

function NoteChoice({ active, onClick, icon, title, desc }: { active: boolean; onClick: () => void; icon: React.ReactNode; title: string; desc: string }) {
    return (
        <button type="button" onClick={onClick} style={{
            flex: "1 1 220px", textAlign: "left", cursor: "pointer", borderRadius: 12, padding: 14,
            border: "1px solid " + (active ? "var(--v-accent)" : "var(--v-border)"),
            background: active ? "color-mix(in srgb, var(--v-accent) 14%, transparent)" : "var(--v-surface-2)",
            transition: "border-color .15s ease, background-color .15s ease",
        }}>
            <div style={{ display: "flex", alignItems: "center", gap: 8, fontSize: 14, fontWeight: 700, color: active ? "var(--v-accent)" : "var(--v-text)" }}>{icon} {title}</div>
            <div style={{ marginTop: 4, fontSize: 12, color: "var(--v-text-2)", lineHeight: 1.45 }}>{desc}</div>
        </button>
    );
}
