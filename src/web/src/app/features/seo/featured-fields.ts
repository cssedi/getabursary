/**
 * The six field landing pages linked from the home page (hard-coded, specs/05-web §3, T-031).
 * Slugs must match `field_of_study.slug` in `data/seed/fields-of-study.json`.
 */
export const FEATURED_FIELDS: readonly { slug: string; name: string }[] = [
  { slug: 'engineering', name: 'Engineering' },
  { slug: 'health-professions-and-related-clinical-sciences', name: 'Health professions' },
  { slug: 'business-economics-and-management-studies', name: 'Business and economics' },
  { slug: 'computer-and-information-sciences', name: 'Computer and information sciences' },
  { slug: 'education', name: 'Education' },
  { slug: 'law', name: 'Law' },
];
