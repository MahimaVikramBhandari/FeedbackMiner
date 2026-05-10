import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { finalize, forkJoin } from 'rxjs';

import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';

import {
  EvaluationHistoryItem,
  FeedbackService,
  ProcessingRun,
  WeeklyDigest
} from '../services/feedback';

@Component({
  selector: 'app-reports-page',
  standalone: true,
  imports: [
    CommonModule,
    MatButtonModule,
    MatCardModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatTooltipModule
  ],
  templateUrl: './reports-page.html',
  styleUrls: ['./reports-page.scss']
})
export class ReportsPageComponent implements OnInit, OnDestroy {

  digest: WeeklyDigest | null = null;
  runs: ProcessingRun[] = [];
  evaluations: EvaluationHistoryItem[] = [];

  loading = false;
  evaluatingRunId: string | null = null;
  evaluationStartedAt: number | null = null;
  evaluationElapsedSeconds = 0;
  private evaluationTimerId: ReturnType<typeof setInterval> | null = null;
  error: string | null = null;
  message: string | null = null;
  summaryLoading = false;
  summaryError: string | null = null;
  summaryText: string | null = null;

  constructor(
    private feedbackService: FeedbackService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadReports();
  }

  ngOnDestroy(): void {
    this.stopEvaluationTimer();
  }

  loadReports(showLoading = true): void {
    if (showLoading) {
      this.loading = true;
    }

    this.error = null;

    forkJoin({
      digest: this.feedbackService.getWeeklyDigest(),
      runs: this.feedbackService.getProcessingRuns(),
      evaluations: this.feedbackService.getEvaluationHistory(10)
    }).subscribe({
      next: ({ digest, runs, evaluations }) => {
        this.digest = digest;
        this.runs = runs ?? [];
        this.evaluations = evaluations ?? [];
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: (error) => {
        this.error = this.describeError(error, 'Report request failed.');
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  summarize(prompt: string): void {
    if (this.summaryLoading) {
      return;
    }

    this.summaryLoading = true;
    this.summaryError = null;
    this.summaryText = null;

    this.feedbackService.askSummarize(prompt)
      .pipe(finalize(() => {
        this.summaryLoading = false;
        this.cdr.detectChanges();
      }))
      .subscribe({
        next: (response) => {
          this.summaryText = response?.summary ?? 'No summary available.';
        },
        error: (error) => {
          this.summaryError = this.describeError(error, 'Summary failed.');
        }
      });
  }

  latestEvaluation(): EvaluationHistoryItem | null {
    return this.evaluations?.[0] ?? null;
  }

  hasEvaluationData(): boolean {
    return this.latestEvaluation() !== null;
  }

  getThemeRelevanceScore(): number {
    return this.latestEvaluation()?.themeRelevance.score ?? 0;
  }

  getDuplicatePrecisionScore(): number {
    return this.latestEvaluation()?.clusteringPrecision ?? 0;
  }

  getRecommendationUsefulnessScore(): number {
    return this.latestEvaluation()?.recommendationUsefulness.score ?? 0;
  }

  getOverallQualityScore(): number {
    return this.latestEvaluation()?.overallQualityScore ?? 0;
  }

  getMetricPercent(value: number, max: number): number {
    if (max <= 0) {
      return 0;
    }

    return Math.min(100, Math.max(0, (value / max) * 100));
  }

  meetsTarget(value: number, target: number): boolean {
    return value >= target;
  }

  qualityStatus(value: number, target: number): string {
    return this.meetsTarget(value, target) ? 'Pass' : 'Needs work';
  }

  runId(run: ProcessingRun): string {
    return run.runId ?? run.id ?? '';
  }

  hasEvaluationForRun(run: ProcessingRun): boolean {
    const processingRunId = this.runId(run);

    return !!processingRunId && this.evaluations.some(
      evaluation => evaluation.processingRunId === processingRunId
    );
  }

  evaluate(run: ProcessingRun): void {
    const processingRunId = this.runId(run);

    if (!processingRunId) {
      this.error = 'Processing run id is missing.';
      return;
    }

    this.evaluatingRunId = processingRunId;
    this.evaluationStartedAt = Date.now();
    this.evaluationElapsedSeconds = 0;
    this.error = null;
    this.message = null;
    this.startEvaluationTimer();

    this.feedbackService.evaluateRun(processingRunId).subscribe({
      next: () => {
        const duration = this.formatElapsed(this.evaluationElapsedSeconds);
        this.message = `Evaluation completed in ${duration}.`;
        this.evaluatingRunId = null;
        this.stopEvaluationTimer();
        this.cdr.detectChanges();
        this.loadReports(false);
      },
      error: (error) => {
        const duration = this.formatElapsed(this.evaluationElapsedSeconds);
        this.error = `${this.describeError(error, 'Evaluation failed.')} Evaluation stopped after ${duration}.`;
        this.evaluatingRunId = null;
        this.stopEvaluationTimer();
        this.cdr.detectChanges();
      }
    });
  }

  getEvaluationStatusText(): string {
    if (!this.evaluatingRunId) {
      return '';
    }

    return `Running for ${this.formatElapsed(this.evaluationElapsedSeconds)}`;
  }

  openWeeklyDigestCsv(): void {
    window.open(this.feedbackService.getWeeklyDigestCsvUrl(), '_blank');
  }

  openNotebook(run: ProcessingRun, format: 'html' | 'json'): void {
    const processingRunId = this.runId(run);

    if (!processingRunId) {
      this.error = 'Processing run id is missing.';
      return;
    }

    if (!this.hasEvaluationForRun(run)) {
      this.error = 'Run evaluation first, then open the notebook.';
      return;
    }

    const url = format === 'html'
      ? this.feedbackService.getNotebookHtmlUrl(processingRunId)
      : this.feedbackService.getNotebookJsonUrl(processingRunId);

    window.open(url, '_blank');
  }

  private formatElapsed(totalSeconds: number): string {
    return totalSeconds < 60
      ? `${totalSeconds}s`
      : `${Math.floor(totalSeconds / 60)}m ${totalSeconds % 60}s`;
  }

  private startEvaluationTimer(): void {
    this.stopEvaluationTimer();

    this.evaluationTimerId = setInterval(() => {
      if (!this.evaluationStartedAt) {
        return;
      }

      this.evaluationElapsedSeconds = Math.floor((Date.now() - this.evaluationStartedAt) / 1000);
      this.cdr.detectChanges();
    }, 1000);
  }

  private stopEvaluationTimer(): void {
    if (this.evaluationTimerId) {
      clearInterval(this.evaluationTimerId);
      this.evaluationTimerId = null;
    }
  }

  private describeError(error: any, fallback: string): string {
    if (error?.status === 0) {
      return 'Backend not reachable. Run dotnet API.';
    }

    return error?.error?.error ?? error?.message ?? fallback;
  }
}
