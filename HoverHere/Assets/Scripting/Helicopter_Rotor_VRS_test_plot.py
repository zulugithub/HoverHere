import pandas as pd
import plotly.graph_objects as go
from plotly.subplots import make_subplots

# 1. Daten einlesen (Sicherstellen, dass der Pfad zur CSV korrekt ist)
csv_path = "D:/Helikopter2/HoverHere/Git/HoverHere/Build/vrs_plot_data.csv"
csv_path = "D:/Helikopter2/HoverHere/Git/HoverHere/vrs_plot_data.csv"

df = pd.read_csv(csv_path)

# Erstelle ein interaktives Plotly-Diagramm
fig = go.Figure()

# Farbpaletten für die unterschiedlichen Geschwindigkeiten definieren
colors = {0.0: '#1f77b4', 0.3: '#ff7f0e', 0.6: '#2ca02c'}

# Schleife über jede getestete Vorwärtsgeschwindigkeit (Vx)
for vx in df['Vx_scaled'].unique():
    sub_df = df[df['Vx_scaled'] == vx]
    color = colors.get(vx, '#7f7f7f')
    
    # Kurve 1: Deine glatte Baseline (Gap Filler für BEMT)
    fig.add_trace(go.Scatter(
        x=sub_df['Vz_scaled'], 
        y=sub_df['Vi_baseline_scaled'],
        mode='lines',
        name=f'Baseline (Vx={vx})',
        line=dict(color=color, width=2.5)
    ))
    
    # Kurve 2: Das finale Signal inklusive VRS-Korrektur (zum optischen Vergleich)
    fig.add_trace(go.Scatter(
        x=sub_df['Vz_scaled'], 
        y=sub_df['Vi_final_scaled'],
        mode='lines',
        name=f'Final mit VRS (Vx={vx})',
        line=dict(color=color, width=2, dash='dash')
    ))

# Zum physikalischen Abgleich: Die theoretisch rein mathematische Momentum Theory (bricht bei Sinken ein)
df_ideal = df[df['Vx_scaled'] == 0.0]
fig.add_trace(go.Scatter(
    x=df_ideal['Vz_scaled'],
    y=df_ideal['Vi_ideal_momentum'],
    mode='lines',
    name='Ideale Impulstheorie (Ref)',
    line=dict(color='black', width=1, dash='dot')
))

# Achsenbeschriftungen und Layout verfeinern (Klassischer Aero-Plot-Style)
fig.update_layout(
    title="Rotor Inflow Charakteristik: Normiertes Sinken (Vz) vs. Induzierte Geschwindigkeit (Vi)",
    xaxis_title="Normierte Sinkgeschwindigkeit (Vz / v_h) <br><sub>[ < 0 ist Sinken | > 0 ist Steigen ]</sub>",
    yaxis_title="Normierte induzierte Geschwindigkeit (Vi / v_h)",
    template="plotly_white",
    hovermode="x unified",
    legend=dict(yanchor="top", y=0.99, xanchor="left", x=0.01)
)

# Diagramm im Browser anzeigen
fig.show()


















# Daten laden
csv_path = "D:/Helikopter2/HoverHere/Git/HoverHere/Build/bemt_rotor_test.csv"
csv_path = "D:/Helikopter2/HoverHere/Git/HoverHere/bemt_rotor_test.csv"

df = pd.read_csv(csv_path)

# 1. Plot für Induzierte Geschwindigkeit (Vi)
fig_vi = go.Figure()
for coll in df['Collective_Deg'].unique():
    sub = df[df['Collective_Deg'] == coll]
    fig_vi.add_trace(go.Scatter(x=sub['Vz_Absolute'], y=sub['Vi_Mean'], mode='lines', name=f'Pitch: {coll}°'))

fig_vi.update_layout(
    title="BEMT Gesamtrotor: Vertikalgeschwindigkeit vs. Mittlere induzierte Geschwindigkeit (Vi)",
    xaxis_title="Absolute Vertikalgeschwindigkeit Vz (m/s) [ < 0 Sinken | > 0 Steigen ]",
    yaxis_title="Mittlere induzierte Geschwindigkeit Vi (m/s)",
    template="plotly_white",
    hovermode="x unified"
)
fig_vi.show()

# 2. Plot für Gesamtschub (Thrust)
fig_thrust = go.Figure()
for coll in df['Collective_Deg'].unique():
    sub = df[df['Collective_Deg'] == coll]
    fig_thrust.add_trace(go.Scatter(x=sub['Vz_Absolute'], y=sub['Thrust_Z_O'], mode='lines', name=f'Pitch: {coll}°'))

fig_thrust.update_layout(
    title="BEMT Gesamtrotor: Vertikalgeschwindigkeit vs. Gesamtschub",
    xaxis_title="Absolute Vertikalgeschwindigkeit Vz (m/s)",
    yaxis_title="Rotorschub (N)",
    template="plotly_white",
    hovermode="x unified"
)
fig_thrust.show()
