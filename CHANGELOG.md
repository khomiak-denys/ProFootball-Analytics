## Changelog (latest 5 pull requests)

**Update 28/04/2026 - Localization + Auth About System + Input Style Reuse**  
**Goal.** Finalize runtime UI localization and improve authentication screen usability/consistency.  
**Description.** Added RESX-based localization wiring for presentation views, fixed runtime text refresh/label rendering issues, introduced a localized "About system" dialog from the login screen, and refactored auth input styling to reuse shared TextBox/PasswordBox styles with consistent focus behavior.

**Update 27/04/2026 - Result Pattern Flow (#12)**  
**Goal.** Replace exception-driven control flow with a result-based approach in core app paths.  
**Description.** Introduced generic `Result` handling for commands/queries, moved errors to dedicated models, aligned dispatching behavior, and updated application/infrastructure tests for the new flow.

**Update 27/04/2026 - Domain Layout Refactor (#11)**  
**Goal.** Simplify domain structure and improve repository placement consistency.  
**Description.** Reorganized domain folders by entity, moved repository interfaces closer to their aggregates, and aligned infrastructure wiring to the new domain layout.

**Update 24/04/2026 - Auth RBAC (#10)**  
**Goal.** Add authentication and role-based access control to the system.  
**Description.** Implemented sign-in/registration, user persistence, password hashing, role-aware access behavior, and end-to-end auth/session integration in presentation and tests.

**Update 21/04/2026 - UI Theme Redesign with ModernWPF (#8)**  
**Goal.** Modernize UI styling and improve shell consistency.  
**Description.** Integrated ModernWPF resources, refreshed shell/header styling, improved visual consistency across views, and stabilized startup/error handling for UI initialization.

**Update 18/04/2026 - Country as Text + File Logging (#7)**  
**Goal.** Simplify country modeling and improve runtime observability.  
**Description.** Migrated country usage to text-based representation across layers and added file logging/configuration improvements for easier diagnostics.

**Update 18/04/2026 - Application CQRS Reorganization (#6)**  
**Goal.** Restructure application flow around CQRS contracts and dispatching.  
**Description.** Added command/query contracts and dispatchers, moved handling to CQRS-driven paths, and aligned presentation/data-loader integration with dispatcher-based execution.

**Update 18/04/2026 - XUnit Feature Coverage Expansion (#5)**  
**Goal.** Improve automated test coverage across solution layers.  
**Description.** Expanded xUnit coverage for domain/application/infrastructure scenarios and stabilized key feature tests for query/import/auth-related behavior.

**Update 18/04/2026 - Leagues & Analytics Reference Redesign (#4)**  
**Goal.** Improve leagues and analytics UX/data presentation.  
**Description.** Refined league/analytics reference pages, improved data visualization behavior, and adjusted related query/presentation logic for clearer dashboard insights.

**Update 16/04/2026 - Players & Teams Reference Redesign (#3)**  
**Goal.** Redesign players/teams reference experience.  
**Description.** Delivered updated players and teams reference flows, including improved page structure and interaction behavior for those sections.

**Update 16/04/2026 - Dashboard Reference Shell (#2)**  
**Goal.** Build a baseline dashboard shell for further features.  
**Description.** Added foundational dashboard shell/navigation structure that subsequent feature branches integrated with.

**Update 16/04/2026 - MVP Read-Only Analytics (#1)**  
**Goal.** Deliver initial end-to-end analytics MVP in read-only mode.  
**Description.** Introduced first read-only analytics slice with core browsing/overview capabilities to bootstrap the product baseline.
