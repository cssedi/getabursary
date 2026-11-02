import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { csrfInterceptor } from './csrf.interceptor';

describe('csrfInterceptor', () => {
  it('nonGetRequests_carryHeader_getDoesNot', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(withInterceptors([csrfInterceptor])), provideHttpClientTesting()],
    });
    const client = TestBed.inject(HttpClient);
    const http = TestBed.inject(HttpTestingController);

    client.get('/g').subscribe();
    expect(http.expectOne('/g').request.headers.has('X-Requested-With')).toBe(false);

    client.post('/x', {}).subscribe();
    client.put('/x', {}).subscribe();
    client.delete('/x').subscribe();
    const requests = http.match('/x');
    expect(requests.length).toBe(3);
    for (const r of requests) expect(r.request.headers.get('X-Requested-With')).toBe('getabursary');
  });
});
