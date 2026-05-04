# Customer Feedback Theme Miner and Action Recommender

## Objective
Analyze survey comments, complaints, and feedback messages to identify recurring themes and recommend product or service improvements.

## Users/Actors
- Customer success
- Product managers
- Quality teams

## Inputs & Data
- CSAT/NPS comments
- Support feedback
- Product feature list
- Escalation categories

## Core C# + OpenAI Components
- Embedding-based clustering
- GPT theme labeling
- Function calling for action classification
- Sentiment and urgency extraction

## Workflow
Ingest feedback → cluster similar issues → label themes → measure impact → recommend action items

## Non-Functional Requirements
- PII redaction
- Explainable clustering output
- Stable theme naming across runs

## Evaluation Criteria
- Theme relevance >= 4/5
- Duplicate issue clustering precision >= 0.8
- Action recommendation usefulness >= 4/5

## Deliverables
- Theme dashboard
- Weekly feedback digest
- Cluster export
- Evaluation notebook
