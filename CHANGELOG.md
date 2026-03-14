# Changelog

All notable changes to this project are documented in this file.

## [Unreleased] - 2026-03-14

### Fixed
- `MotorFunctions.InitializeDynamixelMotors` now checks return values from both `openPort` calls and reports failure if either port does not open.
- `MotorFunctions.IsTorqueOn(string[] motors)` now returns actual torque state instead of always returning `false`.
- `MotorSequence` now initializes `TrainingMotorSequence` and clears sequence dictionaries before reuse to prevent null references and stale entries.
- `Extensions.BuildMotorSequence` now uses method-local storage and de-duplicates motor keys to avoid cross-call state leakage and key collisions.
- `Remember` constructors now initialize instance/static dictionaries more safely to avoid re-initialization side effects.
- `Remember` SQL operations now use parameterized commands for values instead of string concatenation.
- `Remember.QueryLimbicValue` now uses a parameterized query with null handling and returns `0` when no value exists.
- `Remember.RetrieveAnimation` now checks for empty result sets before parsing.
- `Remember.ParseAnimation` now guards against duplicate keys and updates existing command entries safely.
- `Remember.ClearTable` and `Remember.RetrieveData` now validate table names against an allowlist.

### Added (Phase 2)
- Added timed trajectory primitives in `MotionTrajectory.cs`:
  - `MotionTrajectoryStep` for timed pose targets.
  - `MotionTrajectoryPlayer` for executing trajectory steps.
  - `BipedGaitFactory` for generating a simple two-phase biped walking cycle from a neutral pose.
- Added `MotorFunctions.MoveMotorSequenceSmooth(...)` to interpolate target positions over time for smoother movement transitions.
- Added `WalkController` for high-level walking-cycle execution with basic safety checks (torque, load, temperature, voltage).
