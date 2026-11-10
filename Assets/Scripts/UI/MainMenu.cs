using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Tooltip("Nombre exacto de la escena del juego")]
    public string gameScene = "Test_Player";

    private void Start()
    {
        // Por si se llega desde la pausa o el Game Over
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Jugar()
    {
        SceneManager.LoadScene(gameScene);
    }

    public void Salir()
    {
        Debug.Log("Salir del juego");

#if UNITY_EDITOR
        // En el editor: detener el Play (Unity Scripting API: EditorApplication.isPlaying)
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // En el juego compilado: cerrar el juego
        Application.Quit();
#endif
    }
}