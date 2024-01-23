namespace Project
{
    public class JumpHeightEffector : IParameterEffector
    {
        private float _multiplier;
        private float _duration;

        public JumpHeightEffector(float multiplier, float duration)
        {
            _multiplier = multiplier;
            _duration = duration;
        }

        public float Effect(float currentValue)
        {
            return currentValue * _multiplier;
        }

        public EParameter GetEffectedParameter()
        {
            return EParameter.JumpHeight;
        }

        public bool Tick(float deltaTime)
        {
            _duration -= deltaTime;
            return _duration <= 0f;
        }
    }
}

