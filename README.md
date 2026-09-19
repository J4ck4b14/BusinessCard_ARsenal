# ARsenal — AR Business Card

ARsenal is an Android augmented-reality business card developed in Unity 6 using AR Foundation.

The physical card contains two image markers, each launching a different AR experience.

## Front — Professional Profile

Scanning the front displays an interactive professional profile with:

- GitHub
- LinkedIn
- ArtStation
- Email contact
- CV link
- Interactive profile/contact panel
- Play, Pause and Stop controls for portfolio media

The content remains anchored to the physical card and reacts to tracking being acquired or lost.

## Back — ARsenal

Scanning the reverse launches a small AR survival shooter.

Main features include:

- Player-controlled tank
- Automatic target acquisition and shooting
- Enemy waves
- Multiple enemy behaviours
- Procedural cover and obstacles
- Projectile and line-of-sight collision
- Deployable shields
- Upgrade selection between waves
- Increasing difficulty
- Score and high-score system
- Persistent progression and statistics
- Retryable runs without restarting the application

The battlefield changes between waves, forcing the player to adapt movement and use cover.

## Controls

### Android

Touch controls are provided directly through the AR interface.

### Editor / PC

- WASD / Arrow Keys — Movement
- Space / E — Fire
- Gamepad Left Stick — Movement

The project uses Unity's Input System rather than the legacy input API.

## Technology

- Unity 6
- C#
- AR Foundation
- Google ARCore
- Unity Input System
- Android

## Project Structure

The project separates the two AR experiences through the tracked image names:

- Front — Professional AR experience
- Back — ARsenal minigame

Gameplay responsibilities are divided between dedicated controllers for the player, enemies, waves, projectiles, procedural world generation, UI, progression and saving.

## Current Status

The AR tracking system and back-side gameplay are functional.

The front-side interaction system is implemented, although final media content and the definitive CV URL still need to be assigned before the project is considered complete.
