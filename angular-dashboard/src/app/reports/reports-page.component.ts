import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { forkJoin } from 'rxjs';

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
export class ReportsPageComponent implements OnInit {

  digest: WeeklyDigest | null = null;
  runs: ProcessingRun[] = [];
  evaluations: EvaluationHistoryItem[] = [];

  loading = false;
  evaluatingRunId: string | null = null;
  error: string | null = null;
  message: string | null = null;
  summaryLoading = false;
  summaryError: string | null = null;
  summaryText: string | null = null;

  constructor(private feedbackService: FeedbackService) {}

  ngOnInit(): void {
    this.loadReports();
  }

  loadReports(): void {
    this.loading = true;
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
      },
      error: (error) => {
        this.error =
          error?.status === 0
            ? 'Backend not reachable. Run dotnet API.'
            : error?.error?.error ?? error?.message ?? 'Report error';

        this.loading = false;
      }
    });
  }

  summarize(prompt: string): void {
    if (this.summaryLoading) {
      return;
    }

    this.summaryLoading = true;
    this.summaryError = null;

    this.feedbackService.askSummarize(prompt).subscribe({
      next: (response) => {
        this.summaryText = response?.summary ?? 'No summary available.';
        this.summaryLoading = false;
      },
      error: (error) => {
        this.summaryError = error?.error?.error ?? error?.message ?? 'Summary failed.';
        this.summaryLoading = false;
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

  evaluate(run: ProcessingRun): void {
    const processingRunId = this.runId(run);

    if (!processingRunId) {
      this.error = 'Processing run id is missing.';
      return;
    }

    this.evaluatingRunId = processingRunId;
    this.error = null;
    this.message = null;

    this.feedbackService.evaluateRun(processingRunId).subscribe({
      next: () => {
        this.message = 'Evaluation completed.';
        this.evaluatingRunId = null;
        this.loadReports();
      },
      error: (error) => {
        this.error = error?.error?.error ?? error?.message ?? 'Evaluation failed.';
        this.evaluatingRunId = null;
      }
    });
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

    const url = format === 'html'
      ? this.feedbackService.getNotebookHtmlUrl(processingRunId)
      : this.feedbackService.getNotebookJsonUrl(processingRunId);

    window.open(url, '_blank');
  }

  openClusterExport(run: ProcessingRun): void {
    const processingRunId = this.runId(run);

    if (!processingRunId) {
      this.error = 'Processing run id is missing.';
      return;
    }

    window.open(this.feedbackService.getClusterExportUrl(processingRunId), '_blank');
  }
}
