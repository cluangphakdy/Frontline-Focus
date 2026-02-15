using UnityEngine;

public enum AlienType { Normal, Fast, Rare }

public class Alien : MonoBehaviour
{
    public AlienType type;
    [HideInInspector] public bool hasBeenPhotographed = false;

    public int GetPoints()
    {
        if (type == AlienType.Fast) return 5000;
        if (type == AlienType.Rare) return 10000;
        return 1000; // Normal
    }

    private void OnCollisionEnter(Collision collision)
    {
        // If it touches the floor, it's gone! 
        // Make sure your floor is named "Floor" or tagged "Floor"
        if (collision.gameObject.name.Contains("Floor"))
        {
            Destroy(gameObject);
        }
    }
}