# VACAC Conveyor System — Development Notes



## Project



VACAC Graduate Software Engineer Take-Home Technical Task 2026



The task is to create a modular conveyor system in Unity using the provided

conveyor models and simulate the provided products moving along the conveyors.



The minimum requested functionality is:



- Windows-compatible build

- Select and place conveyors into an environment

- Snap conveyors together to form a continuous conveyor line

- Move products along the conveyors



The task is intentionally open-ended and allows additional functionality.



---



# Development Log



## 1. Initial Unity Setup



Created a new Unity 3D project.



Imported the supplied VACAC `.unitypackage`.



The supplied assets include:



### Conveyor Models



- `VA_Conveyor_Belt_Generic.fbx`

- `VA_Conveyor_Belt_Short.fbx`

- `VA_incline_Conveyor_Belt.fbx`



### Product Models



- `VA_Cardboard_Box.fbx`

- `VA_Slim_Can.fbx`



The supplied FBX models use imported transforms that are not ideal for

application-level logic, including non-1 scale values and imported rotation.



Rather than modifying the supplied models directly, clean parent GameObjects

were created around them.



This keeps the original VACAC assets unchanged.



---



# 2. Generic Conveyor Prefab



Created:



`Conveyor_Generic`



Structure:



Conveyor_Generic

├── VA_Conveyor_Belt_Generic

├── Snap_Start

├── Snap_End

├── Path_Start

└── Path_End



The supplied FBX remains the visual model.



`Conveyor_Generic` acts as the clean application-level object with:



- Position: 0, 0, 0

- Rotation: 0, 0, 0

- Scale: 1, 1, 1



This makes runtime placement and snapping easier to manage.



---



# 3. Conveyor Connection Points



Initially created:



- `Snap_Start`

- `Snap_End`



These transforms represent the physical/logical connection points used when

joining conveyor modules.



They are separate child GameObjects rather than relying on the FBX pivot.



The intended logical direction is:



Snap_Start → Snap_End



The Start point is positioned on the beginning side of the conveyor and the

End point on the outgoing side.



Current snap-point height is approximately:



Y = 0.44



This was chosen based on the physical conveyor geometry.



---



# 4. Product Movement Path



Initially product movement reused the conveyor snap points.



This caused a problem because the snap-point height was suitable for connecting

conveyors but not necessarily suitable for placing products on the belt surface.



The design was therefore improved by introducing separate movement points:



- `Path_Start`

- `Path_End`



The architecture is now:



Snap_Start / Snap_End

= conveyor-to-conveyor connection



Path_Start / Path_End

= product movement route



This separation will also make it easier to support the supplied incline

conveyor later.



---



# 5. Conveyor Component



Created:



`Assets/Scripts/Conveyor.cs`



The component stores references to:



- Snap Start

- Snap End

- Path Start

- Path End



The script also draws editor Gizmos to make these points easier to debug.



Gizmo colours:



- Green = Snap_Start

- Red = Snap_End

- Yellow = Path_Start

- Cyan = Path_End



These Gizmos are development/debugging tools and are not intended to appear

in the final application build.



---



# 6. Conveyor Prefab



Converted `Conveyor_Generic` into a reusable Unity prefab.



Prefab:



`Assets/Prefabs/Conveyor_Generic.prefab`



The prefab contains:



- supplied VACAC conveyor model

- Snap_Start

- Snap_End

- Path_Start

- Path_End

- Conveyor component



All four Transform references are assigned directly on the prefab.



This is important because conveyors instantiated at runtime must inherit these

references correctly.



---



# 7. Ground Environment



Created a Unity Plane and renamed it:



`Ground`



This acts as the environment onto which conveyors can be placed.



The Ground object contains a collider used by the placement system for

mouse-ray intersection.



---



# 8. Camera Setup



Adjusted the Main Camera to provide an elevated builder/simulation view.



The intention is to give the user enough visible workspace to construct

multiple conveyor sections while still being able to see the conveyor models

clearly.



Further camera controls such as pan, rotate and zoom may be added later.



---



# 9. Conveyor Placement System



Created:



`Assets/Scripts/PlacementManager.cs`



Created scene GameObject:



`PlacementManager`



The PlacementManager currently references:



- Main Camera

- Conveyor Generic Prefab

- Ground Collider



Current interaction:



- Press `1` to begin placing a generic conveyor

- Move mouse over the Ground to move the conveyor preview

- Left-click to place a valid conveyor

- Press `Esc` to cancel placement



The first conveyor can be placed freely anywhere on the ground.



Once a conveyor already exists, additional conveyors are no longer allowed to

be placed as disconnected free-floating sections.



For subsequent placement, the preview must successfully snap to a valid

conveyor endpoint before the left-click placement is accepted.



The placement system therefore supports an empty starting scene while also

protecting the runtime layout from disconnected conveyor sections.



---


# 10. Conveyor Snapping



Implemented automatic snapping between conveyor modules.



The snapping system now supports extending a conveyor line from either end.



Two valid endpoint relationships are checked:



Forward extension:



Existing Conveyor Snap_End → New Conveyor Snap_Start



Backward extension:



New Conveyor Snap_End → Existing Conveyor Snap_Start



The placement system compares both endpoint combinations and selects the

closest valid connection within the configured snapping distance.



When a valid snap is found:



- the preview is repositioned so the relevant connection points overlap

- the preview inherits the exact rotation of the connected conveyor

- the conveyor line remains straight and continuously connected



This allows the user to extend the same logical conveyor line from either the

left or right side without reversing Start/End semantics.



Disconnected placement is rejected once the first conveyor exists.



Current intended layout:



Snap_Start ================= Snap_End

                               ↑

                               ↓

Snap_Start ================= Snap_End



Each connection preserves the logical travel direction:



Snap_Start → Snap_End



---


# 11. Conveyor Direction Issue



During development the original Start and End points were oriented opposite

to the desired user-facing build direction.



This caused:



- newly placed conveyors to extend in the unexpected direction

- product movement to appear reversed

- some attempted changes to create overlapping conveyor instances



The final decision was NOT to reverse references in code.



Instead, the prefab was corrected so that the actual Transform objects match

their semantic names:



- Snap_Start is physically at the start

- Snap_End is physically at the end

- Path_Start is physically at the start

- Path_End is physically at the end



The Inspector references remain intuitive:



Snap Start → Snap_Start

Snap End → Snap_End

Path Start → Path_Start

Path End → Path_End



This keeps the code easier to understand and explain.



---



# 12. Product Prefab



Created a clean wrapper around the supplied cardboard box FBX.



Structure:



Product_Box

└── VA_Cardboard_Box



Created prefab:



`Assets/Prefabs/Product_Box.prefab`



As with the conveyor, the wrapper keeps runtime transforms separate from the

supplied FBX import transforms.



---



# 13. Product Movement



Created:



`Assets/Scripts/ProductMover.cs`



The product currently:



1. Starts at the current conveyor's Path_Start

2. Moves toward Path_End

3. Uses configurable movement speed

4. Uses a configurable vertical offset so the model visually sits on the belt



Current movement speed:



0.3 units/second



The product movement uses:



`Vector3.MoveTowards`



rather than physical conveyor simulation.



This was intentionally chosen as a simple and deterministic solution for the

technical assessment.



---



# 14. Product Height



Originally the product movement used Snap_Start and Snap_End.



Because these points were above the desired product travel height, the

cardboard box appeared to float above the conveyor.



Introducing Path_Start and Path_End solved the architectural issue.



A small `heightOffset` remains available in ProductMover for final visual

adjustment.



---



# 15. Multi-Conveyor Product Movement



ProductMover was extended to search for a connected conveyor when the product

reaches Path_End.



It checks whether:



Current Conveyor Snap_End



is sufficiently close to:



Another Conveyor Snap_Start



If a connected conveyor is found, that conveyor becomes the current conveyor

and the product continues from its Path_Start.



The goal is:



Conveyor 1 → Conveyor 2 → Conveyor 3 → ...



with the product travelling continuously through the assembled conveyor line.



---



# 16. Current User Flow

Current intended runtime interaction:

1. Application starts with an empty Ground.
2. Press `1` to place a Generic conveyor, `2` for Short, or `3` for Incline.
3. The selected conveyor appears as a runtime preview following the Ground.
4. The first conveyor can be placed freely.
5. Every later conveyor must snap to a free endpoint before placement is accepted.
6. The placement system supports adding the new conveyor after an existing conveyor or before an existing conveyor.
7. Incline and elevated sections can be discovered while the preview remains on the Ground; final placement still applies the complete XYZ offset.
8. Press `4` to spawn a cardboard box.
9. ProductSpawner identifies the first conveyor in the connected line and initializes ProductMover at its `Path_Start`.
10. Press `Esc` to cancel an active conveyor preview.

Current controls:

| Key | Action |
|---|---|
| `1` | Place Generic conveyor |
| `2` | Place Short conveyor |
| `3` | Place Incline conveyor |
| `4` | Spawn Cardboard Box |
| `Esc` | Cancel active placement |

---

# 17. Git / Version Control



Git repository created at the Unity project root.



Tracked project directories:



- Assets

- Packages

- ProjectSettings



Ignored Unity-generated directories include:



- Library

- Temp

- Logs

- Builds

- UserSettings



Unity `.meta` files are intentionally committed.



Git LFS has been enabled because several supplied VACAC FBX files are very

large.



Configured:



`\*.fbx filter=lfs diff=lfs merge=lfs -text`



Large supplied assets include conveyor FBX files around 239–241 MB and the

cardboard box FBX around 126 MB.



---



# 18. Runtime Product Spawning



The original test workflow used a Product_Box instance that was permanently

placed in the scene and manually linked to a conveyor.



This was replaced with a runtime spawning system.



Created:



`Assets/Scripts/ProductSpawner.cs`



Created scene GameObject:



`ProductSpawner`



The Product_Box prefab now contains ProductMover directly and does not require

a Current Conveyor reference to be assigned in the prefab.



Current ProductMover defaults:



- Speed: 0.3

- Height Offset: 0.02

- Connection Tolerance: 0.05



At this historical stage, the spawn key was `2` (later moved to `4` after Short and Incline received their own selection keys). ProductSpawner:



1. Finds the first conveyor in the connected conveyor line

2. Instantiates the Product_Box prefab

3. Retrieves ProductMover from the new instance

4. Calls `Initialize(Conveyor)`

5. Places the product at the starting conveyor's Path_Start



This removes the dependency on a pre-placed product and allows multiple boxes

to be spawned during runtime.



The Unity 6 deprecated `FindObjectsByType` overload using

`FindObjectsSortMode.None` was also replaced with the current overload in the

placement and product-movement scripts.



---



# 19. Placement Validation Improvements



Testing exposed two important placement issues.



First, the original snap logic only supported extending the line in one

direction:



Existing Snap_End → New Snap_Start



This meant a conveyor placed visually before the original conveyor could look

connected but would not form the expected logical product route.



The snapping system was updated to support both valid connection directions:



- Existing Snap_End → New Snap_Start

- New Snap_End → Existing Snap_Start



Second, the preview could previously be placed anywhere on the Ground even

when it was not connected to the conveyor line.



This allowed disconnected conveyor sections that ProductMover could not

traverse.



PlacementManager now distinguishes between:



- first conveyor placement, which is free

- subsequent conveyor placement, which requires a successful snap



`TrySnapPreview()` now reports whether a valid connection was found, and

placement is accepted only when that condition is satisfied after the first

conveyor exists.



The connected conveyor's rotation is copied to the preview during snapping,

which intentionally keeps the current implementation as a straight continuous

conveyor line.



Verified behaviour:



- extend line from the right

- extend line from the left

- reject disconnected conveyor placement

- preserve straight-line orientation

- spawn products from the new first/leftmost conveyor

- move products continuously through the connected line



---

# 20. Baseline State at Commit `6fa72e32e0627fbf3d03106b3e533c18bd333ca5`

The post-incline tuning work documented below starts from commit:

`6fa72e32e0627fbf3d03106b3e533c18bd333ca5`

Commit title:

`fix: refine incline conveyor snap and path alignment`

At that point the project already contained Generic, Short and Incline conveyor prefabs, runtime selection for all three conveyor types, and runtime cardboard-box spawning.

The Incline prefab used the following tuned baseline transforms:

| Transform | Position |
|---|---|
| `Snap_Start` | `(0, 0.46, -0.05)` |
| `Snap_End` | `(0, 0.77, 0.77)` |
| `Path_Start` | `(0, 0.03, -0.05)` |
| `Path_End` | `(0, 0.34, 0.77)` |

The supplied incline FBX also retained the deliberate visual orientation correction required to make the model run in the intended low-to-high direction. The source FBX asset itself was not modified; the correction exists at prefab/instance level.

Although incline placement was functional at this commit, subsequent runtime testing exposed additional issues with elevated continuation and visual seam differences between the three supplied conveyor meshes.

---

# 21. Elevated Conveyor Continuation Fix

A placement bug was found after constructing a layout such as:

`Generic -> Incline -> Generic`

The Generic conveyor placed after the Incline was correctly elevated, but another Generic preview could not be attached after it.

Cause:

- normal flat-to-flat snap discovery used full XYZ distance
- the active preview continued to follow the Ground
- an already elevated flat conveyor therefore remained vertically too far from the Ground-following preview to enter the normal snap radius

PlacementManager was updated so endpoint discovery now distinguishes between normal ground-level flat placement and connections involving an incline or already elevated conveyor.

Current behaviour:

- Ground-level flat -> flat candidate detection uses full 3D distance
- connections involving an Incline use horizontal XZ distance for candidate discovery
- connections involving an already elevated flat conveyor also use horizontal XZ distance for candidate discovery
- the final snap always uses the complete XYZ offset
- endpoint occupancy checks always remain full XYZ

This means horizontal-only distance is used only to discover a candidate endpoint. It is not used as the final placement offset.

An `IsElevated()` check was added using the conveyor root Y position and `elevatedHeightThreshold`.

This allows sequences such as:

`Generic -> Incline -> Generic -> Generic -> Generic`

to continue at the elevated level while keeping the preview Ground-based before the snap is applied.

---

# 22. Conveyor Type Metadata and Adaptive Connection Calibration

Visual testing showed that one fixed `Snap_Start` / `Path_Start` Z position cannot produce a clean seam for every combination of the supplied meshes.

For example, the best start position for a new Generic after another Generic is different from the best start position for that same Generic after a Short or Incline conveyor.

To represent this explicitly, `Conveyor.cs` was extended with:

```csharp
public enum ConveyorType
{
    Generic,
    Short,
    Incline
}
```

Each conveyor prefab is assigned its matching type in the Inspector.

`Conveyor.cs` also gained helper behaviour for runtime calibration:

- `SetStartLocalZ(float z)` changes both `Snap_Start.localPosition.z` and `Path_Start.localPosition.z` together
- `GetSnapStartWorldPositionWithLocalZ(float z)` calculates a candidate world-space start position without permanently changing the preview first

The central calibration rule is now:

> Keep the existing conveyor's end geometry fixed and adapt the **new conveyor's start geometry** to the type of conveyor it is being attached after.

Therefore, when placing:

`Existing -> New`

PlacementManager selects an incoming start profile using both:

- existing conveyor type
- new conveyor type

Both `Snap_Start Z` and `Path_Start Z` are always changed together.

This prevents a visual seam adjustment from moving the snap point without moving the corresponding product path start.

---

# 23. Runtime 3 x 3 Incoming Z Calibration Matrix

These values were measured manually in Play Mode by adjusting the new conveyor until the supplied meshes visually joined as cleanly as possible.

They are important reference values if snapping/seam behaviour needs to be revisited later.

**The Z value in the table is applied to BOTH `Snap_Start` and `Path_Start` of the NEW conveyor.**

| Existing conveyor -> New conveyor | New conveyor start Z | Current observation |
|---|---:|---|
| Generic -> Generic | `-1.68` | working well |
| Short -> Generic | `-1.22` | working well |
| Incline -> Generic | `-1.50` | working very well / close to perfect |
| Generic -> Short | `-1.38` | approximate tuned value |
| Short -> Short | `-0.91` | approximate tuned value |
| Incline -> Short | `-1.22` | approximate tuned value; may still need fine tuning |
| Generic -> Incline | `-0.05` | working very well / currently treated as correct |
| Short -> Incline | `+0.2955` | approximate tuned value |
| Incline -> Incline | `-0.03` | belt surface appears straight and continuous |

Equivalent Inspector calibration fields in `PlacementManager`:

| Field | Value |
|---|---:|
| `genericAfterGenericStartZ` | `-1.68` |
| `genericAfterShortStartZ` | `-1.22` |
| `genericAfterInclineStartZ` | `-1.50` |
| `shortAfterGenericStartZ` | `-1.38` |
| `shortAfterShortStartZ` | `-0.91` |
| `shortAfterInclineStartZ` | `-1.22` |
| `inclineAfterGenericStartZ` | `-0.05` |
| `inclineAfterShortStartZ` | `+0.2955` |
| `inclineAfterInclineStartZ` | `-0.03` |

These values are calibration data, not universal physical dimensions. They were derived from the actual supplied meshes and the current wrapper/pivot setup.

Small future changes to prefab geometry, FBX orientation, wrapper transforms or endpoint positions may require re-measuring the table.

---

# 24. PlacementManager 3 x 3 Connection Matrix

PlacementManager was generalized from Generic-only adaptive calibration to a complete type-to-type incoming calibration matrix.

The system now supports separate start profiles for all nine combinations:

```text
Generic -> Generic
Generic -> Short
Generic -> Incline

Short   -> Generic
Short   -> Short
Short   -> Incline

Incline -> Generic
Incline -> Short
Incline -> Incline
```

During endpoint search:

1. The preview is reset to its canonical/same-type start profile.
2. PlacementManager calculates the candidate `Snap_Start` position for the relevant connection profile without permanently modifying the preview.
3. The closest valid free endpoint is selected.
4. If the connection is `Existing -> New`, the chosen incoming calibration is applied to the preview.
5. `Snap_Start` and `Path_Start` receive the same local Z value.
6. The final full XYZ snap offset is applied.

For reverse placement (`New -> Existing`), the preview connects with its `Snap_End`, so its adaptive start profile is not part of that particular join.

---

# 25. Preserve Runtime Calibration When Placing

A significant implementation detail was changed in `PlaceConveyor()`.

Previously, after positioning the preview, placement instantiated a fresh copy of the original selected prefab.

That would discard any runtime-adjusted `Snap_Start` and `Path_Start` values applied to the preview.

Placement now clones the calibrated preview instance instead.

Conceptually:

```csharp
Instantiate(preview, preview.transform.position, preview.transform.rotation);
```

This preserves the selected connection-specific start profile on the conveyor that is actually placed.

Without this change, the preview could look correctly aligned while the final placed conveyor silently reverted to its prefab defaults.

---

# 26. Repeated Incline Testing

`Incline -> Incline` was tested using approximately:

`Snap_Start Z = Path_Start Z = -0.03`

The belt surfaces appear straight and remain at the same slope through repeated incline sections.

Repeated incline sections initially looked as though the conveyors were increasing in size. Inspector verification showed that the conveyor wrapper roots remained:

`Scale = (1, 1, 1)`

The apparent size increase is caused by accumulated elevation and the support/leg geometry included in each supplied incline model.

Each additional incline raises the next conveyor section, so its built-in supports are also translated upward. Multiple supplied support structures can overlap or look progressively taller relative to the Ground.

This is currently treated as a visual/model-layout limitation rather than a Transform scaling bug.

Potential later options:

- accept repeated inclines as a functional but visually imperfect layout
- limit incline chaining
- hide/interchange interior support geometry when inclines are chained
- use incline primarily as a transition between flat levels

No support-removal solution has been implemented yet.

---

# Current Architecture

```text
Scene
├── Main Camera
├── Directional Light
├── Global Volume
├── Ground
├── PlacementManager
└── ProductSpawner

Prefabs
├── Conveyor_Generic
├── Conveyor_Short
├── Conveyor_Incline
└── Product_Box

Scripts
├── Conveyor.cs
├── PlacementManager.cs
├── ProductMover.cs
└── ProductSpawner.cs
```

Current conveyor controls:

| Key | Action |
|---|---|
| `1` | Generic conveyor |
| `2` | Short conveyor |
| `3` | Incline conveyor |
| `4` | Spawn cardboard box |
| `Esc` | Cancel conveyor preview |

---

# Design Decisions

## Preserve supplied assets

Original VACAC FBX source files are kept intact. Clean wrapper prefabs are used for application-level behaviour and any required visual/orientation correction is applied through the prefab hierarchy rather than editing the FBX source file.

Reasons:

- easier runtime transforms
- clearer prefabs
- protects supplied source assets
- easier debugging

## Separate snapping and product movement

Snap points and product path points remain separate concerns:

- `Snap_Start` / `Snap_End` = conveyor connection geometry
- `Path_Start` / `Path_End` = product movement route

The adaptive calibration system currently changes `Snap_Start Z` and `Path_Start Z` together because both need to represent the same incoming seam position for a given mesh combination.

## Existing end fixed; incoming start adapts

The current seam-calibration strategy is intentionally asymmetric:

- do not repeatedly modify every existing conveyor's `Snap_End` / `Path_End`
- treat the existing end as the stable reference
- adapt the newly placed conveyor's start to the existing conveyor type

This gives one controlled location for connection-specific mesh compensation and avoids repeatedly breaking previously tuned outgoing endpoints.

## Horizontal candidate discovery, full 3D final snap

XZ-only distance is used only when necessary to discover incline/elevated endpoints while the preview is still following the Ground.

Final placement and endpoint occupancy remain full XYZ operations.

This prevents the earlier problem where global horizontal-only snapping could create vertically unrelated/stacked false connections.

## Transform-based product movement

Products currently use scripted interpolation rather than physical belt forces.

Reasons:

- deterministic
- simple
- suitable for modular paths
- easier to maintain within the assessment time limit

---

# Known Limitations / Work Remaining

- The nine start-Z calibration values were derived visually and several are still approximate; final seam polish may require small Inspector adjustments.
- `Incline -> Short` and `Short -> Incline` should receive another visual tuning pass before final submission.
- Repeated incline sections have visually awkward/overlapping built-in support structures even though root scale remains correct.
- Product movement through mixed Generic / Short / Incline chains still requires complete end-to-end verification after the latest adaptive snapping changes.
- Product orientation does not yet intentionally rotate to follow the incline slope; this should be reviewed during incline product-movement testing.
- Current placement intentionally builds one continuous straight line; manual turns, branches and arbitrary conveyor rotation are not implemented.
- Delete/remove conveyor functionality is not implemented.
- Placement preview does not yet provide clear valid/invalid colour feedback.
- UI is still keyboard-driven.
- Supplied Slim Can product support is not implemented.
- Camera pan / zoom / rotation may be added if time permits.
- Environment/visual polish is still minimal.
- Start / Pause / Reset simulation controls are not implemented.
- Product-to-product physical collision/spacing is not simulated, although spawn-overlap protection exists for newly spawned products.
- Windows build and out-of-editor testing are still required.
- Demo video, final README and submission packaging are still required.

---

# Next Planned Work

Recommended next sequence:

1. Run a final visual check of all nine conveyor connection combinations using the calibration matrix above.
2. Fine-tune only the remaining approximate Z values in the PlacementManager Inspector rather than rewriting snapping logic.
3. Test product traversal through mixed chains, especially `Generic -> Incline -> Generic` and longer elevated sections.
4. Tune product height/orientation across incline transitions if necessary.
5. Decide how to handle repeated incline support geometry.
6. Add the supplied Slim Can product.
7. Add user-facing controls / placement feedback if time allows.
8. Create and test the Windows build.
9. Record the demo video and prepare the final README/submission package.

Important reminder for future seam tuning:

> Do not change multiple prefab ends and starts at the same time. Keep the existing conveyor end stable, tune the new conveyor's incoming start profile, and update the calibration table when a value is intentionally changed.

---

# Notes for Final Documentation

The final README should explain:

- purpose of the application
- Unity version used
- controls (`1` Generic, `2` Short, `3` Incline, `4` Box, `Esc` cancel)
- project architecture
- conveyor placement and snapping system
- adaptive type-to-type seam calibration
- product movement system
- supplied VACAC assets used
- build instructions
- known limitations
- future improvements
- GitHub repository URL

The README should remain concise and user-facing.

This `DEVELOPMENT_NOTES.md` file is intentionally more detailed and acts as the development diary and technical reference.
