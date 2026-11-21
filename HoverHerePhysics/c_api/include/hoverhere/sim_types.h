#ifndef HOVERHERE_SIM_TYPES_H
#define HOVERHERE_SIM_TYPES_H

/* Plain data exchanged between the simulation, the C API and the Unity wrapper (HoverHereNative.cs).
 *
 * Conventions
 *   - All vectors and quaternions are RIGHT handed (x forward, y up, z right in the body frame; world frame y up). The Unity wrapper
 *     converts to Unity's left handed system (z flipped) at its boundary.
 *   - SI units (m, s, rad, N, Nm, W). Rotor and motor speeds in rad/s, quaternions as (w, x, y, z), matrices row-major.
 *   - Every struct starts with `struct_size` (the caller's sizeof) so the library stays compatible with older callers.
 *   - Layout: only double, int32_t and uint64_t, every member naturally aligned, no implicit padding (checked by the C# layout test
 *     with hh_layout_value()). */

#include <stdint.h>

#ifdef __cplusplus
extern "C" {
#endif

#define HH_MAX_CONTACTS 56  /* capacity: 6 lists of 8 collision points and the two rotor hubs (ground effect) use 50 */
#define HH_BLADE_STATIONS 10
#define HH_BEMT_RADIAL 4
#define HH_BEMT_AZIMUTH 10
#define HH_BEMT_ELEMENTS 40
#define HH_GEAR_GROUPS 5   /* left, right, steering center, steering left, steering right */
#define HH_SERVOS 4        /* three swashplate servos and the tail rotor servo */

typedef enum hh_rotor_model { HH_ROTOR_CLASSIC = 0, HH_ROTOR_BEMT = 1 } hh_rotor_model;

typedef enum hh_contact_type {
    HH_CONTACT_USUAL = 0,
    HH_CONTACT_GEAR_LEFT = 1,
    HH_CONTACT_GEAR_RIGHT = 2,
    HH_CONTACT_STEERING_CENTER = 3,
    HH_CONTACT_STEERING_LEFT = 4,
    HH_CONTACT_STEERING_RIGHT = 5,
    HH_CONTACT_MAINROTOR_HUB = 6, /* distance only, no force (ground effect) */
    HH_CONTACT_TAILROTOR_HUB = 7
} hh_contact_type;

enum {
    HH_FLAG_FREEWHEELING = 1 << 0,
    HH_FLAG_CRASHED = 1 << 1,           /* a contact force exceeded collision_force_max (latched until reset) */
    HH_FLAG_GOVERNOR_SATURATED = 1 << 2,
    HH_FLAG_MOTOR_ENABLED = 1 << 3,
    HH_FLAG_ANY_CONTACT = 1 << 4,
    HH_FLAG_MAINROTOR_BEMT = 1 << 5,
    HH_FLAG_MECHANICS_MAINROTOR = 1 << 6, /* detailed linkage geometry in use (otherwise the ideal CCPM mixer) */
    HH_FLAG_MECHANICS_TAILROTOR = 1 << 7,
    HH_FLAG_DIVERGED = 1 << 8             /* a state became non finite: the simulation holds its last valid state until reset */
};

enum {
    HH_BEMT_NO_VORTEX_RING = 1 << 0,      /* switch off parts of the BEMT model (default 0: everything on) */
    HH_BEMT_NO_GROUND_EFFECT = 1 << 1,
    HH_BEMT_NO_TURBULENCE = 1 << 2,
    HH_BEMT_NO_DYNAMIC_INFLOW = 1 << 3,
    HH_BEMT_QUASI_STATIC = 1 << 4         /* no lags: steady state physics for sweeps */
};

typedef struct hh_sim_options {
    uint32_t struct_size;
    int32_t rotor_model;     /* hh_rotor_model */
    double dt;               /* fixed physics step [s], default 0.001 */
    double max_frame_dt;     /* frames longer than this are cut [s], default 0.25 */
    double time_scale;       /* simulated seconds per real second, default 1 */
    double control_delay_after_reset;  /* [s] the sticks fade in between half of it and all of it after a reset, default 0 (off) */
    uint64_t seed;           /* random numbers of the turbulence */
    int32_t max_substeps;    /* steps per advance() at most, default 1000 */
    uint32_t bemt_flags;     /* HH_BEMT_* */
} hh_sim_options;

typedef struct hh_reset_params {
    uint32_t struct_size;
    int32_t rotor_spooled_up;     /* 1: motor, rotor and governor start at the setpoint; 0: everything stands still */
    double position_O[3];         /* centre of mass in the world frame [m] */
    double orientation[4];        /* body -> world quaternion (w, x, y, z); all zero: identity */
    double velocity_O[3];         /* [m/s] */
} hh_reset_params;

typedef struct hh_sim_inputs {
    uint32_t struct_size;
    int32_t flight_bank;          /* 0..2 */
    double collective;            /* sticks, [-1, 1]; collective: + more pitch, more thrust */
    double yaw;                   /* the flybarless controller and the tail gyro command rotation RATES of the body: yaw + turns about the vertical body y-axis */
    double pitch;                 /* (nose to the left, seen from above), pitch + about the body z-axis (nose UP), roll + about the body x-axis (roll right). */
    double roll;                  /* Unity: pitch = -(forward stick). */
    double propeller;
    double booster;
    int32_t motor_enabled;        /* 0: throttle hold */
    int32_t wheels_raised;        /* 1: landing gear retracted (wheel brakes fully on) */
    double wheel_brake;           /* target [0, 1] */
    double gear_transition[HH_GEAR_GROUPS];  /* [0, 1] raising of the gear groups, drives the collision points of retractable gear */
    int32_t use_wind_override;    /* 1: wind_O replaces the wind of the scenery parameters */
    int32_t reserved;
    double wind_O[3];             /* [m/s] */
} hh_sim_inputs;

typedef struct hh_contact {
    int32_t type;                 /* hh_contact_type */
    int32_t in_contact;           /* 1: the point touches the ground */
    double position_O[3];         /* [m] */
    double normal_O[3];           /* normal of the ground triangle (unit) */
    double distance;              /* signed distance to the ground along the normal [m]; negative = penetration; 1000 = no ground below */
    double penetration;           /* max(0, -distance), including the bumps [m] */
    double velocity_O[3];         /* [m/s] */
    double normal_force_O[3];     /* spring + damper force [N] */
    double friction_force_O[3];   /* sliding / static friction [N] */
    double normal_force;          /* |normal force| [N] */
    double tangential_force;      /* |friction force| [N] */
} hh_contact;

typedef struct hh_rotor_output {
    double thrust;                /* [N] along the shaft */
    double torque;                /* [Nm] aerodynamic torque on the rotor about its shaft, signed like the rotation (negative when driven) */
    double induced_velocity;      /* [m/s] mean wake speed, positive = downwash */
    double force_O[3];            /* [N] on the fuselage */
    double torque_L[3];           /* [Nm] on the fuselage about the centre of mass */
    double flapping_a_s;          /* [rad] (classic model) */
    double flapping_b_s;
    double ground_effect_factor;
    double vortex_ring_strength;  /* [0, 1] */
    double turbulence_strength;   /* [0, 1] */
} hh_rotor_output;

typedef struct hh_audio {
    double mainrotor_speed;       /* [rad/s] */
    double tailrotor_speed;
    double motor_speed;
    double propeller_speed;
    double mainrotor_noise;       /* [0.15, 1] blade drag measure (Unity: sound_volume_mainrotor) */
    double mainrotor_stall;       /* [0, 1] share of blade elements with large angle of attack */
    double tailrotor_pitch_volume;/* [0.5, 1] */
    double vortex_ring;           /* [0, 1] */
    double turbulence;            /* [0, 1] */
    double motor_load;            /* [0, 1] motor current relative to the maximal current of the battery */
    double motor_current;         /* [A] */
} hh_audio;

typedef struct hh_mainrotor_mechanics {
    double servo_arm_B[3][3];     /* ball joints on the servo arms [m] (rotor frame, y = shaft) */
    double rod_C[3][3];           /* ball joints on the stationary swashplate */
    double swashplate_K[3];       /* swashplate centre */
    double swashplate_A_OLK[9];   /* stationary swashplate frame -> rotor frame (row-major) */
    double swashplate_A_OLJ[9];   /* rotating swashplate frame -> rotor frame */
    double swashplate_D[2][3];    /* ball joints on the rotating swashplate */
    double blade_E[2][3];         /* ball joints on the blade pitch arms */
    double blade_axis[2][3];      /* feathering axes of the two blades */
    double driver_F[2][3];        /* rotation driver arms */
    double driver_G[2][3];
    double driver_H[2][3];
    double blade_beta[2];         /* pitch of the two physical blades [rad] */
    int32_t valid;                /* 1: filled (detailed geometry in use) */
    int32_t reserved;
} hh_mainrotor_mechanics;

typedef struct hh_tailrotor_mechanics {
    double servo_arm_B[3];
    double rod_C[3];
    double lever_E[3];
    double sleeve_K[3];
    double hinge_G[2][3];
    double pitch_link_H[2][3];
    double pitch_arm_I[2][3];
    double hinge_axis[2][3];
    double blade_axis[2][3];
    double lever_angle;           /* [rad] */
    double stroke;                /* [m] */
    double blade_beta;            /* kinematic pitch angle, positive decreases the thrust [rad] */
    int32_t valid;
    int32_t reserved;
} hh_tailrotor_mechanics;

typedef struct hh_sim_outputs {
    uint32_t struct_size;
    uint32_t flags;               /* HH_FLAG_* */
    double time;                  /* simulated time [s] */
    uint64_t step_count;

    /* rigid body */
    double position_O[3];
    double orientation[4];        /* body -> world (w, x, y, z) */
    double velocity_O[3];
    double velocity_L[3];
    double omega_L[3];
    double accel_L[3];            /* specific force in the body frame (accelerometer) [m/s^2] */

    /* drivetrain */
    double motor_speed;           /* [rad/s] */
    double motor_angle;           /* [rad] */
    double mainrotor_speed;
    double mainrotor_angle;       /* shaft angle [rad]; the tail rotor and the propeller angles follow from the gear ratios */
    double tailrotor_speed;
    double tailrotor_angle;
    double propeller_speed;
    double propeller_angle;
    double governor_integral;
    double governor_target;       /* motor speed setpoint after the soft start [rad/s] */
    double motor_voltage_applied; /* [V] */
    double motor_power_electric;  /* [W] */
    double motor_power_mechanical;

    /* rotors */
    hh_rotor_output mainrotor;
    hh_rotor_output tailrotor;
    hh_rotor_output propeller;
    double booster_thrust;        /* [N] */
    double rotor_disc_orientation[4];  /* BEMT rotor disc -> world (w, x, y, z); identity for the classic model */
    double rotor_disc_rate_x;     /* [rad/s] */
    double rotor_disc_rate_z;
    hh_audio audio;

    /* controls and kinematics */
    double servo_command[HH_SERVOS];   /* [rad] flight controller output */
    double servo_actual[HH_SERVOS];    /* [rad] after the servo lag */
    double swashplate_roll;       /* [rad] */
    double swashplate_pitch;
    double swashplate_collective; /* [m] */
    double swashplate_normal[3];
    double blade_pitch[HH_BLADE_STATIONS];  /* [rad] at the azimuth stations psi_c = c 36 deg */
    double tailrotor_pitch;       /* [rad] effective collective pitch of the tail rotor */
    double commands[7];           /* normalized controller outputs: collective_mr, lateral_mr, longitudinal_mr, collective_tr, lateral_tr, longitudinal_tr, propeller */
    hh_mainrotor_mechanics mainrotor_mechanics;
    hh_tailrotor_mechanics tailrotor_mechanics;

    /* ground contact */
    int32_t contact_count;
    int32_t reserved;
    hh_contact contacts[HH_MAX_CONTACTS];
    double gear_force_y[HH_GEAR_GROUPS];         /* [N] force along the body y-axis per gear group, for the animation of the deflection */
    double wheel_rolling_distance[HH_GEAR_GROUPS];  /* [m] */
    double wheel_rolling_velocity[HH_GEAR_GROUPS];  /* [m/s] */
    double wheel_steering[3];     /* center, left, right: heading of the wheel [0, 1) per revolution */
    double wheel_brake_strength;  /* [0, 1] */
    double hub_distance_to_ground[2];  /* main rotor, tail rotor [m] */
} hh_sim_outputs;

typedef struct hh_bemt_telemetry {
    uint32_t struct_size;
    int32_t valid;                /* 1: the main rotor is a BEMT rotor and has been evaluated */
    double time;
    double radius[HH_BEMT_ELEMENTS];     /* index = r * HH_BEMT_AZIMUTH + c */
    double psi[HH_BEMT_ELEMENTS];
    double beta[HH_BEMT_ELEMENTS];
    double Vc[HH_BEMT_ELEMENTS];
    double V2[HH_BEMT_ELEMENTS];
    double Vi_raw[HH_BEMT_ELEMENTS];
    double Vi_target[HH_BEMT_ELEMENTS];
    double Vi[HH_BEMT_ELEMENTS];
    double alpha[HH_BEMT_ELEMENTS];
    double cl[HH_BEMT_ELEMENTS];
    double cd[HH_BEMT_ELEMENTS];
    double dL[HH_BEMT_ELEMENTS];
    double dD[HH_BEMT_ELEMENTS];
    double dT[HH_BEMT_ELEMENTS];
    double dQ[HH_BEMT_ELEMENTS];
    double vrs_weight[HH_BEMT_ELEMENTS];
    double residual[HH_BEMT_ELEMENTS];
    double thrust;
    double shaft_torque;
    double Vi_mean;
    double Vz_scaled;
    double Vx_scaled;
    double use_vrs;
    double strength_vrs;
    double descent_blend_weight;
    double v_johnson_target;
    double v_johnson_lagged;
    double hover_induced_velocity;
    double ground_effect_factor;
    double turbulence_strength;
    double max_residual;
    int32_t nonconverged_elements;
    int32_t reserved;
} hh_bemt_telemetry;

#ifdef __cplusplus
}
#endif

#endif /* HOVERHERE_SIM_TYPES_H */
