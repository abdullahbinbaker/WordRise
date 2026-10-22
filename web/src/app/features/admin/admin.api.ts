import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { DraftDetail, DraftListItem, DraftStatus, GrammarPoint, StaffMember, TwoFactorState } from './admin.models';

const BASE = '/api/v1/admin';

/** The server's own message when it sent one (a rule that was broken), else a fallback. */
export function describeError(error: unknown, fallback: string): string {
  if (error instanceof HttpErrorResponse) {
    const body = error.error as { detail?: string; title?: string } | null;
    return body?.detail || body?.title || fallback;
  }
  return fallback;
}

/** Calls to the staff endpoints; the auth cookie travels with every request. */
@Injectable({ providedIn: 'root' })
export class AdminApi {
  private readonly http = inject(HttpClient);

  curriculum = () => firstValueFrom(this.http.get<GrammarPoint[]>(`${BASE}/lessons/curriculum`));

  drafts = (status?: DraftStatus) =>
    firstValueFrom(this.http.get<DraftListItem[]>(`${BASE}/lessons/drafts`, { params: status ? { status } : {} }));

  draft = (id: string) => firstValueFrom(this.http.get<DraftDetail>(`${BASE}/lessons/drafts/${id}`));

  createDraft = (scenario: string, grammarStep: number, wordCount: number) =>
    firstValueFrom(this.http.post<{ id: string }>(`${BASE}/lessons/drafts`, { scenario, grammarStep, wordCount }));

  approve = (id: string) => firstValueFrom(this.http.post(`${BASE}/lessons/drafts/${id}/approve`, null));

  reject = (id: string, note: string) => firstValueFrom(this.http.post(`${BASE}/lessons/drafts/${id}/reject`, { note }));

  requeue = (id: string) => firstValueFrom(this.http.post(`${BASE}/lessons/drafts/${id}/requeue`, null));

  staff = () => firstValueFrom(this.http.get<StaffMember[]>(`${BASE}/staff`));

  addReviewer = (email: string) => firstValueFrom(this.http.post(`${BASE}/staff/reviewers`, { email }));

  removeReviewer = (id: string) => firstValueFrom(this.http.delete(`${BASE}/staff/reviewers/${id}`));

  twoFactor = () => firstValueFrom(this.http.post<TwoFactorState>('/api/v1/auth/manage/2fa', {}));

  newTwoFactorKey = () => firstValueFrom(this.http.post<TwoFactorState>('/api/v1/auth/manage/2fa', { resetSharedKey: true }));

  enableTwoFactor = (twoFactorCode: string) =>
    firstValueFrom(this.http.post<TwoFactorState>('/api/v1/auth/manage/2fa', { enable: true, twoFactorCode }));
}
