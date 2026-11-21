"""Plots the CSV files written by hoverhere_headless_test (needs pandas and matplotlib).

    hoverhere_headless_test run vrs_descent --heli <Logo600SE_V3_parameter_file.xml> --out out
    py -3 tools/headless/plot_example.py out/vrs_descent_states.csv [out/vrs_descent_bemt_grid.csv]
"""
import sys

import matplotlib.pyplot as plt
import pandas as pd

states = pd.read_csv(sys.argv[1])
fig, axes = plt.subplots(4, 1, sharex=True, figsize=(10, 9))
axes[0].plot(states.time, states.pos_y, label="height [m]")
axes[0].plot(states.time, states.vel_y, label="vertical velocity [m/s]")
axes[1].plot(states.time, states.mainrotor_speed.abs() * 30 / 3.14159265, label="main rotor [rpm]")
axes[1].plot(states.time, states.mr_thrust, label="thrust [N]")
axes[2].plot(states.time, states.roll_deg, label="roll [deg]")
axes[2].plot(states.time, states.pitch_deg, label="pitch [deg]")
axes[2].plot(states.time, states.yaw_deg, label="yaw [deg]")
axes[3].plot(states.time, states.stick_collective, label="collective stick")
axes[3].plot(states.time, states.mr_vortex_ring, label="vortex ring strength")
for ax in axes:
    ax.legend(loc="upper right")
    ax.grid(True)
axes[-1].set_xlabel("time [s]")

if len(sys.argv) > 2:  # BEMT grid: induced velocity of the four radial stations over time (mean over the azimuth)
    grid = pd.read_csv(sys.argv[2])
    mean = grid.groupby(["time", "r_index"]).Vi.mean().unstack()
    fig2, ax2 = plt.subplots(figsize=(10, 4))
    for r in mean.columns:
        ax2.plot(mean.index, mean[r], label=f"radial station {r}")
    ax2.set_xlabel("time [s]")
    ax2.set_ylabel("induced velocity Vi [m/s]")
    ax2.legend()
    ax2.grid(True)
plt.show()
