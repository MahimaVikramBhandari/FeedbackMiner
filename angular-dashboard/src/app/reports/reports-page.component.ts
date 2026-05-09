import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { ChartConfiguration, ChartOptions } from 'chart.js';
import { BaseChartDirective } from 'ng2-charts';
import { forkJoin } from 'rxjs';

import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

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
    BaseChartDirective
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
  evaluationTrendData: ChartConfiguration<'line'>['data'] = {
    labels: [],
    datasets: [
      {
        label: 'Overall quality',
        data: [],
        borderColor: '#7c3aed',
        backgroundColor: 'rgba(124, 58, 237, 0.12)',
        pointBackgroundColor: '#7c3aed',
        tension: 0.35,
        fill: true
      }
    ]
  };
  evaluationTrendOptions: ChartOptions<'line'> = {
    responsive: true,
    maintainAspectRatio: false,
    scales: {
      x: { grid: { display: false } },
      y: { beginAtZero: true, max: 5, grid: { color: '#eef0f4' } }
    },
    plugins: {
      legend: { display: false }
    }
  };

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
        this.updateReportCharts();
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

  hasEvaluationTrendData(): boolean {
    return this.evaluationTrendData.datasets[0].data.some(value => Number(value) > 0);
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

  private updateReportCharts(): void {
    const latestEvaluations = this.evaluations.slice(0, 6).reverse();

    this.evaluationTrendData = {
      ...this.evaluationTrendData,
      labels: latestEvaluations.map(evaluation => new Date(evaluation.createdAt).toLocaleDateString()),
      datasets: [
        {
          ...this.evaluationTrendData.datasets[0],
          data: latestEvaluations.map(evaluation => Number((evaluation.overallQualityScore ?? 0).toFixed(1)))
        }
      ]
    };
  }
}
