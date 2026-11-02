import { isPlatformBrowser } from '@angular/common';
import { Injectable, PLATFORM_ID, effect, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { TrackerEntryResponse } from '../../core/api/models';
import { TrackerApi } from '../../core/api/tracker-api';
import { AuthService } from '../../core/auth/auth.service';
import { ToastService } from '../../shared/ui/toast/toast.service';

const SAVE_FAILED = 'Couldn’t save this bursary. Check your connection and try again.';
const REMOVE_FAILED = 'Couldn’t remove this bursary. Check your connection and try again.';

/** Which bursaries the signed-in student has saved, shared by every save button and the tracker page. */
@Injectable({ providedIn: 'root' })
export class TrackerStore {
  private readonly api = inject(TrackerApi);
  private readonly auth = inject(AuthService);
  private readonly toast = inject(ToastService);
  private loading: Promise<TrackerEntryResponse[]> | undefined;

  private readonly _ids = signal<ReadonlySet<string>>(new Set());
  readonly ids = this._ids.asReadonly();

  constructor() {
    const browser = isPlatformBrowser(inject(PLATFORM_ID));
    effect(() => {
      if (!browser) return;
      if (this.auth.user()) {
        this.load().catch(() => undefined);
      } else {
        this.loading = undefined;
        this._ids.set(new Set());
      }
    });
    // The save a signed-out student tapped, finished once they are signed in.
    this.auth.handle('save', (action) => void this.save(String(action['bursaryId'])));
  }

  /** Fetches the tracker once per sign-in; a failure can be retried. */
  load(): Promise<TrackerEntryResponse[]> {
    this.loading ??= firstValueFrom(this.api.list()).then(
      (entries) => {
        this._ids.set(new Set(entries.map((e) => e.bursary.id)));
        return entries;
      },
      (error: unknown) => {
        this.loading = undefined;
        throw error;
      },
    );
    return this.loading;
  }

  toggle(bursaryId: string): void {
    if (this._ids().has(bursaryId)) void this.remove(bursaryId);
    else this.auth.requireAuth({ type: 'save', bursaryId });
  }

  /** Saves optimistically; rolls back and says so if the API refuses. */
  async save(bursaryId: string): Promise<void> {
    try {
      // PUT replaces status and notes, so never "save" something the student already tracks.
      await this.load();
    } catch {
      this.toast.show(SAVE_FAILED);
      return;
    }
    if (this._ids().has(bursaryId)) return;
    this.setSaved(bursaryId, true);
    try {
      await firstValueFrom(this.api.upsert(bursaryId, { status: 'Saved', appliedOn: null, notes: null }));
    } catch {
      this.setSaved(bursaryId, false);
      this.toast.show(SAVE_FAILED);
    }
  }

  /** Removes optimistically; resolves false (after rolling back) if the API refuses. */
  async remove(bursaryId: string): Promise<boolean> {
    this.setSaved(bursaryId, false);
    try {
      await firstValueFrom(this.api.remove(bursaryId));
      return true;
    } catch {
      this.setSaved(bursaryId, true);
      this.toast.show(REMOVE_FAILED);
      return false;
    }
  }

  private setSaved(bursaryId: string, saved: boolean): void {
    const next = new Set(this._ids());
    if (saved) next.add(bursaryId);
    else next.delete(bursaryId);
    this._ids.set(next);
  }
}
