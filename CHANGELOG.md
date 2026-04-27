## Changelog (latest 5 pull requests)

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
