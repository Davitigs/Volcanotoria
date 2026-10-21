# Learning Tour

Append-only reference for reviewing the project in interview-sized pieces.

## Phase 1 — ASP.NET Core, EF Core, PostGIS

### `HazardDashboard.Api/HazardDashboard.Api.csproj`

- **Does:** Targets .NET 10 and declares EF Core, Npgsql, NetTopologySuite, Hangfire, and FluentValidation dependencies.
- **Why:** Package versions are explicit and the API/runtime target stays aligned with Docker and design-time tooling.
- **Equivalent:** `package.json`; NuGet resolves transitive dependencies through the project file rather than npm scripts alone.
- **Interview:** Explain target framework vs package version; explain why EF Core provider packages must match the EF major version.

### `HazardDashboard.Api/Domain/Volcano.cs` and `Domain/SeismicEvent.cs`

- **Does:** Defines the persistence/domain entities, including spatial `Point` values and the USGS event natural key.
- **Why:** The USGS ID is the seismic primary key, making idempotent upserts straightforward; spatial values stay strongly typed.
- **Equivalent:** Prisma schema models plus TypeScript interfaces; C# classes also participate in EF change tracking.
- **Interview:** Explain natural-key deduplication and why longitude/latitude order matters when creating a point.

### `HazardDashboard.Api/Data/HazardDbContext.cs`

- **Does:** Configures EF Core mappings, PostGIS, geography columns, enum conversion, and GiST indexes.
- **Why:** `geography(Point, 4326)` makes earth-distance queries operate in metres; GiST makes spatial filtering indexable.
- **Equivalent:** Prisma schema field definitions and migrations; provider-specific spatial configuration is more explicit in EF Core.
- **Interview:** Explain `DbContext` as a scoped unit of work and how LINQ becomes provider-translated SQL/PostGIS.

### `HazardDashboard.Api/Data/Migrations/*InitialCreate*`

- **Does:** Creates the volcano/seismic tables, PostGIS extension annotation, spatial columns, and indexes.
- **Why:** Migrations are versioned application code that can be reviewed and applied consistently in clean environments.
- **Equivalent:** Prisma migration folders containing SQL; EF stores generated C# operations and a model snapshot.
- **Interview:** Explain model snapshot drift and why migrations should be generated from the model, not hand-edited casually.

### `HazardDashboard.Api/Data/volcanoes.json` and `Data/DatabaseInitializer.cs`

- **Does:** Stores the static volcano catalog and applies the migration plus seed records on API startup.
- **Why:** GVP is treated as a static seed source; startup initialization makes the Docker demo self-contained.
- **Equivalent:** Prisma seed script; this initializer also runs EF migration execution and creates NTS points.
- **Interview:** Explain idempotent seeding and the trade-off between startup migrations and a separate deployment migration step.

### `HazardDashboard.Api/Dockerfile` and `docker-compose.yml`

- **Does:** Runs PostGIS, the .NET API, and the Angular/Nginx web app with health-gated startup.
- **Why:** Compose gives reviewers one reproducible command; the API waits for database health before applying migrations.
- **Equivalent:** Docker Compose around NestJS, PostgreSQL, and a worker; ASP.NET’s hosted services run inside the API process here.
- **Interview:** Explain multi-stage builds, service DNS (`postgres`), and why `depends_on` needs a healthcheck for readiness.

## Phase 2 — Hangfire and USGS ingestion

### `HazardDashboard.Api/Jobs/UsGeologicalSurveyIngestionJob.cs`

- **Does:** Polls the USGS GeoJSON feed, deliberately maps fields, and inserts or updates seismic events by ID.
- **Why:** Explicit mapping prevents feed shape changes or coordinate-order mistakes from silently corrupting domain data.
- **Equivalent:** A BullMQ processor with a repeatable job and Prisma upsert; Hangfire combines scheduling, persistence, retries, and execution in ASP.NET.
- **Interview:** Explain idempotent ingestion, `FindAsync`/change tracking, UTC conversion, and why feed failures are logged and skipped.

### `HazardDashboard.Api/Program.cs` Hangfire registration

- **Does:** Configures PostgreSQL storage, starts a Hangfire server, and registers the ten-minute recurring job.
- **Why:** The scheduler is declarative and persisted, so restarts do not lose the recurring-job definition.
- **Equivalent:** BullMQ repeat options plus a separately running Worker; Hangfire can host worker and API together for this demo.
- **Interview:** Explain recurring scheduling vs queue execution and where production authorization belongs on `/hangfire`.

## Phase 3 — Controllers, validation, and spatial API

### `HazardDashboard.Api/Controllers/*.cs`

- **Does:** Exposes volcano, seismic-event, nearby-event, and hazard-score endpoints with typed projections.
- **Why:** Controllers make the HTTP boundary easy to walkthrough, while projections avoid returning tracked entities or internal fields.
- **Equivalent:** NestJS controllers and DTOs; ASP.NET model binding and LINQ replace decorators plus Prisma query calls.
- **Interview:** Explain `AsNoTracking`, cancellation tokens, and how LINQ `IsWithinDistance` becomes a PostGIS distance predicate.

### `HazardDashboard.Api/Contracts/Queries.cs` and `Validation/QueryValidators.cs`

- **Does:** Models and validates bounding boxes, time/magnitude filters, and nearby-event coordinates.
- **Why:** Validation is isolated from controllers, reusable through DI, and produces consistent ProblemDetails errors.
- **Equivalent:** NestJS `class-validator` DTOs and pipes; FluentValidation expresses rules in C# validators.
- **Interview:** Explain why latitude/longitude bounds are validated before spatial SQL and how ProblemDetails improves client handling.

## Phase 4 — SignalR real-time updates

### `HazardDashboard.Api/Hubs/HazardHub.cs`

- **Does:** Defines the SignalR hub endpoint and connection lifecycle extension point.
- **Why:** A hub centralizes connection semantics while leaving room for geographic groups later.
- **Equivalent:** NestJS Gateway or Socket.IO namespace; SignalR supplies negotiation, transports, and connection management.
- **Interview:** Explain hubs, connection IDs, groups, and when `IHubContext` is needed from a background job.

### SignalR broadcast in `UsGeologicalSurveyIngestionJob.cs`

- **Does:** Sends only newly inserted events as `NewSeismicEvents` after the database save succeeds.
- **Why:** Post-commit broadcast avoids telling clients about records that failed to persist and avoids resending updates.
- **Equivalent:** Socket.IO `server.emit` from a BullMQ worker; SignalR injects a typed framework context into the job.
- **Interview:** Explain persistence-before-publish ordering and the limits of in-process broadcasts in a multi-instance deployment.

## Phase 5 — Angular dashboard

### `HazardDashboard.Web/src/app/api.service.ts` and `models.ts`

- **Does:** Defines typed frontend contracts and wraps HTTP calls for volcanoes, seismic events, and scores.
- **Why:** API access is centralized and typed, keeping components focused on state and presentation.
- **Equivalent:** Angular service plus TypeScript interfaces; similar to a NestJS client service, but consumed by standalone components.
- **Interview:** Explain typed DTO boundaries and why the frontend should not duplicate hazard calculation logic.

### `src/app/signalr.service.ts`

- **Does:** Owns the SignalR connection, reconnect behavior, status signals, and incremental event stream.
- **Why:** Connection lifecycle is isolated from the map, making reconnection and error handling testable and reusable.
- **Equivalent:** A Socket.IO client service using RxJS subjects; Angular signals provide local reactive state without a store library.
- **Interview:** Explain automatic reconnect and why new events are merged by ID instead of triggering a full refetch.

### `src/app/map.component.ts` and `marker-layer.service.ts`

- **Does:** Renders Leaflet layers and creates volcano/seismic markers with visual encodings.
- **Why:** Leaflet API details stay out of the root component; marker styling is a separate domain-to-visual mapping.
- **Equivalent:** Angular component plus a map utility service; React would commonly use effects and refs for the imperative map object.
- **Interview:** Explain integrating an imperative library with signals and why shallow seismic events use a distinct color.

### `src/app/detail-panel.component.ts` and `app.ts`

- **Does:** Shows selected-volcano details, score state, filters, loading/error state, and live event merging.
- **Why:** The root component coordinates page state while the detail panel remains presentational and focused.
- **Equivalent:** Angular standalone components and signals replace an NgRx/store slice or RxJS-heavy container pattern.
- **Interview:** Explain signal-derived state, optimistic UI boundaries, and production-shaped loading/error handling.

## Phase 6 — Documentation and local deployment

### `README.md`

- **Does:** Documents purpose, architecture, run instructions, URLs, screenshot placeholders, and the scientific disclaimer.
- **Why:** A portfolio reviewer should understand the project and run it without reverse-engineering the repository.
- **Equivalent:** Project README for any TypeScript/NestJS portfolio; the architecture highlights ASP.NET-specific infrastructure.
- **Interview:** Explain the end-to-end data flow from USGS through Hangfire/PostGIS to REST and SignalR clients.

### `HazardDashboard.Web/Dockerfile`, `nginx.conf`, and `.dockerignore` files

- **Does:** Builds the Angular bundle in Node and serves it from Nginx with SPA fallback; excludes generated dependencies/artifacts.
- **Why:** Runtime images stay small and browser refreshes work on client-side routes; clean contexts improve reproducibility.
- **Equivalent:** Angular/Nginx deployment is the same shape as a NestJS frontend; the API uses .NET SDK/runtime stages instead of Node.
- **Interview:** Explain build-time vs runtime containers, SPA fallback routing, and why lockfile/container package installation must be reproducible.

## Phase 7 — DI lifetime compatibility fix

### `HazardDashboard.Api/Program.cs` EF Core registrations

- **Does:** Registers one `IDbContextFactory<HazardDbContext>` and exposes a request-scoped context from it.
- **Why:** Mixing `AddDbContext` scoped options with `AddPooledDbContextFactory` caused a singleton pool to consume scoped options during container validation.
- **Equivalent:** Prisma commonly shares one client; here the factory creates safe short-lived contexts for both HTTP requests and Hangfire jobs.
- **Interview:** Explain why a singleton cannot depend on a scoped service and why a context factory is useful for background work.

## Phase 8 — Container startup seed fix

### `HazardDashboard.Api/Data/DatabaseInitializer.cs` JSON options

- **Does:** Deserializes the camelCase seed JSON into the C# seed record before creating database entities.
- **Why:** `System.Text.Json` property matching is case-sensitive by default; without explicit options, valid JSON silently produced default/null values and violated database constraints.
- **Equivalent:** TypeScript JSON parsing generally preserves object keys directly; C# deserialization needs explicit naming-policy or case-insensitive configuration.
- **Interview:** Explain why database constraints caught the bug and why input deserialization should validate or reject incomplete records before persistence.

## Phase 9 — PostGIS geography response projection fix

### `HazardDashboard.Api/Controllers/VolcanoesController.cs` and `SeismicEventsController.cs`

- **Does:** Keeps `ST_DWithin` distance filtering in PostgreSQL, then reads point coordinates after materialization for JSON DTOs.
- **Why:** `ST_X`/`ST_Y` are geometry functions; directly projecting `Point.X`/`Point.Y` from a geography column generated invalid SQL in this provider combination.
- **Equivalent:** Prisma/PostGIS code would use an explicit geometry cast or raw SQL; here EF translation boundaries are handled deliberately.
- **Interview:** Explain the difference between database-side spatial filtering and in-memory DTO projection, and why provider translation must be verified against generated SQL.

## Phase 10 — Seed and API contract hardening

### `HazardDashboard.Api/Data/DatabaseInitializer.cs` seed transaction

- **Does:** Wraps the initial volcano insert in a database transaction so a failed batch cannot leave partial seed data.
- **Why:** Startup seeding must be atomic; otherwise a later restart may mistake incomplete data for a successful seed.
- **Equivalent:** A Prisma seed script wrapped in an interactive transaction; EF uses `BeginTransactionAsync` explicitly.
- **Interview:** Explain atomicity and why persistent Docker volumes can preserve failed intermediate state.

### `HazardDashboard.Api/Controllers/VolcanoesController.cs` DTO status

- **Does:** Converts the C# enum to its string name in the JSON response.
- **Why:** The frontend contract expects semantic values such as `Active`, not implementation values such as enum integer `0`.
- **Equivalent:** A TypeScript enum/string DTO already serializes according to its chosen representation; C# enum JSON behavior must be configured or mapped.
- **Interview:** Explain why API DTOs should be deliberate contracts rather than exposing persistence types directly.

## Phase 12 — Angular readability refactor

### `HazardDashboard.Web/src/app/app.ts`

- **Does:** Coordinates page-level signals, API loading, volcano selection, score loading, and live-event merging.
- **Why:** Expanded formatting makes state ownership and asynchronous transitions visible instead of hiding them in one-line statements.
- **Equivalent:** An Angular container component resembles a React page component or a NestJS-facing client store; signals replace a larger state library here.
- **Interview:** Explain why page orchestration belongs in a container and why live events are merged by ID.

### `HazardDashboard.Web/src/app/detail-panel.component.ts`, `.html`, and `.scss`

- **Does:** Separates detail-panel behavior, markup, and styling into focused files.
- **Why:** The panel is large enough that an inline template reduces readability; external templates are easier to review and maintain.
- **Equivalent:** Similar to Angular template/style separation or a React component with a separate CSS module; standalone imports remain local to the component.
- **Interview:** Explain Angular inputs/outputs and why a presentational component should not own API calls.

### `HazardDashboard.Web/src/app/map.component.ts`, `marker-layer.service.ts`, `api.service.ts`, `signalr.service.ts`, and `models.ts`

- **Does:** Formats the remaining map, service, SignalR, API, and typed-model files into readable multi-line TypeScript.
- **Why:** Consistent formatting exposes state, lifecycle, mapping, and error-handling boundaries without changing runtime behavior.
- **Equivalent:** These are Angular services/interfaces comparable to TypeScript client services and DTO types; the main difference is Angular DI and signal-based state.
- **Interview:** Explain why the map component owns Leaflet lifecycle while the marker service owns visual mapping and the API/SignalR services own transport concerns.

## Phase 11 — Database startup readiness

### `HazardDashboard.Api/Data/DatabaseInitializer.cs` migration retry loop

- **Does:** Retries EF migration startup when PostgreSQL briefly refuses connections.
- **Why:** Container start order and database readiness are different states; a healthy container can still have a short connection race.
- **Equivalent:** A NestJS startup retry around Prisma connection/migration initialization; the principle is the same even though the APIs differ.
- **Interview:** Explain why `depends_on` is not a complete application-readiness guarantee and why startup retries should be bounded.

## Phase 13 — Angular folder organization


### `HazardDashboard.Web/src/app/components/map/map.component.{ts,html,scss}` and `components/detail-panel/detail-panel.component.{ts,html,scss}`

- **Does:** Groups each component with its own template and styles; the map now also uses external files.
- **Why:** Co-locating the three files makes each UI component easy to find and review.
- **Equivalent:** Angular components own browser UI; a NestJS controller instead owns an HTTP boundary.
- **Interview:** Explain component inputs/outputs and why Leaflet setup and cleanup belong to the map component.

### `HazardDashboard.Web/src/app/services/api.service.ts`, `services/signalr.service.ts`, and `services/marker-layer.service.ts`

- **Does:** Groups injectable services for REST access, live connections, and marker creation.
- **Why:** Separates transport and rendering helpers from component markup; existing DI registrations still apply after moving files.
- **Equivalent:** NestJS injectable providers, with Angular root services typically shared across the browser application.
- **Interview:** Explain how imports differ from dependency injection and how services keep components focused.

### `HazardDashboard.Web/src/app/models/volcano.ts`, `models/seismic-event.ts`, and `models/hazard-score.ts`

- **Does:** Gives each API response interface its own file; volcano status stays with the volcano model.
- **Why:** Explicit model imports make dependencies visible and remove the catch-all models file.
- **Equivalent:** TypeScript DTO interfaces; unlike EF entities, these interfaces disappear at runtime.
- **Interview:** Explain why an HttpClient generic provides compile-time typing rather than runtime response validation.

### `HazardDashboard.Web/src/app/app.ts`

- **Does:** Imports the reorganized components, services, and models.
- **Why:** Keeps application entry files at the root while their supporting roles live in dedicated folders.
- **Equivalent:** Application composition in Angular; NestJS typically expresses provider/controller composition in modules.
- **Interview:** Explain standalone component imports versus injectable service resolution.

## Phase 14 — Angular rules audit and Nx architecture migration

### `HazardDashboard.Web/ANGULAR_AUDIT.md`

- **Does:** Maps the existing frontend violations to the new engineering rules and records unfinished work.
- **Why:** A folder migration alone cannot establish compliance; the audit distinguishes verified structure from remaining behavior and testing changes.
- **Equivalent:** An engineering debt checklist for a NestJS module migration.
- **Interview:** Explain why module boundaries do not automatically enforce state ownership or runtime correctness.

### `HazardDashboard.Web/nx.json`, `eslint.config.mjs`, `apps/dashboard/project.json`, and `libs/**/project.json`

- **Does:** Defines Nx projects, tasks and tagged import constraints, including forbidding UI → data-access.
- **Why:** Architecture now has an executable check instead of depending on folder naming alone.
- **Equivalent:** Nx tags work similarly in a NestJS workspace; Nest modules themselves do not enforce source import direction.
- **Interview:** Explain public library APIs and how lint detects an invalid dependency.

### `HazardDashboard.Web/apps/dashboard/src/app/app.ts`, `app.routes.ts`, and `app.config.ts`

- **Does:** Leaves a thin OnPush router-outlet shell and lazy-loads the dashboard.
- **Why:** Page composition belongs in a feature library, leaving bootstrap and providers in the application.
- **Equivalent:** A NestJS composition root wires modules; Angular additionally lazy-loads browser route code.
- **Interview:** Explain lazy route loading and application versus feature responsibilities.

### `HazardDashboard.Web/libs/hazard/feature-dashboard/dashboard/dashboard.component.*` and `hazard.routes.ts`

- **Does:** Houses the existing dashboard page and route behind a public library entry point.
- **Why:** Establishes the feature boundary before extracting facade/store behavior; subscriptions are still an explicitly recorded follow-up.
- **Equivalent:** A feature module boundary in NestJS, but standalone Angular routes need no NgModule.
- **Interview:** Explain why relocation is distinct from separating business logic and state.

### `HazardDashboard.Web/libs/hazard/ui-map/` and `ui-detail/`

- **Does:** Reuses the map and detail components as OnPush presentation libraries; map marker service stays with Leaflet UI.
- **Why:** Dedicated UI services may render DOM, but these libraries cannot import transport services.
- **Equivalent:** Presentation adapters consuming typed inputs rather than fetching data themselves.
- **Interview:** Explain UI-only service ownership and the input/output contract.

### `HazardDashboard.Web/libs/hazard/data-access/` and `libs/shared/contracts/`

- **Does:** Relocates REST/SignalR services and centralizes their existing response interfaces.
- **Why:** Establishes public import boundaries. Backend contract generation and service lifecycle improvements remain outstanding.
- **Equivalent:** NestJS injectable transport providers and TypeScript DTO definitions; C# contracts require generation to prevent drift.
- **Interview:** Explain why handwritten frontend interfaces are not runtime validation or a shared backend source of truth.

### `HazardDashboard.Web/angular.json`, `tsconfig*.json`, `package.json`, `package-lock.json`, and ignore files

- **Does:** Updates application paths, library aliases, Nx scripts and dependency metadata while preserving Docker's build output directory.
- **Why:** Existing Compose deployment can keep using the web folder as its build context.
- **Equivalent:** TypeScript aliases and workspace task configuration in a NestJS monorepo.
- **Interview:** Explain the difference between an import alias and a runtime dependency-injection registration.

### Phase 14 correction — Nx is optional

The user clarified this during the phase: Nx is not required. The provisional Nx setup above was removed before handoff; those Nx paths and checks are historical, not the final implementation.

- **Files:** `HazardDashboard.Web/src/app/features/hazard/{feature-dashboard,ui-map,ui-detail,data-access}/`, `src/app/shared/contracts/`, `src/app/app.*`.
- **Does:** Keeps the same role separation and lazy OnPush shell using Angular CLI.
- **Why:** Architectural separation does not require adding a monorepo tool. Public aliases and ESLint restricted imports provide scoped dependency checks; they are not a full circular-dependency graph check.
- **Equivalent:** NestJS feature folders can likewise be separated without Nx.
- **Interview:** Explain dependency direction independently of build tooling; distinguish static import boundaries from DI lifetimes.

- **Files:** `HazardDashboard.Web/{angular.json,tsconfig*.json,package.json,package-lock.json,eslint.config.mjs,ANGULAR_AUDIT.md}`.
- **Does:** Restores Angular CLI commands, removes Nx packages/project configuration and documents outstanding compliance work.
- **Why:** Keeps the existing development and Docker workflow while making remaining violations explicit.
- **Equivalent:** ESLint import restrictions and TypeScript paths apply equally to NestJS.
- **Interview:** Explain why successful lint under a limited rule set is not evidence of full policy compliance.

### Phase 15 — Remove obsolete duplicate Angular files

- **Files removed:** `HazardDashboard.Web/src/app/app 2.ts`, `app.config 2.ts`, and `app.routes 2.ts`.
- **Does:** Removes an outdated dashboard copy, duplicate provider configuration, and unused empty routes. The active shell, config, routes, and feature dashboard remain unchanged.
- **Why:** No imports reference these copies and no unique behavior needs preserving. The TypeScript `src/**/*.ts` include can still check unimported files, so stale copies with broken imports should not remain in source.
- **Equivalent:** The same distinction applies in NestJS/TypeScript: runtime imports and the compiler's included files are not identical sets.
- **Interview:** Explain how bootstrap imports select the active Angular root and why an unimported TypeScript file can still cause compilation errors.

### Phase 16 — Use the standalone default

- **Files:** `HazardDashboard.Web/src/app/app.ts`, `src/app/features/hazard/feature-dashboard/dashboard/dashboard.component.ts`, `src/app/features/hazard/ui-map/map/map.component.ts`, and `src/app/features/hazard/ui-detail/detail-panel/detail-panel.component.ts` (all under `HazardDashboard.Web`).
- **Does:** Removes redundant `standalone: true` metadata from all four components without changing imports or OnPush settings.
- **Why:** Angular 21 defaults components to standalone; explicit metadata is unnecessary here.
- **Equivalent:** Omitting an option whose framework default already matches the desired behavior. NestJS modules are a different mechanism, not an equivalent of Angular standalone components.
- **Interview:** Explain why removing this flag does not make these components NgModule-based, and why their template dependencies still belong in `imports`.

### Phase 17 — Signal-based map view query

- **File:** `HazardDashboard.Web/src/app/features/hazard/ui-map/map/map.component.ts`.
- **Does:** Replaces the decorated `@ViewChild` property with `viewChild.required()` and reads the signal when initializing Leaflet.
- **Why:** The map element is always present in the template. A required signal query expresses that contract without a TypeScript definite-assignment assertion; access remains in `ngAfterViewInit` when the view exists.
- **Equivalent:** Unlike NestJS dependency injection, this Angular query locates a template element. TypeScript's `!` only suppresses a compile-time check; the required query reports an unavailable result at runtime when read.
- **Interview:** Explain signal query access and why a required query must not be read before the view is ready.

### Phase 18 — Volcano status string enum

- **File:** `HazardDashboard.Web/src/app/shared/contracts/volcano.model.ts`.
- **Does / why:** Replaces the status string union with a string enum, preserving API values while providing named runtime members.
- **Equivalent:** TypeScript string enums can also represent NestJS DTO values; unlike a union they emit a runtime object. This does not validate incoming JSON or generate the contract from C#.
- **Interview:** Explain string versus numeric enums and why HTTP response typing is not runtime validation.
- **File:** `HazardDashboard.Web/src/app/shared/contracts/index.ts`.
- **Does / why:** Exports the enum as a value, not only a type, so consumers can compare enum members through the public entry point.
- **Equivalent / interview:** Explain TypeScript type-only exports versus runtime exports, equally applicable in NestJS.
- **File:** `HazardDashboard.Web/src/app/features/hazard/ui-map/marker-layer.service.ts`.
- **Does / why:** Accepts `VolcanoStatus` in the icon helper and compares named members; preserves existing icons and fallback behavior in the UI layer.
- **Equivalent / interview:** Explain how a typed parameter prevents arbitrary strings in TypeScript callers without changing the wire format.

### Phase 19 — Initial Git repository publication

- **File:** `.gitignore` (repository root).
- **Does:** Excludes generated .NET/Angular output, dependencies, local environment files, certificate/key files, and machine-specific artifacts.
- **Why:** A clean clone should restore dependencies and build outputs; local secrets must not enter version control. The existing demo database credentials remain development-only configuration.
- **Equivalent:** The same Git rules apply to NestJS/Prisma projects; .NET additionally generates `bin/` and `obj/` directories. EF migrations remain committed, like Prisma migrations.
- **Interview:** Explain source versus generated artifacts and why `.gitignore` cannot remove secrets already committed to history.

### Phase 20 — Portfolio documentation review

- **File:** `README.md`.
- **Does:** Adds clone and verification instructions, code navigation, operational caveats, and explicit current limitations; removes broken placeholder image links.
- **Why:** Reviewers need an accurate starting point. Missing historical data, a development-only dashboard, and incomplete tests should not be presented as finished production functionality.
- **Equivalent:** A NestJS project likewise needs reproducible setup, readiness semantics, and honest validation evidence; a successful HTTP liveness response does not prove database health.
- **Interview:** Explain liveness versus readiness and why a rolling live feed does not automatically provide historical backfill.
- **Validation scope:** Documentation-only phase; commands checked against current configuration, not a new clean-clone end-to-end test. Account-level portfolio review and draft profile text are separate local artifacts, not published in this repository.
