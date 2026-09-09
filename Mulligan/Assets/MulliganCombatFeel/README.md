# Mulligan Combat Feel

A small, self-contained attack-presentation kit — the "feel" from `CombatLab.unity`
(anticipation → lunge → fire slash → impact → hit-stop → camera punch → target reaction →
recovery), pulled out so you can drop it into another project.

**Fully self-contained.** No third-party packs, no external references. 23 files, ~1200
lines of script. Built-in Render Pipeline, Unity 2022.3.

---

## Test it here first

| Scene | Setup |
|---|---|
| `Scene/CombatFeelTest.unity` | world-space `SpriteRenderer` attacker/target |
| `Scene/CombatFeelTest_UI.unity` | **UGUI `Image`** attacker/target on a Screen-Space-Camera canvas — the feel only, no fire visual |

Open one, press **Play**, then **Space** = attack, **R** = reset (or the on-screen buttons).
Blue rectangle = attacker, green = target (placeholder art — you swap these).

---

## Import into your other project

1. Copy the whole **`MulliganCombatFeel/`** folder into that project's `Assets/`.
2. Let it compile. That's it — the scripts land in `Assembly-CSharp`, namespace
   `MulliganCombatFeel`.
3. Open `Scene/CombatFeelTest.unity` there to confirm it plays.

If your other project uses the **new Input System**, `Input.GetKeyDown` in
`VFXLabManualTrigger` will throw. Either set *Project Settings ▸ Player ▸ Active Input
Handling* to **Both**, or delete `VFXLabManualTrigger` and call `VFXLabController.Play()`
yourself (see below).

---

## Wire it into your game

You need, in your combat scene:

| GameObject | Component | Notes |
|---|---|---|
| your camera, **or** a container transform | `VFXLabCameraController` | `shakeTarget` = what gets shaken. World: the camera. **UI: a `RectTransform` that wraps the combatants** (shaking just them, not the whole screen — cleaner). Keep the "home" pose on a parent; this offsets `shakeTarget` and always returns it. |
| your enemy / target | `VFXLabTarget` | **World:** assign `bodyRenderer` + optional `flashOverlay`. **UI:** assign `bodyGraphic` (its `Image`) + optional `flashOverlayGraphic` (a white `Image` on top, starts inactive). Whichever pair you assign is what it uses. |
| an empty "CombatDirector" | `VFXLabController` | assign `attacker`, `target`, `cameraController`, `impactPoint`, `effectOrigin`, `labCamera`, and `currentEffectPrefab`. **World:** `= VFX_CombatFeel_Slash`. **UI / feel-only:** `= VFX_CombatFeel_FeelOnly` (no visual, just the sequencer). Optional bg dim: `bgSuppressQuad` (world quad) **or** `bgSuppressGraphic` (a full-screen dark `Image` between background and combatants, starts inactive). |
| (optional) same object | `VFXLabManualTrigger` | Space / R / on-screen buttons for testing. Delete for production. |

Then fire an attack from your own code:

```csharp
myVFXLabController.Play();      // full sequence: anticipation → … → recovery → auto-reset
// or, to grab a specific frame for a screenshot / cutscene:
myVFXLabController.PlayAndPauseAtPhase("Impact");
```

`Play()` spawns `currentEffectPrefab` at `effectOrigin`, runs the phase sequence, and cleans
itself up. It touches only the transforms/renderers you assigned — **no gameplay, no health,
no damage.** Call your damage logic wherever you want (typically right when you call
`Play()`, or hook `VFXLabEffectPlayer.onImpact`).

### Attacker lunge

`VFXLabController` auto-injects your `attacker` transform into the spawned effect so the
attacker pulls back during anticipation and lunges on release. Tune the distances on the
effect prefab ▸ `VFXLabEffectPlayer` ▸ `attackerAnticipationBy` / `attackerLungeBy`. The
attacker is moved by **localPosition**, so parent it under a rig if its world position
matters (UI: it moves the `RectTransform`, so keep it free of a layout group).

> **Units differ between world and UI.** `VFX_CombatFeel_Slash` (world) uses metres —
> `anticipationBy (-1.1,-0.2,0)`, `lungeBy (2.6,0.1,0)`, target `knockback 0.6`, camera
> `positionalPunch 0.35`. `VFX_CombatFeel_FeelOnly` (UI) is pre-tuned in **pixels** —
> `lungeBy (130,6,0)`, and the UI test scene uses target `knockback 70`, camera
> `positionalPunch 22`. Adjust to your resolution / `CanvasScaler`.

---

## What each script does

| Script | Role |
|---|---|
| `VFXLabController` | The conductor. Owns `Time.timeScale` (playback speed + hit-stop), background suppression, spawns/cleans the effect, exposes `Play()` / `ResetLab()`. Has extra capture helpers (`PlayAndPauseAtPhase`, speed presets) you can ignore or delete. |
| `VFXLabEffectPlayer` | The per-attack sequencer: 4 phases (**Anticipation, Release, Impact, Recovery**) on a scaled-time timeline, plus tail. Fires the attacker lunge, particles, and — at Impact — the target reaction + camera punch + hit-stop. UnityEvent hooks per phase. Lives on the effect prefab. |
| `VFXLabTarget` | Target hit reaction: flash + squash/stretch + directional knockback + rotation punch, eased return. Works with a world `Renderer` (flash via MaterialPropertyBlock) **or** a UGUI `Graphic` (flash via `Image.color`). |
| `VFXLabCameraController` | Directional "camera" punch: recoil opposite the hit + a little roll + Perlin jitter, curve falloff, always returns to base. Just offsets whatever `Transform`/`RectTransform` you give it — works for a real camera or a UI container. |
| `HeavyFireSlashDriver` | Drives the fire-slash visual (the `MulliganCombatFeel/HeavyFireHero` shader) — directional reveal / burn / heat-front, impact star, anticipation glow, spark + ember bursts. Wired to the phase events. This is the swappable "what the attack looks like" layer. |
| `VFXLabManualTrigger` | Space / R / on-screen buttons + optional auto-repeat. Testing only. |

---

## Timing (on `VFX_CombatFeel_Slash` ▸ `VFXLabEffectPlayer`)

Current values (seconds from Play, scaled time):
`anticipation 0.10` · `release 0.23` · `impact 0.37` · `recovery 0.70` · `tail 0.22`
`intensity 0.92` (scales target reaction + camera + hit-stop) · `hitStopSeconds 0.12`

Snappier = pull `release`/`impact` closer together and shorten `anticipation`.
Heavier = raise `intensity`, `hitStopSeconds`, and the camera/target punch values.

---

## Swapping the visual

The *feel* is the 4 script layers above — it does not care what the effect looks like.
`VFX_CombatFeel_Slash` is just the prefab assigned to `currentEffectPrefab`. To use your own
slash/impact art:

- Easiest: keep the prefab, replace the 5 textures in `Textures/` (same names) or retint the
  materials in `Materials/`.
- Or: build your own effect prefab. It needs a `VFXLabEffectPlayer` at its root with the
  phase UnityEvents wired to whatever drives your art. `HeavyFireSlashDriver` is one example
  of such a driver; you can delete it and wire particles/animations directly to
  `onAnticipation / onRelease / onImpact / onRecovery / onReset`.

---

## Using it with UGUI `Image` (Screen-Space-Camera / World-Space canvas)

The feel layer needs **no rework** — `VFXLabTarget`, `VFXLabCameraController` and the
attacker lunge all animate `localScale/localPosition/localRotation`, which behave the same on
a `RectTransform`. Do this:

1. `currentEffectPrefab` = **`VFX_CombatFeel_FeelOnly`** (the fire-slash prefab's particles +
   mesh won't render inside a canvas — use your own slash `Image` / flipbook and hook it to
   `VFXLabEffectPlayer.onImpact` / `onRelease` if you want a visual).
2. On `VFXLabTarget`, assign `bodyGraphic` (not `bodyRenderer`) and, if you want the flash,
   `flashOverlayGraphic` = a white `Image` copy on top (starts inactive).
3. Put `VFXLabCameraController` on a `RectTransform` that wraps attacker + target, set
   `shakeTarget` to that same rect.
4. Bg dim (optional): a full-screen dark `Image` behind the combatants → `bgSuppressGraphic`.
5. Raise the pixel-scale values (see the units note above).

`Scene/CombatFeelTest_UI.unity` is exactly this setup, working.

**Screen-Space-Overlay** is the one case that needs more: there's no camera to see world
effects and particles don't render over an overlay canvas. The *feel* still works as above;
only a world-space slash visual would need a UI rebuild.

---

## Notes / known rough edges

- **Placeholder art**: `Scene/_Placeholders/` — plain rectangles. Delete once you've wired
  your own attacker/target.
- **UGUI required** for the `Image` path (`com.unity.ugui`, on by default).
- **Anticipation glow** is subtle at the current tuning; raise `HeavyFireSlashDriver.
  glowIntensity` / `glowMaxScale` if you want a stronger wind-up tell.
- **Fire slash art** is stylised flame (this was the `HeavyFireSlash` benchmark). If it
  doesn't match your game, swap the visual as above — the feel stays.
- `VFXLabController` keeps a `static Instance`; if you have more than one combat director
  active at once, only the last-enabled one is `Instance` (fine for turn-based / one duel at
  a time).
- Everything is presentation-only and frame-rate-independent (phases use a clamped scaled
  dt). Hit-stop and playback speed drive `Time.timeScale` globally — expected for a duel
  screen; if your game needs the rest of the world to keep moving, drive these per-object
  instead.
