using System.Collections.Generic;
using UnityEngine;

public class WindowManager : MonoBehaviour
{

    public GameObject prefab;

    private Queue<GameObject> spawned = new();
    public void SpawnWindow()
    {
        GameObject newWindow = Instantiate(prefab);
        spawned.Enqueue(newWindow);
    }

    public void DestroyWindow()
    {
        GameObject destroyable = spawned.Dequeue();
        Destroy(destroyable);
    }
}
