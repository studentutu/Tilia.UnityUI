# Installing the package

> * Level: Beginner
>
> * Reading Time: 10 minutes
>
> * Checked with: Unity Editor 2017.4 +, 2018.1 +,  2019.1 +, 2020.1 +

## Introduction

This package includes the support for Unity UI. You can use unlimited amount of pointers and canvases (note this will impact the performance). 
This package is nothing more than a port of the original VRTK 3.3.0 UI System with some of the additional tweaks and fixes.
This system allows you to hover, select, drag, snap, click on all Unity UI component (except Toggle component).

If you have any issues with installation or usage, please make open an issue or go to the official VRTK4 discord.
Please, note if you need pointer to interact with other Non UI objects - please refer to Tilia.Interactions Interactables package (it is in there)

Also this package is fully compatible with VRTK 3.3.0 ,as all of the classes are renamed and stripped out of any legacy dependencies. 

## Let's Start

### Installation (Pre-requisites)

1. Install Zinnia.Unity
2. Install Tilia.Interactions Interactables
3. Install Tilia.Indicators ObjectPointers
4. Install Tilia.UnityUI

You are ready to go!

### Usage : Part 1 VRTK Canvases

1. Add one or many World Canvas (this package will not work with any other type of canvases)
2. Add VRTK4_UICanvas component onto the Canvas to make it work with the system
3. Make sure you have only 1 active EventSystem at the start of the scene (package will add custom Input module to make sure Unity will recognize any pointer as valid UI  raycaster)
4. Add Unity UI standard Graphic Raycaster to enable casting on canvas.
5. Optionally set custom preferences for VRTK4_UICanvas (e.g. point with Interactor collider to make a click)

Note: Please spare as many canvases as you can, as the package package casts against all active VRTK4_UICanvas with active Graphic Raycaster enabled. The more active canvases you have the more raycasting will occur.


### Usage : Part 2 VRTK UI Pointer

1. Setup Interactor with appropriate input actions (Tilia.Interactions Interactables)
2. Add ObjectPointer.Straight (Tilia.Indicators ObjectPointers)
3. Navigate to PointsRenderer under ObjectPointer.Straight -> ObjectPointer.Internal -> Logic -> PointsHandler -> PointsRenderer
4. Add under PointsRenderer UI pointer prefab ( [L_R]_PointsRenderer UI Pointer.prefab )
5. Setup selection (enabling hover) and activation (press to click) input actions
6. Add VRTK4_Player Object component to your Interactor. Set which of the Pointers will it use as the target
7. Optionally set the Custom Origin (use it so that the pointer will not be inside the player collider) 

### Done