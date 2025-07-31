# UserManagementAPI - TechHive Solutions

A comprehensive .NET 8 Web API for user management built with enterprise-grade middleware components, authentication, and auditing capabilities to meet corporate policy requirements.

## ?? Project Overview

The UserManagementAPI is a production-ready web application developed for TechHive Solutions to provide secure user management capabilities while ensuring compliance with corporate policies. The project demonstrates progressive development through three distinct phases, each building upon the previous to create a robust, scalable, and secure API system.

## ?? Key Features

### Core API Functionality
- **Complete User Management**: CRUD operations for user accounts with comprehensive validation
- **JWT Authentication**: Secure token-based authentication with configurable expiration
- **RESTful Design**: Clean, consistent API endpoints following REST principles
- **Comprehensive Validation**: Input validation with detailed error messages
- **Pagination & Search**: Efficient data retrieval with search and sorting capabilities

### Enterprise Middleware Stack
- **Request/Response Logging**: Complete audit trail of all HTTP requests and responses
- **Global Exception Handling**: Standardized error responses with debugging trace IDs
- **JWT Token Authentication**: Secure authentication middleware with path exclusions
- **Performance Monitoring**: Execution time tracking and request correlation

### Security & Compliance
- **Corporate Policy Compliance**: Full auditing, error handling, and authentication
- **Sensitive Data Protection**: Automatic filtering of sensitive headers from logs
- **Thread-Safe Operations**: Concurrent request handling with proper synchronization
- **Comprehensive Error Handling**: Consistent JSON error responses across all endpoints

## ??? Project Architecture

### Technology Stack
- **.NET 8**: Latest framework for optimal performance and modern C# features
- **ASP.NET Core**: Web API framework with built-in dependency injection
- **Entity Framework Core**: ORM with SQL Server integration
- **ASP.NET Core Identity**: User management and authentication framework
- **JWT Bearer Authentication**: Secure token validation and user context management
- **Swagger/OpenAPI**: Interactive API documentation with authentication support

### Project StructureUserManagementAPI/
??? Controllers/          # API controllers (User, Auth, Test)
??? Middleware/          # Custom middleware components
??? Services/           # Business logic and validation services
??? Models/            # Data models and DTOs
??? Data/             # Entity Framework context and configuration
??? Entities/         # Database entity models
??? documents/        # Project documentation and activity summaries

UserManagementAPI_Testing/
??? Unit/             # Unit tests for controllers and services
??? Integration/      # Integration tests for middleware pipeline
??? Infrastructure/   # Test utilities and helper classes
## ?? API Endpoints

### Authentication Endpoints (Public)
- `POST /api/auth/register` - Register new user account
- `POST /api/auth/login` - Authenticate user and receive JWT token
- `POST /api/auth/validate` - Validate JWT token

### User Management Endpoints (Protected)
- `GET /api/user` - Retrieve paginated users with search/sort
- `GET /api/user/{id}` - Get specific user by ID
- `POST /api/user` - Create new user account
- `PUT /api/user/{id}` - Update existing user
- `DELETE /api/user/{id}` - Delete user account

### Development Endpoints
- `GET /api/test` - Test endpoint for middleware validation
- `GET /swagger` - Interactive API documentation
- `GET /health` - Application health check

## ??? Getting Started

### Prerequisites
- .NET 8 SDK
- SQL Server (LocalDB supported)
- Visual Studio 2022 or VS Code

### Installation & Setup

1. **Clone the repository**git clone [repository-url]
cd TechHive
2. **Configure database connection**// appsettings.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=UserManagementAPI;Trusted_Connection=true"
  }
   }
3. **Configure JWT settings**// appsettings.json
{
  "Jwt": {
    "Key": "YourSuperSecretKeyThatIsAtLeast32CharactersLong!",
    "Issuer": "UserManagementAPI",
    "Audience": "UserManagementAPI"
  }
}
4. **Run database migrations**dotnet ef database update --project UserManagementAPI
5. **Start the application**dotnet run --project UserManagementAPI
6. **Access the API**
   - API: `https://localhost:5001`
   - Swagger UI: `https://localhost:5001/swagger`

### Running Tests# Run all tests
dotnet test

# Run specific test category
dotnet test --filter "Category=Integration"
dotnet test --filter "Category=Unit"
## ?? Development Journey

This project was developed through three progressive activities, each demonstrating different aspects of modern API development with Microsoft Copilot assistance:

### [Activity 1: Writing and Enhancing API Code](UserManagementAPI/documents/activity1.md)
- **Focus**: Core API development with Copilot assistance
- **Achievements**: RESTful controller design, DTOs, error handling, async patterns
- **Key Features**: CRUD operations, model validation, dependency injection
- **Copilot Impact**: Code quality improvements, best practices guidance, security recommendations

### [Activity 2: Code Enhancement and Optimization](UserManagementAPI/documents/activity2.md)
- **Focus**: Performance optimization and robust validation
- **Achievements**: Pagination, advanced validation, comprehensive testing
- **Key Features**: Search/sort functionality, business logic validation, exception handling
- **Copilot Impact**: Performance optimization strategies, testing methodologies, validation patterns

### [Activity 3: Implementing and Managing Middleware](UserManagementAPI/documents/activity3.md)
- **Focus**: Enterprise middleware implementation for corporate compliance
- **Achievements**: Custom middleware pipeline, JWT authentication, comprehensive auditing
- **Key Features**: Request/response logging, global exception handling, security middleware
- **Copilot Impact**: Middleware architecture guidance, security best practices, integration testing strategies

## ?? Security Features

### Authentication & Authorization
- **JWT Token Validation**: Comprehensive token verification including signature, issuer, audience, and expiration
- **Path-Based Security**: Intelligent exclusion of public endpoints (login, register, swagger, health)
- **User Context Management**: Proper extraction and setting of user claims in request context
- **Token Sources**: Support for tokens in both Authorization headers and query parameters

### Auditing & Compliance
- **Complete Request Logging**: All HTTP methods, paths, query parameters, and headers
- **Response Tracking**: Status codes, execution times, and response bodies
- **Correlation IDs**: Unique request identifiers for distributed tracing
- **Security-Aware Logging**: Automatic filtering of sensitive headers and authentication tokens

### Error Handling & Monitoring
- **Standardized Error Responses**: Consistent JSON format across all endpoints
- **Exception Type Mapping**: Intelligent mapping of specific exceptions to appropriate HTTP status codes
- **Debug Information**: TraceId inclusion for debugging without exposing sensitive data
- **Comprehensive Coverage**: Handling of all common exception types with proper logging

## ?? Testing Strategy

### Comprehensive Test Suite (37+ Tests)
- **Unit Tests**: Controllers, services, and business logic validation
- **Integration Tests**: Full middleware pipeline testing with real HTTP requests
- **Security Tests**: Authentication, authorization, and sensitive data protection
- **Performance Tests**: Concurrent request handling and execution time validation

### Test Categories
- **UserController Tests**: 16 tests covering CRUD operations with authentication
- **AuthController Tests**: 8 tests for registration, login, and token validation
- **AuthService Tests**: 6 tests for business logic and JWT token management
- **Middleware Integration Tests**: 10+ tests for pipeline execution and security
- **Model Validation Tests**: 3 tests for pagination and input validation

### Testing Infrastructure
- **Custom TestLoggerProvider**: Thread-safe log collection for middleware testing
- **Mock Authentication Context**: Comprehensive user context setup for protected endpoints
- **Concurrent Request Testing**: Multi-threaded request validation with unique request IDs
- **Security Header Filtering**: Validation of sensitive data protection in logs

## ? Performance Characteristics

### Scalability Features
- **Pagination**: Efficient database queries limiting memory usage from O(n) to O(page_size)
- **Async Operations**: Non-blocking operations throughout the application
- **Memory Management**: Size limits for request/response body logging (1MB)
- **Thread Safety**: Concurrent request handling with proper synchronization

### Monitoring & Observability
- **Request Correlation**: Unique request IDs for distributed tracing
- **Execution Time Tracking**: Precise performance measurement using Stopwatch
- **Comprehensive Logging**: Structured logs with contextual information
- **Health Checks**: Application status monitoring endpoint

## ?? Contributing

This project demonstrates enterprise-level development practices and serves as a reference implementation for:
- Middleware design patterns in .NET 8
- JWT authentication and authorization
- Comprehensive testing strategies
- Security best practices
- Performance optimization techniques

## ?? License

This project is developed for educational and demonstration purposes as part of the TechHive Solutions learning initiative.

## ?? Acknowledgments

Special thanks to Microsoft Copilot for providing intelligent code suggestions, architectural guidance, and best practices recommendations throughout the development process. The AI assistance was instrumental in achieving production-ready code quality and comprehensive security implementation.

---

*For detailed information about each development phase, please refer to the individual activity documentation linked above.*