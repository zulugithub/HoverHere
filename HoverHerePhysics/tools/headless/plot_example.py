"""Plots the CSV files written by hoverhere_headless_test (needs pandas and plotly).

    hoverhere_headless_test run vrs_descent --heli <Logo600SE_V3_parameter_file.xml> --out out
    py -3 tools/headless/plot_example.py out/vrs_descent_states.csv [out/vrs_descent_bemt_grid.csv]

    example:
    ./build/msvc/bin/Release/hoverhere_headless_test.exe run vrs_descent --heli ../HoverHere/Assets/Helicopters_Available/Logo600SE_V3/Resources/Logo600SE_V3_parameter_file.xml --out out
    py -3 tools/headless/plot_example.py out/vrs_descent_states.csv out/vrs_descent_bemt_grid.csv
"""
import sys

import pandas as pd
import plotly.graph_objects as go
from plotly.subplots import make_subplots

states = pd.read_csv(sys.argv[1])
fig = make_subplots(rows=4, cols=1, shared_xaxes=True)

fig.add_trace(go.Scatter(x=states.time, y=states.pos_y, name="height [m]"), row=1, col=1)
fig.add_trace(go.Scatter(x=states.time, y=states.vel_y, name="vertical velocity [m/s]"), row=1, col=1)
fig.add_trace(go.Scatter(x=states.time, y=states.mainrotor_speed.abs() * 30 / 3.14159265, name="main rotor [rpm]"), row=2, col=1)
fig.add_trace(go.Scatter(x=states.time, y=states.mr_thrust, name="thrust [N]"), row=2, col=1)
fig.add_trace(go.Scatter(x=states.time, y=states.roll_deg, name="roll [deg]"), row=3, col=1)
fig.add_trace(go.Scatter(x=states.time, y=states.pitch_deg, name="pitch [deg]"), row=3, col=1)
fig.add_trace(go.Scatter(x=states.time, y=states.yaw_deg, name="yaw [deg]"), row=3, col=1)
fig.add_trace(go.Scatter(x=states.time, y=states.stick_collective, name="collective stick"), row=4, col=1)
fig.add_trace(go.Scatter(x=states.time, y=states.mr_vortex_ring, name="vortex ring strength"), row=4, col=1)

fig.update_layout(template="plotly_white", hovermode="x unified", hoverlabel=dict(namelength=-1, font_size=13))
fig.update_xaxes(showgrid=True, title_text="time [s]", row=4, col=1)
fig.update_yaxes(showgrid=True)

fig.show()

if len(sys.argv) > 2:  # BEMT grid: induced velocity of the four radial stations over time (mean over the azimuth)
    grid = pd.read_csv(sys.argv[2])
    mean = grid.groupby(["time", "r_index"]).Vi.mean().unstack()
    fig2 = go.Figure()
    for r in mean.columns:
        fig2.add_trace(go.Scatter(x=mean.index, y=mean[r], name=f"radial station {r}"))
    fig2.update_layout(xaxis_title="time [s]", yaxis_title="induced velocity Vi [m/s]", template="plotly_white", hoverlabel=dict(namelength=-1, font_size=13))
    fig2.show()

if len(sys.argv) > 2:  # BEMT grid: induced velocity of all radial/azimuth stations over time
    grid = pd.read_csv(sys.argv[2])
    fig2 = go.Figure()
    for (r, psi), data in grid.groupby(["r_index", "psi_index"]):
        fig2.add_trace(go.Scatter(
            x=data.time, y=data.Vi, mode="lines",
            name=f"r={r}, psi={psi}"
        ))
    fig2.update_layout(
        xaxis_title="time [s]",
        yaxis_title="induced velocity Vi [m/s]",
        template="plotly_white",
        hovermode="x",
        hoverlabel=dict(namelength=-1, font_size=13)
    )
    fig2.show()
