using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class BombConroller : MonoBehaviour
{
    public Animator animator;
    public GameObject bombPrefab;
    public float maxRandomTilt = 90f;
    public GameObject explosionObject;

    public float dissolveSpeed = 2f;
    private float currentDissolveTime = -1f;
    public bool dissolve = false;

    public Material dissolveMat;
    public Shader dissolveShader;

    public int minX;
    public int maxX;
    public int minZ;
    public int maxZ;

    public int bombsToSpawn = 1;
    // Start is called before the first frame update
    void Start()
    {
        AnimationClip ac = animator.runtimeAnimatorController.animationClips[0];
        AnimationEvent ae = new AnimationEvent();
        ae.functionName = "Kaboom";
        ae.time = ac.length;

        ac.AddEvent(ae);

        animator.enabled = false;

        //this.transform.rotation = Quaternion.Euler(Random.Range(-maxRandomTilt, maxRandomTilt), 0, Random.Range(-maxRandomTilt, maxRandomTilt));

        dissolveMat = explosionObject.GetComponent<MeshRenderer>().material;
        dissolveShader = dissolveMat.shader;
    }

    // Update is called once per frame
    void Update()
    {
        if (dissolve && !animator.enabled)
        {
            currentDissolveTime += Time.deltaTime * dissolveSpeed;
            dissolveMat.SetFloat("_EffectTime", currentDissolveTime);
            
            if (currentDissolveTime > 1f)
            {
                for (int i = 0; i < bombsToSpawn; i++)
                {
                    SpawnNewBomb();
                }
                Destroy(gameObject);                
            }
        }
        
        if (explosionObject.transform.localScale.x > 0)
        {
            Collider[] hitColliders = Physics.OverlapSphere(this.transform.position, 2.5f);
            
            foreach(Collider c in hitColliders)
            {
                if (c.CompareTag("Interactable"))
                {
                    c.gameObject.GetComponent<Animator>().enabled = true;
                }
                if (c.CompareTag("Player"))
                {
                    //die
                    Destroy(c.gameObject);
                }
            }
            
        }
    }

    //void OnDrawGizmos()
    //{
    //    Gizmos.color = Color.red;
    //    Gizmos.DrawSphere(this.transform.position, 2.5f);
    //}

    public void Kaboom()
    {
        //explode
        dissolve = true;
        animator.enabled = false;
        dissolveMat.SetFloat("_EffectTime", -1f);
        currentDissolveTime = -1f;
    }

    private void SpawnNewBomb()
    {
        Debug.Log("Kaboom");
        //spawn new bomb within range
        int spawnX = Random.Range(minX, maxX);
        int spawnZ = Random.Range(minZ, maxZ);
        int spawnY = 20;

        GameObject prefab = (GameObject)Resources.Load("Prefabs/Bomb", typeof(GameObject));
        GameObject newBomb = PrefabUtility.InstantiatePrefab(prefab) as GameObject;

        newBomb.transform.SetPositionAndRotation(new Vector3(spawnX, spawnY, spawnZ), Quaternion.Euler(0, 0, 0));

        //PrefabUtility.InstantiatePrefab(bombPrefab.gameObject);
        //Instantiate(bombPrefab, new Vector3(spawnX, spawnY, spawnZ), Quaternion.Euler(0, 0, 0));
    }
}
