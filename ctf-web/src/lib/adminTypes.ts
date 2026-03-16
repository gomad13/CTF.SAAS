export type StatsOverviewDto = {
    total: number;
    grey: number;
    yellow: number;
    green: number;
    red: number;
    avgProgress: number;
    avgScore: number;
};

export type TrackingUserRowDto = {
    userId: string;
    displayName: string;
    email: string;
    progressPercent: number;
    score: number;
    statusColor: "grey" | "yellow" | "green" | "red";
    lastActivityAt: string | null;
};

export type PagedResult<T> = {
    items: T[];
    page: number;
    pageSize: number;
    total: number;
};

export type AdminPathListItemDto = {
    id: string;
    title: string;
    status: string;
    publishedAt: string | null;
};

