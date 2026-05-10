# FeedbackMiner - Customer Feedback Theme Miner and Action Recommender

## Overview

FeedbackMiner is a sophisticated .NET 10 application that analyzes customer feedback, identifies recurring themes, and recommends actionable improvements. It combines advanced embedding-based clustering, GPT-powered theme labeling, and intelligent action recommendations with comprehensive evaluation metrics.

### Key Features

- **Feedback Ingestion**: Supports multiple data sources (CSV, JSON)
- **Embedding-based Clustering**: Uses OpenAI embeddings for semantic similarity analysis (target precision >= 0.8)
- **Theme Extraction**: Automatic theme labeling and description using GPT (target relevance >= 4.0/5.0)
- **Sentiment & Urgency Analysis**: Extracts sentiment scores and urgency levels
- **Action Recommendations**: Generates concrete, prioritized recommendations using function calling (target usefulness >= 4.0/5.0)
- **Quality Metrics**: Tracks system performance against quality thresholds
- **Evaluation Notebooks**: Generates comprehensive evaluation reports (JSON/HTML)
- **Weekly Digest**: Automatic scheduled digest generation with key insights
- **Complete REST API**: Full backend implementation ready for Angular dashboard

---

## Quick Start

### Prerequisites
- .NET 10 SDK
- SQL Server (or configure other database)
- OpenAI API key

### Setup

1. **Clone the repository**
```bash
git clone https://github.com/MahimaVikramBhandari/FeedbackMiner.git
cd FeedbackMiner
```

2. **Configure environment**
```bash
# Set OpenAI API key in .env file
$env:OPENAI_API_KEY="your-api-key-here"

# Update appsettings.json with database connection
```

3. **Build and run**
```bash
dotnet build
dotnet run
```

The API will be available at `https://localhost:5001`

---

## Quality Metrics & Targets

The system tracks three primary quality metrics:

### 1. Theme Relevance Score (Target: >= 4.0/5.0)
- Measures how well extracted themes match feedback content
- Evaluated using GPT analysis
- **Formula**: Average of all theme relevance scores (1-5 scale)

### 2. Clustering Precision (Target: >= 0.8)
- Measures quality of feedback clustering
- Based on silhouette score analysis
- **Formula**: (silhouette_score + 1) / 2
- **Target**: >= 0.8

### 3. Action Recommendation Usefulness (Target: >= 4.0/5.0)
- Evaluates usefulness and actionability of recommendations
- Considers feasibility, impact, and alignment
- **Formula**: Average of all recommendation usefulness scores (1-5 scale)

---

## Core API Endpoints

### Analysis Pipeline
```
POST /api/analysis/run-pipeline
Execute full feedback analysis pipeline

Request:
{
  "processAllFeedback": true,
  "runName": "Weekly Analysis"
}

Response:
{
  "success": true,
  "data": {
    "runId": "guid",
    "feedbackProcessed": 150,
    "clustersCreated": 12,
    "themesExtracted": 12,
    "status": "Completed"
  }
}
```

### Evaluation Metrics
```
POST /api/evaluation/evaluate/{processingRunId}
Evaluate a processing run against quality metrics

Response:
{
  "success": true,
  "data": {
    "averageThemeRelevance": {
      "score": 4.2,
      "target": 4.0,
      "metThreshold": true
    },
    "clusteringPrecision": {
      "score": 0.82,
      "target": 0.8,
      "metThreshold": true
    },
    "recommendationUsefulness": {
      "score": 4.1,
      "target": 4.0,
      "metThreshold": true
    },
    "overallQualityScore": 85.5,
    "status": "APPROVED"
  }
}
```

### Data Import
```
POST /api/data-sources/import
Import feedback from CSV or JSON

Request:
{
  "sourceType": "CSV",
  "credentials": {
    "FilePath": "C:\\feedback.csv"
  }
}
```

### Evaluation Notebooks
```
GET /api/notebooks/export/html/{processingRunId}
Export evaluation report as interactive HTML

GET /api/notebooks/export/json/{processingRunId}
Export evaluation report as JSON
```

### Weekly Digest
```
GET /api/reports/weekly-digest?weekStart=2024-01-08
Get weekly feedback summary (auto-generated every Monday 8 AM UTC)
```

---

## Data Import Formats

### CSV Format
```csv
Text,Rating,ProductArea,Category,CustomerSegment,CreatedAt
"App crashes on startup",1,"Performance","Bug","Enterprise","2024-01-15"
"Great user experience",5,"UX","Feature","SMB","2024-01-16"
```

### JSON Format
```json
[
  {
    "text": "App crashes on startup",
    "rating": 1,
    "productArea": "Performance",
    "category": "Bug",
    "customerSegment": "Enterprise",
    "createdAt": "2024-01-15"
  }
]
```

---

## Architecture

### Service Layers

**AI Services** (`Services/AI/`)
- `EmbeddingService`: OpenAI embeddings generation
- `SentimentAnalysisService`: Sentiment & urgency extraction
- `ThemeLabelingService`: Theme labeling & relevance scoring
- `ActionRecommendationService`: Action recommendation generation

**Business Services** (`Services/Business/`)
- `FeedbackProcessingService`: Orchestrates full pipeline
- `ClusteringService`: Semantic clustering with precision metrics
- `EvaluationMetricsService`: Quality metrics calculation
- `ReportingService`: Dashboard & report generation
- `EvaluationNotebookService`: Evaluation report generation
- `ScheduledDigestService`: Weekly digest generation

**Background Services** (`Services/Background/`)
- `WeeklyDigestBackgroundService`: Automatic Monday 8 AM digest

**Data Sources** (`Services/DataSources/`)
- `IFeedbackSourceAdapter`: Abstract adapter interface
- `CsvFeedbackAdapter`: CSV import
- `JsonFeedbackAdapter`: JSON import
- `DataSourceManager`: Adapter management

---

## Database Schema

Key entities:
- `FeedbackItem`: Raw feedback with embeddings, sentiment, urgency
- `Theme`: Extracted themes with relevance scores
- `ThemeCluster`: Clustered feedback items
- `ActionRecommendation`: Recommended actions with priority/effort
- `EvaluationRun`: Evaluation metrics and quality scores
- `ThemeEvaluation`: Per-theme quality evaluation
- `ActionRecommendationEvaluation`: Per-recommendation usefulness
- `ScheduledDigestRun`: Serialized weekly digests

---

## Configuration

### appsettings.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=FeedbackMiner;Integrated Security=true;"
  },
  "FeedbackMiner": {
    "DigestScheduleTime": "14:15:00",
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

## Contributors
- Mahima Munjal
- Mahima Vikram Bhandari
- Supriya Kailas Ghadge
- Abhipsha Satpathy
- Satya Prakash Arya

