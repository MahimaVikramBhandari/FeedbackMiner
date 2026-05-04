# Customer Feedback Theme Miner - Implementation Guide

A step-by-step guide to build the AI-powered feedback analysis system.

---

## **Phase 1: Data Foundation & Ingestion**

### Step 1: Expand Your Models
Enhance the data models to support all feedback sources.

**Update FeedbackItem Domain Model** (`Models/Domain/FeedbackItem.cs`):
- Add `Source` field: CSAT/NPS, support ticket, feature request, escalation
- Add `RawText` field: original feedback text
- Add `Sentiment` field: positive/negative/neutral
- Add `Urgency` field: high/medium/low
- Add `CreatedDate` field: when feedback was submitted
- Add `FeatureArea` field: product area reference

**Create Input DTOs** (`Models/DTOs/`):
- `CSATFeedbackRequest.cs` - For CSAT/NPS survey responses
- `SupportTicketRequest.cs` - For support team feedback
- `FeatureRequestRequest.cs` - For feature requests
- `EscalationRequest.cs` - For escalation categories

### Step 2: Build Input Pipeline
Extend the controller to handle multiple feedback sources.

**Update FeedbackController** (`Controllers/FeedbackController.cs`):
- Create endpoint: `POST /api/feedback/ingest` - batch import feedback from multiple sources
- Create endpoint: `POST /api/feedback/validate` - validate input structure before processing
- Add validation logic to ensure all required fields are present
- Return validation results with error details if validation fails

**Update FeedbackRepository** (`Data/Repositories/FeedbackRepository.cs`):
- Add method to bulk insert feedback items
- Add method to retrieve feedback by source type
- Add method to retrieve feedback within date range

---

## **Phase 2: Data Cleaning & PII Redaction**

### Step 3: Enhance Pipeline Processors
Improve text cleaning and PII redaction capabilities.

**Enhance PiiRedactor** (`Pipeline/Processors/PiiRedactor.cs`):
- Implement pattern detection for:
  - Emails (regex: `[\w\.-]+@[\w\.-]+\.\w+`)
  - Phone numbers (regex: `\b\d{3}[-.]?\d{3}[-.]?\d{4}\b`)
  - Social Security Numbers (regex: `\b\d{3}-\d{2}-\d{4}\b`)
  - Credit card numbers (regex: `\b\d{4}[\s-]?\d{4}[\s-]?\d{4}[\s-]?\d{4}\b`)
  - Names (optional: use NER library)
- Replace PII with placeholders: `[EMAIL]`, `[PHONE]`, `[SSN]`, `[CARD]`, `[NAME]`
- Maintain redaction audit log if compliance required

**Enhance TextCleaner** (`Pipeline/Processors/TextCleaner.cs`):
- Normalize whitespace (remove extra spaces, tabs, newlines)
- Remove URLs (regex: `https?://\S+`)
- Convert to lowercase for consistency
- Remove duplicate consecutive words
- Remove special characters (keep only alphanumeric and basic punctuation)
- Trim leading/trailing whitespace

### Step 4: Sentiment & Urgency Extraction
Add new processors for sentiment and urgency detection.

**Create SentimentAnalyzer** (`Pipeline/Processors/SentimentAnalyzer.cs`):
- Use OpenAI API to classify sentiment
- Input: cleaned feedback text
- Output: sentiment (positive/negative/neutral) + confidence score (0-1)
- Store results in FeedbackItem.Sentiment field
- Cache results to avoid re-processing

**Create UrgencyDetector** (`Pipeline/Processors/UrgencyDetector.cs`):
- Extract urgency keywords: "critical", "asap", "broke", "broken", "angry", "frustrated", "urgent", "emergency"
- Use regex or string matching with keyword weights
- Output: urgency level (high/medium/low) + keyword matches
- Store results in FeedbackItem.Urgency field

---

## **Phase 3: Embedding & Clustering**

### Step 5: Create Embedding Service
Build embeddings from cleaned feedback text.

**Create EmbeddingService** (`Services/AI/EmbeddingService.cs`):
- Call OpenAI Embeddings API (model: `text-embedding-3-small` or `text-embedding-ada-002`)
- Input: cleaned feedback text from pipeline
- Output: embedding vector (1536 dimensions)
- Store embedding vector in database:
  - Add `EmbeddingVector` column to FeedbackItem table (as float array)
  - Create database migration
- Cache embeddings to avoid re-processing identical text
- Batch API calls for efficiency (process in groups of 25-50)

**Database Updates**:
- Add migration to create `EmbeddingVector` column
- Add index on embedding column for similarity search
- Update FeedbackDbContext to include embedding property

### Step 6: Implement Clustering
Group similar feedback items into clusters.

**Create ClusteringService** (`Services/Business/ClusteringService.cs`):
- Algorithm choice: K-means or DBSCAN
  - K-means: simpler, requires predefined cluster count
  - DBSCAN: better for variable cluster sizes
- NuGet packages: `ML.NET` or `Accord.NET.MachineLearning`
- Input: stored embeddings from database
- Output: cluster assignments (ClusterId for each feedback item)

**Create Cluster Model** (`Models/Domain/Cluster.cs`):
- `ClusterId`: unique identifier
- `FeedbackItemIds`: list of feedback items in cluster
- `CentroidEmbedding`: centroid of all embeddings
- `ClusterSize`: number of items
- `CreatedDate`: when cluster was formed

**Ensure Stable Naming**:
- Use centroid embedding vectors as cluster fingerprints
- Same feedback = same cluster across multiple runs
- Sort cluster members by relevance score for consistency

**Create ClusterRepository** (`Data/Repositories/ClusterRepository.cs`):
- Save clusters to database
- Retrieve clusters by ID, date range
- Update cluster assignments

---

## **Phase 4: Theme Labeling & Action Classification**

### Step 7: Theme Labeling with GPT
Generate meaningful theme names for clusters.

**Create ThemeLabelingService** (`Services/AI/ThemeLabelingService.cs`):
- For each cluster:
  - Sample 3-5 most representative feedback items
  - Concatenate them for context
  - Call GPT-4 with system prompt:
    ```
    You are a product analyst. Analyze these customer feedback items and 
    identify the core theme or topic they address. Provide a concise theme name (2-3 words),
    a category (bug/feature_request/performance/usability/other), and a brief description.
    ```
- Parse GPT response to extract:
  - Theme name (e.g., "Login Issues")
  - Category (bug/feature_request/performance/usability/other)
  - Description (1-2 sentences explaining the theme)
- Store in database

**Create Theme Model** (`Models/Domain/Theme.cs`):
- `ThemeId`: unique identifier
- `ClusterId`: associated cluster
- `Label`: theme name
- `Category`: category type
- `Description`: detailed explanation
- `CreatedDate`: when theme was labeled

### Step 8: Function Calling for Action Items
Use OpenAI function calling to classify and route actions.

**Update OpenAIService** (`Services/AI/OpenAIService.cs`):
- Define function schemas for GPT:
  ```json
  {
    "name": "recommend_action",
    "parameters": {
      "type": "object",
      "properties": {
        "action_type": {"type": "string", "enum": ["bug_fix", "feature_add", "performance_improvement"]},
        "priority": {"type": "string", "enum": ["high", "medium", "low"]},
        "owner": {"type": "string"},
        "effort_level": {"type": "string", "enum": ["small", "medium", "large"]},
        "description": {"type": "string"}
      }
    }
  }
  ```
  
  ```json
  {
    "name": "escalate_to_product",
    "parameters": {
      "type": "object",
      "properties": {
        "feature_request_id": {"type": "string"},
        "impact_estimate": {"type": "number"}
      }
    }
  }
  ```
  
  ```json
  {
    "name": "route_to_support",
    "parameters": {
      "type": "object",
      "properties": {
        "support_category": {"type": "string"},
        "template_response": {"type": "string"}
      }
    }
  }
  ```

- Call GPT with cluster summary and available functions
- Parse JSON response to extract function calls
- Create action items based on function results

**Create Action Item Model** (`Models/Domain/ActionItem.cs`):
- `ActionId`: unique identifier
- `ThemeId`: associated theme
- `ActionType`: bug_fix/feature_add/performance_improvement
- `Priority`: high/medium/low
- `Owner`: team responsible
- `EffortLevel`: small/medium/large
- `Description`: what action to take
- `CreatedDate`: when action was created

---

## **Phase 5: Impact Measurement & Metrics**

### Step 9: Quantify Impact
Measure the business impact of each theme.

**Create ImpactService** (`Services/Business/ImpactService.cs`):
For each theme, calculate:
- **Frequency**: number of unique feedback items in cluster
- **Reach**: number of unique customers affected
- **Sentiment Distribution**: percentage of positive/negative/neutral feedback
- **Trend**: is frequency increasing or decreasing over time (compare week-over-week)
- **Priority Score**: 
  ```
  Priority = (Frequency × Reach × Urgency Weight) / Total Feedback
  ```

**Create Metrics Model** (`Models/Domain/ThemeMetrics.cs`):
- `MetricId`: unique identifier
- `ThemeId`: associated theme
- `Frequency`: count of items in theme
- `UniqueCustomers`: reach
- `PositiveSentiment%`: percentage positive
- `NegativeSentiment%`: percentage negative
- `NeutralSentiment%`: percentage neutral
- `PriorityScore`: calculated priority
- `Week-over-Week Change`: trend indicator
- `LastUpdated`: when metrics were calculated

### Step 10: Generate Recommendations
Create prioritized recommendation list.

**Create RecommendationService** (`Services/Business/RecommendationService.cs`):
- Rank themes by priority score (highest first)
- For each theme:
  - Link to related product features from product feature list
  - Generate 2-3 specific action items
  - Include success metrics (e.g., "reduce complaints by X%")
  - Estimate effort and timeline
  
Example output:
```
Theme: Login Flow Issues
Priority: High (234 customers, 45% increase this week)
Recommended Actions:
  1. Fix session timeout bug (Effort: Small, Owner: Backend)
  2. Add password reset link email (Effort: Medium, Owner: Frontend)
Success Metric: < 5% login failure rate
Timeline: 2-3 weeks
```

---

## **Phase 6: Deliverables & Reporting**

### Step 11: Theme Dashboard
Build web interface for insights.

**Create DashboardController** (`Controllers/DashboardController.cs`):
- `GET /api/dashboard/themes` - Return all themes with metrics
  ```json
  {
    "themes": [
      {
        "id": "theme_1",
        "label": "Login Issues",
        "frequency": 234,
        "sentiment": "negative",
        "priorityScore": 8.5,
        "actionItems": [...]
      }
    ]
  }
  ```

- `GET /api/dashboard/themes/{id}` - Return detailed view of single theme
  ```json
  {
    "id": "theme_1",
    "label": "Login Issues",
    "description": "Users experiencing timeouts and session loss",
    "metrics": {...},
    "sampleFeedback": [...],
    "recommendedActions": [...],
    "affectedCustomers": [...]
  }
  ```

- `GET /api/dashboard/trends` - Return theme frequency over time
  ```json
  {
    "trends": [
      {
        "week": "2026-05-01",
        "themes": [
          {"label": "Login Issues", "count": 234},
          {"label": "Feature Request: Dark Mode", "count": 89}
        ]
      }
    ]
  }
  ```

**Frontend Dashboard** (React/Vue/Blazor component):
- Display top themes as cards with priority color coding
- Show sentiment gauge (positive/negative/neutral)
- Display trend line (increasing/decreasing)
- Show affected customer count
- Link to action items
- Add filters (by source, category, date range, sentiment)

### Step 12: Weekly Feedback Digest
Automated email summary.

**Create DigestService** (`Services/Business/DigestService.cs`):
- Triggered by scheduled job (use Hangfire NuGet package)
- Configure to run every Monday at 9 AM
- Generate report containing:
  - Top 5 themes by priority
  - Sentiment summary (overall positive/negative ratio)
  - New action items week-over-week
  - Trends (which themes are growing/shrinking)
  - Key customer quotes (2-3 per theme)
  - Recommended focus areas for product team
- Format as: email, PDF, and dashboard view

**Email Template**:
```
Subject: Weekly Feedback Digest - Week of May 4, 2026

Top Themes This Week:
1. Login Flow Issues (234 items, High Priority)
2. Add Dark Mode Feature (89 items, Medium Priority)
...

Overall Sentiment: 65% Positive, 25% Negative, 10% Neutral

Action Items Generated: 12
  - 5 High Priority (Bug Fixes)
  - 4 Medium Priority (Features)
  - 3 Low Priority (Minor improvements)

Trend Alert: "Login Issues" up 45% week-over-week

[View Full Dashboard]
```

### Step 13: Cluster Export
Enable data export for analysis.

**Add Export Endpoint** (`Controllers/FeedbackController.cs`):
- `GET /api/feedback/clusters/export?format=json&dateFrom=2026-05-01&dateTo=2026-05-04`
- Supported formats: JSON, CSV, XLSX
- Export data structure:
  ```json
  {
    "exportDate": "2026-05-04",
    "totalClusters": 42,
    "clusters": [
      {
        "clusterId": "cluster_1",
        "themeLabel": "Login Issues",
        "memberCount": 234,
        "sentiment": "negative",
        "urgency": "high",
        "sampleFeedback": [
          "Text of feedback item 1",
          "Text of feedback item 2"
        ],
        "recommendedActions": [...]
      }
    ]
  }
  ```
- CSV format: cluster_id, theme, member_count, sentiment, urgency, sample_feedback, actions

---

## **Phase 7: Evaluation & Validation**

### Step 14: Set Up Evaluation Framework
Measure system quality against targets.

**Create Evaluation Notebook** (`evaluation.ipynb` or C# test harness):

**1. Theme Relevance Evaluation (Target: >= 4/5)**
- Methodology:
  - Select random sample of 50 themes
  - Have 2-3 domain experts (product managers, CS leads) rate each theme
  - Scale: 1-5 (1=irrelevant, 5=perfectly captures customer issues)
  - Calculate average score and standard deviation
- Success criteria: mean score >= 4.0, std dev < 0.8
- Create evaluation report with:
  - Per-theme scores
  - Low-scoring themes for refinement
  - Recommendations for improving theme labeling

**2. Clustering Precision Evaluation (Target: >= 0.8)**
- Methodology:
  - Identify known duplicate issues (from support tickets, customer complaints)
  - Run clustering on full feedback set
  - Check: are duplicates assigned to same cluster?
  - Calculate precision: `(correctly clustered duplicates) / (total duplicates)`
- Success criteria: precision >= 0.80
- Create evaluation report with:
  - Confusion matrix
  - False positive rate (incorrectly clustered items)
  - False negative rate (duplicates split into different clusters)
  - Recommendations for clustering parameter tuning

**3. Action Recommendation Usefulness (Target: >= 4/5)**
- Methodology:
  - Generate recommendations for sample of 30 themes
  - Have product/CS team rate usefulness of each recommendation
  - Scale: 1-5 (1=not useful, 5=very actionable)
  - Calculate average score
- Success criteria: mean score >= 4.0
- Create evaluation report with:
  - Per-recommendation scores
  - Common feedback on what makes recommendations useful
  - Recommendations for improving action generation

### Step 15: Stability Testing
Ensure consistent results across multiple runs.

**Create Stability Test** (`Tests/StabilityTests.cs` or notebook):
- Run full pipeline (embedding → clustering → theme labeling) 3 times on same data
- Verify consistency across runs:
  - Cluster assignments: >= 95% agreement across runs
  - Theme labels: >= 90% exact match or semantic equivalence
  - Action items: >= 85% same recommendations
  - Metrics: < 5% variance in priority scores

**Test Process**:
```
Run 1: Ingest feedback → Cluster → Label → Generate actions
Run 2: Repeat with same data
Run 3: Repeat with same data

Compare outputs:
- Same feedback items assigned to same clusters?
- Theme names consistent or semantically equivalent?
- Priority scores within 5% variance?
- Action items overlap >= 85%?
```

**Create Stability Report**:
- Consistency metrics per component
- Identify sources of variance (embedding randomness, GPT non-determinism)
- Recommendations for improving stability (fixed seeds, temperature control)

---

## **Implementation Timeline**

| Phase | Duration | Steps | Priority |
|-------|----------|-------|----------|
| Phase 1 | Week 1 | 1-2 | High - Foundation |
| Phase 2 | Week 1-2 | 3-4 | High - Data Quality |
| Phase 3 | Week 2 | 5-6 | High - Core ML |
| Phase 4 | Week 2-3 | 7-8 | High - AI Labeling |
| Phase 5 | Week 3 | 9-10 | Medium - Insights |
| Phase 6 | Week 3-4 | 11-13 | Medium - UI/Reports |
| Phase 7 | Week 4 | 14-15 | High - Validation |

---

## **Technology Stack Required**

**Already Have:**
- C# / ASP.NET Core
- Entity Framework Core (EF Core)
- OpenAI API
- SQL Database

**Need to Add:**
- **ML.NET** or **Accord.NET.MachineLearning** - for clustering algorithms
- **Hangfire** - for scheduled jobs (weekly digest)
- **CsvHelper** - for CSV export
- **EPPlus** or **ClosedXML** - for Excel export (optional)
- **React**, **Vue**, or **Blazor** - for dashboard frontend
- **Jupyter Notebooks** - for evaluation analysis (Python/.NET kernel)

---

## **Key Success Metrics**

- [x] Theme Relevance: >= 4.0/5.0
- [x] Clustering Precision: >= 0.80
- [x] Action Usefulness: >= 4.0/5.0
- [x] Stability: >= 95% cluster consistency across runs
- [x] PII Redaction: 100% detection of sensitive data
- [x] Dashboard Load Time: < 2 seconds
- [x] Weekly Digest Generation: < 5 minutes for 10,000 feedback items

---

## **Notes & Best Practices**

1. **API Rate Limiting**: Implement exponential backoff for OpenAI API calls
2. **Caching**: Cache embeddings and clustering results to avoid re-processing
3. **Error Handling**: Implement comprehensive logging for debugging
4. **Data Privacy**: Ensure PII redaction is airtight before storing or exporting
5. **Testing**: Write unit tests for each processor and service
6. **Documentation**: Document all function schemas and API contracts
7. **Monitoring**: Add metrics collection for API latency and error rates
