"use client";

import {
    Users, Briefcase, Heart, Calculator, Scale, Shield, ShoppingCart,
    GraduationCap, Wrench, BookOpen, Building2, Stethoscope, Activity,
    Rocket, Flame, Star,
} from "lucide-react";
import type { ComponentType } from "react";

// Set d'icônes Lucide (outline uniquement, conforme au Design System) proposé pour les équipes.
// La clé est le nom stocké en base (Team.Icon), la valeur le composant Lucide.
export const TEAM_ICON_COMPONENTS: Record<string, ComponentType<{ size?: number; strokeWidth?: number }>> = {
    Users, Briefcase, Heart, Calculator, Scale, Shield, ShoppingCart,
    GraduationCap, Wrench, BookOpen, Building2, Stethoscope, Activity,
    Rocket, Flame, Star,
};

export const TEAM_ICON_NAMES = Object.keys(TEAM_ICON_COMPONENTS);

/** Rend l'icône d'une équipe (fallback « Users » si nom inconnu/absent). */
export function renderTeamIcon(name: string | null | undefined, size = 16, strokeWidth = 1.75) {
    const Cmp = TEAM_ICON_COMPONENTS[name ?? "Users"] ?? Users;
    return <Cmp size={size} strokeWidth={strokeWidth} />;
}
