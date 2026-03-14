using System;
using System.Collections.Generic;
using System.Linq;

namespace Cartheur.Animals.Robot
{
    /// <summary>
    /// High-level controller for executing walking cycles with basic runtime safety checks.
    /// </summary>
    public class WalkController
    {
        public WalkController(MotorFunctions motorControl = null)
        {
            MotorControl = motorControl ?? new MotorFunctions();
            TrajectoryPlayer = new MotionTrajectoryPlayer(MotorControl);
            MaxLoad = MotorFunctions.PresentLoadAlarm;
            MaxTemperature = 70;
            MinVoltage = 90;
        }

        public MotorFunctions MotorControl { get; private set; }
        public MotionTrajectoryPlayer TrajectoryPlayer { get; private set; }
        public int MaxLoad { get; set; }
        public int MaxTemperature { get; set; }
        public int MinVoltage { get; set; }

        /// <summary>
        /// Executes a walking sequence based on the current robot pose.
        /// </summary>
        /// <returns>True when all steps execute successfully.</returns>
        public bool ExecuteWalkCycle(int cycles = 1, int stepDurationMilliseconds = 450, int interpolationSteps = 8)
        {
            EnsureMotorMap();
            if (!EnsurePortsReady())
                return false;

            string[] involvedMotors = Limbic.All;
            EnsureTorqueOn(involvedMotors);

            var neutralPose = MotorControl.GetPresentPositions(involvedMotors);
            var steps = BipedGaitFactory.BuildTwoStepWalkCycle(neutralPose, cycles, stepDurationMilliseconds);

            foreach (var step in steps)
            {
                if (!ValidateTelemetry(step.Targets.Keys))
                {
                    Logging.WriteLog("Walk cycle aborted because telemetry exceeded safe thresholds.", Logging.LogType.Warning, Logging.LogCaller.MotorControl);
                    return false;
                }

                var safeStep = new MotionTrajectoryStep(step.Targets, step.DurationMilliseconds, interpolationSteps);
                TrajectoryPlayer.ExecuteStep(safeStep);
            }

            return true;
        }

        void EnsureMotorMap()
        {
            if (Motor.MotorContext == null || Motor.MotorContext.Count == 0)
                MotorFunctions.CollateMotorArray();
        }

        bool EnsurePortsReady()
        {
            if (MotorFunctions.DynamixelMotorsInitialized)
                return true;

            string result = MotorControl.InitializeDynamixelMotors();
            if (result.StartsWith("Failed", StringComparison.OrdinalIgnoreCase))
            {
                Logging.WriteLog(result, Logging.LogType.Error, Logging.LogCaller.MotorControl);
                return false;
            }
            return true;
        }

        void EnsureTorqueOn(IEnumerable<string> motors)
        {
            string[] motorArray = motors.ToArray();
            if (!MotorControl.IsTorqueOn(motorArray))
                MotorControl.SetTorqueOn(motorArray);
        }

        bool ValidateTelemetry(IEnumerable<string> motors)
        {
            foreach (string motor in motors)
            {
                int load = MotorControl.GetPresentLoad(motor);
                int temperature = MotorControl.GetPresentTemperture(motor);
                int voltage = MotorControl.GetPresentVoltage(motor);

                if (Math.Abs(load) > MaxLoad)
                {
                    Logging.WriteLog("Unsafe load on " + motor + ": " + load, Logging.LogType.Warning, Logging.LogCaller.MotorControl);
                    return false;
                }
                if (temperature > MaxTemperature)
                {
                    Logging.WriteLog("Unsafe temperature on " + motor + ": " + temperature, Logging.LogType.Warning, Logging.LogCaller.MotorControl);
                    return false;
                }
                if (voltage < MinVoltage)
                {
                    Logging.WriteLog("Unsafe voltage on " + motor + ": " + voltage, Logging.LogType.Warning, Logging.LogCaller.MotorControl);
                    return false;
                }
            }

            return true;
        }
    }
}
