using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ObjectPool : MonoBehaviour
{
    public static ObjectPool Instance;

    [System.Serializable]
    public class PoolEntry
    {
        public string key;
        public GameObject prefab;
        public int initialSize = 20;
    }

    public List<PoolEntry> pools = new List<PoolEntry>();

    private Dictionary<string, Queue<GameObject>> poolDict = new Dictionary<string, Queue<GameObject>>();
    private Dictionary<string, GameObject> prefabDict = new Dictionary<string, GameObject>();

    // Inspector'da tanımlanmamış prefab'lar için otomatik havuz
    // (ölüm partikülü, bomber patlaması gibi kısa ömürlü efektler).
    private Dictionary<GameObject, Queue<GameObject>> prefabPools = new Dictionary<GameObject, Queue<GameObject>>();
    private Dictionary<GameObject, GameObject> instanceToPrefab = new Dictionary<GameObject, GameObject>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        foreach (PoolEntry entry in pools)
        {
            if (entry == null || entry.prefab == null || string.IsNullOrEmpty(entry.key))
            {
                Debug.LogWarning("ObjectPool: Eksik ayarlanmış pool girdisi atlandı.");
                continue;
            }

            Queue<GameObject> queue = new Queue<GameObject>();

            for (int i = 0; i < entry.initialSize; i++)
            {
                GameObject obj = Instantiate(entry.prefab, transform);
                obj.SetActive(false);
                queue.Enqueue(obj);
            }

            poolDict[entry.key] = queue;
            prefabDict[entry.key] = entry.prefab;
        }
    }

    // ==================================
    // KEY İLE HAVUZ
    // ==================================

    public GameObject Spawn(string key, Vector3 position, Quaternion rotation)
    {
        if (!poolDict.ContainsKey(key))
        {
            Debug.LogWarning("ObjectPool: '" + key + "' anahtarı bulunamadı.");
            return null;
        }

        Queue<GameObject> queue = poolDict[key];

        GameObject obj = queue.Count > 0 ? queue.Dequeue() : Instantiate(prefabDict[key], transform);

        obj.transform.position = position;
        obj.transform.rotation = rotation;
        obj.SetActive(true);

        return obj;
    }

    public void Despawn(string key, GameObject obj)
    {
        if (obj == null)
            return;

        if (!poolDict.ContainsKey(key))
        {
            // Bilinmeyen key: kapatıp sahipsiz bırakmak yerine yok et.
            Debug.LogWarning("ObjectPool: '" + key + "' anahtarı bulunamadı, obje yok ediliyor.");
            Destroy(obj);
            return;
        }

        // Zaten havuzdaysa tekrar ekleme (aynı obje iki kez Spawn edilmesin).
        if (!obj.activeSelf)
            return;

        obj.SetActive(false);
        poolDict[key].Enqueue(obj);
    }

    // ==================================
    // PREFAB İLE GEÇİCİ EFEKT
    // ==================================

    // Havuz varsa oradan alır, yoksa Instantiate + Destroy yapar.
    // lifetime dolunca obje otomatik geri döner.
    public static GameObject SpawnTemporary(GameObject prefab, Vector3 position, Quaternion rotation, float lifetime)
    {
        if (prefab == null)
            return null;

        if (Instance == null)
        {
            GameObject fallback = Instantiate(prefab, position, rotation);
            Destroy(fallback, lifetime);
            return fallback;
        }

        return Instance.SpawnFromPrefab(prefab, position, rotation, lifetime);
    }

    public bool IsPrefabPooled(GameObject obj)
    {
        return obj != null && instanceToPrefab.ContainsKey(obj);
    }

    GameObject SpawnFromPrefab(GameObject prefab, Vector3 position, Quaternion rotation, float lifetime)
    {
        if (!prefabPools.TryGetValue(prefab, out Queue<GameObject> queue))
        {
            queue = new Queue<GameObject>();
            prefabPools[prefab] = queue;
        }

        GameObject obj;

        if (queue.Count > 0)
        {
            obj = queue.Dequeue();
            obj.transform.SetPositionAndRotation(position, rotation);
            obj.SetActive(true);
        }
        else
        {
            obj = Instantiate(prefab, position, rotation, transform);
            instanceToPrefab[obj] = prefab;
        }

        StartCoroutine(ReturnPrefabInstanceAfter(obj, lifetime));

        return obj;
    }

    IEnumerator ReturnPrefabInstanceAfter(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (obj == null)
        {
            yield break;
        }

        if (!instanceToPrefab.TryGetValue(obj, out GameObject prefab))
        {
            Destroy(obj);
            yield break;
        }

        obj.SetActive(false);
        prefabPools[prefab].Enqueue(obj);
    }
}
