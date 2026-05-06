import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { forkJoin } from 'rxjs';

import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

import {
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
    MatProgressSpinnerModule
  ],
  templateUrl: './reports-page.html',
  styleUrls: ['./reports-page.scss']
})
export class ReportsPageComponent implements OnInit {

  digest: WeeklyDigest | null = null;
  runs: ProcessingRun[] = [];

  loading = false;
  error: string | null = null;

  constructor(private feedbackService: FeedbackService) {}

  ngOnInit(): void {
    this.loadReports();
  }

  loadReports(): void {
    this.loading = true;
    this.error = null;

    forkJoin({
      digest: this.feedbackService.getWeeklyDigest(),
      runs: this.feedbackService.getProcessingRuns()
    }).subscribe({
      next: ({ digest, runs }) => {
        this.digest = digest;
        this.runs = runs ?? [];
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
}