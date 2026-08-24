# Event Ticketing System

A RESTful .NET8 API for event management, ticket purchasing, and sales
reporting, built around one invariant: the system must never oversell
event inventory. That's enforced through domain validation, optimistic
concurrency, idempotent purchases, and database constraints — verified
under real concurrent load in the integration tests, not just asserted.

## Requirements coverage

| Requirement | Implementation |
|-------------|----------------|
| Create/retrieve/update/delete events | `POST/GET/PUT/DELETE /api/events` |
| Event fields						   | Name, description, venue, date, time, total capacity, pricing tiers |
| Purchase tickets					   | `POST /api/events/{eventId}/tickets` |
| View availability					   | `GET /api/events/{eventId}/tickets/availability` |
| Prevent overselling				   | Domain rules + optimistic concurrency (below) |
| Duplicate-purchase protection		   | Idempotency key + request fingerprint + unique DB constraint |
| Sales reporting					   | Ticket sales summary by event |
| Validation & error handling		   | Request validation + domain validation → RFC 7807 responses |
| Testing							    | Domain, application, and concurrency integration tests |

## Architecture


The application is a modular monolith using:

- API — REST controllers and HTTP concerns
- Application — CQRS commands, queries, handlers and validation
- Domain — Event aggregate and business rules
- Infrastructure — EF Core and SQL Server

The Event owns the ticket inventory invariant:

> An event must never be oversold.

The application uses `IApplicationDbContext` rather than injecting `DbContext` into controllers. This keeps controllers thin and separates HTTP, application and persistence concerns.

```
API (controllers) → Application (commands/queries/handlers) → Domain (Event, PricingTier, TicketPurchase) → Infrastructure (EF Core) → SQL Server
```

Controllers hold no business logic — they dispatch commands/queries through
a small mediator. `Event` is the aggregate that owns inventory: capacity,
sold count, and pricing tiers all live on it, and `PurchaseTickets(...)` is
the only way `SoldTickets` changes — application code can't manipulate
inventory directly. This is deliberately lightweight DDD: one real
invariant worth protecting with an aggregate, not domain events,
per-entity repositories, or event sourcing, which the requirements don't
call for.

A single monolith over microservices because the one operation that
matters most — purchasing a ticket — needs one transaction across
inventory and the purchase record; splitting that across services would
trade a real consistency requirement for operational complexity nothing in
the spec asks for. The layers are still cleanly bounded if that ever
changes.

## Data model

`Event` → `PricingTiers`, `TicketPurchases`. `Event` holds
`TotalCapacity`/`SoldTickets` (remaining is derived, never stored);
`PricingTier` is just a name and price, unique per event; `TicketPurchase`
snapshots `UnitPrice` at purchase time (so a later price change never
rewrites a completed purchase's history) and carries `IdempotencyKey` +
`RequestFingerprint`.

## Preventing overselling

A read-then-write check isn't safe under concurrency — two requests can
both read "1 remaining" and both proceed. `Event.Version` (an EF Core
optimistic-concurrency token) makes the database the authority instead:
both requests read `Version = 7`; the winner's write advances it to `8` and
commits; the loser's write, still targeting `Version = 7`, is rejected with
`DbUpdateConcurrencyException`. It reloads, sees `Remaining = 0`, and is
correctly refused. No lock is ever held across a round-trip, so this holds
even with multiple API instances running behind a load balancer.

## Preventing duplicate purchases

Every purchase requires an `Idempotency-Key` header. Same key + same
request → the original purchase is returned, nothing new is created. Same
key + a *different request → `409 Conflict` (a request fingerprint —
event, tier, purchaser, quantity — is checked on replay). The actual
enforcement point is a unique database constraint on the key, not just an
application-level check, since two truly simultaneous requests can race
past a check-then-insert.

## API reference

```
POST   /api/events
GET    /api/events
GET    /api/events/{eventId}
PUT    /api/events/{eventId}
DELETE /api/events/{eventId}
POST   /api/events/{eventId}/tickets                  (Idempotency-Key required)
GET    /api/events/{eventId}/tickets/availability
GET    /api/reports/events/{eventId}/sales
```

## Status codes:
`400` validation · `404` not found · `409` inventory/concurrency/idempotency
conflict · `500` unexpected (no internal detail leaked).

## Testing


```markdown
Unit tests cover:

- Domain business rules
- Validation
- Event operations
- Ticket purchasing
- Inventory rules

Integration tests cover the behaviours that require the real database:

- API wiring
- Persistence
- Idempotency
- Optimistic concurrency
- Database constraints

The integration tests use SQL Server rather than EF Core InMemory because concurrency and database constraints are part of the application's correctness model.
```

## Trade-offs

- No repository layer — handlers depend on `IApplicationDbContext`
  directly; EF Core's `DbSet<T>` already is the repository.
- SQL Server, not NoSQL — the purchase path needs one transaction
  across an inventory update and a purchase insert, plus real unique
  constraints; that's a relational database's job.
- Optimistic concurrency, not locking — no reader ever blocks; a
  losing writer retries instead of queuing behind a held lock.
- Idempotency key + fingerprint, not key alone — a key alone doesn't
  stop someone reusing it for a genuinely different request.
- No reservation/hold step — the spec asks for purchases, not holds;
  a hold model needs its own lifecycle and expiry, out of scope here.
- No Redis, no Kafka/RabbitMQ — nothing here needs caching,
  distributed coordination, or an async workflow; adding either to the
  purchase path without a real requirement is unjustified risk.
- Deletion blocked once tickets are sold — sold tickets are a
  financial record; the spec doesn't say, but silently destroying that
  history isn't a reasonable default.

  ## Docker

Docker is used to provide a reproducible SQL Server environment for integration tests.

Start:

```bash
docker compose up -d
```

## Setup

```markdown
## Setup

### Prerequisites

- .NET SDK
- Docker Desktop
- Git

### Run

```bash
git clone https://github.com/Johnobasi/TicketSystem
cd TicketSystem

docker compose up -d

dotnet restore
dotnet build
dotnet test
```

Swagger is available at the URL printed on startup.

```bash
dotnet test                    # domain + integration (requires Docker for SQL Server)
```

## Not built, on purpose

Auth, payments, reservation/hold expiry, refunds, caching, async
notifications, rate limiting, audit history — none required by the spec;
listed here as known scope, not oversights.

## AI Declaration
5-10% of the entire codebase was assisted by chapGPT especially putting togther the ReadMe.md file, and some part of the integration tests. 
All generated suggestions were reviewed, tested, and adapted by me.
I remain responsible for the final architecture, implementation, code, tests, and design decisions submitted with this project.