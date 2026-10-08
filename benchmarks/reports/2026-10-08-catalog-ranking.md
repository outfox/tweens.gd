# Tween definition cost ranking

Measured library: `2a21ee6`. Read the [follow-up analysis](2026-10-08-followup.md)
before treating short-run outliers as stable: longer runs did not reproduce the
largest Canvas shader timings or the per-tween shader allocation spikes.

361 definitions, 100 and 1,000 targets, direct-write baselines, memory and threading diagnostics.

Short-run screening; error is the 99.9% confidence half-width. Outlier: per-target cost above Q3 + 3×IQR within the count.

Ratios are ratios of means. Managed allocation totals and threading counts are per benchmark invocation (one frame).

## 100 targets

| Rank | Definition | Tween µs ± error | Direct µs | ns/target | Overhead ns/target | Ratio | Tween B | Direct B | Work items | Contentions | Outlier |
| ---: | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- |
| 1 | GeometryInstanceShaderParameter&lt;Single&gt; | 135.68 ± 571.91 | 4.50 | 1356.8 | 1311.8 | 30.16 | 1 | 0 | 0 | 0 | yes |
| 2 | CanvasItemInstanceShaderParameter&lt;Vector4&gt; | 123.84 ± 16.50 | 11.09 | 1238.4 | 1127.5 | 11.17 | 3201 | 0 | 0 | 0 | yes |
| 3 | CanvasItemInstanceShaderParameter&lt;Vector2&gt; | 105.12 ± 351.50 | 9.83 | 1051.2 | 952.8 | 10.69 | 1 | 0 | 0 | 0 | yes |
| 4 | GeometryInstanceShaderParameter&lt;Double&gt; | 94.36 ± 21.85 | 4.53 | 943.6 | 898.3 | 20.84 | 2401 | 0 | 0 | 0 | yes |
| 5 | PathFollow3DProgressRatio | 79.49 ± 1.49 | 41.15 | 794.9 | 383.4 | 1.93 | 1 | 0 | 0 | 0 | yes |
| 6 | GeometryInstanceShaderParameter&lt;Vector3&gt; | 77.78 ± 7.73 | 10.72 | 777.8 | 670.6 | 7.26 | 1 | 0 | 0 | 0 | yes |
| 7 | GeometryInstanceShaderParameter&lt;Vector4&gt; | 77.77 ± 7.26 | 11.19 | 777.7 | 665.8 | 6.95 | 1 | 0 | 0 | 0 | yes |
| 8 | GeometryInstanceShaderParameter&lt;Vector2&gt; | 76.57 ± 31.75 | 9.90 | 765.7 | 666.7 | 7.73 | 1 | 0 | 0 | 0 | yes |
| 9 | PathFollow3DProgress | 75.93 ± 4.09 | 37.65 | 759.3 | 382.7 | 2.02 | 0 | 0 | 0 | 0 | yes |
| 10 | GeometryInstanceShaderParameter&lt;Color&gt; | 75.88 ± 8.14 | 11.05 | 758.8 | 648.3 | 6.87 | 1 | 0 | 0 | 0 | yes |
| 11 | PathFollow3DVOffset | 74.08 ± 2.62 | 35.18 | 740.8 | 389.1 | 2.11 | 0 | 0 | 0 | 0 | yes |
| 12 | PathFollow3DHOffset | 73.66 ± 0.93 | 35.96 | 736.6 | 377.0 | 2.05 | 0 | 0 | 0 | 0 | yes |
| 13 | GeometryInstanceShaderParameter&lt;Int32&gt; | 73.43 ± 8.69 | 4.54 | 734.3 | 688.9 | 16.18 | 0 | 0 | 0 | 0 | yes |
| 14 | ControlOffsets | 67.51 ± 1.48 | 31.16 | 675.1 | 363.6 | 2.17 | 0 | 0 | 0 | 0 | yes |
| 15 | CanvasItemInstanceShaderParameter&lt;Color&gt; | 55.71 ± 16.81 | 11.49 | 557.1 | 442.2 | 4.85 | 1 | 0 | 0 | 0 | yes |
| 16 | CanvasItemInstanceShaderParameter&lt;Vector3&gt; | 55.34 ± 1.85 | 10.87 | 553.4 | 444.6 | 5.09 | 1 | 0 | 0 | 0 | yes |
| 17 | CanvasItemInstanceShaderParameter&lt;Int32&gt; | 55.30 ± 3.34 | 4.62 | 553.0 | 506.8 | 11.97 | 1 | 0 | 0 | 0 | yes |
| 18 | ControlGlobalPositionY | 54.27 ± 0.61 | 17.24 | 542.7 | 370.2 | 3.15 | 0 | 0 | 0 | 0 | yes |
| 19 | CanvasItemInstanceShaderParameter&lt;Double&gt; | 54.21 ± 11.07 | 4.42 | 542.1 | 498.0 | 12.27 | 1 | 0 | 0 | 0 | yes |
| 20 | ControlGlobalPositionX | 54.17 ± 2.35 | 17.23 | 541.7 | 369.4 | 3.14 | 0 | 0 | 0 | 0 | yes |
| 21 | CanvasItemInstanceShaderParameter&lt;Single&gt; | 53.70 ± 9.10 | 4.63 | 537.0 | 490.7 | 11.59 | 1 | 0 | 0 | 0 | yes |
| 22 | PathFollow2DProgressRatio | 53.60 ± 1.62 | 16.03 | 536.0 | 375.7 | 3.34 | 0 | 0 | 0 | 0 | yes |
| 23 | ControlGlobalPosition | 51.62 ± 0.77 | 14.26 | 516.2 | 373.6 | 3.62 | 0 | 0 | 0 | 0 | yes |
| 24 | PathFollow2DProgress | 51.14 ± 1.45 | 13.88 | 511.4 | 372.6 | 3.68 | 0 | 0 | 0 | 0 | yes |
| 25 | ControlPosition | 50.29 ± 0.10 | 12.47 | 502.9 | 378.1 | 4.03 | 0 | 0 | 0 | 0 | yes |
| 26 | Light3DLightTemperature | 49.92 ± 0.83 | 16.78 | 499.2 | 331.4 | 2.98 | 0 | 0 | 0 | 0 | yes |
| 27 | GlobalRotation3DY | 49.64 ± 2.56 | 13.30 | 496.4 | 363.4 | 3.73 | 0 | 0 | 0 | 0 | yes |
| 28 | ControlPositionX | 49.50 ± 2.84 | 13.15 | 495.0 | 363.4 | 3.76 | 0 | 0 | 0 | 0 | yes |
| 29 | GlobalRotation3DZ | 49.20 ± 0.82 | 13.35 | 492.0 | 358.5 | 3.69 | 0 | 0 | 0 | 0 | yes |
| 30 | ControlPositionY | 48.93 ± 1.92 | 13.21 | 489.3 | 357.2 | 3.70 | 0 | 0 | 0 | 0 | yes |
| 31 | PathFollow2DVOffset | 48.76 ± 0.03 | 12.44 | 487.6 | 363.2 | 3.92 | 0 | 0 | 0 | 0 | yes |
| 32 | GlobalQuaternion3D | 48.64 ± 0.39 | 13.44 | 486.4 | 352.1 | 3.62 | 0 | 0 | 0 | 0 | yes |
| 33 | Quaternion3D | 48.64 ± 2.44 | 14.19 | 486.4 | 344.5 | 3.43 | 0 | 0 | 0 | 0 | yes |
| 34 | GlobalRotation3DX | 48.43 ± 1.42 | 12.93 | 484.3 | 355.0 | 3.75 | 0 | 0 | 0 | 0 | yes |
| 35 | PathFollow2DHOffset | 48.43 ± 1.26 | 12.46 | 484.3 | 359.6 | 3.89 | 0 | 0 | 0 | 0 | yes |
| 36 | ControlSizeX | 48.03 ± 1.92 | 11.00 | 480.3 | 370.3 | 4.37 | 0 | 0 | 0 | 0 |  |
| 37 | ControlSize | 47.92 ± 4.41 | 11.28 | 479.2 | 366.3 | 4.25 | 0 | 0 | 0 | 0 |  |
| 38 | ControlSizeY | 47.81 ± 5.59 | 11.05 | 478.1 | 367.6 | 4.33 | 0 | 0 | 0 | 0 |  |
| 39 | ControlAnchorMin | 46.65 ± 8.92 | 11.46 | 466.5 | 351.9 | 4.07 | 0 | 0 | 0 | 0 |  |
| 40 | ControlAnchorMax | 46.52 ± 29.41 | 11.52 | 465.2 | 350.0 | 4.04 | 0 | 0 | 0 | 0 |  |
| 41 | LightColor3D | 45.98 ± 0.88 | 11.46 | 459.8 | 345.2 | 4.01 | 0 | 0 | 0 | 0 |  |
| 42 | Camera3DVOffset | 45.86 ± 1.29 | 10.69 | 458.6 | 351.7 | 4.29 | 0 | 0 | 0 | 0 |  |
| 43 | ControlOffsetLeft | 45.76 ± 1.41 | 8.16 | 457.6 | 376.0 | 5.61 | 0 | 0 | 0 | 0 |  |
| 44 | Camera3DHOffset | 45.58 ± 0.76 | 10.69 | 455.8 | 348.9 | 4.26 | 0 | 0 | 0 | 0 |  |
| 45 | ControlOffsetRight | 45.15 ± 2.58 | 8.11 | 451.5 | 370.4 | 5.57 | 0 | 0 | 0 | 0 |  |
| 46 | ControlOffsetBottom | 44.98 ± 0.93 | 8.16 | 449.8 | 368.1 | 5.51 | 0 | 0 | 0 | 0 |  |
| 47 | ControlOffsetTop | 44.62 ± 1.71 | 8.26 | 446.2 | 363.5 | 5.40 | 0 | 0 | 0 | 0 |  |
| 48 | GlobalScale2D | 42.65 ± 1.94 | 3.18 | 426.5 | 394.8 | 13.43 | 0 | 0 | 0 | 0 |  |
| 49 | GlobalRotation3D | 42.37 ± 0.71 | 6.70 | 423.7 | 356.7 | 6.32 | 0 | 0 | 0 | 0 |  |
| 50 | Camera2DOffsetX | 41.88 ± 2.42 | 5.63 | 418.8 | 362.5 | 7.44 | 0 | 0 | 0 | 0 |  |
| 51 | ControlAnchorTop | 41.70 ± 1.26 | 5.94 | 417.0 | 357.5 | 7.02 | 0 | 0 | 0 | 0 |  |
| 52 | ControlAnchorLeft | 41.65 ± 1.65 | 5.95 | 416.5 | 357.0 | 7.00 | 0 | 0 | 0 | 0 |  |
| 53 | ControlAnchorBottom | 41.57 ± 2.26 | 5.87 | 415.7 | 357.1 | 7.08 | 0 | 0 | 0 | 0 |  |
| 54 | Camera2DOffsetY | 41.57 ± 0.23 | 5.56 | 415.7 | 360.1 | 7.48 | 0 | 0 | 0 | 0 |  |
| 55 | AudioVolumeDb | 41.40 ± 1.60 | 4.64 | 414.0 | 367.5 | 8.91 | 0 | 0 | 0 | 0 |  |
| 56 | ControlAnchorRight | 41.37 ± 0.80 | 6.75 | 413.7 | 346.1 | 6.12 | 0 | 0 | 0 | 0 |  |
| 57 | GlobalScale2DY | 41.35 ± 0.20 | 5.09 | 413.5 | 362.7 | 8.13 | 0 | 0 | 0 | 0 |  |
| 58 | Camera2DZoomX | 41.29 ± 0.09 | 5.79 | 412.9 | 355.0 | 7.13 | 0 | 0 | 0 | 0 |  |
| 59 | Camera2DZoomY | 40.95 ± 1.15 | 5.79 | 409.5 | 351.7 | 7.08 | 0 | 0 | 0 | 0 |  |
| 60 | AudioVolumeLinear | 40.93 ± 1.09 | 5.28 | 409.3 | 356.6 | 7.76 | 0 | 0 | 0 | 0 |  |
| 61 | Parallax2DAutoscrollX | 40.88 ± 8.39 | 4.48 | 408.8 | 364.1 | 9.13 | 0 | 0 | 0 | 0 |  |
| 62 | Camera2DZoom | 40.86 ± 0.24 | 5.50 | 408.6 | 353.6 | 7.42 | 0 | 0 | 0 | 0 |  |
| 63 | GlobalScale2DX | 40.74 ± 6.84 | 5.08 | 407.4 | 356.6 | 8.02 | 0 | 0 | 0 | 0 |  |
| 64 | Parallax2DScrollOffsetY | 40.43 ± 2.78 | 4.42 | 404.3 | 360.0 | 9.14 | 0 | 0 | 0 | 0 |  |
| 65 | Camera2DOffset | 40.35 ± 1.93 | 5.19 | 403.5 | 351.6 | 7.78 | 0 | 0 | 0 | 0 |  |
| 66 | Parallax2DScrollOffsetX | 40.35 ± 1.12 | 4.26 | 403.5 | 360.9 | 9.48 | 0 | 0 | 0 | 0 |  |
| 67 | CanvasLayerScaleY | 40.24 ± 29.95 | 3.34 | 402.4 | 369.0 | 12.05 | 0 | 0 | 0 | 0 |  |
| 68 | Parallax2DAutoscrollY | 40.22 ± 1.41 | 4.37 | 402.2 | 358.4 | 9.19 | 0 | 0 | 0 | 0 |  |
| 69 | CanvasLayerOffsetY | 40.03 ± 1.62 | 3.29 | 400.3 | 367.4 | 12.15 | 0 | 0 | 0 | 0 |  |
| 70 | CanvasLayerOffsetX | 40.03 ± 1.30 | 3.32 | 400.3 | 367.1 | 12.06 | 0 | 0 | 0 | 0 |  |
| 71 | RangeValue | 39.95 ± 0.57 | 4.88 | 399.5 | 350.7 | 8.18 | 0 | 0 | 0 | 0 |  |
| 72 | GlobalPosition2DX | 39.94 ± 0.49 | 4.58 | 399.4 | 353.6 | 8.73 | 0 | 0 | 0 | 0 |  |
| 73 | GlobalPosition3DX | 39.91 ± 0.46 | 3.20 | 399.1 | 367.1 | 12.48 | 0 | 0 | 0 | 0 |  |
| 74 | CanvasLayerScaleX | 39.89 ± 0.25 | 3.37 | 398.9 | 365.2 | 11.83 | 0 | 0 | 0 | 0 |  |
| 75 | CanvasLayerRotation | 39.76 ± 0.46 | 3.37 | 397.6 | 363.9 | 11.78 | 0 | 0 | 0 | 0 |  |
| 76 | Scale2DY | 39.71 ± 3.89 | 3.31 | 397.1 | 363.9 | 11.98 | 0 | 0 | 0 | 0 |  |
| 77 | GlobalPosition2DY | 39.70 ± 5.05 | 4.57 | 397.0 | 351.3 | 8.69 | 0 | 0 | 0 | 0 |  |
| 78 | GlobalRotation2D | 39.68 ± 0.74 | 3.04 | 396.8 | 366.4 | 13.07 | 0 | 0 | 0 | 0 |  |
| 79 | Parallax2DAutoscroll | 39.58 ± 1.41 | 4.21 | 395.8 | 353.8 | 9.41 | 0 | 0 | 0 | 0 |  |
| 80 | ControlOffsetTransformPivotRatioX | 39.40 ± 12.21 | 3.15 | 394.0 | 362.6 | 12.53 | 0 | 0 | 0 | 0 |  |
| 81 | GlobalSkew2D | 39.33 ± 1.17 | 2.98 | 393.3 | 363.5 | 13.18 | 0 | 0 | 0 | 0 |  |
| 82 | Position2DY | 39.32 ± 4.90 | 3.24 | 393.2 | 360.8 | 12.12 | 0 | 0 | 0 | 0 |  |
| 83 | Scale2DX | 39.31 ± 2.30 | 3.31 | 393.1 | 360.1 | 11.89 | 0 | 0 | 0 | 0 |  |
| 84 | Parallax2DScrollOffset | 39.27 ± 1.45 | 4.05 | 392.7 | 352.2 | 9.71 | 0 | 0 | 0 | 0 |  |
| 85 | ControlOffsetTransformPivot | 39.25 ± 3.83 | 2.61 | 392.5 | 366.4 | 15.05 | 0 | 0 | 0 | 0 |  |
| 86 | CanvasLayerOffset | 39.21 ± 0.57 | 3.05 | 392.1 | 361.5 | 12.84 | 0 | 0 | 0 | 0 |  |
| 87 | ControlOffsetTransformPositionRatioX | 39.19 ± 1.77 | 3.14 | 391.9 | 360.5 | 12.49 | 0 | 0 | 0 | 0 |  |
| 88 | GlobalPosition3DY | 39.19 ± 8.03 | 3.26 | 391.9 | 359.3 | 12.03 | 0 | 0 | 0 | 0 |  |
| 89 | ControlOffsetTransformPositionRatioY | 39.09 ± 2.81 | 3.06 | 390.9 | 360.3 | 12.77 | 0 | 0 | 0 | 0 |  |
| 90 | Skew2D | 39.09 ± 1.14 | 2.58 | 390.9 | 365.1 | 15.18 | 0 | 0 | 0 | 0 |  |
| 91 | ControlOffsetTransformPositionY | 39.08 ± 0.93 | 3.13 | 390.8 | 359.5 | 12.49 | 0 | 0 | 0 | 0 |  |
| 92 | ScrollContainerScrollHorizontal | 39.05 ± 0.90 | 3.31 | 390.5 | 357.4 | 11.79 | 0 | 0 | 0 | 0 |  |
| 93 | ControlOffsetTransformPosition | 39.03 ± 0.63 | 2.58 | 390.3 | 364.5 | 15.15 | 0 | 0 | 0 | 0 |  |
| 94 | AnimatedSprite2DFrame | 39.03 ± 0.04 | 3.59 | 390.3 | 354.4 | 10.88 | 0 | 0 | 0 | 0 |  |
| 95 | ControlOffsetTransformPivotX | 38.95 ± 1.88 | 3.12 | 389.5 | 358.3 | 12.48 | 0 | 0 | 0 | 0 |  |
| 96 | ScrollContainerScrollVertical | 38.85 ± 0.43 | 3.30 | 388.5 | 355.5 | 11.79 | 0 | 0 | 0 | 0 |  |
| 97 | ControlScaleY | 38.80 ± 4.87 | 2.81 | 388.0 | 359.8 | 13.80 | 0 | 0 | 0 | 0 |  |
| 98 | ControlOffsetTransformScaleY | 38.64 ± 2.05 | 3.25 | 386.4 | 353.9 | 11.89 | 0 | 0 | 0 | 0 |  |
| 99 | GlobalPosition3DZ | 38.62 ± 1.73 | 3.30 | 386.2 | 353.2 | 11.69 | 0 | 0 | 0 | 0 |  |
| 100 | ControlOffsetTransformPivotY | 38.62 ± 1.09 | 3.05 | 386.2 | 355.7 | 12.65 | 0 | 0 | 0 | 0 |  |
| 101 | ControlOffsetTransformScaleX | 38.56 ± 2.20 | 3.27 | 385.6 | 352.9 | 11.79 | 0 | 0 | 0 | 0 |  |
| 102 | ControlPivotOffsetRatioX | 38.56 ± 0.69 | 2.73 | 385.6 | 358.3 | 14.14 | 0 | 0 | 0 | 0 |  |
| 103 | ControlOffsetTransformPivotRatio | 38.53 ± 0.97 | 2.61 | 385.3 | 359.2 | 14.76 | 0 | 0 | 0 | 0 |  |
| 104 | ControlCustomMaximumSizeX | 38.53 ± 3.43 | 2.97 | 385.3 | 355.5 | 12.95 | 0 | 0 | 0 | 0 |  |
| 105 | ControlPivotOffsetY | 38.52 ± 0.23 | 2.64 | 385.2 | 358.8 | 14.61 | 0 | 0 | 0 | 0 |  |
| 106 | AnimatedSprite3DFrame | 38.51 ± 0.68 | 3.58 | 385.1 | 349.3 | 10.76 | 0 | 0 | 0 | 0 |  |
| 107 | Position2DX | 38.48 ± 1.81 | 3.22 | 384.8 | 352.6 | 11.95 | 0 | 0 | 0 | 0 |  |
| 108 | PointLight2DOffsetY | 38.36 ± 1.46 | 2.56 | 383.6 | 357.9 | 14.97 | 0 | 0 | 0 | 0 |  |
| 109 | ControlPivotOffsetRatio | 38.35 ± 1.56 | 2.29 | 383.5 | 360.6 | 16.75 | 0 | 0 | 0 | 0 |  |
| 110 | RichTextLabelVisibleRatio | 38.35 ± 0.13 | 2.71 | 383.5 | 356.4 | 14.16 | 0 | 0 | 0 | 0 |  |
| 111 | ControlCustomMaximumSizeY | 38.34 ± 1.02 | 2.97 | 383.4 | 353.7 | 12.92 | 0 | 0 | 0 | 0 |  |
| 112 | ControlOffsetTransformPositionRatio | 38.31 ± 1.55 | 2.60 | 383.1 | 357.1 | 14.72 | 0 | 0 | 0 | 0 |  |
| 113 | SelfModulateAlpha | 38.31 ± 1.07 | 1.73 | 383.1 | 365.8 | 22.14 | 0 | 0 | 0 | 0 |  |
| 114 | GlobalPosition2D | 38.27 ± 0.99 | 2.94 | 382.7 | 353.3 | 13.02 | 0 | 0 | 0 | 0 |  |
| 115 | ControlOffsetTransformScale | 38.24 ± 1.89 | 2.73 | 382.4 | 355.1 | 14.01 | 0 | 0 | 0 | 0 |  |
| 116 | ControlScaleX | 38.21 ± 0.90 | 3.08 | 382.1 | 351.4 | 12.43 | 0 | 0 | 0 | 0 |  |
| 117 | Scale2D | 38.20 ± 2.00 | 2.75 | 382.0 | 354.5 | 13.89 | 0 | 0 | 0 | 0 |  |
| 118 | ControlOffsetTransformPositionX | 38.20 ± 35.86 | 3.13 | 382.0 | 350.6 | 12.18 | 0 | 0 | 0 | 0 |  |
| 119 | ControlPivotOffsetX | 38.16 ± 0.89 | 2.71 | 381.6 | 354.5 | 14.10 | 0 | 0 | 0 | 0 |  |
| 120 | PointLight2DOffsetX | 38.15 ± 0.38 | 2.55 | 381.5 | 356.0 | 14.94 | 0 | 0 | 0 | 0 |  |
| 121 | ColorRectColor | 38.09 ± 0.56 | 2.00 | 380.9 | 360.9 | 19.04 | 0 | 0 | 0 | 0 |  |
| 122 | CanvasLayerScale | 38.08 ± 1.38 | 3.03 | 380.8 | 350.5 | 12.56 | 0 | 0 | 0 | 0 |  |
| 123 | Rotation2D | 38.07 ± 1.63 | 2.68 | 380.7 | 354.0 | 14.23 | 0 | 0 | 0 | 0 |  |
| 124 | ControlOffsetTransformRotation | 38.04 ± 0.71 | 2.48 | 380.4 | 355.6 | 15.35 | 0 | 0 | 0 | 0 |  |
| 125 | ControlPivotOffsetRatioY | 38.03 ± 1.15 | 2.63 | 380.3 | 353.9 | 14.43 | 0 | 0 | 0 | 0 |  |
| 126 | ColorRectColorAlpha | 37.99 ± 1.30 | 2.17 | 379.9 | 358.2 | 17.54 | 0 | 0 | 0 | 0 |  |
| 127 | ControlRotation | 37.99 ± 1.27 | 2.25 | 379.9 | 357.4 | 16.92 | 0 | 0 | 0 | 0 |  |
| 128 | CpuParticles3DGravityY | 37.98 ± 10.61 | 1.12 | 379.8 | 368.6 | 33.87 | 0 | 0 | 0 | 0 |  |
| 129 | CpuParticles3DDirectionX | 37.88 ± 2.71 | 1.16 | 378.8 | 367.3 | 32.78 | 0 | 0 | 0 | 0 |  |
| 130 | Sprite2DOffsetY | 37.85 ± 2.80 | 2.48 | 378.5 | 353.7 | 15.24 | 0 | 0 | 0 | 0 |  |
| 131 | ModulateAlpha | 37.81 ± 1.57 | 1.72 | 378.1 | 360.8 | 21.95 | 0 | 0 | 0 | 0 |  |
| 132 | Position2D | 37.78 ± 3.24 | 2.59 | 377.8 | 351.9 | 14.59 | 0 | 0 | 0 | 0 |  |
| 133 | PointLight2DOffset | 37.77 ± 3.06 | 2.17 | 377.7 | 356.1 | 17.44 | 0 | 0 | 0 | 0 |  |
| 134 | Position3DY | 37.76 ± 0.84 | 1.45 | 377.6 | 363.1 | 26.02 | 0 | 0 | 0 | 0 |  |
| 135 | Scale3DX | 37.74 ± 4.55 | 1.74 | 377.4 | 360.0 | 21.68 | 0 | 0 | 0 | 0 |  |
| 136 | Sprite2DOffsetX | 37.62 ± 1.69 | 2.48 | 376.2 | 351.3 | 15.15 | 0 | 0 | 0 | 0 |  |
| 137 | RichTextLabelVisibleCharacters | 37.61 ± 6.90 | 1.09 | 376.1 | 365.3 | 34.63 | 0 | 0 | 0 | 0 |  |
| 138 | ControlPivotOffset | 37.60 ± 0.16 | 2.29 | 376.0 | 353.1 | 16.41 | 0 | 0 | 0 | 0 |  |
| 139 | ControlScale | 37.59 ± 0.25 | 2.54 | 375.9 | 350.5 | 14.81 | 0 | 0 | 0 | 0 |  |
| 140 | Camera3DFrustumOffsetY | 37.57 ± 1.27 | 2.03 | 375.7 | 355.4 | 18.54 | 0 | 0 | 0 | 0 |  |
| 141 | OmniLight3DOmniAttenuation | 37.53 ± 1.06 | 1.12 | 375.3 | 364.2 | 33.63 | 0 | 0 | 0 | 0 |  |
| 142 | GlobalPosition3D | 37.50 ± 0.45 | 3.12 | 375.0 | 343.8 | 12.02 | 0 | 0 | 0 | 0 |  |
| 143 | ControlOffsetTransformPivotRatioY | 37.49 ± 0.44 | 3.08 | 374.9 | 344.1 | 12.18 | 0 | 0 | 0 | 0 |  |
| 144 | DecalModulateAlpha | 37.48 ± 4.16 | 1.52 | 374.8 | 359.6 | 24.60 | 0 | 0 | 0 | 0 |  |
| 145 | Label3DModulateAlpha | 37.48 ± 6.92 | 1.32 | 374.8 | 361.5 | 28.30 | 0 | 0 | 0 | 0 |  |
| 146 | ControlCustomMaximumSize | 37.48 ± 0.35 | 2.15 | 374.8 | 353.2 | 17.42 | 0 | 0 | 0 | 0 |  |
| 147 | Rotation3DY | 37.47 ± 0.71 | 1.72 | 374.7 | 357.4 | 21.76 | 0 | 0 | 0 | 0 |  |
| 148 | Scale3DY | 37.45 ± 1.29 | 1.73 | 374.5 | 357.2 | 21.67 | 0 | 0 | 0 | 0 |  |
| 149 | Light2DShadowColorAlpha | 37.45 ± 11.59 | 1.63 | 374.5 | 358.2 | 22.99 | 0 | 0 | 0 | 0 |  |
| 150 | Rotation3DZ | 37.41 ± 2.96 | 1.70 | 374.1 | 357.1 | 21.94 | 0 | 0 | 0 | 0 |  |
| 151 | TextureProgressBarTintUnderAlpha | 37.39 ± 4.95 | 1.41 | 373.9 | 359.9 | 26.60 | 0 | 0 | 0 | 0 |  |
| 152 | PointLight2DTextureScale | 37.36 ± 0.45 | 2.17 | 373.6 | 351.9 | 17.20 | 0 | 0 | 0 | 0 |  |
| 153 | TextureProgressBarRadialCenterOffsetX | 37.29 ± 0.88 | 1.26 | 372.9 | 360.4 | 29.70 | 0 | 0 | 0 | 0 |  |
| 154 | Camera3DFrustumOffsetX | 37.28 ± 2.09 | 2.03 | 372.8 | 352.5 | 18.35 | 0 | 0 | 0 | 0 |  |
| 155 | ControlCustomMinimumSizeY | 37.27 ± 1.48 | 1.88 | 372.7 | 353.9 | 19.83 | 0 | 0 | 0 | 0 |  |
| 156 | LabelVisibleRatio | 37.25 ± 0.86 | 1.94 | 372.5 | 353.1 | 19.16 | 0 | 0 | 0 | 0 |  |
| 157 | DecalSizeZ | 37.25 ± 2.98 | 1.48 | 372.5 | 357.7 | 25.20 | 0 | 0 | 0 | 0 |  |
| 158 | DecalSizeX | 37.25 ± 1.34 | 1.43 | 372.5 | 358.1 | 26.00 | 0 | 0 | 0 | 0 |  |
| 159 | CpuParticles2DGravityX | 37.23 ± 0.49 | 1.08 | 372.3 | 361.6 | 34.57 | 0 | 0 | 0 | 0 |  |
| 160 | Rotation3DX | 37.17 ± 0.80 | 1.72 | 371.7 | 354.5 | 21.64 | 0 | 0 | 0 | 0 |  |
| 161 | TextureProgressBarTextureProgressOffsetX | 37.13 ± 0.15 | 1.31 | 371.3 | 358.2 | 28.36 | 0 | 0 | 0 | 0 |  |
| 162 | CpuParticles3DEmissionBoxExtentsZ | 37.13 ± 1.06 | 1.18 | 371.3 | 359.5 | 31.52 | 0 | 0 | 0 | 0 |  |
| 163 | TextureProgressBarTintOverAlpha | 37.12 ± 0.09 | 1.33 | 371.2 | 358.0 | 28.01 | 0 | 0 | 0 | 0 |  |
| 164 | DecalSizeY | 37.10 ± 11.51 | 1.47 | 371.0 | 356.3 | 25.22 | 0 | 0 | 0 | 0 |  |
| 165 | Camera3DFov | 37.09 ± 0.97 | 1.84 | 370.9 | 352.5 | 20.18 | 0 | 0 | 0 | 0 |  |
| 166 | Camera3DSize | 37.04 ± 0.40 | 1.76 | 370.4 | 352.8 | 21.07 | 0 | 0 | 0 | 0 |  |
| 167 | Position3DZ | 37.03 ± 1.22 | 1.44 | 370.3 | 355.9 | 25.65 | 0 | 0 | 0 | 0 |  |
| 168 | Position3DX | 37.00 ± 8.96 | 1.48 | 370.0 | 355.2 | 25.07 | 0 | 0 | 0 | 0 |  |
| 169 | SpriteBase3DModulateAlpha | 36.99 ± 2.04 | 1.35 | 369.9 | 356.4 | 27.32 | 0 | 0 | 0 | 0 |  |
| 170 | ControlSizeFlagsStretchRatio | 36.95 ± 9.53 | 1.53 | 369.5 | 354.2 | 24.13 | 0 | 0 | 0 | 0 |  |
| 171 | Line2DDefaultColorAlpha | 36.95 ± 1.01 | 1.38 | 369.5 | 355.7 | 26.78 | 0 | 0 | 0 | 0 |  |
| 172 | Scale3D | 36.94 ± 0.74 | 1.66 | 369.4 | 352.7 | 22.20 | 0 | 0 | 0 | 0 |  |
| 173 | GpuParticles3DLifetime | 36.92 ± 0.85 | 1.03 | 369.2 | 359.0 | 35.96 | 0 | 0 | 0 | 0 |  |
| 174 | SpriteBase3DOffsetX | 36.89 ± 0.55 | 1.27 | 368.9 | 356.2 | 28.94 | 0 | 0 | 0 | 0 |  |
| 175 | CpuParticles3DEmissionBoxExtentsY | 36.88 ± 2.35 | 1.18 | 368.8 | 357.0 | 31.31 | 0 | 0 | 0 | 0 |  |
| 176 | CpuParticles3DDirectionZ | 36.88 ± 0.27 | 1.16 | 368.8 | 357.2 | 31.78 | 0 | 0 | 0 | 0 |  |
| 177 | Quaternion | 36.86 ± 1.26 | 3.21 | 368.6 | 336.5 | 11.47 | 0 | 0 | 0 | 0 |  |
| 178 | SpriteBase3DOffsetY | 36.85 ± 1.12 | 1.27 | 368.5 | 355.8 | 28.95 | 0 | 0 | 0 | 0 |  |
| 179 | Position3D | 36.84 ± 0.67 | 2.31 | 368.4 | 345.2 | 15.93 | 0 | 0 | 0 | 0 |  |
| 180 | AudioVolumeLinear2D | 36.83 ± 0.99 | 1.22 | 368.3 | 356.1 | 30.11 | 0 | 0 | 0 | 0 |  |
| 181 | TextureProgressBarTintProgressAlpha | 36.83 ± 0.58 | 1.35 | 368.3 | 354.8 | 27.27 | 0 | 0 | 0 | 0 |  |
| 182 | CpuParticles3DExplosiveness | 36.82 ± 6.76 | 0.71 | 368.2 | 361.0 | 51.78 | 0 | 0 | 0 | 0 |  |
| 183 | Camera3DNear | 36.79 ± 0.57 | 1.72 | 367.9 | 350.7 | 21.40 | 0 | 0 | 0 | 0 |  |
| 184 | TextureProgressBarRadialCenterOffsetY | 36.78 ± 5.27 | 1.24 | 367.8 | 355.4 | 29.56 | 0 | 0 | 0 | 0 |  |
| 185 | Camera3DFrustumOffset | 36.76 ± 0.52 | 1.80 | 367.6 | 349.6 | 20.37 | 0 | 0 | 0 | 0 |  |
| 186 | Scale3DZ | 36.74 ± 1.06 | 1.73 | 367.4 | 350.1 | 21.27 | 0 | 0 | 0 | 0 |  |
| 187 | GpuParticles2DExplosiveness | 36.74 ± 0.97 | 1.04 | 367.4 | 357.0 | 35.47 | 0 | 0 | 0 | 0 |  |
| 188 | CpuParticles2DGravityY | 36.72 ± 3.60 | 1.10 | 367.2 | 356.2 | 33.51 | 0 | 0 | 0 | 0 |  |
| 189 | CpuParticles2DEmissionRectExtentsX | 36.71 ± 2.53 | 1.13 | 367.1 | 355.8 | 32.51 | 0 | 0 | 0 | 0 |  |
| 190 | TextureProgressBarRadialInitialAngle | 36.69 ± 4.46 | 1.16 | 366.9 | 355.3 | 31.52 | 0 | 0 | 0 | 0 |  |
| 191 | GpuParticles2DLifetime | 36.67 ± 1.64 | 1.09 | 366.7 | 355.8 | 33.70 | 0 | 0 | 0 | 0 |  |
| 192 | PointLight2DHeight | 36.66 ± 1.04 | 1.27 | 366.6 | 353.9 | 28.84 | 0 | 0 | 0 | 0 |  |
| 193 | FogVolumeSizeZ | 36.66 ± 1.21 | 1.52 | 366.6 | 351.4 | 24.13 | 0 | 0 | 0 | 0 |  |
| 194 | Camera3DFar | 36.66 ± 2.72 | 1.81 | 366.6 | 348.4 | 20.21 | 0 | 0 | 0 | 0 |  |
| 195 | CpuParticles3DGravityX | 36.60 ± 1.77 | 1.11 | 366.0 | 354.9 | 33.02 | 0 | 0 | 0 | 0 |  |
| 196 | ControlCustomMinimumSizeX | 36.60 ± 2.30 | 1.90 | 366.0 | 346.9 | 19.25 | 0 | 0 | 0 | 0 |  |
| 197 | Sprite2DOffset | 36.58 ± 1.47 | 2.03 | 365.8 | 345.5 | 18.06 | 0 | 0 | 0 | 0 |  |
| 198 | Label3DOffsetX | 36.57 ± 1.43 | 1.30 | 365.7 | 352.7 | 28.14 | 0 | 0 | 0 | 0 |  |
| 199 | SpotRange | 36.56 ± 1.11 | 1.16 | 365.6 | 354.0 | 31.56 | 0 | 0 | 0 | 0 |  |
| 200 | SpotAngle | 36.56 ± 0.90 | 1.16 | 365.6 | 354.0 | 31.56 | 0 | 0 | 0 | 0 |  |
| 201 | FogVolumeSize | 36.55 ± 1.41 | 1.72 | 365.5 | 348.3 | 21.25 | 0 | 0 | 0 | 0 |  |
| 202 | Sprite2DRegionRect | 36.55 ± 1.45 | 2.26 | 365.5 | 342.9 | 16.20 | 0 | 0 | 0 | 0 |  |
| 203 | FogVolumeSizeY | 36.53 ± 1.46 | 1.53 | 365.3 | 350.0 | 23.88 | 0 | 0 | 0 | 0 |  |
| 204 | SelfModulate | 36.51 ± 0.44 | 1.75 | 365.1 | 347.6 | 20.88 | 0 | 0 | 0 | 0 |  |
| 205 | CpuParticles2DEmissionRectExtentsY | 36.50 ± 0.79 | 1.12 | 365.0 | 353.8 | 32.49 | 0 | 0 | 0 | 0 |  |
| 206 | TextureProgressBarTextureProgressOffsetY | 36.50 ± 10.05 | 1.29 | 365.0 | 352.1 | 28.27 | 0 | 0 | 0 | 0 |  |
| 207 | CpuParticles2DDirectionY | 36.49 ± 2.52 | 1.12 | 364.9 | 353.7 | 32.47 | 0 | 0 | 0 | 0 |  |
| 208 | Label3DOffsetY | 36.49 ± 10.79 | 1.28 | 364.9 | 352.1 | 28.55 | 0 | 0 | 0 | 0 |  |
| 209 | AnimationPlayerSpeedScale | 36.49 ± 0.39 | 0.71 | 364.9 | 357.8 | 51.67 | 0 | 0 | 0 | 0 |  |
| 210 | Polygon2DTextureOffsetY | 36.49 ± 1.10 | 1.24 | 364.9 | 352.5 | 29.50 | 0 | 0 | 0 | 0 |  |
| 211 | CanvasModulateColorAlpha | 36.47 ± 0.50 | 1.24 | 364.7 | 352.3 | 29.36 | 0 | 0 | 0 | 0 |  |
| 212 | CanvasModulateColor | 36.47 ± 0.24 | 1.62 | 364.7 | 348.5 | 22.49 | 0 | 0 | 0 | 0 |  |
| 213 | DecalEmissionEnergy | 36.47 ± 1.18 | 1.09 | 364.7 | 353.8 | 33.49 | 0 | 0 | 0 | 0 |  |
| 214 | Polygon2DTextureOffsetX | 36.47 ± 3.99 | 1.24 | 364.7 | 352.3 | 29.50 | 0 | 0 | 0 | 0 |  |
| 215 | FogVolumeSizeX | 36.47 ± 9.36 | 1.53 | 364.7 | 349.4 | 23.88 | 0 | 0 | 0 | 0 |  |
| 216 | CpuParticles3DEmissionBoxExtentsX | 36.47 ± 10.97 | 1.19 | 364.7 | 352.8 | 30.73 | 0 | 0 | 0 | 0 |  |
| 217 | ControlCustomMinimumSize | 36.44 ± 0.27 | 1.54 | 364.4 | 349.0 | 23.63 | 0 | 0 | 0 | 0 |  |
| 218 | Rotation3D | 36.42 ± 1.18 | 1.72 | 364.2 | 347.0 | 21.15 | 0 | 0 | 0 | 0 |  |
| 219 | Label3DOutlineModulateAlpha | 36.42 ± 0.84 | 1.36 | 364.2 | 350.6 | 26.71 | 0 | 0 | 0 | 0 |  |
| 220 | SpriteBase3DModulate | 36.41 ± 0.98 | 1.70 | 364.1 | 347.1 | 21.43 | 0 | 0 | 0 | 0 |  |
| 221 | Modulate | 36.41 ± 1.00 | 1.75 | 364.1 | 346.6 | 20.84 | 0 | 0 | 0 | 0 |  |
| 222 | CpuParticles2DRandomness | 36.40 ± 0.76 | 0.71 | 364.0 | 356.8 | 51.13 | 0 | 0 | 0 | 0 |  |
| 223 | CpuParticles2DDirectionX | 36.39 ± 1.33 | 1.08 | 363.9 | 353.2 | 33.77 | 0 | 0 | 0 | 0 |  |
| 224 | Label3DPixelSize | 36.39 ± 9.34 | 0.83 | 363.9 | 355.6 | 43.62 | 0 | 0 | 0 | 0 |  |
| 225 | Parallax2DScrollScaleY | 36.39 ± 1.01 | 1.08 | 363.9 | 353.0 | 33.61 | 0 | 0 | 0 | 0 |  |
| 226 | Light2DShadowColor | 36.38 ± 0.80 | 1.80 | 363.8 | 345.7 | 20.17 | 0 | 0 | 0 | 0 |  |
| 227 | CpuParticles2DColorAlpha | 36.37 ± 3.14 | 1.15 | 363.7 | 352.2 | 31.59 | 0 | 0 | 0 | 0 |  |
| 228 | CpuParticles3DGravityZ | 36.37 ± 1.48 | 1.08 | 363.7 | 352.9 | 33.70 | 0 | 0 | 0 | 0 |  |
| 229 | DecalSize | 36.36 ± 0.28 | 1.66 | 363.6 | 347.0 | 21.92 | 0 | 0 | 0 | 0 |  |
| 230 | GpuParticles3DSpeedScale | 36.35 ± 0.22 | 1.01 | 363.5 | 353.3 | 35.82 | 0 | 0 | 0 | 0 |  |
| 231 | Polygon2DTextureScaleX | 36.34 ± 1.62 | 1.24 | 363.4 | 351.0 | 29.38 | 0 | 0 | 0 | 0 |  |
| 232 | AudioStreamPlayer3DMaxDistance | 36.33 ± 3.19 | 0.77 | 363.3 | 355.7 | 47.44 | 0 | 0 | 0 | 0 |  |
| 233 | CpuParticles2DExplosiveness | 36.33 ± 0.33 | 0.71 | 363.3 | 356.2 | 50.95 | 0 | 0 | 0 | 0 |  |
| 234 | Parallax2DScrollScaleX | 36.31 ± 1.07 | 1.08 | 363.1 | 352.3 | 33.71 | 0 | 0 | 0 | 0 |  |
| 235 | AnimatedSprite2DSpeedScale | 36.31 ± 0.24 | 0.71 | 363.1 | 356.0 | 51.26 | 0 | 0 | 0 | 0 |  |
| 236 | LightEnergy3D | 36.31 ± 0.65 | 1.10 | 363.1 | 352.1 | 32.98 | 0 | 0 | 0 | 0 |  |
| 237 | Polygon2DOffsetX | 36.30 ± 0.56 | 1.24 | 363.0 | 350.7 | 29.30 | 0 | 0 | 0 | 0 |  |
| 238 | TextureProgressBarTintUnder | 36.30 ± 0.78 | 1.55 | 363.0 | 347.5 | 23.38 | 0 | 0 | 0 | 0 |  |
| 239 | CpuParticles2DSpeedScale | 36.29 ± 0.22 | 0.71 | 362.9 | 355.9 | 51.44 | 0 | 0 | 0 | 0 |  |
| 240 | AudioVolumeDb3D | 36.29 ± 0.66 | 0.86 | 362.9 | 354.3 | 42.21 | 0 | 0 | 0 | 0 |  |
| 241 | Line2DWidth | 36.28 ± 1.33 | 0.86 | 362.8 | 354.2 | 42.23 | 0 | 0 | 0 | 0 |  |
| 242 | CpuParticles2DSpread | 36.28 ± 1.66 | 0.71 | 362.8 | 355.7 | 51.28 | 0 | 0 | 0 | 0 |  |
| 243 | GpuParticles3DExplosiveness | 36.27 ± 2.22 | 1.04 | 362.7 | 352.3 | 34.88 | 0 | 0 | 0 | 0 |  |
| 244 | AudioVolumeLinear3D | 36.26 ± 0.94 | 1.22 | 362.6 | 350.4 | 29.75 | 0 | 0 | 0 | 0 |  |
| 245 | TextureProgressBarTintProgress | 36.26 ± 2.23 | 1.68 | 362.6 | 345.9 | 21.64 | 0 | 0 | 0 | 0 |  |
| 246 | Polygon2DColorAlpha | 36.26 ± 1.63 | 1.38 | 362.6 | 348.7 | 26.21 | 0 | 0 | 0 | 0 |  |
| 247 | Polygon2DOffsetY | 36.26 ± 0.57 | 1.21 | 362.6 | 350.4 | 29.85 | 0 | 0 | 0 | 0 |  |
| 248 | GpuParticles2DAmountRatio | 36.25 ± 5.21 | 1.05 | 362.5 | 352.0 | 34.43 | 0 | 0 | 0 | 0 |  |
| 249 | CpuParticles2DLifetime | 36.23 ± 5.79 | 0.73 | 362.3 | 355.0 | 49.59 | 0 | 0 | 0 | 0 |  |
| 250 | AudioPitchScale | 36.22 ± 1.69 | 0.87 | 362.2 | 353.4 | 41.41 | 0 | 0 | 0 | 0 |  |
| 251 | CpuParticles2DGravity | 36.21 ± 1.29 | 0.79 | 362.1 | 354.3 | 45.88 | 0 | 0 | 0 | 0 |  |
| 252 | TextureProgressBarRadialCenterOffset | 36.21 ± 3.93 | 0.93 | 362.1 | 352.7 | 38.81 | 0 | 0 | 0 | 0 |  |
| 253 | CpuParticles3DLifetime | 36.20 ± 0.15 | 0.73 | 362.0 | 354.7 | 49.64 | 0 | 0 | 0 | 0 |  |
| 254 | Parallax2DScrollScale | 36.20 ± 3.46 | 0.79 | 362.0 | 354.1 | 46.07 | 0 | 0 | 0 | 0 |  |
| 255 | Polygon2DTextureOffset | 36.20 ± 0.88 | 0.91 | 362.0 | 352.9 | 39.65 | 0 | 0 | 0 | 0 |  |
| 256 | CpuParticles2DColor | 36.19 ± 1.23 | 1.55 | 361.9 | 346.5 | 23.38 | 0 | 0 | 0 | 0 |  |
| 257 | GpuParticles3DRandomness | 36.18 ± 1.53 | 1.03 | 361.8 | 351.5 | 35.10 | 0 | 0 | 0 | 0 |  |
| 258 | Label3DOffset | 36.18 ± 0.87 | 0.92 | 361.8 | 352.5 | 39.14 | 0 | 0 | 0 | 0 |  |
| 259 | CpuParticles3DColorAlpha | 36.16 ± 1.90 | 1.17 | 361.6 | 349.9 | 30.90 | 0 | 0 | 0 | 0 |  |
| 260 | CpuParticles2DEmissionRectExtents | 36.15 ± 0.28 | 0.82 | 361.5 | 353.3 | 44.17 | 0 | 0 | 0 | 0 |  |
| 261 | GpuParticles2DRandomness | 36.15 ± 0.86 | 1.04 | 361.5 | 351.1 | 34.86 | 0 | 0 | 0 | 0 |  |
| 262 | AudioPitchScale3D | 36.12 ± 0.18 | 0.88 | 361.2 | 352.4 | 40.93 | 0 | 0 | 0 | 0 |  |
| 263 | CpuParticles3DDirectionY | 36.12 ± 10.27 | 1.14 | 361.2 | 349.7 | 31.55 | 0 | 0 | 0 | 0 |  |
| 264 | CpuParticles3DSpread | 36.09 ± 0.73 | 0.71 | 360.9 | 353.7 | 50.63 | 0 | 0 | 0 | 0 |  |
| 265 | DecalModulate | 36.08 ± 0.28 | 1.75 | 360.8 | 343.3 | 20.62 | 0 | 0 | 0 | 0 |  |
| 266 | AudioVolumeDb2D | 36.07 ± 0.96 | 0.87 | 360.7 | 352.0 | 41.62 | 0 | 0 | 0 | 0 |  |
| 267 | GeometryInstance3DTransparency | 36.07 ± 0.32 | 1.33 | 360.7 | 347.4 | 27.07 | 0 | 0 | 0 | 0 |  |
| 268 | TextureProgressBarRadialFillDegrees | 36.07 ± 0.40 | 0.86 | 360.7 | 352.0 | 41.81 | 0 | 0 | 0 | 0 |  |
| 269 | SpotLight3DSpotAttenuation | 36.04 ± 1.12 | 1.10 | 360.4 | 349.4 | 32.80 | 0 | 0 | 0 | 0 |  |
| 270 | Line2DDefaultColor | 36.03 ± 0.72 | 1.65 | 360.3 | 343.8 | 21.86 | 0 | 0 | 0 | 0 |  |
| 271 | SpotLight3DSpotAngleAttenuation | 36.02 ± 8.52 | 1.12 | 360.2 | 348.9 | 32.11 | 0 | 0 | 0 | 0 |  |
| 272 | AudioStreamPlayer3DUnitSize | 36.00 ± 0.48 | 0.74 | 360.0 | 352.6 | 48.80 | 0 | 0 | 0 | 0 |  |
| 273 | Polygon2DColor | 35.99 ± 0.55 | 1.61 | 359.9 | 343.8 | 22.30 | 0 | 0 | 0 | 0 |  |
| 274 | LightColor2D | 35.98 ± 0.74 | 1.81 | 359.8 | 341.8 | 19.91 | 0 | 0 | 0 | 0 |  |
| 275 | Light3DLightIndirectEnergy | 35.96 ± 0.41 | 1.12 | 359.6 | 348.4 | 32.16 | 0 | 0 | 0 | 0 |  |
| 276 | GpuParticles2DSpeedScale | 35.95 ± 0.78 | 1.01 | 359.5 | 349.4 | 35.50 | 0 | 0 | 0 | 0 |  |
| 277 | Polygon2DOffset | 35.95 ± 2.85 | 0.93 | 359.5 | 350.2 | 38.84 | 0 | 0 | 0 | 0 |  |
| 278 | SpriteBase3DOffset | 35.92 ± 5.01 | 1.01 | 359.2 | 349.1 | 35.67 | 0 | 0 | 0 | 0 |  |
| 279 | TextureProgressBarTextureProgressOffset | 35.91 ± 3.55 | 0.95 | 359.1 | 349.6 | 37.61 | 0 | 0 | 0 | 0 |  |
| 280 | Polygon2DTextureRotation | 35.91 ± 0.77 | 0.84 | 359.1 | 350.7 | 42.55 | 0 | 0 | 0 | 0 |  |
| 281 | SpringArm3DSpringLength | 35.91 ± 1.42 | 0.81 | 359.1 | 351.0 | 44.38 | 0 | 0 | 0 | 0 |  |
| 282 | CpuParticles2DDirection | 35.90 ± 0.20 | 0.80 | 358.9 | 350.9 | 44.68 | 0 | 0 | 0 | 0 |  |
| 283 | AudioStreamPlayer3DAttenuationFilterCutoffHz | 35.89 ± 0.88 | 0.71 | 358.9 | 351.8 | 50.76 | 0 | 0 | 0 | 0 |  |
| 284 | AudioStreamPlayer2DPanningStrength | 35.87 ± 0.54 | 0.72 | 358.7 | 351.5 | 49.59 | 0 | 0 | 0 | 0 |  |
| 285 | OmniRange | 35.86 ± 0.25 | 1.19 | 358.6 | 346.7 | 30.10 | 0 | 0 | 0 | 0 |  |
| 286 | TextureProgressBarTintOver | 35.86 ± 0.12 | 1.57 | 358.6 | 342.9 | 22.89 | 0 | 0 | 0 | 0 |  |
| 287 | AudioStreamPlayer3DEmissionAngleFilterAttenuationDb | 35.84 ± 0.89 | 0.73 | 358.4 | 351.1 | 48.86 | 0 | 0 | 0 | 0 |  |
| 288 | AudioStreamPlayer3DEmissionAngleDegrees | 35.84 ± 0.36 | 0.77 | 358.4 | 350.7 | 46.30 | 0 | 0 | 0 | 0 |  |
| 289 | CpuParticles3DRandomness | 35.81 ± 2.30 | 0.70 | 358.1 | 351.1 | 50.82 | 0 | 0 | 0 | 0 |  |
| 290 | CpuParticles3DEmissionBoxExtents | 35.80 ± 1.92 | 1.49 | 358.0 | 343.0 | 23.97 | 0 | 0 | 0 | 0 |  |
| 291 | SpriteBase3DPixelSize | 35.78 ± 1.41 | 0.85 | 357.8 | 349.2 | 41.88 | 0 | 0 | 0 | 0 |  |
| 292 | CpuParticles3DEmissionSphereRadius | 35.76 ± 0.99 | 0.73 | 357.6 | 350.3 | 49.00 | 0 | 0 | 0 | 0 |  |
| 293 | LabelVisibleCharacters | 35.76 ± 0.16 | 0.93 | 357.6 | 348.3 | 38.34 | 0 | 0 | 0 | 0 |  |
| 294 | AudioStreamPlayer3DPanningStrength | 35.75 ± 0.88 | 0.75 | 357.5 | 350.1 | 47.94 | 0 | 0 | 0 | 0 |  |
| 295 | Label3DOutlineModulate | 35.75 ± 1.81 | 1.54 | 357.5 | 342.1 | 23.28 | 0 | 0 | 0 | 0 |  |
| 296 | Polygon2DTextureScaleY | 35.74 ± 32.29 | 1.24 | 357.4 | 345.1 | 28.88 | 0 | 0 | 0 | 0 |  |
| 297 | CpuParticles3DSpeedScale | 35.73 ± 2.92 | 0.73 | 357.3 | 350.0 | 48.98 | 0 | 0 | 0 | 0 |  |
| 298 | Light3DShadowOpacity | 35.71 ± 1.36 | 1.11 | 357.1 | 346.0 | 32.11 | 0 | 0 | 0 | 0 |  |
| 299 | CpuParticles3DColor | 35.71 ± 10.31 | 1.58 | 357.1 | 341.4 | 22.67 | 0 | 0 | 0 | 0 |  |
| 300 | Label3DModulate | 35.71 ± 0.77 | 1.68 | 357.1 | 340.4 | 21.31 | 0 | 0 | 0 | 0 |  |
| 301 | CpuParticles3DGravity | 35.71 ± 1.54 | 1.52 | 357.1 | 341.9 | 23.55 | 0 | 0 | 0 | 0 |  |
| 302 | CpuParticles2DEmissionSphereRadius | 35.70 ± 0.25 | 0.76 | 357.0 | 349.4 | 46.70 | 0 | 0 | 0 | 0 |  |
| 303 | AnimatedSprite3DSpeedScale | 35.62 ± 0.45 | 0.72 | 356.2 | 349.1 | 49.81 | 0 | 0 | 0 | 0 |  |
| 304 | AudioStreamPlayer2DMaxDistance | 35.59 ± 1.80 | 0.75 | 355.9 | 348.4 | 47.63 | 0 | 0 | 0 | 0 |  |
| 305 | Sprite2DFrame | 35.57 ± 1.32 | 1.02 | 355.7 | 345.6 | 35.03 | 0 | 0 | 0 | 0 |  |
| 306 | CpuParticles3DDirection | 35.57 ± 4.50 | 1.47 | 355.7 | 341.0 | 24.13 | 0 | 0 | 0 | 0 |  |
| 307 | GpuParticles3DAmountRatio | 35.52 ± 0.48 | 1.04 | 355.2 | 344.8 | 34.00 | 0 | 0 | 0 | 0 |  |
| 308 | Light3DLightVolumetricFogEnergy | 35.48 ± 1.38 | 1.12 | 354.8 | 343.6 | 31.70 | 0 | 0 | 0 | 0 |  |
| 309 | AudioStreamPlayer3DAttenuationFilterDb | 35.46 ± 1.48 | 0.71 | 354.6 | 347.5 | 50.07 | 0 | 0 | 0 | 0 |  |
| 310 | AudioPitchScale2D | 35.45 ± 0.96 | 0.88 | 354.5 | 345.8 | 40.37 | 0 | 0 | 0 | 0 |  |
| 311 | LightEnergy2D | 35.39 ± 35.87 | 1.26 | 353.9 | 341.4 | 28.17 | 0 | 0 | 0 | 0 |  |
| 312 | AudioStreamPlayer2DAttenuation | 35.26 ± 1.15 | 0.70 | 352.6 | 345.6 | 50.17 | 0 | 0 | 0 | 0 |  |
| 313 | Polygon2DTextureScale | 35.24 ± 1.10 | 0.91 | 352.4 | 343.4 | 38.90 | 0 | 0 | 0 | 0 |  |
| 314 | Vector4 | 32.70 ± 1.21 | 1.06 | 327.0 | 316.3 | 30.71 | 0 | 0 | 0 | 0 |  |
| 315 | Vector3 | 32.46 ± 0.29 | 1.11 | 324.6 | 313.5 | 29.27 | 0 | 0 | 0 | 0 |  |
| 316 | Rect2 | 32.45 ± 2.23 | 1.06 | 324.5 | 313.9 | 30.62 | 0 | 0 | 0 | 0 |  |
| 317 | Vector2 | 32.39 ± 1.38 | 0.28 | 323.9 | 321.1 | 115.36 | 0 | 0 | 0 | 0 |  |
| 318 | Color | 32.12 ± 0.39 | 1.07 | 321.2 | 310.5 | 30.13 | 0 | 0 | 0 | 0 |  |
| 319 | Float | 31.96 ± 1.81 | 0.20 | 319.6 | 317.6 | 157.26 | 0 | 0 | 0 | 0 |  |
| 320 | Double | 31.47 ± 0.15 | 0.21 | 314.7 | 312.6 | 153.03 | 0 | 0 | 0 | 0 |  |
| 321 | ShaderParameter&lt;Color&gt; | 15.31 ± 0.86 | 9.18 | 153.1 | 61.3 | 1.67 | 0 | 0 | 0 | 0 |  |
| 322 | ShaderParameter&lt;Vector4&gt; | 15.19 ± 0.93 | 9.21 | 151.9 | 59.8 | 1.65 | 0 | 0 | 0 | 0 |  |
| 323 | ShaderParameter&lt;Vector2&gt; | 15.01 ± 0.71 | 8.39 | 150.1 | 66.2 | 1.79 | 0 | 0 | 0 | 0 |  |
| 324 | ShaderParameter&lt;Vector3&gt; | 14.89 ± 0.84 | 8.89 | 148.9 | 60.0 | 1.68 | 0 | 0 | 0 | 0 |  |
| 325 | ShaderParameter&lt;Double&gt; | 14.54 ± 0.66 | 4.21 | 145.4 | 103.3 | 3.45 | 0 | 0 | 0 | 0 |  |
| 326 | ShaderParameter&lt;Single&gt; | 14.39 ± 0.40 | 2.71 | 143.9 | 116.8 | 5.32 | 0 | 0 | 0 | 0 |  |
| 327 | ShaderParameter&lt;Int32&gt; | 14.36 ± 0.37 | 2.46 | 143.6 | 119.0 | 5.84 | 0 | 0 | 0 | 0 |  |
| 328 | Property&lt;Quaternion&gt; | 10.34 ± 0.24 | 3.22 | 103.4 | 71.2 | 3.21 | 0 | 0 | 0 | 0 |  |
| 329 | MaterialUv1ScaleY | 10.02 ± 0.46 | 2.13 | 100.2 | 78.9 | 4.71 | 0 | 0 | 0 | 0 |  |
| 330 | MaterialUV2ScaleY | 10.01 ± 0.57 | 2.11 | 100.1 | 79.0 | 4.74 | 0 | 0 | 0 | 0 |  |
| 331 | MaterialUv1Scale | 9.96 ± 0.65 | 2.12 | 99.6 | 78.4 | 4.69 | 0 | 0 | 0 | 0 |  |
| 332 | MaterialUv1ScaleZ | 9.95 ± 0.40 | 2.14 | 99.5 | 78.1 | 4.65 | 0 | 0 | 0 | 0 |  |
| 333 | MaterialUV2OffsetZ | 9.93 ± 0.28 | 2.15 | 99.3 | 77.8 | 4.62 | 0 | 0 | 0 | 0 |  |
| 334 | MaterialUV2ScaleZ | 9.91 ± 0.24 | 2.11 | 99.1 | 78.0 | 4.70 | 0 | 0 | 0 | 0 |  |
| 335 | MaterialAlbedoAlpha | 9.90 ± 0.16 | 2.21 | 99.0 | 76.9 | 4.48 | 0 | 0 | 0 | 0 |  |
| 336 | MaterialUv1ScaleX | 9.90 ± 0.16 | 2.12 | 99.0 | 77.8 | 4.66 | 0 | 0 | 0 | 0 |  |
| 337 | MaterialUV2OffsetX | 9.89 ± 0.30 | 2.14 | 98.9 | 77.5 | 4.62 | 0 | 0 | 0 | 0 |  |
| 338 | MaterialUv1OffsetZ | 9.89 ± 0.24 | 2.12 | 98.9 | 77.7 | 4.66 | 0 | 0 | 0 | 0 |  |
| 339 | MaterialUv1Offset | 9.89 ± 1.39 | 2.13 | 98.9 | 77.6 | 4.64 | 0 | 0 | 0 | 0 |  |
| 340 | MaterialUv1OffsetX | 9.88 ± 0.16 | 2.14 | 98.8 | 77.4 | 4.62 | 0 | 0 | 0 | 0 |  |
| 341 | MaterialUV2Offset | 9.85 ± 0.40 | 2.12 | 98.5 | 77.3 | 4.64 | 0 | 0 | 0 | 0 |  |
| 342 | MaterialUV2OffsetY | 9.85 ± 0.30 | 2.14 | 98.5 | 77.1 | 4.61 | 0 | 0 | 0 | 0 |  |
| 343 | MaterialUv1OffsetY | 9.78 ± 0.30 | 2.13 | 97.8 | 76.5 | 4.60 | 0 | 0 | 0 | 0 |  |
| 344 | MaterialUV2ScaleX | 9.78 ± 0.24 | 2.13 | 97.8 | 76.5 | 4.59 | 0 | 0 | 0 | 0 |  |
| 345 | MaterialUV2Scale | 9.72 ± 0.13 | 2.13 | 97.2 | 75.9 | 4.57 | 0 | 0 | 0 | 0 |  |
| 346 | MaterialEmission | 9.64 ± 0.32 | 2.15 | 96.4 | 74.9 | 4.49 | 0 | 0 | 0 | 0 |  |
| 347 | MaterialNormalScale | 9.64 ± 0.44 | 1.83 | 96.4 | 78.0 | 5.26 | 0 | 0 | 0 | 0 |  |
| 348 | MaterialAlbedoColor | 9.58 ± 0.33 | 2.17 | 95.8 | 74.1 | 4.41 | 0 | 0 | 0 | 0 |  |
| 349 | MaterialEmissionIntensity | 9.53 ± 0.39 | 1.90 | 95.3 | 76.4 | 5.03 | 0 | 0 | 0 | 0 |  |
| 350 | MaterialMetallicSpecular | 9.50 ± 0.34 | 1.71 | 95.0 | 77.8 | 5.55 | 0 | 0 | 0 | 0 |  |
| 351 | MaterialEmissionEnergyMultiplier | 9.48 ± 0.02 | 1.89 | 94.8 | 75.9 | 5.01 | 0 | 0 | 0 | 0 |  |
| 352 | MaterialMetallic | 9.29 ± 0.41 | 1.69 | 92.9 | 76.0 | 5.50 | 0 | 0 | 0 | 0 |  |
| 353 | MaterialRoughness | 9.22 ± 0.22 | 1.74 | 92.2 | 74.8 | 5.31 | 0 | 0 | 0 | 0 |  |
| 354 | Property&lt;Vector3&gt; | 7.58 ± 0.11 | 1.05 | 75.8 | 65.3 | 7.19 | 0 | 0 | 0 | 0 |  |
| 355 | Property&lt;Vector4&gt; | 7.33 ± 0.17 | 1.05 | 73.3 | 62.8 | 6.98 | 0 | 0 | 0 | 0 |  |
| 356 | Property&lt;Rect2&gt; | 7.29 ± 0.40 | 1.05 | 72.9 | 62.4 | 6.93 | 0 | 0 | 0 | 0 |  |
| 357 | Property&lt;Vector2&gt; | 7.28 ± 0.31 | 0.29 | 72.8 | 69.9 | 25.09 | 0 | 0 | 0 | 0 |  |
| 358 | Property&lt;Color&gt; | 7.25 ± 0.26 | 1.06 | 72.5 | 62.0 | 6.87 | 0 | 0 | 0 | 0 |  |
| 359 | Property&lt;Int32&gt; | 7.17 ± 1.17 | 0.36 | 71.7 | 68.2 | 20.13 | 0 | 0 | 0 | 0 |  |
| 360 | Property&lt;Double&gt; | 7.05 ± 0.08 | 0.23 | 70.5 | 68.2 | 31.06 | 0 | 0 | 0 | 0 |  |
| 361 | Property&lt;Single&gt; | 6.91 ± 0.26 | 0.23 | 69.1 | 66.8 | 30.35 | 0 | 0 | 0 | 0 |  |

## 1,000 targets

| Rank | Definition | Tween µs ± error | Direct µs | ns/target | Overhead ns/target | Ratio | Tween B | Direct B | Work items | Contentions | Outlier |
| ---: | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- |
| 1 | CanvasItemInstanceShaderParameter&lt;Color&gt; | 1845.89 ± 18286.38 | 138.69 | 1845.9 | 1707.2 | 13.31 | 5 | 0 | 0 | 0 | yes |
| 2 | CanvasItemInstanceShaderParameter&lt;Single&gt; | 1810.68 ± 18239.38 | 64.07 | 1810.7 | 1746.6 | 28.26 | 5 | 0 | 0 | 0 | yes |
| 3 | GeometryInstanceShaderParameter&lt;Single&gt; | 991.36 ± 83.62 | 64.47 | 991.4 | 926.9 | 15.38 | 24008 | 0 | 0 | 0 | yes |
| 4 | GeometryInstanceShaderParameter&lt;Int32&gt; | 889.96 ± 366.83 | 65.27 | 890.0 | 824.7 | 13.64 | 5 | 0 | 0 | 0 | yes |
| 5 | GeometryInstanceShaderParameter&lt;Double&gt; | 870.81 ± 188.97 | 65.52 | 870.8 | 805.3 | 13.29 | 5 | 0 | 0 | 0 | yes |
| 6 | PathFollow3DProgressRatio | 863.84 ± 143.46 | 483.43 | 863.8 | 380.4 | 1.79 | 5 | 0 | 0 | 0 | yes |
| 7 | GeometryInstanceShaderParameter&lt;Vector2&gt; | 860.32 ± 431.36 | 122.55 | 860.3 | 737.8 | 7.02 | 5 | 0 | 0 | 0 | yes |
| 8 | GeometryInstanceShaderParameter&lt;Color&gt; | 849.91 ± 442.11 | 137.67 | 849.9 | 712.2 | 6.17 | 5 | 0 | 0 | 0 | yes |
| 9 | GeometryInstanceShaderParameter&lt;Vector3&gt; | 839.47 ± 50.16 | 136.96 | 839.5 | 702.5 | 6.13 | 8 | 0 | 0 | 0 | yes |
| 10 | GeometryInstanceShaderParameter&lt;Vector4&gt; | 827.40 ± 92.21 | 136.46 | 827.4 | 690.9 | 6.06 | 5 | 0 | 0 | 0 | yes |
| 11 | PathFollow3DVOffset | 822.25 ± 46.88 | 410.72 | 822.3 | 411.5 | 2.00 | 5 | 0 | 0 | 0 | yes |
| 12 | PathFollow3DProgress | 804.52 ± 35.88 | 421.67 | 804.5 | 382.9 | 1.91 | 5 | 0 | 0 | 0 | yes |
| 13 | PathFollow3DHOffset | 792.11 ± 163.09 | 410.27 | 792.1 | 381.8 | 1.93 | 5 | 0 | 0 | 0 | yes |
| 14 | ControlOffsets | 720.67 ± 16.51 | 326.04 | 720.7 | 394.6 | 2.21 | 5 | 0 | 0 | 0 | yes |
| 15 | CanvasItemInstanceShaderParameter&lt;Vector3&gt; | 647.83 ± 145.71 | 140.00 | 647.8 | 507.8 | 4.63 | 5 | 0 | 0 | 0 | yes |
| 16 | CanvasItemInstanceShaderParameter&lt;Vector4&gt; | 611.98 ± 27.26 | 138.41 | 612.0 | 473.6 | 4.42 | 5 | 0 | 0 | 0 | yes |
| 17 | CanvasItemInstanceShaderParameter&lt;Vector2&gt; | 599.08 ± 77.89 | 122.24 | 599.1 | 476.8 | 4.90 | 5 | 0 | 0 | 0 | yes |
| 18 | CanvasItemInstanceShaderParameter&lt;Double&gt; | 597.51 ± 57.53 | 63.60 | 597.5 | 533.9 | 9.39 | 5 | 0 | 0 | 0 | yes |
| 19 | Light3DLightTemperature | 590.86 ± 17.27 | 156.69 | 590.9 | 434.2 | 3.77 | 4 | 0 | 0 | 0 | yes |
| 20 | CanvasItemInstanceShaderParameter&lt;Int32&gt; | 581.51 ± 59.96 | 87.04 | 581.5 | 494.5 | 6.68 | 5 | 0 | 0 | 0 | yes |
| 21 | ControlGlobalPositionY | 573.61 ± 8.31 | 187.04 | 573.6 | 386.6 | 3.07 | 4 | 0 | 0 | 0 | yes |
| 22 | ControlGlobalPositionX | 571.01 ± 19.68 | 186.96 | 571.0 | 384.0 | 3.05 | 4 | 0 | 0 | 0 | yes |
| 23 | PathFollow2DProgressRatio | 566.86 ± 18.31 | 201.24 | 566.9 | 365.6 | 2.82 | 4 | 0 | 0 | 0 | yes |
| 24 | PathFollow2DProgress | 543.12 ± 2.51 | 179.06 | 543.1 | 364.1 | 3.03 | 4 | 0 | 0 | 0 | yes |
| 25 | ControlGlobalPosition | 525.71 ± 18.05 | 157.15 | 525.7 | 368.6 | 3.35 | 4 | 0 | 0 | 0 |  |
| 26 | PathFollow2DVOffset | 525.07 ± 3.80 | 162.59 | 525.1 | 362.5 | 3.23 | 4 | 0 | 0 | 0 |  |
| 27 | PathFollow2DHOffset | 515.98 ± 7.83 | 163.19 | 516.0 | 352.8 | 3.16 | 4 | 0 | 0 | 0 |  |
| 28 | ControlPositionX | 515.52 ± 11.12 | 144.34 | 515.5 | 371.2 | 3.57 | 4 | 0 | 0 | 0 |  |
| 29 | ControlPositionY | 509.90 ± 21.71 | 144.68 | 509.9 | 365.2 | 3.52 | 4 | 0 | 0 | 0 |  |
| 30 | ControlPosition | 507.28 ± 5.56 | 136.24 | 507.3 | 371.0 | 3.72 | 4 | 0 | 0 | 0 |  |
| 31 | GlobalRotation3DY | 499.56 ± 74.93 | 140.28 | 499.6 | 359.3 | 3.56 | 4 | 0 | 0 | 0 |  |
| 32 | GlobalRotation3DX | 499.15 ± 9.22 | 136.62 | 499.1 | 362.5 | 3.65 | 4 | 0 | 0 | 0 |  |
| 33 | GlobalRotation3DZ | 497.87 ± 23.56 | 137.11 | 497.9 | 360.8 | 3.63 | 4 | 0 | 0 | 0 |  |
| 34 | Quaternion3D | 497.53 ± 18.89 | 145.17 | 497.5 | 352.4 | 3.43 | 4 | 0 | 0 | 0 |  |
| 35 | ControlSizeX | 496.42 ± 8.97 | 124.20 | 496.4 | 372.2 | 4.00 | 4 | 0 | 0 | 0 |  |
| 36 | ControlSizeY | 493.78 ± 103.13 | 130.01 | 493.8 | 363.8 | 3.80 | 4 | 0 | 0 | 0 |  |
| 37 | GlobalQuaternion3D | 493.01 ± 22.10 | 133.52 | 493.0 | 359.5 | 3.69 | 5 | 0 | 0 | 0 |  |
| 38 | ControlSize | 489.72 ± 15.44 | 120.89 | 489.7 | 368.8 | 4.05 | 4 | 0 | 0 | 0 |  |
| 39 | ControlAnchorMin | 487.42 ± 30.31 | 122.59 | 487.4 | 364.8 | 3.98 | 4 | 0 | 0 | 0 |  |
| 40 | ControlAnchorMax | 483.94 ± 19.42 | 124.80 | 483.9 | 359.1 | 3.88 | 4 | 0 | 0 | 0 |  |
| 41 | LightColor3D | 471.35 ± 4.03 | 114.41 | 471.3 | 356.9 | 4.12 | 4 | 0 | 0 | 0 |  |
| 42 | ControlOffsetTop | 466.01 ± 72.50 | 97.38 | 466.0 | 368.6 | 4.79 | 4 | 0 | 0 | 0 |  |
| 43 | Camera3DVOffset | 465.59 ± 76.64 | 111.32 | 465.6 | 354.3 | 4.18 | 4 | 0 | 0 | 0 |  |
| 44 | Camera3DHOffset | 465.36 ± 61.21 | 111.61 | 465.4 | 353.7 | 4.17 | 4 | 0 | 0 | 0 |  |
| 45 | ControlOffsetBottom | 464.89 ± 72.83 | 95.76 | 464.9 | 369.1 | 4.85 | 4 | 0 | 0 | 0 |  |
| 46 | ControlOffsetRight | 459.63 ± 10.79 | 96.62 | 459.6 | 363.0 | 4.76 | 4 | 0 | 0 | 0 |  |
| 47 | ControlOffsetLeft | 459.13 ± 5.39 | 95.23 | 459.1 | 363.9 | 4.82 | 4 | 0 | 0 | 0 |  |
| 48 | Camera2DOffset | 446.03 ± 20.93 | 54.42 | 446.0 | 391.6 | 8.20 | 4 | 0 | 0 | 0 |  |
| 49 | RichTextLabelVisibleRatio | 443.97 ± 82.40 | 74.63 | 444.0 | 369.3 | 5.95 | 4 | 0 | 0 | 0 |  |
| 50 | ControlAnchorRight | 440.57 ± 61.12 | 69.67 | 440.6 | 370.9 | 6.32 | 4 | 0 | 0 | 0 |  |
| 51 | ScrollContainerScrollHorizontal | 438.37 ± 68.61 | 68.65 | 438.4 | 369.7 | 6.39 | 4 | 0 | 0 | 0 |  |
| 52 | RangeValue | 435.06 ± 10.12 | 79.54 | 435.1 | 355.5 | 5.47 | 4 | 0 | 0 | 0 |  |
| 53 | ControlAnchorTop | 433.44 ± 4.06 | 70.29 | 433.4 | 363.2 | 6.17 | 4 | 0 | 0 | 0 |  |
| 54 | ControlAnchorLeft | 430.12 ± 10.29 | 69.94 | 430.1 | 360.2 | 6.15 | 4 | 0 | 0 | 0 |  |
| 55 | AudioVolumeDb | 429.43 ± 318.97 | 47.12 | 429.4 | 382.3 | 9.11 | 4 | 0 | 0 | 0 |  |
| 56 | ControlAnchorBottom | 428.80 ± 57.06 | 69.84 | 428.8 | 359.0 | 6.14 | 4 | 0 | 0 | 0 |  |
| 57 | ScrollContainerScrollVertical | 426.36 ± 52.92 | 79.66 | 426.4 | 346.7 | 5.35 | 4 | 0 | 0 | 0 |  |
| 58 | Parallax2DScrollOffsetX | 426.10 ± 148.47 | 51.10 | 426.1 | 375.0 | 8.34 | 4 | 0 | 0 | 0 |  |
| 59 | Camera2DOffsetY | 425.43 ± 6.01 | 58.47 | 425.4 | 367.0 | 7.28 | 4 | 0 | 0 | 0 |  |
| 60 | GlobalRotation3D | 424.57 ± 38.61 | 69.33 | 424.6 | 355.2 | 6.12 | 1 | 0 | 0 | 0 |  |
| 61 | AnimatedSprite2DFrame | 423.14 ± 9.68 | 65.98 | 423.1 | 357.2 | 6.41 | 4 | 0 | 0 | 0 |  |
| 62 | AnimatedSprite3DFrame | 422.79 ± 3.64 | 66.44 | 422.8 | 356.3 | 6.36 | 4 | 0 | 0 | 0 |  |
| 63 | AudioVolumeLinear | 421.65 ± 18.71 | 52.74 | 421.6 | 368.9 | 7.99 | 4 | 0 | 0 | 0 |  |
| 64 | ControlOffsetTransformScaleX | 418.90 ± 25.22 | 46.24 | 418.9 | 372.7 | 9.06 | 4 | 0 | 0 | 0 |  |
| 65 | GlobalScale2DX | 418.52 ± 17.17 | 57.12 | 418.5 | 361.4 | 7.33 | 4 | 0 | 0 | 0 |  |
| 66 | Camera2DZoomY | 417.85 ± 5.84 | 62.90 | 417.9 | 355.0 | 6.64 | 4 | 0 | 0 | 0 |  |
| 67 | GlobalScale2DY | 417.50 ± 104.22 | 57.26 | 417.5 | 360.2 | 7.29 | 4 | 0 | 0 | 0 |  |
| 68 | Parallax2DAutoscrollY | 417.23 ± 34.19 | 53.08 | 417.2 | 364.1 | 7.86 | 4 | 0 | 0 | 0 |  |
| 69 | Camera2DZoomX | 417.18 ± 38.48 | 64.70 | 417.2 | 352.5 | 6.45 | 4 | 0 | 0 | 0 |  |
| 70 | ControlOffsetTransformPositionRatioY | 417.18 ± 13.48 | 44.81 | 417.2 | 372.4 | 9.31 | 4 | 0 | 0 | 0 |  |
| 71 | ControlOffsetTransformPositionRatioX | 417.08 ± 25.09 | 44.46 | 417.1 | 372.6 | 9.38 | 4 | 0 | 0 | 0 |  |
| 72 | Camera2DOffsetX | 416.37 ± 4.53 | 58.29 | 416.4 | 358.1 | 7.14 | 4 | 0 | 0 | 0 |  |
| 73 | ControlOffsetTransformPivotY | 415.39 ± 60.21 | 44.29 | 415.4 | 371.1 | 9.38 | 4 | 0 | 0 | 0 |  |
| 74 | ControlOffsetTransformScaleY | 414.72 ± 7.57 | 46.26 | 414.7 | 368.5 | 8.97 | 4 | 0 | 0 | 0 |  |
| 75 | Parallax2DScrollOffsetY | 412.63 ± 183.01 | 51.52 | 412.6 | 361.1 | 8.01 | 4 | 0 | 0 | 0 |  |
| 76 | ControlOffsetTransformPositionY | 412.57 ± 1.31 | 43.45 | 412.6 | 369.1 | 9.49 | 4 | 0 | 0 | 0 |  |
| 77 | CanvasLayerScaleY | 412.28 ± 20.63 | 41.05 | 412.3 | 371.2 | 10.04 | 4 | 0 | 0 | 0 |  |
| 78 | ControlOffsetTransformPositionX | 412.03 ± 7.89 | 45.40 | 412.0 | 366.6 | 9.08 | 4 | 0 | 0 | 0 |  |
| 79 | ControlOffsetTransformPivotRatioX | 412.01 ± 15.46 | 44.98 | 412.0 | 367.0 | 9.16 | 4 | 0 | 0 | 0 |  |
| 80 | Parallax2DAutoscrollX | 411.75 ± 43.72 | 53.07 | 411.7 | 358.7 | 7.76 | 4 | 0 | 0 | 0 |  |
| 81 | ControlScaleX | 411.70 ± 18.43 | 37.49 | 411.7 | 374.2 | 10.98 | 4 | 0 | 0 | 0 |  |
| 82 | ControlCustomMaximumSizeX | 411.21 ± 50.84 | 31.99 | 411.2 | 379.2 | 12.86 | 4 | 0 | 0 | 0 |  |
| 83 | GlobalPosition2DX | 411.20 ± 42.58 | 53.52 | 411.2 | 357.7 | 7.68 | 4 | 0 | 0 | 0 |  |
| 84 | Camera2DZoom | 410.94 ± 2.85 | 62.90 | 410.9 | 348.0 | 6.53 | 4 | 0 | 0 | 0 |  |
| 85 | CanvasLayerOffsetX | 410.73 ± 22.25 | 40.66 | 410.7 | 370.1 | 10.10 | 4 | 0 | 0 | 0 |  |
| 86 | ControlScaleY | 409.01 ± 12.91 | 38.53 | 409.0 | 370.5 | 10.62 | 4 | 0 | 0 | 0 |  |
| 87 | ControlOffsetTransformPosition | 408.87 ± 52.13 | 39.79 | 408.9 | 369.1 | 10.27 | 4 | 0 | 0 | 0 |  |
| 88 | ControlPivotOffsetRatioY | 408.79 ± 38.12 | 35.52 | 408.8 | 373.3 | 11.51 | 4 | 0 | 0 | 0 |  |
| 89 | ControlOffsetTransformPivotX | 408.21 ± 115.39 | 46.36 | 408.2 | 361.8 | 8.81 | 4 | 0 | 0 | 0 |  |
| 90 | Scale2DX | 407.80 ± 26.00 | 39.38 | 407.8 | 368.4 | 10.36 | 4 | 0 | 0 | 0 |  |
| 91 | CanvasLayerOffsetY | 406.62 ± 46.34 | 39.96 | 406.6 | 366.7 | 10.18 | 4 | 0 | 0 | 0 |  |
| 92 | ControlOffsetTransformPivotRatioY | 406.30 ± 52.35 | 44.92 | 406.3 | 361.4 | 9.04 | 4 | 0 | 0 | 0 |  |
| 93 | CanvasLayerScale | 406.14 ± 25.21 | 38.29 | 406.1 | 367.8 | 10.61 | 4 | 0 | 0 | 0 |  |
| 94 | GlobalPosition3DY | 406.05 ± 3.63 | 38.38 | 406.1 | 367.7 | 10.58 | 4 | 0 | 0 | 0 |  |
| 95 | CanvasLayerRotation | 405.75 ± 8.32 | 39.66 | 405.7 | 366.1 | 10.23 | 4 | 0 | 0 | 0 |  |
| 96 | CanvasLayerOffset | 405.70 ± 18.91 | 37.07 | 405.7 | 368.6 | 10.94 | 4 | 0 | 0 | 0 |  |
| 97 | Parallax2DAutoscroll | 405.61 ± 20.98 | 49.90 | 405.6 | 355.7 | 8.13 | 4 | 0 | 0 | 0 |  |
| 98 | Position2DX | 404.73 ± 26.86 | 34.66 | 404.7 | 370.1 | 11.68 | 4 | 0 | 0 | 0 |  |
| 99 | ControlOffsetTransformScale | 404.59 ± 4.97 | 40.33 | 404.6 | 364.3 | 10.03 | 4 | 0 | 0 | 0 |  |
| 100 | GlobalPosition2DY | 404.58 ± 164.24 | 47.90 | 404.6 | 356.7 | 8.45 | 4 | 0 | 0 | 0 |  |
| 101 | Parallax2DScrollOffset | 404.51 ± 8.81 | 46.30 | 404.5 | 358.2 | 8.74 | 4 | 0 | 0 | 0 |  |
| 102 | ControlOffsetTransformPivot | 404.45 ± 48.06 | 39.99 | 404.5 | 364.5 | 10.11 | 4 | 0 | 0 | 0 |  |
| 103 | ControlPivotOffsetRatioX | 403.55 ± 10.64 | 35.61 | 403.5 | 367.9 | 11.33 | 4 | 0 | 0 | 0 |  |
| 104 | CanvasLayerScaleX | 401.87 ± 319.73 | 40.18 | 401.9 | 361.7 | 10.00 | 4 | 0 | 0 | 0 |  |
| 105 | ControlPivotOffsetY | 401.76 ± 51.31 | 35.59 | 401.8 | 366.2 | 11.29 | 4 | 0 | 0 | 0 |  |
| 106 | Position2DY | 400.89 ± 25.99 | 35.72 | 400.9 | 365.2 | 11.22 | 4 | 0 | 0 | 0 |  |
| 107 | ControlPivotOffsetX | 400.83 ± 31.84 | 37.72 | 400.8 | 363.1 | 10.63 | 4 | 0 | 0 | 0 |  |
| 108 | GlobalScale2D | 400.82 ± 10.14 | 35.05 | 400.8 | 365.8 | 11.43 | 4 | 0 | 0 | 0 |  |
| 109 | ControlOffsetTransformRotation | 400.75 ± 22.27 | 36.75 | 400.8 | 364.0 | 10.91 | 4 | 0 | 0 | 0 |  |
| 110 | ControlOffsetTransformPositionRatio | 400.30 ± 26.59 | 39.50 | 400.3 | 360.8 | 10.14 | 4 | 0 | 0 | 0 |  |
| 111 | PointLight2DOffsetY | 399.54 ± 4.63 | 31.34 | 399.5 | 368.2 | 12.75 | 4 | 0 | 0 | 0 |  |
| 112 | ControlOffsetTransformPivotRatio | 399.26 ± 10.07 | 39.59 | 399.3 | 359.7 | 10.08 | 4 | 0 | 0 | 0 |  |
| 113 | ColorRectColorAlpha | 399.10 ± 10.88 | 25.00 | 399.1 | 374.1 | 15.97 | 4 | 0 | 0 | 0 |  |
| 114 | ControlRotation | 399.08 ± 35.36 | 32.26 | 399.1 | 366.8 | 12.37 | 4 | 0 | 0 | 0 |  |
| 115 | ControlPivotOffset | 399.04 ± 31.86 | 32.76 | 399.0 | 366.3 | 12.18 | 4 | 0 | 0 | 0 |  |
| 116 | Sprite2DOffsetY | 398.87 ± 165.47 | 28.46 | 398.9 | 370.4 | 14.01 | 4 | 0 | 0 | 0 |  |
| 117 | Position2D | 398.83 ± 17.74 | 31.65 | 398.8 | 367.2 | 12.60 | 4 | 0 | 0 | 0 |  |
| 118 | GlobalRotation2D | 398.35 ± 17.09 | 35.58 | 398.3 | 362.8 | 11.19 | 4 | 0 | 0 | 0 |  |
| 119 | GlobalPosition3DZ | 398.16 ± 9.25 | 38.88 | 398.2 | 359.3 | 10.24 | 4 | 0 | 0 | 0 |  |
| 120 | GlobalSkew2D | 398.07 ± 22.84 | 34.07 | 398.1 | 364.0 | 11.68 | 4 | 0 | 0 | 0 |  |
| 121 | ControlScale | 397.73 ± 69.83 | 34.92 | 397.7 | 362.8 | 11.39 | 4 | 0 | 0 | 0 |  |
| 122 | Sprite2DOffset | 397.27 ± 19.15 | 24.78 | 397.3 | 372.5 | 16.03 | 4 | 0 | 0 | 0 |  |
| 123 | GlobalPosition3DX | 396.86 ± 11.71 | 39.24 | 396.9 | 357.6 | 10.11 | 4 | 0 | 0 | 0 |  |
| 124 | Scale2DY | 396.20 ± 7.68 | 39.87 | 396.2 | 356.3 | 9.94 | 4 | 0 | 0 | 0 |  |
| 125 | Scale2D | 396.03 ± 48.69 | 34.22 | 396.0 | 361.8 | 11.57 | 4 | 0 | 0 | 0 |  |
| 126 | ControlPivotOffsetRatio | 395.39 ± 7.99 | 31.96 | 395.4 | 363.4 | 12.37 | 4 | 0 | 0 | 0 |  |
| 127 | ControlCustomMaximumSize | 393.71 ± 8.13 | 25.47 | 393.7 | 368.2 | 15.46 | 4 | 0 | 0 | 0 |  |
| 128 | GlobalPosition2D | 391.80 ± 25.65 | 32.39 | 391.8 | 359.4 | 12.10 | 4 | 0 | 0 | 0 |  |
| 129 | PointLight2DOffset | 391.56 ± 45.70 | 29.63 | 391.6 | 361.9 | 13.22 | 4 | 0 | 0 | 0 |  |
| 130 | SpriteBase3DModulateAlpha | 391.42 ± 9.83 | 14.88 | 391.4 | 376.5 | 26.31 | 4 | 0 | 0 | 0 |  |
| 131 | LabelVisibleRatio | 391.05 ± 3.37 | 30.28 | 391.1 | 360.8 | 12.92 | 4 | 0 | 0 | 0 |  |
| 132 | PointLight2DTextureScale | 390.53 ± 2.62 | 30.03 | 390.5 | 360.5 | 13.00 | 4 | 0 | 0 | 0 |  |
| 133 | ColorRectColor | 389.14 ± 17.47 | 22.84 | 389.1 | 366.3 | 17.04 | 4 | 0 | 0 | 0 |  |
| 134 | ControlCustomMaximumSizeY | 389.08 ± 97.61 | 31.04 | 389.1 | 358.0 | 12.53 | 4 | 0 | 0 | 0 |  |
| 135 | GlobalPosition3D | 389.04 ± 5.04 | 33.05 | 389.0 | 356.0 | 11.77 | 4 | 0 | 0 | 0 |  |
| 136 | Skew2D | 388.48 ± 21.04 | 35.16 | 388.5 | 353.3 | 11.05 | 4 | 0 | 0 | 0 |  |
| 137 | PointLight2DOffsetX | 387.75 ± 34.25 | 32.55 | 387.8 | 355.2 | 11.91 | 4 | 0 | 0 | 0 |  |
| 138 | Rotation2D | 386.31 ± 4.86 | 33.84 | 386.3 | 352.5 | 11.42 | 4 | 0 | 0 | 0 |  |
| 139 | Rotation3DX | 385.57 ± 23.41 | 17.76 | 385.6 | 367.8 | 21.71 | 4 | 0 | 0 | 0 |  |
| 140 | Scale3DZ | 385.07 ± 8.87 | 17.74 | 385.1 | 367.3 | 21.71 | 4 | 0 | 0 | 0 |  |
| 141 | Rotation3DZ | 385.01 ± 25.90 | 18.03 | 385.0 | 367.0 | 21.35 | 4 | 0 | 0 | 0 |  |
| 142 | Scale3DY | 384.13 ± 114.49 | 18.22 | 384.1 | 365.9 | 21.09 | 4 | 0 | 0 | 0 |  |
| 143 | SelfModulateAlpha | 383.67 ± 9.93 | 20.35 | 383.7 | 363.3 | 18.86 | 4 | 0 | 0 | 0 |  |
| 144 | Camera3DFrustumOffset | 382.85 ± 24.78 | 21.00 | 382.9 | 361.8 | 18.23 | 4 | 0 | 0 | 0 |  |
| 145 | SpriteBase3DOffsetY | 382.21 ± 8.78 | 12.35 | 382.2 | 369.9 | 30.96 | 4 | 0 | 0 | 0 |  |
| 146 | Position3DX | 382.20 ± 10.99 | 16.08 | 382.2 | 366.1 | 23.77 | 4 | 0 | 0 | 0 |  |
| 147 | Sprite2DRegionRect | 382.00 ± 45.96 | 27.44 | 382.0 | 354.6 | 13.92 | 4 | 0 | 0 | 0 |  |
| 148 | ControlCustomMinimumSizeX | 381.86 ± 7.56 | 19.06 | 381.9 | 362.8 | 20.03 | 4 | 0 | 0 | 0 |  |
| 149 | FogVolumeSizeX | 381.28 ± 19.16 | 15.35 | 381.3 | 365.9 | 24.84 | 4 | 0 | 0 | 0 |  |
| 150 | Sprite2DOffsetX | 381.26 ± 21.34 | 27.65 | 381.3 | 353.6 | 13.79 | 4 | 0 | 0 | 0 |  |
| 151 | Polygon2DColorAlpha | 381.05 ± 47.03 | 13.77 | 381.0 | 367.3 | 27.67 | 4 | 0 | 0 | 0 |  |
| 152 | FogVolumeSizeZ | 380.96 ± 17.72 | 15.50 | 381.0 | 365.5 | 24.58 | 4 | 0 | 0 | 0 |  |
| 153 | SpriteBase3DOffsetX | 380.72 ± 99.50 | 12.71 | 380.7 | 368.0 | 29.95 | 4 | 0 | 0 | 0 |  |
| 154 | ModulateAlpha | 380.26 ± 19.09 | 18.45 | 380.3 | 361.8 | 20.61 | 4 | 0 | 0 | 0 |  |
| 155 | Rotation3DY | 380.00 ± 57.63 | 17.74 | 380.0 | 362.3 | 21.42 | 4 | 0 | 0 | 0 |  |
| 156 | Scale3DX | 379.98 ± 60.26 | 17.92 | 380.0 | 362.1 | 21.20 | 4 | 0 | 0 | 0 |  |
| 157 | Light2DShadowColorAlpha | 379.95 ± 17.66 | 20.10 | 379.9 | 359.8 | 18.90 | 4 | 0 | 0 | 0 |  |
| 158 | CpuParticles3DDirectionZ | 379.56 ± 4.03 | 11.86 | 379.6 | 367.7 | 32.01 | 4 | 0 | 0 | 0 |  |
| 159 | ControlCustomMinimumSizeY | 379.11 ± 17.75 | 18.55 | 379.1 | 360.6 | 20.44 | 4 | 0 | 0 | 0 |  |
| 160 | TextureProgressBarTintProgressAlpha | 379.05 ± 26.74 | 13.94 | 379.1 | 365.1 | 27.20 | 4 | 0 | 0 | 0 |  |
| 161 | Rotation3D | 378.83 ± 36.16 | 17.30 | 378.8 | 361.5 | 21.90 | 4 | 0 | 0 | 0 |  |
| 162 | GeometryInstance3DTransparency | 378.79 ± 57.56 | 15.92 | 378.8 | 362.9 | 23.80 | 4 | 0 | 0 | 0 |  |
| 163 | DecalSizeY | 378.66 ± 16.21 | 15.08 | 378.7 | 363.6 | 25.12 | 4 | 0 | 0 | 0 |  |
| 164 | TextureProgressBarTintUnderAlpha | 378.59 ± 7.58 | 13.72 | 378.6 | 364.9 | 27.60 | 4 | 0 | 0 | 0 |  |
| 165 | LightColor2D | 378.55 ± 6.42 | 18.65 | 378.6 | 359.9 | 20.29 | 4 | 0 | 0 | 0 |  |
| 166 | DecalSizeX | 377.88 ± 6.68 | 14.54 | 377.9 | 363.3 | 26.00 | 4 | 0 | 0 | 0 |  |
| 167 | DecalSizeZ | 377.85 ± 8.05 | 14.63 | 377.9 | 363.2 | 25.83 | 4 | 0 | 0 | 0 |  |
| 168 | ControlSizeFlagsStretchRatio | 377.69 ± 10.27 | 17.58 | 377.7 | 360.1 | 21.49 | 4 | 0 | 0 | 0 |  |
| 169 | Camera3DFrustumOffsetX | 377.46 ± 5.87 | 24.31 | 377.5 | 353.1 | 15.52 | 4 | 0 | 0 | 0 |  |
| 170 | Position3DY | 377.25 ± 29.17 | 15.82 | 377.3 | 361.4 | 23.85 | 4 | 0 | 0 | 0 |  |
| 171 | CpuParticles2DGravityX | 377.12 ± 2.37 | 10.62 | 377.1 | 366.5 | 35.50 | 4 | 0 | 0 | 0 |  |
| 172 | DecalModulateAlpha | 376.97 ± 2.92 | 15.18 | 377.0 | 361.8 | 24.83 | 4 | 0 | 0 | 0 |  |
| 173 | CpuParticles3DEmissionBoxExtentsY | 376.77 ± 6.19 | 11.97 | 376.8 | 364.8 | 31.47 | 4 | 0 | 0 | 0 |  |
| 174 | Camera3DFar | 376.65 ± 50.60 | 21.61 | 376.7 | 355.0 | 17.43 | 4 | 0 | 0 | 0 |  |
| 175 | AudioPitchScale3D | 376.49 ± 45.22 | 9.62 | 376.5 | 366.9 | 39.13 | 4 | 0 | 0 | 0 |  |
| 176 | AudioPitchScale | 376.49 ± 86.71 | 9.23 | 376.5 | 367.3 | 40.78 | 4 | 0 | 0 | 0 |  |
| 177 | TextureProgressBarTintOverAlpha | 376.35 ± 3.96 | 13.38 | 376.3 | 363.0 | 28.12 | 4 | 0 | 0 | 0 |  |
| 178 | ControlCustomMinimumSize | 376.02 ± 20.01 | 15.44 | 376.0 | 360.6 | 24.35 | 4 | 0 | 0 | 0 |  |
| 179 | CpuParticles3DColorAlpha | 375.90 ± 103.01 | 11.65 | 375.9 | 364.3 | 32.27 | 4 | 0 | 0 | 0 |  |
| 180 | Camera3DFov | 375.72 ± 21.04 | 21.96 | 375.7 | 353.8 | 17.11 | 4 | 0 | 0 | 0 |  |
| 181 | Camera3DFrustumOffsetY | 375.67 ± 1.71 | 24.45 | 375.7 | 351.2 | 15.37 | 4 | 0 | 0 | 0 |  |
| 182 | TextureProgressBarTextureProgressOffsetX | 375.66 ± 35.73 | 13.21 | 375.7 | 362.4 | 28.45 | 4 | 0 | 0 | 0 |  |
| 183 | SelfModulate | 374.76 ± 17.28 | 19.11 | 374.8 | 355.7 | 19.61 | 4 | 0 | 0 | 0 |  |
| 184 | AudioStreamPlayer3DEmissionAngleDegrees | 374.30 ± 9.14 | 7.58 | 374.3 | 366.7 | 49.40 | 4 | 0 | 0 | 0 |  |
| 185 | Polygon2DTextureOffsetY | 374.12 ± 14.05 | 12.45 | 374.1 | 361.7 | 30.04 | 4 | 0 | 0 | 0 |  |
| 186 | FogVolumeSize | 373.96 ± 21.64 | 17.21 | 374.0 | 356.7 | 21.73 | 4 | 0 | 0 | 0 |  |
| 187 | Label3DModulateAlpha | 373.88 ± 4.68 | 13.85 | 373.9 | 360.0 | 26.99 | 4 | 0 | 0 | 0 |  |
| 188 | Camera3DSize | 373.75 ± 4.24 | 20.88 | 373.7 | 352.9 | 17.90 | 4 | 0 | 0 | 0 |  |
| 189 | CpuParticles3DDirectionY | 373.35 ± 90.99 | 12.00 | 373.4 | 361.3 | 31.10 | 4 | 0 | 0 | 0 |  |
| 190 | Camera3DNear | 373.31 ± 2.93 | 20.92 | 373.3 | 352.4 | 17.84 | 4 | 0 | 0 | 0 |  |
| 191 | CpuParticles3DGravityZ | 373.13 ± 34.53 | 11.85 | 373.1 | 361.3 | 31.50 | 4 | 0 | 0 | 0 |  |
| 192 | Parallax2DScrollScaleX | 373.07 ± 13.46 | 10.77 | 373.1 | 362.3 | 34.64 | 4 | 0 | 0 | 0 |  |
| 193 | CpuParticles2DDirectionX | 372.99 ± 54.36 | 10.70 | 373.0 | 362.3 | 34.85 | 4 | 0 | 0 | 0 |  |
| 194 | OmniRange | 372.30 ± 38.81 | 11.57 | 372.3 | 360.7 | 32.18 | 4 | 0 | 0 | 0 |  |
| 195 | SpriteBase3DModulate | 372.27 ± 50.84 | 16.73 | 372.3 | 355.5 | 22.25 | 4 | 0 | 0 | 0 |  |
| 196 | GpuParticles2DRandomness | 372.09 ± 7.41 | 10.38 | 372.1 | 361.7 | 35.86 | 4 | 0 | 0 | 0 |  |
| 197 | Label3DOffsetY | 371.92 ± 15.53 | 12.59 | 371.9 | 359.3 | 29.53 | 4 | 0 | 0 | 0 |  |
| 198 | CanvasModulateColorAlpha | 371.89 ± 11.80 | 12.27 | 371.9 | 359.6 | 30.32 | 4 | 0 | 0 | 0 |  |
| 199 | TextureProgressBarTextureProgressOffsetY | 371.73 ± 11.29 | 13.16 | 371.7 | 358.6 | 28.24 | 4 | 0 | 0 | 0 |  |
| 200 | GpuParticles3DRandomness | 371.67 ± 30.03 | 10.50 | 371.7 | 361.2 | 35.38 | 4 | 0 | 0 | 0 |  |
| 201 | Polygon2DOffsetX | 371.54 ± 39.43 | 12.36 | 371.5 | 359.2 | 30.05 | 4 | 0 | 0 | 0 |  |
| 202 | Scale3D | 371.53 ± 19.94 | 17.01 | 371.5 | 354.5 | 21.84 | 4 | 0 | 0 | 0 |  |
| 203 | CpuParticles2DEmissionRectExtentsX | 371.45 ± 13.49 | 11.45 | 371.5 | 360.0 | 32.44 | 4 | 0 | 0 | 0 |  |
| 204 | AudioPitchScale2D | 371.45 ± 5.04 | 9.45 | 371.4 | 362.0 | 39.30 | 4 | 0 | 0 | 0 |  |
| 205 | SpotAngle | 371.43 ± 18.70 | 11.93 | 371.4 | 359.5 | 31.15 | 4 | 0 | 0 | 0 |  |
| 206 | SpriteBase3DOffset | 371.10 ± 23.00 | 9.75 | 371.1 | 361.4 | 38.07 | 4 | 0 | 0 | 0 |  |
| 207 | Label3DOutlineModulateAlpha | 370.97 ± 6.43 | 13.59 | 371.0 | 357.4 | 27.31 | 4 | 0 | 0 | 0 |  |
| 208 | GpuParticles2DExplosiveness | 370.92 ± 6.81 | 10.38 | 370.9 | 360.5 | 35.72 | 4 | 0 | 0 | 0 |  |
| 209 | Line2DDefaultColor | 370.75 ± 7.16 | 16.58 | 370.7 | 354.2 | 22.36 | 4 | 0 | 0 | 0 |  |
| 210 | CpuParticles3DGravityY | 370.64 ± 61.39 | 13.22 | 370.6 | 357.4 | 28.03 | 4 | 0 | 0 | 0 |  |
| 211 | DecalModulate | 370.56 ± 33.68 | 17.44 | 370.6 | 353.1 | 21.25 | 4 | 0 | 0 | 0 |  |
| 212 | Light2DShadowColor | 370.55 ± 53.40 | 19.29 | 370.6 | 351.3 | 19.21 | 4 | 0 | 0 | 0 |  |
| 213 | OmniLight3DOmniAttenuation | 370.55 ± 2.63 | 10.95 | 370.6 | 359.6 | 33.85 | 4 | 0 | 0 | 0 |  |
| 214 | Label3DOffsetX | 370.54 ± 12.93 | 12.86 | 370.5 | 357.7 | 28.81 | 4 | 0 | 0 | 0 |  |
| 215 | Polygon2DTextureScaleY | 370.49 ± 17.41 | 12.52 | 370.5 | 358.0 | 29.59 | 4 | 0 | 0 | 0 |  |
| 216 | CpuParticles3DEmissionBoxExtentsX | 370.41 ± 14.56 | 12.19 | 370.4 | 358.2 | 30.40 | 4 | 0 | 0 | 0 |  |
| 217 | TextureProgressBarRadialCenterOffsetX | 370.36 ± 104.66 | 12.12 | 370.4 | 358.2 | 30.56 | 4 | 0 | 0 | 0 |  |
| 218 | RichTextLabelVisibleCharacters | 370.29 ± 40.71 | 14.30 | 370.3 | 356.0 | 25.90 | 4 | 0 | 0 | 0 |  |
| 219 | FogVolumeSizeY | 370.22 ± 28.86 | 15.24 | 370.2 | 355.0 | 24.29 | 4 | 0 | 0 | 0 |  |
| 220 | TextureProgressBarRadialCenterOffsetY | 370.20 ± 54.49 | 12.05 | 370.2 | 358.1 | 30.72 | 4 | 0 | 0 | 0 |  |
| 221 | CpuParticles3DDirectionX | 370.19 ± 13.03 | 11.98 | 370.2 | 358.2 | 30.90 | 4 | 0 | 0 | 0 |  |
| 222 | DecalSize | 370.16 ± 60.50 | 16.55 | 370.2 | 353.6 | 22.37 | 4 | 0 | 0 | 0 |  |
| 223 | CpuParticles3DEmissionBoxExtentsZ | 369.84 ± 41.82 | 12.20 | 369.8 | 357.6 | 30.31 | 4 | 0 | 0 | 0 |  |
| 224 | Position3DZ | 369.82 ± 8.54 | 16.86 | 369.8 | 353.0 | 21.93 | 4 | 0 | 0 | 0 |  |
| 225 | Modulate | 369.77 ± 13.62 | 18.23 | 369.8 | 351.5 | 20.29 | 4 | 0 | 0 | 0 |  |
| 226 | Polygon2DTextureScaleX | 369.65 ± 29.83 | 12.36 | 369.6 | 357.3 | 29.91 | 4 | 0 | 0 | 0 |  |
| 227 | CpuParticles3DGravityX | 369.54 ± 115.35 | 11.62 | 369.5 | 357.9 | 31.79 | 4 | 0 | 0 | 0 |  |
| 228 | Quaternion | 369.49 ± 11.84 | 31.95 | 369.5 | 337.5 | 11.57 | 4 | 0 | 0 | 0 |  |
| 229 | TextureProgressBarRadialCenterOffset | 369.37 ± 22.28 | 9.40 | 369.4 | 360.0 | 39.29 | 4 | 0 | 0 | 0 |  |
| 230 | Polygon2DTextureOffsetX | 369.26 ± 13.20 | 12.31 | 369.3 | 356.9 | 30.00 | 4 | 0 | 0 | 0 |  |
| 231 | AudioStreamPlayer3DMaxDistance | 369.19 ± 24.34 | 7.58 | 369.2 | 361.6 | 48.72 | 4 | 0 | 0 | 0 |  |
| 232 | GpuParticles3DExplosiveness | 369.17 ± 68.35 | 10.51 | 369.2 | 358.7 | 35.13 | 4 | 0 | 0 | 0 |  |
| 233 | CpuParticles2DColorAlpha | 369.14 ± 12.86 | 11.66 | 369.1 | 357.5 | 31.65 | 4 | 0 | 0 | 0 |  |
| 234 | Polygon2DOffset | 369.10 ± 46.68 | 9.08 | 369.1 | 360.0 | 40.64 | 4 | 0 | 0 | 0 |  |
| 235 | SpotRange | 368.73 ± 3.65 | 11.79 | 368.7 | 356.9 | 31.28 | 4 | 0 | 0 | 0 |  |
| 236 | TextureProgressBarTextureProgressOffset | 368.71 ± 7.61 | 9.87 | 368.7 | 358.8 | 37.36 | 4 | 0 | 0 | 0 |  |
| 237 | CpuParticles2DEmissionRectExtentsY | 368.67 ± 16.41 | 11.27 | 368.7 | 357.4 | 32.71 | 4 | 0 | 0 | 0 |  |
| 238 | Polygon2DColor | 368.66 ± 27.81 | 16.14 | 368.7 | 352.5 | 22.85 | 4 | 0 | 0 | 0 |  |
| 239 | LabelVisibleCharacters | 368.65 ± 6.83 | 10.03 | 368.6 | 358.6 | 36.74 | 4 | 0 | 0 | 0 |  |
| 240 | TextureProgressBarTintOver | 368.64 ± 51.15 | 16.75 | 368.6 | 351.9 | 22.00 | 4 | 0 | 0 | 0 |  |
| 241 | AudioStreamPlayer3DUnitSize | 368.64 ± 62.16 | 7.28 | 368.6 | 361.4 | 50.60 | 4 | 0 | 0 | 0 |  |
| 242 | CpuParticles2DDirectionY | 368.42 ± 20.33 | 10.35 | 368.4 | 358.1 | 35.60 | 4 | 0 | 0 | 0 |  |
| 243 | GpuParticles2DSpeedScale | 368.40 ± 5.11 | 10.17 | 368.4 | 358.2 | 36.24 | 4 | 0 | 0 | 0 |  |
| 244 | Line2DDefaultColorAlpha | 368.36 ± 8.23 | 14.06 | 368.4 | 354.3 | 26.20 | 4 | 0 | 0 | 0 |  |
| 245 | Light3DLightVolumetricFogEnergy | 368.14 ± 5.29 | 11.10 | 368.1 | 357.0 | 33.18 | 4 | 0 | 0 | 0 |  |
| 246 | AudioVolumeLinear3D | 368.02 ± 38.75 | 12.20 | 368.0 | 355.8 | 30.16 | 4 | 0 | 0 | 0 |  |
| 247 | Position3D | 368.00 ± 19.23 | 16.91 | 368.0 | 351.1 | 21.76 | 4 | 0 | 0 | 0 |  |
| 248 | Label3DOffset | 367.96 ± 63.61 | 9.42 | 368.0 | 358.5 | 39.05 | 4 | 0 | 0 | 0 |  |
| 249 | Polygon2DOffsetY | 367.87 ± 33.78 | 12.43 | 367.9 | 355.4 | 29.59 | 4 | 0 | 0 | 0 |  |
| 250 | AudioVolumeLinear2D | 367.67 ± 37.49 | 12.27 | 367.7 | 355.4 | 29.96 | 4 | 0 | 0 | 0 |  |
| 251 | TextureProgressBarTintProgress | 367.62 ± 3.84 | 15.94 | 367.6 | 351.7 | 23.06 | 4 | 0 | 0 | 0 |  |
| 252 | Parallax2DScrollScaleY | 367.56 ± 22.21 | 11.83 | 367.6 | 355.7 | 31.07 | 4 | 0 | 0 | 0 |  |
| 253 | SpotLight3DSpotAngleAttenuation | 367.56 ± 81.14 | 11.28 | 367.6 | 356.3 | 32.59 | 4 | 0 | 0 | 0 |  |
| 254 | CpuParticles3DEmissionBoxExtents | 367.53 ± 56.22 | 15.04 | 367.5 | 352.5 | 24.44 | 4 | 0 | 0 | 0 |  |
| 255 | AnimatedSprite2DSpeedScale | 367.50 ± 12.31 | 7.10 | 367.5 | 360.4 | 51.79 | 4 | 0 | 0 | 0 |  |
| 256 | TextureProgressBarRadialFillDegrees | 367.44 ± 46.15 | 9.28 | 367.4 | 358.2 | 39.61 | 4 | 0 | 0 | 0 |  |
| 257 | TextureProgressBarTintUnder | 367.35 ± 4.64 | 15.98 | 367.3 | 351.4 | 22.98 | 4 | 0 | 0 | 0 |  |
| 258 | CpuParticles2DEmissionRectExtents | 367.34 ± 45.47 | 8.01 | 367.3 | 359.3 | 45.84 | 4 | 0 | 0 | 0 |  |
| 259 | CpuParticles2DGravityY | 367.28 ± 9.85 | 10.74 | 367.3 | 356.5 | 34.21 | 4 | 0 | 0 | 0 |  |
| 260 | GpuParticles3DSpeedScale | 367.21 ± 8.31 | 10.67 | 367.2 | 356.5 | 34.40 | 4 | 0 | 0 | 0 |  |
| 261 | GpuParticles2DLifetime | 367.20 ± 16.89 | 10.40 | 367.2 | 356.8 | 35.31 | 4 | 0 | 0 | 0 |  |
| 262 | AnimationPlayerSpeedScale | 367.08 ± 35.48 | 6.98 | 367.1 | 360.1 | 52.58 | 4 | 0 | 0 | 0 |  |
| 263 | SpringArm3DSpringLength | 367.00 ± 9.70 | 8.00 | 367.0 | 359.0 | 45.88 | 4 | 0 | 0 | 0 |  |
| 264 | AudioStreamPlayer3DEmissionAngleFilterAttenuationDb | 366.86 ± 3.21 | 6.96 | 366.9 | 359.9 | 52.68 | 4 | 0 | 0 | 0 |  |
| 265 | LightEnergy3D | 366.71 ± 22.69 | 10.97 | 366.7 | 355.7 | 33.44 | 4 | 0 | 0 | 0 |  |
| 266 | AudioStreamPlayer2DMaxDistance | 366.54 ± 8.57 | 7.35 | 366.5 | 359.2 | 49.84 | 4 | 0 | 0 | 0 |  |
| 267 | CpuParticles3DDirection | 365.97 ± 59.62 | 14.72 | 366.0 | 351.3 | 24.86 | 4 | 0 | 0 | 0 |  |
| 268 | AnimatedSprite3DSpeedScale | 365.81 ± 37.45 | 6.98 | 365.8 | 358.8 | 52.40 | 4 | 0 | 0 | 0 |  |
| 269 | Polygon2DTextureRotation | 365.77 ± 40.11 | 8.41 | 365.8 | 357.4 | 43.51 | 4 | 0 | 0 | 0 |  |
| 270 | CpuParticles2DLifetime | 365.71 ± 3.37 | 7.17 | 365.7 | 358.5 | 50.99 | 4 | 0 | 0 | 0 |  |
| 271 | SpotLight3DSpotAttenuation | 365.61 ± 68.56 | 11.31 | 365.6 | 354.3 | 32.32 | 4 | 0 | 0 | 0 |  |
| 272 | CanvasModulateColor | 365.49 ± 2.31 | 15.95 | 365.5 | 349.5 | 22.92 | 4 | 0 | 0 | 0 |  |
| 273 | CpuParticles2DEmissionSphereRadius | 365.38 ± 27.94 | 7.25 | 365.4 | 358.1 | 50.38 | 4 | 0 | 0 | 0 |  |
| 274 | CpuParticles3DRandomness | 365.31 ± 11.49 | 6.97 | 365.3 | 358.3 | 52.44 | 4 | 0 | 0 | 0 |  |
| 275 | Label3DPixelSize | 365.31 ± 37.26 | 8.40 | 365.3 | 356.9 | 43.48 | 4 | 0 | 0 | 0 |  |
| 276 | Light3DShadowOpacity | 365.13 ± 5.12 | 11.01 | 365.1 | 354.1 | 33.18 | 4 | 0 | 0 | 0 |  |
| 277 | DecalEmissionEnergy | 365.11 ± 25.55 | 10.46 | 365.1 | 354.6 | 34.91 | 4 | 0 | 0 | 0 |  |
| 278 | GpuParticles3DAmountRatio | 364.95 ± 102.92 | 10.32 | 364.9 | 354.6 | 35.37 | 4 | 0 | 0 | 0 |  |
| 279 | CpuParticles2DGravity | 364.87 ± 28.08 | 8.43 | 364.9 | 356.4 | 43.29 | 4 | 0 | 0 | 0 |  |
| 280 | TextureProgressBarRadialInitialAngle | 364.87 ± 41.02 | 11.48 | 364.9 | 353.4 | 31.77 | 4 | 0 | 0 | 0 |  |
| 281 | Sprite2DFrame | 364.84 ± 122.75 | 10.55 | 364.8 | 354.3 | 34.57 | 4 | 0 | 0 | 0 |  |
| 282 | CpuParticles2DRandomness | 364.80 ± 26.17 | 6.97 | 364.8 | 357.8 | 52.33 | 4 | 0 | 0 | 0 |  |
| 283 | Light3DLightIndirectEnergy | 364.78 ± 7.61 | 11.00 | 364.8 | 353.8 | 33.17 | 4 | 0 | 0 | 0 |  |
| 284 | CpuParticles2DColor | 364.77 ± 32.31 | 15.62 | 364.8 | 349.2 | 23.35 | 4 | 0 | 0 | 0 |  |
| 285 | Polygon2DTextureScale | 364.59 ± 19.51 | 9.08 | 364.6 | 355.5 | 40.14 | 4 | 0 | 0 | 0 |  |
| 286 | CpuParticles3DSpeedScale | 364.54 ± 3.00 | 6.95 | 364.5 | 357.6 | 52.44 | 4 | 0 | 0 | 0 |  |
| 287 | CpuParticles2DDirection | 364.52 ± 3.60 | 7.82 | 364.5 | 356.7 | 46.61 | 4 | 0 | 0 | 0 |  |
| 288 | CpuParticles3DGravity | 364.34 ± 10.79 | 15.02 | 364.3 | 349.3 | 24.26 | 4 | 0 | 0 | 0 |  |
| 289 | Label3DOutlineModulate | 364.24 ± 7.78 | 16.80 | 364.2 | 347.4 | 21.68 | 4 | 0 | 0 | 0 |  |
| 290 | AudioVolumeDb3D | 364.10 ± 2.76 | 8.70 | 364.1 | 355.4 | 41.85 | 4 | 0 | 0 | 0 |  |
| 291 | Line2DWidth | 364.07 ± 68.93 | 8.56 | 364.1 | 355.5 | 42.51 | 4 | 0 | 0 | 0 |  |
| 292 | CpuParticles3DEmissionSphereRadius | 363.96 ± 28.60 | 7.20 | 364.0 | 356.8 | 50.53 | 4 | 0 | 0 | 0 |  |
| 293 | Polygon2DTextureOffset | 363.90 ± 1.69 | 9.98 | 363.9 | 353.9 | 36.48 | 4 | 0 | 0 | 0 |  |
| 294 | AudioStreamPlayer3DAttenuationFilterDb | 363.90 ± 28.41 | 6.96 | 363.9 | 356.9 | 52.31 | 4 | 0 | 0 | 0 |  |
| 295 | PointLight2DHeight | 363.82 ± 2.77 | 14.90 | 363.8 | 348.9 | 24.42 | 4 | 0 | 0 | 0 |  |
| 296 | GpuParticles2DAmountRatio | 363.42 ± 8.02 | 10.43 | 363.4 | 353.0 | 34.85 | 4 | 0 | 0 | 0 |  |
| 297 | AudioVolumeDb2D | 363.38 ± 17.26 | 8.73 | 363.4 | 354.7 | 41.63 | 4 | 0 | 0 | 0 |  |
| 298 | CpuParticles3DColor | 363.30 ± 10.59 | 15.74 | 363.3 | 347.6 | 23.08 | 4 | 0 | 0 | 0 |  |
| 299 | CpuParticles3DLifetime | 363.27 ± 21.09 | 7.13 | 363.3 | 356.1 | 50.94 | 4 | 0 | 0 | 0 |  |
| 300 | CpuParticles2DExplosiveness | 363.18 ± 14.13 | 7.04 | 363.2 | 356.1 | 51.60 | 4 | 0 | 0 | 0 |  |
| 301 | LightEnergy2D | 363.06 ± 26.68 | 14.24 | 363.1 | 348.8 | 25.50 | 4 | 0 | 0 | 0 |  |
| 302 | GpuParticles3DLifetime | 363.02 ± 16.47 | 10.39 | 363.0 | 352.6 | 34.95 | 4 | 0 | 0 | 0 |  |
| 303 | CpuParticles2DSpread | 362.57 ± 11.08 | 7.74 | 362.6 | 354.8 | 46.86 | 4 | 0 | 0 | 0 |  |
| 304 | CpuParticles3DSpread | 362.49 ± 3.04 | 6.99 | 362.5 | 355.5 | 51.85 | 4 | 0 | 0 | 0 |  |
| 305 | CpuParticles3DExplosiveness | 362.00 ± 17.38 | 6.99 | 362.0 | 355.0 | 51.79 | 4 | 0 | 0 | 0 |  |
| 306 | Label3DModulate | 361.88 ± 165.38 | 16.73 | 361.9 | 345.1 | 21.62 | 4 | 0 | 0 | 0 |  |
| 307 | AudioStreamPlayer3DAttenuationFilterCutoffHz | 361.49 ± 12.19 | 6.97 | 361.5 | 354.5 | 51.83 | 4 | 0 | 0 | 0 |  |
| 308 | SpriteBase3DPixelSize | 361.44 ± 17.81 | 8.78 | 361.4 | 352.7 | 41.17 | 4 | 0 | 0 | 0 |  |
| 309 | CpuParticles2DSpeedScale | 360.75 ± 18.23 | 8.75 | 360.7 | 352.0 | 41.24 | 4 | 0 | 0 | 0 |  |
| 310 | AudioStreamPlayer3DPanningStrength | 360.69 ± 22.62 | 7.17 | 360.7 | 353.5 | 50.27 | 4 | 0 | 0 | 0 |  |
| 311 | Parallax2DScrollScale | 360.17 ± 12.64 | 7.72 | 360.2 | 352.5 | 46.68 | 4 | 0 | 0 | 0 |  |
| 312 | AudioStreamPlayer2DPanningStrength | 358.48 ± 22.73 | 7.19 | 358.5 | 351.3 | 49.89 | 4 | 0 | 0 | 0 |  |
| 313 | AudioStreamPlayer2DAttenuation | 358.09 ± 2.41 | 6.97 | 358.1 | 351.1 | 51.39 | 4 | 0 | 0 | 0 |  |
| 314 | Vector4 | 338.89 ± 205.03 | 10.56 | 338.9 | 328.3 | 32.10 | 4 | 0 | 0 | 0 |  |
| 315 | Vector3 | 335.00 ± 14.97 | 11.50 | 335.0 | 323.5 | 29.14 | 4 | 0 | 0 | 0 |  |
| 316 | Rect2 | 333.80 ± 22.16 | 10.60 | 333.8 | 323.2 | 31.50 | 4 | 0 | 0 | 0 |  |
| 317 | Color | 333.40 ± 14.02 | 10.59 | 333.4 | 322.8 | 31.48 | 4 | 0 | 0 | 0 |  |
| 318 | Vector2 | 326.33 ± 18.41 | 2.65 | 326.3 | 323.7 | 122.93 | 4 | 0 | 0 | 0 |  |
| 319 | Float | 326.27 ± 36.96 | 1.96 | 326.3 | 324.3 | 166.56 | 4 | 0 | 0 | 0 |  |
| 320 | Double | 325.36 ± 12.89 | 1.95 | 325.4 | 323.4 | 166.88 | 4 | 0 | 0 | 0 |  |
| 321 | ShaderParameter&lt;Vector2&gt; | 167.54 ± 2.60 | 93.78 | 167.5 | 73.8 | 1.79 | 2 | 0 | 0 | 0 |  |
| 322 | ShaderParameter&lt;Color&gt; | 162.88 ± 12.30 | 98.55 | 162.9 | 64.3 | 1.65 | 2 | 0 | 0 | 0 |  |
| 323 | ShaderParameter&lt;Double&gt; | 161.02 ± 44.04 | 32.91 | 161.0 | 128.1 | 4.89 | 2 | 0 | 0 | 0 |  |
| 324 | ShaderParameter&lt;Int32&gt; | 159.77 ± 3.93 | 33.66 | 159.8 | 126.1 | 4.75 | 0 | 0 | 0 | 0 |  |
| 325 | ShaderParameter&lt;Single&gt; | 158.67 ± 23.09 | 33.23 | 158.7 | 125.4 | 4.77 | 0 | 0 | 0 | 0 |  |
| 326 | ShaderParameter&lt;Vector4&gt; | 157.97 ± 2.51 | 98.40 | 158.0 | 59.6 | 1.61 | 2 | 0 | 0 | 0 |  |
| 327 | ShaderParameter&lt;Vector3&gt; | 157.42 ± 2.09 | 99.98 | 157.4 | 57.4 | 1.57 | 2 | 0 | 0 | 0 |  |
| 328 | MaterialUv1ScaleZ | 124.01 ± 34.80 | 36.11 | 124.0 | 87.9 | 3.43 | 2 | 0 | 0 | 0 |  |
| 329 | MaterialUv1Scale | 122.92 ± 40.82 | 35.58 | 122.9 | 87.3 | 3.45 | 2 | 0 | 0 | 0 |  |
| 330 | MaterialUV2Scale | 121.62 ± 2.49 | 35.00 | 121.6 | 86.6 | 3.47 | 2 | 0 | 0 | 0 |  |
| 331 | MaterialUV2OffsetY | 121.57 ± 3.38 | 36.79 | 121.6 | 84.8 | 3.30 | 0 | 0 | 0 | 0 |  |
| 332 | MaterialUV2Offset | 121.48 ± 10.70 | 35.51 | 121.5 | 86.0 | 3.42 | 2 | 0 | 0 | 0 |  |
| 333 | MaterialUv1OffsetX | 121.11 ± 7.94 | 34.91 | 121.1 | 86.2 | 3.47 | 2 | 0 | 0 | 0 |  |
| 334 | MaterialUv1Offset | 121.04 ± 7.84 | 34.46 | 121.0 | 86.6 | 3.51 | 2 | 0 | 0 | 0 |  |
| 335 | MaterialUV2OffsetX | 120.98 ± 2.57 | 36.58 | 121.0 | 84.4 | 3.31 | 2 | 0 | 0 | 0 |  |
| 336 | MaterialUV2ScaleX | 119.99 ± 1.75 | 35.03 | 120.0 | 85.0 | 3.43 | 0 | 0 | 0 | 0 |  |
| 337 | MaterialUV2OffsetZ | 119.91 ± 3.36 | 36.64 | 119.9 | 83.3 | 3.27 | 2 | 0 | 0 | 0 |  |
| 338 | MaterialUv1ScaleY | 119.87 ± 6.20 | 35.95 | 119.9 | 83.9 | 3.33 | 0 | 0 | 0 | 0 |  |
| 339 | MaterialUV2ScaleY | 119.72 ± 8.48 | 35.71 | 119.7 | 84.0 | 3.35 | 2 | 0 | 0 | 0 |  |
| 340 | MaterialUv1OffsetY | 119.68 ± 4.89 | 36.33 | 119.7 | 83.3 | 3.29 | 2 | 0 | 0 | 0 |  |
| 341 | MaterialUV2ScaleZ | 119.60 ± 8.04 | 36.20 | 119.6 | 83.4 | 3.30 | 2 | 0 | 0 | 0 |  |
| 342 | MaterialUv1ScaleX | 119.34 ± 7.15 | 35.68 | 119.3 | 83.7 | 3.34 | 2 | 0 | 0 | 0 |  |
| 343 | MaterialRoughness | 119.31 ± 5.29 | 29.81 | 119.3 | 89.5 | 4.00 | 1 | 0 | 0 | 0 |  |
| 344 | MaterialAlbedoColor | 119.23 ± 1.61 | 33.24 | 119.2 | 86.0 | 3.59 | 0 | 0 | 0 | 0 |  |
| 345 | MaterialEmissionEnergyMultiplier | 119.18 ± 24.43 | 34.21 | 119.2 | 85.0 | 3.48 | 2 | 0 | 0 | 0 |  |
| 346 | MaterialUv1OffsetZ | 118.80 ± 4.80 | 35.27 | 118.8 | 83.5 | 3.37 | 2 | 0 | 0 | 0 |  |
| 347 | MaterialAlbedoAlpha | 117.69 ± 4.01 | 33.57 | 117.7 | 84.1 | 3.51 | 2 | 0 | 0 | 0 |  |
| 348 | MaterialEmission | 117.66 ± 3.46 | 32.29 | 117.7 | 85.4 | 3.64 | 2 | 0 | 0 | 0 |  |
| 349 | MaterialMetallicSpecular | 116.22 ± 4.80 | 28.19 | 116.2 | 88.0 | 4.12 | 1 | 0 | 0 | 0 |  |
| 350 | MaterialEmissionIntensity | 116.19 ± 0.93 | 36.21 | 116.2 | 80.0 | 3.21 | 0 | 0 | 0 | 0 |  |
| 351 | MaterialNormalScale | 115.57 ± 2.71 | 33.95 | 115.6 | 81.6 | 3.40 | 0 | 0 | 0 | 0 |  |
| 352 | MaterialMetallic | 115.14 ± 5.59 | 29.12 | 115.1 | 86.0 | 3.95 | 1 | 0 | 0 | 0 |  |
| 353 | Property&lt;Quaternion&gt; | 105.29 ± 2.39 | 32.38 | 105.3 | 72.9 | 3.25 | 2 | 0 | 0 | 0 |  |
| 354 | Property&lt;Vector3&gt; | 76.96 ± 2.34 | 10.82 | 77.0 | 66.1 | 7.11 | 1 | 0 | 0 | 0 |  |
| 355 | Property&lt;Rect2&gt; | 75.40 ± 2.94 | 10.59 | 75.4 | 64.8 | 7.12 | 1 | 0 | 0 | 0 |  |
| 356 | Property&lt;Color&gt; | 74.99 ± 13.35 | 10.59 | 75.0 | 64.4 | 7.08 | 1 | 0 | 0 | 0 |  |
| 357 | Property&lt;Vector4&gt; | 74.26 ± 5.12 | 10.54 | 74.3 | 63.7 | 7.05 | 1 | 0 | 0 | 0 |  |
| 358 | Property&lt;Int32&gt; | 73.48 ± 0.19 | 3.45 | 73.5 | 70.0 | 21.32 | 0 | 0 | 0 | 0 |  |
| 359 | Property&lt;Vector2&gt; | 72.00 ± 6.45 | 2.82 | 72.0 | 69.2 | 25.55 | 1 | 0 | 0 | 0 |  |
| 360 | Property&lt;Single&gt; | 71.58 ± 5.12 | 2.19 | 71.6 | 69.4 | 32.74 | 0 | 0 | 0 | 0 |  |
| 361 | Property&lt;Double&gt; | 71.10 ± 1.24 | 2.60 | 71.1 | 68.5 | 27.34 | 0 | 0 | 0 | 0 |  |
