# Mouse Utils, a plugin for [OpenTabletDriver](https://github.com/OpenTabletDriver/OpenTabletDriver)

This plugin comes with 2 filters, Abs2Rel and Windows Mouse Acceleration, with separate bindings for each.

## Abs2Rel
Put simply, this is an extensive attempt at turning the absolute mode of OTD into a relative mode.

### Why use this when relative mode exists?
This plugin adds support for features otherwise missing from relative mode. As well as adding a lot of customizability with bindings.

**\*important\* this filter only works in absolute mode. As such it comes with the caveat that certain applications (such as games) might not be able to interpret the inputs correctly.**

### Feature List

#### - Use your pen as a mouse -
Self-explanatory. The added benefits being you can set and customize your work area on the tablet and the display however you like. This includes features like clamping and limiting input to said areas.

Other features of note are automatic normalization of aspect ratios (without the need to change work areas), allowing you to transfer exactly what you draw on your tablet, to the screen. As well as integration with the Windows Mouse Acceleration filter.

#### - Fully customizable bindings -
Every single parameter you can set in the plugin's settings, you can bind to a button in both [Toggle] and [Hold] modes. For checkboxes, that means switching between enabled and disabled states. For other parameters, you may put in a custom value to switch to and from the settings you have saved in the plugin settings. Filling in the custom value is not necessary for checkboxes.

### Parameters
#### Reset Time
Time in milliseconds between pen inputs before treating movements as a new relative stroke. Setting this value to zero disables this resetting, resulting in every new report shifting the input by the distance travelled between points. Setting this value to negative values effectively disables relative travel mode (you can use a toggle binding with this value to move the cursor to somewhere before turning relative mode back on).

#### Ignore Input Outside Full Tablet Area
In my tests, I found that the tablet has huge margins of error moving the pen completely outside the tablet area. This feature simply ignores all those inputs to remain precise with movement. This feature is separate from the clamping and limiting options you can set on your work area.

#### Normalize Aspect Ratio
This feature normalizes your input in such a way that if you draw something on the tablet, that shape is transferred 1:1 to your display. When disabled, depending on how you setup your tablet and display areas, your input may end up stretched or squished.

#### Speed Multiplier
A direct multiplier to your movement speed. Keep in mind that the base speed of the cursor is dependant on the size of your tablet and display areas.

### Limitations
As mentioned at the top, this filter is built on top of absolute mode and as such will not work in relative mode. The consequence of this is that certain programs (such of games), which require relative movement, may not be able to interpret your inputs correctly.

## Windows Mouse Acceleration
Applies a subtle acceleration to your movements, directly mimicking the method by which windows applies it to mouse movement. In windows this feature is found in mouse settings under the name "Enhance pointer precision". 

This filter works both with Abs2Rel in absolute mode, and on its own in relative mode. (but it does nothing on its own in absolute mode)

**\*Note\* if you use a plugin such as VMulti to emulate a mouse, windows will apply its own acceleration on top of this fitler. You may want to disable it in mouse settings as mentioned above.**

### Parameters
#### Acceleration Intensity
Similar to speed multiplier, this parameter scales your movement speed. However it has a square root scale meaning it scales up slower the higher value you set it. You can disable the acceleration by setting the intensity to zero (via a binding for example).