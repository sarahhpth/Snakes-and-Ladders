using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FollowPath : MonoBehaviour
{
    public Transform[] waypoints;
    [SerializeField]
    public float moveSpeed = 2f;
    [HideInInspector]
    public int waypointIndex = 0;
    public bool moveAllowed = false;

    // Additional variables for hop effect
    public float hopHeight = 0.1f;
    public bool isHopping = false;
    private Vector3 originalScale;

    private AudioSource audioSource;
    //public AudioClip footstepSound;

    // Start is called before the first frame update
    private void Start()
    {
        transform.position = waypoints[waypointIndex].transform.position;
        originalScale = transform.localScale;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            Debug.LogError("No AudioSource component found on the GameObject. Please add one.");
        }
    }

    // Update is called once per frame
    private void Update()
    {
        if (moveAllowed)
        {
            GameControl.playerIsMoving = true;
            WalkHop();

        }
        else
        {
            audioSource.Stop();
        }
    }

    private void Walk()
    {
        if (waypointIndex <= waypoints.Length - 1)
        {
            //Vector2 targetPosition = waypoints[waypointIndex].transform.position;
            //if (transform.position != (Vector3)targetPosition)
            //{
            //    // Move towards the target position
            //    transform.position = Vector2.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

            //    // Play the footstep sound if not already playing
            //    if (!audioSource.isPlaying)
            //    {
            //        audioSource.PlayOneShot(footstepSound);
            //    }
            //}

            transform.position = Vector2.MoveTowards(transform.position, waypoints[waypointIndex].transform.position, moveSpeed * Time.deltaTime);
            

            if (transform.position == waypoints[waypointIndex].transform.position)
            {
                waypointIndex += 1;
            }
        }
        //Debug.Log("waypointIndex: " + waypointIndex);
    }

    private void WalkHop()
    {
        if (waypointIndex <= waypoints.Length - 1)
        {
            Vector2 targetPosition = waypoints[waypointIndex].transform.position;
            //Vector2 direction = targetPosition - transform.position;
            //float distance = direction.magnitude;
            float step = moveSpeed * Time.deltaTime;

            // Move towards the target position
            transform.position = Vector2.MoveTowards(transform.position, targetPosition, step);
            //if (!audioSource.isPlaying)
            //{
            //    audioSource.Play();
                
            //}
            //audioSource.Stop();

            // Check if the player is at the target position
            if (transform.position == waypoints[waypointIndex].transform.position)
            {
                waypointIndex += 1;
                isHopping = true; // Start the hopping animation
                StartCoroutine(HopAnimation());
                
            }
        }
    }

    // Coroutine to handle the hopping animation
    private IEnumerator HopAnimation()
    {
        float elapsedTime = 0;

        // Calculate the target height for the hop
        float targetHeight = transform.position.y + hopHeight;

        while (elapsedTime < 0.5f) // Adjust the duration of the hop
        {
            elapsedTime += Time.deltaTime;

            // Calculate the interpolation factor
            float t = elapsedTime / 0.05f; // Adjust the duration of the hop

            // Interpolate between the original scale and a slightly stretched scale
            transform.localScale = Vector2.Lerp(originalScale, originalScale * 1.05f, t);

            // Interpolate the position vertically to create the hop effect
            transform.position = new Vector2(transform.position.x, Mathf.Lerp(transform.position.y, targetHeight, t));

            yield return null;
        }

        // Reset the scale and position after the hop animation
        transform.localScale = originalScale;
        isHopping = false;
    }

}