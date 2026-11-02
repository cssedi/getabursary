import { TestBed } from '@angular/core/testing';
import { openBursary } from '../../../../testing/fixtures';
import { SourceIconsComponent } from './source-icons.component';

describe('SourceIconsComponent', () => {
  function create() {
    const fixture = TestBed.createComponent(SourceIconsComponent);
    fixture.componentRef.setInput('sources', openBursary.sources);
    fixture.detectChanges();
    return fixture;
  }

  it('render_perSource_showsLinkedIcon', () => {
    const el = create().nativeElement as HTMLElement;
    const links = el.querySelectorAll('a');
    expect(links.length).toBe(2);
    expect(links[0].getAttribute('href')).toBe(openBursary.sources[0].url);
    expect(links[0].getAttribute('target')).toBe('_blank');
    expect(links[0].getAttribute('rel')).toBe('noopener nofollow');
    const img = links[0].querySelector('img')!;
    expect(img.getAttribute('alt')).toBe('ZABursaries');
    expect(img.getAttribute('loading')).toBe('lazy');
    expect(img.getAttribute('width')).toBe('16');
  });

  it('iconError_showsInitialsFallback', () => {
    const fixture = create();
    const el = fixture.nativeElement as HTMLElement;
    el.querySelector('img')!.dispatchEvent(new Event('error'));
    fixture.detectChanges();
    expect(el.querySelector('.source-initials')?.textContent?.trim()).toBe('Z');
    expect(el.querySelectorAll('img').length).toBe(1);
  });
});
