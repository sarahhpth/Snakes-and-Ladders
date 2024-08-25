using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class dice : MonoBehaviour
{
    //array of dice sides sprites to load from Resoorce folder
    private Sprite[] diceSides;

    //reference to sprite renderer to change sprites
    private SpriteRenderer rend;

    public static int whosTurn = 1;
    private bool coroutineAllowed = true;
    public PopupManager popupManager;

    private void Start()
    {
        rend = GetComponent<SpriteRenderer>();
        diceSides = Resources.LoadAll<Sprite>("DiceSides/");
    }

    // click over the dice, RollTheDice coroutine is started
    private void OnMouseDown()
    {
        if (!GameControl.gameOver && coroutineAllowed && !popupManager.popupPrompt.activeSelf && !GameControl.playerIsMoving)
            StartCoroutine("RollTheDice");
    }


    // Coroutine that rolls the dice
    private IEnumerator RollTheDice()
    {
        coroutineAllowed = false;
        int randomDiceSide = 0;

        // Loop to switch dice sides ramdomly before final side appears. 15 iterations here.
        for (int i = 0; i <= 15; i++)
        {
            // Pick up random value from 0 to 5 
            randomDiceSide = Random.Range(0, 6);

            // Set sprite to upper face of dice from array according to random value
            rend.sprite = diceSides[randomDiceSide];

            // Pause before next itteration
            yield return new WaitForSeconds(0.05f);
        }

        // Assigning final side
        GameControl.diceSideThrown = randomDiceSide + 1;
        if (whosTurn == 1)
        {
            GameControl.MovePlayer(1);
        }
        else if (whosTurn == -1)
        {
            GameControl.MovePlayer(2);
        }
        whosTurn *= -1;
        coroutineAllowed = true;

    }
}
