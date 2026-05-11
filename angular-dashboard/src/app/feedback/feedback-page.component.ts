import { CommonModule } from '@angular/common';
import { Component, DestroyRef, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatTooltipModule } from '@angular/material/tooltip';
import { timer } from 'rxjs';
import { FeedbackItem, FeedbackService } from '../services/feedback';

type FeedbackSortMode = 'newest' | 'oldest' | 'positive' | 'negative';
type SentimentFilter = 'all' | 'positive' | 'neutral' | 'negative' | 'unscanned';

@Component({
  selector: 'app-feedback-page',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatTooltipModule,
  ],
  templateUrl: './feedback-page.html',
  styleUrls: ['./feedback-page.scss'],
})
export class FeedbackPageComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);

  feedbackItems: FeedbackItem[] = [];
  loading = false;
  saving = false;
  error: string | null = null;
  sortMode: FeedbackSortMode = 'newest';
  sentimentFilter: SentimentFilter = 'all';

  form = this.fb.group({
    source: ['manual', Validators.required],
    text: ['', [Validators.required, Validators.minLength(8)]],
  });

  constructor(private feedbackService: FeedbackService) {}

  ngOnInit() {
    this.loadFeedback();
    this.startAutoRefresh();
  }

  loadFeedback(showLoading = true) {
    if (showLoading) {
      this.loading = true;
    }

    this.error = null;

    this.feedbackService.getFeedback(100).subscribe({
      next: (items) => {
        this.feedbackItems = items ?? [];
        this.loading = false;
      },
      error: (error) => {
        this.error = this.describeError(error);
        this.loading = false;
      },
    });
  }

  refreshLatest() {
    this.loadFeedback(false);
  }

  get displayedFeedbackItems(): FeedbackItem[] {
    const filtered = this.feedbackItems.filter(item => {
      if (this.sentimentFilter === 'all') {
        return true;
      }

      const sentiment = this.normalize(item.sentimentLabel);

      if (this.sentimentFilter === 'unscanned') {
        return !sentiment;
      }

      return sentiment === this.sentimentFilter;
    });

    return [...filtered].sort((a, b) => this.compareFeedback(a, b));
  }

  get visibleFeedbackCount(): number {
    return this.displayedFeedbackItems.length;
  }

  submit() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving = true;
    this.error = null;

    const v = this.form.getRawValue();

    const request = {
      source: v.source ?? 'manual',
      text: v.text ?? '',
    };

    this.feedbackService.createFeedback(request).subscribe({
      next: (created) => {
        this.saving = false;

        if (created) {
          this.feedbackItems = [created, ...this.feedbackItems];
        }

        this.form.reset({
          source: 'manual',
          text: '',
        });
        this.form.markAsPristine();
        this.form.markAsUntouched();

        this.loadFeedback(false);
      },
      error: (error) => {
        this.error = this.describeError(error);
        this.saving = false;
      },
    });
  }

  private describeError(error: any): string {
    return error.status === 0
      ? 'Backend is not reachable. Start API using dotnet run.'
      : error.error?.error ?? error.message ?? 'Feedback request failed.';
  }

  private compareFeedback(a: FeedbackItem, b: FeedbackItem): number {
    switch (this.sortMode) {
      case 'oldest':
        return this.getCreatedTime(a) - this.getCreatedTime(b);
      case 'positive':
        return this.getSentimentRank(b, 'positive') - this.getSentimentRank(a, 'positive')
          || this.getCreatedTime(b) - this.getCreatedTime(a);
      case 'negative':
        return this.getSentimentRank(b, 'negative') - this.getSentimentRank(a, 'negative')
          || this.getCreatedTime(b) - this.getCreatedTime(a);
      case 'newest':
      default:
        return this.getCreatedTime(b) - this.getCreatedTime(a);
    }
  }

  private getCreatedTime(item: FeedbackItem): number {
    return item.createdAt ? new Date(item.createdAt).getTime() : 0;
  }

  private getSentimentRank(item: FeedbackItem, sentiment: 'positive' | 'negative'): number {
    return this.normalize(item.sentimentLabel) === sentiment ? 1 : 0;
  }

  private normalize(value: string | undefined): string {
    return value?.toLowerCase() ?? '';
  }

  private startAutoRefresh() {
    // Keep latest feedback list fresh while user stays on this page.
    timer(8000, 8000)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.refreshLatest());
  }
}
