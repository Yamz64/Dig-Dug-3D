using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Halter : MonoBehaviour
{
    public GameObject visuals;
    public GameObject player;
    public bool start;
    private TerrainGeneration generator;
    IEnumerator HaltGame()
    {
        yield return new WaitForEndOfFrame();
        yield return new WaitUntil(() => generator.GetFinished());
        yield return new WaitUntil(() => start);
        generator.ForceRenderChunks();
        yield return new WaitUntil(() => GameObject.FindGameObjectWithTag("Player") == false);
        Instantiate(player, Vector3.zero, Quaternion.identity);
        generator.GenerateCaves(true);
        visuals.SetActive(true);
    }

    // Start is called before the first frame update
    void Start()
    {
        GameObject[] generators = GameObject.FindGameObjectsWithTag("TerrainGenerator");
        for (int i = 0; i < generators.Length; i++)
        {
            if (generators[i].scene == gameObject.scene)
                generator = generators[i].GetComponent<TerrainGeneration>();
        }
        StartCoroutine(HaltGame());
    }

    private void Update()
    {
        if (gameObject.scene == SceneManager.GetActiveScene())
            generator.ForceLoadChunks();
    }
}
