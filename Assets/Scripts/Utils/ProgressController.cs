using UnityEngine;

namespace Progression
{
    public class ProgressController
    {
        public static void SaveProgress(int levelNum)
        {
            PlayerPrefs.SetInt("Progress", levelNum);
            PlayerPrefs.Save();
        }

        public static int LoadProgress()
        {
            return PlayerPrefs.GetInt("Progress", 0);
        }
        
        public static void ClearProgress()
        {
            PlayerPrefs.DeleteKey("Progress");
            PlayerPrefs.Save();
        }
    }
}