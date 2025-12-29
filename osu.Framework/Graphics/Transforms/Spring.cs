// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osuTK;

namespace osu.Framework.Graphics.Transforms
{
    public readonly record struct SpringParameters(
        float NaturalFrequency = 1,
        float Damping = 1,
        float Response = 1
    );

    /// <summary>
    /// Simulates a value following a target value over time using spring physics.
    /// See TestSceneSpring for a visualization of the spring parameters.
    /// </summary>
    public abstract class Spring<T>
        where T : struct
    {
        /// <summary>
        /// The current value of the spring.
        /// </summary>
        public T Current;

        /// <summary>
        /// The current velocity of the spring.
        /// </summary>
        public T Velocity;

        /// <summary>
        /// The target value of the previous frame.
        /// </summary>
        public T PreviousTarget;

        private SpringParameters parameters;

        public SpringParameters Parameters
        {
            get => parameters;
            set
            {
                parameters = value;

                k1 = Damping / (MathF.PI * NaturalFrequency);
                k2 = 1 / ((2 * MathF.PI * NaturalFrequency) * (2 * MathF.PI * NaturalFrequency));
                k3 = Response * Damping / (2 * MathF.PI * NaturalFrequency);
            }
        }

        /// <summary>
        /// Controls the overall movement speed of the spring and the frequency (in hertz) that the spring will tend to vibrate at.
        /// </summary>
        public float NaturalFrequency
        {
            get => Parameters.NaturalFrequency;
            set => Parameters = Parameters with { NaturalFrequency = value };
        }

        /// <summary>
        /// Rate at which the spring looses energy over time.
        /// If the value is 0, the spring will vibrate indefinitely.
        /// If the value is between 0 and 1, the vibration will settle over time.
        /// If the value is greater than or equal to 1 the spring will not vibrate, and will approach the target value at decreasing speeds as damping is increased.
        /// </summary>
        public float Damping
        {
            get => Parameters.Damping;
            set => Parameters = Parameters with { Damping = value };
        }

        /// <summary>
        /// Controls the initial response to target value changes.
        /// If the value is 0, the system will take time to begin moving towards the target value.
        /// If the value is positive, the spring will react immediately to value changes.
        /// If the value is negative, the spring will anticipate value changes by moving in the opposite direction at first.
        /// If the value is greater than 1, the spring will overshoot the target value before it settles down.
        /// </summary>
        public float Response
        {
            get => Parameters.Response;
            set => Parameters = Parameters with { Response = value };
        }

        private float k1, k2, k3;

        protected Spring(T initialValue = default, float naturalFrequency = 1, float damping = 1, float response = 0)
        {
            Current = initialValue;
            PreviousTarget = initialValue;

            Parameters = new SpringParameters
            {
                NaturalFrequency = naturalFrequency,
                Damping = damping,
                Response = response,
            };
        }

        protected abstract T GetTargetVelocity(T target, T previousTarget, float dt);

        public T Update(double elapsed, T target, T? targetVelocity = null)
        {
            float dt = (float)(elapsed / 1000);

            if (targetVelocity == null)
            {
                targetVelocity = GetTargetVelocity(target, PreviousTarget, dt);
                PreviousTarget = target;
            }

            return ComputeNextValue(dt, target, targetVelocity.Value);
        }

        protected abstract T ComputeNextValue(float dt, T target, T targetVelocity);

        protected void ComputeSingleValue(float dt, ref float current, ref float velocity, float target, float targetVelocity)
        {
            float k2Stable = MathF.Max(MathF.Max(k2, dt * dt / 2 + dt * k1 / 2), dt * k1);

            current += dt * velocity;
            velocity += (dt * (target + k3 * targetVelocity - current - k1 * velocity)) / k2Stable;
        }
    }

    public class FloatSpring : Spring<float>
    {
        protected override float GetTargetVelocity(float target, float previousTarget, float dt) => (target - previousTarget) / dt;

        protected override float ComputeNextValue(float dt, float target, float targetVelocity)
        {
            ComputeSingleValue(dt, ref Current, ref Velocity, target, targetVelocity);

            return Current;
        }
    }

    public class Vector2Spring : Spring<Vector2>
    {
        protected override Vector2 GetTargetVelocity(Vector2 target, Vector2 previousTarget, float dt) => (target - previousTarget) / dt;

        protected override Vector2 ComputeNextValue(float dt, Vector2 target, Vector2 targetVelocity)
        {
            ComputeSingleValue(dt, ref Current.X, ref Velocity.X, target.X, targetVelocity.X);
            ComputeSingleValue(dt, ref Current.Y, ref Velocity.Y, target.Y, targetVelocity.Y);

            return Current;
        }
    }
}
