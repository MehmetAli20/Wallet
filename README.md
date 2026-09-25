# Wallet

A Splitwise-style shared expense tracker built on .NET 10 and Clean Architecture, backed by a double-entry obligations ledger that records who owes whom and never holds money.

![CI](https://github.com/MehmetAli20/wallet/actions/workflows/ci.yml/badge.svg)

## What this is

Wallet keeps track of shared costs between people — a trip, a flatshare, a dinner, or a
one-to-one debt between two friends — and tells everyone where they stand.

It is an **obligations ledger, not a wallet in the custodial sense**. The application never
holds, moves or touches real money. It records claims between people; the actual payment
happens outside the system, and users tell Wallet that it happened. This is a deliberate
design decision: holding user funds would put the project squarely inside e-money and
payment-institution regulation, and nothing about splitting a restaurant bill needs it.

## How the ledger works

Every obligation is written as **two mirrored ledger entries**, and every entry carries both
the context it belongs to (`GroupId`) and the person on the other side (`CounterpartyId`).

Ayşe pays 90 ₺ for a dinner shared by Ayşe, Burak and Cem:

| Account | Type   | Amount | Group  | Counterparty |
|---------|--------|--------|--------|--------------|
| Burak   | Debit  | 30 ₺   | Dinner | Ayşe         |
| Ayşe    | Credit | 30 ₺   | Dinner | Burak        |
| Cem     | Debit  | 30 ₺   | Dinner | Ayşe         |
| Ayşe    | Credit | 30 ₺   | Dinner | Cem          |

The payer's own share is never posted — you do not owe yourself. Because each side of an
obligation is written twice, the ledger satisfies three invariants that are checked
continuously: every account's stored balance equals the sum of its entries, every currency
nets to zero across the system, and **every group nets to zero on its own**. That last one is
what catches a half-written obligation that a global check would happily hide.

Carrying the counterparty on each entry is what makes "why do I owe this?" answerable. A bare
running balance cannot answer it.

## Features

**Groups and people**
- Named groups (trips, flatshares) and lightweight pairs for one-to-one debts
- Invitations that record who invited whom, with accept and decline
- Member roles and statuses; the last admin cannot be removed
- Placeholder members for people who have not signed up yet, with single-use, expiring claim
  tokens so they can take over their own history when they join

**Expenses**
- Equal splits, fixed per-person shares, or a mix — fixed shares are honoured and the
  remainder is split equally among the rest
- Deterministic largest-remainder rounding, so shares always sum back to the exact total
  down to the last cent
- Corrections by **reversal and revision**, never by mutating history
- Recurring expenses, posted automatically by a background worker

**Balances and settling up**
- Per-group balances showing each member's net position
- Optional debt simplification (`?simplify=true`) that collapses a tangle of debts into the
  fewest payments — computed per request as a **display preference**, never stored, so the
  underlying history stays intact and auditable
- Settlements to record that a real-world payment happened
- Group activity feed with per-user unread tracking

**Operations**
- Reconciliation jobs that verify all four ledger invariants against the live database
- Health checks, structured logging, OpenAPI/Swagger in development

## Security

Authorization is layered rather than sprinkled over controllers:

- **Deny by default.** A fallback authorization policy means every endpoint requires a valid
  token unless it explicitly opts out; `/health` and the auth endpoints are the only ones
  that do.
- **Row scoping in the data layer.** EF Core global query filters restrict accounts, ledger
  entries, groups and expenses to what the current user is entitled to see. A cross-tenant
  read returns 404 rather than 403, so the API does not leak the existence of other people's
  data. Bypassing a filter requires a deliberate, greppable `IgnoreQueryFilters()`.
- **JWT bearer tokens** with issuer, audience, lifetime and signature validation, and BCrypt
  password hashing.
- **Login lockout** after 5 failed attempts within a rolling window, for 15 minutes.
- **Distributed rate limiting** on Redis (token bucket), with separate policies for anonymous
  traffic, registration, authenticated reads and authenticated writes — so counters hold
  across multiple API instances rather than per process.
- **Idempotency keys** on state-changing commands, so a retried request cannot post the same
  expense or settlement twice.
- **Forwarded headers** are opt-in and bounded by an explicit proxy allow-list, so a client
  cannot spoof its own IP past the rate limiter.

## Architecture

```
src/
  Wallet.Domain          entities, value objects, invariants — no dependencies
  Wallet.Application     use cases (MediatR), validation, ports
  Wallet.Infrastructure  EF Core, PostgreSQL, Redis, JWT, repositories
  Wallet.Api             HTTP layer: controllers, contracts, middleware
  Wallet.Worker          background jobs: reconciliation, recurring expenses, pruning
tests/
  Wallet.UnitTests         domain rules and use-case logic
  Wallet.IntegrationTests  real PostgreSQL via Testcontainers, plus end-to-end HTTP tests
  Wallet.ArchitectureTests dependency rules enforced with NetArchTest
```

Dependencies point inward only, and that is enforced by a test rather than by convention.
Cross-cutting concerns live in MediatR pipeline behaviours: validation, idempotency, logging
and retry.

## Tech stack

.NET 10 · ASP.NET Core · EF Core 10 · PostgreSQL 17 · Redis 7 · MediatR · FluentValidation ·
BCrypt · xUnit · FluentAssertions · Testcontainers · NetArchTest · Docker · Kubernetes ·
Skaffold · GitHub Actions

## Getting started

**Requirements:** .NET 10 SDK and Docker.

Create a `.env` file in the repository root:

```
POSTGRES_USER=wallet
POSTGRES_PASSWORD=<choose one>
POSTGRES_DB=wallet
JWT_SIGNING_KEY=<at least 32 characters>
```

Start everything:

```bash
docker compose up --build
```

Apply the database schema (the application does not migrate on startup, on purpose):

```bash
dotnet ef database update --project src/Wallet.Infrastructure --startup-project src/Wallet.Api
```

The API listens on `http://localhost:8080`. In development, Swagger is at `/swagger`.

Register, log in, and paste the returned token into Swagger's **Authorize** box:

```bash
curl -X POST http://localhost:8080/api/v1/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"ayse@example.com","password":"password123","displayName":"Ayşe"}'
```

## Running the tests

Docker must be running — the integration tests start a real PostgreSQL container and the
endpoint tests boot the full API against it.

```bash
dotnet test Wallet.slnx
```

366 tests: 220 unit, 143 integration, 3 architecture.

## API overview

| Area | Endpoints |
|---|---|
| Auth | `POST /api/v1/auth/register`, `POST /api/v1/auth/login` (bearer), `POST`/`DELETE /api/v1/auth/session`, `POST /api/v1/auth/session/refresh` (browser session) |
| Groups | `POST /api/v1/groups`, `GET /api/v1/groups`, `POST /api/v1/groups/pairs`, `POST /api/v1/groups/{id}/members`, `GET /api/v1/groups/{id}/balance` |
| Invitations | `GET /api/v1/invitations`, `POST /api/v1/invitations/{id}/accept`, `DELETE /api/v1/invitations/{id}` |
| Expenses | `POST /api/v1/expenses`, `GET /api/v1/groups/{id}/expenses`, `POST /api/v1/expenses/{id}/reversal`, `POST /api/v1/expenses/{id}/revisions` |
| Recurring | `POST /api/v1/recurringexpenses`, `GET /api/v1/recurringexpenses`, `DELETE /api/v1/recurringexpenses/{id}` |
| Settling up | `POST /api/v1/transfers` |
| Activity | `GET /api/v1/groups/{id}/activity`, `POST /api/v1/groups/{id}/activity/seen`, `GET /api/v1/groups/activity/unread` |
| Placeholders | `POST /api/v1/groups/{id}/placeholders`, `POST /api/v1/placeholders/{id}/claim-token`, `POST /api/v1/placeholders/claim` |
| Accounts | `GET /api/v1/accounts`, `GET /api/v1/accounts/{id}`, `GET /api/v1/accounts/{id}/entries` |

## Project status

The backend is functional and covered by tests, but this is a work in progress and not yet
running in production. Known gaps:

- No client application — the API is the whole product today
- Notifications are recorded as activity but not yet delivered by email or push
- PostgreSQL row-level security is planned as defence-in-depth behind the EF query filters
- Access tokens only; no refresh-token flow yet
- One currency per group; multi-currency groups are out of scope for now

## Deployment

Kubernetes manifests live in `k8s/` (API, worker, PostgreSQL StatefulSet, Redis, services and
secrets), with `skaffold.yaml` for local cluster development. Secrets are not committed —
`k8s/*-secret.yaml` and `.env` are gitignored, and you supply your own.
