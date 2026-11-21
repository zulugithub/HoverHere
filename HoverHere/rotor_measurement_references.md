# Rotor Measurement References

These digitized reference curves are comparison targets, not raw experimental datasets. Both were traced approximately from figures in the downloaded reports; marker-level source data was not available in the PDFs.

## NACA TN 2474, Figure 12

Walter Castles Jr. and Robin B. Gray, *Empirical Relation Between Induced Velocity, Thrust, and Rate of Descent of a Helicopter Rotor as Determined by Wind-Tunnel Tests on Four Model Rotors*, NACA TN 2474 (1951). Public record and report: https://ntrs.nasa.gov/citations/19920074916

The selected curve is Figure 12: a 6-ft-diameter, three-bladed, constant-chord, untwisted rotor with NACA 0015 sections and effective solidity 0.05. Its diameter and estimated solidity are close to the LOGO 600 SE test rotor (1.5 m diameter and approximately 0.047 geometric solidity). Blade count, airfoil, blade planform, and Reynolds number are not identical.

NACA reports measured wind-tunnel thrust, torque, blade angle, and descent speed, then calculates mean induced velocity using blade-element theory and a uniform-inflow assumption. The points in `naca_tn2474_fig12_digitized.csv` approximate the report's fitted empirical line, not the original scattered measurement points. The empirical fit is only digitized over its plotted range, approximately $0\leq\lambda_z\leq2.5$; the separate simple-momentum curve at larger descent rates is deliberately excluded. The report's descent coordinate is positive downward; the CSV uses the simulator's climb-positive convention, so its horizontal coordinate is negated. NACA defines both nondimensional velocities using the test-point thrust scale $v_T=\sqrt{T/(2\rho A)}$, not one fixed hover reference.

## Veismann et al. 2022, Figure 4.2(b)

Marcel Veismann et al., *Parametric Study of Small-Scale Rotors in Axial Descent*, Physics of Fluids 34, 035124 (2022), https://doi.org/10.1063/5.0083761

Figure 4.2(b) gives measured mean thrust normalized by hover thrust against normalized climb velocity for the reference rotor. The reference blade has a 6-inch (152.4 mm) diameter and 15 mm chord, much smaller than the LOGO rotor. The curve in `veismann_2022_fig4_2b_digitized.csv` is an approximate center trend traced from the figure's repeated-run markers; it is not raw measurement data. It is included as a qualitative check on the normalized thrust loss and VRS-region location, not as a directly equivalent rotor-performance prediction.

## Plot normalization

The NACA induced-velocity comparison uses the instantaneous BEMT thrust at each point, matching the NACA report's velocity scale:

$$v_T = \sqrt{\frac{T}{2\rho A}}.$$

For each positive-thrust BEMT point, the overlay axes are `Vz_Absolute / v_T` and `Vi_Mean / v_T`. The Veismann thrust comparison uses `Thrust_Z_O / T_h` and the separate hover scale

$$v_h = \sqrt{\frac{T_h}{2\rho A}},$$

matching its figure's `Vc/vh` and `T/Th` axes. These are distinct normalizations and should not be conflated.
