import { appConfig } from './app.config';

interface Nested {
  ɵproviders?: unknown[];
  useValue?: unknown;
  useFactory?: unknown;
}

/** Source text of every factory/value in a (possibly nested) provider list. */
function sources(providers: unknown): string[] {
  if (Array.isArray(providers)) return providers.flatMap(sources);
  const p = providers as Nested;
  if (p?.ɵproviders) return sources(p.ɵproviders);
  return [String(p?.useFactory ?? ''), String(p?.useValue ?? '')];
}

describe('appConfig', () => {
  it('hydration_isConfiguredWithEventReplay', () => {
    // A bare provideClientHydration() has no replay setup; withEventReplay() adds it.
    expect(sources(appConfig.providers).join('\n')).toMatch(/replay/i);
  });
});
