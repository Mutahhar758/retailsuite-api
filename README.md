# Boilerplate .NET 8 WebAPI Project

## Project Overview
A clean architecture boilerplate for .NET 8 WebAPI, designed for scalable, maintainable, and testable RESTful APIs. Includes PostgreSQL integration, localization, CQRS, and best practices for enterprise-grade solutions.

## Getting Started
To start a new project using this boilerplate, follow these steps:

   ```
   dotnet new kl-api -n YourProjectName
   ```
   > Replace `YourProjectName` with your desired project name.

## Technology Stack
- .NET 8
- ASP.NET Core WebAPI
- PostgreSQL
- Clean Architecture (Core, Infrastructure, Host)
- CQRS Pattern
- Dependency Injection
- FluentValidation
- IStringLocalizer for localization

## Architecture
- **Core**: Domain and Application layers
- **Infrastructure**: Data access and external services
- **Host**: API and Function endpoints
- **CQRS**: Command and Query separation
- **Dependency Injection**: All interfaces in Application, implementations in Infrastructure

## Localization and Messages
- Message keys in `MessageConstants.cs` (PascalCase)
- Translations in JSON files under `Localization` (snake_case keys)
- Language files: `en-US.json`, `fr-FR.json`, etc.
- Use `{0}`, `{1}` for placeholders
- Retrieve messages via `IStringLocalizer`

## API Design
- Request DTOs: Suffix with `RequestDto`
- Response DTOs: Suffix with `ResponseDto`
- Standard HTTP methods for CRUD
- RESTful endpoint naming
- API versioning via `@VersionedApiController` or `@VersionNeutralApiController`
- Use `ProducesResponseType` for responses
- Validation attributes in RequestDto
- EnumDataType for enum validation

## Data Access
- Use `IRepository<T>` for data operations
- No direct DbContext usage for DML
- Entity table names: snake_case
- Use migrations for schema changes
- Migration command:
  ```
  dotnet ef migrations add <migrationName> --project ../../Migrators/Migrators.PostgreSQL --context ApplicationDbContext -o Migrations/Application
  ```
- Implement pagination via `PaginatedListAsync`

## Code Organization
- Controllers: Only call service methods
- Use partial classes for complex entities
- Organize code into:
  - Controllers
  - Services
  - DTOs
  - Models
  - Specifications
  - Validators

## Validation and Error Handling
- Use FluentValidation
- Consistent error response format
- Proper exception handling and error messages

## Security
- Authentication and authorization
- HTTPS only
- Secure sensitive data

## Documentation
- XML comments for public APIs
- OpenAPI/Swagger standards
- Document complex logic

## Performance
- Caching strategies
- Use async/await for I/O
- Optimize queries
- Pagination for large datasets

## Logging and Monitoring
- Structured logging
- Appropriate log levels
- Monitoring and alerting

## Code Quality
- SOLID principles
- Clean, maintainable code
- Design patterns
- DRY principle
- Proper naming and formatting
