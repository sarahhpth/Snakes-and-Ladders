using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameControl : MonoBehaviour
{
    public static GameObject player1MoveText, player2MoveText;
    public TextMeshProUGUI whoWinsText;
    public static GameObject player1, player2;
    public static int diceSideThrown = 0;
    public static bool playerIsMoving = false;
    public static int player1StartWaypoint = 0;
    public static int player2StartWaypoint = 0;
    public static bool gameOver = false;

    // popup prompt
    public PopupManager popupManager; //script

    // scoring
    public static float player1Score = 0f;
    public static float player2Score = 0f;
    public static int player1Prompts = 0;
    public static int player2Prompts = 0;
    public TextMeshProUGUI player1ScoreText;
    public TextMeshProUGUI player2ScoreText;

    public GameObject gameOverPanel;

    // Start is called before the first frame update
    void Start()
    {
        //whoWinsText = GameObject.Find("WhoWinsText");
        player1MoveText = GameObject.Find("Player1MoveText");
        player2MoveText = GameObject.Find("Player2MoveText");

        player1 = GameObject.Find("Player1");
        player2 = GameObject.Find("Player2");

        player1.GetComponent<FollowPath>().moveAllowed = false;
        player2.GetComponent<FollowPath>().moveAllowed = false;

        whoWinsText.gameObject.SetActive(false);
        player1MoveText.SetActive(true);
        player2MoveText.SetActive(false);
        gameOverPanel.SetActive(false);

        UpdateScoreText();
    }
    
    // Update is called once per frame
    void Update()
    {
        if (gameOver)
            return;

        if (player1.GetComponent<FollowPath>().waypointIndex > player1StartWaypoint + diceSideThrown)
        {
            playerIsMoving = false;
            int newWaypointIndex = CheckSnakeOrLadder(player1);
            if (newWaypointIndex != -1) // climb up/slide down
            {
                player1.GetComponent<FollowPath>().waypointIndex = newWaypointIndex;
                player1StartWaypoint = player1.GetComponent<FollowPath>().waypointIndex;
                Vector2 newPosition = player1.GetComponent<FollowPath>().waypoints[newWaypointIndex].position;
                string newPositionTag = player1.GetComponent<FollowPath>().waypoints[newWaypointIndex].tag;
                StartCoroutine(ClimbUpSlideDown(player1.transform, newPosition, player1.GetComponent<FollowPath>().moveSpeed, newPositionTag));
            }
            else
            {
                player1StartWaypoint = player1.GetComponent<FollowPath>().waypointIndex -1;
            }

            player1.GetComponent<FollowPath>().moveAllowed = false;
            player1MoveText.SetActive(false);
            player2MoveText.SetActive(true);

            Debug.Log("Dice: " + diceSideThrown + "\n" + "player1StartWaypoint on index: " + player1StartWaypoint + "\n" + "Player 1 score: " + player1ScoreText.text);
            
        }

        if (player2.GetComponent<FollowPath>().waypointIndex > player2StartWaypoint + diceSideThrown)
        {
            playerIsMoving = false;
            int newWaypointIndex = CheckSnakeOrLadder(player2);
            if (newWaypointIndex != -1) // climb up/slide down
            {
                player2.GetComponent<FollowPath>().waypointIndex = newWaypointIndex;
                player2StartWaypoint = player2.GetComponent<FollowPath>().waypointIndex;
                Vector2 newPosition = player2.GetComponent<FollowPath>().waypoints[newWaypointIndex].position;
                string newPositionTag = player2.GetComponent<FollowPath>().waypoints[newWaypointIndex].tag;
                StartCoroutine(ClimbUpSlideDown(player2.transform, newPosition, player2.GetComponent<FollowPath>().moveSpeed, newPositionTag));
            }
            else
            {
                player2StartWaypoint = player2.GetComponent<FollowPath>().waypointIndex -1;
            }

            player2.GetComponent<FollowPath>().moveAllowed = false;
            player2MoveText.SetActive(false);
            player1MoveText.SetActive(true);

            Debug.Log("Dice: " + diceSideThrown + "\n" + "player2StartWaypoint on index: " + player2StartWaypoint + "\n" + "Player 2 score: " + player2ScoreText.text);
        }

        if (player1.GetComponent<FollowPath>().waypointIndex == player1.GetComponent<FollowPath>().waypoints.Length)
        {
            EndGame("Player 1 wins!");
        }

        if (player2.GetComponent<FollowPath>().waypointIndex == player2.GetComponent<FollowPath>().waypoints.Length)
        {
            EndGame("Player 2 wins!");
        }

        UpdateScoreText();
    }

    public static void MovePlayer(int playerToMove)
    {

        switch (playerToMove)
        {
            case 1:
                player1.GetComponent<FollowPath>().moveAllowed = true;
                break;

            case 2:
                player2.GetComponent<FollowPath>().moveAllowed = true;
                break;
        }
    }


    int CheckSnakeOrLadder(GameObject player)
    {
        FollowPath playerPath = player.GetComponent<FollowPath>();
        Transform[] waypoints = playerPath.waypoints;
        int waypointIndex = playerPath.waypointIndex - 1;

        Transform currentWaypoint = waypoints[waypointIndex];
        string currentTag = currentWaypoint.tag;


        if (currentTag.StartsWith("Bottom"))
        {
            // Get the ladder number from the tag
            string ladderTag = currentTag.Replace("Bottom", "");
            Debug.Log("Ladder tag: " + ladderTag);
            int ladderNumber = int.Parse(ladderTag);

            // Find the corresponding end point of the ladder
            for (int i = waypointIndex + 1; i < waypoints.Length; i++)
            {
                if (waypoints[i].CompareTag("Top" + ladderNumber))
                {
                    playerPath.waypointIndex = i;
                    //ClimbUpSlideDown(player.transform, waypoints[i].position, playerPath.moveSpeed);
                    return i;
                }
            }
        }
        else if (currentTag.StartsWith("Head"))
        {
            // Get the snake number from the tag
            string snakeTag = currentTag.Replace("Head", "");
            Debug.Log("Snake tag: " + snakeTag);
            int snakeNumber = int.Parse(snakeTag);


            // Find the corresponding end point of the snake
            for (int i = waypointIndex - 1; i >= 0; i--)
            {
                if (waypoints[i].CompareTag("Tail" + snakeNumber))
                {
                    playerPath.waypointIndex = i;
                    //ClimbUpSlideDown(player.transform, waypoints[i].position, playerPath.moveSpeed);
                    return i;
                }
            }
        }

        return -1; //if its neither snake/ladder

    }

    IEnumerator ClimbUpSlideDown(Transform playerTransform, Vector2 targetPosition, float moveSpeed, string currentTag)
    {
        while (Vector2.Distance(playerTransform.position, targetPosition) > 0.01f)
        {
            playerTransform.position = Vector2.MoveTowards(playerTransform.position, targetPosition, moveSpeed * Time.deltaTime);
            yield return null;
        }

        // show popup prompt. call PopupManager.cs
        popupManager.ShowPopupPrompt(currentTag);
    }

    void UpdateScoreText()
    {
        player1ScoreText.text = player1Score.ToString();
        player2ScoreText.text = player2Score.ToString();
    }

    void EndGame(string winnerMessage)
    {
        if (gameOver)
            return; 

        whoWinsText.gameObject.SetActive(true);
        whoWinsText.text = winnerMessage;
        player1MoveText.SetActive(false);
        player2MoveText.SetActive(false);
        Debug.Log(winnerMessage);

        gameOver = true;
        gameOverPanel.SetActive(true);
    }

    public void RestartGame()
    {
        gameOver = false;
        gameOverPanel.SetActive(false);

        // reset player position
        player1.GetComponent<FollowPath>().waypointIndex = 0;
        player2.GetComponent<FollowPath>().waypointIndex = 0;
        player1.transform.position = player1.GetComponent<FollowPath>().waypoints[0].position;
        player2.transform.position = player2.GetComponent<FollowPath>().waypoints[0].position;

        // reset game state
        player1StartWaypoint = 0;
        player2StartWaypoint = 0;
        player1Score = 0f;
        player2Score = 0f;
        player1Prompts = 0;
        player2Prompts = 0;
        player1MoveText.SetActive(true);
        player2MoveText.SetActive(false);

        diceSideThrown = 0;
        player1.GetComponent<FollowPath>().moveAllowed = false;
        player2.GetComponent<FollowPath>().moveAllowed = false;

        dice.whosTurn = 1;

        UpdateScoreText();
    }

    public void QuitGame()
    {
        // if running in unity editor
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

}
