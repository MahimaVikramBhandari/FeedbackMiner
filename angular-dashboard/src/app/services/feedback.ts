import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map, timeout } from 'rxjs';
import { environment } from '../../environments/environment';

export interface ApiResponse<T> {
  success: boolean;
  count?: number;
  page?: number;
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
  productArea?: string;
  category?: string;
  customerSegment?: string;
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
}

export interface Theme {
  id: string;
  themeId?: string;
  label: string;
  description: string;
  relevanceScore: number;
  feedbackCount: number;
  averageSentimentScore: number;
  averageUrgencyScore: number;
  impactScore: number;
  createdAt: string;
  updatedAt: string;
  affectedAreas?: string[];
  affectedSegments?: string[];
  topRecommendations?: ActionRecommendation[];
}

export interface ActionRecommendation {
  id: string;
  title: string;
  description: string;
  category: string;
  priority: string;
  estimatedEffort: number;
  impactScore: number;
  usefulnessRating?: number;
  status?: string;
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
  averageClusterQuality?: number;
  duplicateDetectionPrecision?: number;
  averageThemeRelevance?: number;
  averageActionUsefulness?: number;
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
  highPriorityActions: ActionRecommendation[];
  feedbackSourceBreakdown: Record<string, number>;
  productAreaBreakdown: Record<string, number>;
  sentimentBreakdown: Record<string, number>;
}

export interface ClusterExport {
  clusterNumber: number;
  suggestedTheme: string;
  itemCount: number;
  averageSimilarity: number;
  silhouetteScore: number;
  feedbackItems: FeedbackItem[];
}

export interface EvaluationHistoryItem {
  evaluationRunId: string;
  processingRunId: string;
  createdAt: string;
  completedAt?: string;
  status: string;
  themeRelevance: { score: number; metPercentage: number };
  clusteringPrecision: number;
  recommendationUsefulness: { score: number; metPercentage: number };
  overallQualityScore: number;
}

export interface DashboardAssistantResponse {
  answer: string;
}

export interface SummaryResponse {
  success: boolean;
  summary: string;
  data?: unknown;
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
  private readonly requestTimeoutMs = environment.requestTimeoutMs ?? 15000;

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
      .pipe(timeout(this.requestTimeoutMs), map(res => (res.data ?? []).map(theme => this.normalizeTheme(theme))));
  }

  getThemeDashboard(pageSize = 10): Observable<Theme[]> {
    return this.http
      .get<ApiResponse<Theme[]>>(`${this.apiUrl}/themes/dashboard`, { params: { pageSize } })
      .pipe(timeout(this.requestTimeoutMs), map(res => (res.data ?? []).map(theme => this.normalizeTheme(theme))));
  }

  getThemeRecommendations(themeId: string): Observable<ActionRecommendation[]> {
    return this.http
      .get<ApiResponse<ActionRecommendation[]>>(`${this.apiUrl}/themes/${themeId}/recommendations`)
      .pipe(timeout(this.requestTimeoutMs), map(res => res.data ?? []));
  }

  getThemeFeedback(themeId: string, take = 20): Observable<FeedbackItem[]> {
    return this.http
      .get<ApiResponse<FeedbackItem[]>>(`${this.apiUrl}/themes/${themeId}/feedback`, { params: { take } })
      .pipe(timeout(this.requestTimeoutMs), map(res => res.data ?? []));
  }

  /* -------- PIPELINE  ----  */

  runPipeline(request: RunPipelineRequest): Observable<ProcessingRun> {
    return this.http
      .post<ApiResponse<ProcessingRun>>(`${this.apiUrl}/analysis/run-pipeline`, request)
      .pipe(timeout(this.requestTimeoutMs), map(res => res.data));
  }

  getDuplicates(threshold = 0.75, limit = 20): Observable<unknown[]> {
    return this.http
      .get<ApiResponse<unknown[]>>(`${this.apiUrl}/analysis/duplicates`, { params: { threshold, limit } })
      .pipe(timeout(this.requestTimeoutMs), map(res => res.data ?? []));
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

  getClusterExport(processingRunId: string): Observable<ClusterExport[]> {
    return this.http
      .get<ApiResponse<ClusterExport[]>>(`${this.apiUrl}/reports/clusters-export/${processingRunId}`)
      .pipe(timeout(this.requestTimeoutMs), map(res => res.data ?? []));
  }

  getClusterExportUrl(processingRunId: string): string {
    return `${this.apiUrl}/reports/clusters-export/${processingRunId}`;
  }

  evaluateRun(processingRunId: string): Observable<unknown> {
    return this.http
      .post<ApiResponse<unknown>>(`${this.apiUrl}/evaluation/evaluate/${processingRunId}`, {})
      .pipe(timeout(this.requestTimeoutMs), map(res => res.data));
  }

  getEvaluationHistory(pageSize = 10, page = 0): Observable<EvaluationHistoryItem[]> {
    return this.http
      .get<ApiResponse<EvaluationHistoryItem[]>>(`${this.apiUrl}/evaluation/history`, { params: { pageSize, page } })
      .pipe(timeout(this.requestTimeoutMs), map(res => res.data ?? []));
  }

  getWeeklyDigestCsvUrl(): string {
    return `${this.apiUrl}/reports/weekly-digest-csv`;
  }

  getNotebookHtmlUrl(processingRunId: string): string {
    return `${this.apiUrl}/notebooks/export/html/${processingRunId}`;
  }

  getNotebookJsonUrl(processingRunId: string): string {
    return `${this.apiUrl}/notebooks/export/json/${processingRunId}`;
  }

  askDashboardAssistant(question: string): Observable<DashboardAssistantResponse> {
    return this.http
      .post<ApiResponse<DashboardAssistantResponse>>(`${this.apiUrl}/assistant/dashboard-guide`, { question })
      .pipe(timeout(this.requestTimeoutMs), map(res => res.data));
  }

  askSummarize(question: string): Observable<SummaryResponse> {
    return this.http
      .post<ApiResponse<SummaryResponse>>(`${this.apiUrl}/summarize/ask`, { question })
      .pipe(timeout(this.requestTimeoutMs), map(res => res.data));
  }

  private normalizeTheme(theme: Theme): Theme {
    return {
      ...theme,
      id: theme.id ?? theme.themeId ?? '',
    };
  }
}
