using System;

namespace Game.Utils
{
    public static class EventSystem
    {
        public static Action OnPlayerStartsMoving;
        public static Action OnPlayerFinishedMoving;
        
        public static Action OnPlayerStartsAttacking;
        public static Action OnPlayerFinishedAttacking;
    }
}