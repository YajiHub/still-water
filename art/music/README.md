# Still Water: music source

All music is original, synthesized by [`compose.py`](compose.py) with a fixed seed. No samples and no downloads.
It runs on Blender's bundled Python, because that has numpy and can write OGG Vorbis:

```bash
"C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup --python art/music/compose.py -- client/Assets/_Project/Resources/Music
```

The track list and what each is for is in the script's header. `MusicDirector` (Scripts/Core) plays them, and
`MusicImport` (Scripts/Editor) sets the loops to stream. To change a piece, edit its function, re-run, and Unity
re-imports. Keep the file names: they are the constants in `MusicDirector`.
