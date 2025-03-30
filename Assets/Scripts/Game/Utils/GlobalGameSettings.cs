namespace Game.Utils
{
    public static class GlobalGameSettings
    {
        public static bool IsStepByStepMovement = false;
        public static int PlayerMaxMoveDistance = 4;
        public static int EnemyMaxMoveDistance = 4;
        public static float StepTime = 1f;
        public static float StepDelayTime = 0f;
        public static float TotalStepTime => StepTime + StepDelayTime;
    }
}