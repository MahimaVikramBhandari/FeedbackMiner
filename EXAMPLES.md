# FeedbackMiner API Usage Examples

This document provides examples of how to use the FeedbackMiner API to submit and manage feedback data.

## Base URL

```
http://localhost:5000/api
```

## Authentication

Currently, the API does not require authentication. Ensure you have valid environment variables configured:
- `OPENAI_API_KEY`: Your OpenAI API key for AI-powered analysis

## Endpoints

### 1. Submit Feedback

**Endpoint**: `POST /api/feedback`

**Description**: Creates and ingests a new feedback item into the system for processing and analysis.

**Request Body**:

```json
{
  "source": "customer_email",
  "text": "The new dashboard is intuitive and user-friendly. However, the export feature takes too long to complete.",
  "rating": 4,
  "productArea": "Dashboard",
  "category": "UX/UI",
  "customerSegment": "Enterprise",
  "metadata": {
	"customerId": "CUST-12345",
	"region": "North America",
	"industry": "Finance"
  }
}
```

**Fields**:

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| source | string | Yes | Source of the feedback (e.g., "customer_email", "support_ticket", "survey") |
| text | string | Yes | The actual feedback content |
| rating | integer | No | Customer satisfaction rating (typically 1-5 scale) |
| productArea | string | No | Product area the feedback relates to (e.g., "Dashboard", "API", "Mobile App") |
| category | string | No | Feedback category (e.g., "Feature Request", "Bug Report", "UX/UI") |
| customerSegment | string | No | Target customer segment (e.g., "Enterprise", "SMB", "Startup") |
| metadata | object | No | Additional custom metadata as key-value pairs |

**Response**:

```json
HTTP/1.1 200 OK
```

---

## Usage Examples

### Example 1: Bug Report Feedback

```bash
curl -X POST http://localhost:5000/api/feedback \
  -H "Content-Type: application/json" \
  -d '{
	"source": "github_issue",
	"text": "Application crashes when uploading files larger than 500MB",
	"rating": 1,
	"productArea": "File Upload",
	"category": "Bug Report",
	"customerSegment": "Enterprise",
	"metadata": {
	  "severity": "Critical",
	  "os": "Windows 11",
	  "browserVersion": "Chrome 125"
	}
  }'
```

### Example 2: Feature Request

```bash
curl -X POST http://localhost:5000/api/feedback \
  -H "Content-Type: application/json" \
  -d '{
	"source": "customer_portal",
	"text": "We would love to see integrations with Slack and Microsoft Teams for notifications",
	"rating": 4,
	"productArea": "Integrations",
	"category": "Feature Request",
	"customerSegment": "SMB",
	"metadata": {
	  "accountName": "Acme Corp",
	  "implementationStage": "Post-Launch",
	  "priority": "High"
	}
  }'
```

### Example 3: Support Ticket Feedback

```bash
curl -X POST http://localhost:5000/api/feedback \
  -H "Content-Type: application/json" \
  -d '{
	"source": "support_ticket",
	"text": "Support team was incredibly helpful in resolving our data migration issues. Quick response time and clear guidance.",
	"rating": 5,
	"productArea": "Support",
	"category": "Compliment",
	"customerSegment": "Enterprise",
	"metadata": {
	  "ticketId": "SUP-98765",
	  "supportAgent": "agent@example.com",
	  "resolutionTime": "2 hours"
	}
  }'
```

### Example 4: Multilingual Feedback

```bash
curl -X POST http://localhost:5000/api/feedback \
  -H "Content-Type: application/json" \
  -d '{
	"source": "email",
	"text": "我非常喜欢这个产品的界面设计，但是文档需要更多示例代码。",
	"rating": 4,
	"productArea": "Documentation",
	"category": "Suggestion",
	"customerSegment": "Developer",
	"metadata": {
	  "language": "Chinese",
	  "country": "China"
	}
  }'
```

### Example 5: Survey Response

```bash
curl -X POST http://localhost:5000/api/feedback \
  -H "Content-Type: application/json" \
  -d '{
	"source": "survey",
	"text": "The pricing model is competitive, but we need more flexible billing options. Consider monthly or custom tier options.",
	"rating": 3,
	"productArea": "Billing",
	"category": "Pricing Feedback",
	"customerSegment": "SMB",
	"metadata": {
	  "surveyId": "SUR-2024-Q1",
	  "completionTime": "5 minutes",
	  "respondentRole": "CFO"
	}
  }'
```

---

## Integrated AI Processing

Once feedback is submitted via the API, the system automatically performs the following AI-powered analyses:

1. **Language Detection**: Automatically detects the language of the feedback
2. **Sentiment Analysis**: Classifies feedback sentiment (positive, negative, neutral)
3. **Theme Labeling**: Identifies key themes and topics in the feedback
4. **Action Recommendations**: Generates actionable recommendations based on the feedback
5. **Embedding Generation**: Creates vector embeddings for similarity analysis and clustering
6. **Clustering**: Groups similar feedback items together for trend identification

---

## Error Handling

Common error responses:

```json
{
  "error": "Invalid request",
  "message": "The 'text' field is required."
}
```

---

## Best Practices

1. **Include Metadata**: Use metadata to capture additional context that may be useful for analysis
2. **Consistent Categorization**: Use consistent values for `source`, `productArea`, and `category` to improve analysis accuracy
3. **Use Ratings**: Provide ratings when available to enhance sentiment analysis
4. **Clean Text**: Remove personal information (emails, phone numbers) before submission
5. **Batch Processing**: For bulk submissions, submit feedback items individually to allow for proper asynchronous processing

---

## Environment Setup

Before using the API, ensure the following is configured:

1. Create a `.env` file in the project root:
   ```
   OPENAI_API_KEY=your_openai_api_key_here
   DefaultConnection=your_sql_server_connection_string
   ```

2. Run database migrations:
   ```bash
   dotnet ef database update
   ```

3. Start the application:
   ```bash
   dotnet run
   ```

4. Access Swagger documentation:
   ```
   http://localhost:5000/swagger
   ```

---

## C# Client Example

If you're integrating with FeedbackMiner from a C# application:

```csharp
using HttpClient client = new();
var request = new CreateFeedbackRequest
{
	Source = "api_client",
	Text = "The API response times have improved significantly in the latest release.",
	Rating = 5,
	ProductArea = "API Performance",
	Category = "Compliment",
	CustomerSegment = "Developer",
	Metadata = new Dictionary<string, string>
	{
		{ "benchmarkImprovement", "25% faster" },
		{ "testedEndpoint", "/api/feedback" }
	}
};

var json = JsonConvert.SerializeObject(request);
var content = new StringContent(json, Encoding.UTF8, "application/json");
var response = await client.PostAsync("http://localhost:5000/api/feedback", content);

if (response.IsSuccessStatusCode)
{
	Console.WriteLine("Feedback submitted successfully!");
}
```

---

## Support

For issues or questions about the API, please refer to the project repository or contact the development team.
