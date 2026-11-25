# HoverHerePhysics

Platform independent C++20 physics engine of the **HoverHere** RC helicopter simulator. It replaces the physics that used to live in
the Unity C# code (`Helicopter_ODE.cs` and friends) with a deterministic, allocation free library that is used from Unity through a
C API (`hoverhere.dll`), from a headless command line runner and from unit tests.

What is simulated:

- 6-DOF rigid body fuselage, integrated with RK4
- drivetrain: brushless motor, speed governor, freewheel with torsional spring/damper, rotor shaft
- main rotor, tail rotor and pusher propeller, either as **classic** analytical rotor or as **4 x 10 blade element momentum (BEMT)**
  rotor with Pitt-Peters dynamic inflow, Johnson vortex ring state, wake turbulence and ground effect
- swashplate / servo / pitch link mechanics, servo dynamics
- fuselage and wing aerodynamics including the interaction with the rotor wakes
- ground collision of landing gear, skids and fuselage on a triangle mesh
- a flight controller behind an interface (flybarless controller with tail gyro today)

Contents: [Architecture](#architecture-and-simulation-loop) | [Coordinates](#coordinate-systems-and-sign-conventions) |
[Modules](#modules) | [Parameters](#parameter-pipeline) | [Build and test](#build-and-test) | [Headless runner](#headless-maneuver-runner)

---

## Architecture and simulation loop

### Time: `FixedStepClock`

The physics always runs with a fixed step (`dt = 1 ms` by default). The host (Unity frame, viewer, test) hands in the variable frame
time; `numerics::FixedStepClock` turns it into a whole number of fixed steps. Time is accumulated in integer nanoseconds, so the step
count does not drift and is reproducible. Guards against the spiral of death: `max_frame_dt` (default 0.25 s, longer frames are cut) and
`max_substeps` (default 1000). The rest of the frame time (`alpha`, in [0, 1)) is used to interpolate the *displayed* state between the
last two steps. `time_scale` gives slow motion or fast forward.

### One physics step

`simulation::HelicopterSim::step_fixed()` does the following, in this order:

1. **`evaluate()` once at the start state.** This is RK4 stage k1 *and* the "nominal" evaluation of everything that has memory
   (rotor, drivetrain, controller, collision).
2. **Three more RK4 stages (k2..k4).** Each one is a *pure function of the stage state*. The controller output and the servo
   positions are held constant over the step (zero order hold). Nothing in these stages writes outside the RK4 state vector, so the
   stages can be evaluated in any order.
3. **`post_step(...)` exactly once**, using the nominal evaluation. Everything that is not a derivative lives here:
   - quaternion normalization, angle reset of the open freewheel, governor soft start
   - **PT1 servo lags** (`ServoBank`, exact discretization)
   - **Pitt-Peters dynamic inflow** of every BEMT element (implicit Euler, unconditionally stable)
   - **Johnson vortex ring state lags** (PT1 on the strength and on the Johnson inflow)
   - wake turbulence random numbers, stiction anchors of the gear, wheel rolling, crash latch
   - **flight controller integrators** (`IFlightController::post_step`)

The consequence: a step is a pure function of `(state, memory, inputs)`. `step_fixed()` is deterministic, the same parameters, seed and
inputs give bit-identical output on one machine, and an RK4 stage can never corrupt a filter state.

The Pitt-Peters time constant of an element is `tau = k * 8 / (3 pi) * R / U`, with `k = wake_inertia_fumble_factor` and the
through flow of the disc `U = max(sqrt(v_horizontal^2 + (Vi + Vc)^2), 0.5 m/s)`. The blade speed `Omega * r` is deliberately *not* part of `U`: the apparent
mass is the air column that is pushed through the disc, and with `Omega * r` the time constant would shrink to about 10 ms and let a
reversal of the air column through undamped (visible as a shake at the reversal point of a tick-tock).

### Threads

| Use | Calls |
|---|---|
| Synchronous (tests, headless) | `step_fixed(inputs)`, or `advance(frame_dt, inputs)` for a variable frame time, then `fill_outputs()` |
| Physics thread (Unity) | `start_thread()`, then per frame `post_inputs()` and `read_outputs()` |

With the physics thread, a background thread runs the clock on the wall time. Sticks and outputs cross through **lock free triple
buffers**, so neither side waits for the other. Structural changes (`configure`, `reset`, ground mesh, live parameter update) take a
mutex that the physics thread holds for one batch of steps (a few ms at most).

---

## Coordinate systems and sign conventions

**The whole library is right handed.** The only left handed code is the Unity wrapper, which converts at its boundary.

| Frame | Meaning | Axes |
|---|---|---|
| `O` | inertial world frame | y up, gravity along -y |
| `L` | body frame at the center of mass | x forward, y up, z right |
| `R` | rotor frame (`oriLH` relative to `L`, S123 Euler angles) | y = shaft axis |
| `N` | non rotating frame of the virtual blades (BEMT) | follows the shaft, does not spin |
| `D` | tilting rotor disc (BEMT) | follows the disc state |

- `A_RL` maps coordinates of `L` to `R` (`r_R = A_RL r_L`), `A_LR = A_RL^T`. Euler sequences: `S123` is `Rz(psi) Ry(theta) Rx(phi)`,
  `B123` is `Rx Ry Rz`. Quaternions are `(w, x, y, z)`, `q` rotates `L` to `R`, `q' = 1/2 q (x) (0, omega_L)`. Matrices are row major.
- Body angular rates: x roll, y yaw, z pitch (as in the original C# code).
- SI units everywhere inside the physics: m, s, rad, N, Nm, W; rotor and motor speeds in rad/s. User units (deg, rpm, mm, ...) only
  exist in the parameter files and are compiled to SI by `params::compile_to_si` / `update_parameters`.

### Unity boundary

Unity is left handed. The conversion is a mirror at the x-y plane, done in `HoverHereCoordinates` in
`HoverHere/Assets/Scripting/Native/HoverHereNative.cs` and nowhere else:

- vectors: `z` is negated
- quaternions: mirrored (`M R M` with `M = diag(1, 1, -1)`)
- angular velocities (pseudo vectors): `(x, y, z)` becomes `(-x, -y, z)`
- ground meshes: `z` negated and the triangle winding flipped

### Rotor direction and sign of the induced velocity

- A positive shaft speed is a right hand rotation about +y of the rotor frame (counter clockwise seen from above); a blade at azimuth
  `psi` points along `(cos psi, 0, -sin psi)`. The blade element equations are written for exactly this sense of rotation.
  `invert_mainrotor_rotation_direction` selects the opposite sense: the drivetrain flips the sign of the motor setpoint, and the BEMT
  rotor evaluates the mirror image (`rotation_sign`) so that the aerodynamic torque still opposes the rotation.
- Induced velocity `Vi` is the speed of the wake along the *negative* rotor axis: positive for a lifting rotor (downwash of a main
  rotor), it has the sign of the thrust. The Unity code returns the opposite sign from its classic model (`v_i = -Vi`).
- Servo angles are right hand angles about the servo shaft. The C# mechanics chain mirrored the arm between step 1 and step 2 (hidden
  by the -15 deg / -0.0065 m gains of `Helicopter_ODE.cs`); here both steps use the right hand angle, the throws are positive.

---

## Modules

Everything lives in the namespace `hoverhere`; headers are in `physics/include/hoverhere/<module>/`, sources in `physics/src/<module>/`.

| Module | What it does |
|---|---|
| `core` | `Real` (double), `Vec3` / `Mat3` / `Quat` on **Boost.QVM**, frame transformations and Euler conversions (`frames.hpp`) |
| `numerics` | `FixedStepClock`, generic `rk4` step, smooth step / smooth damp, Perlin noise |
| `params` | generated parameter structs (user units and SI), descriptor tables, validation against the schema bounds, compilation user units -> SI, parameter documents |
| `dynamics` | `rigid_body` (Newton-Euler 6-DOF, quaternion kinematics, inertia tensor) and `drivetrain` (motor, PI governor with anti windup, freewheel, rotor shaft) |
| `kinematics` | swashplate / servo / pitch link mechanics of the main rotor (steps 1 to 4) and the tail rotor pitch slider, plus the ideal linear CCPM mixer |
| `aero` | `rotor_classic`, `rotor_bemt`, `rotor_common`, `aerodynamics` (fuselage and wings in the rotor wakes) |
| `control` | `IFlightController`, `FblController` (flybarless + tail gyro), `ServoBank` |
| `collision` | `GroundMesh` (triangle soup with a hash grid, no heap allocation per query) and `GroundCollision` (spring-damper, friction with stiction anchors, brake, gear raising, crash latch) |
| `simulation` | `HelicopterSim`: composes all of the above, runs the loop described above, owns the physics thread |
| `io` | `param_loader`: reads the XML parameter files with **pugixml** into the parameter structs |
| `c_api` | C ABI `hoverhere.dll` / `libhoverhere.so` (`sim_api.h`, `sim_types.h`) |

### Rotor models (`aero`)

- **Classic rotor:** analytical thrust and induced velocity (Heffley / Cai et al., iterative), profile / induced / parasite / climb
  power, tip-path-plane flapping dynamics with hub stiffness, vortex ring state, wake turbulence, ground effect. Also used for the
  tail rotor and the propeller. The flapping angles are the only rotor related RK4 state.
- **BEMT rotor (4 radial x 10 azimuth = 40 elements):** a tilting rotor disc (quaternion + two rates, spring and damper against the
  hub) that is an RK4 state itself. Each element is solved with a damped Newton iteration (warm started from the last raw solution).
  On top of the quasi static solution:
  - *Pitt-Peters apparent mass:* PT1 lag of the induced velocity of each element, time constant see above
  - *Johnson vortex ring state:* global Johnson inflow of the descending disc blended in smoothly with the descent, gated by how coherent
    the descent is over the disc; the strength and the inflow follow their instantaneous values with PT1 lags
  - wake turbulence, ground effect (thrust only, not torque), low rpm handling of the blade pitch
  - the elements and their forces are exported as telemetry for debug displays (`hh_bemt_telemetry`)
- Both rotors share `rotor_common` (frames, inflow tables, lag helpers). `aerodynamics` uses the wake velocity published by the rotor
  filter states of the *previous* step, which breaks the algebraic loop between rotor torque, fuselage drag and wake.

### Flight controller interface (`control`)

```cpp
class IFlightController {
    virtual ControllerOutputs evaluate(const ControllerInputs&, Real dt) const = 0;   // pure, allowed in every RK4 stage
    virtual void post_step(const ControllerInputs&, Real dt) = 0;                     // once per step: integrators, filters
    virtual void reset() = 0;
};
```

The inputs are only what a real flight controller sees: IMU (gyro, accelerometer), rotor speed and the radio sticks. The outputs are
what it drives: four servo angles (three swashplate servos and the tail servo), the motor speed setpoint and the normalized commands for
models without servo kinematics. `FblController` is the default (PID per axis, derivative low pass, anti windup, integrator decay at low
rotor speed). Because nothing of the physics state leaks through the interface, a C++ replica of **Rotorflight 2** (with the parameters
of a NEXUS-XR) or a **hardware-in-the-loop** controller plugs in behind the same interface with `HelicopterSim::set_flight_controller()`
(a hardware controller returns the last received frame from `evaluate()` and exchanges data in `post_step()`).

### C API (`c_api`)

Plain C structs, every struct starts with `struct_size` (the caller's `sizeof`) so the library stays compatible with older callers;
only `double`, `int32_t` and `uint64_t`, naturally aligned, no padding (checked by the C# layout test). `HH_ABI_VERSION` is incremented
on every incompatible change. Typical use:

```
hh_sim_create -> hh_sim_load_xml (helicopter, optionally scenery and the two mechanics files)
  -> hh_sim_set_ground_mesh -> hh_sim_reset
  -> per frame: hh_sim_step(frame_dt, inputs, outputs)                         // synchronous
  or:           hh_sim_thread_start, then hh_sim_post_inputs / hh_sim_read_outputs   // physics thread
```

`hh_sim_update_parameters` applies freshly loaded parameter files to the *running* simulation (state, filters, integrators and contact
memory stay). The Unity side is `HoverHereNative.cs` / `HoverHereNative.Structs.g.cs`; the optional native path is in
`Helicopter_NativePhysics.cs`.

---

## Parameter pipeline

The parameters have a single source of truth, the schema:

```
schema/parameters.yaml, mechanics.yaml, units.yaml
        |  schema/tools/generate_params.py generate        (Python >= 3.9 + PyYAML)
        v
generated: params/model_params.{hpp,cpp}, mechanics_params.{hpp,cpp}, param_documents.{hpp,cpp}     (C++ structs, descriptor tables)
           bindings/csharp/HoverHere.Native/ModelParams.g.cs                                           (C# mirror)
        |  io/param_loader (pugixml)
        v
XML parameter files of the helicopters  ->  ModelParams (user units)  ->  compile  ->  ModelParamsSI  ->  HelicopterSim
        ^
        |  hh_sim_load_xml + hh_sim_update_parameters         live update from the Unity GUI, no reset
```

- Each parameter is one leaf in the schema with unit, default, bounds and a documentation text. Parameters that only exist in the
  C++ port carry `cpp_only: true`. The generated files must never be edited by hand; **regenerate after every schema change**.
- The XML files are *overlays on the defaults*. Unknown or misplaced elements and out of range values are warnings
  (`hh_sim_get_message`), not errors. `hoverhere_params_check <dir>` lists them for a directory of shipped files, and
  `schema/tools/fix_param_xml.py` repairs the known cases.
- Regenerate and check:

```bash
py -3 schema/tools/generate_params.py generate      # lint, then write the generated C++ and C# files
py -3 schema/tools/generate_params.py check         # fails if a generated file is out of date
py -3 schema/tools/generate_params.py lint [--seed seed.json] [--strict]   # --seed compares the schema with the C# classes
```

  PyYAML is required. If it is not installed system wide, use a virtual environment outside the repository
  (`py -3 -m venv <dir>` and `<dir>/Scripts/python.exe -m pip install pyyaml`). CMake registers the schema tests only if it finds a
  Python with PyYAML.
- Layout dependent files (`c_api/src/layout_table.inc`, `HoverHereNative.Structs.g.cs`) are generated by `py -3 tools/gen_layout_table.py`
  after every change of `c_api/include/hoverhere/sim_types.h`. The CTest `csharp_interop` checks them against the real DLL.
- `tools/csharp_reference` is a small .NET tool built from the original C# code. It produces the golden files of the XML loader tests and
  the schema seed (`schema-seed`) for the C# sync check.

---

## Build and test

**Requirements:** a C++20 compiler (MSVC 2026 / VS 18, GCC or Clang from MSYS2 UCRT64), CMake >= 3.25, Ninja for GCC/Clang. Boost.QVM
(header only), pugixml and doctest are used from the system or vcpkg if present, otherwise **downloaded at configure time**
(`HOVERHERE_FETCH_DEPENDENCIES`). The Python tools need Python 3 with PyYAML, the C# checks need the .NET SDK, the plots need pandas and plotly.

### CMake presets

| Configure preset | Toolchain | Generator |
|---|---|---|
| `msvc` | Visual Studio 18 2026 | Visual Studio |
| `msvc-vcpkg` | Visual Studio, dependencies from vcpkg | Visual Studio |
| `gcc-msys2`, `clang-msys2` | MSYS2 UCRT64 (GCC / Clang) | Ninja Multi-Config |
| `gcc`, `clang` | compilers from `PATH` (Linux / macOS) | Ninja Multi-Config |

Every preset has `-debug` and `-release` build and test presets, and a **workflow** that configures, builds and tests Debug and Release
in one go. Build trees are in `build/<preset>/`.

```bash
# day to day, one compiler (MSVC)
cmake --build --preset msvc-debug
ctest --preset msvc-debug

# complete check of one toolchain
cmake --workflow --preset msvc
export PATH=/c/msys64/ucrt64/bin:$PATH        # GCC / Clang from MSYS2
cmake --workflow --preset gcc-msys2
cmake --workflow --preset clang-msys2
```

The GCC and Clang presets build with warnings as errors (including `-Wsign-conversion`: use `std::size_t` for array indices, also in
tests). The MSVC and MinGW runtimes are linked statically, so `hoverhere.dll` needs no redistributable.

### Batch files (Windows)

| File | Does |
|---|---|
| `build_msvc_release.bat` | builds MSVC Release, runs the CTests, copies `hoverhere.dll` to `HoverHere/Assets/Plugins/x86_64/` (close Unity or leave Play mode first) |
| `build_msvc_headless.bat` | builds only the target `hoverhere_headless_test` (MSVC Release) |
| `run_maneuver.bat [name]` | runs a maneuver with the Logo 600 SE v3, writes the CSV files to `out/` and plots them (see below) |

### Tests

The CTest suite (`ctest --preset <preset>`):

| Test | Content |
|---|---|
| `unit_tests` | the doctest executable `hoverhere_tests`: frames, RK4, clock, rigid body, drivetrain, kinematics, classic and BEMT rotor, aerodynamics, collision, flight controller, parameter loader, whole simulation (about 185 test cases) |
| `smoke` | `hoverhere_headless_test --smoke`: math, version, the C API lifecycle from a C translation unit |
| `headless_hover` | a hover through the headless runner |
| `csharp_layout_blob`, `csharp_layout`, `csharp_interop` | the C# mirror of the structs against the real DLL (needs the .NET SDK) |
| `schema_*` | schema lint, generator self test, generated files up to date, C# sync (need PyYAML / .NET) |

Run a subset of the unit tests with doctest filters, e.g. `build/msvc/bin/Debug/hoverhere_tests.exe -tc="*Pitt*"`.

---

## Headless maneuver runner

`hoverhere_headless_test` runs the complete simulation without Unity, for regression runs and for looking at the physics:

```bash
build/msvc/bin/Release/hoverhere_headless_test.exe run <maneuver> --heli <parameter_file.xml> [options]
build/msvc/bin/Release/hoverhere_headless_test.exe run --profile <sticks.csv> --heli <parameter_file.xml> [options]
```

Built-in maneuvers: `hover`, `collective_step`, `roll_step`, `pitch_step`, `yaw_step`, `roll_doublet`, `pitch_doublet`, `vrs_descent`,
`takeoff_land`. The hover trim (collective) is found by a bisection unless `--trim` is given.

Important options (the usage text of the executable lists all of them):

| Option | Meaning |
|---|---|
| `--rotor bemt\|classic` | main rotor model (default `bemt`) |
| `--duration S`, `--dt S`, `--seed N` | override duration, physics step, turbulence seed |
| `--rate HZ`, `--grid-rate HZ` | sample rate of the state CSV (100) and of the BEMT grid CSV (20, 0 = off) |
| `--out DIR` | output directory (default `./hoverhere_out`) |
| `--scenery FILE`, `--main-mech FILE`, `--tail-mech FILE`, `--no-mechanics` | additional parameter files |
| `--set PATH=VALUE` | set a parameter after loading (user units), e.g. `--set helicopter/flybarless/K_d=0` |
| `--ground-obj FILE` | ground mesh from a Wavefront OBJ |
| `--no-vrs --no-ground-effect --no-turbulence --no-dynamic-inflow --quasi-static` | switch off parts of the BEMT model |

Output: `<maneuver>_states.csv` (time, position, velocity, attitude, rotor speed, thrust, sticks, vortex ring strength, ...) and
`<maneuver>_bemt_grid.csv` (the 40 elements: `Vc`, `V2`, `Vi_raw`, `Vi_target`, `Vi`, `alpha`, `dT`, ...).

### Plotting

```bash
py -3 tools/headless/plot_example.py out/vrs_descent_states.csv out/vrs_descent_bemt_grid.csv    # needs pandas, plotly
py -3 tools/headless/animation_example.py out/vrs_descent_states.csv out/vrs_descent_bemt_grid.csv   # animation of states and grid
```

The BEMT airfoil plot uses the Re/Ncrit lookup table from `data/airfoil_polars/`. From the
`HoverHerePhysics` directory, rebuild the headless executable and export a fresh CSV before plotting:

```bash
build/msvc/bin/Release/hoverhere_headless_test.exe airfoil --heli ../HoverHere/Assets/Helicopters_Available/Logo600SE_V3/Resources/Logo600SE_V3_parameter_file.xml --out out/airfoil.csv
py -3 tools/headless/plot_airfoil.py out/airfoil.csv
```

To plot a specific built-in profile instead of the profile selected in the helicopter file, omit `--heli` and pass
its polar filename. For example:

```bash
build/msvc/bin/Release/hoverhere_headless_test.exe airfoil --airfoil-polar vr7-il_polar.xml --out out/airfoil.csv
py -3 tools/headless/plot_airfoil.py out/airfoil.csv
```

The exported CSV records the selected profile name, which the plotter includes in the figure title.

For an interactive profile picker, run `build_mscv_headless_and_plot_selected_airfoil_curves.bat`; you can also
pass a profile name directly, for example `build_mscv_headless_and_plot_selected_airfoil_curves.bat VR-7`.

The CSV contains the runtime-interpolated Re=50k, 100k, 200k and 500k curves at Ncrit=9, plus Re=200k at
Ncrit=1, 5 and 9. Existing CSV files with only `cl_mr` / `cd_mr` columns predate the lookup-table export;
regenerate them with the rebuilt executable. The XML files document their AirfoilTools source, resampling,
and the estimated (not directly measured) Ncrit=1 slice. They can be regenerated with
`py -3 tools/fetch_airfoil_polars.py data/airfoil_polars` (requires numpy, pandas and internet access).

Each BEMT blade element computes its Reynolds number from its local relative speed and chord, then interpolates the
polar table at that Re. Ncrit is also evaluated per element; it is an engineering blend of the turbulence and
vortex-ring strengths, not a separately measured flow quantity:

```text
disturbance = clamp01(0.5 * turbulence_strength + vrs_strength)
Ncrit = 9 - 8 * disturbance
```

Thus undisturbed flow uses Ncrit 9, maximum turbulence alone gives Ncrit 5, and a fully developed vortex ring
can drive the value to Ncrit 1. Lower Ncrit represents earlier boundary-layer transition. The lookup interpolates
between the available Ncrit 1, 5 and 9 slices; note that Ncrit 1 is extrapolated from XFOIL's Ncrit 5 and 9
polars, so it is an estimate rather than a direct XFOIL run.

The Unity parameter panel exposes the BEMT airfoil under **Mainrotor > airfoil**. The Logo600SE defaults to
NACA0012. NACA0009sm-il (Selig-modified), NACA23012, NACA8H12 and VR-7 are also available. Their Re/Ncrit polar
slices retain signed angles of attack, so camber lift at zero angle and different positive/negative stall behavior
are not mirrored away. The selected mainrotor parameter chooses the matching XML polar table; a missing or invalid
table prevents BEMT configuration rather than silently reverting to an old analytic polar.

`run_maneuver.bat vrs_descent` does all of it: it runs the maneuver on the Logo 600 SE v3 (path in the batch file) and then calls
`plot_example.py`. (The error message of `run_maneuver.bat` mentions `build_headless.bat`, the file is `build_msvc_headless.bat`.)

---

## Repository layout

```
physics/            the engine (include/hoverhere/<module>, src/<module>)
io/                 XML parameter loader
c_api/              C ABI (hoverhere.dll) and the layout table
schema/             parameter schema and the Python generator / lint tools
bindings/csharp/    generated C# parameter classes and the C# interop checks
tools/headless/     maneuver runner (hoverhere_headless_test) and plot scripts
tools/params_check/ lists misplaced or unknown elements of parameter files
tools/csharp_reference/  the original C# code as a reference tool (golden files, schema seed)
tests/              doctest unit tests and test data (tests/data)
viewer/             placeholder for a GLFW + Dear ImGui viewer (not implemented)
cmake/              warnings, dependencies
```
