# Mouse Mode, a plugin for [OpenTabletDriver](https://github.com/OpenTabletDriver/OpenTabletDriver)

**\*important\* this plugin only works in absolute mode. As such it comes with the caveat that certain applications (such as games) might not be able to interpret the inputs correctly.**

## What is this?
Put simply, this is an extensive attempt at turning the absolute mode of OTD into a relative mode. 

## Why use this when relative mode exists?
This plugin adds support for features otherwise missing from relative mode. As well as adding a lot of customizability with bindings.

## Feature List

### - Use your pen as a mouse -
Self-explanatory. The added benefits being you can set and customize your work area on the tablet and the display however you like. This includes features like clamping and limiting input to said areas.

Other features of note are a toggleable acceleration formula directly emulating the one windows applies to mouse (called *"Enhance pointer position"* in Windows mouse properties). As well as automatic normalization of aspect ratios (without the need to change work areas), allowing you to transfer exactly what you draw on your tablet, to the screen.

### - Fully customizable bindings -
Every single parameter you can set in the plugin's settings, you can bind to a button in both [Toggle] and [Hold] modes. For checkboxes, that means switching between enabled and disabled states. For other parameters, you may put in a custom value to switch to and from the settings you have saved in the plugin settings. Filling in the custom value in not necessary for checkboxes.

## Parameters
### Reset Time
Time in milliseconds between pen inputs before treating movements as a new relative stroke. Setting this value to any negative number disables this resetting, resulting in every new report shifting the input by the distance travelled between points. Setting this value to zero effectively disables relative travel mode (you can use a toggle binding with this value to move the cursor to somewhere before turning relative mode back on).

### Ignore Input Outside Full Tablet Area
In my tests, I found that the tablet has huge margins of error moving the pen completely outside the tablet area. This feature simply ignores all those inputs to remain precise with movement. This feature is separate from the clamping and limiting options you can set on your work area.

### Normalize Aspect Ratio
This feature normalizes your input in such a way that if you draw something on the tablet, that shape is transferred 1:1 to your display. When disabled, depending on how you setup your tablet and display areas, your input may end up stretched or squished.

### Speed Multiplier
A direct multiplier to your movement speed. Keep in mind that the base speed of the cursor is dependant on the size of your tablet and display areas.

### Use Windows Mouse Acceleration Curve
Applies a subtle acceleration to your movements, directly mimicking the method by which windows applies it to mouse movement.

### Acceleration Intensity
Similar to speed multiplier, this parameter scales your movement speed. However it has a square root scale meaning it scales up slower the higher value you give it. This multiplier is only applied if acceleration is enabled. You may use it to counter the speed gained from acceleration, without changing your base speed multiplier, should you choose to bind a button to toggling acceleration.

## Limitations
As mentioned at the top, this plugin is built on top of absolute mode and as such will not work in relative mode. The consequence of this is that certain programs (such of games), which require relative movement, may not be able to interpret your inputs correctly.