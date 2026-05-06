import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map, timeout } from 'rxjs';
import { environment } from '../../environments/environment';

export interface ApiResponse<T> {
  success: boolean;
  count?: number;
  message?: string;
  error?: string;
  data: T;
}

/* =========================
   MODELS
========================= */

export interface FeedbackItem {
  id: string;
  source: string;
  text: string;
  processedText?: string;
  rating?: number;
  productArea: string;
  category: string;
  customerSegment: string;
  createdAt: string;
  language?: string;
  metadataJson?: string;
  sentimentScore?: number;
  sentimentLabel?: string;
  urgencyScore?: number;
  urgencyLevel?: string;
  themeClusterId?: string;
  themeId?: string;
  similarityScore?: number;
}

export interface CreateFeedbackRequest {
  source: string;
  text: string;
  rating?: number | null;
  productArea: string;
  category: string;
  customerSegment: string;
  metadata?: Record<string, unknown>;
}

export interface Theme {
  id: string;
  label: string;
  description: string;
  relevanceScore: number;
  feedbackCount: number;
  averageSentimentScore: number;
  averageUrgencyScore: number;
  impactScore: number;
  createdAt: string;
  updatedAt: string;
}

/* =========================
   REPORT MODELS
========================= */

export interface ProcessingRun {
  id?: string;
  runId?: string;
  name?: string;
  runName?: string;
  status: string;
  feedbackProcessed?: number;
  feedbackItemCount?: number;
  clustersCreated?: number;
  clusterCount?: number;
  themesExtracted?: number;
  themeCount?: number;
  startedAt: string;
  completedAt?: string;
}

export interface WeeklyDigest {
  weekStart: string;
  weekEnd: string;
  totalFeedbackReceived: number;
  newThemesIdentified: number;
  activeThemes: number;
  averageSentiment: number;
  criticalUrgencyCount: number;
  topThemesByImpact: Theme[];
  feedbackSourceBreakdown: Record<string, number>;
  productAreaBreakdown: Record<string, number>;
  sentimentBreakdown: Record<string, number>;
}

/* =========================
   PIPELINE REQUEST
========================= */

export interface RunPipelineRequest {
  runName?: string;
  processAllFeedback: boolean;
  clusterSimilarityThreshold?: number;
}

/* =========================
   SERVICE
========================= */

@Injectable({
  providedIn: 'root',
})
export class FeedbackService {

  private readonly apiUrl = environment.apiBaseUrl;
  private readonly requestTimeoutMs = 15000;

  constructor(private http: HttpClient) {}

  /* -------- FEEDBACK -------- */

  createFeedback(request: CreateFeedbackRequest): Observable<FeedbackItem> {
    return this.http
      .post<ApiResponse<FeedbackItem>>(`${this.apiUrl}/feedback`, request)
      .pipe(timeout(this.requestTimeoutMs), map(res => res.data));
  }

  getFeedback(take = 100): Observable<FeedbackItem[]> {
    return this.http
      .get<ApiResponse<FeedbackItem[]>>(`${this.apiUrl}/feedback`, { params: { take } })
      .pipe(timeout(this.requestTimeoutMs), map(res => res.data ?? []));
  }

  /* -------- THEMES -------- */

  getThemes(take = 50): Observable<Theme[]> {
    return this.http
      .get<ApiResponse<Theme[]>>(`${this.apiUrl}/themes`, { params: { take } })
      .pipe(timeout(this.requestTimeoutMs), map(res => res.data ?? []));
  }

  /* -------- PIPELINE  ----  */

  runPipeline(request: RunPipelineRequest): Observable<unknown> {
    return this.http
      .post<ApiResponse<unknown>>(`${this.apiUrl}/analysis/run-pipeline`, request)
      .pipe(timeout(this.requestTimeoutMs), map(res => res.data));
  }

  /* -------- REPORTS -------- */

  getWeeklyDigest(): Observable<WeeklyDigest> {
    return this.http
      .get<ApiResponse<WeeklyDigest>>(`${this.apiUrl}/reports/weekly-digest`)
      .pipe(timeout(this.requestTimeoutMs), map(res => res.data));
  }

  getProcessingRuns(): Observable<ProcessingRun[]> {
    return this.http
      .get<ApiResponse<ProcessingRun[]>>(`${this.apiUrl}/reports/runs`)
      .pipe(timeout(this.requestTimeoutMs), map(res => res.data ?? []));
  }
}