# Stage 01 Spec - 부리박자

## Short Summary
고정된 둥지 위에서 아기새들이 순환하고,
일반 아기새에게는 한 박에 먹이를 주며,
섞여 들어온 뻐꾸기는 한 박 안의 반박 두 번으로 따닥 쳐내는 리듬 미니게임이다.

## Current Scope
Prototype first.
Do not implement full scoring, combo, fever, result screen, or final UI yet.

## Gameplay
- Mother bird is on the left.
- Nest and chicks are on the right.
- Normal chicks require one beat input.
- Cuckoo chicks require a quick double-tap input.
- Most animation should be done by sprite switching, movement, scale changes, and sorting order changes.

## Reusable Systems
- DSP-based music time
- Chart text parsing
- Input timing judgment
- Input timing offset
- SFX playback

## Not Current Scope
- Old walking bird prototype
- Flying enemy note structure
- Camera rotation
- Ground/tree/air section
