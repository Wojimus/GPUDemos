# GPU Demos
A collection of GPU demos aiming to educate and provide examples of utilising GPU parallel computing in game development.

## Boids Simulation
A recreation of the famous boids simulation often used in flock simulation in game development and animation. Boid behaviour and drawing is done completely on the GPU through the use of compute shaders and passing buffers into graphical shaders to bypass the requirement of passing data back to the CPU.

## Perlin Flowfield
Entities being manipulated by a perlin flowfield. This simulation is similar to the boids simulation however in this case the entities behaviour is reactive to the external input.

## Voxel and Grass
A fully complete voxel terrain and mesh generation system combined with a completely procedural grass system. Every step is completed through the use of the GPU including Simplex noise generation, Mesh Generation and UV and texture mapping. Secondly the accompanying grass system showcases how compute shaders can be used to create, draw and animate rich grass fields at a performant level.

## Features and Skills Showcased
Below is a non comprehensive list of topics and techniques used in the creation of this project. If you are looking for implementation and code examples for any of these techniques feel free to browse the source code of this project.
- Compute Shaders
- Compute Buffers
- URP Shaders
- Texture Arrays
- Render Textures
- GPU Mesh Generation
- GPU Noise Generation
- GPU Grass Generation

# Final Words
This work is a culmination of a year's progress on my final year project, and I hope, a useful and educational resource that will help other software developers to utilise the computing power of the GPU in their game projects to achieve new highs and performance.

I offer this entire project, including its source code and assets, free of charge and license requirements in the hopes other developers can use this repository to educate themselves and so better developers than I can use this as a springboard to educate the game development community in this underused method of computation.

An important note is that the GPU Simplex noise library used in this project were provided by https://github.com/stegu/webgl-noise under the MIT license and maintains that license and credit. It was simply modified to be converted to HLSL to be applicable in Unity.

### Author
Wojciech Marek

### Unity Version
Unity 2021.3.6f1
