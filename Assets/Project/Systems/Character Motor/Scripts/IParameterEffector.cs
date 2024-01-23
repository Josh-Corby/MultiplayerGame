namespace Project
{
    public interface IParameterEffector
    {
        float Effect(float currentValue);

        EParameter GetEffectedParameter();

        bool Tick(float deltaTime);
    }
}