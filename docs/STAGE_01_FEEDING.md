# Stage 01 – Feeding

## Status
This is the current design source of truth for Stage 1.

The previous Ground → Tree → Air prototype is obsolete.

Stage 1 is one minigame in a larger rhythm-game collection. Later stages may use completely different rules.

---

## Core concept

A mother bird feeds chicks in rhythm.

Normal chicks and cuckoo chicks rotate through a nest and approach the feeding position.

The player has two logical inputs:

- `ActionA` = feed
- `ActionB` = peck

There is no game over. Mistakes are recorded internally as MISS, but the stage continues in sync with the music.

---

## Input rules

### Normal chick

Correct input:
- `ActionA` on the intended beat = SUCCESS
- Mother bird state: `Feed`
- Normal chick state: `Fed`

Wrong input:
- `ActionB` = MISS
- Mother bird state: `WrongPeck`
- Normal chick remains `Idle`
- This does **not** start a Peck1 → Peck2 sequence.
- Pressing `ActionB` again must not advance the mother bird to `Peck2`.

Missed beat:
- If the intended `ActionA` timing is missed, record MISS internally.
- Do not show a special reaction.
- The normal chick remains unfed (`Idle`) until it transitions to `Exit`.
- The stage continues without interruption.

### Cuckoo chick

Wrong feed:
- `ActionA` = MISS
- Mother bird state: `WrongFeed`
- Cuckoo state: `Fed`
- The cuckoo successfully receives the food even though the player made the wrong choice.

Correct response:
- The cuckoo is removed with two `ActionB` inputs in the intended two-hit rhythm.

First `ActionB`:
- Mother bird state: `Peck1`
- Cuckoo state: `Hit1`

Second valid `ActionB`:
- Mother bird state: `Peck2`
- Cuckoo state: `Hit2`
- The cuckoo is then knocked away / removed.

The `Peck1 → Peck2` two-hit sequence exists **only for a cuckoo target**.

The exact judge windows and final chart timing will be tuned later.

---

## Visual state result table

| Target | Player input/result | Mother bird | Target bird | Result |
|---|---|---|---|---|
| Normal chick | Correct `ActionA` | `Feed` | `Fed` | SUCCESS |
| Normal chick | Wrong `ActionB` | `WrongPeck` | stays `Idle` | MISS |
| Normal chick | Missed beat | no special reaction required | stays `Idle`, then `Exit` | MISS |
| Cuckoo | Wrong `ActionA` | `WrongFeed` | `Fed` | MISS |
| Cuckoo | First valid `ActionB` | `Peck1` | `Hit1` | sequence continues |
| Cuckoo | Second valid `ActionB` | `Peck2` | `Hit2` | SUCCESS / cuckoo removed |

---

## Nest flow

The nest itself is static.

Birds create the illusion of clockwise rotation by moving between positions.

Front flow:

`3 → 2 → 1`

- Position 1 = current target
- Position 2 = next
- Position 3 = following

After Position 1 is resolved:
- the bird moves toward the back/exit path
- normal exit behavior uses the `Exit` state when appropriate
- the bird becomes smaller as it moves away
- it leaves the visible area
- the remaining birds advance

The nest does not require frame-by-frame rotation animation.

A cuckoo successfully removed by `Peck2` may leave through its knock-away presentation instead of the normal exit path.

---

## Placeholder visual rules

Placeholder visuals are temporary and will be replaced by final art without changing gameplay code.

### Normal chick
- `Idle` = yellow circle
- `Fed` = yellow triangle
- `Exit` = yellow square

### Cuckoo
- `Idle` = black circle
- `Fed` = black triangle
- `Exit` = black square
- `Hit1` = dedicated placeholder sprite if available
- `Hit2` = dedicated placeholder sprite if available

### Mother bird
Normal states may temporarily use the same white horizontal oval:
- `Idle`
- `Feed`
- `Peck1`
- `Peck2`

Wrong-input states use a red horizontal oval:
- `WrongFeed` = fed cuckoo with `ActionA`
- `WrongPeck` = pecked normal chick with `ActionB`

All visual states should be represented through sprite/state swapping rather than separate gameplay logic.

---

## Current prefab / scene preparation

The project currently has placeholder objects/prefabs prepared for:
- Mother bird
- Normal chick
- Cuckoo

Placeholder image assets are currently stored under:

`Art/Stage_01/Placeholder`

The images are imported as single sprites using Full Rect.

Implementation should allow final sprites to replace these placeholders later without rewriting gameplay systems.

Prefer:
- one gameplay object
- one child visual object / `SpriteRenderer`
- state-based sprite swapping

Do not create a separate GameObject for every pose unless there is a strong technical reason.

---

## Character visual states

### Mother bird
Required logical states:
- `Idle`
- `Feed`
- `Peck1`
- `Peck2`
- `WrongFeed`
- `WrongPeck`

### Normal chick
Required logical states:
- `Idle`
- `Fed`
- `Exit`

### Cuckoo
Required logical states:
- `Idle`
- `Fed`
- `Exit`
- `Hit1`
- `Hit2`

---

## Background art

Current Stage 1 background assets:
- blue sky
- cloud 1
- cloud 2
- nest
- tree branch supporting the nest

Background/nest presentation is largely static.

---

## Audio

Final Stage 1 music exists outside the Unity project at the moment:
- `Feeding Time!.mp3`

SFX also exist or will be added later.

Do not assume the audio files are already inside `Assets/`.

The rhythm/audio implementation should allow clips to be assigned later without structural changes.

---

## Architecture direction

Keep truly shared systems separate from Stage 1-specific logic.

### Shared
- rhythm clock / DSP song time
- input routing (`ActionA`, `ActionB`)
- audio playback
- result / miss tracking

### Stage 1 only
- feeding rules
- normal chick vs cuckoo rules
- cuckoo two-hit sequence
- nest position / rotation illusion
- mother/chick/cuckoo visual state changes

Avoid building one giant universal `RhythmGameManager` containing all future minigame rules.

---

## Important rule constraints

- `Normal Chick + ActionB` resolves immediately as MISS with `WrongPeck`.
- A normal chick can never enter the cuckoo `Peck1 → Peck2` sequence.
- A second `ActionB` on a normal chick must not cause `Peck2`.
- `Cuckoo + ActionA` is MISS, but visually the cuckoo becomes `Fed`.
- `Cuckoo + first valid ActionB` pairs `Peck1` with `Hit1`.
- `Cuckoo + second valid ActionB` pairs `Peck2` with `Hit2`.
- Missing a normal chick's feed timing causes no special visual reaction; record MISS and let the chick continue to `Exit`.
- MISS does not stop or rewind the stage.

---

## Current implementation priority

Before full charting or polish:

1. Keep placeholder character state swapping working.
2. Establish shared logical `ActionA` / `ActionB` input abstraction.
3. Build Stage 1 target-resolution rules using the state table above.
4. Build nest slot progression.
5. Add DSP/music timing and a short test chart.
6. Add final Stage 1 music.
7. Add SFX and polish.

Do not add unnecessary systems such as HP/game-over, walking/flying, camera rotation, or old prototype mechanics.
