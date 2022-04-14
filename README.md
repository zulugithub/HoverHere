

# Hover Here, a Free RC Helicopter Simulator

![Alt text](https://github.com/zulugithub/HoverHere/blob/master/HoverHere/Assets/Images/SikorskyS61_3.jpg?raw=true "Title")

## Introduction

This Unity project is a simulator of RC controlled model helicopter. It is intended to be used for training purposes but also should give the interested reader through the open source code the insight how the helicopter dynamics can be modeled. Indeed the motivation for developing this project was the 

* curiosity about how the controller of a RC-helicopter works 
* and in a far future create a more realistic flight model with a less artificial/clean flight dynamics behavior then available games offer.

In focus is the authors RC helicopter LOGO 600 SE V3. It is possible to use several other helicopter models and to load different environments (Skymaps with collision objects). The environments are stored in the `\HoverHere\Assets\StreamingAssets\Skymaps\` folder and can always be changed. Building new helicopter models requires in the actual code version Unity to be installed. 

**This repository contains due to limited space on git only the LOGO600 SE V3 helicopter model. All other model settings has to be deleted manually in Unity. See in Unity --> Hirarchy --> Helicopters_Available --> delete all models except Logo600SE_V3.**

## Physical Theories 

The physics related code is mostly oriented on following documents:

**Helicopter**

* [Unmanned Rotorcraft Systems](https://www.springer.com/de/book/9780857296344) - Chapter 6: Flight Dynamics Modeling
* [Autonomous Aerobatic Maneuvering of Miniature Helicopters](https://core.ac.uk/download/pdf/4385472.pdf) -  Chapter 3.4: Component forces and moments
* [Modelling and control of a hexarotor UAV](http://liu.diva-portal.org/smash/get/diva2:821251/FULLTEXT01.pdf) - Chapter 4.2: States, ...  Chapter 4.5: The complete non-linear dynamic mode
* [Study of helicopterroll control effectiveness criteria](https://ntrs.nasa.gov/archive/nasa/casi.ntrs.nasa.gov/19870015897.pdf) 

**Brushless Motor**

* [Modeling of DC Motor](http://ocw.nctu.edu.tw/course/dssi032/DSSI_2.pdf)  - Equation 2-14

**Unity**

* [Solving ODEs in Unity](https://joinerda.github.io/Solving-ODEs-in-Unity/) 


## Game Details

All parameter are stored in a large structure. The user can modify these and store copies in Unity's "Application.persistentDataPath". The parameter menu is built automatically from the parameter structure. That means changing the structure in c# does not require manual updating the UI.

Collisions are detected between the blades and the 3D collision object 'collision_object.obj' and they lead to resetting the model to the initial conditions. Therefore the built in routines from Unity are used. The detection runs with the frame rate.


## Code Details

The main coordinate system is a right-handed system, in contrast to Unity's left handed coordinate system. This makes transformations, and caution while using Unity's methods necessary.  

Following calculations are done in a separate thread with a high update frequency:

### Calculation Kernel 

The core of the code are the ordinary differential equations (ODE) of a rigid body, given in `Helicopter_ODE.cs`. The use of quaternions for the orientations result in thirteen first order ODEs. Additional differential equations arise from the main rotor flapping dynamics, the dynamics of the rotating parts (brushless motor, shafts and blades) and from the control routines of the flybareless-, gyro and governor controllers. This results in sum in over 30 ODEs.

This coupled ordinary differential equations are solved with an explicit Runge–Kutta method which preserves the quaternion unit norm sufficiently.

While the rigid body dynamics equations are relative simple, the main difficulty is to describe the flightdynamics adequate in a realtime application.

Another challenge is to find the correct model parameter.

A simple contact detection routine detects collision between the landing skids (represented as points) and a 3D collision object 'collision_landing_object.obj' (represented as triangles). It is used to calculate the contact forces and torques during landing. Axis-aligned bounding boxes (AABB) are utilized to speed up the calculation. To avoid rare 'falling throug gaps problem' in the concave mesh every collision point has a copy close to it.

## Building and Contributing

This is the author's first Unity project, therefore thre is no claim to having found in every detail of the code the best solution. 

* Therefore critical reviews are welcome. 
* Also contributions to model the aerodynamics and air dynamics 
* sensor and flybareless controller detailing
* or in UI design are welcome.

To build this game you need at least a free version of Unity (https://store.unity.com/download). 
After downloading and installing Unity and the latest files from this repository you can open Unity Hub and browse for the project files. Opening the project the first time takes a while. Then you have to go to "File" -> "Open Scene" -> select "Scenes"-folder -> select "Main.unity" -> klick Open. To run the game in the Unity editor klick the "play" button in the top middle of the screen.

The latest release uses Unity 2020.2.0b12.

## Credits

Some of the code is based on different sources from the internet recherches. Please reffer to the links given in the soure code.

## License

This software is licensed under the GPLv3 license. A copy of the license can
be found in `LICENSE.txt`


