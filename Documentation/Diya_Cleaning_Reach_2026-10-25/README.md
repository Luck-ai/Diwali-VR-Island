# Cleaning reach adjustment

The authored scene and the DiyaGame default now use a 1.25 m cleaning radius, increased from 0.55 m. Brushing still requires holding and moving the feather; remaining stationary does not clean the diya.

Nine focused Play Mode checks passed. The character capsule remained outside the preparation bench (body edge X33.620; bench begins X34.026), and the feather stayed outside too (edge X33.962). Sweeping at a measured 1.018 m from the diya cleaned it and emitted dust particles. The dust coating then cleared completely.

XR Interaction Simulator settings were not changed. PlayerTrackedCollision remains a runtime component on the shared XR Origin, so it applies across Diwali, Christmas and Geo in headset builds with the simulator disabled. These checks were performed in desktop Play Mode.
