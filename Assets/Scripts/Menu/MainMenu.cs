using System.Collections;
using Progression;
using Tymski;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Menu
{
    public class MainMenu : MonoBehaviour
    {
        [SerializeField] private Button _continuesButton;
        [SerializeField] private BaseScreen _loadingScreen;
        [SerializeField] private BaseScreen _settingsScreen;
        [SerializeField] private SceneReference _gameScene;

        private void Start()
        {
            _continuesButton.interactable = ProgressController.LoadProgress() != 0;
            
            _settingsScreen.Hide();
            _loadingScreen.Hide();
        }
        
        public void StartGame(bool isNewGame)
        {
            if (isNewGame)
                ProgressController.ClearProgress();
            OpenScene(_gameScene);
        }

        public void OpenSettings() => _settingsScreen.Show();
        
        public void ExitGame() => Application.Quit();

        private void OpenScene(string sceneName) => StartCoroutine(LoadSceneCoroutine(sceneName));

        private IEnumerator LoadSceneCoroutine(string sceneName)
        {
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            asyncLoad.allowSceneActivation = false;
            _loadingScreen.Show();

            yield return new WaitForSeconds(1.4f);

            while (asyncLoad.progress < 0.9)
                yield return null;

            asyncLoad.allowSceneActivation = true;
            _loadingScreen.Hide();
        }
    }
}