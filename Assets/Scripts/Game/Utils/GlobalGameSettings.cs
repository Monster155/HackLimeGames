namespace Game.Utils
{
    public static class GlobalGameSettings
    {
        public static bool IsStepByStepMovement = false;
        public static float StepTime = 1f;
        public static float StepDelayTime = 0.2f;
        public static float TotalStepTime => StepTime + StepDelayTime;
    }
}