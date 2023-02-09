# ThreeDeeSprites
Realtime sprite prerendering system for converting 3D models to 2D sprite images during runtime.

For small indie devs it is often necessary to find ways to speed up workflow due to limited resources and manpower. Sprites and 2D artwork are
often used because the can get away with reduced fidelity which allows for a better time-to-quality ratio. However, they also lack many features that
3D models can utilize to gain back time during development such as animation retargeting and hardpoints. This system aims to meld the two by allowing
devs to create low-fidelity 3D models that are converted to 2D sprites in realtime. This allows for many additional possibilities beyond simple asset
including realtime 3D lighting, kinematics, dynamic animation masking and blending and much more all while maintaining an asthetic that appears to be
2D sprites.

I think that the use of shaders would be the most optimal way to implement this system however I opted to go with pre-rendering to render textures
for a few reasons the primary being that I am not very good with shaders. As an exaperiment I didn't want to get bogged down with that rabbit hole
until I have proven the concept will work in general. So for now it simply uses a seperate camera and render texture to handle realtime conversion of
models to sprites.

# Experimental  
This project is currently experimental and as such is highly likely to receive large changes.

# Limits  
Currently this system is not setup to take full advantage of what 3D models can offer. Realtime 3D lighting simply isn't an option with how it is setup
at the moment. Neither is kinematics. There is also a limit to how many objects can be rendered using this system due to how it allocates chuncks of space
on the render texture. Once I've proven the concept can work in theory then I'll go back and attempt to address these other issues.
