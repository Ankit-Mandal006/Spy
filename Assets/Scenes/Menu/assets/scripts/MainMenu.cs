using UnityEngine;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private Canvas menuCanvas;
    [SerializeField] private Canvas loadingCanvas;
    [SerializeField] private DominoLoadingEffect dominoEffect;

    public void StartButton()
    {
        if (menuCanvas != null)
            menuCanvas.gameObject.SetActive(false);

        if (loadingCanvas != null)
            loadingCanvas.gameObject.SetActive(true);

        if (dominoEffect != null)
            dominoEffect.StartLoading();
    }

    public void QuitButton()
    {
        Application.Quit();
    }
}
