## AR Interfaces for Mid-Air 6-DoF Alignment: Ergonomics-Aware Design and Evaluation

This repository contains the source code for the AR HMD mid-air alignment prototypes described in the paper "AR Interfaces for Mid-Air 6-DoF Alignment: Ergonomics-Aware Design and Evaluation." 

### Requirements

- HoloLens 1
- Desktop running SteamVR with Vive Headset
- Unity 2019

### Setup Instructions

The same Unity project is deployed on both the desktop (via the Unity Editor) and on the HoloLens (by compiling to a UWP project). The desktop instance acts as the server, to which the HoloLens app connects as a client. 

Before building/deploying, the SampleScene in the Unity project should be updated so that the network address points to an IP address or hostname of the desktop that is running the server. 

Once the client and server are connected, the poses of the Vive controllers are sent to the HoloLens and kept in sync. However, until the two coordinate systems (HoloLens and Vive) are merged, the visualizations will not appear on top of the HoloLens' user's view of the physical controllers. To merge the coordinate systems, the HoloLens user should do an air-tap gesture to place a world anchor on the ground. This anchor can be air-tapped and dragged into position such that it matches the physical location of the Vive coordinate system's origin. Additional UI elements visible to the HoloLens user can be air-tapped to do small-scale refinement. The world anchor's position is persisted between sessions, so once the coordinate system are merged, the app can be restarted without needing to do the calibration step again.

By default, the app is in a "live demo" mode, where pressing the controller trigger will place a new 6-DoF pose at the controller's current position. The user can then move the controller around and see how the visualization changes based on the relative pose between the current controller pose and the indicated target pose. Pressing the controller trigger again will complete the alignment and make the placed pose disappear. Clicking left/right/up/down on the controller trackpad cycles between the alignment interfaces.

In the Game view of the Unity Editor, the desktop project lets the user set user properties, such as resetting the user's height and setting the desired "near" and "far" distances for the user's outstretched arm. The desktop application also lets the user start an alignment session, in which a sequence of 12 randomly-generated poses are presented to the HoloLens user, who performs each 6-DoF mid-air alignment one at a time.