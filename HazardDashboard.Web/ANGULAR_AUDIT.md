# Angular engineering audit — phase 1

This is a staged migration, not a declaration of full compliance. The user requested small reviewable increments with a stop after each phase.

## Completed in this phase

- Angular CLI retained following the user's clarification that Nx is optional.
- Thin `src/app` shell: bootstrap, providers, router outlet, routes.
- Lazy dashboard feature in `src/app/features/hazard/feature-dashboard`.
- Existing map and detail panel moved into independent `ui-map` and `ui-detail` libraries.
- Transport services moved into `features/hazard/data-access`.
- Existing response interfaces centralized in `shared/contracts`; **these are still handwritten, not generated**.
- Each role folder exposes a public `index.ts`; imports use aliases.
- ESLint restricted imports prohibit UI → data-access, data-access → UI and shared → feature dependencies. This is scoped enforcement, not a full graph/cycle checker.
- All components explicitly standalone and OnPush.
- Reused all existing UI and service implementations; no duplicate components.

## Remaining findings and next phases

| Rule | Current issue | Required update |
| --- | --- | --- |
| §3, §4, §6, §9 | Dashboard contains HTTP subscriptions, selection orchestration, mutable signals, and mapping | Route-provided facade/store; API services per resource; RxJS reads with cancellation and `toSignal` |
| §4, §11 | `mergedEvents()` performs work during template rendering | Computed state in the facade/store |
| §2, §13, §15 | `any`, template `$any`, and map view-query non-null assertion remain | Unknown error narrowing, typed filter output, signal view query |
| §6, §9 | SignalR appends to an unbounded writable signal; initial connection failure has no retry; connection has no destruction cleanup | Observable event stream, bounded state in store, retry/cleanup, readonly UI signals |
| §7 | Response interfaces manually mirror C# anonymous responses | Backend-owned named contracts and reproducible generation/drift check; simply moving interfaces does not satisfy this rule |
| §3, §6, §14 | Marker classification and event merging have no pure utilities or tests | Domain util functions with table-driven unit tests |
| §11, §12 | Hardcoded style values and Leaflet HTML strings; marker CSS is scoped to the page despite Leaflet-created DOM | Central tokens, dedicated map styling and safe DOM creation, responsive panel |
| §6 | API/SignalR URLs are hardcoded | Shared configuration token, centralized error handling |
| §10 | Feature services are root-scoped | Route providers for facade/store/transport |
| §14 | No functioning frontend test suite | Component tests, facade state/cancellation tests, SignalR teardown tests |
| Runtime | Live events bypass the chosen time window; out-of-order requests can replace newer selections | Fix with facade stream changes and regression tests |
| Runtime | Unloaded hazard scores display as zero; detail lists only event count | Distinguish unknown score; populate contributing-event detail |

## Checklist (§16)

1. Layer placement: partial; component orchestration remains to extract.
2. Forbidden constructs: fail; listed above, preserved from the existing implementation.
3. Components: OnPush, standalone, signal inputs/outputs, built-in flow pass.
4. Smart/dumb split: UI dependency direction passes; feature must still become thin.
5. Nx: not applicable; public imports and scoped directional checks pass.
6. Reuse/shared admission: reused existing components; shared contains only contracts.
7. Pure rules and tests: outstanding.
8. Boundary lint, formatting, TypeScript and production build: see phase handoff; no automated behavior test coverage claimed.
9. Changes and deviations: this document records the staged scope and every identified remaining violation. No full-compliance claim.

## Commands

`npm start` and `npm run build` continue to use Angular CLI. Docker output remains `dist/HazardDashboard.Web/browser`.

`npm run lint` currently checks **module boundaries**, not the entire engineering policy. `npm run typecheck` and `npm run format:check` verify compiler and formatting rules. Full Angular/TypeScript policy lint and tests are still outstanding.
