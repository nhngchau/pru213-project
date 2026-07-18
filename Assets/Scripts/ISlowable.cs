public interface ISlowable
{
    void AddSpeedModifier(object source, float multiplier);

    void RemoveSpeedModifier(object source);
}
