// Types alignés sur les DTOs C# (Contracts/Coaching/CoachingDtos.cs).
// Sérialisation JSON .NET par défaut → camelCase.

export type CoachingFeedback = {
    id: string;
    challengeAttemptId: string;
    challengeType: string;
    content: string;
    /** "Generated" | "Fallback" | "Cached" | "Failed" */
    status: string;
    createdAt: string; // ISO 8601 UTC
};

export type CoachingHistoryPage = {
    items: CoachingFeedback[];
    page: number;
    pageSize: number;
    totalCount: number;
};
