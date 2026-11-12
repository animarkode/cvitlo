export const APP_ROUTES = {
  home: '/',
  resumes: '/resumes',
  about: '/about',
  account: '/account',
  accountDeleted: '/account-deleted',
} as const;

/** The account page's sections, each a child route of `/account`. The API returns from Keycloak to these paths too. */
export const ACCOUNT_SECTIONS = {
  personalInfo: 'personal-info',
  signingIn: 'signing-in',
  devices: 'devices',
  linkedAccounts: 'linked-accounts',
} as const;
