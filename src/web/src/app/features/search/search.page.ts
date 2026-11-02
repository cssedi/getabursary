import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { JsonPipe } from '@angular/common';
import { FilterPanelComponent } from './filter-panel/filter-panel.component';
import { SearchStateService } from './search-state.service';

@Component({
  selector: 'app-search-page',
  imports: [FilterPanelComponent, JsonPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="mx-auto grid max-w-5xl gap-4 p-4 md:grid-cols-[18rem_1fr]">
      <h1 class="text-2xl font-bold md:col-span-2">Search bursaries</h1>
      <app-filter-panel />
      <!-- Placeholder for the result list (T-028). -->
      <pre class="overflow-auto text-sm">{{ state.apiParams() | json }}</pre>
    </div>
  `,
})
export class SearchPage {
  protected readonly state = inject(SearchStateService);
}
