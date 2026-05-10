import { Component } from '@angular/core';
import { BreakpointObserver, Breakpoints, BreakpointState } from '@angular/cdk/layout';
import { Observable } from 'rxjs';
import { FormsModule } from '@angular/forms';

import { MatSidenavModule } from '@angular/material/sidenav';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDividerModule } from '@angular/material/divider';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { Router, RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FeedbackService } from './services/feedback';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatSidenavModule,
    MatToolbarModule,
    MatIconModule,
    MatListModule,
    MatButtonModule,
    MatCardModule,
    MatDividerModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
    FormsModule
  ],
  templateUrl: './app.html',
  styleUrls: ['./app.scss']
})
export class AppComponent {

  // Reactive breakpoint stream (no manual subscription needed)
  isHandset$: Observable<BreakpointState>;
  assistantOpen = false;
  assistantInput = '';
  assistantLoading = false;
  assistantError: string | null = null;
  assistantMessages: { role: 'assistant' | 'user'; text: string }[] = [];

  constructor(
    private breakpointObserver: BreakpointObserver,
    private router: Router,
    private feedbackService: FeedbackService
  ) {
    this.isHandset$ = this.breakpointObserver.observe([Breakpoints.Handset]);
  }

  openPage(path: string, drawer: { mode: string; close: () => void }) {
    this.router.navigateByUrl(path);

    if (drawer.mode === 'over') {
      drawer.close();
    }
  }

  isActive(path: string): boolean {
    return this.router.isActive(path, {
      paths: 'exact',
      queryParams: 'ignored',
      fragment: 'ignored',
      matrixParams: 'ignored',
    });
  }

  toggleAssistant(): void {
    this.assistantOpen = !this.assistantOpen;
  }

  askQuick(prompt: string): void {
    this.assistantInput = prompt;
    this.askAssistant();
  }

  askAssistant(): void {
    const question = this.assistantInput.trim();
    if (!question || this.assistantLoading) {
      return;
    }

    this.assistantOpen = true;
    this.assistantError = null;
    this.assistantLoading = true;
    this.assistantMessages.push({ role: 'user', text: question });
    this.assistantInput = '';

    this.feedbackService.askSummarize(question).subscribe({
      next: (response) => {
        this.assistantMessages.push({
          role: 'assistant',
          text: response?.summary ?? 'No summary available.'
        });
        this.assistantLoading = false;
      },
      error: (error) => {
        this.assistantError =
          error?.status === 0
            ? 'Backend is not reachable. Start API using dotnet run.'
            : error?.error?.error ?? error?.message ?? 'Assistant request failed.';
        this.assistantLoading = false;
      }
    });
  }
}
