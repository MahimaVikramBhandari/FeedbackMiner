import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';

import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

import { FeedbackService, Theme } from '../services/feedback';

@Component({
  selector: 'app-themes-page',
  standalone: true,
  imports: [
    CommonModule,
    MatButtonModule,
    MatCardModule,
    MatIconModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './themes-page.html',
  styleUrls: ['./themes-page.scss']
})
export class ThemesPageComponent implements OnInit {

  themes: Theme[] = [];

  loading = false;
  running = false;

  error: string | null = null;
  pipelineMessage: string | null = null;

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
        this.loading = false;
      },
      error: (error) => {
        this.error = this.describeError(error);
        this.loading = false;
      }
    });
  }

  runPipeline(): void {
    this.running = true;
    this.error = null;
    this.pipelineMessage = null;

    this.feedbackService.runPipeline({
      runName: `Dashboard-${new Date().toISOString()}`,
      processAllFeedback: false,
      clusterSimilarityThreshold: 0.5
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
}
