// DTO models mirroring the API contract (specs/01-architecture §6, specs/02-data-model §1 and §5,
// tasks T-020 to T-023). Field names are camelCase; enum values are the PascalCase names used by the API.
// Dates are ISO 8601 strings (`YYYY-MM-DD` for dates, full timestamps for `updatedAt`).

export type StudyLevel = 'Matric' | 'Undergraduate' | 'Honours' | 'Masters' | 'Doctoral';

export type BursaryStatus = 'Open' | 'Closed' | 'Unknown';

export type Citizenship = 'SouthAfrican' | 'SouthAfricanOrPermanentResident' | 'Any' | 'Unknown';

export type CoverageItem =
  | 'Tuition'
  | 'Accommodation'
  | 'Books'
  | 'Meals'
  | 'Laptop'
  | 'Stipend'
  | 'Transport'
  | 'Other';

export type InstitutionType =
  | 'TraditionalUniversity'
  | 'ComprehensiveUniversity'
  | 'UniversityOfTechnology'
  | 'Tvet'
  | 'Private';

export type Province =
  | 'EasternCape'
  | 'FreeState'
  | 'Gauteng'
  | 'KwaZuluNatal'
  | 'Limpopo'
  | 'Mpumalanga'
  | 'NorthWest'
  | 'NorthernCape'
  | 'WesternCape';

export type ReportReason =
  | 'WrongClosingDate'
  | 'WrongEligibility'
  | 'BrokenLink'
  | 'BursaryClosed'
  | 'Duplicate'
  | 'Other';

export type SearchSort = 'BestMatch' | 'ClosingSoonest' | 'Newest';

/** Taxonomy: `GET /api/v1/taxonomy/institutions` (T-020). */
export interface InstitutionResponse {
  id: string;
  name: string;
  shortName: string;
  slug: string;
  type: InstitutionType;
  province: Province;
}

/** Taxonomy: `GET /api/v1/taxonomy/institutions/{id}/faculties` (T-020). */
export interface FacultyResponse {
  id: string;
  name: string;
  slug: string;
}

/** Taxonomy: `GET /api/v1/taxonomy/faculties/{id}/qualifications` (T-020). */
export interface QualificationResponse {
  id: string;
  name: string;
  abbreviation: string;
  studyLevel: StudyLevel;
  slug: string;
  fieldCodes: string[];
}

/** Taxonomy: `GET /api/v1/taxonomy/fields` (T-020). First-order fields with nested second-order `children`. */
export interface FieldOfStudyResponse {
  id: string;
  code: string;
  name: string;
  slug: string;
  parentCode: string | null;
  children: FieldOfStudyResponse[];
}

/** Query parameters for `GET /api/v1/bursaries/search` (T-021). Omitted keys are not sent. */
export interface BursarySearchParams {
  qualificationId?: string;
  fieldIds?: string[];
  institutionId?: string;
  studyLevel?: StudyLevel;
  average?: number;
  closingFrom?: string;
  closingTo?: string;
  includeClosed?: boolean;
  sort?: SearchSort;
  page?: number;
  pageSize?: number;
}

/** One source (aggregator) a bursary was found on (FR-05). */
export interface SourceLink {
  key: string;
  name: string;
  iconUrl: string;
  url: string;
}

/** Search result item and closed-bursary summary (specs/02-data-model §5 "Output"). */
export interface BursarySummaryResponse {
  id: string;
  slug: string;
  name: string;
  funderName: string | null;
  closingDate: string | null;
  isClosingSoon: boolean;
  status: BursaryStatus;
  studyLevels: StudyLevel[];
  minAverage: number | null;
  coverage: CoverageItem[];
  sources: SourceLink[];
  matchScore: number;
}

/** Paged list envelope returned by search (T-021). */
export interface PagedResponse<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}

/** One minimum-subject requirement (`min_subject_requirements` jsonb, specs/02-data-model §2.2). */
export interface SubjectRequirement {
  subject: string;
  minPercent: number;
}

/** Full detail for an open bursary: `GET /api/v1/bursaries/{slug}` (T-022). */
export interface BursaryDetailResponse extends BursarySummaryResponse {
  summary: string;
  valueSummary: string | null;
  workBackRequired: boolean | null;
  workBackDetails: string | null;
  minSubjectRequirements: SubjectRequirement[];
  citizenship: Citizenship;
  provinces: Province[];
  fields: { code: string; name: string }[];
  qualifications: { id: string; name: string; institutionShortName: string }[];
  institutions: { id: string; shortName: string }[];
  openDate: string | null;
  applyUrl: string | null;
  updatedAt: string;
}

/** Body of `POST /api/v1/bursaries/{id}/reports` (T-023). */
export interface CreateReportRequest {
  reason: ReportReason;
  comment?: string;
  contactEmail?: string;
}

/** `201 Created` body of the report endpoint (T-023). */
export interface CreateReportResponse {
  id: string;
}
