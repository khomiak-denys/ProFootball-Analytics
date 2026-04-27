## Changelog (from Git/PR history)

**Update 24/04/2026 - Auth RBAC (#10)**  
**Goal.** Add authentication and role-based access control to the system.  
**Description.** Introduced sign-in/registration flow, persisted app users, wired password hashing, added auth-related tests, and integrated auth/session behavior into the presentation shell.

**Update 21/04/2026 - UI Theme Redesign with ModernWPF (#8)**  
**Goal.** Modernize the UI style and improve visual consistency.  
**Description.** Added ModernWPF resources, refreshed shell/theme styling, improved header/dashboard behavior, and stabilized startup/exception handling around UI initialization.

**Update 18/04/2026 - Country as Text + File Logging (#7)**  
**Goal.** Simplify country model usage and improve observability.  
**Description.** Reworked country handling to use text-based country names, updated querying/presentation paths accordingly, and added file-based logging/config updates.

**Update 18/04/2026 - Application CQRS Reorganization (#6)**  
**Goal.** Restructure application flow around CQRS.  
**Description.** Introduced command/query contracts and dispatchers, moved handlers into infrastructure wiring, migrated presentation/data-loader interactions to dispatcher-based flow, and updated tests.

**Update 18/04/2026 - XUnit Feature Coverage Expansion (#5)**  
**Goal.** Increase automated test coverage across layers.  
**Description.** Added/expanded domain, application, and infrastructure tests (including query/import scenarios), improved test project structure, and fixed test-related edge cases.

**Update 18/04/2026 - Leagues/Analytics Reference Redesign (#4)**  
**Goal.** Improve analytics and league reference UX/logic.  
**Description.** Refined analytics visualization behavior, improved fallback/empty states, and adjusted league/division calculations for more accurate dashboard summaries.

**Update 16/04/2026 - Players/Teams Reference Redesign (#3)**  
**Goal.** Improve player/team reference pages and interactions.  
**Description.** Merged branch for player/team reference redesign, aligning data presentation and interaction patterns for those views.

**Update 16/04/2026 - Dashboard Reference Shell (#2)**  
**Goal.** Build the dashboard shell foundation.  
**Description.** Added the initial dashboard-oriented shell/navigation structure used by subsequent feature work.

**Update 16/04/2026 - MVP Read-Only Analytics (#1)**  
**Goal.** Deliver first working analytics MVP in read-only mode.  
**Description.** Introduced baseline analytics features and read-only data browsing to establish the first end-to-end functional slice.

**Update 25/04/2026 - Data Model Cleanup + Repository Refactor (current branch work)**  
**Goal.** Normalize persistence model and enforce repository-based DB access.  
**Description.** Removed `football_clubs`, dropped `*ApiID` columns from final schema via migration, remapped import linkage to internal IDs, added repository contracts per entity in Domain, implemented EF repositories in Infrastructure, moved query/import DB interaction behind repositories, and updated tests accordingly.
