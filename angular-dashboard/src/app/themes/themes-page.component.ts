import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ChartConfiguration, ChartOptions } from 'chart.js';
import { BaseChartDirective } from 'ng2-charts';

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
export class ThemesPageComponent implements OnInit {

  themes: Theme[] = [];

  loading = false;
  running = false;
  processAllFeedback = true;

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

  constructor(private feedbackService: FeedbackService) {}

  ngOnInit(): void {
    this.loadThemes();
  }

  loadThemes(): void {
    this.loading = true;
    this.error = null;

    this.feedbackService.getThemeDashboard(100).subscribe({
      next: (themes) => {
        this.themes = themes ?? [];
        this.updateThemeChart();
        this.loading = false;
      },
      error: (error) => {
        this.error = this.describeError(error);
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
        this.summaryError = this.describeError(error);
        this.summaryLoading = false;
      }
    });
  }

  hasThemeChartData(): boolean {
    return this.themes.some(theme => (theme.impactScore ?? 0) > 0 || (theme.relevanceScore ?? 0) > 0);
  }

  runPipeline(): void {
    this.running = true;
    this.error = null;
    this.pipelineMessage = null;

    this.feedbackService.runPipeline({
      runName: `Dashboard-${new Date().toISOString()}`,
      processAllFeedback: this.processAllFeedback,
    }).subscribe({
      next: (run) => {
        this.pipelineMessage =
          `Analysis completed: ${run.feedbackProcessed ?? 0} feedback, ` +
          `${run.clustersCreated ?? 0} clusters, ${run.themesExtracted ?? 0} themes.`;
        this.running = false;
        this.loadThemes();
      },
      error: (error) => {
        this.error = this.describeError(error);
        this.running = false;
      }
    });
  }

  private describeError(error: any): string {
    if (error?.status === 0) {
      return 'Backend is not reachable. Start API using dotnet run.';
    }

    if (error?.status === 404) {
      return 'API endpoint not found. Check backend routes.';
    }

    return error?.error?.error ?? error?.message ?? 'Theme request failed.';
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
