# PurrUI

A Unity UI framework by the PurrNet team featuring procedural UI rendering, view management, and UI pooling.


<img width="1034" height="491" alt="image" src="https://github.com/user-attachments/assets/d3abc18b-0a5f-4752-9092-c6c233bfbf2d" />

<img width="680" height="363" alt="image" src="https://github.com/user-attachments/assets/e75fbd28-3cd4-4579-b39f-be524e8d0c22" />

## Features

- **Procedural UI** - SDF-based rendering with `RectangleGraphic` and `GlowGraphic` (for glow/shadows)
- **Material Icons** - Simple to use material icon support for textmeshpro
- **View Management** - `ViewStack` and `ViewCollection` for managing UI views with transition support
- **Color Palette** - Theme your UI from a single asset; `ColoredGraphic` applies slots to any graphic with smooth transitions
- **Sounds2D** - Lightweight 2D audio system with a fluent API for fire-and-forget sound effects

## Installation

### Unity Package Manager (Git URL)

**Latest release:**
```
https://github.com/PurrNet/PurrUI.git?path=Assets/PurrUI#release
```

**Latest development:**
```
https://github.com/PurrNet/PurrUI.git?path=Assets/PurrUI#dev
```

# Procedural UI

<img width="553" height="718" alt="image" src="https://github.com/user-attachments/assets/fcb56d3e-8791-4d87-800b-6373a0d6d4f9" />

<img width="542" height="222" alt="image" src="https://github.com/user-attachments/assets/e02617f5-d104-4122-86c6-eaeb97382f1d" />

# Material Icons

<img width="374" height="251" alt="image" src="https://github.com/user-attachments/assets/f8417017-f4e4-4f61-852f-3edad7361264" />

```
Hello world
<icon=reddit><icon=gamepad_circle><icon=gamepad_variant>
<icon=gamepad_square><icon=keyboard_f1><icon=keyboard_return>
```

To use Material Icons you simply need to follow the example above `<icon=your_icon_name>` and you also need to add the `MaterialIconProcessor` component to your text.

<img width="547" height="287" alt="image" src="https://github.com/user-attachments/assets/8cfb3f24-ca29-42dc-a851-c887c8bda9fd" />

This `MaterialIconProcessor` allows the icons to work but it also gives you a handy little search bar to search for icons so you never get lost.

<img width="544" height="337" alt="image" src="https://github.com/user-attachments/assets/6c152d58-8247-4686-949d-fb9e25bf42f4" />

The `Copy` button will give you something like this directly: `<icon=account_alert>`.

# View System Documentation

PurrUI provides a stack-based view navigation system for Unity. Views are managed through a `ViewStack` that handles pushing, popping, ordering, visibility, and animated transitions.

All classes live in the `PurrNet.UI` namespace.

## Core Concepts

| Class | Type | Purpose |
|---|---|---|
| `ViewStack` | MonoBehaviour | Navigation controller that manages a stack of views |
| `MonoView` | MonoBehaviour | Base class for all views |
| `ViewCollection` | ScriptableObject | Asset that holds references to view prefabs |
| `ViewTransitions` | Static class | Built-in transition animations (fade, slide, etc.) |

## Setup

### 1. Create a ViewCollection

Right-click in your Project window and select **Create > PurrNet > View Collection**.

A `ViewCollection` is a ScriptableObject that stores references to your view prefabs. It has two modes:

- **Auto Generate** (default): Assign folders to watch and the collection automatically discovers all `MonoView` prefabs inside them. It refreshes whenever assets change.
- **Manual**: Disable `autoGenerate` and drag prefabs into the `views` array yourself.

<img width="551" height="353" alt="image" src="https://github.com/user-attachments/assets/74ae1e77-79d3-4248-bc1f-7f54b3332b05" />

### 2. Create View Prefabs

Each view is a prefab with a `MonoView` (or subclass) component on its root GameObject. `MonoView` automatically requires a `Canvas` and `CanvasGroup`, so those are added for you.

<img width="553" height="567" alt="image" src="https://github.com/user-attachments/assets/62c9f210-9c3e-4415-b665-43528ced7622" />

### 3. Add a ViewStack to Your Scene

Add a `ViewStack` component to a GameObject. In the inspector, configure:

- **Parent**: The transform where instantiated views will be parented (defaults to the ViewStack's own transform).
- **Prefabs**: Your `ViewCollection` asset.
- **Push On Start** (optional): A view to automatically push when the scene starts.
- **Order Offset**: An offset applied to canvas sorting order values.

<img width="552" height="133" alt="image" src="https://github.com/user-attachments/assets/cc3e6703-4860-4a38-9dd9-8f0c37cca6ae" />

## Creating Custom Views

Subclass `MonoView` to create your own views:

```csharp
using PurrNet.UI;
using UnityEngine;

public class ProfileView : MonoView
{
    [SerializeField] private TMPro.TMP_Text _username;
    [SerializeField] private TMPro.TMP_Text _bio;

    public void Setup(string username, string bio)
    {
        _username.text = username;
        _bio.text = bio;
    }
}
```

### The `Setup` Convention

We follow a convention of adding a `Setup` method to views that need initialization data. This is **not enforced** by the base class, it's simply a pattern. The idea is that `Setup` is called immediately after pushing:

```csharp
_stack.Push<ProfileView>().Setup("Jane", "Hello world!");
```

All built-in views provided by PurrUI follow this pattern. You are encouraged to do the same for consistency, but it is not required.

## Pushing and Popping Views

### Pushing

There are two ways to push a view onto the stack:

```csharp
// Generic push: looks up the prefab by type from the ViewCollection
var view = _stack.Push<DialogView>();

// Direct prefab push
var view = _stack.Push(someViewPrefab);
```

When a view is pushed:
1. The current top view (if any) is moved to the background (raycasts disabled).
2. The prefab is instantiated under the configured parent transform.
3. The view is initialized and assigned a sorting order.
4. If the view defines an enter transition, it plays (raycasts are blocked until it completes).

### Popping

```csharp
// Pop the top view
_stack.Pop();

// Pop a specific view instance (regardless of position in the stack)
_stack.Pop(viewInstance);

// From inside a view, call CloseMe() (can be wired to a button in the inspector)
CloseMe();
```

When a view is popped:
1. If the view defines an exit transition, it plays before the view is destroyed.
2. The new top view is moved to the foreground (raycasts re-enabled, `OnBecomeForeground` called).

### Other Stack Operations

```csharp
// Remove all views from the stack (exit transitions play for each)
_stack.Clear();

// Move an existing view to the top of the stack
_stack.MoveToTop(viewInstance);
_stack.MoveToTop<DialogView>();
```

## Transitions

Override `OnEnterTransition` and `OnExitTransition` in your `MonoView` subclass to define animated transitions. They return `IEnumerator` coroutines.

```csharp
public class MyView : MonoView
{
    [SerializeField] private RectTransform _content;

    protected override IEnumerator OnEnterTransition()
    {
        return ViewTransitions.FadeIn(this);
    }

    protected override IEnumerator OnExitTransition()
    {
        return ViewTransitions.FadeOut(this);
    }
}
```

### Built-in Transitions

`ViewTransitions` provides ready-to-use animations. All use smoothstep easing and unscaled time (unaffected by `Time.timeScale`). Default duration is `0.2s`.

**Fade:**
```csharp
ViewTransitions.FadeIn(view, duration)
ViewTransitions.FadeOut(view, duration)
ViewTransitions.Fade(canvasGroup, fromAlpha, toAlpha, duration)
```

**Slide:**
```csharp
ViewTransitions.SlideFromLeft(content, duration)
ViewTransitions.SlideToLeft(content, duration)
ViewTransitions.SlideFromRight(content, duration)
ViewTransitions.SlideToRight(content, duration)
ViewTransitions.SlideFromTop(content, duration)
ViewTransitions.SlideToTop(content, duration)
ViewTransitions.SlideFromBottom(content, duration)
ViewTransitions.SlideToBottom(content, duration)
```

Slide methods operate on a `RectTransform` (typically a child content container, not the root view), using `anchoredPosition` and the rect's own width/height to calculate offsets.

### Combining Transitions

Use `ViewTransitions.Parallel` to run multiple transitions at the same time:

```csharp
protected override IEnumerator OnEnterTransition()
{
    var fade = ViewTransitions.FadeIn(this);
    var slide = ViewTransitions.SlideFromLeft(_content);
    return ViewTransitions.Parallel(fade, slide);
}

protected override IEnumerator OnExitTransition()
{
    var fade = ViewTransitions.FadeOut(this);
    var slide = ViewTransitions.SlideToRight(_content);
    return ViewTransitions.Parallel(fade, slide);
}
```

## View Lifecycle & Visibility

- **Sorting Order**: Each view in the stack is assigned an incrementing sorting order (plus `_orderOffset`), so views higher in the stack render on top.
- **Raycasts**: Only the top view receives raycasts. Views below the top have raycasts disabled.
- **Cull Windows Behind**: If a view has `cullWindowsBehind` enabled, all views below it in the stack have their `Canvas` disabled entirely. Useful for fullscreen views where nothing behind them is visible.
- **OnBecomeForeground**: Override this virtual method in your `MonoView` subclass to react when the view returns to the top of the stack (e.g., after the view above it is popped).

# Color Palette

A theming system for UI. Define a `ColorPalette` once, drive any `Graphic` from it with `ColoredGraphic`, and cascade the palette down the hierarchy through `IPaletteProvider`. Changes propagate live, with optional smooth transitions between colors.

## ColorPalette

Right-click in your Project window and select **Create > PurrNet > PurrUI > Color Palette**. The asset exposes nine slots grouped into **Base** (`Black`, `White`, `Muted`), **Backgrounds** (`Background`, `Surface`), **Primary** (`Accent`), and **Status** (`Success`, `Warning`, `Danger`). Each slot (except Base) also has a dedicated contrast color - the foreground used for text or content rendered on top of it.

Color Palette Scriptable Object:
<img width="402" height="739" alt="image" src="https://github.com/user-attachments/assets/364494b0-a5d9-4978-abc5-b9100ddd65a1" />

The inspector includes a live mock-UI preview built from rectangles so you can see how the slots land together as you tweak them - useful for catching low-contrast pairings at a glance.

## Shape and interaction styles

Each palette also contains **Styles** for button, panel, input and item roundness, button/toggle/input transition durations, disabled button opacity, pressed button scale, and focused input outline width. HeroUI source prefabs inherit these settings by default. The supplied defaults preserve HeroUI's pill buttons, 16 px panels and 10 px inputs.

On a `RectangleGraphic`, select a **Roundness Role** to inherit the corresponding theme value. **Custom** retains the local maximum, uniform or individual corner settings. Full roundness follows the element's size, while pixel roundness stays fixed. Circular icons, tracks and existing explicit prefab overrides retain their local shape. Control inspectors expose **From Theme** checkboxes; turn one off to customize that value for a single element.

Styles use the same `PaletteProvider`/`ViewStack` hierarchy as colors. Edit the palette asset to update its users, or update a copy at runtime:

```csharp
var styles = palette.styles;
styles.buttonRoundness = CornerRoundness.Pixels(12f);
styles.panelRoundness = CornerRoundness.Pixels(20f);
styles.buttonTransitionDuration = 0.1f;
styles.inputFocusWidth = 3f;
palette.SetStyles(styles);
```

`SetStyles` notifies existing users. Running transitions keep their progress, and local overrides remain intact. These APIs and controls live in the core `PurrNet.UI` namespace.

## ColoredGraphic

Add a `ColoredGraphic` next to any `UI.Graphic` (`Image`, `RawImage`, `TMP_Text`, etc.) to drive its color from the active palette.

Colored Graphic Component:
<img width="402" height="318" alt="image" src="https://github.com/user-attachments/assets/103486d4-2121-4bb3-8676-06183b564c99" />

Pick a slot via the dropdown. Toggle **Contrast** to use that slot's paired foreground instead (e.g. `Accent` with contrast on = the accent foreground color).

For multi-colored targets - components implementing `IColored` - the inspector lists one row per named key with its index (`[0] Background`, `[1] Text`, etc.). Each row has an enable checkbox so you opt in only to the slots you want to override; untouched slots keep whatever color the target already had.

### Transitions

`ColoredGraphic` has a `Transition Duration` field. Any palette change - either editing the asset or calling the runtime API - smoothly lerps from the current color to the new target. Set to `0` to snap. Edit-mode changes always snap (for a stable editor preview without needing continuous repaints).

### Runtime API

```csharp
// Single-graphic target
coloredGraphic.SetColor(new ColorInfo { enabled = true, color = ColorType.Accent });

// Specific slot on an IColored target
coloredGraphic.SetColor(1, new ColorInfo { enabled = true, color = ColorType.Background, contrast = true });

// Tweak the blend duration at runtime
coloredGraphic.transitionDuration = 0.25f;
```

Your own state machine (hover/pressed/etc.) can push new `ColorInfo`s and the transition system handles the visual blend.

## Providers

`ColoredGraphic` walks up the hierarchy via `GetComponentInParent<IPaletteProvider>()` to find the active palette:

- `ViewStack` already implements `IPaletteProvider` - the palette you configure on it cascades into every view pushed onto the stack.
- Drop a `PaletteProvider` component on any child GameObject to override the palette for that subtree (e.g., a "dark card" inside an otherwise-light scene).

The closest provider wins, so nested providers work naturally for scoped theme overrides.

# Sounds2D

A lightweight, fire-and-forget 2D audio system. It manages a small pool of `AudioSource` components (up to 5) on a `DontDestroyOnLoad` GameObject, so you never have to wire up audio sources yourself.

## Quick Start

```csharp
using PurrNet.UI;
using UnityEngine;

public class ButtonSFX : MonoBehaviour
{
    [SerializeField] private AudioClip _clickSound;

    public void OnClick()
    {
        Sounds2D.Play(new AudioSession(_clickSound));
    }
}
```

## AudioSession

`AudioSession` is a struct that describes *what* to play and *how*. It uses a fluent builder API:

```csharp
// Simple: play a clip at default volume and pitch
Sounds2D.Play(new AudioSession(clip));

// With volume and pitch
Sounds2D.Play(new AudioSession(clip).WithVolume(0.8f).WithPitch(1.2f));

// Random variation: adds ± randomRange to the base value each time
Sounds2D.Play(new AudioSession(clip).WithVolume(0.7f, 0.1f).WithPitch(1f, 0.15f));

// Random clip from an array (great for footsteps, impacts, etc.)
AudioClip[] hitSounds = { hit1, hit2, hit3 };
Sounds2D.Play(new AudioSession(hitSounds).WithVolume(0.9f));
```

## Master Volume

```csharp
// Set a global master volume (0-1) that scales all sounds
Sounds2D.masterVolume = 0.5f;
```
