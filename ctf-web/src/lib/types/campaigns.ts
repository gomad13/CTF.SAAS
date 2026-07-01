export type CampaignStatus = "Upcoming" | "Active" | "Completed";

export type CampaignContentType = "Parcours" | "Scenario";

export type CampaignSummary = {
    id: string;
    name: string;
    description: string | null;
    startDate: string;
    endDate: string;
    status: CampaignStatus;
    assignedCount: number;
    contentCount: number;
    globalCompletion: number;
    isArchived: boolean;
    createdAt: string;
};

export type CampaignContent = {
    id: string;
    contentType: CampaignContentType;
    contentId: string;
    title: string;
    category: string | null;
    displayOrder: number;
};

export type CampaignAssignmentRow = {
    id: string;
    userId: string;
    email: string;
    firstName: string;
    lastName: string;
    assignedAt: string;
};

export type CampaignDetail = {
    id: string;
    name: string;
    description: string | null;
    startDate: string;
    endDate: string;
    status: CampaignStatus;
    assignedToWholeTenant: boolean;
    isArchived: boolean;
    createdAt: string;
    updatedAt: string;
    contents: CampaignContent[];
    assignments: CampaignAssignmentRow[];
};

export type EmployeeProgress = {
    userId: string;
    email: string;
    firstName: string;
    lastName: string;
    completionPercentage: number;
    status: "NotStarted" | "InProgress" | "Completed";
    isLate: boolean;
};

export type CampaignDashboard = {
    campaignId: string;
    name: string;
    status: CampaignStatus;
    totalAssigned: number;
    notStarted: number;
    inProgress: number;
    completed: number;
    globalCompletionPercentage: number;
    averageSuccessRate: number;
    lateEmployeesCount: number;
    employeeProgress: EmployeeProgress[];
};

export type AvailableContent = {
    contentType: CampaignContentType;
    contentId: string;
    title: string;
    category: string | null;
};

export type EmployeeCampaignContent = {
    campaignContentId: string;
    contentType: CampaignContentType;
    contentId: string;
    title: string;
    status: "NotStarted" | "InProgress" | "Completed" | "Failed";
    completionPercentage: number | null;
    isSuccess: boolean | null;
};

export type EmployeeCampaign = {
    campaignId: string;
    name: string;
    description: string | null;
    status: CampaignStatus;
    startDate: string;
    endDate: string;
    myCompletionPercentage: number;
    contents: EmployeeCampaignContent[];
};

export type CreateCampaignPayload = {
    name: string;
    description: string | null;
    startDate: string;
    endDate: string;
    contents: { contentType: CampaignContentType; contentId: string; displayOrder: number }[];
    assignToWholeTenant: boolean;
    assignedUserIds: string[] | null;
};

export type UpdateCampaignPayload = {
    name: string;
    description: string | null;
    startDate: string;
    endDate: string;
    contents: { contentType: CampaignContentType; contentId: string; displayOrder: number }[];
};

export type AssignEmployeesPayload = {
    assignToWholeTenant: boolean;
    userIds: string[] | null;
};

export const STATUS_STYLES: Record<CampaignStatus, { label: string; bg: string; color: string; border: string }> = {
    Upcoming: { label: "À venir", bg: "#F1F5F9", color: "#475569", border: "#CBD5E1" },
    Active: { label: "En cours", bg: "rgba(16,185,129,0.10)", color: "#065F46", border: "rgba(16,185,129,0.25)" },
    Completed: { label: "Terminée", bg: "rgba(59,130,246,0.10)", color: "#1E40AF", border: "rgba(59,130,246,0.25)" },
};
