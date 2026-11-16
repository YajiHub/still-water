# Phone-hosted multiplayer

- Added **Local Multiplayer → Host / Join** for Lab 5 Option A. The two-player match uses a direct TCP connection on the hotspot, the existing five-room race scene, and the installed level layouts and Listener tuning.
- The host controls the countdown, clock, catches, pushes, scoring and results. A dropped connection returns to the local lobby during a match.
- Game and EditMode test assemblies compiled against the installed Unity references. Four new race tests passed in a standalone .NET harness using the project level data. The sandbox blocked the TCP loopback test and Unity batch startup.
- Next: run EditMode in Unity, export an updated Android APK, then record a complete two-phone hotspot race and a disconnect with mobile data off. [Setup steps](../SETUP.md#8-playing-without-internet-on-a-phone-hotspot).
