// Realistic sample API objects for page and service tests. Dates are fixed so tests are deterministic.
import {
  BursaryDetailResponse,
  BursarySummaryResponse,
  CreateReportRequest,
  FacultyResponse,
  FieldOfStudyResponse,
  InstitutionResponse,
  PagedResponse,
  QualificationResponse,
} from '../app/core/api/models';

export const institutions: InstitutionResponse[] = [
  {
    id: '0196a5f0-0000-7000-8000-000000000001',
    name: 'University of Cape Town',
    shortName: 'UCT',
    slug: 'university-of-cape-town',
    type: 'TraditionalUniversity',
    province: 'WesternCape',
  },
  {
    id: '0196a5f0-0000-7000-8000-000000000002',
    name: 'University of the Witwatersrand',
    shortName: 'Wits',
    slug: 'university-of-the-witwatersrand',
    type: 'TraditionalUniversity',
    province: 'Gauteng',
  },
];

export const faculties: FacultyResponse[] = [
  {
    id: '0196a5f0-0000-7000-8000-000000000011',
    name: 'Faculty of Engineering and the Built Environment',
    slug: 'engineering-and-the-built-environment',
  },
  {
    id: '0196a5f0-0000-7000-8000-000000000012',
    name: 'Faculty of Science',
    slug: 'science',
  },
];

export const qualifications: QualificationResponse[] = [
  {
    id: '0196a5f0-0000-7000-8000-000000000021',
    name: 'BSc (Eng) Electrical Engineering',
    abbreviation: 'BSc(Eng) EE',
    studyLevel: 'Undergraduate',
    slug: 'bsc-eng-electrical-engineering',
    fieldCodes: ['08'],
  },
  {
    id: '0196a5f0-0000-7000-8000-000000000022',
    name: 'BSc Chemical Engineering',
    abbreviation: 'BSc ChemEng',
    studyLevel: 'Undergraduate',
    slug: 'bsc-chemical-engineering',
    fieldCodes: ['08', '0801'],
  },
  {
    id: '0196a5f0-0000-7000-8000-000000000023',
    name: 'BSc Honours Chemistry',
    abbreviation: 'BSc(Hons) Chem',
    studyLevel: 'Honours',
    slug: 'bsc-honours-chemistry',
    fieldCodes: ['14'],
  },
];

export const fields: FieldOfStudyResponse[] = [
  {
    id: '0196a5f0-0000-7000-8000-000000000031',
    code: '08',
    name: 'Engineering',
    slug: 'engineering',
    parentCode: null,
    children: [
      {
        id: '0196a5f0-0000-7000-8000-000000000032',
        code: '0801',
        name: 'Chemical Engineering',
        slug: 'chemical-engineering',
        parentCode: '08',
        children: [],
      },
    ],
  },
  {
    id: '0196a5f0-0000-7000-8000-000000000033',
    code: '14',
    name: 'Physical Sciences',
    slug: 'physical-sciences',
    parentCode: null,
    children: [],
  },
];

const sourceZabursaries = {
  key: 'zabursaries',
  name: 'ZABursaries',
  iconUrl: 'https://www.zabursaries.co.za/favicon.ico',
};

/** Open, not closing soon. */
export const openBursary: BursarySummaryResponse = {
  id: '0196a5f0-0000-7000-8000-000000000101',
  slug: 'anglo-american-engineering-bursary-2027',
  name: 'Anglo American Engineering Bursary 2027',
  funderName: 'Anglo American',
  closingDate: '2027-03-31',
  isClosingSoon: false,
  status: 'Open',
  studyLevels: ['Undergraduate'],
  minAverage: 65,
  coverage: ['Tuition', 'Books', 'Stipend'],
  sources: [
    {
      ...sourceZabursaries,
      url: 'https://www.zabursaries.co.za/anglo-american-engineering-bursary-2027',
    },
    {
      key: 'studytrust',
      name: 'StudyTrust',
      iconUrl: 'https://studytrust.org.za/favicon.ico',
      url: 'https://studytrust.org.za/listings/anglo-american-engineering',
    },
  ],
  matchScore: 3.5,
};

/** Open and closing within 14 days of the fixture date (2026-11-02). */
export const closingSoonBursary: BursarySummaryResponse = {
  id: '0196a5f0-0000-7000-8000-000000000102',
  slug: 'sasol-chemistry-honours-bursary',
  name: 'Sasol Chemistry Honours Bursary',
  funderName: 'Sasol',
  closingDate: '2026-11-10',
  isClosingSoon: true,
  status: 'Open',
  studyLevels: ['Honours'],
  minAverage: 70,
  coverage: ['Tuition', 'Accommodation', 'Meals', 'Laptop'],
  sources: [
    {
      ...sourceZabursaries,
      url: 'https://www.zabursaries.co.za/sasol-chemistry-honours-bursary',
    },
  ],
  matchScore: 2,
};

/** Closed; the API returns this summary with `410 Gone`. */
export const closedBursary: BursarySummaryResponse = {
  id: '0196a5f0-0000-7000-8000-000000000103',
  slug: 'nsfas-top-achiever-bursary-2026',
  name: 'Top Achiever Bursary 2026',
  funderName: null,
  closingDate: '2026-09-30',
  isClosingSoon: false,
  status: 'Closed',
  studyLevels: [],
  minAverage: null,
  coverage: ['Other'],
  sources: [
    {
      key: 'allbursaries',
      name: 'AllBursaries',
      iconUrl: 'https://allbursaries.co.za/favicon.ico',
      url: 'https://allbursaries.co.za/top-achiever-2026',
    },
  ],
  matchScore: 0,
};

export const bursarySummaries: BursarySummaryResponse[] = [openBursary, closingSoonBursary, closedBursary];

export const bursaryDetail: BursaryDetailResponse = {
  ...openBursary,
  summary: 'Funds full tuition and books for final-year-entry engineering students who meet the academic criteria.',
  valueSummary: 'Full tuition, books and a monthly stipend for the academic year.',
  workBackRequired: true,
  workBackDetails: 'One year of employment at an Anglo American site for each year funded.',
  minSubjectRequirements: [
    { subject: 'Mathematics', minPercent: 70 },
    { subject: 'Physical Sciences', minPercent: 60 },
  ],
  citizenship: 'SouthAfrican',
  provinces: [],
  fields: [{ code: '08', name: 'Engineering' }],
  qualifications: [
    {
      id: '0196a5f0-0000-7000-8000-000000000021',
      name: 'BSc (Eng) Electrical Engineering',
      institutionShortName: 'UCT',
    },
  ],
  institutions: [{ id: '0196a5f0-0000-7000-8000-000000000001', shortName: 'UCT' }],
  openDate: '2026-08-01',
  applyUrl: 'https://www.zabursaries.co.za/anglo-american-engineering-bursary-2027',
  updatedAt: '2026-10-20T08:30:00Z',
};

export const searchPage: PagedResponse<BursarySummaryResponse> = {
  items: [openBursary, closingSoonBursary],
  page: 1,
  pageSize: 20,
  totalCount: 2,
};

export const reportRequest: CreateReportRequest = {
  reason: 'WrongClosingDate',
  comment: 'The closing date on the funder site is 30 April 2027.',
  contactEmail: 'student@example.co.za',
};
