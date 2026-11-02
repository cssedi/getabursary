import { Dialog } from '@angular/cdk/dialog';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { errorInterceptor } from '../api/error.interceptor';
import { AuthService } from './auth.service';

const ME = { id: 'u-1', email: 'thandi@example.co.za', roles: ['Student'] };

describe('AuthService', () => {
  let auth: AuthService;
  let http: HttpTestingController;
  let open: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    sessionStorage.clear();
    open = vi.fn();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([errorInterceptor])),
        provideHttpClientTesting(),
        { provide: Dialog, useValue: { open } },
      ],
    });
    auth = TestBed.inject(AuthService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  async function loadAs(me: typeof ME | null) {
    const loading = auth.load();
    const req = http.expectOne('/api/v1/me');
    if (me) req.flush(me);
    else req.flush({ title: 'Unauthorized' }, { status: 401, statusText: 'Unauthorized' });
    await loading;
  }

  it('load_signedIn_setsUserAndAdminFlag', async () => {
    await loadAs({ ...ME, roles: ['Student', 'Admin'] });
    expect(auth.user()?.email).toBe(ME.email);
    expect(auth.isAdmin()).toBe(true);
  });

  it('load_401_meansSignedOut', async () => {
    await loadAs(null);
    expect(auth.user()).toBeNull();
    expect(auth.isAdmin()).toBe(false);
  });

  it('load_calledTwice_requestsOnce', async () => {
    const first = auth.load();
    auth.load();
    http.expectOne('/api/v1/me').flush(ME);
    await first;
  });

  it('requireAuth_signedIn_runsHandlerImmediately', async () => {
    await loadAs(ME);
    const run = vi.fn();
    auth.handle('save', run);
    auth.requireAuth({ type: 'save', bursaryId: 'b-1' });
    expect(run).toHaveBeenCalledWith({ type: 'save', bursaryId: 'b-1' });
    expect(open).not.toHaveBeenCalled();
  });

  it('requireAuth_signedOut_opensDialogAndReplaysAfterSignIn', async () => {
    await loadAs(null);
    const run = vi.fn();
    auth.handle('save', run);
    auth.requireAuth({ type: 'save', bursaryId: 'b-1' });
    await vi.waitFor(() => expect(open).toHaveBeenCalled());
    expect(run).not.toHaveBeenCalled();

    const signingIn = auth.signIn('thandi@example.co.za', 'password1');
    const login = http.expectOne('/api/v1/auth/login?useCookies=true');
    expect(login.request.body).toEqual({ email: 'thandi@example.co.za', password: 'password1' });
    login.flush({});
    await vi.waitFor(() => http.expectOne('/api/v1/me').flush(ME));
    await signingIn;

    expect(run).toHaveBeenCalledOnce();
    expect(run).toHaveBeenCalledWith({ type: 'save', bursaryId: 'b-1' });
  });

  it('pendingActionSurvivesRedirect_replaysWhenHandlerRegistersAfterLoad', async () => {
    sessionStorage.setItem('pendingAuthAction', JSON.stringify({ type: 'save', bursaryId: 'b-2' }));
    await loadAs(ME);
    const run = vi.fn();
    auth.handle('save', run);
    expect(run).toHaveBeenCalledWith({ type: 'save', bursaryId: 'b-2' });
    expect(sessionStorage.getItem('pendingAuthAction')).toBeNull();
  });

  it('openSignIn_discardsAStalePendingAction', async () => {
    await loadAs(null);
    auth.requireAuth({ type: 'save', bursaryId: 'b-1' });
    auth.openSignIn();
    expect(sessionStorage.getItem('pendingAuthAction')).toBeNull();
  });

  it('signOut_clearsUser', async () => {
    await loadAs(ME);
    const out = auth.signOut();
    http.expectOne('/api/v1/auth/logout').flush({});
    await out;
    expect(auth.user()).toBeNull();
  });
});
