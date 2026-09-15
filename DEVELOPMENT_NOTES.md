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

\- Left-click to place the conveyor

\- Press `Esc` to cancel placement



The first conveyor can be placed freely anywhere on the ground.



This means the scene can start with an empty environment instead of requiring

a permanently pre-placed starter conveyor.



\---



\# 10. Conveyor Snapping



Implemented automatic snapping between conveyor modules.



The placement system searches existing Conveyor components and compares:



New Conveyor Snap\_Start



against:



Existing Conveyor Snap\_End



When the two points are within the configured snapping distance, the preview

is repositioned so the two connection points align.



Current snapping concept:



Existing Conveyor:



Snap\_Start ================= Snap\_End

&#x20;                               ↑

&#x20;                               ↓

&#x20;                          Snap\_Start ================= Snap\_End

&#x20;                                     New Conveyor



This allows multiple conveyor pieces to form a continuous line.



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

4\. User positions the conveyor

5\. Left-click places the first conveyor freely

6\. User presses `1` again

7\. New conveyor preview appears

8\. Moving it near an existing Snap\_End causes automatic snapping

9\. Left-click confirms placement

10\. Additional conveyors can be added to extend the line

11\. Products can travel using the Path\_Start → Path\_End route



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



\# Current Architecture



Scene

├── Main Camera

├── Directional Light

├── Global Volume

├── Ground

└── PlacementManager



Prefabs

├── Conveyor\_Generic

└── Product\_Box



Scripts

├── Conveyor.cs

├── PlacementManager.cs

└── ProductMover.cs



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



\- Product spawning should be user-controlled rather than beginning automatically

\- Add UI instead of relying entirely on keyboard shortcuts

\- Add the short conveyor model

\- Add the incline conveyor model

\- Validate incline conveyor snapping and product transfer

\- Add the supplied slim can product

\- Consider conveyor rotation during placement

\- Add delete/remove conveyor functionality

\- Add placement validity feedback

\- Improve conveyor preview appearance

\- Add camera pan / zoom / rotation if time permits

\- Improve environment visuals

\- Add Start / Pause / Reset simulation controls

\- Prevent invalid conveyor overlap

\- Test longer conveyor chains

\- Test multiple products

\- Create Windows build

\- Test the Windows build outside the Unity Editor

\- Record demo video

\- Prepare final README

\- Package project/build for submission



\---



\# Next Planned Feature



Replace the permanently placed Product\_Box with a runtime spawning system.



Proposed interaction:



1 = Place Generic Conveyor



2 = Spawn Cardboard Box



Possible future UI:



\[Generic Conveyor]

\[Short Conveyor]

\[Incline Conveyor]



\[Spawn Box]

\[Spawn Can]



\[Start]

\[Pause]

\[Reset]



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

