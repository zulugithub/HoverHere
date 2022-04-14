// ############################################################################
// Free RC helicopter Simulator
// 20.01.2020 
// Copyright (c) zulu
// Source: https://joinerda.github.io/Solving-ODEs-in-Unity/
//
// Unity c# code
// ############################################################################

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// Helicopter_Integrator is an abstract class for integrating a system of ODEs
/// </summary>
abstract public class Helicopter_Integrator  {

	int nEquations;
	double [] store;
	double [] k1;
	double [] k2;
	double [] k3;
	double [] k4;
	double [] ym1;
	double [] ym2;
	double [] ym3;
	double [] P;
	double [] dm1;
	double [] dm2;
	double [] dm3;
	double [] dp1;
	int abmSteps =0;
	double abmRms2;

	public Helicopter_Integrator() {
		Init (1);
	}

	/// <summary>
	/// Allocate memory for all storage arrays and set number of equations
	/// </summary>
	/// <param name="nEquations">N equations.</param>
	public void Init (int nEquations) {
		// set up temp arrays
		this.nEquations = nEquations;
		store = new double[nEquations];
		k1 = new double[nEquations];
		k2 = new double[nEquations];
		k3 = new double[nEquations];
		k4 = new double[nEquations];
		ym1 = new double[nEquations];
		ym2 = new double[nEquations];
		ym3 = new double[nEquations];
		P = new double[nEquations];
		dm1 = new double[nEquations];
		dm2 = new double[nEquations];
		dm3 = new double[nEquations];
		dp1 = new double[nEquations];
		abmSteps = 0;
	}

	/// <summary>
	/// Abstract void, override this method to set the ODEs to be
	/// integrated.
	/// </summary>
	/// <param name="x">The values being integrated.</param>
	/// <param name="xdot">The derivatives being calculated.</param>
	abstract public void ODE(int integrator_function_call_number, ref double[] x, double[] u, double[] xdot, double t , double dt);
	abstract public void ODE_DISC(int integrator_function_call_number, ref double[] x, double[] u, double[] xdot, double t , double dt);

	/// <summary>
	/// Step forward using Euler's method
	/// </summary>
	/// <param name="x">The values being integrated.</param>
	/// <param name="h">The time step.</param>
	public bool EulerStep(double [] x, double[] u, ref double t, double h) {
        int integrator_function_call_number = 0;
        ODE(integrator_function_call_number++, ref x, u, k1, t, h);
		for(int i=0;i<nEquations;i++) {
			x[i] += k1[i]*h;
		}
		return false;
	}

	/// <summary>
	/// Step forward using 4th order Runge Kutta method
	/// </summary>
	/// <param name="x">The values being integrated.</param>
	/// <param name="h">The time step.</param>
	public bool RK4Step(ref double [] x, double[] u, ref double t, double h, int state_begin, int state_end) {
        int integrator_function_call_number = 0;
        ODE (integrator_function_call_number++, ref x, u, k1, t , h / 2.00000000000f);
		if (check_results(x)) return true;
		for (int i = state_begin; i < state_end; i++) {
			store [i] = x [i] + k1 [i] * h / 2.0;
		}
        ODE(integrator_function_call_number++, ref store, u, k2, t + 0.5f * h, h / 2);
		if (check_results(store)) return true;
		for (int i = state_begin; i < state_end; i++){
			store [i] = x [i] + k2 [i] * h / 2.0;
		}
        ODE(integrator_function_call_number++, ref store, u, k3, t + 0.5f * h, h / 2);
		if (check_results(store)) return true;
		for (int i = state_begin; i < state_end; i++) {
			store [i] = x [i] + k3 [i] * h;
		}
        ODE(integrator_function_call_number++, ref store, u, k4, t + h, h);
		if (check_results(store)) return true;
		for (int i = state_begin; i < state_end; i++) {
			x [i] = x [i] + (k1[i] +2.0*k2[i]+ 2.0*k3 [i]+k4[i]) * h/6.0;
		}
		if (check_results(x)) return true;
		t = t + h;
		return false;
	}

	public bool check_results(double[] x)
	{
		/*
				x_R = x_states[0]; // [m] right handed system, position x in reference frame
				y_R = x_states[1]; // [m] right handed system, position y in reference frame
				z_R = x_states[2]; // [m] right handed system, position z in reference frame
				q0 = x_states[3]; // [-] w quaternion orientation real
				q1 = x_states[4]; // [-] x quaternion orientation imag i
				q2 = x_states[5]; // [-] y quaternion orientation imag j
				q3 = x_states[6]; // [-] z quaternion orientation imag k
				dxdt_R = x_states[7]; // [m/sec] right handed system, velocity x in reference frame
				dydt_R = x_states[8]; // [m/sec] right handed system, velocity y in reference frame
				dzdt_R = x_states[9]; // [m/sec] right handed system, velocity z in reference frame
				wx_LH = x_states[10]; // [rad/sec] local rotational velocity vector x   around longitudial x-axis 
				wy_LH = x_states[11]; // [rad/sec] local rotational velocity vector y   around vertical y-axis  
				wz_LH = x_states[12]; // [rad/sec] local rotational velocity vector z   around lateral z-axis 

				flapping_a_s_mr_LR = x_states[13]; // [rad] mainrotor pitch flapping angle a_s in local frame (longitudial direction)
				flapping_b_s_mr_LR = x_states[14]; // [rad] mainrotor roll flapping angle b_s in local frame (lateral direction) 
				flapping_a_s_tr_LR = x_states[15]; // [rad] tailrotor pitch flapping angle a_s in local frame (longitudial direction)
				flapping_b_s_tr_LR = x_states[16]; // [rad] tailrotor roll flapping angle b_s in local frame (lateral direction) 

				omega_mo = x_states[17]; // [rad/sec] brushless motor rotational speed
				Omega_mo = x_states[18]; // [rad] brushless motor rotational angle
				omega_mr = x_states[19]; // [rad/sec] mainrotor rotational speed
				Omega_mr = x_states[20]; // [rad] mainrotor rotational angle

				DELTA_omega_mo___int = x_states[21]; // [rad] // PI Controller's integral part

				DELTA_x_roll__int = x_states[22];   // [rad] flybarless error value integral
				DELTA_y_yaw__int = x_states[23];    // [rad] gyro error value integral
				DELTA_z_pitch__int = x_states[24];  // [rad] flybarless error value integral

				servo_col_mr_damped = x_states[25];  // [-1...1] damping of mainrotor collective movement - Collective
				servo_lat_mr_damped = x_states[26];  // [-1...1] damping of mainrotor lateral movement - Roll
				servo_lon_mr_damped = x_states[27];  // [-1...1] damping of mainrotor longitudial movement - Pitch
				servo_col_tr_damped = x_states[28];  // [-1...1] damping of tailrotor collective movement - Yaw
				servo_lat_tr_damped = x_states[29];  // [-1...1] damping of tailrotor lateral movement 
				servo_lon_tr_damped = x_states[30];  // [-1...1] damping of tailrotor longitudial movement

				q0_DO = x_states[31]; // [-] w quaternion orientation real - rotor disc
				q1_DO = x_states[32]; // [-] x quaternion orientation imag i - rotor disc
				q2_DO = x_states[33]; // [-] y quaternion orientation imag j - rotor disc
				q3_DO = x_states[34]; // [-] z quaternion orientation imag k - rotor disc
				wx_DO_LD = x_states[35]; // [rad/sec] local rotational velocity vector x   around longitudial x-axis - rotor disc
				wy_DO_LD = omega_mr; //     x_states[36]; // [rad/sec] local rotational velocity vector y   around vertical y-axis - rotor disc    ////// TODO reomve it. not a extra state --> use omega_mr
				wz_DO_LD = x_states[36]; // [rad/sec] local rotational velocity vector z   around lateral z-axis - rotor disc*/

		double[] mylimits = {10000,10000,10000, 1.01, 1.01, 1.01, 1.01, 100,100,100, 1000,1000,1000,  1,1,1,1,  100000,10000000,10000,1000000,  1000, 100,100,100,  2,2,2,2,2,2,   1.01, 1.01, 1.01, 1.01, 100000, 100000, 100000};

		for (int i = 0; i < nEquations; i++)
		{
			if(Double.IsNaN(x[i]) || Double.IsInfinity(x[i]))
			{
#if UNITY_EDITOR
				UnityEngine.Debug.Log("ERROR NAN i:" + i.ToString() + "  x:" + x[i].ToString());
#endif
				return true;
			}


			if (Math.Abs(x[i]) > mylimits[i])
			{
#if UNITY_EDITOR
				UnityEngine.Debug.Log("ERROR RANGE i:" + i.ToString() + "  x:" + x[i].ToString());
#endif
				return true;
			}

		}
		return false;
	}



	/// <summary>
	/// Step forward using 4th order Runge Kutta method
	/// </summary>
	/// <param name="x">The values being integrated.</param>
	/// <param name="h">The time step.</param>
	public bool RK4Step_DISC(ref double[] x, double[] u, ref double t, double h, int state_begin, int state_end)
	{
		int integrator_function_call_number = 0;
		ODE_DISC(integrator_function_call_number++, ref x, u, k1, t, h / 2.00000000000f);
		for (int i = state_begin; i < state_end; i++)
		{
			store[i] = x[i] + k1[i] * h / 2.0;
		}
		ODE_DISC(integrator_function_call_number++, ref store, u, k2, t + 0.5f * h, h / 2);
		for (int i = state_begin; i < state_end; i++)
		{
			store[i] = x[i] + k2[i] * h / 2.0;
		}
		ODE_DISC(integrator_function_call_number++, ref store, u, k3, t + 0.5f * h, h / 2);
		for (int i = state_begin; i < state_end; i++)
		{
			store[i] = x[i] + k3[i] * h;
		}
		ODE_DISC(integrator_function_call_number++, ref store, u, k4, t + h , h);
		for (int i = state_begin; i < state_end; i++)
		{
			x[i] = x[i] + (k1[i] + 2.0 * k2[i] + 2.0 * k3[i] + k4[i]) * h / 6.0;
		}
		t = t + h;
		return false;
	}



	/**
	 * Calculates a single step using Adams Bashforth Moulton,
	 * 
	 * @param x Array of values being integrated.
	 * @param t Time at which step begins
	 * @param h Duration of step
	 * @return Error prediction at end of step
	 */
	public double abmStep(double [] x, double[] u, double t, double h) {
		abmRms2 = 0.0;
        int integrator_function_call_number=0;
		if(abmSteps==0) {
			for(int i=0;i<x.Length;i++) {
				ym3[i] = x[i];
				ym2[i] = x[i];
			}
			ODE(integrator_function_call_number++, ref dm3, u, ym3,t,h);
			RK4Step(ref ym2, u, ref t, h, 0, nEquations);
			ODE(integrator_function_call_number++, ref dm2, u, ym2, t,h);
			for(int i=0;i<x.Length;i++) {
				x[i] = ym2[i];
			}
			abmSteps+=1;
			return 1.0;
		} else if(abmSteps==1) {
			for(int i=0;i<x.Length;i++) {
				ym1[i] = ym2[i];
			}
			RK4Step(ref ym1, u, ref t,  h, 0, nEquations);
			ODE(integrator_function_call_number++, ref dm1, u, ym1, t, h);
			for(int i=0;i<x.Length;i++) {
				x[i] = ym1[i];
			}
			abmSteps +=1;
			return 1.0;
		} else {
			ODE(integrator_function_call_number++, ref k1, u, x, t, h);
			for(int i=0;i<x.Length;i++) {
				P[i] = x[i] + (h/24.0)*
					(55.0*k1[i]-59.0*dm1[i]+37.0*dm2[i]-9.0*dm3[i]);
			}
			ODE(integrator_function_call_number++, ref dp1, u, P, t+h, h);
			abmRms2 = 0.0;
			for(int i=0;i<x.Length;i++) {
				store[i] = x[i];
				x[i] += (h/24.0)*(9*dp1[i]+19.0*k1[i]-5.0*dm1[i]+dm2[i]);
				dm3[i] = dm2[i];
				dm2[i] = dm1[i];
				dm1[i] = k1[i];
				ym3[i] = ym2[i];
				ym2[i] = ym1[i];
				ym1[i] = store[i];
				abmRms2 += (x[i]-P[i])*(x[i]-P[i])/(x[i]+P[i])/(x[i]+P[i]);
			}
			abmRms2 /= x.Length;
			if(abmSteps<5) abmSteps += 1;
			return t+h;
		}
	}

	public double abmError() {
		return abmRms2;
	}

}