# Email Batch Queue Application

This application creates an API to queue and process bulk emails as well as a web application to view the status of each
email batch.

A batch of emails can be sent as an array to the API, which will then queue and process them with a configurable delay
between each. The submitted emails are also saved in a database. If the application needs to restart before all emails
are processed, any unsent emails will be loaded from the database and processing will continue.

[![Georgia EPD-IT](https://raw.githubusercontent.com/gaepdit/gaepd-brand/main/blinkies/blinkies.cafe-gaepdit.gif)](https://github.com/gaepdit)
[![.NET Test](https://github.com/gaepdit/email-queue/actions/workflows/dotnet-test.yml/badge.svg)](https://github.com/gaepdit/email-queue/actions/workflows/dotnet-test.yml)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=gaepdit_email-queue&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=gaepdit_email-queue)
[![Lines of Code](https://sonarcloud.io/api/project_badges/measure?project=gaepdit_email-queue&metric=ncloc)](https://sonarcloud.io/summary/new_code?id=gaepdit_email-queue)

## API Configuration

The API application is configured through `appsettings.json` with the following sections:

### Email Queue Settings

Configure the delay in seconds between processing each email.

```json
{
  "EmailQueueSettings": {
    "ProcessingDelaySeconds": 5
  }
}
```

### Security

All API endpoints require authentication using a Client ID passed in the `X-Client-ID` header and an API Key passed in
the `X-API-Key` header with each request.

Valid API clients are configured in `appsettings.json`:

```json
{
  "ApiClients": [
    {
      "ClientName": "Your Web Application",
      "ClientId": "your-client-guid",
      "ApiKey": "your-secret-api-key"
    }
  ]
}
```

The Client Name and ID fields are saved in the database with each email record.

### API Endpoints

#### GET `/health`

Returns OK if the API is running.

#### POST `/add`

Submits a batch of email tasks for processing.

Request body: Array of email tasks.

```json
[
  {
    "from": "from.email@example.net",
    "recipients": [
      "email@example.com"
    ],
    "copyRecipients": [],
    "subject": "Email Subject",
    "body": "Email content",
    "isHtml": false
  }
]
```

Each email task contains the following properties:

- `from`: The return (from) email address (Required)
- `recipients`: List of recipient email addresses (Required; may not be empty or contain any empty values)
- `copyRecipients`: List of copied email addresses (Optional; may not contain empty values if included)
- `subject`: Email subject line, max 200 characters (Required)
- `body`: Email content, max 20,000 characters (Required)
- `isHtml`: Boolean indicating if the body is formatted as HTML (Required)

Response format if successful:

```json
{
  "status": "Success",
  "count": 1,
  "batchId": "guid-of-batch"
}
```

The Batch ID is a GUID.

If no email tasks are submitted, the following response will be returned:

```json
{
  "status": "Empty",
  "count": 0,
  "batchId": ""
}
```

#### GET `/all-batches`

Returns a list of all Batch IDs in the system for the provided Client ID, ordered by creation date descending.

Response format:

```json
[
  {
    "batchId": "guid-of-batch",
    "count": 1,
    "queued": 1,
    "sent": 0,
    "failed": 0,
    "skipped": 0,
    "createdAt": "2025-06-02T19:30:00.0000000"
  }
]
```

#### POST `/batch-status`

Returns the status of a specific Batch ID.

Request body:

```json
{
  "batchId": "guid-of-batch"
}
```

Response format:

```json
{
  "batchId": "guid-of-batch",
  "count": 1,
  "queued": 1,
  "sent": 0,
  "failed": 0,
  "skipped": 0,
  "createdAt": "2025-06-02T19:30:00.0000000"
}
```

#### POST `/batch-details`

Returns the status of each email task for a specific Batch ID, ordered by creation date ascending.

Request body:

```json
{
  "batchId": "guid-of-batch"
}
```

Response format:

```json
[
  {
    "id": "guid-of-email-task",
    "counter": 1,
    "status": "Queued",
    "failureReason": null,
    "createdAt": "2025-06-02T19:30:00.0000000",
    "attemptedAt": "2025-06-02T19:30:00.0000000",
    "from": "from.email@example.net",
    "recipients": [
      "email@example.com"
    ],
    "copyRecipients": [],
    "subject": "Email Subject"
  }
]
```

---

## Sample Web App Configuration

A sample web application is provided to demonstrate displaying data from the API. The web application is configured
through `appsettings.json` with the following section:

```json
{
  "EmailQueueApi": {
    "BaseUrl": "https://localhost:7145",
    "ClientId": "your-client-guid",
    "ApiKey": "your-secret-api-key"
  }
}
```
