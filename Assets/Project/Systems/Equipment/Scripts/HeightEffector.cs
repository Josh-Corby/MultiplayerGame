namespace Project
{
    public class HeightEffector : IParameterEffector
    {
        private float _multiplier;
        private float _duration;

        public HeightEffector(float multiplier, float duration)
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
            return EParameter.Height;
        }

        public bool Tick(float deltaTime)
        {
            _duration -= deltaTime;
            return _duration <= 0f;
        }
    }
}

