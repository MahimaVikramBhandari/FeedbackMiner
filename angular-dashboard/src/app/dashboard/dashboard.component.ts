import { Component, OnInit, DestroyRef, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ChartConfiguration, ChartOptions } from 'chart.js';
import { BaseChartDirective } from 'ng2-charts';

import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';

import { FeedbackService, FeedbackItem, ProcessingRun, Theme, WeeklyDigest, EvaluationHistoryItem } from '../services/feedback';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
    BaseChartDirective
  ],
  templateUrl: './dashboard.html',
  styleUrls: ['./dashboard.scss']
})
export class DashboardComponent implements OnInit {

  private destroyRef = inject(DestroyRef);

  feedbackItems: FeedbackItem[] = [];
  themes: Theme[] = [];
  digest: WeeklyDigest | null = null;
  latestRun: ProcessingRun | null = null;
  latestEvaluation: EvaluationHistoryItem | null = null;

  loading = true;
  error: string | null = null;

  totalFeedback = 0;
  positiveFeedback = 0;
  negativeFeedback = 0;
  neutralFeedback = 0;
  averageSentiment = 0;
  highUrgencyCount = 0;
  highPriorityActions = 0;
  assistantInput = '';
  assistantLoading = false;
  assistantError: string | null = null;
  assistantMessages: { role: 'assistant' | 'user'; text: string }[] = [];
  sentimentChartData: ChartConfiguration<'pie'>['data'] = {
    labels: ['Positive', 'Neutral', 'Negative'],
    datasets: [
      {
        data: [0, 0, 0],
        backgroundColor: ['#0f8b6f', '#748094', '#c94a4a'],
        borderColor: '#ffffff',
        borderWidth: 2,
      },
    ],
  };
  sentimentChartOptions: ChartOptions<'pie'> = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: {
        display: false,
      },
      tooltip: {
        titleFont: {
          family: 'Roboto, "Helvetica Neue", sans-serif',
          size: 13,
          weight: 650,
        },
        bodyFont: {
          family: 'Roboto, "Helvetica Neue", sans-serif',
          size: 13,
          weight: 400,
        },
        callbacks: {
          label: (context) => `${context.label}: ${context.parsed}`,
        },
      },
    },
  };
  topThemesChartData: ChartConfiguration<'bar'>['data'] = {
    labels: [],
    datasets: [
      {
        label: 'Impact',
        data: [],
        backgroundColor: '#2563eb',
        borderRadius: 6,
        barThickness: 16,
      },
    ],
  };
  topThemesChartOptions: ChartOptions<'bar'> = {
    indexAxis: 'y',
    responsive: true,
    maintainAspectRatio: false,
    scales: {
      x: {
        beginAtZero: true,
        max: 5,
        grid: { color: '#eef0f4' },
        ticks: {
          color: '#667085',
          font: {
            family: 'Roboto, "Helvetica Neue", sans-serif',
            size: 12,
            weight: 500,
          },
        },
      },
      y: {
        grid: { display: false },
        ticks: {
          autoSkip: false,
          color: '#667085',
          padding: 8,
          align: 'end',
          crossAlign: 'far',
          callback: (value) => this.shortenLabel(String(this.topThemesChartData.labels?.[Number(value)] ?? '')),
          font: {
            family: 'Roboto, "Helvetica Neue", sans-serif',
            size: 12,
            weight: 500,
          },
        },
      },
    },
    plugins: {
      legend: { display: false },
      tooltip: {
        titleFont: {
          family: 'Roboto, "Helvetica Neue", sans-serif',
          size: 13,
          weight: 650,
        },
        bodyFont: {
          family: 'Roboto, "Helvetica Neue", sans-serif',
          size: 13,
          weight: 400,
        },
        callbacks: {
          title: (items) => items[0]?.label ?? '',
        },
      },
    },
  };
  constructor(private feedbackService: FeedbackService) {}

  ngOnInit() {
    this.loadDashboardData();
  }

  loadDashboardData() {
    this.loading = true;
    this.error = null;

    forkJoin({
      feedback: this.feedbackService.getFeedback(100),
      themes: this.feedbackService.getThemeDashboard(10),
      digest: this.feedbackService.getWeeklyDigest(),
      runs: this.feedbackService.getProcessingRuns(),
      evaluations: this.feedbackService.getEvaluationHistory(1),
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: ({ feedback, themes, digest, runs, evaluations }) => {
          this.feedbackItems = feedback ?? [];
          this.themes = themes ?? [];
          this.digest = digest;
          this.latestRun = runs?.[0] ?? null;
          this.latestEvaluation = evaluations?.[0] ?? null;

          this.calculateStatistics();
          this.loading = false;
        },
        error: (error) => {
          this.error = this.getErrorMessage(error);
          this.loading = false;
        },
      });
  }

  calculateStatistics() {
    const items = this.feedbackItems;

    this.totalFeedback = items.length;
    this.highPriorityActions = this.digest?.highPriorityActions?.length ?? 0;

    this.positiveFeedback = items.filter(
      f => this.normalize(f.sentimentLabel) === 'positive'
    ).length;

    this.negativeFeedback = items.filter(
      f => this.normalize(f.sentimentLabel) === 'negative'
    ).length;

    this.neutralFeedback = items.filter(
      f => this.normalize(f.sentimentLabel) === 'neutral'
    ).length;

    this.sentimentChartData = {
      ...this.sentimentChartData,
      datasets: [
        {
          ...this.sentimentChartData.datasets[0],
          data: [this.positiveFeedback, this.neutralFeedback, this.negativeFeedback],
        },
      ],
    };

    this.highUrgencyCount = items.filter(
      f => ['high', 'critical'].includes(this.normalize(f.urgencyLevel))
    ).length;

    const scoredItems = items.filter(
      f => typeof f.sentimentScore === 'number'
    );

    this.averageSentiment = this.digest
      ? this.digest.averageSentiment
      : scoredItems.length
      ? scoredItems.reduce((sum, f) => sum + (f.sentimentScore ?? 0), 0) / scoredItems.length
      : 0;

    this.updateDashboardCharts();
  }

  getErrorMessage(error: any): string {
    if (error?.status === 0) {
      return 'Backend is not reachable. Start API and check swagger.';
    }

    if (error?.status === 404) {
      return 'API endpoint not found. Check backend routes.';
    }

    return error?.error?.error ?? error?.message ?? 'Unknown dashboard error';
  }

  getSentimentClass(sentiment: string | undefined): string {
    return `sentiment-${this.normalize(sentiment) || 'unknown'}`;
  }

  hasSentimentData(): boolean {
    return this.positiveFeedback + this.neutralFeedback + this.negativeFeedback > 0;
  }

  hasTopThemeChartData(): boolean {
    return this.themes.some(theme => (theme.impactScore ?? 0) > 0);
  }

  hasEvaluationData(): boolean {
    return this.latestEvaluation !== null;
  }

  getThemeRelevanceScore(): number {
    return this.latestEvaluation?.themeRelevance.score ?? 0;
  }

  getDuplicatePrecisionScore(): number {
    return this.latestEvaluation?.clusteringPrecision ?? 0;
  }

  getRecommendationQualityScore(): number {
    return this.latestEvaluation?.recommendationUsefulness.score ?? 0;
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

  getPriorityClass(priority: string | undefined): string {
    return `priority-${this.normalize(priority) || 'unknown'}`;
  }

  getDashboardFeedbackText(item: FeedbackItem): string {
    return this.toStarMaskedText(item.processedText || item.text);
  }

  getDashboardSource(source: string | undefined): string {
    return this.toStarMaskedText(source);
  }

  private toStarMaskedText(value: string | undefined): string {
    if (!value) {
      return '';
    }

    return value
      .replace(/\[REDACTED_[A-Z_]+\]/g, '********');
  }

  askAssistant() {
    const question = this.assistantInput.trim();
    if (!question || this.assistantLoading) {
      return;
    }

    this.assistantError = null;
    this.assistantLoading = true;
    this.assistantMessages.push({ role: 'user', text: question });
    this.assistantInput = '';

    this.feedbackService.askSummarize(question)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          this.assistantMessages.push({
            role: 'assistant',
            text: response?.summary ?? 'I could not generate a summary right now.'
          });
          this.assistantLoading = false;
        },
        error: (error) => {
          this.assistantError = this.getErrorMessage(error);
          this.assistantLoading = false;
        }
      });
  }

  askQuick(prompt: string) {
    this.assistantInput = prompt;
    this.askAssistant();
  }

  private updateDashboardCharts() {
    const topThemes = this.themes.slice(0, 5);

    this.topThemesChartData = {
      ...this.topThemesChartData,
      labels: topThemes.map(theme => theme.label || 'Theme'),
      datasets: [
        {
          ...this.topThemesChartData.datasets[0],
          data: topThemes.map(theme => Number((theme.impactScore ?? 0).toFixed(1))),
        },
      ],
    };
  }

  private shortenLabel(value: string): string {
    return value.length > 18 ? `${value.slice(0, 17)}...` : value;
  }

  private normalize(value: string | undefined): string {
    return value?.toLowerCase() ?? '';
  }
}
