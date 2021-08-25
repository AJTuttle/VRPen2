# VRPen2
## Introduction
VRPen is an internet-networked light-weight Unity package for collaborative drawing. While initially made as a drawing solution for VR software, it has evolved into a generic drawing package for Unity.

<img src="/Runtime/Materials/Readme_images/readme-img0.PNG" width="600" >
<img src="/Runtime/Materials/Readme_images/readme-img1.PNG" width="600" >

## VRPen has many drawing functions
VRPen has a variety of functions and features, including...
- Adding stamps (images)
- Adding text
- Backgrounds
- Undo
- Clear
- Color changing
- Line thickness changing
## VRPen is light-weight
VRPen only renders new lines and graphics. By not clearing previous drawing data from the render texture, VRPen is able to avoid rendering the entire canvas every frame.
## VRPen is easily networked
VRPen is not separately networked but is simple to integrate with any pre-existing networking solution. VRPen also has an integrated light-weight caching system for users that join late.
## VRPen has generic input
VRpen comes with the following input devices and is built in such a way that it is easy to add new custom input devices.
- VR Markers
- Mouse
- Touch screen
- Graphics tablets
## Import the package
```diff
- Note: This package is in development and will undergo changes.
```
To import this packet, add the git link to the Unity package manager. Please note that this package depends on TextMeshPro.

<img src="/Runtime/Materials/Readme_images/readme-img2.PNG" width="300" >

Once imported, a sample scene can also be loaded via the package manager.

<img src="/Runtime/Materials/Readme_images/readme-img3.PNG" width="300" >
