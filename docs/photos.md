# Family photos

In the full version a family can show its own photos behind the countdown. There are two ways to turn them on, both in
the look picker on Home:

- **The Photos scene**: the photo fills the screen with nothing painted in front.
- **My photos behind the scene**, a switch under the scene list: the photo replaces the sky of whichever painted scene
  is chosen, and that scene's houses, fireplace or train stay in front.

Choosing the Photos scene turns the switch on and locks it, because there the photos are the scene. Choosing another
scene afterwards keeps the photos behind it until the switch is turned off.

Without the full version, or with no photos added yet, both lead to the grown-ups' corner, where photos are added.

## How it is stored

- `FamilySettings.SceneId` is the chosen scene. `SceneCatalog.PhotosId` (`"photos"`) is the Photos scene; it isn't one
  of `SceneCatalog.All`, so `SceneCatalog.Resolve` falls back to the first painted scene whenever there is no photo to
  show.
- `FamilySettings.UsesPhotos` is the switch.
- `PhotoBackdrop` (one per window) decides whether a photo shows (`IsActive`) and whether the scene's foreground is
  painted over it (`ShowForeground`).

## Data from before version 2

Version 1 had a "My photos" entry in the scene list plus a separate "In front of the photos" choice in the grown-ups'
corner (`PhotoForegroundSceneId`). `FamilyService` migrates it on load: photos with a foreground scene become that
scene with the switch on, photos with "just the photo" become the Photos scene, and anything else keeps its scene.
`FamilyServiceTests` covers each case. The old property stays on `FamilySettings` only so old files can be read.
