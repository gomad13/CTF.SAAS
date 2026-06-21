// Paramètres entreprise (admin).

export interface TenantSettings {
    tenantId: string;
    name: string;
    description: string | null;
    sector: string | null;
    googleSsoEnabled: boolean;
    microsoftSsoEnabled: boolean;
    googleSsoConfigured: boolean;     // clé OAuth présente côté serveur (global)
    microsoftSsoConfigured: boolean;
    defaultTeamsOpen: boolean;
    teamsModeEnabled: boolean;
    teamsCount: number;
    createdAt: string;
}

export interface UpdateTenantSettings {
    name: string;
    description: string | null;
    sector: string | null;
    googleSsoEnabled: boolean;
    microsoftSsoEnabled: boolean;
    defaultTeamsOpen: boolean;
}
