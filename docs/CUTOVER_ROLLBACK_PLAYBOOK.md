# Cutover & Rollback Playbook

## Cutover Steps

1. Freeze legacy writes and take final legacy backup.
2. On the migrated app, create DB backup using maintenance command.
3. Export migration data package (`.json`) as transfer snapshot.
4. Deploy packaged migrated build to target users.
5. Verify startup + migrations + core workflow:
   - import file
   - apply rules
   - manual recategorization
   - dashboard refresh

## Rollback Trigger Conditions

- migration failure on production data
- critical import/categorization regression
- data integrity mismatch after cutover checks

## Rollback Procedure

1. Stop migrated app instances.
2. Restore previous DB backup using restore command.
3. Re-enable legacy runtime if required.
4. Capture incident details and affected datasets.
5. Open post-mortem and corrective patch plan.

## Post-cutover Monitoring

- import warnings/errors rate
- uncategorized queue growth
- dashboard rendering latency
- user-reported category correction churn
