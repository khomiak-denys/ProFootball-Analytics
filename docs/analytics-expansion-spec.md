# Analytics Expansion Spec (Pending Items Only)

## Implemented

### SQL automation
- Added in migrations:
  - `sp_backfill_team_league_links()`
  - `fn_get_player_latest_rating(p_player_id int)`
  - `trg_matches_validate_teams`
  - `trg_matches_validate_season_by_date`
  - `trg_matches_validate_league_country_consistency`
  - `trg_teams_shortname_normalize`
  - `trg_player_attributes_validate_range`
  - `trg_player_attributes_chronology_guard`
  - `trg_prevent_delete_referenced_team`
  - `trg_league_max_teams_guard`
- Added non-materialized dashboard view:
  - `vw_dashboard_overview`

## Not Implemented Yet

### Remaining scope
1. Tune synthetic generation realism (event distributions by league profile and player role weighting).
2. Add runbook documentation for generation lifecycle (`regenerate`/`append`, seed reproducibility, failure recovery).
3. Expand automated tests for statistical quality thresholds (not only integrity/idempotency).

## Acceptance Checklist (Pending)
- [x] Triggers/procedures/functions implemented in migrations.
- [x] New analytical entities implemented and filled.
- [ ] Synthetic data generation documented in runbooks.
- [ ] Test suites extended for advanced statistical quality checks.
