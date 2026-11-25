#!/usr/bin/env python3
"""Fetch XFOIL polars and generate C++-ready XML lookup tables."""

import io
import sys
import urllib.request
import xml.etree.ElementTree as ET
from pathlib import Path
from xml.dom import minidom

import numpy as np
import pandas as pd

AIRFOILS = ("n0012-il", "n0009sm-il", "naca23012-il", "n8h12-il", "vr7-il")
RE_LIST = (50000, 100000, 200000, 500000)
NCRIT_LIST = (1.0, 5.0, 9.0)
ALPHA_MIN_DEG = -22.0
ALPHA_MAX_DEG = 22.0
ALPHA_STEP_DEG = 0.5
ALPHA_GRID = np.arange(ALPHA_MIN_DEG, ALPHA_MAX_DEG + 1e-6, ALPHA_STEP_DEG)


def fetch_polar(airfoil: str, re: int, ncrit: int) -> pd.DataFrame:
    polar = f"xf-{airfoil}-{re}" if ncrit == 9 else f"xf-{airfoil}-{re}-n{ncrit}"
    url = f"http://airfoiltools.com/polar/csv?polar={polar}"
    request = urllib.request.Request(url, headers={"User-Agent": "Mozilla/5.0"})
    with urllib.request.urlopen(request, timeout=20) as response:
        text = response.read().decode("utf-8")
    header = next((i for i, line in enumerate(text.splitlines()) if line.startswith("Alpha,Cl,Cd")), None)
    if header is None:
        raise ValueError(f"no XFOIL data returned for {polar}")
    frame = pd.read_csv(io.StringIO("\n".join(text.splitlines()[header:])))
    frame = frame[np.isfinite(frame["Alpha"]) & np.isfinite(frame["Cl"]) & np.isfinite(frame["Cd"])]
    if frame.empty:
        raise ValueError(f"no valid XFOIL rows returned for {polar}")
    return frame.sort_values("Alpha").drop_duplicates("Alpha")


def resample(frame: pd.DataFrame) -> tuple[np.ndarray, np.ndarray, float, float]:
    alpha = frame["Alpha"].to_numpy()
    alpha_min = max(ALPHA_MIN_DEG, float(alpha[0]))
    alpha_max = min(ALPHA_MAX_DEG, float(alpha[-1]))
    if not alpha_min < 0 < alpha_max:
        raise ValueError(f"polar does not span positive and negative angles: {alpha_min} .. {alpha_max}")
    cl = np.interp(ALPHA_GRID, alpha, frame["Cl"].to_numpy())
    cd = np.interp(ALPHA_GRID, alpha, frame["Cd"].to_numpy())
    return cl, cd, alpha_min, alpha_max


def write_xml(
    output: Path,
    airfoil: str,
    slices: dict[tuple[int, float], tuple[np.ndarray, np.ndarray, float, float]],
) -> None:
    root = ET.Element("AirfoilPolar", {
        "name": airfoil,
        "alpha_min_deg": f"{ALPHA_MIN_DEG:.1f}",
        "alpha_max_deg": f"{ALPHA_MAX_DEG:.1f}",
        "alpha_step_deg": f"{ALPHA_STEP_DEG:.1f}",
    })
    root.append(ET.Comment(
        " Source and regeneration: signed-angle data are resampled from public AirfoilTools XFOIL polars for "
        + airfoil + ". AirfoilTools identifiers are used in the URL (n8h12-il is NACA 8H12). The fetch URLs "
        "follow http://airfoiltools.com/polar/csv?polar=xf-" + airfoil
        + "-200000-n5; Ncrit 9 uses the same key without the n9 suffix. Run "
        "py -3 tools/fetch_airfoil_polars.py data/airfoil_polars from HoverHerePhysics; numpy, pandas and "
        "internet access are required. The script fetches Re 50000, 100000, 200000 and 500000 at Ncrit 5 and 9, "
        "then resamples signed Alpha, Cl and Cd from -22 to +22 degrees at 0.5 degree spacing. Slice limits "
        "record the last converged source alpha on each side; endpoint extension is blended to the full-circle "
        "continuation over the final up to three degrees. Ncrit 1 is an estimate extrapolated independently at "
        "each signed alpha from Ncrit 5 and 9 (negative drag is clipped to zero), not an XFOIL run. The C++ "
        "runtime interpolates Re and Ncrit and uses shape-preserving cubic interpolation over signed alpha; it "
        "does not mirror the negative side. Positive and negative post-stall continuations have separate "
        "parameters, initialized from the Re=200000, Ncrit=9 lift extrema."
    ))
    ET.SubElement(root, "ReGrid").text = " ".join(str(re) for re in RE_LIST)
    ET.SubElement(root, "NcritGrid").text = " ".join(f"{n:.1f}" for n in NCRIT_LIST)
    cl_200k_n9, _, alpha_min_200k_n9, alpha_max_200k_n9 = slices[(200000, 9.0)]
    positive = (ALPHA_GRID >= 0) & (ALPHA_GRID <= alpha_max_200k_n9)
    negative = (ALPHA_GRID <= 0) & (ALPHA_GRID >= alpha_min_200k_n9)
    positive_indices = np.flatnonzero(positive)
    negative_indices = np.flatnonzero(negative)
    positive_peak_index = positive_indices[np.argmax(cl_200k_n9[positive])]
    negative_peak_index = negative_indices[np.argmin(cl_200k_n9[negative])]
    slice_parent = ET.SubElement(root, "Slices")
    for re in RE_LIST:
        for ncrit in NCRIT_LIST:
            cl, cd, alpha_min, alpha_max = slices[(re, ncrit)]
            node = ET.SubElement(slice_parent, "Slice", {
                "re": str(re), "ncrit": f"{ncrit:.1f}", "alpha_min_deg": f"{alpha_min:.1f}",
                "alpha_max_deg": f"{alpha_max:.1f}",
            })
            ET.SubElement(node, "Cl").text = " ".join(f"{value:.9g}" for value in cl)
            ET.SubElement(node, "Cd").text = " ".join(f"{value:.9g}" for value in cd)
    ET.SubElement(root, "PostStall", {
        "cl_positive_peak_deg": f"{ALPHA_GRID[positive_peak_index]:.1f}",
        "cl_negative_peak_deg": f"{abs(ALPHA_GRID[negative_peak_index]):.1f}",
        "cl_positive_amplitude": f"{cl_200k_n9[positive_peak_index]:.9g}",
        "cl_negative_amplitude": f"{abs(cl_200k_n9[negative_peak_index]):.9g}",
        "cd_positive_first_min_deg": "-1.0",
        "cd_negative_first_min_deg": "-1.0",
        "cd_positive_amplitude_or_offset": "0.90",
        "cd_negative_amplitude_or_offset": "0.90",
    })
    pretty = minidom.parseString(ET.tostring(root)).toprettyxml(indent="  ")
    output.write_text(pretty, encoding="utf-8")


def main() -> int:
    output_dir = Path(sys.argv[1]) if len(sys.argv) > 1 else Path("airfoil_polars")
    output_dir.mkdir(parents=True, exist_ok=True)
    for airfoil in AIRFOILS:
        slices = {}
        for re in RE_LIST:
            cl5, cd5, alpha_min5, alpha_max5 = resample(fetch_polar(airfoil, re, 5))
            cl9, cd9, alpha_min9, alpha_max9 = resample(fetch_polar(airfoil, re, 9))
            # AirfoilTools publishes Ncrit 5 and 9; Ncrit 1 is an explicitly approximate extrapolation.
            slices[(re, 9.0)] = (cl9, cd9, alpha_min9, alpha_max9)
            slices[(re, 5.0)] = (cl5, cd5, alpha_min5, alpha_max5)
            slices[(re, 1.0)] = (
                cl5 - (cl9 - cl5),
                np.maximum(cd5 - (cd9 - cd5), 0.0),
                max(alpha_min5, alpha_min9),
                min(alpha_max5, alpha_max9),
            )
        write_xml(output_dir / f"{airfoil}_polar.xml", airfoil, slices)
        print(output_dir / f"{airfoil}_polar.xml")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
