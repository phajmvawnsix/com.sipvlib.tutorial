# Changelog

## [1.1.0] - 2026-09-08

Odin Inspector is no longer required. TutorialActionForceTouch, TutorialActionInvokeEvent, TutorialActionListenEvent, TutorialActionWaitUserData and TutorialNodeTargetConditionUserData now use Alchemy (com.annulusgames.alchemy) instead of Sirenix.OdinInspector -- including their expression-based ShowIf conditions, which Alchemy does not support and are now computed bool properties instead.

## [1.0.2] - 2026-09-03

Pin com.sipvlib.config/debugging/event/pool/ui/userdata/utilities and com.cysharp.unitask to semver versions instead of git URLs, so this package installs cleanly via the OpenUPM registry.

## [1.0.1] - 2026-09-03

Lower minimum Unity Editor version to 2022.3 LTS (was 6000.3) and add a `repository`
field to `package.json`, both required for OpenUPM registry submission.

## [1.0.0] - 2026-07-18

Initial extraction from SiPVLib monolith.
