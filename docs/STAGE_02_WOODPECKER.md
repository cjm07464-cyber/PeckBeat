# STAGE 02 — WOODPECKER SCULPTOR

## 0. Document Purpose

This document is the complete standalone specification for Stage 02 of **PeckBeat**.

It must be readable and implementable **without referring to the Stage 01 document**.

Stage 02 is a short Rhythm Heaven-style rhythm minigame in which a woodpecker sculptor carves logs brought in by mice.

The implementation should stay intentionally small and deadline-friendly.

---

# 1. High-Level Concept

## Theme

A woodpecker sculptor sits at the center of the screen, facing the camera/front.

The woodpecker is positioned on or in front of a visible tree-ring / cut-tree surface in a forest setting.

From the **left side of the screen**, mice ride wheeled boards and carry logs toward the woodpecker.

The woodpecker carves the logs in rhythm.

There are only two gameplay target types:

1. **Normal Hit Target**
   - Uses Action A.
   - Requires 1 or 2 on-beat pecks.
   - A small sign held by the mouse shows the required number in advance.

2. **Hold Target**
   - Uses Action B.
   - Two mice carry a longer log.
   - The player holds the button while the woodpecker rapidly pecks.
   - On success, the long log becomes a baseball bat.

This is the full gameplay concept.

Do not add additional target types unless explicitly requested later.

---

# 2. Final Game Flow Context

The overall game flow is:

```text
Title
  ↓
Stage 01
  ↓
Stage 02
  ↓
Result / End
```

Stage 02 should be able to run as its own Unity scene and should later be callable from the overall game flow.

Stage 02 has no game over.

A miss should not stop the song or end the stage.

---

# 3. Input

Use the Unity Input System.

Do not hardcode gameplay logic directly to physical keys.

Logical actions:

```text
ActionA
ActionB
```

Current intended bindings:

```text
ActionA
- Keyboard: D
- Dedicated / Xbox-compatible controller: Left gameplay button

ActionB
- Keyboard: J
- Dedicated / Xbox-compatible controller: Right gameplay button
```

If the existing project already maps the dedicated controller through Gamepad bindings, reuse that abstraction.

Stage 02 should consume the same logical input layer used by the rest of PeckBeat.

---

# 4. Core Screen Layout

The stage is viewed from the front.

Suggested composition:

```text
LEFT                                              CENTER                                     RIGHT

Spawn / approach area                    Woodpecker + tree rings                       Exit area

        Mouse + Log  --------------------->  TARGET  ------------------------------->

             Slot1                              Slot2                                     Slot3
```

The woodpecker stays near the center.

The incoming mice/log targets move from left to right.

The camera does not need to move.

---

# 5. Slot System

Stage 02 only needs three gameplay slots.

## Slot1 — Spawn / Approach

The target first appears here.

Purpose:
- Gives the player visual warning.
- The sign number must already be visible for normal targets.
- The long-log silhouette must already identify hold targets.

## Slot2 — Judgment Position

This is the center position in front of the woodpecker.

This is the actual rhythm judgment point.

All Action A hit timing and Action B hold timing are judged relative to this position / chart time.

## Slot3 — Exit

After the target has been resolved, it moves toward Slot3 and leaves the scene.

Suggested flow:

```text
Slot1
  ↓
Slot2
  ↓
resolve gameplay
  ↓
Slot3
  ↓
Destroy
```

Movement should be synchronized to song/chart time, not uncontrolled frame-based drift.

A simple linear movement is sufficient.

No easing system is required for the first implementation.

---

# 6. Target Type A — Normal Sculpture Target

## Visual

One mouse rides a small wheeled board.

The mouse carries:
- one normal log
- one small sign

The sign displays:

```text
1
2
```

The number tells the player exactly how many pecks are required.

The sign is a gameplay telegraph and must be readable before Slot2.

The mouse does not need complex animation.

---

# 7. Action A Rules

Action A is the normal peck button.

Each correct press triggers one woodpecker Peck visual.

All required pecks are on the beat.

There are only two normal patterns:

```text
1-hit
A

2-hit
A A
```

The chart determines the exact beats.

The player must never need to guess whether the target requires 1 or 2 hits.

The mouse's sign provides that information visually.

## Important Consistency Rule

The same number always means the same input count.

```text
Sign 1 = exactly 1 ActionA hit
Sign 2 = exactly 2 ActionA hits
```

Do not create hidden variants.

Do not create offbeat versions of the same numbered sign unless explicitly redesigned later.

---

# 8. Normal Target Success

After the required number of correct Action A hits is completed:

1. The log is considered successfully carved.
2. Replace or swap the log visual with one of the finished wooden sculptures.
3. The target continues to Slot3.
4. It exits and is destroyed.

There are two wooden sculpture result types.

Example mapping:

```text
1-hit → Sculpture A
2-hit → Sculpture B
```

This mapping is recommended because it is simple and readable.

If the actual art later suggests a different mapping, changing the visual mapping is acceptable as long as the gameplay rule stays unchanged.

---

# 9. Normal Target Miss

The exact scoring model can remain simple.

A miss occurs when a required Action A timing passes without a valid hit, or when the existing project-wide judgment rules consider the press invalid.

For the first implementation:

- Do not stop the song.
- Do not trigger game over.
- Do not require a unique miss animation for the mouse.
- Do not add complicated recovery logic.
- Continue the target toward Slot3.

The woodpecker may use the existing Idle / Peck visuals only.

A dedicated Miss sprite is not required for Stage 02.

Internal success/miss tracking is enough until the global result screen is implemented.

---

# 10. Target Type B — Hold Target

## Visual

Two mice ride one longer wheeled board.

They carry a long log together.

The long-log target must be visually distinct from the normal target before reaching Slot2.

No number sign is required for this target.

The two-mouse + long-log silhouette is the telegraph that this is the Hold pattern.

---

# 11. Action B Hold Rules

Action B is a hold input.

The chart defines a start beat. Every Hold Target lasts exactly 2 beats:

```text
Hold Start Beat
Hold End Beat = Hold Start Beat + 2
```

The player presses and holds Action B for the required duration.

Conceptually:

```text
B Down
↓
hold
↓
B Up
```

During the valid hold:

- the woodpecker performs 8 distinct Peck strikes at 0.25-beat intervals
- this creates a "dododododo" rapid carving motion

Example visual loop:

```text
Peck → short Idle gap, repeated 8 times
```

This loop is purely visual.

Do not require additional woodpecker sprites for the rapid-peck sequence.

---

# 12. Hold Success

On successful completion of the hold:

```text
Long Log
   ↓
Baseball Bat
```

The completed baseball bat then travels toward Slot3 with the mouse board and exits.

The baseball bat is the only special finished object required for the hold target.

---

# 13. Hold Miss

For the first implementation, keep hold failure simple.

Possible miss conditions:

- Action B was not pressed near the hold start.
- Action B was released substantially too early.
- The required hold was not completed.

On miss:

- record Miss
- stop the rapid Peck/Idle loop if it is running
- return the woodpecker to Idle
- let the target continue to Slot3
- do not add a special broken-object animation

No game over.

Do not add multiple grades of hold accuracy unless explicitly requested later.

---

# 14. Woodpecker Visual Requirements

Only two woodpecker gameplay sprites are required.

```text
1. Idle
2. Peck
```

That is intentional.

Do not expand the sprite list unless a later polish pass explicitly requests it.

## Idle

The woodpecker faces forward and is visually readable as the sculptor character.

## Peck

The peck pose must clearly show that the woodpecker is striking the log.

Because this is a front-facing scene, exaggeration is allowed if needed for readability.

## Hold Animation

No dedicated Hold sprite.

During Action B hold, each 0.25-beat strike briefly shows Peck and returns to Idle before the next strike:

```text
Peck → Idle × 8
```

plays rapidly.

The effect should feel mechanical, energetic, and rhythmic.

---

# 15. Mouse Visual Requirements

## Normal Mouse Target

Required visual:

- one mouse
- small wheeled board
- normal-sized log
- small sign in one hand
- sign number is shown separately as 1 / 2

The sign number should preferably be a reusable UI/Text/Sprite element rather than three completely separate mouse illustrations.

This reduces art workload.

## Long Hold Target

Required visual:

- long wheeled board
- two mice
- one mouse on each side / end
- both support one long log

No walk cycle is required.

No wheel animation is required for MVP.

The entire target can simply slide across the screen.

---

# 16. Required Finished Object Art

Only three completed-object visuals are required:

```text
Wood Sculpture A
Wood Sculpture B
Baseball Bat
```

The two sculptures should be visually distinguishable.

They do not require separate animation.

A simple sprite replacement at success timing is sufficient.

---

# 17. Minimum Background Art

Recommended minimum environment art:

```text
Forest background
Tree-ring / cut-tree visual behind or under the woodpecker
```

The environment should frame the woodpecker as a sculptor working in a forest.

Do not spend implementation time on parallax unless the core game is already complete.

---

# 18. Minimum Art Asset List

The practical MVP art list is:

```text
Woodpecker
- Idle
- Peck

Target
- Normal mouse + wheeled board + log
- Long board + two mice + long log

Finished objects
- Sculpture A
- Sculpture B
- Baseball Bat

Environment
- Forest background
- Tree-ring / work-area visual

Optional reusable elements
- Sign plate
- Number 1
- Number 2
```

Keep the art list small.

AI-generated art consistency is a known deadline risk.

Do not attempt frame-by-frame AI animation.

Use Unity transforms and sprite switching for motion instead.

---

# 19. Animation Philosophy

Stage 02 should use limited animation intentionally.

Do not depend on AI-generated frame-by-frame animation.

The game should feel alive using:

- position movement
- sprite swapping
- short scale changes
- small rotations
- impact effects
- accurate SFX timing

The only required character animation is:

```text
Idle → Peck → Idle
```

For hold:

```text
Idle → Peck → Idle → Peck → ...
```

Smoothness should come from Unity motion and rhythm timing, not from a large sprite sheet.

---

# 20. Audio Requirements

Stage 02 needs:

```text
1. Stage 02 BGM
2. Normal Peck SFX
3. Hold rapid-peck sound or repeatable Peck SFX
4. Successful sculpture completion SFX
5. Miss SFX
```

Some sounds may be reused if appropriate.

Avoid building a complicated audio mixer specifically for Stage 02 unless already available globally.

Use the existing project audio architecture when possible.

---

# 21. Rhythm / DSP Requirements

Rhythm timing must be based on the project's song-time / DSP-time rhythm system.

Do not judge inputs using `Time.time` if the existing DSP song clock is available.

The stage chart should drive:

- target spawn timing
- Slot1 → Slot2 movement
- judgment timing
- Slot2 → Slot3 movement
- Action A hit beats
- Action B hold start
- Action B hold end

Visual position and judgment timing must stay synchronized with the music.

---

# 22. Suggested Stage 02 Chart Data

A simple target event can conceptually contain:

```text
Target Beat
Target Type
Required Hits
Hold End Beat
Result Visual Type
```

Recommended conceptual structure:

```text
Stage02TargetEvent

targetBeat
targetType:
    Normal
    Hold

requiredHits:
    1
    2

holdEndBeat
```

For Normal targets:

```text
requiredHits = 1 / 2
```

For Hold targets:

```text
requiredHits is ignored
holdEndBeat is used
```

Do not over-generalize this data model for hypothetical future stages.

Stage 02 only needs to support its actual rules.

---

# 23. Suggested Runtime Components

Names are suggestions, not strict requirements.

A small implementation could use:

```text
Stage02WoodpeckerController
Stage02TargetController
Stage02Feeding/Stage02Chart equivalent
Stage02AudioController
```

Prefer clearer Woodpecker-specific naming, for example:

```text
Stage02WoodpeckerController
Stage02SculptingChart
Stage02SculptTarget
Stage02AudioController
```

The existing shared systems should remain shared:

```text
Input abstraction
DSP rhythm/song clock
General audio infrastructure
Result tracking
```

Stage-specific rules should stay inside Stage 02 scripts.

Do not modify Stage 01 gameplay scripts to implement Stage 02 mechanics.

---

# 24. Suggested Scene Hierarchy

Example:

```text
Stage02_Woodpecker

Main Camera

Managers
├─ InputManager
├─ RhythmManager
└─ AudioManager
   ├─ BGMSource
   └─ SFXSource

StageRoot
├─ ForestBackground
├─ TreeRing
└─ DecorativeElements

GameplayRoot
├─ Woodpecker
│  └─ Visual
│
├─ TargetContainer
│
└─ Slots
   ├─ Slot1
   ├─ Slot2
   └─ Slot3

UIRoot
└─ Stage-specific minimal UI if needed
```

Adapt this to the current project organization rather than duplicating global managers if they already persist across scenes.

Avoid duplicate Input/Rhythm/Audio managers.

---

# 25. Basic Target Runtime Flow

## Normal Target

```text
Spawn
↓
Move toward Slot1 / begin approach
↓
Move Slot1 → Slot2 using chart timing
↓
Read sign: 1 / 2
↓
Player performs required ActionA hits
↓
Success or Miss
↓
If success: log sprite → sculpture sprite
↓
Move Slot2 → Slot3
↓
Destroy
```

## Hold Target

```text
Spawn
↓
Two mice + long log approach
↓
Move Slot1 → Slot2
↓
ActionB Down near hold start
↓
8 Peck strikes at 0.25-beat intervals
↓
Hold until chart end
↓
Release / complete
↓
If success: long log → baseball bat
↓
Move Slot2 → Slot3
↓
Destroy
```

---

# 26. Readability Rules

The player should be able to understand the required action before the judgment moment.

Normal target:

```text
Number sign = number of A presses
```

Hold target:

```text
Two mice + long log = hold B
```

Do not make two visually identical targets require different input behavior.

Do not hide rhythm rules from the player.

Consistency is more important than surprise.

---

# 27. Difficulty Philosophy

Stage 02 should be simpler to understand than Stage 01.

Difficulty should come mainly from:

- target density
- musical placement
- alternation between 1 / 2 hits
- timing of Hold targets

Difficulty should NOT come from:

- invisible rules
- changing what the same sign means
- arbitrary offbeat variants
- sudden unexplained input changes

---

# 28. Example Pattern Vocabulary

Example sequence only:

```text
1-hit

2-hit

1-hit
1-hit
2-hit

HOLD

2-hit
1-hit

HOLD

2-hit
```

The actual chart should be created after listening to the final Stage 02 BGM.

Do not blindly use this example as the final chart.

---

# 29. Result Tracking

Stage 02 only needs basic result data at first:

```text
Success Count
Miss Count
```

If the global game later uses grades or scores, Stage 02 can report its results to that shared result system.

Do not build a separate complex scoring system inside Stage 02.

---

# 30. Controller Disconnect Compatibility

The overall project may support runtime controller disconnect handling.

Stage 02 should not implement its own separate controller-disconnect system.

If a global controller connection manager is added:

- Stage 02 should pause through the shared rhythm/pause system.
- The BGM and DSP-based song time must remain synchronized after resume.
- A currently active Hold should be cancelled or safely reset on pause unless the global pause design explicitly preserves it.

This is a global system concern, not a Stage 02-specific mechanic.

---

# 31. Five-Hour Development Scope

Stage 02 is intentionally scoped to be buildable quickly.

Target development budget:

```text
Art / asset preparation          ~1h 20m
Basic scene + target movement    ~1h 20m
Action A 1/2-hit gameplay        ~50m
Action B Hold gameplay           ~40m
Chart + audio + testing          ~50m
```

This is an approximate target, not a guarantee.

The biggest risk is AI art generation consistency.

If art generation begins consuming too much time, use simpler placeholder art and finish gameplay first.

---

# 32. MVP Definition

Stage 02 is considered functionally complete when all of the following work:

- Stage scene loads.
- Stage 02 BGM plays in sync with the DSP rhythm clock.
- Normal mouse/log targets move Slot1 → Slot2 → Slot3.
- Normal signs correctly show 1 or 2.
- Action A can complete 1-hit targets.
- Action A can complete 2-hit targets.
- Successful normal target becomes a finished sculpture.
- Hold target visibly uses two mice and a long log.
- Action B hold is detected.
- Woodpecker rapidly alternates Idle / Peck during Hold.
- Successful Hold produces a baseball bat.
- Misses do not game-over the stage.
- Targets exit and clean themselves up.
- Stage can reach the end of the song without errors.

Everything beyond this is polish.

---

# 33. Explicit Non-Goals for the First Pass

Do NOT add the following unless explicitly requested:

```text
Walking animation
Mouse walk cycle
Wheel rotation animation
Frame-by-frame carving animation
Multiple carving stages
Physics-based logs
Rigidbody-based target motion
Combo system
Complex score formula
Game over
Branching routes
Extra target types
More than two normal hit counts
Different meanings for the same sign
Complex particle system
Camera movement
Parallax system
Character Animator state-machine complexity
Object pooling
Generic framework for hypothetical future stages
```

These features are not required to make Stage 02 work.

Avoid overengineering.

---

# 34. Optional Polish — Only After MVP Works

These are NEW / OPTIONAL polish ideas.

They are not required for the core implementation.

Only add them if the entire Stage 02 already works and deadline time remains.

Possible low-cost polish:

```text
Small impact flash on each Peck
Tiny tree/log shake
0.05–0.1 second squash/stretch
Small wood-chip particles
Short "POOF" effect when sculpture finishes
Mouse board bounce
Completion sound accent
```

Why these are useful:

They improve impact and rhythm readability without requiring additional AI animation frames.

Why they are optional:

The stage is fully playable without them.

---

# 35. Art Production Rule

Because AI image generation has limited frame-to-frame consistency:

**Do not design Stage 02 around smooth AI-generated animation.**

Instead:

```text
AI creates a few stable key images
+
Unity moves those images
+
Sprite swapping creates pose changes
+
SFX creates impact
```

This is the intended production strategy.

---

# 36. Priority Order Under Deadline

If time becomes limited, work in this order:

```text
1. Input works
2. DSP/BGM timing works
3. Targets move through Slot1 → Slot2 → Slot3
4. A 1/2-hit logic works
5. B Hold logic works
6. Target success visuals swap correctly
7. Basic SFX
8. Final art replacement
9. Extra visual polish
```

Never sacrifice functional end-to-end gameplay to add optional animation.

---

# 37. Final Design Summary

Stage 02 is a woodpecker sculpting rhythm game.

```text
NORMAL TARGET

Mouse arrives with log + number sign
                ↓
              Slot2
                ↓
      Press A on the beat
           1 / 2 times
                ↓
        Log becomes sculpture
                ↓
              Exit
```

```text
HOLD TARGET

Two mice arrive with long log
                ↓
              Slot2
                ↓
             Hold B
                ↓
     Peck → Idle × 8 rapidly
                ↓
       Log becomes baseball bat
                ↓
              Exit
```

Core visual requirements:

```text
Woodpecker: Idle + Peck only
Normal mouse target
Two-mouse long-log target
2 wooden sculptures
1 baseball bat
Forest background
Tree-ring work area
```

Core stage rule:

> Keep it readable, rhythmic, small, and consistent.

Do not expand the design unless a later task explicitly asks for expansion.
