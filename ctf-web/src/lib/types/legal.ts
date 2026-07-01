export type LegalDocumentSummary = {
    slug: string;
    title: string;
    version: string;
    isRequired: boolean;
    publishedAt: string;
};

export type LegalDocument = {
    slug: string;
    title: string;
    version: string;
    contentHtml: string;
    isRequired: boolean;
    publishedAt: string;
    changeLog: string | null;
};

export type ConsentItem = {
    documentSlug: string;
    documentVersion: string;
    accepted: boolean;
};

export type UserConsent = {
    id: string;
    documentSlug: string;
    documentVersion: string;
    documentTitle: string;
    accepted: boolean;
    acceptedAt: string;
    ipAddress: string | null;
    userAgent: string | null;
    source: string;
    isCurrentVersion: boolean;
};

export type MissingConsent = {
    documentSlug: string;
    documentTitle: string;
    currentVersion: string;
    lastAcceptedVersion: string | null;
    changeLog: string | null;
};

export type ConsentStatus = {
    isUpToDate: boolean;
    missingConsents: MissingConsent[];
};
