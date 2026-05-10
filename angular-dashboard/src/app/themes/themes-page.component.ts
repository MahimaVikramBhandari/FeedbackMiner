import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ChartConfiguration, ChartOptions } from 'chart.js';
import { BaseChartDirective } from 'ng2-charts';
import { finalize } from 'rxjs';

import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatRadioModule } from '@angular/material/radio';
import { MatTooltipModule } from '@angular/material/tooltip';

import { FeedbackService, Theme } from '../services/feedback';

@Component({
  selector: 'app-themes-page',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatButtonModule,
    MatCardModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatRadioModule,
    MatTooltipModule,
    BaseChartDirective
  ],
  templateUrl: './themes-page.html',
  styleUrls: ['./themes-page.scss']
})
export class ThemesPageComponent implements OnInit, OnDestroy {

  themes: Theme[] = [];

  loading = false;
  running = false;
  processAllFeedback = true;
  runStartedAt: number | null = null;
  runElapsedSeconds = 0;
  private runTimerId: ReturnType<typeof setInterval> | null = null;
  private postRunRefreshTimerId: ReturnType<typeof setTimeout> | null = null;

  error: string | null = null;
  pipelineMessage: string | null = null;
  summaryLoading = false;
  summaryError: string | null = null;
  summaryText: string | null = null;
  themeChartData: ChartConfiguration<'bar'>['data'] = {
    labels: [],
    datasets: [
      {
        label: 'Impact',
        data: [],
        backgroundColor: '#2563eb',
        borderRadius: 6,
      },
      {
        label: 'Relevance',
        data: [],
        backgroundColor: '#0f766e',
        borderRadius: 6,
      }
    ]
  };
  themeChartOptions: ChartOptions<'bar'> = {
    responsive: true,
    maintainAspectRatio: false,
    scales: {
      x: {
        grid: { display: false },
        ticks: { maxRotation: 0, minRotation: 0 }
      },
      y: {
        beginAtZero: true,
        max: 5,
        grid: { color: '#eef0f4' }
      }
    },
    plugins: {
      legend: { position: 'bottom' }
    }
  };

  constructor(
    private feedbackService: FeedbackService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadThemes();
  }

  ngOnDestroy(): void {
    this.stopRunTimer();
    this.clearPostRunRefresh();
  }

  loadThemes(showLoading = true): void {
    if (showLoading) {
      this.loading = true;
    }

    this.error = null;

    this.feedbackService.getThemeDashboard(100).subscribe({
      next: (themes) => {
        this.themes = themes ?? [];
        this.updateThemeChart();
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: (error) => {
        this.error = this.describeError(error, 'Theme request failed.');
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

  hasThemeChartData(): boolean {
    return this.themes.some(theme => (theme.impactScore ?? 0) > 0 || (theme.relevanceScore ?? 0) > 0);
  }

  runPipeline(): void {
    this.running = true;
    this.runStartedAt = Date.now();
    this.runElapsedSeconds = 0;
    this.error = null;
    this.pipelineMessage = null;
    this.startRunTimer();

    this.feedbackService.runPipeline({
      runName: `Dashboard-${new Date().toISOString()}`,
      processAllFeedback: this.processAllFeedback,
    }).subscribe({
      next: (run) => {
        const duration = this.formatElapsed(this.runElapsedSeconds);
        this.pipelineMessage =
          `Analysis completed in ${duration}: ${run.feedbackProcessed ?? 0} feedback, ` +
          `${run.clustersCreated ?? 0} clusters, ${run.themesExtracted ?? 0} themes.`;
        this.running = false;
        this.stopRunTimer();
        this.cdr.detectChanges();
        this.refreshThemesAfterRun();
      },
      error: (error) => {
        const duration = this.formatElapsed(this.runElapsedSeconds);
        this.error = `${this.describeError(error, 'Analysis failed.')} Analysis stopped after ${duration}.`;
        this.running = false;
        this.stopRunTimer();
        this.cdr.detectChanges();
      }
    });
  }

  getRunStatusText(): string {
    if (!this.running) {
      return '';
    }

    return `Running for ${this.formatElapsed(this.runElapsedSeconds)}`;
  }

  private formatElapsed(totalSeconds: number): string {
    return totalSeconds < 60
      ? `${totalSeconds}s`
      : `${Math.floor(totalSeconds / 60)}m ${totalSeconds % 60}s`;
  }

  private startRunTimer(): void {
    this.stopRunTimer();

    this.runTimerId = setInterval(() => {
      if (!this.runStartedAt) {
        return;
      }

      this.runElapsedSeconds = Math.floor((Date.now() - this.runStartedAt) / 1000);
      this.cdr.detectChanges();
    }, 1000);
  }

  private stopRunTimer(): void {
    if (this.runTimerId) {
      clearInterval(this.runTimerId);
      this.runTimerId = null;
    }
  }

  private refreshThemesAfterRun(): void {
    this.loadThemes(false);
    this.clearPostRunRefresh();

    this.postRunRefreshTimerId = setTimeout(() => {
      this.loadThemes(false);
      this.postRunRefreshTimerId = null;
    }, 1500);
  }

  private clearPostRunRefresh(): void {
    if (this.postRunRefreshTimerId) {
      clearTimeout(this.postRunRefreshTimerId);
      this.postRunRefreshTimerId = null;
    }
  }

  private describeError(error: any, fallback: string): string {
    if (error?.status === 0) {
      return 'Backend is not reachable. Start API using dotnet run.';
    }

    if (error?.status === 404) {
      return 'API endpoint not found. Check backend routes.';
    }

    return error?.error?.error ?? error?.message ?? fallback;
  }

  private updateThemeChart(): void {
    const topThemes = this.themes.slice(0, 6);

    this.themeChartData = {
      ...this.themeChartData,
      labels: topThemes.map(theme => this.shortenLabel(theme.label || 'Theme')),
      datasets: [
        {
          ...this.themeChartData.datasets[0],
          data: topThemes.map(theme => Number((theme.impactScore ?? 0).toFixed(1)))
        },
        {
          ...this.themeChartData.datasets[1],
          data: topThemes.map(theme => Number((theme.relevanceScore ?? 0).toFixed(1)))
        }
      ]
    };
  }

  private shortenLabel(value: string): string {
    return value.length > 16 ? `${value.slice(0, 15)}...` : value;
  }
}
