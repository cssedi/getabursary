import { BursaryDetailResponse, Citizenship, Province } from '../../core/api/models';

export const CITIZENSHIP_LABELS: Record<Citizenship, string> = {
  SouthAfrican: 'South African citizens only',
  SouthAfricanOrPermanentResident: 'South African citizens or permanent residents',
  Any: 'Open to any citizenship',
  Unknown: 'Not specified',
};

export const PROVINCE_LABELS: Record<Province, string> = {
  EasternCape: 'Eastern Cape',
  FreeState: 'Free State',
  Gauteng: 'Gauteng',
  KwaZuluNatal: 'KwaZulu-Natal',
  Limpopo: 'Limpopo',
  Mpumalanga: 'Mpumalanga',
  NorthWest: 'North West',
  NorthernCape: 'Northern Cape',
  WesternCape: 'Western Cape',
};

export const DESCRIPTION_MAX_LENGTH = 155;

/** Collapses whitespace and cuts at 155 characters, ending with an ellipsis when cut. */
export function truncateDescription(text: string): string {
  const clean = text.replace(/\s+/g, ' ').trim();
  if (clean.length <= DESCRIPTION_MAX_LENGTH) {
    return clean;
  }
  return `${clean.slice(0, DESCRIPTION_MAX_LENGTH - 1).trimEnd()}…`;
}

/** schema.org `MonetaryGrant` for an open bursary (specs/05-web §7). Optional fields are omitted when unknown. */
export function grantJsonLd(bursary: BursaryDetailResponse): Record<string, unknown> {
  return {
    '@context': 'https://schema.org',
    '@type': 'MonetaryGrant',
    name: bursary.name,
    ...(bursary.funderName
      ? { funder: { '@type': 'Organization', name: bursary.funderName } }
      : {}),
    ...(bursary.applyUrl ? { url: bursary.applyUrl } : {}),
    ...(bursary.closingDate ? { applicationDeadline: bursary.closingDate } : {}),
  };
}
