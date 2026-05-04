export type ScenarioCatalogItem = {
    id: string;
    externalId: string;
    version: string;
    name: string;
    description: string;
    category: string;
    difficulty: "easy" | "medium" | "hard";
    durationDays: number;
    emailCount: number;
    attackStepCount: number;
};

export type VsfStep = {
    stepId: string;
    stepOrder: number;
    type: string;
    delayDays: number;
    delayHours: number;
    delayMinutes: number;
    fromCharacterId: string;
    toRecipient: string;
    subject: string;
    bodyTemplate: string;
    isAttackStep: boolean;
    decisionBranches: {
        click: string | null;
        report: string | null;
        ignoreAfterHours: number;
        ignoreNextStepId: string | null;
    } | null;
    hints: string[];
};

export type VsfCharacter = {
    id: string;
    roleLabel: string;
    fictionalEmailPattern: string;
};

export type VsfOutcome = {
    label: string;
    triggerCoaching: boolean;
    criImpact: number;
};

export type ScenarioCatalogDetail = ScenarioCatalogItem & {
    characters?: VsfCharacter[];
    timeline?: VsfStep[];
    outcomes?: Record<string, VsfOutcome>;
};

export type EligibleSender = {
    userId: string;
    firstName: string;
    lastName: string;
    email: string;
};

export type StepCustomization = {
    stepId: string;
    subject?: string;
    bodyTemplate?: string;
};

export type LaunchScenarioRequest = {
    templateId: string;
    targetUserId: string;
    senderUserId: string;
    mode: "normal" | "demo";
    scheduledStartAt?: string | null;
    stepOverrides?: StepCustomization[];
};

export type ScenarioInstanceListItem = {
    id: string;
    templateName: string;
    templateExternalId: string;
    category: string;
    targetEmail: string;
    targetFullName: string;
    senderEmail: string;
    senderFullName: string;
    mode: string;
    status: string;
    currentStepId: string | null;
    scheduledStartAt: string;
    startedAt: string | null;
    completedAt: string | null;
    stopReason: string | null;
    emailsSent: number;
    openedCount: number;
    clickedCount: number;
    reportedCount: number;
};

export type ScenarioInstanceStep = {
    id: string;
    stepId: string;
    stepOrder: number;
    status: string;
    scheduledAt: string;
    sentAt: string | null;
};

export type ScenarioInstanceEmail = {
    id: string;
    subject: string;
    fromName: string;
    fromEmail: string;
    isAttackStep: boolean;
    sentAt: string;
    firstReadAt: string | null;
    firstClickAt: string | null;
    reportedAt: string | null;
};

export type ScenarioInstanceDetail = {
    id: string;
    tenantId: string;
    templateId: string;
    templateName: string;
    templateExternalId: string;
    category: string;
    targetUserId: string;
    targetEmail: string;
    targetFullName: string;
    senderUserId: string;
    senderEmail: string;
    senderFullName: string;
    launchedByUserId: string;
    mode: string;
    status: string;
    currentStepId: string | null;
    scheduledStartAt: string;
    startedAt: string | null;
    completedAt: string | null;
    stopReason: string | null;
    steps: ScenarioInstanceStep[];
    emails: ScenarioInstanceEmail[];
};

export type InboxEmailListItem = {
    id: string;
    subject: string;
    fromName: string;
    fromEmail: string;
    sentAt: string;
    isRead: boolean;
    isReported: boolean;
    isSystemNotification: boolean;
};

export type InboxEmailDetail = InboxEmailListItem & {
    bodyHtml: string;
};

export type ReportPhishingResponse = {
    success: boolean;
    triggeredOutcome: boolean;
    outcomeKey: string;
    message: string;
};

export type ScenarioLandingDetail = {
    emailId: string;
    instanceId: string | null;
    subject: string;
    fromName: string;
    fromEmail: string;
    isAttackStep: boolean;
    wasClicked: boolean;
    wasReported: boolean;
    scenarioName: string | null;
    scenarioCategory: string | null;
    hints: string[];
};

export type SenderConsent = { consentsToBeFictionalSender: boolean };
