Marching-Squares-for-Unity3D
============================

To Do:
- Change MarchingSquaresTerrain.Paint() to work correctly with radii > resolution * scale.
- Implement PaintStroke method for terrain and chunk.
	- Use shortest distance between a point and a line segment?
- Implement detection of isolated islands.
- Implement support for Rigidbody chunks.
- Improve mesh generation.
	- Maximize shared vertices. Currently using about twice as many as is necessary.
		- Implement a MeshGraph data structure?
	- Simplify flat surfaces. Almost like tessellation.
- Implement saving and loading of terrain to and from binary files.
- Implement texturing.
	- Use a splat map like Unity's own Terrain type.
- Implement network support.
- Create Demos:
	- Terrain generator with caves.
	- Asteroids
	- Fluid simulation.
