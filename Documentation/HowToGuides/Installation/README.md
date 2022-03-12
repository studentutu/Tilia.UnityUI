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
2. Install Tilia.Interactions Interactables (Default Hand Grab like Pointers)
3. Install Tilia.Indicators ObjectPointers (Line/Curved Pointers)
4. Install Tilia.UnityUI
5. Install Tillia.UnityInputManager (actions for old Unity Input Manager)
6. Install Tilia.Input.CombinedActions (combinations for axis and other actions) and after reimport - add new Axis (from default window popup)
7. Switch active input handling to Both (new Unity Input System - does not support old event handlers - IPointer... and EventTriggers)
8. Install Tillia.UnityInputSystem (actions for new Unity Input System)


You are ready to go!

### Usage : Part 1 VRTK Canvases

1. Add one or many World Canvas (this package will not work with any other type of canvases)
2. Add VRTK4_UICanvas component onto the Canvas to make it work with the system
3. Make sure you have only 1 active EventSystem at the start of the scene (package will add custom Input module to make sure Unity will recognize any pointer as valid UI  raycaster)
4. Add Unity UI standard Graphic Raycaster to enable casting on canvas.
5. Optionally set custom preferences for VRTK4_UICanvas (e.g. point with Interactor collider to make a click)
(Watch Doc2 screenshot for help)
Note: Please spare as many canvases as you can, as the package will use all Unity UI graphics against all active VRTK4_UICanvas with active Graphic Raycaster enabled. The more active canvases you have the more graphics will need to be processed.


### Usage : Part 2 VRTK UI Pointer

1. Setup Interactor with appropriate input actions (Tilia.Interactions Interactables)
(Watch Doc3 screenshot for help)
2. Add [L_R]_ UI Pointer on Interactor.prefab under your Interactor.
3. Add VRTK4_Player Object component to your Hand/Custom Interactor. Set [L_R]_ UI Pointer on Interactor.prefab as the target 
4. Add ObjectPointer.Straight (Tilia.Indicators ObjectPointers)
5. Setup selection (click action) and activation (hover activation) input actions.
6. Finish setup for [L_R]_ UI Pointer on Interactor. Setup pointer facade.   
Optionally set the Custom Origin on [L_R]_ UI Pointer on Interactor (use it so that the pointer will not be inside the player collider) 

[Full_Image]


[Full_Image]: ./Full_Interactor_With_ObjectPointer_Straight_and_VRTK4_UI_Pointer.PNG

### Done