1. Add Graph Manager Component to your main camera
2. From Any Object simply use one of the variations that you require
    // Draws a graph overlaid on your camera, and it is displaying the currentDeltaTime value, and the color of that value is green
    a) GraphManager.Graph.Plot("Test_ScreenSpace", currentDeltaTime, Color.green);

    // Draws a graph overlaid on your camera, and it is displaying the currentDeltaTime value, and the color of that value is green
        // The rect you pass allows you to render it anywhere on the screen and the size of it
    b) GraphManager.Graph.Plot("Test_ScreenSpace", currentDeltaTime, Color.green, GraphRect);

    // Draws a graph overlaid on your camera, and it is displaying the currentDeltaTime value, and the color of that value is green
    // The wrapper is used to attach the graph to an object in world space with rotation scale etc, refer to example script
    c) GraphManager.Graph.Plot("Test_WorldSpace", currentDeltaTime, Color.green, new GraphManager.Matrix4x4Wrapper(transform.position, transform.rotation, transform.localScale));

3. To See an example of how things work
    a) Create any game object in the hierarchy
    b) Assign the script called "GpuGraphTest"
    c) It will show you 2 types of graphs inside the library