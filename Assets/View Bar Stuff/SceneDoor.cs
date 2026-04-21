using UnityEngine;
using System.Collections;

public class SceneDoor : MonoBehaviour
{
    public string targetScene = "Scene1";
    public string spawnDoorTag = "DoorA";
    private bool isOnCooldown = false;

    // Static lock — both CurlyMovement and ZoeyAI check this
    public static bool movementLocked = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isOnCooldown) return;
        CurlyMovement curly = other.GetComponent<CurlyMovement>();
        if (curly != null)
        {
            isOnCooldown = true;
            movementLocked = true;
            curly.CancelMovement();
            if (RumbleManager.instance != null) RumbleManager.instance.Rumble(0.3f, 0.2f, 0.2f);
            SaveManager.QueueAutoSave();
            SceneTransition.instance.GoToScene(targetScene, spawnDoorTag);
        }
    }

    public void StartCooldown()
    {
        StartCoroutine(Cooldown());
    }

    IEnumerator Cooldown()
    {
        isOnCooldown = true;
        yield return new WaitForSeconds(2f);
        isOnCooldown = false;
    }
}