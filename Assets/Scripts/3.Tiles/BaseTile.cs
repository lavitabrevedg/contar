using UnityEngine;

public abstract class BaseTile : MonoBehaviour
{
    public abstract void Init(SerializedTile data);
    public virtual void OnPlayerEnter() { }
}
