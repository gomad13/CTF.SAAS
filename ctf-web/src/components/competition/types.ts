export type ScoreboardEntry = {
    rank: number;
    userId: string;
    displayName: string;
    initials: string;
    score: number;
    challengesCompleted: number;
    isCurrentUser: boolean;
    isTopThree: boolean;
    basePoints?: number;
    speedBonus?: number;
    teamName?: string | null;
    teamColor?: string | null;
};

export type TeamLeaderboardEntry = {
    rank: number;
    teamId: string;
    name: string;
    color: string | null;
    icon: string | null;
    score: number;
    memberCount: number;
    isCurrentUserTeam: boolean;
    isTopThree: boolean;
};

export type MyRank = {
    individualRank: number | null;
    individualScore: number;
    individualBasePoints: number;
    individualSpeedBonus: number;
    totalParticipants: number;
    teamId: string | null;
    teamName: string | null;
    teamColor: string | null;
    teamIcon: string | null;
    teamRank: number | null;
    teamScore: number;
    totalTeams: number;
};

export type ScoreboardPage = {
    items: ScoreboardEntry[];
    page: number;
    pageSize: number;
    total: number;
};

export type Podium = {
    first: ScoreboardEntry | null;
    second: ScoreboardEntry | null;
    third: ScoreboardEntry | null;
};
