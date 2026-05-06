import { Component, OnInit, DestroyRef, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { forkJoin } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

import { FeedbackService, FeedbackItem, Theme } from '../services/feedback';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    MatButtonModule,
    MatCardModule,
    MatIconModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './dashboard.html',
  styleUrls: ['./dashboard.scss']
})
export class DashboardComponent implements OnInit {

  private destroyRef = inject(DestroyRef);

  feedbackItems: FeedbackItem[] = [];
  themes: Theme[] = [];

  loading = true;
  error: string | null = null;

  totalFeedback = 0;
  positiveFeedback = 0;
  negativeFeedback = 0;
  neutralFeedback = 0;
  averageSentiment = 0;
  highUrgencyCount = 0;

  constructor(private feedbackService: FeedbackService) {}

  ngOnInit() {
    this.loadDashboardData();
  }

  loadDashboardData() {
    this.loading = true;
    this.error = null;

    forkJoin({
      feedback: this.feedbackService.getFeedback(100),
      themes: this.feedbackService.getThemes(50),
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: ({ feedback, themes }) => {
          this.feedbackItems = feedback ?? [];
          this.themes = themes ?? [];

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

    this.positiveFeedback = items.filter(
      f => this.normalize(f.sentimentLabel) === 'positive'
    ).length;

    this.negativeFeedback = items.filter(
      f => this.normalize(f.sentimentLabel) === 'negative'
    ).length;

    this.neutralFeedback = items.filter(
      f => this.normalize(f.sentimentLabel) === 'neutral'
    ).length;

    this.highUrgencyCount = items.filter(
      f => ['high', 'critical'].includes(this.normalize(f.urgencyLevel))
    ).length;

    const scoredItems = items.filter(
      f => typeof f.sentimentScore === 'number'
    );

    this.averageSentiment = scoredItems.length
      ? scoredItems.reduce((sum, f) => sum + (f.sentimentScore ?? 0), 0) / scoredItems.length
      : 0;
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

  private normalize(value: string | undefined): string {
    return value?.toLowerCase() ?? '';
  }
}