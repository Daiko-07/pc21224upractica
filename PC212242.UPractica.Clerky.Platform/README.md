# Clerky Platform API

## Overview
Clerky Platform is a REST API developed with ASP.NET Core 10 for contract management.

The application follows Domain-Driven Design (DDD) and Command Query Responsibility Segregation (CQRS) principles.

## Technologies
- C# and .NET 10
- ASP.NET Core
- Entity Framework Core 9
- MySQL
- Pomelo Entity Framework Core MySQL Provider
- OpenAPI

## Architecture
The project is organized into two main areas:

- Paperwork: Contract domain, application services, infrastructure, and REST interfaces.
- Shared: Reusable domain components and shared infrastructure.

## Features
- Create contracts
- Validate monetary amounts and validity periods
- Prevent duplicate document identifiers
- Persist contracts in MySQL
- Return localized validation errors in English and Spanish

## API Endpoint

POST /api/v1/contracts

Example request:

{
  "documentId": "550e8400-e29b-41d4-a716-446655440000",
  "clientId": "550e8400-e29b-41d4-a716-446655440001",
  "periodStartDate": "2026-11-01",
  "periodEndDate": "2026-12-01",
  "monetaryAmountValue": 1500.00,
  "monetaryAmountCurrency": "USD"
}

## HTTP Responses
- 201 Created: Contract created successfully.
- 400 Bad Request: Invalid contract information.
- 409 Conflict: Duplicate document identifier.

## Running the Application
1. Install the .NET 10 SDK.
2. Configure the MySQL connection string.
3. Run dotnet restore.
4. Run dotnet ef database update.
5. Run dotnet run.

## Documentation
OpenAPI specification: /openapi/v1.json

XML documentation generation is enabled.

## Database
The application uses MySQL and Entity Framework Core migrations.
