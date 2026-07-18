# Java Fullstack Project Ideas

Use these projects to practice Spring Boot, React or Angular, PostgreSQL/SQL Server, Docker, and AWS. Each project is designed to create interview stories, not just code.

## 1. Product Catalog + Auth

**Stack:** Spring Boot, React or Angular, PostgreSQL, Flyway, Docker, AWS S3/CloudFront/ECS/RDS.

Build:

- register/login
- product CRUD
- owner-scoped product list
- category filter
- pagination/sorting/search
- Problem Details validation
- AWS deployment outline

Interview signals:

- REST design
- auth and ownership checks
- schema/index design
- frontend form validation
- deployment story

Advanced extensions:

- Cognito auth
- product image upload to S3
- admin dashboard
- audit events
- optimistic locking

## 2. Team Notes with Sharing

**Stack:** Spring Boot, Angular, PostgreSQL, Flyway, JWT, Docker.

Build:

- notes CRUD
- tags
- share note with another user
- roles: owner/editor/viewer
- full-text search
- archive/restore

Interview signals:

- many-to-many modeling
- authorization beyond simple roles
- SQL indexing for search/list
- Angular guards/interceptors/forms

Advanced extensions:

- offline draft support
- collaborative editing simulation
- audit trail
- export notes to PDF/object storage

## 3. Inventory Import Pipeline

**Stack:** Spring Boot, React, PostgreSQL or SQL Server, S3/MinIO, SSE, Docker.

Build:

- upload CSV
- validate rows
- create import job
- process asynchronously
- stream progress to SPA
- show row-level errors
- commit valid rows

Interview signals:

- background jobs
- idempotency
- file upload safety
- streaming progress
- transactional boundaries

Advanced extensions:

- SQS queue
- dead-letter queue
- retry policy
- presigned S3 uploads
- large-file chunking

## 4. E-Commerce Admin Console

**Stack:** Spring Boot, Angular, SQL Server, Flyway, AWS Elastic Beanstalk or ECS.

Build:

- product management
- inventory adjustments
- order lookup
- admin-only routes
- audit events
- dashboard metrics

Interview signals:

- enterprise SQL Server familiarity
- role-based access
- admin UX
- reporting queries
- operational logging

Advanced extensions:

- optimistic locking for inventory updates
- CQRS-style read models
- export reports
- CloudWatch dashboards

## 5. Customer Support Ticket System

**Stack:** Spring Boot, React, PostgreSQL, Docker, AWS.

Build:

- users create tickets
- agents assign/update status
- comments and attachments
- SLA due dates
- email notification stub
- dashboard filters

Interview signals:

- workflow/state transitions
- authorization by role and assignment
- indexes for queues
- frontend state management
- cloud deployment

Advanced extensions:

- event-driven notifications
- SQS background worker
- file attachments in S3
- audit log and timeline

## 6. Appointment Booking System

**Stack:** Spring Boot, Angular, PostgreSQL, Flyway, Docker.

Build:

- provider availability
- customer booking
- conflict prevention
- cancellation/reschedule
- email/SMS stub
- calendar view

Interview signals:

- concurrency
- transaction isolation
- unique constraints for time slots
- time zone handling
- Angular forms and route flows

Advanced extensions:

- waitlist
- recurring availability
- payment authorization stub
- iCalendar export

## 7. Personal Finance Tracker

**Stack:** Spring Boot, React, PostgreSQL, Docker, AWS.

Build:

- accounts
- transactions
- categories
- monthly budgets
- charts
- CSV import
- search/filter

Interview signals:

- aggregate queries
- data validation
- frontend chart state
- import pipeline
- privacy/security discussion

Advanced extensions:

- background categorization rules
- read model for monthly summaries
- encrypted sensitive fields
- scheduled reports

## 8. Multi-Tenant SaaS Starter

**Stack:** Spring Boot, React or Angular, PostgreSQL, Cognito optional, ECS/Fargate.

Build:

- organizations/tenants
- users belong to tenant
- role management
- tenant-scoped products/projects
- admin tenant switch
- invite flow stub

Interview signals:

- tenant isolation
- authorization model
- database indexing with tenant ID
- SaaS architecture
- secure deployment

Advanced extensions:

- subdomain routing
- per-tenant rate limits
- per-tenant audit logs
- billing plan model
- row-level security discussion

## 9. Product Image Manager

**Stack:** Spring Boot, React, PostgreSQL, S3/MinIO, CloudFront.

Build:

- upload product images
- store metadata
- generate presigned upload/download URLs
- validate file type/size
- display gallery
- delete/archive images

Interview signals:

- object storage
- presigned URL security
- metadata modeling
- CDN caching
- frontend upload progress

Advanced extensions:

- background thumbnail generation
- virus scan stub
- image moderation queue
- lifecycle policies

## 10. Real-Time Operations Dashboard

**Stack:** Spring Boot, Angular, PostgreSQL, SSE/WebSocket, Docker.

Build:

- metrics ingestion endpoint
- dashboard cards
- live event stream
- filters by service/environment
- incident notes

Interview signals:

- streaming trade-offs
- backpressure
- dashboard UX
- time-series-ish queries
- observability mindset

Advanced extensions:

- WebSocket rooms
- Redis pub/sub
- retention policy
- alert rules

## 11. Restaurant Ordering System

**Stack:** Spring Boot, React, PostgreSQL, Docker, AWS.

Build:

- menu management
- cart
- checkout stub
- kitchen order queue
- order status updates
- admin menu edits

Interview signals:

- domain modeling
- state transitions
- transactional checkout
- frontend route/state design
- deployment and scaling discussion

Advanced extensions:

- payment provider integration stub
- receipt emails
- kitchen SSE updates
- inventory deduction

## 12. Developer Portal for Internal APIs

**Stack:** Spring Boot, Angular, PostgreSQL, S3, Cognito.

Build:

- API catalog
- documentation pages
- team ownership
- access request workflow
- favorites
- search

Interview signals:

- enterprise app design
- auth groups/roles
- workflow state
- search/indexing
- AWS hosting

Advanced extensions:

- markdown docs stored in S3
- approval notifications
- OpenAPI upload/preview
- audit trail

## How to choose a project

| Goal | Pick |
|---|---|
| First Java fullstack portfolio | Product Catalog + Auth |
| Angular-heavy role | Team Notes or E-Commerce Admin |
| React-heavy role | Product Catalog, Finance Tracker, Image Manager |
| Database interview prep | Appointment Booking or Inventory Import |
| AWS/cloud prep | Multi-Tenant SaaS or Image Manager |
| Senior system design prep | Multi-Tenant SaaS or Support Ticket System |

## Project README requirements

Every project should include:

- architecture diagram
- tech stack
- local setup
- environment variables
- API endpoints
- database schema and indexes
- auth model
- screenshots or demo script
- tests
- deployment outline
- trade-offs
- known limitations
- next improvements

## Minimum production-hardening list

- DTOs, validation, and Problem Details
- authentication and authorization
- owner/tenant scoping
- Flyway migrations
- database constraints and indexes
- structured logging with correlation IDs
- Docker Compose local setup
- frontend loading/error/empty states
- tests for risky behavior
- no committed secrets
- AWS deployment plan
