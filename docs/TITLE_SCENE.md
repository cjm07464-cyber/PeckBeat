# TITLE SCENE — PECKBEAT

## 0. Document Purpose

This document is the standalone implementation specification for the **PeckBeat Title Scene**.

It is intended for Codex / implementation work.

The Title Scene controls the first user flow before Stage 01 begins.

The flow is:

```text
Black screen / Input Device Select
  ↓
Language Select
  ↓
Main Title Screen
  ↓
Start Game
  ↓
Stage 01
```

This document should be treated as the source of truth for the Title Scene.

---

# 1. Core Goal

The Title Scene must let the player choose:

1. Which input method they will use.
2. Which language they will use.
3. Then show the main title screen.
4. Then start Stage 01 when the player presses any valid button.

The Title Scene is not a complex menu.

Do not add options, settings, save data, stage select, animation systems, or advanced UI unless explicitly requested later.

---

# 2. Supported Input Methods

There are two selectable input methods.

```text
1. Xbox Adaptive Controller
2. Keyboard / Mouse
```

The spelling is:

```text
Xbox Adaptive Controller
```

However, in UI text, shorter labels are allowed if needed:

```text
Xbox
Keyboard / Mouse
```

---

# 3. Important Input Lock Rule

At the first screen only, both input methods are accepted.

After the player chooses one input method, the game locks to that method.

## Before selection

```text
Input Mode = None
Xbox input is accepted
Keyboard input is accepted
```

## After Xbox is selected

```text
Input Mode = Xbox
Only Xbox / Gamepad input is accepted
Keyboard input is ignored
```

## After Keyboard / Mouse is selected

```text
Input Mode = KeyboardMouse
Only keyboard input is accepted
Xbox / Gamepad input is ignored
```

This rule applies to:

- Language Select screen
- Main Title screen
- Start input

---

# 4. Logical Button Rules

Use the existing project input abstraction if possible.

The title scene uses two logical actions:

```text
ActionA
ActionB
```

Current intended physical mapping:

```text
ActionA
- Keyboard: D
- Xbox Adaptive Controller: left large button / mapped ActionA

ActionB
- Keyboard: J
- Xbox Adaptive Controller: right large button / mapped ActionB
```

In the title scene:

```text
ActionA = Move focus
ActionB = Confirm / Select
```

This differs from gameplay meaning, but uses the same logical buttons.

---

# 5. Screen Flow

## 5.1 Input Device Select Screen

Initial state.

Visual style:

```text
Black background
Two large selectable icons
```

Required options:

```text
[Xbox Adaptive Controller]
[Keyboard / Mouse]
```

The player can move focus between the two options.

```text
ActionA = Move focus
ActionB = Select focused option
```

This screen accepts both Xbox and Keyboard input.

When selected:

- Store selected input mode.
- Lock future title input to the selected mode.
- Move to Language Select.

---

## 5.2 Language Select Screen

Required options:

```text
[한국어]
[English]
```

Input:

```text
ActionA = Move focus
ActionB = Select focused language
```

This screen only accepts the selected input method.

When selected:

- Store selected language.
- Move to Main Title Screen.

---

## 5.3 Main Title Screen

The main title screen shows:

```text
Game Logo
Background image
Prompt text
```

Prompt text depends on language.

Korean:

```text
아무 버튼이나 눌러 시작
```

English:

```text
Press Any Button to Start
```

However, “Any Button” means any valid button from the selected input method only.

Example:

- If Xbox was selected, keyboard input must not start the game.
- If Keyboard / Mouse was selected, gamepad input must not start the game.

When a valid button is pressed:

```text
Load Stage 01
```

---

# 6. Recommended Title Scene State Machine

Use a simple state enum.

```text
TitleState
- InputDeviceSelect
- LanguageSelect
- MainTitle
- StartingGame
```

Use a selected input enum.

```text
SelectedInputMode
- None
- Xbox
- KeyboardMouse
```

Use a selected language enum.

```text
SelectedLanguage
- Korean
- English
```

The implementation should be simple and explicit.

Do not build a generic menu framework.

---

# 7. Recommended Scene Hierarchy

Suggested hierarchy:

```text
TitleScene
├─ Main Camera
├─ EventSystem
├─ TitleManager
└─ Canvas
   ├─ BlackBackground
   │
   ├─ InputSelectPanel
   │  ├─ XboxIcon
   │  ├─ PcIcon
   │  ├─ FocusFrame
   │  └─ HelpText
   │
   ├─ LanguageSelectPanel
   │  ├─ KoreanOption
   │  ├─ EnglishOption
   │  ├─ FocusFrame
   │  └─ HelpText
   │
   └─ MainTitlePanel
      ├─ Background
      ├─ Logo
      └─ PressAnyButtonText
```

Only one panel should be visible at a time.

```text
InputDeviceSelect:
InputSelectPanel ON
LanguageSelectPanel OFF
MainTitlePanel OFF

LanguageSelect:
InputSelectPanel OFF
LanguageSelectPanel ON
MainTitlePanel OFF

MainTitle:
InputSelectPanel OFF
LanguageSelectPanel OFF
MainTitlePanel ON
```

---

# 8. UI Focus Effect

The focused option should be visually clear.

A simple focus effect is enough.

Recommended:

```text
Focused:
- Scale 1.1
- Full opacity
- Focus frame ON

Not focused:
- Scale 1.0
- Slightly dimmed
- Focus frame OFF
```

Do not spend time on complicated animation.

A blinking focus frame is optional.

---

# 9. Required Art Assets

The Title Scene needs these art assets.

```text
1. Xbox Adaptive Controller icon
2. Keyboard / Mouse icon
3. Game logo
4. Title background
```

Temporary placeholder sprites are acceptable for implementation.

Final art can be swapped later.

---

# 10. Background Size

Recommended title background size:

```text
1920 x 1080
16:9
```

Important visual elements should be inside the central safe area.

Safe area recommendation:

```text
Keep important text/logo/icons inside the center 80% of the image.
Edges may be cropped on some displays.
```

The UI should use Canvas scaling.

Recommended Canvas Scaler:

```text
UI Scale Mode: Scale With Screen Size
Reference Resolution: 1920 x 1080
Screen Match Mode: Match Width Or Height
Match: 0.5
```

---

# 11. Scene Loading

When the player starts the game from the Main Title Screen:

```text
Load Stage 01 scene
```

Use the actual project scene name for Stage 01.

If the exact scene name is uncertain, expose it as an Inspector string.

Recommended field:

```text
[SerializeField] private string stage01SceneName = "Stage01_Prototype";
```

Do not hardcode a scene name without allowing easy Inspector adjustment.

---

# 12. Input Implementation Notes

If using the existing `PeckBeatInput`, make sure the Title Scene can distinguish whether the input came from:

```text
Keyboard
Gamepad / Xbox
```

If the existing input abstraction does not expose device source, add only the minimum required Title-specific detection.

Possible approach:

- Use Unity Input System directly in TitleManager for this scene only.
- Check keyboard keys D / J.
- Check Gamepad buttons mapped to ActionA / ActionB.

Do not rewrite Stage 01 or Stage 02 input logic.

Do not modify gameplay behavior.

---

# 13. Keyboard / Mouse Mode

Despite the label “Keyboard / Mouse,” the minimum required implementation only needs keyboard buttons.

Required:

```text
D = ActionA
J = ActionB
```

Mouse click support is optional.

Do not delay implementation for mouse click support.

If mouse click support is simple, it may be added later as polish.

---

# 14. Xbox Adaptive Controller Mode

The Xbox Adaptive Controller should be treated as a Gamepad-compatible device.

Expected logical mapping:

```text
Left large button  = ActionA
Right large button = ActionB
```

If the controller is not detected as expected, this is a hardware/Input System setup issue, not a Title Scene rule change.

---

# 15. Text by Language

## Input Select

Korean:

```text
조작 방식을 선택하세요
A: 이동 / B: 선택
```

English:

```text
Select Input Method
A: Move / B: Select
```

## Language Select

Korean:

```text
언어를 선택하세요
A: 이동 / B: 선택
```

English:

```text
Select Language
A: Move / B: Select
```

## Main Title

Korean:

```text
아무 버튼이나 눌러 시작
```

English:

```text
Press Any Button to Start
```

---

# 16. What Not To Build

Do not add these in the first pass:

```text
Options menu
Stage select
Save/load
Complex localization framework
Animated cutscene
Advanced transition system
Settings screen
Audio volume sliders
Controller rebinding UI
Mouse-only UI dependency
Complex menu framework
```

Keep the scene small.

---

# 17. MVP Definition

The Title Scene MVP is complete when:

- The scene opens to a black input-selection screen.
- The player can switch focus with ActionA.
- The player can confirm with ActionB.
- Both keyboard and Xbox input are accepted on the first screen.
- After choosing input type, only that input type is accepted.
- The language selection screen appears.
- Korean / English can be selected.
- The main title screen appears.
- The prompt text changes based on selected language.
- Pressing a valid button starts Stage 01.
- Pressing an invalid device after input lock does nothing.
- No Stage 01 / Stage 02 gameplay code is broken.

---

# 18. Deadline Priority

Build in this order:

```text
1. State machine works
2. Keyboard input works
3. Gamepad input works
4. Input lock works
5. Language selection works
6. Main title starts Stage 01
7. Placeholder icons/logo
8. Final art replacement
9. Focus polish
```

Do not block implementation waiting for final art.

---

# 19. Final Summary

Title Scene flow:

```text
Black screen
↓
Choose input method
[Xbox Adaptive Controller] / [Keyboard Mouse]
↓
Chosen input mode becomes locked
↓
Choose language
[한국어] / [English]
↓
Main Title
↓
Any valid selected-device button
↓
Stage 01
```

Core controls:

```text
ActionA = Move focus
ActionB = Confirm
```

First screen accepts both device types.

After input method selection, only the selected device type should control the title flow.

Keep it simple and stable.
