export type Me = {
    id: string;
    firstName: string;
    lastName: string;
    email: string;
    role: "admin" | "user" | string;
    tenantId: string;
    tenantName: string;
};

export type AssignmentMine = {
    pathId: string;
    pathTitle: string | null;
    pathLevel: string | null;
    status: "assigned" | "started" | "completed" | string;
    dueAt: string | null;
    assignedAt: string;
    startedAt: string | null;
    completedAt: string | null;
    updatedAt: string;
    progressStatus: string;
    progressPercent: number;
};

export type PathDetail = {
    path: {
        id: string;
        type: string;
        title: string;
        description: string | null;
        level: string | null;
        status: string;
    };
    modules: Array<{
        id: string;
        title: string;
        sortOrder: number;
        challenges: ChallengeItem[];
    }>;
};

export type ChallengeItem = {
    id: string;
    moduleId: string;
    type: string;
    contentType?: string;
    title: string;
    instructions: string;
    difficulty: number | null;
    points: number;
    status: string;
    // Consignes pédagogiques (peuvent être absentes → fallback gracieux côté UI)
    instructionTitle?: string | null;
    instructionBody?: string | null;
    instructionShortReminder?: string | null;
};

export type RecentSubmission = {
    id: string;
    challengeId: string;
    challengeTitle: string;
    isCorrect: boolean;
    scoreAwarded: number;
    submittedAt: string;
};

export type SubmissionResult = {
    id: string;
    userId: string;
    attemptNo: number;
    isCorrect: boolean;
    scoreAwarded: number;
    submittedAt: string;
    correctAnswer?: string;
    explanation?: string;
};
