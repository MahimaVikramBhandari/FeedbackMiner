import { Component, OnInit, DestroyRef, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

import { FeedbackService, FeedbackItem, ProcessingRun, Theme, WeeklyDigest } from '../services/feedback';

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
    MatProgressSpinnerModule
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
  assistantMessages: { role: 'assistant' | 'user'; text: string }[] = [
    {
      role: 'assistant',
      text: 'Ask me about themes, clustering, evaluation thresholds, digest/reports, or which page to use.'
    }
  ];

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
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: ({ feedback, themes, digest, runs }) => {
          this.feedbackItems = feedback ?? [];
          this.themes = themes ?? [];
          this.digest = digest;
          this.latestRun = runs?.[0] ?? null;

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

  askAssistant() {
    const question = this.assistantInput.trim();
    if (!question || this.assistantLoading) {
      return;
    }

    this.assistantError = null;
    this.assistantLoading = true;
    this.assistantMessages.push({ role: 'user', text: question });
    this.assistantInput = '';

    this.feedbackService.askDashboardAssistant(question)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          this.assistantMessages.push({
            role: 'assistant',
            text: response?.answer ?? 'I could not generate an answer right now.'
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

  onAssistantEnter(event: Event) {
    const keyEvent = event as KeyboardEvent;
    if (!keyEvent.shiftKey) {
      event.preventDefault();
      this.askAssistant();
    }
  }

  private normalize(value: string | undefined): string {
    return value?.toLowerCase() ?? '';
  }
}
