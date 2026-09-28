using UnityEngine;
using TMPro;
using MagicSchool.Contracts;

namespace MagicSchool.Items
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class Item : MonoBehaviour, IInspectableItem, IDraggable, IEquipment
    {
        [SerializeField] private ItemDataSO _data;
        [SerializeField] private TextMeshPro _nameLabel;    // FLAGGING: name on the item, this is for prototype
        private ICustomModifier _modifier;

        // ================================= getter =================================
        public ItemDataSO Data => _data;
        public ICustomModifier Modifier => _modifier;
        // === IInspectableItem ===
        public string DisplayName => _data != null ? _data.Name : name;
        public string Description => _data != null ? _data.Description : string.Empty;
        public bool IsAlive => this != null;

        // ================================= life cycle =================================
        // show a name of the item's prefab even in editor mode.
#if UNITY_EDITOR
        private void OnValidate() => ShowName();
#endif

        void Awake()
        {
            Init(_data);
        }

        // init item
        private void Init(ItemDataSO data)
        {
            if (data == null) return;

            _data = data;
            _modifier = ItemLibrary.Resolve(data.ItemId);
            ShowName();
        }

        // ================================= public =================================
        // this is factory, to spawn item into the scene
        public static Item Spawn(ItemDataSO data, Vector3 position)
        {
            if (data == null)
            {
                Debug.LogError("[ItemSpawner] asked to spawn an item with no data.");
                return null;
            }

            if (data.Prefab == null)
            {
                Debug.LogError($"[ItemSpawner] {data.Name} has no prefab, so it cannot be spawned.");
                return null;
            }

            GameObject spawned = Object.Instantiate(data.Prefab, position, Quaternion.identity);

            Item item = spawned.GetComponent<Item>();
            if (item == null)
            {
                Debug.LogError($"[ItemSpawner] {data.Name}'s prefab has no Item on it.");
                return null;
            }

            item.Init(data);

            // BaseItem(Clone) tells nobody anything, and every item shares that prefab
            spawned.name = data.Name;

            return item;
        }

        // ================================= private =================================
        private void ShowName()
        {
            if (_nameLabel == null) return;

            _nameLabel.text = _data != null ? _data.Name : string.Empty;
        }
    }
}
