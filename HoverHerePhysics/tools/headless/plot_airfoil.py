"""Plots the airfoil polar written by the C++ code (needs pandas and plotly). It contains no airfoil formula of its own.

    hoverhere_headless_test airfoil --heli <Logo600SE_V3_parameter_file.xml> --out out/airfoil.csv
    py -3 tools/headless/plot_airfoil.py out/airfoil.csv

    example:
    ./build/msvc/bin/Release/hoverhere_headless_test.exe airfoil --heli ../HoverHere/Assets/Helicopters_Available/Logo600SE_V3/Resources/Logo600SE_V3_parameter_file.xml --out out/airfoil.csv
    py -3 tools/headless/plot_airfoil.py out/airfoil.csv
"""
import sys

import pandas as pd
import plotly.graph_objects as go
from plotly.subplots import make_subplots

if len(sys.argv) < 2:
    sys.exit(__doc__)

polar = pd.read_csv(sys.argv[1])
rotors = [("mr", "main rotor"), ("tr", "tail rotor")]
zoom = polar[polar.alpha_deg.abs() <= 25]

fig = make_subplots(
    rows=2,
    cols=2,
    subplot_titles=("360 deg overview", "zoom -25 ... +25 deg", "glide ratio cl / cd (zoom)", "Lilienthal polar cl over cd"),
)
for k, (key, label) in enumerate(rotors):
    dash = "dash" if k else "solid"
    cl, cd = f"cl_{key}", f"cd_{key}"
    fig.add_trace(go.Scatter(x=polar.alpha_deg, y=polar[cl], name=f"cl {label}", line=dict(color="#1f77b4", dash=dash)), row=1, col=1)
    fig.add_trace(go.Scatter(x=polar.alpha_deg, y=polar[cd], name=f"cd {label}", line=dict(color="#d62728", dash=dash)), row=1, col=1)
    fig.add_trace(go.Scatter(x=zoom.alpha_deg, y=zoom[cl], name=f"cl {label}", line=dict(color="#1f77b4", dash=dash), showlegend=False), row=1, col=2)
    fig.add_trace(go.Scatter(x=zoom.alpha_deg, y=zoom[cd], name=f"cd {label}", line=dict(color="#d62728", dash=dash), showlegend=False), row=1, col=2)
    fig.add_trace(go.Scatter(x=zoom.alpha_deg, y=zoom[cl] / zoom[cd], name=f"cl/cd {label}", line=dict(color="#2ca02c", dash=dash)), row=2, col=1)
    fig.add_trace(go.Scatter(x=polar[cd], y=polar[cl], name=f"polar {label}", line=dict(color="#9467bd", dash=dash)), row=2, col=2)

fig.update_xaxes(title_text="alpha [deg]", row=1, col=1)
fig.update_xaxes(title_text="alpha [deg]", row=1, col=2)
fig.update_xaxes(title_text="alpha [deg]", row=2, col=1)
fig.update_xaxes(title_text="cd [-]", row=2, col=2)
fig.update_yaxes(title_text="cl, cd [-]", row=1, col=1)
fig.update_yaxes(title_text="cl, cd [-]", row=1, col=2)
fig.update_yaxes(title_text="cl / cd [-]", row=2, col=1)
fig.update_yaxes(title_text="cl [-]", row=2, col=2)
fig.update_layout(template="plotly_white", hovermode="x unified", hoverlabel=dict(namelength=-1, font_size=13))
fig.show()
