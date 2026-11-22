
"""
Animate HoverHere helicopter states and BEMT induced velocities.

Usage:
    python animation_example.py states.csv bemt_grid.csv

Example:
    ./build/msvc/bin/Release/hoverhere_headless_test.exe run vrs_descent --heli ../HoverHere/Assets/Helicopters_Available/Logo600SE_V3/Resources/Logo600SE_V3_parameter_file.xml --out out
    py -3 tools/headless/animation_example.py out/vrs_descent_states.csv out/vrs_descent_bemt_grid.csv 

Optional:
    --hub-offset 0,0,0
    --vector-scale 0.5
    --step 1

The hub offset is expressed in helicopter body coordinates.
The default is zero because the exact offset still needs verification.
"""

import argparse
import numpy as np
import pandas as pd
import plotly.graph_objects as go


# ------------------------------------------------------------
# Quaternion and coordinate transformations
# ------------------------------------------------------------

def normalize_quaternion(q):
    q = np.asarray(q, dtype=float)
    n = np.linalg.norm(q)
    if n < 1e-12:
        return np.array([1., 0., 0., 0.])
    return q / n


def quaternion_matrix(q):
    """Scalar-first quaternion: [w, x, y, z]."""
    w, x, y, z = normalize_quaternion(q)

    return np.array([
        [1 - 2*(y*y + z*z), 2*(x*y - z*w),
         2*(x*z + y*w)],
        [2*(x*y + z*w), 1 - 2*(x*x + z*z),
         2*(y*z - x*w)],
        [2*(x*z - y*w), 2*(y*z + x*w),
         1 - 2*(x*x + y*y)]
    ])


def rotate_points(R, points):
    return np.asarray(points) @ R.T


def make_line_segments(starts, ends):
    """Create a Plotly Scatter3d line using NaN separators."""
    x, y, z = [], [], []

    for a, b in zip(starts, ends):
        x.extend([a[0], b[0], None])
        y.extend([a[1], b[1], None])
        z.extend([a[2], b[2], None])

    return x, y, z


def line_trace(starts, ends, color, width=4, name=""):
    x, y, z = make_line_segments(starts, ends)

    return go.Scatter3d(
        x=x, y=y, z=z,
        mode="lines",
        line=dict(color=color, width=width),
        name=name
    )


# ------------------------------------------------------------
# Helicopter and rotor geometry
# ------------------------------------------------------------

def helicopter_geometry(state, hub_offset, grid, vector_scale):
    position = np.array([
        state.pos_x,
        state.pos_y,
        state.pos_z
    ], dtype=float)

    # Helicopter body orientation (Right-handed: X=forward, Y=up, Z=right)
    q_body = [
        state.q_w, state.q_x,
        state.q_y, state.q_z
    ]
    R_body = quaternion_matrix(q_body)

    # Body coordinate system (X=red, Y=green/up, Z=blue)
    body_axes = rotate_points(R_body, np.eye(3) * 1.0)
    body_starts = np.tile(position, (3, 1))
    body_ends = position + body_axes

    # Rotor hub position in world coordinates
    hub = position + R_body @ hub_offset

    # Non-rotating rotor disk frame (aligned with body X-Z plane, Y = rotor axis)
    # Remove spin around rotor Y-axis from disc_q so the BEMT azimuth grid stays non-rotating
    q_disk = [
        state.disc_q_w,
        state.disc_q_x,
        state.disc_q_y,
        state.disc_q_z
    ]
    R_disk_spin = quaternion_matrix(q_disk)
    # Project disk up-axis (column 1 = Y) and reconstruct non-spinning disk basis
    disk_up = R_disk_spin[:, 1]
    body_fwd = R_body[:, 0]
    disk_right = np.cross(disk_up, body_fwd)
    if np.linalg.norm(disk_right) > 1e-6:
        disk_right /= np.linalg.norm(disk_right)
    else:
        disk_right = R_body[:, 2]
    disk_fwd = np.cross(disk_right, disk_up)
    R_disk = np.column_stack([disk_fwd, disk_up, disk_right])

    # Rotor disk coordinate axes
    disk_axes = rotate_points(R_disk, np.eye(3) * 0.7)
    disk_starts = np.tile(hub, (3, 1))
    disk_ends = hub + disk_axes

    # Rotor perimeter in horizontal X-Z plane (Y = 0)
    theta = np.linspace(0, 2 * np.pi, 100)
    disk_local = np.column_stack([
        np.cos(theta),
        np.zeros_like(theta),
        np.sin(theta)
    ])
    disk_world = hub + rotate_points(R_disk, disk_local * grid.radius.max())

    # BEMT sampling positions in horizontal X-Z plane (Y = 0)
    psi = grid["psi"].to_numpy()
    radius = grid["radius"].to_numpy()
    vi = grid["Vi"].to_numpy()

    sample_local = np.column_stack([
        radius * np.cos(psi),
        np.zeros(len(grid)),
        radius * np.sin(psi)
    ])
    sample_world = hub + sample_local @ R_disk.T

    # Induced velocity Vi acts downward along negative rotor Y-axis (-disk_up)
    vector_ends = (
        sample_world
        - (vi * vector_scale)[:, None] * disk_up
    )

    return (
        position, hub,
        body_starts, body_ends,
        disk_starts, disk_ends,
        disk_world, sample_world,
        vector_ends
    )



# ------------------------------------------------------------
# Main
# ------------------------------------------------------------

parser = argparse.ArgumentParser()

parser.add_argument("states")
parser.add_argument("grid")

parser.add_argument(
    "--hub-offset",
    default="0,0.28,0",
    help="Rotor hub offset in body coordinates [x,y,z] metres"
)

parser.add_argument(
    "--vector-scale",
    type=float,
    default=0.5,
    help="Scale factor for visualizing Vi vectors"
)

parser.add_argument(
    "--step",
    type=int,
    default=1,
    help="Use every Nth state frame"
)

args = parser.parse_args()

states = pd.read_csv(args.states)
grid = pd.read_csv(args.grid)

hub_offset = np.array(
    [float(x) for x in args.hub_offset.split(",")]
)

if len(hub_offset) != 3:
    raise ValueError("--hub-offset requires three values")

if args.step < 1:
    raise ValueError("--step must be at least 1")

# Sort data
states = states.sort_values("time").reset_index(drop=True)
grid = grid.sort_values(
    ["time", "r_index", "psi_index"]
).reset_index(drop=True)

# Check grid dimensions
grid_sizes = grid.groupby("time").size()

if not (grid_sizes == 40).all():
    print("Warning: Some timestamps do not contain exactly 40 points.")

# Match BEMT data to state timestamps.
grid_by_time = {
    float(t): data.reset_index(drop=True)
    for t, data in grid.groupby("time")
}

# Select animation frames
states = states.iloc[::args.step].reset_index(drop=True)

# ------------------------------------------------------------
# Generate animation frames
# ------------------------------------------------------------

frames = []

for i, state in states.iterrows():

    t = float(state.time)

    if t not in grid_by_time:
        # Allow small floating-point timestamp differences
        available = np.array(list(grid_by_time.keys()))
        nearest = available[np.argmin(np.abs(available - t))]

        if abs(nearest - t) > 1e-6:
            continue

        g = grid_by_time[nearest]
    else:
        g = grid_by_time[t]

    (
        position, hub,
        body_starts, body_ends,
        disk_starts, disk_ends,
        disk_world, sample_world,
        vector_ends
    ) = helicopter_geometry(state, hub_offset, g, args.vector_scale )

    # Body axes: X red, Y green, Z blue
    body_trace = line_trace(
        body_starts, body_ends,
        ["red", "green", "blue"],
        width=7,
        name="Helicopter body axes"
    )

    # Rotor disk axes
    disk_trace = line_trace(
        disk_starts, disk_ends,
        ["orange", "orange", "orange"],
        width=5,
        name="Rotor disk axes"
    )

    # Rotor perimeter
    perimeter_trace = go.Scatter3d(
        x=disk_world[:, 0],
        y=disk_world[:, 1],
        z=disk_world[:, 2],
        mode="lines",
        line=dict(color="orange", width=3),
        name="Rotor disk"
    )

    # BEMT sample locations
    points_trace = go.Scatter3d(
        x=sample_world[:, 0],
        y=sample_world[:, 1],
        z=sample_world[:, 2],
        mode="markers",
        marker=dict(
            size=4,
            color=g["Vi"],
            colorscale="Viridis",
            colorbar=dict(title="Vi [m/s]"),
            showscale=(i == 0)
        ),
        name="BEMT sample points",
        customdata=np.column_stack([
            g["r_index"],
            g["psi_index"],
            g["radius"],
            g["psi"],
            g["Vi"]
        ]),
        hovertemplate=(
            "r_index: %{customdata[0]}<br>"
            "psi_index: %{customdata[1]}<br>"
            "radius: %{customdata[2]:.3f} m<br>"
            "psi: %{customdata[3]:.3f} rad<br>"
            "Vi: %{customdata[4]:.3f} m/s"
            "<extra></extra>"
        )
    )

    # Vi vectors
    velocity_trace = line_trace(
        sample_world,
        vector_ends,
        "magenta",
        width=4,
        name="Induced velocity Vi"
    )

    # Helicopter origin
    origin_trace = go.Scatter3d(
        x=[position[0], hub[0]],
        y=[position[1], hub[1]],
        z=[position[2], hub[2]],
        mode="markers",
        marker=dict(size=5, color="black"),
        name="Body origin / rotor hub"
    )

    frame_data = [
        body_trace,
        disk_trace,
        perimeter_trace,
        points_trace,
        velocity_trace,
        origin_trace
    ]

    frames.append(
        go.Frame(
            data=frame_data,
            name=f"{i}",
            traces=[0, 1, 2, 3, 4, 5]
        )
    )

if not frames:
    raise ValueError("No matching state and BEMT timestamps found.")

# ------------------------------------------------------------
# Create figure
# ------------------------------------------------------------

fig = go.Figure(
    data=frames[0].data,
    frames=frames
)

times = [
    float(states.iloc[int(frame.name)].time)
    for frame in frames
]

slider_steps = [
    dict(
        method="animate",
        args=[
            [frame.name],
            dict(
                mode="immediate",
                frame=dict(duration=0, redraw=True),
                transition=dict(duration=0)
            )
        ],
        label=f"{t:.2f}"
    )
    for frame, t in zip(frames, times)
]

fig.update_layout(
    title="HoverHere - Helicopter and BEMT induced velocity",
    template="plotly_white",

    scene=dict(
        xaxis_title="X [m]",
        yaxis_title="Y [m]",
        zaxis_title="Z [m]",
        xaxis=dict(title="X [m]", range=[-65, 65]),
        yaxis=dict(title="Y [m]", range=[0, 130]),
        zaxis=dict(title="Z [m]", range=[-65, 65]),
        camera=dict(up=dict(x=0, y=1, z=0)),
        aspectmode="cube"
    ),

    width=1100,
    height=850,

    legend=dict(
        x=0.01,
        y=0.99
    ),

    updatemenus=[
        dict(
            type="buttons",
            showactive=False,
            x=0.05,
            y=1.10,
            buttons=[
                dict(
                    label="▶ Play",
                    method="animate",
                    args=[
                        None,
                        dict(
                            frame=dict(
                                duration=40,
                                redraw=True
                            ),
                            transition=dict(duration=0),
                            fromcurrent=True
                        )
                    ]
                ),
                dict(
                    label="❚❚ Pause",
                    method="animate",
                    args=[
                        [None],
                        dict(
                            mode="immediate",
                            frame=dict(duration=0, redraw=False),
                            transition=dict(duration=0)
                        )
                    ]
                )
            ]
        )
    ],

    sliders=[
        dict(
            active=0,
            currentvalue=dict(
                prefix="Simulation time: "
            ),
            steps=slider_steps
        )
    ]
)

fig.show()
