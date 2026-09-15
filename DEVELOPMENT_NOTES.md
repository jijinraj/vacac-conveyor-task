\# VACAC Conveyor System — Development Notes



\## Project



VACAC Graduate Software Engineer Take-Home Technical Task 2026



The task is to create a modular conveyor system in Unity using the provided

conveyor models and simulate the provided products moving along the conveyors.



The minimum requested functionality is:



\- Windows-compatible build

\- Select and place conveyors into an environment

\- Snap conveyors together to form a continuous conveyor line

\- Move products along the conveyors



The task is intentionally open-ended and allows additional functionality.



\---



\# Development Log



\## 1. Initial Unity Setup



Created a new Unity 3D project.



Imported the supplied VACAC `.unitypackage`.



The supplied assets include:



\### Conveyor Models



\- `VA\_Conveyor\_Belt\_Generic.fbx`

\- `VA\_Conveyor\_Belt\_Short.fbx`

\- `VA\_incline\_Conveyor\_Belt.fbx`



\### Product Models



\- `VA\_Cardboard\_Box.fbx`

\- `VA\_Slim\_Can.fbx`



The supplied FBX models use imported transforms that are not ideal for

application-level logic, including non-1 scale values and imported rotation.



Rather than modifying the supplied models directly, clean parent GameObjects

were created around them.



This keeps the original VACAC assets unchanged.



\---



\# 2. Generic Conveyor Prefab



Created:



`Conveyor\_Generic`



Structure:



Conveyor\_Generic

├── VA\_Conveyor\_Belt\_Generic

├── Snap\_Start

├── Snap\_End

├── Path\_Start

└── Path\_End



The supplied FBX remains the visual model.



`Conveyor\_Generic` acts as the clean application-level object with:



\- Position: 0, 0, 0

\- Rotation: 0, 0, 0

\- Scale: 1, 1, 1



This makes runtime placement and snapping easier to manage.



\---



\# 3. Conveyor Connection Points



Initially created:



\- `Snap\_Start`

\- `Snap\_End`



These transforms represent the physical/logical connection points used when

joining conveyor modules.



They are separate child GameObjects rather than relying on the FBX pivot.



The intended logical direction is:



Snap\_Start → Snap\_End



The Start point is positioned on the beginning side of the conveyor and the

End point on the outgoing side.



Current snap-point height is approximately:



Y = 0.44



This was chosen based on the physical conveyor geometry.



\---



\# 4. Product Movement Path



Initially product movement reused the conveyor snap points.



This caused a problem because the snap-point height was suitable for connecting

conveyors but not necessarily suitable for placing products on the belt surface.



The design was therefore improved by introducing separate movement points:



\- `Path\_Start`

\- `Path\_End`



The architecture is now:



Snap\_Start / Snap\_End

= conveyor-to-conveyor connection



Path\_Start / Path\_End

= product movement route



This separation will also make it easier to support the supplied incline

conveyor later.



\---



\# 5. Conveyor Component



Created:



`Assets/Scripts/Conveyor.cs`



The component stores references to:



\- Snap Start

\- Snap End

\- Path Start

\- Path End



The script also draws editor Gizmos to make these points easier to debug.



Gizmo colours:



\- Green = Snap\_Start

\- Red = Snap\_End

\- Yellow = Path\_Start

\- Cyan = Path\_End



These Gizmos are development/debugging tools and are not intended to appear

in the final application build.



\---



\# 6. Conveyor Prefab



Converted `Conveyor\_Generic` into a reusable Unity prefab.



Prefab:



`Assets/Prefabs/Conveyor\_Generic.prefab`



The prefab contains:



\- supplied VACAC conveyor model

\- Snap\_Start

\- Snap\_End

\- Path\_Start

\- Path\_End

\- Conveyor component



All four Transform references are assigned directly on the prefab.



This is important because conveyors instantiated at runtime must inherit these

references correctly.



\---



\# 7. Ground Environment



Created a Unity Plane and renamed it:



`Ground`



This acts as the environment onto which conveyors can be placed.



The Ground object contains a collider used by the placement system for

mouse-ray intersection.



\---



\# 8. Camera Setup



Adjusted the Main Camera to provide an elevated builder/simulation view.



The intention is to give the user enough visible workspace to construct

multiple conveyor sections while still being able to see the conveyor models

clearly.



Further camera controls such as pan, rotate and zoom may be added later.



\---



\# 9. Conveyor Placement System



Created:



`Assets/Scripts/PlacementManager.cs`



Created scene GameObject:



`PlacementManager`



The PlacementManager currently references:



\- Main Camera

\- Conveyor Generic Prefab

\- Ground Collider



Current interaction:



\- Press `1` to begin placing a generic conveyor

\- Move mouse over the Ground to move the conveyor preview

\- Left-click to place a valid conveyor

\- Press `Esc` to cancel placement



The first conveyor can be placed freely anywhere on the ground.



Once a conveyor already exists, additional conveyors are no longer allowed to

be placed as disconnected free-floating sections.



For subsequent placement, the preview must successfully snap to a valid

conveyor endpoint before the left-click placement is accepted.



The placement system therefore supports an empty starting scene while also

protecting the runtime layout from disconnected conveyor sections.



\---


\# 10. Conveyor Snapping



Implemented automatic snapping between conveyor modules.



The snapping system now supports extending a conveyor line from either end.



Two valid endpoint relationships are checked:



Forward extension:



Existing Conveyor Snap\_End → New Conveyor Snap\_Start



Backward extension:



New Conveyor Snap\_End → Existing Conveyor Snap\_Start



The placement system compares both endpoint combinations and selects the

closest valid connection within the configured snapping distance.



When a valid snap is found:



\- the preview is repositioned so the relevant connection points overlap

\- the preview inherits the exact rotation of the connected conveyor

\- the conveyor line remains straight and continuously connected



This allows the user to extend the same logical conveyor line from either the

left or right side without reversing Start/End semantics.



Disconnected placement is rejected once the first conveyor exists.



Current intended layout:



Snap\_Start ================= Snap\_End

                               ↑

                               ↓

Snap\_Start ================= Snap\_End



Each connection preserves the logical travel direction:



Snap\_Start → Snap\_End



\---


\# 11. Conveyor Direction Issue



During development the original Start and End points were oriented opposite

to the desired user-facing build direction.



This caused:



\- newly placed conveyors to extend in the unexpected direction

\- product movement to appear reversed

\- some attempted changes to create overlapping conveyor instances



The final decision was NOT to reverse references in code.



Instead, the prefab was corrected so that the actual Transform objects match

their semantic names:



\- Snap\_Start is physically at the start

\- Snap\_End is physically at the end

\- Path\_Start is physically at the start

\- Path\_End is physically at the end



The Inspector references remain intuitive:



Snap Start → Snap\_Start

Snap End → Snap\_End

Path Start → Path\_Start

Path End → Path\_End



This keeps the code easier to understand and explain.



\---



\# 12. Product Prefab



Created a clean wrapper around the supplied cardboard box FBX.



Structure:



Product\_Box

└── VA\_Cardboard\_Box



Created prefab:



`Assets/Prefabs/Product\_Box.prefab`



As with the conveyor, the wrapper keeps runtime transforms separate from the

supplied FBX import transforms.



\---



\# 13. Product Movement



Created:



`Assets/Scripts/ProductMover.cs`



The product currently:



1\. Starts at the current conveyor's Path\_Start

2\. Moves toward Path\_End

3\. Uses configurable movement speed

4\. Uses a configurable vertical offset so the model visually sits on the belt



Current movement speed:



0.3 units/second



The product movement uses:



`Vector3.MoveTowards`



rather than physical conveyor simulation.



This was intentionally chosen as a simple and deterministic solution for the

technical assessment.



\---



\# 14. Product Height



Originally the product movement used Snap\_Start and Snap\_End.



Because these points were above the desired product travel height, the

cardboard box appeared to float above the conveyor.



Introducing Path\_Start and Path\_End solved the architectural issue.



A small `heightOffset` remains available in ProductMover for final visual

adjustment.



\---



\# 15. Multi-Conveyor Product Movement



ProductMover was extended to search for a connected conveyor when the product

reaches Path\_End.



It checks whether:



Current Conveyor Snap\_End



is sufficiently close to:



Another Conveyor Snap\_Start



If a connected conveyor is found, that conveyor becomes the current conveyor

and the product continues from its Path\_Start.



The goal is:



Conveyor 1 → Conveyor 2 → Conveyor 3 → ...



with the product travelling continuously through the assembled conveyor line.



\---



\# 16. Current User Flow



Current intended runtime interaction:



1\. Application starts with an empty Ground

2\. User presses `1`

3\. Generic conveyor preview appears

4\. User positions the first conveyor freely

5\. Left-click places the first conveyor

6\. User presses `1` again to create another preview

7\. The preview can move freely over the Ground

8\. Moving the preview near either open end of the conveyor line causes automatic snapping

9\. Snapping can extend the line from either the left or right side

10\. Clicking while the preview is disconnected does not place a conveyor

11\. Snapped conveyors inherit the existing line orientation so the layout remains straight

12\. User can press `Esc` at any time to cancel active conveyor placement

13\. User presses `2` to spawn a cardboard box

14\. ProductSpawner identifies the first conveyor in the connected line

15\. The product is initialized at that conveyor's Path\_Start

16\. ProductMover carries the product through each connected conveyor in sequence

17\. Pressing `2` again can spawn additional independent cardboard boxes



Current controls:



1 = Place Generic Conveyor



2 = Spawn Cardboard Box



Esc = Cancel Active Conveyor Placement



\---


\# 17. Git / Version Control



Git repository created at the Unity project root.



Tracked project directories:



\- Assets

\- Packages

\- ProjectSettings



Ignored Unity-generated directories include:



\- Library

\- Temp

\- Logs

\- Builds

\- UserSettings



Unity `.meta` files are intentionally committed.



Git LFS has been enabled because several supplied VACAC FBX files are very

large.



Configured:



`\*.fbx filter=lfs diff=lfs merge=lfs -text`



Large supplied assets include conveyor FBX files around 239–241 MB and the

cardboard box FBX around 126 MB.



\---



\# 18. Runtime Product Spawning



The original test workflow used a Product\_Box instance that was permanently

placed in the scene and manually linked to a conveyor.



This was replaced with a runtime spawning system.



Created:



`Assets/Scripts/ProductSpawner.cs`



Created scene GameObject:



`ProductSpawner`



The Product\_Box prefab now contains ProductMover directly and does not require

a Current Conveyor reference to be assigned in the prefab.



Current ProductMover defaults:



\- Speed: 0.3

\- Height Offset: 0.02

\- Connection Tolerance: 0.05



When the user presses `2`, ProductSpawner:



1\. Finds the first conveyor in the connected conveyor line

2\. Instantiates the Product\_Box prefab

3\. Retrieves ProductMover from the new instance

4\. Calls `Initialize(Conveyor)`

5\. Places the product at the starting conveyor's Path\_Start



This removes the dependency on a pre-placed product and allows multiple boxes

to be spawned during runtime.



The Unity 6 deprecated `FindObjectsByType` overload using

`FindObjectsSortMode.None` was also replaced with the current overload in the

placement and product-movement scripts.



\---



\# 19. Placement Validation Improvements



Testing exposed two important placement issues.



First, the original snap logic only supported extending the line in one

direction:



Existing Snap\_End → New Snap\_Start



This meant a conveyor placed visually before the original conveyor could look

connected but would not form the expected logical product route.



The snapping system was updated to support both valid connection directions:



\- Existing Snap\_End → New Snap\_Start

\- New Snap\_End → Existing Snap\_Start



Second, the preview could previously be placed anywhere on the Ground even

when it was not connected to the conveyor line.



This allowed disconnected conveyor sections that ProductMover could not

traverse.



PlacementManager now distinguishes between:



\- first conveyor placement, which is free

\- subsequent conveyor placement, which requires a successful snap



`TrySnapPreview()` now reports whether a valid connection was found, and

placement is accepted only when that condition is satisfied after the first

conveyor exists.



The connected conveyor's rotation is copied to the preview during snapping,

which intentionally keeps the current implementation as a straight continuous

conveyor line.



Verified behaviour:



\- extend line from the right

\- extend line from the left

\- reject disconnected conveyor placement

\- preserve straight-line orientation

\- spawn products from the new first/leftmost conveyor

\- move products continuously through the connected line



\---


\# Current Architecture



Scene

├── Main Camera

├── Directional Light

├── Global Volume

├── Ground

├── PlacementManager

└── ProductSpawner



Prefabs

├── Conveyor\_Generic

└── Product\_Box



Scripts

├── Conveyor.cs

├── PlacementManager.cs

├── ProductMover.cs

└── ProductSpawner.cs



\---


\# Design Decisions



\## Preserve supplied assets



Original VACAC FBX files are not modified directly.



Clean wrapper GameObjects are used instead.



Reason:



\- easier runtime transforms

\- clearer prefabs

\- protects supplied assets

\- easier debugging



\## Separate snapping and product movement



Snap points and product path points are independent.



Reason:



\- conveyor connection geometry and belt travel geometry are different concerns

\- product height can be changed without breaking conveyor snapping

\- better preparation for incline conveyors



\## Transform-based product movement



Products currently move using scripted interpolation rather than belt physics.



Reason:



\- deterministic

\- simple

\- suitable for modular conveyor paths

\- easier to maintain within the assessment time limit



\---



\# Known Limitations / Work Remaining



The following functionality is not yet complete or polished:



\- Add UI instead of relying entirely on keyboard shortcuts

\- Add the short conveyor model

\- Add the incline conveyor model

\- Validate incline conveyor snapping and product transfer

\- Add the supplied slim can product

\- Current placement intentionally supports a straight continuous line only

\- Manual conveyor rotation / branching is not currently supported

\- Add delete/remove conveyor functionality

\- Add clearer placement-validity feedback, for example valid/invalid preview colours

\- Improve conveyor preview appearance

\- Prevent duplicate placement onto an already occupied snap endpoint

\- Improve protection against geometry overlap in more complex future layouts

\- Add camera pan / zoom / rotation if time permits

\- Improve environment visuals

\- Add Start / Pause / Reset simulation controls

\- Test longer conveyor chains

\- Stress-test multiple simultaneously moving products

\- Product-to-product collision / spacing is not currently simulated

\- Create Windows build

\- Test the Windows build outside the Unity Editor

\- Record demo video

\- Prepare final README

\- Package project/build for submission



\---


\# Next Planned Feature



Add the supplied short conveyor as a second selectable conveyor type while

preserving the same snapping, straight-line validation and product traversal

architecture.



The next implementation should:



\- create a clean `Conveyor\_Short` wrapper prefab

\- add Snap\_Start and Snap\_End transforms

\- add Path\_Start and Path\_End transforms

\- attach the existing Conveyor component

\- extend the placement system so the user can select either Generic or Short

\- verify that Generic and Short conveyors can snap to each other

\- verify ProductMover can traverse mixed conveyor lengths



After the short conveyor is working, the planned progression is:



1\. Incline conveyor support

2\. Slim can product support

3\. User-facing UI / controls

4\. Camera and visual polish

5\. Windows build and final submission packaging



\---


\# Notes for Final Documentation



The final README should explain:



\- purpose of the application

\- Unity version used

\- controls

\- project architecture

\- conveyor placement system

\- snapping system

\- product movement system

\- supplied VACAC assets used

\- build instructions

\- known limitations

\- future improvements

\- GitHub repository URL



The README should be concise and user-facing.



This DEVELOPMENT\_NOTES file is intentionally more detailed and acts as a

development diary/reference.

