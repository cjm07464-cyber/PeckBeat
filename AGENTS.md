# AGENTS.md

## Project
- Project name: PeckBeat
- Korean title: 부리박자
- Unity version: 6000.3.9f1
- Template: Universal 2D
- Goal: one-stage rhythm mini game prototype.

## Core Concept
The player controls a mother bird.
Normal chicks should be fed on the beat.
Cuckoo chicks should not be fed; they require a quick double-tap rhythm action.

## Input
Use Unity New Input System only.
Do not use UnityEngine.Input.GetKeyDown.

Primary input:
- Keyboard D
- Xbox Controller A / Gamepad buttonSouth

Secondary input:
- Keyboard J
- Xbox Controller B / Gamepad buttonEast

## Code Rules
- Keep scripts small and prototype-friendly.
- Do not add DOTween unless explicitly requested.
- Do not create GameManager or SoundManager unless explicitly requested.
- Prefer SerializeField fields for Unity Inspector tuning.
- Avoid touching Library, Temp, Obj, Logs, UserSettings.
- Do not move Unity project settings unless requested.

## Folder Rules
Main project files should live under:
Assets/_PeckBeat

Scripts:
Assets/_PeckBeat/Scripts

Art:
Assets/_PeckBeat/Art

Audio:
Assets/_PeckBeat/Audio

Prefabs:
Assets/_PeckBeat/Prefabs

Scenes:
Assets/_PeckBeat/Scenes
