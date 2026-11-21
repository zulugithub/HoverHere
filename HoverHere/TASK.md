# Task: Validate and Repair the Main-Rotor Inflow/VRS Model

## Goal

Produce physically defensible, real-time main-rotor predictions for thrust, torque, and induced velocity across hover, climb, axial descent, vortex-ring state (VRS), and windmill-brake regimes.

Do not optimize for visual smoothness alone. The solution must be compared against experimental data at matched rotor loading and geometry, identify where classic momentum/BEMT assumptions cease to apply, and use an explicit model transition there. Do not silently substitute an empirical value and label it a converged BEMT solution.

## User's Reference Profile

The supplied wind-tunnel plot is titled “Typical (wind-tunnel) experimental data” and plots non-dimensional induced velocity against non-dimensional rate of climb:

- Hover is approximately `(Vz/vh, Vi/vh) = (0, 1)`.
- Climb is positive on the horizontal axis; descent is negative.
- The VRS region has a broad induced-velocity peak around `Vz/vh ~= -1.5`, with `Vi/vh ~= 2.4-2.6`.
- At greater descent rates the curve returns toward a lower windmill-brake branch, around `Vi/vh ~= 0.4-0.8` near `Vz/vh ~= -2.5` in the plotted example.
- The points are scattered in VRS; the expected mean trend is broad and continuous, not a numerically exact smooth curve.

The image credits Brown, Simons, Chan, Perry, and Newman, “Modelling the Mean Flow Through a Rotor in Axial Flight Including Vortex Ring Conditions,” 2nd International Basic Research Conference on Rotorcraft Technology, Nanjing, China, 7-9 November 2005. Treat that plot as a qualitative/reference profile until its rotor/loading conditions are recovered.

## Repository and Inputs

Main implementation: `Assets/Scripting/Helicopter_Rotor.cs`

Plotting and CSV comparisons: `Assets/Scripting/Helicopter_Rotor_VRS_test_plot.py`

Batch CSV runner: `Assets/Editor/RotorCsvTestRunner.cs` and `run_rotor_csv_tests.ps1`

Current generated files: `bemt_rotor_test.csv`, `vrs_plot_data.csv`

Approximate digitizations: `naca_tn2474_fig12_digitized.csv`, `veismann_2022_fig4_2b_digitized.csv`

Reference notes: `rotor_measurement_references.md`

User downloaded PDFs:

- `D:\Helikopter2\Documents\NASA_VRS_1951.pdf`
- `D:\Helikopter2\Documents\Veismann_Marcel_2022.pdf`

Unity project version is `6000.5.6f1`. To regenerate CSVs in batch mode, run from the project root:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\run_rotor_csv_tests.ps1
```

The runner uses an isolated temporary Unity project/cache under `%TEMP%\HoverHereRotorCsvTest` because the actual Unity project is open in the user's editor. The first import can take several minutes; later runs should reuse the cache. It copies only the verified CSV outputs back to the project root.

## Current Rotor-Test Configuration

`ExportCompleteBEMTRotorTest` currently uses:

- Rotor radius `R = 0.75 m` (1.5 m diameter).
- Chord `c = 0.055 m`.
- Two blades.
- Test mass `4.5 kg`.
- Rotor speed `2500 rpm`.
- Air density hard-coded to `1.27 kg/m^3` in the BEMT calculation.
- Collective test values `-2, 0, 4, 8 deg`.
- Axial velocity from `-15` to `+10 m/s` in `0.05 m/s` increments.

The rotor has an estimated geometric solidity `sigma ~= B*c/(pi*R) ~= 0.0467`. This is close to the NACA TN 2474 untwisted rotor's effective solidity `0.05`, but blade count, airfoil, planform, Reynolds number, and the definition of solidity are not identical.

The test uses four radial stations, ten azimuthal sectors, and a 20% root cutout. The radial stations are midpoint samples at approximately `r = 0.225, 0.375, 0.525, 0.675 m`. The azimuth spacing is `2*pi/10`. The test traces outward from hover separately in both directions: hover to descent, reset continuation state at hover, then hover to climb. Rows are written back in ascending `Vz` order for plotting.

## Current BEMT Discretization and Equations

At each radial/azimuthal sample:

- `Vc = dr_LBO_LB_dt[r,c].y` is the axial flow component. The test labels its inertial counterpart `Vz_Absolute`; positive means climb and negative means descent.
- `V2 = dr_LBO_LB_dt[r,c].z` is local tangential velocity. `Rotor_Disc_BEMT_Function` currently uses its magnitude in the inflow angle.
- `Vi` is the solver's induced axial velocity. `V0 = Vi + Vc`.
- `phi = atan2(V0, abs(V2))`, `alpha = beta - phi` (wrapped to `[-pi, pi]`).
- Lift and drag use custom smooth `Cl(alpha)` and `Cd(alpha)` approximations, not measured LOGO 600 airfoil polars.
- The per-sector blade-element thrust per radial length is
  `dT_BE/dr = factor1 * (V0^2 + V2^2) * (Cl*cos(phi) - Cd*sin(phi)`
  with `factor1 = 0.5*rho*B*c/c_n`.
- The per-sector annular momentum term is
  `dT_M/dr = factor2 * Vi * V0`
  with `factor2 = 4*pi*r*rho/c_n`.
- The root solver residual is `factor2*Vi*V0 - dT_BE/dr`.
- Actual thrust integration applies an additional factor `0.8` to blade-element thrust for an approximate tip-loss correction. This factor is not included in the root residual; assess whether that inconsistency is acceptable or should be made internally consistent.
- Torque is integrated from the blade-element drag/lift components. The simulator also has a separate power-based rotor torque model outside this disk solver; do not conflate the two torque pathways.
- `Vi_mean` is area-weighted using `dA = r*dr*dPsi` over the discretized disk.

The balancing is per azimuth sector: both blade-element and momentum terms contain `/c_n`, then the sector forces are summed. Verify the annular/sector derivation rather than removing only one factor based on intuition.

## Current Numerical Solve and Ad Hoc Branch Handling

The per-cell solver uses a damped Newton iteration with central-difference derivative, step limit `1 m/s`, up to 60 iterations, and keeps the best-residual iterate. It accepts a root at residual `<= 0.001 N`.

The initial/continuation seed is `Vi_LD[r,c]` when nonzero, otherwise an analytical Johnson-derived `Vi_bridge`. The code currently contains an experimental lower-root probe:

```csharp
if (local_Vz_scaled < -2.0f && Vi * thrust_polarity > 0.0f)
    Vi = 0.0f;
```

This forcibly changes the Newton seed for deep descent. It is unvalidated and may be the source of the current deep-descent probe behavior. The handoff AI must assess and either justify/replace/remove it; do not treat it as established physics.

After solving, a residual-weighted fallback blends toward `Vi_bridge` for `local_Vz_scaled > -2` when best residual exceeds `0.5 N`, reaching full bridge weight by `5 N`. Deep descent does not use this fallback. The exported `BEMT_MaxResidual_N` and `BEMT_NonconvergedElements` record pre-fallback BEMT convergence, so output smoothness can coexist with a non-converged BEMT point.

The raw BEMT value is stored in `Vi_LD` for continuation before VRS correction. The actual force calculation uses the VRS-adjusted value. The VRS correction is blended per element only when BEMT element thrust is aligned with collective polarity; the load gate is ramped over `0.05-0.5 N`.

## Johnson Model in This Code

Relevant functions: `MomentumTheory`, `Johnson_Baseline`, `Johnson_VRS_Correction`, `Johnson_Table3_Algorithm`, `ComputeHoverInducedVelocity`, and the block inside `Rotor_Disc_BEMT_Calculations`.

Johnson parameters currently include:

- Baseline boundaries `VzA=-1.5`, `VzB=-2.1`.
- VRS bounds/points `VzD=-0.2`, `VzN=-0.45`, `VzX=-1.5`, `VzE=-2.0`.
- Horizontal limit `VxM=0.95`.
- Empirical target values `Vz_plus_v_N=0.85`, `Vz_plus_v_X=1.25`.
- Correction factors `zeta=0.5`, `f=1.0`.

The code uses separate upper/lower momentum-theory branches, a smoothed baseline bridge over a “gap,” then an empirical correction anchored at N/X points. It blends the resulting Johnson inflow toward the raw BEMT inflow only where the global coherent-descent gate and per-element thrust-load gate permit it. The VRS test sweep bypasses the time lag; live simulation uses a first-order lag measured in rotor revolutions.

The user describes the invalid BEMT region as approximately `Vz/vh = -2..0`; treat this as a working hypothesis to validate, not a universal boundary. A gap model should be explicit about its validity region, endpoint values and slopes, and transition direction. Compare Johnson against alternatives only after checking their sign and normalization conventions and the actual measured data.

## Sign and Normalization Conventions

- Simulator `Vz_Absolute > 0`: climb; `< 0`: descent.
- User's attached Brown et al. profile uses the same climb-positive convention.
- NACA TN 2474 uses descent-positive horizontal coordinate; negate it for overlay with simulator data.
- The NACA report's `ki/kz` use a thrust-dependent scale approximately `v_T = sqrt(T/(2*rho*A))` at each test point. The current normalized BEMT comparison uses that pointwise scale and excludes non-positive-thrust points where the square root is undefined.
- Veismann Fig. 4.2(b) uses hover-normalized thrust `T/Th` and descent/climb normalized by `vh`; its digitized data uses its own `Vz_over_vh` convention already converted to climb-positive for plotting.
- Do not compare curves using mismatched hover-vs-current-thrust scales, positive-down vs positive-up axes, or non-matched thrust coefficient.

## Experimental References and Applicability

### NACA TN 2474 (1951)

Public record: https://ntrs.nasa.gov/citations/19920074916

This is the best available geometry-scale reference for the induced-velocity question. It reports wind-tunnel force tests on four rotors; the selected Figure 12 rotor is a 6-ft-diameter, three-bladed, constant-chord, untwisted rotor with NACA 0015 sections and effective solidity 0.05. NACA measured thrust, torque, blade angle, and descent speed, then derived mean induced velocity assuming uniform disk inflow. The report itself warns that momentum/vortex theory does not give a useful unique answer in vertical descent.

`naca_tn2474_fig12_digitized.csv` is a manually/pixel-digitized approximation of the empirical fit, not the raw scatter. It covers only the measured-fit domain (approximately `Vz/vT=-2.5..0`). The separately drawn simple-momentum curve at larger descent was intentionally excluded. Verify Figure 12's loading/`CT` series and digitize the proper loading curve(s) before making a quantitative pass/fail judgment. The older PDF is OCR-noisy; consult plotted page 53 and report equations/definitions directly.

### Veismann et al. (2022)

DOI: https://doi.org/10.1063/5.0083761

Figure 4.2(b) reports measured mean thrust normalized by hover thrust versus normalized climb/descent for a reference small-scale rotor. The reference blade is 6 in (152.4 mm) diameter and 15 mm chord, much smaller than the LOGO. `veismann_2022_fig4_2b_digitized.csv` is a 21-point approximate center trend traced from repeated-run markers, not raw measurements. Use it as a qualitative thrust-loss/VRS-shape check, not as a geometry-matched induced-velocity curve.

### Brown et al. (2005) image from user

The supplied image cites R. E. Brown, I. Simons, W. Y. F. Chan, F. J. Perry, and S. Newman, “Modelling the Mean Flow Through a Rotor in Axial Flight Including Vortex Ring Conditions,” 2nd International Basic Research Conference on Rotorcraft Technology, Nanjing, 7-9 November 2005. It shows an aggregated experimental `Vi/vh` versus `Vz/vh` profile with a broad VRS peak near `-1.5`. The original paper/source data is not yet in the repository; find it if possible and verify the curve and rotor-loading context.

## Known Mismatches and Data Caveats

- Earlier diagnostic runs found positive-collective BEMT roots with residuals below `0.001 N` and smooth near-hover curves, but the normalized NACA comparison still differed substantially in descent. At `Vz/vT ~= -1.2`, a previous plot comparison showed NACA `Vi/vT ~= 2.22` versus BEMT roughly `1.55` at `4 deg`; at `-2.5`, BEMT was around `2.9` while the NACA fit was near `0.95`. Regenerate the current CSV and recompute these values before relying on them because the lower-root seed probe has changed since some of those measurements.
- On a previous `4 deg/8 deg` comparison, at `Vz/vh ~= -1.2`, Veismann mean thrust was around `0.80 Th`, while BEMT fixed-collective thrust was `~1.76 Th` and `~1.21 Th`. This is suggestive but not a strict validation because rotor geometry, Reynolds number, and loading control differ.
- The `-2 deg` collective case has previously shown no converged BEMT root around hover, with residuals around `29 N` and full fallback weight. Treat it as an invalid/reverse-thrust case until the model's windmill/negative-thrust balance is addressed.
- `VRS_Blend_Weight`, `VRS_Target_Vi`, radial `Vi`, maximum BEMT residual, non-converged element count, and residual fallback weight are exported in `bemt_rotor_test.csv`; use these to distinguish root behavior from empirical blending.
- Test defaults hard-code `rho=1.27 kg/m^3`, `2500 rpm`, `mass=4.5 kg`; compare `CT/sigma`, Reynolds number, and blade geometry before asserting physical disagreement.
- The test and live simulation share the BEMT calculation, but test mode bypasses transient lag and some runtime behavior. A successful CSV test does not by itself prove in-game temporal stability.

## Handoff Questions and Work Plan

1. Reproduce the current data with `run_rotor_csv_tests.ps1`; inspect runner log and confirm source timestamp/columns.
2. Verify the NACA TN 2474 equations, exact definitions of `ki`, `kz`, and `CT`, Figure 12 curve identity, and whether the digitized curve matches a fixed loading comparable to LOGO `CT/sigma`.
3. Find/obtain the Brown et al. 2005 source or data behind the user's attached typical experimental profile. Prefer this if it gives the same `Vi/vh` convention and descent range.
4. Quantify the BEMT residual roots for representative positive and negative collective across each radial ring. Track roots with continuation from hover in both directions; do not force a sign/root solely to make plots smooth.
5. Re-derive the sector momentum balance and thrust integration, including `/c_n`, radial `dr`, azimuth sector weighting, and the `0.8` thrust loss factor. The residual and force output must represent the same physical balance.
6. Determine exactly where this BEMT implementation loses a physically relevant steady root. Keep the ordinary BEMT branch outside that region; inside it use an explicit VRS/windmill model with value-and-slope continuity at both boundaries.
7. Evaluate Johnson Table 3 as a candidate gap/VRS model, but verify its target values against the chosen measurement, its velocity sign convention, and whether its intended scaling variable is hub horizontal speed or local blade tangential speed.
8. Treat torque as a coupled validation target: compare integrated torque/power with `Q*Omega`, and document differences from the separate live power-based torque model.

## Acceptance Criteria

- No unexplained branch jumps or hard discontinuities across the plotted operating domain.
- Outside the declared invalid momentum-theory region, every reported BEMT point must have a documented residual/convergence status and agree with the intended physical branch.
- Inside the gap/VRS region, transition into and out of the empirical model must be continuous in both value and first derivative unless measurement evidence supports a real discontinuity.
- Normalized comparisons use matching `CT/sigma`, geometry, rotor speed/Reynolds number, sign convention, and normalization; state all remaining mismatches.
- Thrust, torque, and induced velocity must be checked together. Do not use post-process smoothing or a residual fallback as evidence of physical correctness.
- Keep approximate digitizations visibly labeled and preserve source figure/page/normalization metadata.
- Validate both the generated CSV and at least one runtime/in-game trajectory after the mathematical model is changed.
