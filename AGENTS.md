# Project Guidance

This project is a collection of short rhythm minigames. Each stage may have different gameplay rules.

## Shared principles
- Music timing is the core of gameplay.
- There is no game over during a stage.
- Use Unity Input System actions, not hardcoded keyboard/gamepad checks.
- The game must support keyboard, standard gamepads, and Xbox Adaptive Controller-compatible mappings.
- Use logical inputs such as `ActionA` and `ActionB`.
- Keep shared systems limited to genuinely common concerns: rhythm clock/song time, input abstraction, audio, and result tracking.
- Do not force stage-specific mechanics into one universal gameplay manager.
- Final artwork will replace placeholder sprites later. Gameplay code must not depend on placeholder shapes/colors.

## Current work
Current implementation target: **Stage 1 – Feeding**.

Read:
- `docs/STAGE_01_FEEDING.md`

## Deprecated prototype
The previous prototype direction is obsolete and must not be treated as current design:
- Ground → Tree → Air progression
- walking/flying gameplay
- 90-degree camera rotation
- previous Suno prototype music
- previous moving-note presentation tied to that concept

Old code may be reused only when it is clearly useful for the new project structure.
