# Activity 3: Middleware Testing and Validation Guide

## Overview
This document provides testing instructions for the implemented middleware components in the UserManagementAPI.

## Implemented Middleware Components

### 1. RequestResponseLoggingMiddleware
**Purpose**: Logs all HTTP requests and responses for auditing purposes
**Location**: `UserManagementAPI/Middleware/RequestResponseLoggingMiddleware.cs`

**Features**:
- Logs HTTP method, request path, query parameters
- Logs response status code and execution time
- Includes request/response headers (excludes sensitive ones)
- Captures request/response bodies with size limits
- Assigns unique request IDs for correlation

### 2. GlobalExceptionHandlingMiddleware
**Purpose**: Provides standardized error handling across all endpoints
**Location**: `UserManagementAPI/Middleware/GlobalExceptionHandlingMiddleware.cs`

**Features**:
- Catches all unhandled exceptions
- Returns consistent JSON error responses
- Maps specific exception types to appropriate HTTP status codes
- Includes trace IDs for debugging
- Enhanced logging with request context

### 3. TokenAuthenticationMiddleware
**Purpose**: Validates JWT tokens and enforces authentication
**Location**: `UserManagementAPI/Middleware/TokenAuthenticationMiddleware.cs`

**Features**:
- Validates JWT tokens from Authorization header or query parameter
- Excludes certain paths from authentication (login, register, swagger)
- Returns 401 Unauthorized for invalid/missing tokens
- Sets user context for authenticated requests

## Middleware Pipeline Order

The middleware is configured in the optimal order in `Program.cs`:

1. **GlobalExceptionHandlingMiddleware** (first - catches all exceptions)
2. **CORS** (if in development)
3. **HTTPS Redirection**
4. **Swagger** (if in development)
5. **TokenAuthenticationMiddleware** (validates authentication)
6. **UseAuthentication() / UseAuthorization()** (ASP.NET Core auth)
7. **RequestResponseLoggingMiddleware** (last - logs complete flow)
8. **MapControllers**

## Testing Instructions

### Step 1: Start the Application
```bash
dotnet run --project UserManagementAPI
```

### Step 2: Test Authentication Endpoints (No Token Required)

#### Register a New User
```bash
POST https://localhost:5001/api/auth/register
Content-Type: application/json

{
  "email": "test@example.com",
  "username": "testuser",
  "password": "Password123!",
  "fullName": "Test User"
}
```

**Expected**: 200 OK with JWT token

#### Login
```bash
POST https://localhost:5001/api/auth/login
Content-Type: application/json

{
  "email": "test@example.com",
  "password": "Password123!"
}
```

**Expected**: 200 OK with JWT token

### Step 3: Test Protected Endpoints

#### Without Token (Should Fail)
```bash
GET https://localhost:5001/api/user
```

**Expected**: 401 Unauthorized with error message

#### With Valid Token
```bash
GET https://localhost:5001/api/user
Authorization: Bearer [your-jwt-token]
```

**Expected**: 200 OK with user data

#### With Invalid Token
```bash
GET https://localhost:5001/api/user
Authorization: Bearer invalid-token
```

**Expected**: 401 Unauthorized

### Step 4: Test Error Handling

#### Trigger Exception (Invalid User ID)
```bash
GET https://localhost:5001/api/user/invalid-id
Authorization: Bearer [your-jwt-token]
```

**Expected**: Consistent error response with trace ID

### Step 5: Validate Logging

Check the console output for:
- Request logs showing HTTP method, path, headers
- Response logs showing status code, execution time
- Authentication logs for successful/failed attempts
- Error logs with trace IDs for exceptions

### Expected Log Format

**Request Log Example**:
```
[INFO] HTTP Request | RequestId: abc123 | Method: GET | Path: /api/user | Query: ?pageNumber=1
```

**Response Log Example**:
```
[INFO] HTTP Response | RequestId: abc123 | StatusCode: 200 | ElapsedMs: 45 | ContentType: application/json
```

**Authentication Log Example**:
```
[INFO] User user123 authenticated for request to /api/user
```

## Security Features Validated

? **Authentication Required**: All user endpoints require valid JWT token
? **Error Handling**: Consistent error responses across all endpoints  
? **Request Logging**: Complete audit trail of all API calls
? **Token Validation**: Proper JWT validation with secure algorithms
? **Path Exclusions**: Public endpoints (auth, swagger) work without tokens

## Performance Considerations

- Request/response logging has 1MB size limits to prevent memory issues
- Sensitive headers (Authorization, Cookie) are excluded from logs
- Middleware uses efficient streaming for response capture
- JWT validation uses proper token caching mechanisms

## Troubleshooting

### Common Issues

1. **401 on all requests**: Check JWT configuration in appsettings.json
2. **Missing logs**: Verify logging level configuration
3. **Token not recognized**: Ensure "Bearer " prefix in Authorization header
4. **Build errors**: Verify all NuGet packages are installed

### Configuration Files

- JWT settings: `appsettings.json` under "Jwt" section
- Logging levels: `appsettings.json` under "Logging" section
- Database connection: `appsettings.json` under "ConnectionStrings"

## Compliance Summary

? **Corporate Policy Requirements Met**:
- ? Log all incoming requests and outgoing responses for auditing
- ? Enforce standardized error handling across all endpoints  
- ? Secure API endpoints using token-based authentication
- ? Middleware pipeline configured for optimal performance