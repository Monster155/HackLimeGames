using UnityEngine;

namespace Progression
{
    public class ProgressController
    {
        private static string ProgressKey = "Progress";
        
        public static void SaveProgress(int levelNum)
        {
            PlayerPrefs.SetInt(ProgressKey, levelNum);
            PlayerPrefs.Save();
        }

        public static int LoadProgress()
        {
            return PlayerPrefs.GetInt(ProgressKey, 0);
        }
        
        public static void ClearProgress()
        {
            PlayerPrefs.DeleteKey(ProgressKey);
            PlayerPrefs.Save();
        }
    }
}