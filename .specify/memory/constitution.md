<!--
================================================================================
SYNC IMPACT REPORT
================================================================================
Version Change: 1.1.0 → 1.2.0 (Added documentation policy)
Modified Principles:
  - V. Infrastructure as Code → Added .NET Aspire for local orchestration (prior)
Added Sections:
  - Documentation Policy (under Governance)
Removed Sections: None
Templates Requiring Updates:
  - .specify/templates/plan-template.md: ✅ Compatible
  - .specify/templates/spec-template.md: ✅ Compatible
  - .specify/templates/tasks-template.md: ✅ Compatible
  - .specify/templates/agent-file-template.md: ✅ Compatible
  - .specify/templates/checklist-template.md: ✅ Compatible
Follow-up TODOs: None
================================================================================
-->

# Meeting Copilot Constitution

## Core Principles

### I. Modern .NET First

All code MUST use .NET 10 with C# 14 features and follow Microsoft's latest best practices:
- Top-level statements for `Program.cs` (minimal API pattern)
- File-scoped namespaces to reduce nesting
- Primary constructors for dependency injection where appropriate
- Record types for immutable data transfer objects
- Pattern matching and switch expressions for cleaner control flow
- Nullable reference types enabled project-wide
- ImplicitUsings enabled for cleaner imports

**Rationale**: Modern C# features reduce boilerplate, improve readability, and align with Microsoft's
recommended patterns. .NET 10 provides performance improvements and latest Blazor enhancements.

### II. Blazor Interactive Server

The application MUST use Blazor Interactive Server rendering with these constraints:
- Components use `@rendermode InteractiveServer` for real-time updates
- SignalR connections managed via `ReconnectModal` for connection resilience
- Scoped services for per-circuit state (e.g., `SpeechRecognitionService`)
- Razor components follow separation: `.razor` for markup, `.razor.cs` for code-behind when complex
- JavaScript interop minimized; use only for browser APIs (microphone, Web Audio)

**Rationale**: Interactive Server provides real-time capabilities for speech recognition without
WASM complexity. SignalR enables server-push for transcription updates.

### III. Azure-Native Security (NON-NEGOTIABLE)

All Azure service integrations MUST follow secure authentication patterns:
- Managed Identity for Azure-hosted deployments (DefaultAzureCredential)
- User Secrets for local development—NEVER commit secrets to source control
- Azure Key Vault for production secrets and configuration
- No hardcoded credentials or connection strings in code or config files
- Least privilege RBAC: scope roles to specific resources, not subscriptions

**Rationale**: Azure MCP best practices mandate credential security. Key-based authentication
creates security vulnerabilities and operational risks during key rotation.

### IV. AI Agent Framework Integration

When implementing AI/Agent features, code MUST use Microsoft Agent Framework:
- Use `Microsoft.Agents.AI` and related packages (preview versions with `--prerelease`)
- Invoke `aitk-get_agent_code_gen_best_practices` before any AI agent code generation
- Invoke `aitk-get_evaluation_code_gen_best_practices` for evaluation code
- Invoke `aitk-evaluation_agent_runner_best_practices` for agent runner setup
- Prefer GitHub Models for prototyping; Microsoft Foundry for production
- Document model selection rationale in implementation plans

**Rationale**: Azure AI Toolkit MCP tools provide official guidance that prevents common
misconfigurations, security issues, and ensures latest patterns are followed.

### V. Infrastructure as Code & Local Orchestration

All Azure resource provisioning MUST use Infrastructure as Code, and local development MUST use .NET Aspire:
- .NET Aspire CLI (`dotnet aspire`) for local development orchestration and service discovery
- Aspire AppHost project for defining application topology and dependencies
- Aspire ServiceDefaults for consistent configuration across services
- Bicep files stored in `infra/` directory (not PowerShell or bare `az` commands)
- Azure Developer CLI (`azd`) for deployment orchestration to Azure
- Always validate deployments with `azd provision --preview` before executing
- Disable key-based access for Storage and Cosmos DB accounts
- Enable Managed Identity for all deployed resources

**Local Development Flow**:
- Use `dotnet aspire run` to start the application with all dependencies
- Aspire dashboard provides observability for local debugging
- Service discovery handled automatically via Aspire configuration

**Deployment Flow**:
- Use `azd up` for full provision + deploy workflow
- Use `azd provision` for infrastructure only
- Use `azd deploy` for application deployment only

**Rationale**: .NET Aspire simplifies local development by providing service orchestration, dependency
management, and built-in observability. `azd` provides seamless Azure deployment that works with
Aspire-defined topology. IaC ensures reproducible, auditable deployments.

### VI. Observability & Tracing

All services MUST implement structured logging and tracing:
- Use `Microsoft.Extensions.Logging` with structured log messages
- Invoke `aitk-get_tracing_code_gen_best_practices` for AI-specific tracing
- Application Insights integration for production monitoring
- Include correlation IDs for distributed tracing across services
- Log security events and authentication failures

**Rationale**: Observability is critical for debugging real-time speech recognition issues
and AI agent behavior. Structured logging enables effective troubleshooting.

### VII. Test-Driven Quality

Testing MUST be prioritized with these requirements:
- Unit tests for business logic and services using xUnit
- Integration tests for Azure service connections
- Contract tests for API endpoints and SignalR hubs
- Red-Green-Refactor cycle: tests written before implementation when explicitly requested
- Minimum coverage: critical paths (speech recognition, agent orchestration) MUST be tested

**Rationale**: Meeting transcription accuracy is critical. Tests ensure regressions are caught
and speech recognition edge cases are handled correctly.

## Technology Stack Requirements

**Runtime & Language**:
- .NET 10.0 (latest preview/stable)
- C# 14 with nullable reference types enabled
- Blazor Interactive Server for UI components

**Azure Services**:
- Azure Cognitive Services Speech SDK (v1.43.0+)
- Azure OpenAI for AI agent capabilities
- Azure Key Vault for secrets management
- Azure App Service or Container Apps for hosting

**Data Access**:
- Entity Framework Core 8.0+ with SQLite (development) or Azure SQL (production)
- Repository pattern for data access abstraction

**AI/Agent Framework**:
- Microsoft.Agents.AI (preview)
- Microsoft.Extensions.AI for AI abstractions
- Azure.AI.OpenAI for Azure OpenAI integration

**CLI Prerequisites** (MUST be installed and authenticated):
- Azure CLI (`az`) - latest version
- .NET CLI (`dotnet`) - .NET 10 SDK
- .NET Aspire workload (`dotnet workload install aspire`) - for local orchestration
- GitHub CLI (`gh`) - for repository operations
- Azure Developer CLI (`azd`) - for deployment orchestration

## Development Workflow & Quality Gates

**Branch Strategy**:
- Feature branches: `###-feature-name` format
- All changes via pull requests with required reviews
- CI/CD pipelines for build validation and deployment

**Code Review Requirements**:
- Constitution compliance verification on all PRs
- Security review for any credential or authentication changes
- Azure MCP tool invocation evidence in AI-related changes

**Deployment Gates**:
- Local build success (`dotnet build`)
- Local orchestration success (`dotnet aspire run` - all services healthy)
- All tests passing (`dotnet test`)
- Preview validation (`azd provision --preview`)
- Smoke test after deployment

**MCP Tool Invocation Requirements**:
- `mcp_azure_azure-m_get_bestpractices`: Before any Azure code generation or deployment
- `aitk-get_agent_code_gen_best_practices`: Before AI agent code generation
- `aitk-get_evaluation_code_gen_best_practices`: Before evaluation code
- `aitk-get_tracing_code_gen_best_practices`: Before tracing implementation

## Governance

This constitution supersedes all conflicting practices. Amendments require:
1. Documentation of the change rationale
2. Impact analysis on existing code and templates
3. Version increment following semantic versioning:
   - MAJOR: Backward incompatible principle changes
   - MINOR: New principles or materially expanded guidance
   - PATCH: Clarifications, wording fixes

**Compliance Verification**:
- All PRs MUST include a constitution compliance statement
- Complexity deviations MUST be justified with documented rationale
- Use `.specify/templates/agent-file-template.md` for runtime development guidance

**Documentation Policy**:
- Do NOT auto-generate documentation after every task
- Documentation is handled by dedicated custom agents when explicitly invoked
- Code comments for complex logic are still required inline

**Version**: 1.2.0 | **Ratified**: 2025-12-05 | **Last Amended**: 2025-12-06
