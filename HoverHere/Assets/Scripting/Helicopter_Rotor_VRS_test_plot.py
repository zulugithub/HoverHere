import numpy as np
import pandas as pd
import plotly.graph_objects as go
from plotly.subplots import make_subplots
from pathlib import Path

# =====================================================================
# 1. PFADE & DATEN LADEN
# =====================================================================
project_root = Path(__file__).resolve().parents[2]

def find_file(filename):
    candidates = [
        project_root / filename,
        project_root / "Build" / filename,
        project_root / "Data" / filename,
        Path(__file__).resolve().parent / filename,
        project_root.parent / "Python" / "Python_Rotor_Test_V2" / filename,
        project_root.parent / "Python" / "Python_Rotor_Test_V2" / "Data" / filename,
    ]
    for c in candidates:
        if c.exists():
            return c
    return None

bemt_csv = find_file("bemt_rotor_test.csv")
vrs_csv = find_file("vrs_plot_data.csv")
naca_csv = find_file("naca_tn2474_fig12_digitized.csv")
veismann_csv = find_file("veismann_2022_fig4_2b_digitized.csv")

if bemt_csv is None:
    raise FileNotFoundError("Konnte bemt_rotor_test.csv weder im Hauptverzeichnis noch in Build/ finden.")

print(f"Lade BEMT-Daten: {bemt_csv}")
df_bemt = pd.read_csv(bemt_csv)
df_vrs = pd.read_csv(vrs_csv) if vrs_csv and vrs_csv.exists() else None
df_naca = pd.read_csv(naca_csv) if naca_csv and naca_csv.exists() else None
df_vei = pd.read_csv(veismann_csv) if veismann_csv and veismann_csv.exists() else None

# =====================================================================
# 2. PHYSIKALISCHE KONSTANTEN FÜR NORMIERUNG
# =====================================================================
R = 0.75
chord = 0.055
num_blades = 2
mass_total = 4.5
rpm = 1500.0
rho_air = 1.27

omega_mr = rpm / 60.0 * 2.0 * np.pi
V_tip = omega_mr * R
A_disk = np.pi * R * R
v_h = np.sqrt((mass_total * 9.81) / (2.0 * rho_air * A_disk))

collectives = sorted(df_bemt['Collective_Deg'].unique())
coll_str = ", ".join([f"{c:g}°" for c in collectives])

# =====================================================================
# 3. 3x3 DASHBOARD INITIALISIEREN (Ein Fenster, exakt wie axial_bemt_rotor_new15.py)
# =====================================================================
fig = make_subplots(
    rows=3, cols=3,
    specs=[[{"type": "table"}, {"type": "xy"}, {"type": "xy"}],
           [{"type": "xy"}, {"type": "xy"}, {"type": "xy"}],
           [{"type": "xy"}, {"type": "xy"}, {"type": "xy"}]],
    subplot_titles=(
        "<b>Rotor & Simulation Parameter (1500 RPM, Unity BEMT)</b>", "<b>Rotor Schub ($F_z$) vs. $V_z$</b>", "<b>Thrust Coeff. ($C_T$) vs. $V_z / v_h$</b>",
        "<b>Profil Lift ($C_L$) & Drag ($C_D$) vs. $\\alpha$</b>", "<b>Mittlere induzierte Geschw. ($V_i$) vs. $V_z$</b>", "<b>Norm. Induced Vel. ($V_i / v_h$) vs. $V_z / v_h$</b>",
        "<b>Johnson VRS Comparison vs. NACA TN 2474</b>", "<b>Rotormoment ($Q$) vs. $V_z$</b>", "<b>Torque Coeff. ($C_Q$) vs. $V_z / v_h$</b>"
    ),
    vertical_spacing=0.10, horizontal_spacing=0.08
)

# ---------------------------------------------------------------------
# Spalte 1 - Zeile 1: Parameter-Tabelle
# ---------------------------------------------------------------------
fig.add_trace(go.Table(
    header=dict(values=["<b>Parameter</b>", "<b>Value</b>"], fill_color='paleturquoise', align='left'),
    cells=dict(values=[
        ["Radius (R)", "Blade Chord", "Blades", "Mass Total", "RPM", "Air Density", "Hover Inflow (vh)", "Collectives"],
        [f"{R} m", f"{chord} m", f"{num_blades}", f"{mass_total} kg", f"{rpm:.1f}", f"{rho_air} kg/m³", f"{v_h:.2f} m/s", coll_str]
    ], fill_color='lavender', align='left')
), row=1, col=1)

# ---------------------------------------------------------------------
# Spalte 1 - Zeile 2: Cl / Cd Kurven (NACA 0012 analytische Repräsentation)
# ---------------------------------------------------------------------
def helper_step(x, x0: float, h0: float, x1: float, h1: float):
    out = np.where(x <= x0, h0, h1)
    mask = (x > x0) & (x < x1)
    a = (x - x0) / (x1 - x0)
    smooth = h0 + (h1 - h0) * a * a * (3.0 - 2.0 * a)
    return np.where(mask, smooth, out)

alphas_deg = np.linspace(-180, 180, 360)
alpha_rad = np.radians(alphas_deg)
abs_alpha = np.abs(alpha_rad)

s_cl = helper_step(abs_alpha, np.radians(12.0), 1.0, np.radians(26.0), 0.0)
cl = (s_cl * (5.0 * abs_alpha) + (1.0 - s_cl) * np.sin((1.5 / (1.0 - 40.0 / 180.0)) * (abs_alpha - np.pi)) * 1.10) * np.sign(alpha_rad)

s_cd = helper_step(abs_alpha, np.radians(20.0), 1.0, np.radians(35.0), 0.0)
cd = s_cd * (1.5 * (abs_alpha ** 3) + 0.011) + (1.0 - s_cd) * (-np.cos((2.0 / 1.0055) * (abs_alpha - np.pi)) * 0.90 + 0.90)

fig.add_trace(go.Scatter(x=alphas_deg, y=cl, mode='lines', name='Lift Coeff (Cl)', line=dict(color='blue', width=2)), row=2, col=1)
fig.add_trace(go.Scatter(x=alphas_deg, y=cd, mode='lines', name='Drag Coeff (Cd)', line=dict(color='red', width=2, dash='dash')), row=2, col=1)

# ---------------------------------------------------------------------
# Spalte 1 - Zeile 3: Johnson VRS Comparison vs. NACA TN 2474
# ---------------------------------------------------------------------
vrs_colors = {0.0: '#1f77b4', 0.3: '#ff7f0e', 0.6: '#2ca02c', 0.7: '#d62728', 0.8: '#9467bd', 0.9: '#8c564b', 1.0: '#e377c2'}
if df_vrs is not None:
    for vx_val in sorted(df_vrs['Vx_scaled'].unique()):
        sub_df = df_vrs[df_vrs['Vx_scaled'] == vx_val]
        color = vrs_colors.get(round(vx_val, 2), '#7f7f7f')
        fig.add_trace(go.Scatter(x=sub_df['Vz_scaled'], y=sub_df['Vi_baseline_scaled'], mode='lines', name=f'Baseline (Vx={vx_val})', line=dict(color=color, width=2), showlegend=False), row=3, col=1)
        fig.add_trace(go.Scatter(x=sub_df['Vz_scaled'], y=sub_df['Vi_final_scaled'], mode='lines', name=f'Final VRS (Vx={vx_val})', line=dict(color=color, width=1.5, dash='dash'), showlegend=False), row=3, col=1)

    df_ideal = df_vrs[df_vrs['Vx_scaled'] == 0.0]
    fig.add_trace(go.Scatter(x=df_ideal['Vz_scaled'], y=df_ideal['Vi_ideal_momentum'], mode='lines', name='Ideale Impulstheorie (Ref)', line=dict(color='black', width=1.5, dash='dot')), row=3, col=1)

if df_naca is not None:
    fig.add_trace(go.Scatter(x=df_naca['Vz_over_vT'], y=df_naca['Vi_over_vT'], mode='lines', name='NACA TN 2474 Fig.12', line=dict(color='#111111', width=2, dash='dot')), row=3, col=1)

# ---------------------------------------------------------------------
# Spalten 2 & 3: BEMT Simulation (Physikalisch & Normiert)
# ---------------------------------------------------------------------
has_vx = 'Vx' in df_bemt.columns
vx_list = sorted(df_bemt['Vx'].unique()) if has_vx else [0.0]
num_vx = len(vx_list)
num_coll = len(collectives)

# Schöne harmonische Farbpalette für die Collectives
palette = [
    '#1f77b4', '#ff7f0e', '#2ca02c', '#d62728', '#9467bd',
    '#8c564b', '#e377c2', '#7f7f7f', '#bcbd22', '#17becf'
]

def get_curve_color(vx_idx, coll_idx):
    if num_vx > 1:
        l_factor = 0.8 - 0.5 * (vx_idx / max(1, num_vx - 1))
        s_factor = 0.6 + 0.4 * (vx_idx / max(1, num_vx - 1))
        hue = (coll_idx - num_coll // 2) * 20.0
        if hue < 0: hue += 360
        return f"hsl({hue:.1f}, {s_factor * 100:.1f}%, {l_factor * 100:.1f}%)"
    else:
        return palette[coll_idx % len(palette)]

for vx_idx, vx_val in enumerate(vx_list):
    df_sub_vx = df_bemt[df_bemt['Vx'] == vx_val] if has_vx else df_bemt

    for coll_idx, coll in enumerate(collectives):
        sub_coll = df_sub_vx[df_sub_vx['Collective_Deg'] == coll].sort_values('Vz_Absolute')
        if sub_coll.empty:
            continue

        color = get_curve_color(vx_idx, coll_idx)
        trace_name = f"Vx={vx_val} m/s, Coll: {coll:g}°" if num_vx > 1 else f"Pitch: {coll:g}°"
        legend_group = f"g_{vx_val}_{coll}"

        # --- Spalte 2: Physikalische Kurven vs. Vz (m/s) ---
        fig.add_trace(go.Scatter(
            x=sub_coll['Vz_Absolute'], y=sub_coll['Thrust_Z_O'],
            mode='lines', name=trace_name, legendgroup=legend_group,
            line=dict(color=color, width=1.5), showlegend=True
        ), row=1, col=2)

        fig.add_trace(go.Scatter(
            x=sub_coll['Vz_Absolute'], y=sub_coll['Vi_Mean'],
            mode='lines', name=trace_name, legendgroup=legend_group,
            line=dict(color=color, width=1.5), showlegend=False
        ), row=2, col=2)

        fig.add_trace(go.Scatter(
            x=sub_coll['Vz_Absolute'], y=sub_coll['Torque_Y_LD'],
            mode='lines', name=trace_name, legendgroup=legend_group,
            line=dict(color=color, width=1.5), showlegend=False
        ), row=3, col=2)

        # --- Spalte 3: Normierte Kurven vs. Vz / vh ---
        vz_norm = sub_coll['Vz_Absolute'] / v_h
        ct_norm = sub_coll['Thrust_Z_O'] / (rho_air * A_disk * V_tip * V_tip)
        vi_norm = sub_coll['Vi_Mean'] / v_h
        cq_norm = sub_coll['Torque_Y_LD'] / (rho_air * A_disk * V_tip * V_tip * R)

        fig.add_trace(go.Scatter(
            x=vz_norm, y=ct_norm,
            mode='lines', name=trace_name, legendgroup=legend_group,
            line=dict(color=color, width=1.5), showlegend=False
        ), row=1, col=3)

        fig.add_trace(go.Scatter(
            x=vz_norm, y=vi_norm,
            mode='lines', name=trace_name, legendgroup=legend_group,
            line=dict(color=color, width=1.5), showlegend=False
        ), row=2, col=3)

        fig.add_trace(go.Scatter(
            x=vz_norm, y=cq_norm,
            mode='lines', name=trace_name, legendgroup=legend_group,
            line=dict(color=color, width=1.5), showlegend=False
        ), row=3, col=3)

# Veismann 2022 Windkanal-Referenzkurve
if df_vei is not None:
    # Skalierung über Modell-Schub bei +4 deg im Schwebeflug (Vz = 0)
    ref_coll = 4.0 if 4.0 in collectives else collectives[-1]
    hover = df_bemt[(df_bemt['Collective_Deg'] == ref_coll) & (df_bemt['Vz_Absolute'].abs() < 0.05)]
    if not hover.empty:
        ct_hover = hover['Thrust_Z_O'].iloc[0] / (rho_air * A_disk * V_tip * V_tip)
        fig.add_trace(go.Scatter(
            x=df_vei['Vz_over_vh'],
            y=df_vei['Thrust_over_Th'] * ct_hover,
            mode='lines+markers',
            name=f'Veismann 2022 Fig.4.2b (skaliert @ {ref_coll:g}° Hover)',
            line=dict(color='#111111', width=2, dash='dot'),
            marker=dict(size=4)
        ), row=1, col=3)

# =====================================================================
# 4. LAYOUT & ACHSENBESCHRIFTUNGEN
# =====================================================================
fig.update_layout(
    title_text="<b>Helicopter Rotor Simulation & VRS Dashboard (Unity Export)</b>",
    template="plotly_white",
    height=1200,
    hovermode="x unified",
    legend=dict(yanchor="top", y=0.99, xanchor="left", x=1.01)
)

# Spalte 1 Labels
fig.update_xaxes(title_text="<b>Winkel \\alpha (deg)</b>", row=2, col=1)
fig.update_yaxes(title_text="<b>Koeffizient</b>", row=2, col=1)
fig.update_xaxes(title_text="<b>Norm. Sinken Vz / v_h</b>", row=3, col=1)
fig.update_yaxes(title_text="<b>Norm. Vi / v_h</b>", row=3, col=1)

# Spalte 2 Labels (Physikalisch)
fig.update_xaxes(title_text="<b>Axiale Geschw. Vz (m/s)</b>", row=3, col=2)
fig.update_yaxes(title_text="<b>Schub (N)</b>", row=1, col=2)
fig.update_yaxes(title_text="<b>Induzierte Geschw. (m/s)</b>", row=2, col=2)
fig.update_yaxes(title_text="<b>Drehmoment (Nm)</b>", row=3, col=2)

# Spalte 3 Labels (Normiert)
fig.update_xaxes(title_text="<b>Normierte Geschw. Vz / v_h</b>", row=3, col=3)
fig.update_yaxes(title_text="<b>Schubkoeffizient C_T</b>", row=1, col=3)
fig.update_yaxes(title_text="<b>Normierte Induzierte Geschw. Vi / v_h</b>", row=2, col=3)
fig.update_yaxes(title_text="<b>Drehmomentkoeffizient C_Q</b>", row=3, col=3)

print("Öffne interaktives Dashboard im Browser...")
fig.show()
