namespace GameTimeNext.Core.Framework.UI
{
    /// <summary>
    /// A value moved by a damped spring. A new target keeps the current velocity, so an interrupted movement bends
    /// smoothly into the new one instead of restarting. Solved exactly per step, so it stays stable with any frame time.
    /// </summary>
    internal sealed class SpringValue
    {
        private readonly double _min;
        private readonly double _max;
        private readonly double _precision;

        // Seconds for one undamped oscillation (the perceived speed) and damping ratio (1 = no overshoot)
        private double _response = 0.4;
        private double _damping = 1;

        public SpringValue(double min, double max, double precision)
        {
            _min = min;
            _max = max;
            _precision = precision;
        }

        public double Value { get; private set; }

        public double Target { get; private set; }

        public double Velocity { get; private set; }

        public double Delay { get; private set; }

        /// <summary>True when it has arrived and nothing is pending.</summary>
        public bool IsSettled => Delay <= 0 && Value == Target && Velocity == 0;

        public void MoveTo(double target, double response, double damping, double delay = 0)
        {
            Target = target;
            Delay = delay;
            _response = response;
            _damping = damping;
        }

        /// <summary>Jumps to a value without a movement.</summary>
        public void Reset(double value)
        {
            Value = value;
            Target = value;
            Velocity = 0;
            Delay = 0;
        }

        public void Step(double seconds)
        {
            if (Delay > 0)
            {
                Delay -= seconds;
                return;
            }

            double omega = 2 * Math.PI / _response;
            double offset = Value - Target;
            double newOffset;
            double newVelocity;

            if (_damping < 1)
            {
                double decayRate = _damping * omega;
                double dampedOmega = omega * Math.Sqrt(1 - _damping * _damping);
                double b = (Velocity + decayRate * offset) / dampedOmega;
                double decay = Math.Exp(-decayRate * seconds);
                double cos = Math.Cos(dampedOmega * seconds);
                double sin = Math.Sin(dampedOmega * seconds);

                newOffset = decay * (offset * cos + b * sin);
                newVelocity = decay * ((b * dampedOmega - decayRate * offset) * cos - (offset * dampedOmega + decayRate * b) * sin);
            }
            else
            {
                double b = Velocity + omega * offset;
                double decay = Math.Exp(-omega * seconds);

                newOffset = (offset + b * seconds) * decay;
                newVelocity = (b - omega * (offset + b * seconds)) * decay;
            }

            Value = Target + newOffset;
            Velocity = newVelocity;

            // A hard stop, e.g. a cover can't move behind the case
            if (Value < _min || Value > _max)
            {
                Value = Math.Clamp(Value, _min, _max);
                Velocity = 0;
            }

            if (Math.Abs(Value - Target) < _precision && Math.Abs(Velocity) < _precision * 10)
            {
                Value = Target;
                Velocity = 0;
            }
        }
    }
}
