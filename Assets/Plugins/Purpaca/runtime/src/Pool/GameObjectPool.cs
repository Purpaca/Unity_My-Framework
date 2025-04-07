using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Purpaca.Pool
{
    [Serializable]
    public class GameObjectPool
    {
        [SerializeField]
        private GameObject m_prefab;

        private List<GameObject> m_pooledGameObjects = new List<GameObject>();

        #region 构造器
        public GameObjectPool(GameObject prefab)
        {
            if(prefab.scene.IsValid())
            {
                Debug.LogWarning("Using a scene GameObject to initialize GameObjectPool might cause exception when the scene that the GameObject attached to unloaded.");
            }
            m_prefab = prefab;
        }

        public GameObjectPool(GameObject prefab, int initialCount = 10) : this(prefab) 
        {

        }
        #endregion


    }
}