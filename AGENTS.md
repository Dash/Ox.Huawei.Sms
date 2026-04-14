# Ox.Huawei.Sms Project Guidelines for AI Agents

## Project Overview
This repository contains a cross-platform .NET solution for interacting with Huawei USB dongles for SMS functionality. It consists of:
- **Ox.Huawei.Sms**: Core library with API clients, models, and interfaces for Huawei device communication.
- **Ox.Huawei.Sms.Monitor**: Background service that monitors for new SMS messages and dispatches them via MQTT or email.
- **Ox.Huawei.Sms.SendCli**: Command-line utility for sending SMS messages through the dongle.

The project is Huawei-specific due to tight API coupling and focuses exclusively on SMS functionality.

## Coding Standards and Style

### General Guidelines
- **Language**: Modern C# (.NET 10.0 LTS target)
- **Architecture**: Clean separation of concerns with dependency injection
- **Async/Await**: Use async/await patterns throughout for I/O operations
- **Error Handling**: Use exceptions appropriately; prefer typed exceptions over generic ones
- **Documentation**: XML documentation comments for public APIs
- **Naming**: PascalCase for types/methods/properties, camelCase for parameters/locals
- **Interfaces**: Prefix with 'I' (e.g., `IApiClient`)

### Code Structure
- **Library (Ox.Huawei.Sms)**: Core abstractions, clients, and data models
- **Monitor**: Service logic, dispatchers, listeners - keep business logic separate from infrastructure
- **CLI**: Simple, focused command-line interface - minimal dependencies
- **Tests**: Comprehensive unit tests with clear naming (e.g., `MethodName_Scenario_ExpectedResult`)

### Dependency Injection
- Use Microsoft.Extensions.DependencyInjection for service registration
- Prefer constructor injection
- Singletons for shared state (e.g., HttpClient instances)
- Scoped/transient for request-specific services

### HTTP Client Usage
- Use `HttpClient` with proper base address configuration
- Implement session management for Huawei API authentication
- Handle XML responses (Huawei API uses XML, not JSON)
- Timeout configuration: 5 seconds default

### Configuration
- Use `IConfiguration` for settings
- Support appsettings.json with environment overrides
- Platform-specific configurations (e.g., Linux-specific paths)

### Logging
- Use Microsoft.Extensions.Logging
- Structured logging with scopes
- Appropriate log levels (Information, Warning, Error)

### Testing
- MSTest for unit tests
- Mock/stub dependencies using standalone classes, do not use third party mocking libraries
- Test public APIs and error conditions

## .editorconfig Compliance
Strictly follow the provided .editorconfig settings:
- Indentation: Tabs (size 4)
- Line endings: CRLF
- Specific C# style preferences (e.g., expression-bodied members, var usage, etc.)
- Naming conventions enforced

## Huawei API Specifics
- API endpoint: Typically `http://192.168.8.1/api/`
- Authentication: Session tokens via `/webserver/SesTokInfo`
- Data format: XML for requests/responses
- Focus: SMS operations only (send, receive, monitor)

## Contribution Guidelines
- Maintain backward compatibility for public APIs
- Add tests for new functionality
- Update documentation for API changes
- Follow semantic versioning

## File Organization
- Keep related classes in appropriate folders (e.g., Messages/, Model/, Dispatchers/)
- Use partial classes if files become too large
- Consistent file naming matching class names

## AI Agent Guidelines

### Change Requirements
All code changes must include meaningful logic improvements alongside any syntactical updates. Purely syntactical changes (e.g., reformatting, renaming variables without functional impact, or style-only adjustments) are not acceptable without accompanying logic enhancements. This ensures that every modification adds value to the codebase.

### Logic Enhancement Examples
When making changes, always consider:
- **Performance**: Optimize algorithms, reduce unnecessary operations, or improve resource usage
- **Reliability**: Add error handling, input validation, or edge case coverage
- **Maintainability**: Refactor for better readability, extract methods, or improve separation of concerns
- **Functionality**: Add new features, fix bugs, or enhance existing behavior
- **Testing**: Include or update tests to cover new logic paths

### Syntactical Changes Policy
If a syntactical change is needed (e.g., due to .editorconfig updates), it must be bundled with logic changes. Never submit changes that are exclusively:
- Code formatting or indentation adjustments
- Variable renaming without functional changes
- Comment updates without code improvements
- Style preference changes without logic benefits

### Review Standards
Ensure that proposed changes demonstrate clear logic improvements, such as:
- Better algorithm efficiency
- Enhanced error resilience
- Improved code organization
- New capabilities or bug fixes
- Comprehensive test coverage for new logic

When generating code, ensure it integrates seamlessly with the existing architecture, maintains the high-quality standards established in the codebase, and always includes substantive logic enhancements.