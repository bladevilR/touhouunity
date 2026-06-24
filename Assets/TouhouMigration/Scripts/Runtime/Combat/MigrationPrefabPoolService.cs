using System;
using System.Collections.Generic;
using UnityEngine;

namespace TouhouMigration.Runtime.Combat
{
    [DisallowMultipleComponent]
    public sealed class MigrationPrefabPoolService : MonoBehaviour
    {
        [SerializeField] private int maxInactivePerPrefab = 128;

        private readonly Dictionary<GameObject, Stack<GameObject>> inactiveByPrefab = new();
        private readonly Dictionary<GameObject, GameObject> prefabByInstance = new();
        private readonly HashSet<GameObject> inactiveInstances = new();
        private readonly HashSet<GameObject> activeInstances = new();

        public int TotalCreatedCount { get; private set; }
        public int TotalReusedCount { get; private set; }
        public int TotalGetCount { get; private set; }
        public int TotalReleasedCount { get; private set; }
        public int PrefabKeyCount => inactiveByPrefab.Count;
        public int ActiveInstanceCount => activeInstances.Count;
        public int InactiveInstanceCount => inactiveInstances.Count;

        public void ConfigurePool(int maxInactivePerPrefab)
        {
            this.maxInactivePerPrefab = Mathf.Max(0, maxInactivePerPrefab);
        }

        public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null)
            {
                throw new ArgumentNullException(nameof(prefab));
            }

            Stack<GameObject> inactiveStack = GetOrCreateStack(prefab);
            GameObject instance = PopReusableInstance(inactiveStack);
            if (instance == null)
            {
                instance = Instantiate(prefab, position, rotation, transform);
                instance.name = prefab.name;
                prefabByInstance[instance] = prefab;
                TotalCreatedCount++;
            }
            else
            {
                TotalReusedCount++;
            }

            instance.transform.SetParent(transform, true);
            instance.transform.SetPositionAndRotation(position, rotation);
            inactiveInstances.Remove(instance);
            activeInstances.Add(instance);
            instance.SetActive(true);
            TotalGetCount++;
            return instance;
        }

        public T Get<T>(T prefab, Vector3 position, Quaternion rotation) where T : Component
        {
            if (prefab == null)
            {
                throw new ArgumentNullException(nameof(prefab));
            }

            return Get(prefab.gameObject, position, rotation).GetComponent<T>();
        }

        public bool Release(GameObject instance)
        {
            if (instance == null || !prefabByInstance.TryGetValue(instance, out GameObject prefab))
            {
                return false;
            }

            if (inactiveInstances.Contains(instance))
            {
                return false;
            }

            activeInstances.Remove(instance);
            TotalReleasedCount++;
            instance.SetActive(false);
            instance.transform.SetParent(transform, true);

            if (maxInactivePerPrefab == 0)
            {
                prefabByInstance.Remove(instance);
                Destroy(instance);
                return true;
            }

            Stack<GameObject> inactiveStack = GetOrCreateStack(prefab);
            if (inactiveStack.Count >= maxInactivePerPrefab)
            {
                prefabByInstance.Remove(instance);
                Destroy(instance);
                return true;
            }

            inactiveStack.Push(instance);
            inactiveInstances.Add(instance);
            return true;
        }

        public bool Release(Component component)
        {
            return component != null && Release(component.gameObject);
        }

        public bool IsPooledInstance(GameObject instance)
        {
            return instance != null && prefabByInstance.ContainsKey(instance);
        }

        public GameObject GetPrefabKey(GameObject instance)
        {
            if (instance == null)
            {
                return null;
            }

            return prefabByInstance.TryGetValue(instance, out GameObject prefab) ? prefab : null;
        }

        private Stack<GameObject> GetOrCreateStack(GameObject prefab)
        {
            if (!inactiveByPrefab.TryGetValue(prefab, out Stack<GameObject> inactiveStack))
            {
                inactiveStack = new Stack<GameObject>();
                inactiveByPrefab[prefab] = inactiveStack;
            }

            return inactiveStack;
        }

        private static GameObject PopReusableInstance(Stack<GameObject> inactiveStack)
        {
            while (inactiveStack.Count > 0)
            {
                GameObject candidate = inactiveStack.Pop();
                if (candidate != null)
                {
                    return candidate;
                }
            }

            return null;
        }
    }
}
