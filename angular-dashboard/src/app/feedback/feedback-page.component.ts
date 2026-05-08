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
import { fromEvent, timer } from 'rxjs';
import { FeedbackItem, FeedbackService } from '../services/feedback';

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
      next: () => {
        this.saving = false;
        this.form.reset({
          source: 'manual',
          text: '',
        });

        this.loadFeedback();
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

  private startAutoRefresh() {
    // Keep latest feedback list fresh while user stays on this page.
    timer(8000, 8000)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.refreshLatest());

    // Refresh when app window regains focus.
    fromEvent(window, 'focus')
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.refreshLatest());
  }
}
