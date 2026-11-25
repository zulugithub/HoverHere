"""Plot the Re/Ncrit-interpolated airfoil polars exported by the headless C++ tool.

Generate a fresh CSV with the rebuilt executable first:
    hoverhere_headless_test airfoil --heli <parameter_file.xml> --out out/airfoil.csv
    py -3 tools/headless/plot_airfoil.py out/airfoil.csv

The XML polar file can also be plotted directly. Plotting a legacy CSV without
Re/Ncrit columns is rejected because it contains only the old analytic curve.
"""
import math
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

import numpy as np
import pandas as pd
import plotly.graph_objects as go
from plotly.subplots import make_subplots

AIRFOIL_NAMES = {
    "n0012-il": "NACA0012",
    "n0009sm-il": "NACA0009sm-il",
    "naca23012-il": "NACA23012",
    "n8h12-il": "NACA8H12",
    "vr7-il": "VR-7",
}


def monotone_cubic(values: np.ndarray, positions: np.ndarray) -> np.ndarray:
    """Shape-preserving cubic Hermite interpolation matching the C++ BEMT LUT."""
    secants = np.diff(values)
    slopes = np.zeros_like(values)

    def endpoint_slope(d0: float, d1: float) -> float:
        slope = (3.0 * d0 - d1) / 2.0
        if (d0 > 0.0 and slope < 0.0) or (d0 < 0.0 and slope > 0.0):
            return 0.0
        if ((d0 > 0.0) != (d1 > 0.0)) and abs(slope) > 3.0 * abs(d0):
            return 3.0 * d0
        return slope

    slopes[0] = endpoint_slope(secants[0], secants[1])
    interior = secants[:-1] * secants[1:] > 0.0
    left = secants[:-1]
    right = secants[1:]
    slopes[1:-1][interior] = (2.0 * left[interior] * right[interior]) / (
        left[interior] + right[interior]
    )
    slopes[-1] = endpoint_slope(secants[-1], secants[-2])

    clipped = np.clip(positions, 0.0, len(values) - 1.0)
    i = np.minimum(clipped.astype(int), len(values) - 2)
    t = clipped - i
    t2, t3 = t * t, t * t * t
    return (
        (2 * t3 - 3 * t2 + 1) * values[i]
        + (t3 - 2 * t2 + t) * slopes[i]
        + (-2 * t3 + 3 * t2) * values[i + 1]
        + (t3 - t2) * slopes[i + 1]
    )


def post_stall_polar(alpha_deg: np.ndarray, post: ET.Element) -> tuple[np.ndarray, np.ndarray]:
    alpha = np.deg2rad(np.abs(alpha_deg))
    positive = alpha_deg >= 0.0
    cl = np.empty_like(alpha)
    cd = np.empty_like(alpha)

    cl_positive_peak = float(post.attrib["cl_positive_peak_deg"])
    cl_negative_peak = float(post.attrib["cl_negative_peak_deg"])
    cl_positive_amplitude = float(post.attrib["cl_positive_amplitude"])
    cl_negative_amplitude = float(post.attrib["cl_negative_amplitude"])
    cd_positive_min = float(post.attrib["cd_positive_first_min_deg"])
    cd_negative_min = float(post.attrib["cd_negative_first_min_deg"])
    cd_positive_amplitude = float(post.attrib["cd_positive_amplitude_or_offset"])
    cd_negative_amplitude = float(post.attrib["cd_negative_amplitude_or_offset"])

    cl[positive] = np.sin(
        (1.5 / (1.0 - cl_positive_peak / 180.0)) * (alpha[positive] - math.pi)
    ) * cl_positive_amplitude
    cl[~positive] = -np.sin(
        (1.5 / (1.0 - cl_negative_peak / 180.0)) * (alpha[~positive] - math.pi)
    ) * cl_negative_amplitude
    cd[positive] = (
        -np.cos((2.0 / (1.0 - cd_positive_min / 180.0)) * (alpha[positive] - math.pi))
        * cd_positive_amplitude + cd_positive_amplitude
    )
    cd[~positive] = (
        -np.cos((2.0 / (1.0 - cd_negative_min / 180.0)) * (alpha[~positive] - math.pi))
        * cd_negative_amplitude + cd_negative_amplitude
    )
    return cl, cd


def read_xml_polar(path: Path) -> pd.DataFrame:
    root = ET.parse(path).getroot()
    airfoil = AIRFOIL_NAMES.get(root.attrib["name"], root.attrib["name"])
    re_values = [int(value) for value in root.findtext("ReGrid").split()]
    ncrit_values = [float(value) for value in root.findtext("NcritGrid").split()]
    alpha_min = float(root.attrib["alpha_min_deg"])
    alpha_max = float(root.attrib["alpha_max_deg"])
    alpha_step = float(root.attrib["alpha_step_deg"])
    alpha_grid = np.arange(alpha_min, alpha_max + alpha_step * 0.5, alpha_step)
    alpha_deg = np.arange(-180.0, 180.0 + 0.125, 0.25)
    pos = (alpha_deg - alpha_min) / alpha_step
    post = root.find("PostStall")
    cl_post, cd_post = post_stall_polar(alpha_deg, post)

    frames = []
    for re in re_values:
        for ncrit in ncrit_values:
            node = next(
                item for item in root.find("Slices").findall("Slice")
                if int(item.attrib["re"]) == re and float(item.attrib["ncrit"]) == ncrit
            )
            cl_raw = np.fromstring(node.findtext("Cl"), sep=" ")
            cd_raw = np.fromstring(node.findtext("Cd"), sep=" ")
            slice_alpha_min = float(node.attrib.get("alpha_min_deg", str(alpha_min)))
            alpha_max = float(node.attrib.get("alpha_max_deg", "22"))
            alpha_max_end = min(alpha_max, 21.5)
            alpha_max_start = min(16.0, alpha_max_end - 3.0)
            alpha_min_end = max(slice_alpha_min, -21.5)
            alpha_min_start = max(-16.0, alpha_min_end + 3.0)
            grid_alpha_deg = alpha_grid
            grid_cl_post, grid_cd_post = post_stall_polar(grid_alpha_deg, post)
            positive = grid_alpha_deg >= 0.0
            transition = np.empty_like(grid_alpha_deg)
            positive_t = np.clip(
                (grid_alpha_deg[positive] - alpha_max_start) / (alpha_max_end - alpha_max_start),
                0.0, 1.0,
            )
            negative_alpha = -grid_alpha_deg[~positive]
            negative_t = np.clip(
                (negative_alpha + alpha_min_start) / (-alpha_min_end + alpha_min_start),
                0.0, 1.0,
            )
            transition[positive] = positive_t * positive_t * (3.0 - 2.0 * positive_t)
            transition[~positive] = negative_t * negative_t * (3.0 - 2.0 * negative_t)
            lut_weight = 1.0 - transition
            cl_table = lut_weight * cl_raw + (1.0 - lut_weight) * grid_cl_post
            cd_table = lut_weight * cd_raw + (1.0 - lut_weight) * grid_cd_post
            cl = monotone_cubic(cl_table, pos)
            cd = monotone_cubic(cd_table, pos)
            beyond_table = (alpha_deg <= alpha_grid[0]) | (alpha_deg >= alpha_grid[-1])
            cl[beyond_table] = cl_post[beyond_table]
            cd[beyond_table] = cd_post[beyond_table]
            frames.append(pd.DataFrame({
                "airfoil": airfoil, "alpha_deg": alpha_deg, "re": re, "ncrit": ncrit, "curve_style": "solid",
                "cl": cl, "cd": cd,
            }))
            if re == 200000:
                frames.append(pd.DataFrame({
                    "airfoil": airfoil, "alpha_deg": alpha_deg, "re": re, "ncrit": ncrit, "curve_style": "dash",
                    "cl": cl, "cd": cd,
                }))
    return pd.concat(frames, ignore_index=True)


def plot_polars(polar: pd.DataFrame) -> None:
    required = {"alpha_deg", "re", "ncrit", "curve_style", "cl", "cd"}
    if not required.issubset(polar.columns):
        raise SystemExit(
            "This CSV contains only the old analytic polar. Rebuild the headless executable, "
            "regenerate the CSV with the airfoil command, then plot it again."
        )

    polar = polar[
        ((polar["curve_style"] == "solid") & (polar["ncrit"] == 9.0))
        | ((polar["curve_style"] == "dash") & (polar["re"] == 200000))
    ]
    profile_names = sorted(polar["airfoil"].dropna().astype(str).unique()) if "airfoil" in polar else []
    title = f"{', '.join(profile_names)} airfoil polars" if profile_names else "Airfoil polars"
    re_values = sorted(polar["re"].unique())
    cl_colors = {re: color for re, color in zip(re_values, ("#e5a0a0", "#d67d7d", "#c45c5c", "#a83b3b"))}
    cd_colors = {re: color for re, color in zip(re_values, ("#9ecae1", "#6baed6", "#4292c6", "#2171b5"))}
    fig = make_subplots(
        rows=2,
        cols=2,
        subplot_titles=("Cl: 360 deg", "Cl: -25 ... +25 deg", "Cd: 360 deg", "Cd: -25 ... +25 deg"),
    )
    for (re, ncrit, curve_style), curve in polar.groupby(["re", "ncrit", "curve_style"], sort=False):
        dash = "dash" if curve_style == "dash" else "solid"
        for quantity, row, color in (("cl", 1, cl_colors[re]), ("cd", 2, cd_colors[re])):
            fig.add_trace(
                go.Scatter(
                    x=curve["alpha_deg"], y=curve[quantity],
                    name=f"{quantity.upper()} Re={int(re):,} Ncrit={ncrit:g} {curve_style}",
                    mode="lines", line=dict(color=color, dash=dash, width=2), opacity=0.75,
                ),
                row=row, col=1,
            )
            curve_zoom = curve[curve["alpha_deg"].abs() <= 25.0]
            fig.add_trace(
                go.Scatter(
                    x=curve_zoom["alpha_deg"], y=curve_zoom[quantity],
                    name=f"{quantity.upper()} Re={int(re):,} Ncrit={ncrit:g} {curve_style}",
                    mode="lines", line=dict(color=color, dash=dash, width=2), opacity=0.75, showlegend=False,
                ),
                row=row, col=2,
            )

    fig.update_xaxes(title_text="alpha [deg]", row=1, col=1)
    fig.update_xaxes(title_text="alpha [deg]", row=1, col=2)
    fig.update_xaxes(title_text="alpha [deg]", row=2, col=1)
    fig.update_xaxes(title_text="alpha [deg]", row=2, col=2)
    fig.update_yaxes(title_text="Cl [-]", row=1, col=1)
    fig.update_yaxes(title_text="Cl [-]", row=1, col=2)
    fig.update_yaxes(title_text="Cd [-]", row=2, col=1)
    fig.update_yaxes(title_text="Cd [-]", row=2, col=2)
    fig.update_layout(
        template="plotly_white", hovermode="x unified", hoverlabel=dict(namelength=-1, font_size=13),
        height=850, title=title,
    )
    fig.show()


def main() -> None:
    if len(sys.argv) < 2:
        sys.exit(__doc__)

    source = Path(sys.argv[1])
    if source.suffix.lower() == ".xml":
        data = read_xml_polar(source)
    else:
        data = pd.read_csv(source)
    plot_polars(data)


if __name__ == "__main__":
    main()
