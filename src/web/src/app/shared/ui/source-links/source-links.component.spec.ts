import { TestBed } from '@angular/core/testing';
import { openBursary } from '../../../../testing/fixtures';
import { SourceLinksComponent } from './source-links.component';

describe('SourceLinksComponent', () => {
  function create(sources = openBursary.sources) {
    const fixture = TestBed.createComponent(SourceLinksComponent);
    fixture.componentRef.setInput('sources', sources);
    fixture.detectChanges();
    return fixture;
  }

  it('render_everySource_isANamedLinkWithHrefRelAndTarget', () => {
    const el = create().nativeElement as HTMLElement;
    const links = el.querySelectorAll('a');
    expect(links.length).toBe(openBursary.sources.length);
    openBursary.sources.forEach((s, i) => {
      expect(links[i].textContent?.trim()).toBe(s.name);
      expect(links[i].getAttribute('href')).toBe(s.url);
      expect(links[i].getAttribute('target')).toBe('_blank');
      expect(links[i].getAttribute('rel')).toBe('noopener nofollow');
    });
    const img = links[0].querySelector('img')!;
    expect(img.getAttribute('width')).toBe('16');
    expect(img.getAttribute('loading')).toBe('lazy');
    expect(el.textContent).toMatch(/Found on\s+\S.*,\s+\S/);
  });

  it('threeSources_allListedSeparatedByCommas', () => {
    const [a, b] = openBursary.sources;
    const el = create([a, b, { ...b, key: 'c', name: 'AllBursaries', url: 'https://c.example/x' }]).nativeElement as HTMLElement;
    expect(el.querySelectorAll('a').length).toBe(3);
    expect(el.textContent!.match(/,/g)!.length).toBe(2);
  });

  it('iconError_dropsTheImageButKeepsTheNamedLink', () => {
    const fixture = create();
    const el = fixture.nativeElement as HTMLElement;
    el.querySelector('img')!.dispatchEvent(new Event('error'));
    fixture.detectChanges();
    expect(el.querySelectorAll('img').length).toBe(openBursary.sources.length - 1);
    expect(el.querySelectorAll('a').length).toBe(openBursary.sources.length);
  });
});
