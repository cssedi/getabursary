import { DOCUMENT } from '@angular/common';
import { Injectable, inject } from '@angular/core';
import { Meta, Title } from '@angular/platform-browser';

/** Public origin for canonical and Open Graph URLs (product domain, specs/00-product-brief). */
export const SITE_ORIGIN = 'https://getabursary.co.za';
export const SITE_NAME = 'GetABursary';

const JSON_LD_MARKER = 'data-seo-jsonld';

export interface SeoConfig {
  /** Page name without the site suffix. The service sets the title to "{title} | GetABursary". */
  title: string;
  description: string;
  /** Path only, for example `/bursaries/some-slug`. The service prefixes {@link SITE_ORIGIN}. */
  canonicalPath: string;
  /** Structured data for a `<script type="application/ld+json">` tag. Omitted → no structured data. */
  jsonLd?: Record<string, unknown>;
  /** Value for `<meta name="robots">`, e.g. `noindex`. Omitted → any robots tag is removed. */
  robots?: string;
}

/**
 * Sets page-level SEO tags. Each call replaces the tags set by the previous call, so client-side
 * navigation never leaves duplicates or stale values behind.
 */
@Injectable({ providedIn: 'root' })
export class SeoService {
  private readonly title = inject(Title);
  private readonly meta = inject(Meta);
  private readonly doc = inject(DOCUMENT);

  set(config: SeoConfig): void {
    const fullTitle = `${config.title} | ${SITE_NAME}`;
    const url = `${SITE_ORIGIN}${config.canonicalPath}`;

    this.title.setTitle(fullTitle);
    this.meta.updateTag({ name: 'description', content: config.description });
    this.setCanonical(url);
    this.meta.updateTag({ property: 'og:title', content: fullTitle });
    this.meta.updateTag({ property: 'og:description', content: config.description });
    this.meta.updateTag({ property: 'og:url', content: url });
    this.setRobots(config.robots);
    this.setJsonLd(config.jsonLd);
  }

  private setCanonical(href: string): void {
    let link = this.doc.head.querySelector<HTMLLinkElement>('link[rel="canonical"]');
    if (!link) {
      link = this.doc.createElement('link');
      link.setAttribute('rel', 'canonical');
      this.doc.head.appendChild(link);
    }
    link.setAttribute('href', href);
  }

  private setRobots(robots: string | undefined): void {
    if (robots) {
      this.meta.updateTag({ name: 'robots', content: robots });
    } else {
      this.meta.removeTag('name="robots"');
    }
  }

  private setJsonLd(jsonLd: Record<string, unknown> | undefined): void {
    this.doc.head
      .querySelectorAll(`script[${JSON_LD_MARKER}]`)
      .forEach((script) => script.remove());
    if (!jsonLd) {
      return;
    }
    const script = this.doc.createElement('script');
    script.setAttribute('type', 'application/ld+json');
    script.setAttribute(JSON_LD_MARKER, '');
    // Escaping `<` keeps a value such as `</script>` from closing the tag early.
    script.textContent = JSON.stringify(jsonLd).replace(/</g, '\\u003c');
    this.doc.head.appendChild(script);
  }
}
