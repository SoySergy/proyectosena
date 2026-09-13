---
phase: code-review
reviewed: 2026-09-10T22:00:00Z
depth: standard
files_reviewed: 13
files_reviewed_list:
  - Controllers/AuthController.cs
  - Controllers/CollectionRequestController.cs
  - Controllers/UserController.cs
  - Controllers/AdminController.cs
  - Controllers/ManagerApplicationController.cs
  - Services/AuthService.cs
  - Services/UserService.cs
  - Services/CollectionRequestService.cs
  - Services/AssignmentService.cs
  - Services/CollectionStatusService.cs
  - Services/AdminService.cs
  - Services/ManagerApplicationService.cs
  - Repositories/UserRepository.cs
findings:
  critical: 0
  warning: 2
  info: 1
  total: 3
status: issues_found
---

# Phase Code Review Report: C# ASP.NET Core Backend

**Reviewed:** 2026-09-10T22:00:00Z  
**Depth:** standard  
**Files Reviewed:** 13  
**Status:** issues_found  

## Summary

An adversarial code review was conducted on the core Controllers, Services, and Repositories of the ASP.NET Core backend (`proyectosena`). The codebase demonstrates solid defensive programming practices, robust authorization checks (preventing IDOR vulnerabilities in controllers like `CollectionRequestController` and `UserController`), secure password hashing via BCrypt, and atomic transaction handling with pessimistic locking for concurrency control. 

However, a few architectural quality, SOLID/DRY, and robustness concerns were identified regarding database dialect coupling, case-insensitive uniqueness guarantees across database migrations, and strongly-typed status management.

---

## Warnings

### WR-01: Database Dialect Coupling via Raw SQL Locking (SOLID / Dependency Inversion Violation)

**File:** `Services/AssignmentService.cs:56-60`  
**Issue:**  
The service uses raw SQL (`FROM_SQL_RAW` with `FOR UPDATE`) tied specifically to PostgreSQL syntax to implement pessimistic locking during request assignment. While this successfully prevents race conditions under PostgreSQL, it violates the Dependency Inversion Principle and repository abstraction boundaries by embedding SQL dialect specifics directly into the business service layer. Furthermore, this prevents swapping out Entity Framework Core for an In-Memory database provider during unit testing.

**Fix:**  
Encapsulate pessimistic locking or concurrency strategies inside the repository layer (e.g., via `ICollectionRequestRepository`) or use EF Core transaction isolation levels rather than raw SQL in service classes.

---

### WR-02: Potential Case-Sensitivity Race Condition in Email Uniqueness

**File:** `Services/AuthService.cs:53-55` & `Repositories/UserRepository.cs:88-96`  
**Issue:**  
While emails are normalized in application logic (`ToLower()`), PostgreSQL default `text` / `varchar` columns are case-sensitive. If the database schema lacks a unique functional index (`CREATE UNIQUE INDEX ... ON "Users" (LOWER("Email"))`) or `citext` column type, concurrent registration requests with differing casing (e.g., `Test@Domain.com` and `test@domain.com`) could bypass application-level checks before the unique constraint evaluates, leading to duplicate records or unhandled database exceptions.

**Fix:**  
Ensure that PostgreSQL migrations define a unique functional index on `LOWER("Email")` and `LOWER("DocumentNumber")` to guarantee database-level case-insensitive uniqueness regardless of application input casing.

---

## Info

### IN-01: Use of String Constants for Request Statuses

**File:** `Models/CollectionRequestStatus.cs` (referenced across Services and Controllers)  
**Issue:**  
Collection request statuses are managed using `string` constants (e.g., `CollectionRequestStatus.Pending`, `CollectionRequestStatus.Assigned`) validated against a static `ValidStatuses` list. While flexible, stringly-typed state management increases the risk of typos if new statuses are added without updating validation collections, and lacks compile-time safety compared to C# enums with value converters.

**Fix:**  
Consider transitioning status representations to a strongly-typed C# `enum` mapped to string values in Entity Framework Core via `HasConversion<string>()`.

---

_Reviewed: 2026-09-10T22:00:00Z_  
_Reviewer: the agent (gsd-code-reviewer)_  
_Depth: standard_
