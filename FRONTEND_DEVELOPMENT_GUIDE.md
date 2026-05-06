# FeedbackMiner Angular Dashboard - Frontend Development Guide

**Last Updated**: May 5, 2026  
**Current Status**: Early Stage - Basic Infrastructure Setup Complete  
**Angular Version**: 21.2.0  
**Node.js**: 18+  
**npm**: 10.9.2+

---

## 📋 Table of Contents

1. [Project Overview](#project-overview)
2. [Current Implementation Status](#current-implementation-status)
3. [Planned Development](#planned-development)
4. [Quick Start - Running the UI](#quick-start---running-the-ui)
5. [Frontend Folder Structure](#frontend-folder-structure)
6. [Backend-Frontend Integration](#backend-frontend-integration)
7. [Development Workflow](#development-workflow)
8. [Testing & Validation](#testing--validation)

---

## Project Overview

### Purpose
The Angular Dashboard is a modern, responsive web interface for the FeedbackMiner backend. It allows users to:
- View consolidated customer feedback
- Analyze feedback themes
- Monitor sentiment and urgency metrics
- Generate reports
- Manage feedback sources
- View actionable recommendations

### Technology Stack
| Layer | Technology | Version |
|-------|-----------|---------|
| Framework | Angular | 21.2.0 |
| Language | TypeScript | 5.9.2 |
| UI Components | Angular Material | 21.2.9 |
| Charting | Chart.js + ng2-charts | 4.5.1 |
| HTTP | Angular HttpClient | 21.2.0 |
| Server | Express | 5.1.0 |
| Rendering | Angular SSR | 21.2.9 |
| Build | Angular CLI | 21.2.9 |

---

## Current Implementation Status

### ✅ Completed

#### 1. **Project Setup & Infrastructure**
- Angular 21 project created with SSR/SSG support
- Node modules installed with all dependencies
- Build and development configurations established
- TypeScript configuration for modern syntax support

#### 2. **Application Structure**
- **Root Module**: `app.ts` - Application bootstrapping
- **Configuration**: `app.config.ts` - Providers setup for Material, HTTP, Router, Animations
- **Routing**: `app.routes.ts` - Basic route definitions
- **Main Template**: `app.html` - Material sidenav layout with toolbar
- **Styling**: `app.scss` - Global Material theme imports

#### 3. **Layout & Navigation**
```
Material Sidenav Layout
├── Sidebar Navigation (250px fixed)
│   ├── Dashboard (icon: dashboard)
│   ├── Feedback (icon: feedback)
│   ├── Themes (icon: category)
│   └── Reports (icon: analytics)
├── Main Content Area
│   ├── Toolbar (Primary Color)
│   │   └── "FeedbackMiner Dashboard" Title
│   └── Main Content Region
│       └── Router Outlet (dynamic content)
```

#### 4. **Core Services**
- **FeedbackService** (`src/app/services/feedback.ts`)
  - Location: `d:\Desktop\Project-feedbackminer\FeedbackMiner\angular-dashboard\src\app\services\feedback.ts`
  - Interfaces: `FeedbackItem`, `CreateFeedbackRequest`, `Theme`
  - Methods:
    - `createFeedback(request)` - POST feedback
    - `getFeedback()` - GET all feedback
    - `triggerAnalysis()` - POST analysis trigger
    - `getThemes()` - GET themes
    - `getReports()` - GET reports
  - API Base URL: `http://localhost:5283/api`

#### 5. **Components Generated**
- **Dashboard Component** (`src/app/dashboard/`)
  - Currently: Empty shell with Material layout imports
  - Files:
    - `dashboard.ts` - Component definition
    - `dashboard.html` - Template (to be populated)
    - `dashboard.scss` - Styles

#### 6. **Dependencies Installed**
```json
{
  "@angular/material": "^21.2.9",
  "@angular/cdk": "^21.2.9",
  "chart.js": "^4.5.1",
  "ng2-charts": "^10.0.0",
  "@angular/platform-server": "^21.2.0",
  "@angular/ssr": "^21.2.9"
}
```

---

## Planned Development

### Phase 1: Core Dashboard (Next Priority)
- [ ] Dashboard component - implement feedback metrics display
  - Total feedback count
  - Sentiment distribution chart (pie/bar)
  - Urgency level breakdown
  - Recent feedback list
  - Top themes summary
- [ ] Integrate FeedbackService into Dashboard
- [ ] Add Material cards for data containers
- [ ] Implement Chart.js visualizations using ng2-charts

### Phase 2: Feature Components
- [ ] **Feedback Component** (`src/app/feedback/`)
  - Display feedback list/table with Material Table
  - Filter, sort, and pagination
  - Create new feedback form
  - View feedback details modal
  - Bulk operations

- [ ] **Themes Component** (`src/app/themes/`)
  - Display identified themes
  - Theme statistics (relevance, impact scores)
  - Affected product areas and segments
  - Action recommendations per theme

- [ ] **Reports Component** (`src/app/reports/`)
  - Report generation interface
  - Report history and download
  - Custom report builder
  - Export functionality (PDF/CSV)

- [ ] **Analysis Component** (`src/app/analysis/`)
  - Trigger analysis runs
  - Monitor processing status
  - View analysis results
  - Quality metrics display

### Phase 3: Advanced Features
- [ ] Data visualization enhancements
  - Trend charts (sentiment over time)
  - Heatmaps for theme-segment relationships
  - Embedding visualization (t-SNE/UMAP if needed)
- [ ] Real-time updates (WebSocket integration if applicable)
- [ ] Advanced filtering and search
- [ ] User preferences and settings
- [ ] Error handling and loading states

### Phase 4: Polish & Optimization
- [ ] Responsive design testing
- [ ] Performance optimization
- [ ] Unit and integration tests
- [ ] E2E testing with Cypress
- [ ] Accessibility compliance (WCAG 2.1)
- [ ] SSR/SSG optimization for production

---

## Quick Start - Running the UI

### Prerequisites
Ensure you have:
- Node.js 18+ installed
- npm 10.9.2+ installed
- Backend running on `http://localhost:5283`

### Step 1: Start the Backend
```bash
# From FeedbackMiner project root
cd d:\Desktop\Project-feedbackminer\FeedbackMiner
dotnet run
```

**Expected Output**:
```
Building...
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5283
```

**Verify Backend**:
- Open browser: `http://localhost:5283/swagger`
- Should see Swagger API documentation

### Step 2: Start the Frontend Development Server
```bash
# From angular-dashboard root
cd d:\Desktop\Project-feedbackminer\FeedbackMiner\angular-dashboard

# Install dependencies (if not already done)
npm install

# Start development server
npm start
```

**Expected Output**:
```
✔ Application bundle generated successfully. 47.32 seconds.

Initial Chunk Files | Names         | Size
main.ts            | main          | 385.50 kB

Build at: 2026-05-05T10:30:00.000Z - Hash: abc123def456
Compiled successfully!
```

### Step 3: Access the Dashboard
Open your browser and navigate to:
```
http://localhost:4200
```

You should see:
- **Left Sidebar**: Navigation menu with Dashboard, Feedback, Themes, Reports links
- **Top Toolbar**: "FeedbackMiner Dashboard" title with primary color background
- **Main Content Area**: Currently empty router outlet (will show Dashboard component)

### Troubleshooting

**Issue**: Cannot reach `http://localhost:4200`
- **Solution**: Verify `npm start` is running. Check terminal for compilation errors.

**Issue**: "Cannot GET /api/feedback" errors in console
- **Solution**: Ensure backend is running on port 5283. Check firewall settings.

**Issue**: Material icons not showing
- **Solution**: Material icons should load automatically. Clear browser cache and reload.

**Issue**: CORS errors
- **Solution**: Backend should have CORS configured. Verify in `Program.cs`.

---

## Frontend Folder Structure

```
angular-dashboard/
├── node_modules/              # Dependencies (git-ignored)
├── src/                        # Source code
│   ├── app/                    # Angular application
│   │   ├── dashboard/          # Dashboard component
│   │   │   ├── dashboard.ts    # Component class
│   │   │   ├── dashboard.html  # Component template
│   │   │   └── dashboard.scss  # Component styles
│   │   │
│   │   ├── services/           # HTTP services
│   │   │   └── feedback.ts     # API service for feedback operations
│   │   │
│   │   ├── app.ts             # Root component
│   │   ├── app.html           # Root template (sidenav layout)
│   │   ├── app.scss           # Root styles
│   │   ├── app.routes.ts      # Route definitions
│   │   ├── app.config.ts      # Application configuration & providers
│   │   ├── app.config.server.ts # SSR configuration
│   │   └── app.routes.server.ts # SSR routes
│   │
│   ├── main.ts                 # Application entry point
│   ├── main.server.ts          # SSR entry point
│   └── styles.scss             # Global styles
│
├── public/                      # Static assets
│   ├── favicon.ico
│   └── ...
│
├── dist/                        # Build output (generated)
├── angular.json                 # Angular CLI configuration
├── tsconfig.json               # TypeScript configuration
├── package.json                # Dependencies & scripts
├── package-lock.json           # Dependency lock file
└── README.md                   # Project README
```

### File Purposes

| File | Purpose |
|------|---------|
| `src/app/app.ts` | Root component - bootstraps the entire app |
| `src/app/app.html` | Contains sidenav layout, toolbar, navigation menu |
| `src/app/app.config.ts` | Configures providers: HttpClient, Router, Material, SSR |
| `src/app/app.routes.ts` | Defines navigation routes between pages |
| `src/app/services/feedback.ts` | Handles all API calls to backend |
| `src/main.ts` | Entry point for browser |
| `src/main.server.ts` | Entry point for server-side rendering |
| `angular.json` | CLI configuration (ports, build options) |
| `package.json` | Dependencies and build scripts |

---

## Backend-Frontend Integration

### Integration Architecture

```
┌──────────────────────────────────────────────────────────┐
│                    Frontend (Angular)                     │
│              http://localhost:4200                         │
├──────────────────────────────────────────────────────────┤
│  • Components (Dashboard, Feedback, Themes, Reports)      │
│  • Services (FeedbackService with HttpClient)             │
│  • Material UI Components                                  │
│  • Chart.js Visualizations                                │
└─────────────────────────────┬──────────────────────────────┘
                              │
                    HTTP REST API Calls
                    (JSON over HTTP)
                              │
┌─────────────────────────────┴──────────────────────────────┐
│                    Backend (.NET 10)                       │
│              http://localhost:5283                         │
├──────────────────────────────────────────────────────────┤
│  • Controllers (FeedbackController, etc.)                  │
│  • Services (Analysis, Clustering, Theme Labeling)        │
│  • Database (SQL Server with EF Core)                      │
│  • OpenAI Integration (Embeddings, LLMs)                   │
└──────────────────────────────────────────────────────────┘
```

### API Contract

#### Base URL
```
http://localhost:5283/api
```

#### Available Endpoints (from `FeedbackService`)

**Feedback Endpoints**
```
POST   /api/feedback                    # Create feedback
GET    /api/feedback                    # Get all feedback
GET    /api/feedback/{id}               # Get feedback by ID (expandable)
```

**Analysis Endpoints**
```
POST   /api/analysis                    # Trigger analysis run
GET    /api/analysis/{id}               # Get analysis results
```

**Theme Endpoints**
```
GET    /api/themes                      # Get all themes
GET    /api/themes/{id}                 # Get theme details
GET    /api/themes/{id}/feedback        # Get feedback for theme
```

**Report Endpoints**
```
GET    /api/reports                     # Get reports
POST   /api/reports/generate            # Generate new report
GET    /api/reports/{id}/download       # Download report
```

**Additional Endpoints** (available on backend, can be integrated)
```
GET    /api/analysis/metrics            # Quality metrics
GET    /api/evaluation/metrics           # Evaluation results
GET    /api/dataSources                 # Data sources
POST   /api/dataSources/import          # Import from CSV/JSON
```

### Service Implementation

**Location**: `src/app/services/feedback.ts`

```typescript
@Injectable({ providedIn: 'root' })
export class FeedbackService {
  private apiUrl = 'http://localhost:5283/api';
  
  constructor(private http: HttpClient) {}
  
  // Methods available to components
  createFeedback(request): Observable<FeedbackItem>
  getFeedback(): Observable<FeedbackItem[]>
  triggerAnalysis(): Observable<any>
  getThemes(): Observable<Theme[]>
  getReports(): Observable<any>
}
```

### Data Models

**FeedbackItem** (from backend)
```typescript
interface FeedbackItem {
  id: string;                    // Unique identifier
  source: string;                // Source system (e.g., "email", "survey")
  text: string;                  // Original feedback text
  processedText?: string;        // Cleaned/processed text
  rating?: number;               // Optional rating (1-5)
  productArea: string;           // Which product area (e.g., "UI", "API")
  category: string;              // Feedback category
  customerSegment: string;       // Customer segment (e.g., "enterprise")
  createdAt: string;             // ISO timestamp
  language?: string;             // Detected language
  sentimentScore?: number;       // -1 to 1 sentiment score
  sentimentLabel?: string;       // "positive", "negative", "neutral"
  urgencyScore?: number;         // 0 to 1 urgency score
  urgencyLevel?: string;         // "low", "medium", "high", "critical"
  themeId?: string;              // Assigned theme
  similarityScore?: number;      // Similarity to cluster
}
```

**Theme** (from backend)
```typescript
interface Theme {
  id: string;
  label: string;                 // Theme title (e.g., "Payment Issues")
  description: string;           // Theme description
  relevanceScore: number;        // 0-5 relevance score
  feedbackCount: number;         // Items in this theme
  averageSentimentScore: number; // Average sentiment
  averageUrgencyScore: number;   // Average urgency
  impactScore: number;           // Business impact
  affectedProductAreasJson: string;     // Affected areas
  affectedSegmentsJson: string;         // Affected segments
  createdAt: string;
  updatedAt: string;
}
```

### HTTP Configuration

**Location**: `src/app/app.config.ts`

```typescript
provideHttpClient(withInterceptorsFromDi())
```

This enables:
- Automatic JSON serialization/deserialization
- Support for HTTP interceptors (for auth, error handling, etc.)
- Dependency injection of HttpClient

### CORS Handling

The backend should have CORS configured in `Program.cs`:

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularUI", builder =>
    {
        builder.WithOrigins("http://localhost:4200")
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});
```

---

## Development Workflow

### Creating a New Component

**Example**: Adding a Feedback List Component

1. **Generate component**
```bash
cd angular-dashboard
ng generate component feedback --skip-tests
```

2. **Inject service**
```typescript
// feedback/feedback.ts
import { Component, OnInit } from '@angular/core';
import { FeedbackService, FeedbackItem } from '../services/feedback';

@Component({
  selector: 'app-feedback',
  templateUrl: './feedback.html',
  styleUrl: './feedback.scss',
})
export class FeedbackComponent implements OnInit {
  feedbackItems: FeedbackItem[] = [];
  loading = true;

  constructor(private feedbackService: FeedbackService) {}

  ngOnInit() {
    this.loadFeedback();
  }

  loadFeedback() {
    this.feedbackService.getFeedback().subscribe(
      (data) => {
        this.feedbackItems = data;
        this.loading = false;
      },
      (error) => {
        console.error('Error loading feedback:', error);
        this.loading = false;
      }
    );
  }
}
```

3. **Add to routing**
```typescript
// app.routes.ts
export const routes: Routes = [
  { path: '', component: DashboardComponent },
  { path: 'feedback', component: FeedbackComponent },
  { path: '**', redirectTo: '' }
];
```

4. **Create template**
```html
<!-- feedback/feedback.html -->
<div class="feedback-container">
  <h1>Customer Feedback</h1>
  
  @if (loading) {
    <p>Loading feedback...</p>
  } @else {
    @for (item of feedbackItems; track item.id) {
      <mat-card class="feedback-item">
        <mat-card-content>
          <strong>{{ item.text }}</strong>
          <p>Sentiment: {{ item.sentimentLabel }}</p>
          <p>Urgency: {{ item.urgencyLevel }}</p>
        </mat-card-content>
      </mat-card>
    }
  }
</div>
```

### Building for Production

```bash
# Build optimized production bundle
npm run build

# Output located in: dist/angular-dashboard/
```

### Building with SSR

```bash
# Build with Server-Side Rendering
ng build --configuration=production

# Serve with SSR
npm run serve:ssr:angular-dashboard
```

---

## Testing & Validation

### Manual Testing Checklist

- [ ] **Sidebar Navigation**
  - Click each nav link (Dashboard, Feedback, Themes, Reports)
  - Verify page loads without errors
  - Check URL updates

- [ ] **Material Components**
  - Toolbar displays correctly
  - Icons render properly
  - Colors are consistent

- [ ] **API Integration** (when Dashboard component is implemented)
  - Open browser DevTools → Network tab
  - Click "Dashboard" link
  - Verify API calls to `http://localhost:5283/api/`
  - Check response status (should be 200)
  - Verify data displays in component

- [ ] **Responsive Design**
  - Test on desktop (1920px)
  - Test on tablet (768px)
  - Test on mobile (375px)
  - Sidenav should collapse on small screens

- [ ] **Error Handling**
  - Stop backend server
  - Try loading data
  - Should show error message (to be implemented)
  - Restart backend
  - Verify data loads again

### Browser DevTools Console Checks

1. **No Red Errors**
   ```
   Console should be clean (no red error messages)
   ```

2. **Network Requests**
   - Status codes should be 200, 304
   - No 404, 500, or CORS errors

3. **Performance**
   - Initial page load: < 2 seconds
   - API responses: < 500ms

### Running Unit Tests (When Available)

```bash
# Run tests
npm test

# Run tests with coverage
ng test --code-coverage
```

---

## Next Steps

### Immediate (This Session)
1. Fix the `app.html` template to ensure clean sidenav rendering
2. Implement Dashboard component with:
   - Material cards
   - Chart.js pie/bar charts
   - Key metrics display
3. Test integration between Dashboard and FeedbackService

### Short-term (Next 1-2 days)
1. Create Feedback component with Material Table
2. Create Themes component with theme list
3. Add error handling and loading states
4. Implement responsive design

### Medium-term (Next week)
1. Create Reports component
2. Add filtering and search
3. Implement Create Feedback form
4. Add unit tests

### Long-term (Ongoing)
1. Performance optimization
2. Accessibility improvements
3. Advanced features (real-time updates, etc.)
4. Production deployment setup

---

## Useful Commands

```bash
# Navigate to frontend project
cd angular-dashboard

# Start development server
npm start

# Build for production
npm run build

# Generate component
ng generate component <name> --skip-tests

# Generate service
ng generate service <name> --skip-tests

# Run tests
npm test

# Check code style
npx prettier --check .

# Format code
npx prettier --write .
```

---

## Resources

- [Angular Documentation](https://angular.dev)
- [Angular Material Docs](https://material.angular.io)
- [Chart.js Documentation](https://www.chartjs.org)
- [ng2-charts Documentation](https://valor-software.com/ng2-charts)
- [TypeScript Handbook](https://www.typescriptlang.org/docs)
- [RxJS Documentation](https://rxjs.dev)

---

## Support & Troubleshooting

### Common Issues

**Port 4200 already in use**
```bash
# Use different port
ng serve --port 4300
```

**Dependencies conflict**
```bash
# Clear and reinstall
rm -r node_modules package-lock.json
npm install
```

**Backend not responding**
```bash
# Verify backend is running
# Check http://localhost:5283/swagger
# Verify API calls in browser DevTools → Network
```

**Material styles not loading**
```bash
# In app.config.ts, ensure:
provideAnimationsAsync()

# In styles.scss, ensure Material theme is imported:
@import '@angular/material/prebuilt-themes/indigo-pink.css';
```

---

**Document Version**: 1.0  
**Last Updated**: May 5, 2026  
**Status**: Active Development
