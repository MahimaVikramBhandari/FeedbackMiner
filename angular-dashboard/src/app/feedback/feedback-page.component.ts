import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
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

  feedbackItems: FeedbackItem[] = [];
  loading = false;
  saving = false;
  error: string | null = null;

  form = this.fb.group({
    source: ['manual', Validators.required],
    text: ['', [Validators.required, Validators.minLength(8)]],
    rating: [null as number | null],
    productArea: ['Product', Validators.required],
    category: ['General', Validators.required],
    customerSegment: ['All customers', Validators.required],
  });

  constructor(private feedbackService: FeedbackService) {}

  ngOnInit() {
    this.loadFeedback();
  }

  loadFeedback() {
    this.loading = true;
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
      rating: v.rating ?? null,
      productArea: v.productArea ?? 'Product',
      category: v.category ?? 'General',
      customerSegment: v.customerSegment ?? 'All customers',
      metadata: {},
    };

    this.feedbackService.createFeedback(request).subscribe({
      next: () => {
        this.saving = false;
        this.form.reset({
          source: 'manual',
          text: '',
          rating: null,
          productArea: 'Product',
          category: 'General',
          customerSegment: 'All customers',
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
}